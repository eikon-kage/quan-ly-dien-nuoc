/**
 * Kỳ lương và quyết toán.
 *
 * Điều phải giữ bằng mọi giá và được kiểm ở đây: **quyết toán không làm mất dữ liệu**.
 * Chốt kỳ xong thì bảng lương về 0, nhưng buổi công và ứng tiền vẫn nằm nguyên trong
 * kho — bỏ chốt ra là mọi thứ trở lại y như cũ.
 */

import {
  banGhiChuaChot,
  banGhiCuaKy,
  baoCaoKyHienTai,
  baoCaoTrongKy,
  boChot,
  cacKyMoiTruoc,
  khoangKyHienTai,
  kyGanNhat,
  kyHienTai,
  noDauKy,
  quyetToan,
  tongCuaKy,
  traDuKien,
} from '../ky';
import { DuLieuChamCong, duLieuRong } from '../kieu';
import { cham, datCong, themTho, themUng } from '../thaoTac';

const NGAY_TAO = '2026-07-01';

function kho(): { duLieu: DuLieuChamCong; tuan: string; binh: string } {
  let duLieu = duLieuRong();

  const themTuan = themTho(duLieu, 'Anh Tuấn', 300_000, NGAY_TAO);
  duLieu = themTuan.duLieu;

  const themBinh = themTho(duLieu, 'Anh Bình', 250_000, NGAY_TAO);
  duLieu = themBinh.duLieu;

  return { duLieu, tuan: themTuan.tho.id, binh: themBinh.tho.id };
}

/** Chấm cả ngày cho một thợ: sáng nửa công, chiều nửa công — cả ngày là một công. */
function chamCaNgay(duLieu: DuLieuChamCong, thoId: string, ngay: string): DuLieuChamCong {
  return cham(cham(duLieu, thoId, ngay, 'Sang'), thoId, ngay, 'Chieu');
}

function dongCua(duLieu: DuLieuChamCong, thoId: string, homNay = '2026-07-10') {
  return kyHienTai(duLieu, homNay).dongs.find((d) => d.tho.id === thoId);
}

describe('kỳ đang mở', () => {
  test('chưa chốt kỳ nào thì mọi thứ nằm trong kỳ đầu tiên', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');

    const ky = kyHienTai(duLieu, '2026-07-10');

    expect(ky.dongs).toHaveLength(1);
    expect(ky.dongs[0].tongCong).toBe(1);
    expect(ky.dongs[0].tienCong).toBe(300_000);
    expect(ky.tongPhaiTra).toBe(300_000);
    expect(ky.chotDuoc).toBe(true);
  });

  test('kỳ trống thì không chốt được', () => {
    const { duLieu } = kho();
    const ky = kyHienTai(duLieu, '2026-07-10');

    expect(ky.dongs).toEqual([]);
    expect(ky.chotDuoc).toBe(false);
    expect(() => quyetToan(duLieu, { denNgay: '2026-07-10' })).toThrow(/chưa có gì/i);
  });

  test('khoảng kỳ chạy từ ngày sớm nhất chưa trả tới hôm nay', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-07-03', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-07-08', 'Sang');

    expect(khoangKyHienTai(duLieu, '2026-07-10')).toEqual({
      tuNgay: '2026-07-03',
      denNgay: '2026-07-10',
    });
  });

  test('chốt kỳ hôm nay thì khoảng kỳ mới không bị ghi ngược', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 0]]) });

    // Kỳ mới bắt đầu từ 11/07, mà hôm nay vẫn là 10/07 — không kẹp lại thì màn hình ghi
    // "11/07 → 10/07", đọc lên như sổ hỏng.
    const khoang = khoangKyHienTai(duLieu, '2026-07-10');
    expect(khoang.tuNgay).toBe('2026-07-11');
    expect(khoang.denNgay).toBe('2026-07-11');
    expect(khoang.tuNgay <= khoang.denNgay).toBe(true);
  });

  test('chấm trước cho ngày mai thì cuối kỳ chạy tới ngày mai', () => {
    let { duLieu, tuan } = kho();
    duLieu = cham(duLieu, tuan, '2026-07-12', 'Sang');

    expect(khoangKyHienTai(duLieu, '2026-07-10').denNgay).toBe('2026-07-12');
  });
});

