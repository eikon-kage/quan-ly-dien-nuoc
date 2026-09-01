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

import {
  ChuaBietKhongLayDuoc,
  DaChotKhongSuaDuoc,
  doiChieu,
  layTheoBenKia,
  tongChuaBiet,
} from '../doiChieu';
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

/** Id của Tuấn khi bài kiểm thử chỉ cần một id, không cần cả cái kho. */
function tuan(): string {
  return kho().tuan;
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

    expect(so.dongs).toEqual([{ ngay: '2026-08-10', buoi: 'Sang', soCong: 0.5 }]);
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
      daTra: new Map([[tuan, 150_000]]),
    });
    duLieu = cham(duLieu, tuan, '2026-08-11', 'Sang');

    const so = catSo(duLieu, tuan, 'chu', '2026-08-01', '2026-08-31', TAO_LUC);
    expect(so.dongs).toEqual([
      { ngay: '2026-08-10', buoi: 'Sang', soCong: 0.5, daChot: true },
      { ngay: '2026-08-11', buoi: 'Sang', soCong: 0.5 },
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
        { ngay: '2026-08-10', buoi: 'Sang', soCong: 0.5 },
        { ngay: '2026-08-10', buoi: 'Chieu', soCong: 0.5 },
      ]),
      HOM_NAY,
    );

    expect(ket.lechs).toEqual([]);
    expect(ket.soKhop).toBe(2);
    // Hai buổi khớp nhau là đúng một công — cả ngày đi đủ.
    expect(ket.tongCongMinh).toBe(1);
    expect(ket.tongCongBenKia).toBe(1);
  });

  it('gọi tên đúng ba loại lệch', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang'); // thợ không chấm
    duLieu = cham(duLieu, tuan, '2026-08-11', 'Sang', 0.25); // hai bên lệch số công

    const ket = doiChieu(
      soChu(duLieu, tuan),
      soTho(tuan, [
        { ngay: '2026-08-11', buoi: 'Sang', soCong: 0.5 },
        { ngay: '2026-08-12', buoi: 'Chieu', soCong: 0.5 }, // chủ không có
      ]),
      HOM_NAY,
    );

    expect(ket.lechs).toEqual([
      { ngay: '2026-08-10', buoi: 'Sang', soCongMinh: 0.5, soCongBenKia: null, loai: 'chiMinhCo', daChot: false },
      { ngay: '2026-08-11', buoi: 'Sang', soCongMinh: 0.25, soCongBenKia: 0.5, loai: 'lechSoCong', daChot: false },
      { ngay: '2026-08-12', buoi: 'Chieu', soCongMinh: null, soCongBenKia: 0.5, loai: 'chiBenKiaCo', daChot: false },
    ]);
  });

  it('chỉ so trong phần giao của hai khoảng ngày', () => {
    let { duLieu, tuan } = kho();
    // Chủ có cả tháng 8; máy thợ mới cài ngày 15 nên chỉ khai từ 15.
    duLieu = cham(duLieu, tuan, '2026-08-05', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-08-20', 'Sang');

    const ket = doiChieu(
      soChu(duLieu, tuan),
      soTho(tuan, [{ ngay: '2026-08-20', buoi: 'Sang', soCong: 0.5 }], '2026-08-15', '2026-08-31'),
      HOM_NAY,
    );

    // Buổi ngày 5 không bị mang ra bắt lỗi: hôm ấy thợ chưa có app. Nhưng cũng không mất
    // hút — ở bài này sổ mình là sổ chủ, nên nó là buổi *bên kia chưa biết*.
    expect(ket.lechs).toEqual([]);
    expect(ket.chuaBiets.map((l) => [l.ngay, l.loai])).toEqual([['2026-08-05', 'benKiaChuaBiet']]);
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

  /**
   * Khoảng đầu trang đọc lên thành "So từ ... đến ...", nên nó **không được ngược**.
   *
   * Chỗ ngược được là khi hai khoảng khai không giao nhau: `giaoTu` với `giaoDen` lúc ấy là
   * hai mốc chéo nhau, lấy nguyên ra là câu "So từ 20/08 đến 19/08". Hai đường chặn: so được
   * buổi nào thì khoảng nói theo buổi ấy, còn không so được gì thì `khongTrungKhoang` bật lên
   * và màn hình ẩn hẳn khoảng đi.
   */
  describe('khoảng nói ở đầu trang', () => {
    /** Chủ chấm theo lô nên sổ mới tới hôm qua; máy thợ vừa nhận vai nên chỉ khai hôm nay. */
    function lechDungMotHom(): { soTho: SoCong; soChu: SoCong } {
      const thoId = tuan();
      return {
        soTho: soTho(thoId, [{ ngay: '2026-08-20', buoi: 'Sang', soCong: 1 }], '2026-08-20', '2026-08-20'),
        soChu: {
          thoId,
          tenTho: 'Anh Tuấn',
          nguon: 'chu',
          tuNgay: '2026-08-01',
          denNgay: '2026-08-19',
          dongs: [{ ngay: '2026-08-19', buoi: 'Sang', soCong: 1 }],
          taoLuc: TAO_LUC,
        },
      };
    }

    it('chỉ có buổi tạm gác của hôm nay thì vẫn là chưa có ngày nào chung', () => {
      const { soTho: cuaTho, soChu: cuaChu } = lechDungMotHom();
      const ket = doiChieu(cuaTho, cuaChu, '2026-08-20');

      // Một buổi mới một bên chấm của hôm nay không phải là một buổi đã so được.
      expect(ket.soKhop).toBe(0);
      expect(ket.soTamGac).toBe(1);
      expect(ket.khongTrungKhoang).toBe(true);
      // `khongTrungKhoang` bật là lời canh cho hai mốc kia: chúng chéo nhau (20 > 19) nên
      // không được đọc lên thành câu "So từ ... đến ...". Màn hình ẩn cả khối ấy.
    });

    it('nhìn từ máy chủ cũng vậy — hàm chạy đúng ở cả hai bên', () => {
      const { soTho: cuaTho, soChu: cuaChu } = lechDungMotHom();
      const ket = doiChieu(cuaChu, cuaTho, '2026-08-20');

      expect(ket.khongTrungKhoang).toBe(true);
      expect(ket.chuaBiets.map((l) => [l.ngay, l.loai])).toEqual([['2026-08-19', 'benKiaChuaBiet']]);
    });

    it('hai khoảng khai không giao nhau mà có buổi so được thì chỉ nói phần đã so', () => {
      const thoId = tuan();
      const ket = doiChieu(
        // Máy thợ khai từ 20, gửi kèm buổi chấm bù ngày 15.
        soTho(thoId, [{ ngay: '2026-08-15', buoi: 'Sang', soCong: 1 }], '2026-08-20', '2026-08-20'),
        {
          thoId,
          tenTho: 'Anh Tuấn',
          nguon: 'chu',
          tuNgay: '2026-08-15',
          denNgay: '2026-08-19',
          dongs: [{ ngay: '2026-08-15', buoi: 'Sang', soCong: 1 }],
          taoLuc: TAO_LUC,
        },
        '2026-08-20',
      );

      // Đúng một buổi được so, nên đầu trang nói đúng một ngày ấy — không nới tới 19 theo
      // mốc khai của chủ, vì trong khoảng 16–19 hai sổ chưa hề so gì với nhau.
      expect(ket.soKhop).toBe(1);
      expect(ket.khongTrungKhoang).toBe(false);
      expect([ket.tuNgay, ket.denNgay]).toEqual(['2026-08-15', '2026-08-15']);
    });
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

/**
 * Khoảng máy chủ khai là đầy đủ. Đây là một **lời quả quyết** — trong khoảng ấy, không có
 * dòng nghĩa là thợ nghỉ — nên khai rộng hơn phần chủ thật sự đã ghi là nói dối, và lời nói
 * dối ấy hiện ra thành mấy chục dòng đỏ trên máy thợ.
 */
describe('cuaSoCuaChu', () => {
  function khoChamCac(ngays: string[]): DuLieuChamCong {
    let { duLieu, tuan } = kho();
    for (const ngay of ngays) {
      duLieu = cham(duLieu, tuan, ngay, 'Sang', 1);
    }
    return duLieu;
  }

  it('chủ ghi đủ dài thì khai đúng 90 ngày gần nhất', () => {
    const duLieu = khoChamCac(['2026-05-21', '2026-07-01', '2026-08-19']);
    expect(cuaSoCuaChu(duLieu, '2026-08-19')).toEqual({
      tuNgay: '2026-05-21',
      denNgay: '2026-08-19',
    });
  });

  it('chủ mới cài app thì không khai lùi về trước hôm chủ có sổ', () => {
    const duLieu = khoChamCac(['2026-08-17', '2026-08-18', '2026-08-19']);

    // Khai 90 ngày ở đây là quả quyết "ba tháng qua thợ nghỉ hết", mà thợ dùng app cả tháng
    // thì đối chiếu ra hai bảy dòng "chủ không chấm" — toàn là ngày chủ chưa có app.
    expect(cuaSoCuaChu(duLieu, '2026-08-19')).toEqual({
      tuNgay: '2026-08-17',
      denNgay: '2026-08-19',
    });
  });

  it('chủ chưa nhập công hôm qua thì dừng ở ngày chủ ghi cuối cùng', () => {
    const duLieu = khoChamCac(['2026-08-10', '2026-08-17']);

    // Chủ chấm cho cả nhóm theo lô, chậm một hai hôm là thường. Khai tới 19 là quả quyết
    // "18 với 19 cả nhóm nghỉ", nên sáng nào thợ mở app cũng thấy hai dòng đỏ của hôm qua.
    expect(cuaSoCuaChu(duLieu, '2026-08-19').denNgay).toBe('2026-08-17');
  });

  it('lấy theo ngày của cả nhóm, không phải của riêng một thợ', () => {
    let { duLieu, tuan } = kho();
    const themBinh = themTho(duLieu, 'Anh Bình', 250_000, NGAY_TAO);
    duLieu = cham(themBinh.duLieu, tuan, '2026-08-10', 'Sang', 1);
    duLieu = cham(duLieu, themBinh.tho.id, '2026-08-19', 'Sang', 1);

    // Tuấn nghỉ từ 11 đến 19, mà chủ vẫn đang nhập sổ bình thường. Cắt theo riêng Tuấn thì
    // mất luôn phần sổ quanh ngày nghỉ của anh ấy.
    expect(cuaSoCuaChu(duLieu, '2026-08-19')).toEqual({
      tuNgay: '2026-08-10',
      denNgay: '2026-08-19',
    });
  });

  it('chủ chưa ghi gì thì khai đúng một ngày hôm nay', () => {
    expect(cuaSoCuaChu(kho().duLieu, '2026-08-19')).toEqual({
      tuNgay: '2026-08-19',
      denNgay: '2026-08-19',
    });
  });
});

/**
 * Buổi mà **một bên không biết** ngày ấy thì không phải chỗ hai bên nói khác nhau.
 *
 * Đây là chỗ sai cũ: hàm chỉ so trong phần giao hai khoảng khai, nên (a) dòng chấm bù nằm
 * ngoài khoảng khai biến mất khỏi đối chiếu, và (b) để chữa việc mất ấy, máy thợ phải khai
 * bừa xuống tận buổi chấm bù — rồi mấy ngày trống giữa đó thành "thợ nghỉ" và ra một màn
 * hình đỏ rực đúng cho người vừa cài app.
 */
describe('bên không biết ngày ấy thì không tính là lệch', () => {
  const TU_20 = { tuNgay: '2026-08-20', denNgay: '2026-08-20' };

  it('thợ vừa cài app, chấm bù một buổi: buổi ấy vẫn được so, mấy ngày trống thì không', () => {
    let { duLieu, tuan } = kho();
    for (const ngay of ['2026-08-15', '2026-08-16', '2026-08-17', '2026-08-18', '2026-08-19']) {
      duLieu = cham(duLieu, tuan, ngay, 'Sang');
      duLieu = cham(duLieu, tuan, ngay, 'Chieu');
    }

    const ket = doiChieu(
      // Máy thợ khai đúng hôm nhận vai máy, nhưng gửi kèm buổi chấm bù ngày 15.
      soTho(tuan, [{ ngay: '2026-08-15', buoi: 'Sang', soCong: 0.5 }], TU_20.tuNgay, TU_20.denNgay),
      catSo(duLieu, tuan, 'chu', '2026-08-15', '2026-08-19', TAO_LUC),
      '2026-08-20',
    );

    // Buổi chấm bù được so và khớp — công không mất hút.
    expect(ket.soKhop).toBe(1);
    // Chín buổi kia là ngày máy thợ chưa tồn tại: không phải lệch, nhưng vẫn phải hiện ra
    // ở phần riêng — chủ đã chấm thật, và thợ chấm bù được từng buổi.
    expect(ket.lechs).toEqual([]);
    expect(ket.chuaBiets).toHaveLength(9);
    expect(new Set(ket.chuaBiets.map((l) => l.loai))).toEqual(new Set(['minhChuaBiet']));
    // Đầu trang nói khoảng phủ được buổi đã so, không phải khoảng giao rỗng.
    expect(ket.tuNgay).toBe('2026-08-15');
  });

  it('buổi chấm bù mà chủ không có thì vẫn báo lệch — chủ biết ngày ấy', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-15', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-08-19', 'Sang');

    const ket = doiChieu(
      soTho(
        tuan,
        [
          { ngay: '2026-08-15', buoi: 'Sang', soCong: 0.5 },
          // Chủ quên chấm buổi này, mà nó nằm giữa khoảng chủ khai là đầy đủ.
          { ngay: '2026-08-17', buoi: 'Sang', soCong: 0.5 },
        ],
        TU_20.tuNgay,
        TU_20.denNgay,
      ),
      catSo(duLieu, tuan, 'chu', '2026-08-15', '2026-08-19', TAO_LUC),
      '2026-08-20',
    );

    expect(ket.lechs.map((l) => [l.ngay, l.loai])).toEqual([['2026-08-17', 'chiMinhCo']]);
  });

  it('ngày chủ chưa nhập tới thì buổi thợ đã chấm không thành dòng đỏ', () => {
    const ket = doiChieu(
      soTho(
        tuan(),
        [
          { ngay: '2026-08-17', buoi: 'Sang', soCong: 1 },
          { ngay: '2026-08-19', buoi: 'Sang', soCong: 1 },
        ],
        '2026-08-01',
        '2026-08-20',
      ),
      // Chủ mới nhập tới ngày 17.
      {
        thoId: tuan(),
        tenTho: 'Anh Tuấn',
        nguon: 'chu',
        tuNgay: '2026-08-01',
        denNgay: '2026-08-17',
        dongs: [{ ngay: '2026-08-17', buoi: 'Sang', soCong: 1 }],
        taoLuc: TAO_LUC,
      },
      '2026-08-20',
    );

    expect(ket.soKhop).toBe(1);
    expect(ket.lechs).toEqual([]);
    // Buổi ngày 19 chờ chủ nhập tới, không phải chỗ hai bên nói khác nhau.
    expect(ket.chuaBiets.map((l) => [l.ngay, l.loai])).toEqual([['2026-08-19', 'benKiaChuaBiet']]);
  });
});

/**
 * Buổi **chỉ một bên có sổ** phải hiện ra, không được bỏ đi lặng lẽ.
 *
 * Đây là chỗ sai được kể trong `KetQuaDoiChieu.chuaBiets`: chỉ so từ phía sổ mình ra, nên
 * ngày chủ đã chấm mà máy thợ chưa biết thì rơi mất — hai tổng đầu trang đọc thành "chủ chấm
 * thiếu", rồi thợ chấm bù đúng ngày ấy là hai tổng nhảy số.
 */
describe('buổi chỉ một bên có sổ', () => {
  /** Chủ chấm 17 với 18, thợ chấm 18 với 19, mỗi ngày hai buổi. Máy thợ khai từ 18. */
  function haiSoLechNgay() {
    const thoId = tuan();
    const hai = (ngay: string) => [
      { ngay, buoi: 'Sang' as const, soCong: 1 },
      { ngay, buoi: 'Chieu' as const, soCong: 1 },
    ];
    return {
      thoId,
      soMinh: soTho(thoId, [...hai('2026-08-18'), ...hai('2026-08-19')], '2026-08-18', '2026-08-20'),
      soChu: {
        thoId,
        tenTho: 'Anh Tuấn',
        nguon: 'chu' as const,
        tuNgay: '2026-08-17',
        denNgay: '2026-08-19',
        dongs: [...hai('2026-08-17'), ...hai('2026-08-18')],
        taoLuc: TAO_LUC,
      },
    };
  }

  it('công chủ đã chấm ở ngày máy thợ chưa biết thì vẫn hiện ra', () => {
    const { soMinh, soChu } = haiSoLechNgay();
    const ket = doiChieu(soMinh, soChu, '2026-08-20');

    // Ngày 17: máy thợ chưa tồn tại nên không phải lệch, nhưng chủ chấm 2 công thật.
    expect(ket.chuaBiets.map((l) => [l.ngay, l.buoi, l.loai])).toEqual([
      ['2026-08-17', 'Sang', 'minhChuaBiet'],
      ['2026-08-17', 'Chieu', 'minhChuaBiet'],
    ]);
    expect(tongChuaBiet(ket.chuaBiets)).toEqual({ minh: 0, benKia: 2 });

    // Tổng nói đúng cái mỗi sổ ghi: chủ chấm 17 với 18 là 4 công, thợ chấm 18 với 19 cũng 4.
    // Hai công của ngày 17 nằm trong tổng của chủ, và có câu riêng chỉ ra chúng chưa so được.
    expect(ket.tongCongMinh).toBe(4);
    expect(ket.tongCongBenKia).toBe(4);
    expect(ket.lechs.map((l) => [l.ngay, l.buoi, l.loai])).toEqual([
      ['2026-08-19', 'Sang', 'chiMinhCo'],
      ['2026-08-19', 'Chieu', 'chiMinhCo'],
    ]);
  });

  it('thợ chấm bù đúng ngày ấy thì buổi chuyển sang khớp, không sinh thêm công lạ', () => {
    const { soMinh, soChu } = haiSoLechNgay();
    const daChamBu = {
      ...soMinh,
      dongs: [
        { ngay: '2026-08-17', buoi: 'Sang' as const, soCong: 1 },
        { ngay: '2026-08-17', buoi: 'Chieu' as const, soCong: 1 },
        ...soMinh.dongs,
      ],
    };

    const ket = doiChieu(daChamBu, soChu, '2026-08-20');

    // Đây là chỗ hai tổng phải **đứng yên** về phía sổ chủ: chấm bù là việc của sổ thợ, sổ
    // chủ không đổi một chữ nào. Trước 4, sau vẫn 4; chỉ sổ thợ tăng từ 4 lên 6.
    expect(ket.chuaBiets).toEqual([]);
    expect(ket.soKhop).toBe(4);
    expect(ket.tongCongMinh).toBe(6);
    expect(ket.tongCongBenKia).toBe(4);
  });

  it('lấy theo sổ bên kia được: đó là chấm bù vào sổ mình', () => {
    let { duLieu, tuan } = kho();
    const ket = doiChieu(
      // Sổ mình (máy thợ) khai từ 18, chưa biết gì ngày 17.
      soTho(tuan, [], '2026-08-18', '2026-08-20'),
      {
        thoId: tuan,
        tenTho: 'Anh Tuấn',
        nguon: 'chu',
        tuNgay: '2026-08-17',
        denNgay: '2026-08-18',
        dongs: [{ ngay: '2026-08-17', buoi: 'Sang', soCong: 1 }],
        taoLuc: TAO_LUC,
      },
      '2026-08-20',
    );

    duLieu = layTheoBenKia(duLieu, tuan, ket.chuaBiets[0]);
    expect(dangCham(duLieu, tuan, '2026-08-17', 'Sang')?.soCong).toBe(1);
  });

  it('bên kia chưa biết ngày ấy thì không lấy theo được — lấy là xoá công thật của mình', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-08-19', 'Sang');

    const ket = doiChieu(
      soTho(tuan, [{ ngay: '2026-08-19', buoi: 'Sang', soCong: 0.5 }], '2026-08-01', '2026-08-20'),
      // Chủ mới nhập tới ngày 17, chưa biết gì ngày 19.
      {
        thoId: tuan,
        tenTho: 'Anh Tuấn',
        nguon: 'chu',
        tuNgay: '2026-08-01',
        denNgay: '2026-08-17',
        dongs: [],
        taoLuc: TAO_LUC,
      },
      '2026-08-20',
    );

    expect(ket.chuaBiets.map((l) => l.loai)).toEqual(['benKiaChuaBiet']);
    expect(() => layTheoBenKia(duLieu, tuan, ket.chuaBiets[0])).toThrow(ChuaBietKhongLayDuoc);
    // Sổ mình không bị chạm tới.
    expect(dangCham(duLieu, tuan, '2026-08-19', 'Sang')?.soCong).toBe(0.5);
  });

  it('buổi của hôm nay vẫn là tạm gác, không thành dòng "chưa biết"', () => {
    const thoId = tuan();
    const ket = doiChieu(
      // Máy thợ nhận vai đúng hôm nay, chưa chấm ô nào.
      soTho(thoId, [], '2026-08-20', '2026-08-20'),
      {
        thoId,
        tenTho: 'Anh Tuấn',
        nguon: 'chu',
        tuNgay: '2026-08-20',
        denNgay: '2026-08-20',
        dongs: [{ ngay: '2026-08-20', buoi: 'Sang', soCong: 1 }],
        taoLuc: TAO_LUC,
      },
      '2026-08-20',
    );

    expect(ket.soTamGac).toBe(1);
    expect(ket.chuaBiets).toEqual([]);
  });

  it('chưa so được buổi nào vẫn trả về phần chỉ một bên có sổ', () => {
    const thoId = tuan();
    const ket = doiChieu(
      soTho(thoId, [], '2026-09-01', '2026-09-30'),
      {
        thoId,
        tenTho: 'Anh Tuấn',
        nguon: 'chu',
        tuNgay: '2026-08-01',
        denNgay: '2026-08-19',
        dongs: [{ ngay: '2026-08-19', buoi: 'Sang', soCong: 1 }],
        taoLuc: TAO_LUC,
      },
      '2026-09-30',
    );

    // Hai khoảng khai không giao nhau — nhưng bỏ trắng cả trang thì thợ không bao giờ biết
    // sổ chủ đang có công của mình.
    expect(ket.khongTrungKhoang).toBe(true);
    expect(ket.lechs).toEqual([]);
    expect(ket.chuaBiets.map((l) => [l.ngay, l.loai])).toEqual([['2026-08-19', 'minhChuaBiet']]);
    // Chưa so được gì không có nghĩa là hai sổ trống: sổ chủ vẫn đang giữ một công.
    expect(ket.tongCongMinh).toBe(0);
    expect(ket.tongCongBenKia).toBe(1);
  });
});
