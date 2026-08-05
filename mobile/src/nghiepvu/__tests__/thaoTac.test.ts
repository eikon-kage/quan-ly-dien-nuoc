import { DuLieuChamCong, duLieuRong } from '../kieu';
import {
  boCham,
  cham,
  dangCham,
  datCong,
  luuTho,
  tatCaTho,
  themTho,
  themUng,
  thoDangLam,
} from '../thaoTac';

const NGAY_LAM = '2026-08-03';

function khoCoTho(ten = 'Anh Tuấn', tienMotCong = 300_000) {
  const { duLieu, tho } = themTho(duLieuRong(), ten, tienMotCong, NGAY_LAM);
  return { duLieu, tho };
}

describe('chấm công', () => {
  test('mỗi buổi một dòng, mặc định một công', () => {
    const { duLieu, tho } = khoCoTho();

    let sau: DuLieuChamCong = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    sau = cham(sau, tho.id, NGAY_LAM, 'Chieu');

    expect(sau.buoiCongs).toHaveLength(2);
    expect(sau.buoiCongs.every((b) => b.soCong === 1)).toBe(true);
  });

  test('chấm lại cùng buổi thì sửa dòng cũ chứ không thêm dòng mới', () => {
    const { duLieu, tho } = khoCoTho();

    const lanDau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    const lanSau = cham(lanDau, tho.id, NGAY_LAM, 'Sang', 0.5, 'về sớm');

    expect(lanSau.buoiCongs).toHaveLength(1);
    expect(lanSau.buoiCongs[0].id).toBe(lanDau.buoiCongs[0].id);
    expect(lanSau.buoiCongs[0].soCong).toBe(0.5);
    expect(lanSau.buoiCongs[0].ghiChu).toBe('về sớm');
  });

  test('không chụp giá vào buổi công — giá lấy theo mốc lương của thợ', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');

    // Chụp giá vào đây thì sửa mốc lương sau này sẽ không tính lại được.
    expect(sau.buoiCongs[0].tienMotCong).toBeNull();
  });

  test('buổi vốn có giá riêng thì chấm lại vẫn giữ giá riêng đó', () => {
    const { duLieu, tho } = khoCoTho();
    const coGiaRieng = {
      ...cham(duLieu, tho.id, NGAY_LAM, 'Sang'),
    };
    coGiaRieng.buoiCongs[0].tienMotCong = 500_000;

    const sau = cham(coGiaRieng, tho.id, NGAY_LAM, 'Sang', 0.5);

    expect(sau.buoiCongs[0].tienMotCong).toBe(500_000);
    expect(sau.buoiCongs[0].soCong).toBe(0.5);
  });

  test('số công không dương thì báo lỗi', () => {
    const { duLieu, tho } = khoCoTho();

    expect(() => cham(duLieu, tho.id, NGAY_LAM, 'Sang', 0)).toThrow();
  });

  test('thợ không có trong danh sách thì báo lỗi', () => {
    expect(() => cham(duLieuRong(), 'khong-co', NGAY_LAM, 'Sang')).toThrow();
  });

  test('dữ liệu cũ không bị sửa tại chỗ', () => {
    const { duLieu, tho } = khoCoTho();

    cham(duLieu, tho.id, NGAY_LAM, 'Sang');

    expect(duLieu.buoiCongs).toHaveLength(0);
  });
});

describe('bỏ chấm', () => {
  test('xoá đúng buổi đó thôi', () => {
    const { duLieu, tho } = khoCoTho();
    let sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    sau = cham(sau, tho.id, NGAY_LAM, 'Chieu');

    sau = boCham(sau, tho.id, NGAY_LAM, 'Sang');

    expect(dangCham(sau, tho.id, NGAY_LAM, 'Sang')).toBeUndefined();
    expect(dangCham(sau, tho.id, NGAY_LAM, 'Chieu')).toBeDefined();
  });

  test('buổi chưa chấm thì dữ liệu giữ nguyên', () => {
    const { duLieu, tho } = khoCoTho();

    expect(boCham(duLieu, tho.id, NGAY_LAM, 'Sang').buoiCongs).toHaveLength(0);
  });

  test('chấm ngày này không đụng tới ngày khác', () => {
    const { duLieu, tho } = khoCoTho();
    let sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    sau = cham(sau, tho.id, '2026-08-04', 'Sang');

    sau = boCham(sau, tho.id, NGAY_LAM, 'Sang');

    expect(dangCham(sau, tho.id, '2026-08-04', 'Sang')).toBeDefined();
  });
});

describe('datCong', () => {
  test('null nghĩa là cho nghỉ buổi đó', () => {
    const { duLieu, tho } = khoCoTho();
    const sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');

    expect(datCong(sau, tho.id, NGAY_LAM, 'Sang', null).buoiCongs).toHaveLength(0);
  });

  test('số công lẻ ghi được', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = datCong(duLieu, tho.id, NGAY_LAM, 'Chieu', 1.5);

    expect(dangCham(sau, tho.id, NGAY_LAM, 'Chieu')?.soCong).toBe(1.5);
  });
});

describe('danh sách thợ', () => {
  test('thoDangLam bỏ qua thợ đã nghỉ', () => {
    let { duLieu } = khoCoTho('Anh Tuấn');
    const them = themTho(duLieu, 'Anh Bình', 280_000, NGAY_LAM);
    duLieu = luuTho(them.duLieu, { ...them.tho, dangLam: false });

    const danhSach = thoDangLam(duLieu);

    expect(danhSach).toHaveLength(1);
    expect(danhSach[0].ten).toBe('Anh Tuấn');
  });

  test('tatCaTho xếp người đang làm lên trước', () => {
    let { duLieu } = khoCoTho('Anh Tuấn');
    const them = themTho(duLieu, 'Anh Bình', 280_000, NGAY_LAM);
    duLieu = luuTho(them.duLieu, { ...them.tho, dangLam: false });

    expect(tatCaTho(duLieu).map((t) => t.ten)).toEqual(['Anh Tuấn', 'Anh Bình']);
  });

  test('thoDangLam xếp theo tên', () => {
    let { duLieu } = khoCoTho('Anh Tuấn');
    duLieu = themTho(duLieu, 'Anh Bình', 280_000, NGAY_LAM).duLieu;

    expect(thoDangLam(duLieu).map((t) => t.ten)).toEqual(['Anh Bình', 'Anh Tuấn']);
  });

  test('themTho cắt khoảng trắng thừa ở tên', () => {
    const { tho } = themTho(duLieuRong(), '  Anh Tuấn  ', 300_000, NGAY_LAM);

    expect(tho.ten).toBe('Anh Tuấn');
  });
});

describe('ứng tiền', () => {
  test('ghi được một lần ứng', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = themUng(duLieu, tho.id, NGAY_LAM, 500_000, 'ứng đổ xăng');

    expect(sau.ungTiens).toHaveLength(1);
    expect(sau.ungTiens[0].soTien).toBe(500_000);
    expect(sau.ungTiens[0].ghiChu).toBe('ứng đổ xăng');
  });

  test('số tiền không dương thì báo lỗi', () => {
    const { duLieu, tho } = khoCoTho();

    expect(() => themUng(duLieu, tho.id, NGAY_LAM, 0)).toThrow();
  });
});