describe('quyết toán', () => {
  test('chốt xong bảng lương về 0 nhưng buổi công và ứng tiền còn nguyên', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = themUng(duLieu, tuan, '2026-07-03', 200_000);

    const truocKhiChot = { buoi: duLieu.buoiCongs.length, ung: duLieu.ungTiens.length };
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    // Bảng lương sạch...
    const kySau = kyHienTai(duLieu, '2026-07-11');
    expect(kySau.dongs).toEqual([]);
    expect(kySau.tongPhaiTra).toBe(0);

    // ...nhưng dữ liệu thì không mất một dòng nào. Đây là điều quan trọng nhất.
    expect(duLieu.buoiCongs).toHaveLength(truocKhiChot.buoi);
    expect(duLieu.ungTiens).toHaveLength(truocKhiChot.ung);
  });

  test('bản chụp ghi lại đủ số công, tiền công, đã ứng và đã trả', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = themUng(duLieu, tuan, '2026-07-03', 200_000);

    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    const ky = kyGanNhat(duLieu)!;

    expect(ky.dongs).toHaveLength(1);
    expect(ky.dongs[0]).toMatchObject({
      tenTho: 'Anh Tuấn',
      congSang: 0.5,
      congChieu: 0.5,
      tongCong: 1,
      tienCong: 300_000,
      daUng: 200_000,
      noKyTruoc: 0,
      phaiTra: 100_000,
      daTra: 100_000,
      chuyenKySau: 0,
    });
    expect(ky.tuNgay).toBe('2026-07-02');
    expect(ky.denNgay).toBe('2026-07-10');
  });

  test('không ghi số trả thì mặc định là trả đủ', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');

    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    expect(kyGanNhat(duLieu)!.dongs[0].daTra).toBe(300_000);
    expect(kyGanNhat(duLieu)!.dongs[0].chuyenKySau).toBe(0);
  });

  test('thợ ứng quá tay thì mặc định không phải trả thêm đồng nào', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = themUng(duLieu, tuan, '2026-07-03', 500_000);

    const dong = dongCua(duLieu, tuan)!;
    expect(dong.conLai).toBe(-200_000);
    // Máy không tự đi đòi lại tiền: mặc định trả 0, còn đòi hay không là chuyện của người.
    expect(traDuKien(dong)).toBe(0);

    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    expect(kyGanNhat(duLieu)!.dongs[0].daTra).toBe(0);
    // Thợ đang cầm dư 200.000, kỳ sau trừ lại.
    expect(kyGanNhat(duLieu)!.dongs[0].chuyenKySau).toBe(-200_000);
  });

  test('không cho ghi số tiền trả âm', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');

    expect(() =>
      quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, -1]]) }),
    ).toThrow(/số âm/i);
  });

  test('chốt cả tổ một lượt, thợ nào cũng có mặt trong tờ quyết toán', () => {
    let { duLieu, tuan, binh } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = cham(duLieu, binh, '2026-07-02', 'Sang');

    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    // Xếp theo tên tiếng Việt: Bình trước Tuấn.
    expect(kyGanNhat(duLieu)!.dongs.map((d) => d.tenTho)).toEqual(['Anh Bình', 'Anh Tuấn']);
  });
});

describe('trả thiếu thì chuyển sang kỳ sau', () => {
  test('phần thiếu thành nợ đầu kỳ của kỳ mới', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');

    duLieu = quyetToan(duLieu, {
      denNgay: '2026-07-10',
      daTra: new Map([[tuan, 100_000]]),
    });

    expect(kyGanNhat(duLieu)!.dongs[0].chuyenKySau).toBe(200_000);
    expect(noDauKy(duLieu).get(tuan)).toBe(200_000);
  });

  test('thợ chỉ còn mỗi khoản nợ, kỳ sau chưa đi làm buổi nào, vẫn hiện ra', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 100_000]]) });

    const ky = kyHienTai(duLieu, '2026-07-20');

    expect(ky.dongs).toHaveLength(1);
    expect(ky.dongs[0].tongCong).toBe(0);
    expect(ky.dongs[0].tienCong).toBe(0);
    expect(ky.dongs[0].noKyTruoc).toBe(200_000);
    expect(ky.dongs[0].conLai).toBe(200_000);
  });

  test('nợ cộng vào tiền công của kỳ sau', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 100_000]]) });

    duLieu = chamCaNgay(duLieu, tuan, '2026-07-15');
    const dong = dongCua(duLieu, tuan, '2026-07-20')!;

    expect(dong.tienCong).toBe(300_000);
    expect(dong.noKyTruoc).toBe(200_000);
    expect(dong.conLai).toBe(500_000);
  });

  test('trả dư thì kỳ sau trừ lại', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 400_000]]) });

    expect(noDauKy(duLieu).get(tuan)).toBe(-100_000);

    duLieu = chamCaNgay(duLieu, tuan, '2026-07-15');
    expect(dongCua(duLieu, tuan, '2026-07-20')!.conLai).toBe(200_000);
  });

  test('nợ không cộng dồn hai lần qua ba kỳ', () => {
    let { duLieu, tuan } = kho();

    // Kỳ 1: làm 300.000, trả 100.000 → nợ 200.000.
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 100_000]]) });

    // Kỳ 2: làm 300.000 nữa, cộng nợ cũ là 500.000, trả 300.000 → nợ 200.000.
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-15');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-20', daTra: new Map([[tuan, 300_000]]) });

    expect(kyGanNhat(duLieu)!.dongs[0].noKyTruoc).toBe(200_000);
    expect(kyGanNhat(duLieu)!.dongs[0].phaiTra).toBe(500_000);
    expect(kyGanNhat(duLieu)!.dongs[0].chuyenKySau).toBe(200_000);

    // Kỳ 3 chỉ mang đúng 200.000, không phải 400.000.
    expect(dongCua(duLieu, tuan, '2026-07-25')!.conLai).toBe(200_000);
  });
});

