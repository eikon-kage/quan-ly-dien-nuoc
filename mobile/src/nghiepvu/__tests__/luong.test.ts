import { duLieuRong } from '../kieu';
import {
  datLuong,
  lichSuLuong,
  luongTaiNgay,
  themTho,
  timTho,
  xoaMocLuong,
} from '../thaoTac';

function khoCoTho(tienMotCong = 300_000, ngayTao = '2026-01-01') {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', tienMotCong, ngayTao);
  return { duLieu, thoId: tho.id };
}

describe('lịch sử tiền công', () => {
  test('thêm thợ thì có sẵn một mốc tính từ ngày thêm', () => {
    const { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');

    expect(timTho(duLieu, thoId)!.mocLuong).toEqual([
      { tuNgay: '2026-01-01', tienMotCong: 300_000 },
    ]);
  });

  test('lấy đúng giá của từng giai đoạn', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');
    duLieu = datLuong(duLieu, thoId, '2026-08-01', 350_000);
    const tho = timTho(duLieu, thoId)!;

    expect(luongTaiNgay(tho, '2026-07-31')).toBe(300_000);
    expect(luongTaiNgay(tho, '2026-08-01')).toBe(350_000);
    expect(luongTaiNgay(tho, '2026-12-25')).toBe(350_000);
  });

  test('ngày trước mốc đầu tiên thì lấy chính mốc đầu tiên, không ra 0 đồng', () => {
    const { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');

    expect(luongTaiNgay(timTho(duLieu, thoId)!, '2025-06-15')).toBe(300_000);
  });

  test('mốc luôn xếp theo ngày dù đặt lộn xộn', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');
    duLieu = datLuong(duLieu, thoId, '2026-09-01', 400_000);
    duLieu = datLuong(duLieu, thoId, '2026-05-01', 320_000);

    expect(timTho(duLieu, thoId)!.mocLuong.map((m) => m.tuNgay)).toEqual([
      '2026-01-01',
      '2026-05-01',
      '2026-09-01',
    ]);
  });

  test('đặt lại đúng ngày đã có thì sửa đè chứ không thêm mốc trùng', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');
    duLieu = datLuong(duLieu, thoId, '2026-08-01', 350_000);
    duLieu = datLuong(duLieu, thoId, '2026-08-01', 360_000);

    const mocLuong = timTho(duLieu, thoId)!.mocLuong;
    expect(mocLuong).toHaveLength(2);
    expect(mocLuong[1].tienMotCong).toBe(360_000);
  });

  test('sửa đè mốc đầu tiên là cách chữa lúc nhập nhầm giá', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');
    duLieu = datLuong(duLieu, thoId, '2026-01-01', 280_000);

    expect(luongTaiNgay(timTho(duLieu, thoId)!, '2026-03-10')).toBe(280_000);
  });

  test('lichSuLuong đưa mốc mới nhất lên đầu', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');
    duLieu = datLuong(duLieu, thoId, '2026-08-01', 350_000);

    expect(lichSuLuong(timTho(duLieu, thoId)!).map((m) => m.tuNgay)).toEqual([
      '2026-08-01',
      '2026-01-01',
    ]);
  });

  test('tiền công không dương thì báo lỗi', () => {
    const { duLieu, thoId } = khoCoTho();

    expect(() => datLuong(duLieu, thoId, '2026-08-01', 0)).toThrow();
  });

  test('xoá được mốc đặt nhầm', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');
    duLieu = datLuong(duLieu, thoId, '2026-08-01', 350_000);
    duLieu = xoaMocLuong(duLieu, thoId, '2026-08-01');

    expect(luongTaiNgay(timTho(duLieu, thoId)!, '2026-09-01')).toBe(300_000);
  });

  test('không cho xoá mốc cuối cùng — thợ phải còn một giá', () => {
    const { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');

    expect(() => xoaMocLuong(duLieu, thoId, '2026-01-01')).toThrow();
  });

  test('dữ liệu cũ không bị sửa tại chỗ', () => {
    const { duLieu, thoId } = khoCoTho(300_000, '2026-01-01');

    datLuong(duLieu, thoId, '2026-08-01', 350_000);

    expect(timTho(duLieu, thoId)!.mocLuong).toHaveLength(1);
  });
});
