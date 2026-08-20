import {
  banCanXoa,
  dongGoi,
  GoiHong,
  moGoi,
  ngayTuTenFile,
  PHIEN_BAN,
  tenFileSaoLuu,
  tomTat,
} from '../goiSaoLuu';
import { duLieuRong } from '../kieu';
import { cham, themUng, themTho } from '../thaoTac';

function khoDayDu() {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  const daCham = cham(them.duLieu, them.tho.id, '2026-08-03', 'Sang');
  return themUng(daCham, them.tho.id, '2026-08-04', 500_000, 'mua xăng');
}

describe('tên file sao lưu', () => {
  test('mỗi ngày một tên, ngày viết kiểu yyyy-MM-dd để sắp xếp ra đúng thứ tự', () => {
    expect(tenFileSaoLuu('2026-08-05')).toBe('Cham-cong-2026-08-05.json');

    const cacTen = ['2026-01-09', '2026-08-05', '2025-12-31'].map(tenFileSaoLuu);
    expect([...cacTen].sort()).toEqual([
      'Cham-cong-2025-12-31.json',
      'Cham-cong-2026-01-09.json',
      'Cham-cong-2026-08-05.json',
    ]);
  });

  test('đọc ngược lại ra ngày', () => {
    expect(ngayTuTenFile('Cham-cong-2026-08-05.json')).toBe('2026-08-05');
  });

  test('file lạ trên Drive thì trả null chứ không đoán bừa', () => {
    expect(ngayTuTenFile('Cham-cong-05-08-2026.xlsx')).toBeNull();
    expect(ngayTuTenFile('Bang luong.json')).toBeNull();
    expect(ngayTuTenFile('Cham-cong-2026-08-05.json.bak')).toBeNull();
  });
});

describe('đóng gói và mở gói', () => {
  test('gói rồi mở ra được đúng dữ liệu ban đầu', () => {
    const kho = khoDayDu();

    const daMo = moGoi(dongGoi(kho, '2026-08-05T09:00:00.000Z'));

    expect(daMo.duLieu).toEqual(kho);
    expect(daMo.taoLuc).toBe('2026-08-05T09:00:00.000Z');
    expect(daMo.phienBan).toBe(PHIEN_BAN);
  });

  test('gói giữ nguyên cả kỳ đã chốt lẫn ứng tiền, không cắt bớt như file Excel', () => {
    const kho = khoDayDu();

    const daMo = moGoi(dongGoi(kho, '2026-08-05T09:00:00.000Z'));

    expect(tomTat(daMo.duLieu)).toEqual({ soTho: 1, soBuoiCong: 1, soUngTien: 1, soKy: 0 });
  });
});

describe('từ chối file không phải bản sao lưu', () => {
  test('không phải JSON', () => {
    expect(() => moGoi('không phải json')).toThrow(GoiHong);
  });

  test('JSON của app khác', () => {
    expect(() => moGoi(JSON.stringify({ app: 'app-khac', phienBan: 1, duLieu: {} }))).toThrow(
      GoiHong,
    );
  });

  test('mảng hoặc số trần', () => {
    expect(() => moGoi('[1,2,3]')).toThrow(GoiHong);
    expect(() => moGoi('42')).toThrow(GoiHong);
  });

  test('thiếu hẳn phần dữ liệu', () => {
    expect(() => moGoi(JSON.stringify({ app: 'cham-cong', phienBan: 1 }))).toThrow(GoiHong);
  });

  /**
   * Quan trọng: app cũ nuốt gói của app mới thì cấu trúc lệch nhau, khôi phục xong dữ
   * liệu hỏng mà không ai biết. Thà báo "hãy cập nhật app".
   */
  test('gói của phiên bản app mới hơn', () => {
    const goiMoi = JSON.stringify({ app: 'cham-cong', phienBan: PHIEN_BAN + 1, duLieu: {} });

    expect(() => moGoi(goiMoi)).toThrow(/phiên bản app mới hơn/);
  });
});

describe('vá dữ liệu thiếu', () => {
  test('gói thiếu mảng nào thì mảng ấy thành rỗng, không nổ', () => {
    const goiThieu = JSON.stringify({ app: 'cham-cong', phienBan: 1, duLieu: { thos: [] } });

    expect(moGoi(goiThieu).duLieu).toEqual(duLieuRong());
  });

  /** Bản sao lưu làm từ app đời trước, thợ còn để một mức tiền công duy nhất. */
  test('thợ bản cũ được chuyển thành mốc lương đầu tiên', () => {
    const goiCu = JSON.stringify({
      app: 'cham-cong',
      phienBan: 1,
      duLieu: {
        thos: [{ id: 't1', ten: 'Anh Tuấn', tienMotCong: 250_000, ngayTao: '2026-01-01' }],
      },
    });

    const tho = moGoi(goiCu).duLieu.thos[0];
    expect(tho.mocLuong).toEqual([{ tuNgay: '2026-01-01', tienMotCong: 250_000 }]);
  });
});

/**
 * Chọn bản để xoá. Chọn sai ở đây là **xoá mất bản sao lưu**, mà lỗi ấy chỉ hiện ra lúc
 * người dùng cần quay về — nên soát kỹ hơn mức một hàm bốn dòng thường được soát.
 */
describe('dọn bản cũ', () => {
  const cacTen = ['2026-08-01', '2026-08-02', '2026-08-03', '2026-08-04'].map(tenFileSaoLuu);

  test('giữ đúng số bản mới nhất, xoá phần còn lại', () => {
    expect(banCanXoa(cacTen, 2)).toEqual([
      tenFileSaoLuu('2026-08-02'),
      tenFileSaoLuu('2026-08-01'),
    ]);
  });

  test('thứ tự file lộn xộn cũng chọn đúng — không tin thứ tự hệ điều hành trả về', () => {
    const loanXa = [cacTen[2], cacTen[0], cacTen[3], cacTen[1]];

    expect(banCanXoa(loanXa, 1)).toEqual([
      tenFileSaoLuu('2026-08-03'),
      tenFileSaoLuu('2026-08-02'),
      tenFileSaoLuu('2026-08-01'),
    ]);
  });

  test('ít bản hơn mức giữ thì không xoá gì', () => {
    expect(banCanXoa(cacTen, 30)).toEqual([]);
    expect(banCanXoa([], 30)).toEqual([]);
  });

  /** File lạ trong thư mục không phải việc của mình. Xoá bừa là xoá file của người ta. */
  test('không đụng tới file không đúng khuôn tên', () => {
    const lan = [...cacTen, 'ghi-chu.txt', 'Cham-cong-thang-8.json'];

    const xoa = banCanXoa(lan, 0);
    expect(xoa).toHaveLength(4);
    expect(xoa).not.toContain('ghi-chu.txt');
    expect(xoa).not.toContain('Cham-cong-thang-8.json');
  });
});