describe('chấm bù ngày đã nằm trong kỳ đã chốt', () => {
  test('buổi chấm bù rơi vào kỳ đang mở chứ không mất', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    // Hôm sau mới nhớ ra ngày 5 nó cũng có đi — ngày đó nằm trong kỳ vừa chốt.
    duLieu = cham(duLieu, tuan, '2026-07-05', 'Sang');

    const ky = kyHienTai(duLieu, '2026-07-11');
    expect(ky.dongs).toHaveLength(1);
    expect(ky.dongs[0].tongCong).toBe(0.5);
    expect(ky.dongs[0].tienCong).toBe(150_000);
    // Đầu kỳ lùi về đúng ngày chấm bù, không bỏ nó ra ngoài khoảng đang hiện.
    expect(khoangKyHienTai(duLieu, '2026-07-11').tuNgay).toBe('2026-07-05');
  });

  test('kỳ đã chốt không bị buổi chấm bù làm sai lệch', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    duLieu = cham(duLieu, tuan, '2026-07-05', 'Sang');

    // Tờ quyết toán là bản chụp, chốt xong là đóng.
    expect(kyGanNhat(duLieu)!.dongs[0].tienCong).toBe(300_000);
    expect(banGhiCuaKy(duLieu, kyGanNhat(duLieu)!).buoiCongs).toHaveLength(2);
  });

  test('sửa số công của buổi đã chốt thì buổi đó vẫn thuộc kỳ cũ', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    // `cham` sửa đè lên bản ghi cũ, giữ nguyên id — nên buổi này vẫn nằm trong kỳ đã chốt
    // và không nhảy sang kỳ mới. Số tiền đã trả không tự nhiên đổi sau lưng người dùng.
    duLieu = datCong(duLieu, tuan, '2026-07-02', 'Sang', 0.25);

    expect(kyHienTai(duLieu, '2026-07-11').dongs).toEqual([]);
    expect(kyGanNhat(duLieu)!.dongs[0].tienCong).toBe(300_000);
  });
});

describe('bỏ chốt', () => {
  test('gỡ kỳ vừa chốt thì mọi thứ trở lại y như cũ', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = themUng(duLieu, tuan, '2026-07-03', 200_000);

    const truoc = kyHienTai(duLieu, '2026-07-10');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    duLieu = boChot(duLieu, kyGanNhat(duLieu)?.id ?? '');

    const sau = kyHienTai(duLieu, '2026-07-10');
    expect(duLieu.kyLuongs).toEqual([]);
    expect(sau.tongPhaiTra).toBe(truoc.tongPhaiTra);
    expect(sau.dongs).toHaveLength(truoc.dongs.length);
    expect(sau.dongs[0].tienCong).toBe(300_000);
    expect(sau.dongs[0].daUng).toBe(200_000);
  });

  test('bỏ chốt kỳ cũ thì không cho, vì nợ của các kỳ sau nó sẽ sai', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    const kyDau = kyGanNhat(duLieu)!.id;

    duLieu = chamCaNgay(duLieu, tuan, '2026-07-15');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-20' });

    expect(() => boChot(duLieu, kyDau)).toThrow(/mới nhất/i);
    expect(duLieu.kyLuongs).toHaveLength(2);
  });

  test('chưa chốt kỳ nào mà bỏ chốt thì báo lỗi chứ không im lặng', () => {
    const { duLieu } = kho();
    expect(() => boChot(duLieu, 'khong-co-that')).toThrow(/chưa quyết toán/i);
  });
});

