/**
 * Cắt sổ công và đối chiếu hai sổ.
 *
 * Ba điều phải giữ, kiểm hết ở đây:
 *   1. Sổ gửi đi **không mang theo đồng tiền nào** — cắt ở lúc đóng gói, không phải ở
 *      giao diện. Gói đã gửi là nằm trong tay người ta.
 *   2. Chỉ so trong phần giao của hai khoảng ngày, không lôi ngày máy thợ chưa có app ra
 *      bắt lỗi.
 *   3. Buổi đã quyết toán thì khoá, lấy theo sổ bên kia phải hỏng.
 */

import { DaChotKhongSuaDuoc, doiChieu, layTheoBenKia } from '../doiChieu';
import { DuLieuChamCong, duLieuRong } from '../kieu';
import { quyetToan } from '../ky';
import { SoCong, catSo, cuaSoCuaChu } from '../soCong';
import { cham, dangCham, themTho, themUng } from '../thaoTac';

const NGAY_TAO = '2026-07-01';
const TAO_LUC = '2026-08-19T08:00:00.000Z';
/** Đứng sau mọi ngày trong bài này, để không vướng luật tạm gác buổi của hôm nay. */
const HOM_NAY = '2026-08-31';

function kho(): { duLieu: DuLieuChamCong; tuan: string } {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, NGAY_TAO);
  return { duLieu: them.duLieu, tuan: them.tho.id };
}

/** Sổ do máy thợ làm ra: không có kỳ nên không có cờ đã chốt. */
function soTho(
  thoId: string,
  dongs: { ngay: string; buoi: 'Sang' | 'Chieu'; soCong: number }[],
  tuNgay = '2026-08-01',
  denNgay = '2026-08-31',
): SoCong {
  return { thoId, tenTho: 'Anh Tuấn', nguon: 'tho', tuNgay, denNgay, dongs, taoLuc: TAO_LUC };
}

describe('catSo', () => {
  it('chỉ lấy đúng thợ được hỏi, và không mang theo tiền', () => {
    let { duLieu, tuan } = kho();
    const themBinh = themTho(duLieu, 'Anh Bình', 250_000, NGAY_TAO);
    duLieu = themBinh.duLieu;

    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = cham(duLieu, themBinh.tho.id, '2026-08-10', 'Sang');
    duLieu = themUng(duLieu, tuan, '2026-08-10', 500_000);

    const so = catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC);

    expect(so.dongs).toEqual([{ ngay: '2026-08-10', buoi: 'Sang', soCong: 1 }]);
    expect(so.tenTho).toBe('Anh Tuấn');

    // Không tin vào mắt mình mà đọc từng trường: soát cả gói xem có số tiền nào lọt ra.
    const chu = JSON.stringify(so);
    expect(chu).not.toContain('300000');
    expect(chu).not.toContain('500000');
    expect(chu).not.toContain('tienMotCong');
    expect(chu).not.toContain('soTien');
  });

  it('bỏ buổi ngoài khoảng ngày đã khai', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-07-20', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-09-05', 'Sang');

    const so = catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC);
    expect(so.dongs.map((d) => d.ngay)).toEqual(['2026-08-10']);
  });

  it('đánh dấu buổi đã quyết toán', () => {
    let { duLieu, tuan } = kho();

    // Chấm ngày 10 rồi chốt kỳ; ngày 11 chấm sau nên rơi vào kỳ đang mở.
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = quyetToan(duLieu, {
      denNgay: '2026-08-10',
      chotLuc: '2026-08-10T18:00:00.000Z',
      daTra: new Map([[tuan, 300_000]]),
    });
    duLieu = cham(duLieu, tuan, '2026-08-11', 'Sang');

    const so = catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC);
    expect(so.dongs).toEqual([
      { ngay: '2026-08-10', buoi: 'Sang', soCong: 1, daChot: true },
      { ngay: '2026-08-11', buoi: 'Sang', soCong: 1 },
    ]);
  });
});

describe('doiChieu', () => {
  function soChu(duLieu: DuLieuChamCong, thoId: string): SoCong {
    return catSo(duLieu, thoId, 'chu', '2026-08-01', '2026-08-31', TAO_LUC);
  }

  it('hai sổ giống nhau thì không có dòng lệch nào', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Chieu');

    const ket = doiChieu(
      soChu(duLieu, tuan),
      soTho(tuan, [
        { ngay: '2026-08-10', buoi: 'Sang', soCong: 1 },
        { ngay: '2026-08-10', buoi: 'Chieu', soCong: 1 },
      ]),
      HOM_NAY,
    );

    expect(ket.lechs).toEqual([]);
    expect(ket.soKhop).toBe(2);
    expect(ket.tongCongMinh).toBe(2);
    expect(ket.tongCongBenKia).toBe(2);
  });

  it('gọi tên đúng ba loại lệch', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang'); // thợ không chấm
    duLieu = cham(duLieu, tuan, '2026-08-11', 'Sang', 0.5); // hai bên lệch số công

    const ket = doiChieu(
      soChu(duLieu, tuan),
      soTho(tuan, [
        { ngay: '2026-08-11', buoi: 'Sang', soCong: 1 },
        { ngay: '2026-08-12', buoi: 'Chieu', soCong: 1 }, // chủ không có
      ]),
      HOM_NAY,
    );

    expect(ket.lechs).toEqual([
      { ngay: '2026-08-10', buoi: 'Sang', soCongMinh: 1, soCongBenKia: null, loai: 'chiMinhCo', daChot: false },
      { ngay: '2026-08-11', buoi: 'Sang', soCongMinh: 0.5, soCongBenKia: 1, loai: 'lechSoCong', daChot: false },
      { ngay: '2026-08-12', buoi: 'Chieu', soCongMinh: null, soCongBenKia: 1, loai: 'chiBenKiaCo', daChot: false },
    ]);
  });

  it('chỉ so trong phần giao của hai khoảng ngày', () => {
    let { duLieu, tuan } = kho();
    // Chủ có cả tháng 8; máy thợ mới cài ngày 15 nên chỉ khai từ 15.
    duLieu = cham(duLieu, tuan, '2026-08-05', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-08-20', 'Sang');

    const ket = doiChieu(
      soChu(duLieu, tuan),
      soTho(tuan, [{ ngay: '2026-08-20', buoi: 'Sang', soCong: 1 }], '2026-08-15', '2026-08-31'),
      HOM_NAY,
    );

    // Buổi ngày 5 không bị mang ra bắt lỗi: hôm ấy thợ chưa có app.
    expect(ket.lechs).toEqual([]);
    expect(ket.tuNgay).toBe('2026-08-15');
    expect(ket.denNgay).toBe('2026-08-31');
  });

  /**
   * Luật của **hôm nay**: bên chưa chấm không phải là bên nói "nghỉ", vì ngày còn đang chạy.
   * Đây là chỗ máy thợ vừa cài xong mở đối chiếu ra đã thấy đỏ, xem ghi chú ở `doiChieu`.
   */
  describe('buổi của hôm nay', () => {
    it('chỉ một bên chấm thì tạm gác, không báo lệch và không cộng vào tổng', () => {
      let { duLieu, tuan } = kho();
      duLieu = cham(duLieu, tuan, '2026-08-20', 'Sang');
      duLieu = cham(duLieu, tuan, '2026-08-20', 'Chieu');

      const ket = doiChieu(
        soChu(duLieu, tuan),
        // Máy thợ vừa cài hôm nay, chưa chấm ô nào.
        soTho(tuan, [], '2026-08-20', '2026-08-20'),
        '2026-08-20',
      );

      expect(ket.lechs).toEqual([]);
      expect(ket.soTamGac).toBe(2);
      expect(ket.soKhop).toBe(0);
      expect(ket.tongCongMinh).toBe(0);
      expect(ket.tongCongBenKia).toBe(0);
    });

    it('cả hai bên đều chấm mà số công khác nhau thì vẫn báo lệch', () => {
      let { duLieu, tuan } = kho();
      duLieu = cham(duLieu, tuan, '2026-08-20', 'Sang', 1);

      const ket = doiChieu(
        soChu(duLieu, tuan),
        soTho(tuan, [{ ngay: '2026-08-20', buoi: 'Sang', soCong: 0.5 }], '2026-08-20', '2026-08-20'),
        '2026-08-20',
      );

      expect(ket.lechs).toEqual([
        { ngay: '2026-08-20', buoi: 'Sang', soCongMinh: 1, soCongBenKia: 0.5, loai: 'lechSoCong', daChot: false },
      ]);
      expect(ket.soTamGac).toBe(0);
      expect(ket.tongCongMinh).toBe(1);
      expect(ket.tongCongBenKia).toBe(0.5);
    });

    it('ngày đã qua thì thiếu một bên vẫn là lệch', () => {
      let { duLieu, tuan } = kho();
      duLieu = cham(duLieu, tuan, '2026-08-19', 'Sang');

      const ket = doiChieu(soChu(duLieu, tuan), soTho(tuan, []), '2026-08-20');

      expect(ket.lechs.map((l) => l.loai)).toEqual(['chiMinhCo']);
      expect(ket.soTamGac).toBe(0);
    });
  });

  it('trong một ngày thì Sáng đứng trước Chiều', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Chieu');

    const ket = doiChieu(soChu(duLieu, tuan), soTho(tuan, []), HOM_NAY);

    expect(ket.lechs.map((lech) => lech.buoi)).toEqual(['Sang', 'Chieu']);
  });

  it('hai sổ không có ngày nào chung thì nói thẳng là chưa so được', () => {
    const { duLieu, tuan } = kho();
    const ket = doiChieu(
      soChu(duLieu, tuan),
      soTho(tuan, [], '2026-09-01', '2026-09-30'),
      HOM_NAY,
    );

    expect(ket.khongTrungKhoang).toBe(true);
    expect(ket.lechs).toEqual([]);
  });
});