describe('xem lại các kỳ', () => {
  test('kỳ mới nhất lên đầu', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-15');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-20' });

    expect(cacKyMoiTruoc(duLieu).map((ky) => ky.denNgay)).toEqual(['2026-07-20', '2026-07-10']);
  });

  test('cộng tổng của một kỳ', () => {
    let { duLieu, tuan, binh } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = cham(duLieu, binh, '2026-07-02', 'Sang');
    duLieu = themUng(duLieu, tuan, '2026-07-03', 100_000);

    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[binh, 0]]) });

    expect(tongCuaKy(kyGanNhat(duLieu)!)).toEqual({
      tongCong: 1.5,
      tienCong: 425_000,
      daUng: 100_000,
      daTra: 200_000,
      chuyenKySau: 125_000,
    });
  });

  test('đổi tên thợ sau này không làm sai tờ quyết toán cũ', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });

    duLieu = {
      ...duLieu,
      thos: duLieu.thos.map((t) => (t.id === tuan ? { ...t, ten: 'Anh Tuấn (đã nghỉ)' } : t)),
    };

    expect(kyGanNhat(duLieu)!.dongs[0].tenTho).toBe('Anh Tuấn');
  });

  test('bản ghi chưa chốt không lẫn bản ghi đã chốt', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    duLieu = cham(duLieu, tuan, '2026-07-15', 'Sang');

    expect(banGhiChuaChot(duLieu).buoiCongs.map((b) => b.ngay)).toEqual(['2026-07-15']);
    expect(banGhiCuaKy(duLieu, kyGanNhat(duLieu)!).buoiCongs.map((b) => b.ngay)).toEqual([
      '2026-07-02',
      '2026-07-02',
    ]);
  });
});

describe('chi tiết một thợ trong kỳ', () => {
  test('kỳ đang mở chỉ tính buổi chưa trả tiền', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    duLieu = cham(duLieu, tuan, '2026-07-05', 'Sang');

    const baoCao = baoCaoKyHienTai(duLieu, tuan, '2026-07-11')!;

    // Ngày 2 đã trả tiền rồi nên không đếm lại, dù nó nằm trong khoảng ngày đang xem.
    expect(baoCao.ngayCongs.map((d) => d.ngay)).toEqual(['2026-07-05']);
    expect(baoCao.tienCong).toBe(150_000);
  });

  test('kỳ đang mở cộng cả nợ kỳ trước vào số còn phải trả', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 100_000]]) });
    duLieu = cham(duLieu, tuan, '2026-07-15', 'Sang');

    const baoCao = baoCaoKyHienTai(duLieu, tuan, '2026-07-20')!;

    expect(baoCao.tienCong).toBe(150_000);
    expect(baoCao.noKyTruoc).toBe(200_000);
    expect(baoCao.conLai).toBe(350_000);
  });

  test('xem hẹp hơn cả kỳ thì bỏ nợ kỳ trước ra, kẻo con số dưới đáy vô nghĩa', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10', daTra: new Map([[tuan, 100_000]]) });
    duLieu = cham(duLieu, tuan, '2026-07-15', 'Sang');
    duLieu = cham(duLieu, tuan, '2026-07-16', 'Sang');

    const hep = baoCaoKyHienTai(duLieu, tuan, '2026-07-20', '2026-07-16', '2026-07-16')!;

    expect(hep.tienCong).toBe(150_000);
    expect(hep.noKyTruoc).toBe(0);
    expect(hep.conLai).toBe(150_000);
  });

  test('kỳ đã chốt mở lại được đúng những ngày của nó', () => {
    let { duLieu, tuan } = kho();
    duLieu = chamCaNgay(duLieu, tuan, '2026-07-02');
    duLieu = cham(duLieu, tuan, '2026-07-04', 'Sang');
    duLieu = quyetToan(duLieu, { denNgay: '2026-07-10' });
    duLieu = cham(duLieu, tuan, '2026-07-15', 'Sang');

    const baoCao = baoCaoTrongKy(duLieu, kyGanNhat(duLieu)!, tuan)!;

    expect(baoCao.ngayCongs.map((d) => d.ngay)).toEqual(['2026-07-02', '2026-07-04']);
    expect(baoCao.tienCong).toBe(450_000);
    // Ngày nghỉ cắt ở ngày chốt kỳ, không chạy tới hôm nay.
    expect(baoCao.ngayNghis.every((ngay) => ngay <= '2026-07-10')).toBe(true);
  });
});