describe('layTheoBenKia', () => {
  it('ghi số công của bên kia vào sổ mình', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-11', 'Sang', 0.5);

    const ket = doiChieu(
      catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC),
      soTho(tuan, [{ ngay: '2026-08-11', buoi: 'Sang', soCong: 1 }]),
      HOM_NAY,
    );

    duLieu = layTheoBenKia(duLieu, tuan, ket.lechs[0]);
    expect(dangCham(duLieu, tuan, '2026-08-11', 'Sang')?.soCong).toBe(1);
  });

  it('bên kia không chấm thì bỏ chấm bên mình', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-11', 'Sang');

    const ket = doiChieu(
      catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC),
      soTho(tuan, []),
      HOM_NAY,
    );

    duLieu = layTheoBenKia(duLieu, tuan, ket.lechs[0]);
    expect(dangCham(duLieu, tuan, '2026-08-11', 'Sang')).toBeUndefined();
  });

  it('không sửa được buổi đã quyết toán', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');

    duLieu = quyetToan(duLieu, {
      denNgay: '2026-08-10',
      chotLuc: '2026-08-10T18:00:00.000Z',
      daTra: new Map([[tuan, 300_000]]),
    });

    const ket = doiChieu(
      catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC),
      soTho(tuan, [{ ngay: '2026-08-10', buoi: 'Sang', soCong: 2 }]),
      HOM_NAY,
    );

    expect(ket.lechs[0].daChot).toBe(true);
    expect(() => layTheoBenKia(duLieu, tuan, ket.lechs[0])).toThrow(DaChotKhongSuaDuoc);
  });
});

describe('cuaSoCuaChu', () => {
  it('khai đúng 90 ngày gần nhất', () => {
    expect(cuaSoCuaChu('2026-08-19')).toEqual({ tuNgay: '2026-05-21', denNgay: '2026-08-19' });
  });
});
