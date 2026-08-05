import { baoCaoKhoang, baoCaoThang, daChamHomNay } from '../baoCao';
import { DuLieuChamCong, duLieuRong } from '../kieu';
import { soNgayTrongThang } from '../ngayViet';
import { cham, datLuong, themTho, themUng } from '../thaoTac';

const NGAY_TAO = '2026-08-01';

function khoCoTho(tienMotCong = 300_000, ngayTao = NGAY_TAO) {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', tienMotCong, ngayTao);
  return { duLieu, thoId: tho.id };
}

function bao(duLieu: DuLieuChamCong, thoId: string, homNay = '2026-08-31') {
  return baoCaoThang(duLieu, thoId, 2026, 8, homNay)!;
}

describe('số ngày trong tháng', () => {
  test.each([
    [2026, 8, 31],
    [2026, 2, 28],
    [2024, 2, 29],
    [2026, 4, 30],
  ])('tháng %s/%s có %s ngày', (nam, thang, mongDoi) => {
    expect(soNgayTrongThang(nam, thang)).toBe(mongDoi);
  });
});

describe('báo cáo tháng của một thợ', () => {
  test('gộp sáng và chiều của cùng một ngày thành một dòng', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');

    const ketQua = bao(duLieu, thoId);

    expect(ketQua.ngayCongs).toHaveLength(1);
    expect(ketQua.ngayCongs[0]).toMatchObject({
      ngay: '2026-08-03',
      congSang: 1,
      congChieu: 1,
      tongCong: 2,
      tien: 600_000,
    });
  });

  test('đi một buổi thì buổi kia là null', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');

    const [dong] = bao(duLieu, thoId).ngayCongs;

    expect(dong.congSang).toBe(1);
    expect(dong.congChieu).toBeNull();
  });

  test('ngày công xếp theo thứ tự thời gian', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-20', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-11', 'Sang');

    expect(bao(duLieu, thoId).ngayCongs.map((d) => d.ngay)).toEqual([
      '2026-08-03',
      '2026-08-11',
      '2026-08-20',
    ]);
  });

  test('tiền từng ngày tính theo mốc lương của đúng ngày đó', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-10', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-20', 'Sang');
    duLieu = datLuong(duLieu, thoId, '2026-08-15', 350_000);

    const ketQua = bao(duLieu, thoId);

    expect(ketQua.ngayCongs[0].tien).toBe(300_000);
    expect(ketQua.ngayCongs[1].tien).toBe(350_000);
    expect(ketQua.tienCong).toBe(650_000);
  });

  test('ngày nghỉ là ngày trong kỳ mà không có công nào', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-08-01');
    duLieu = cham(duLieu, thoId, '2026-08-01', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');

    const ketQua = bao(duLieu, thoId, '2026-08-04');

    expect(ketQua.ngayNghis).toEqual(['2026-08-02', '2026-08-04']);
  });

  test('ngày chưa tới thì không tính là nghỉ', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-08-01');
    duLieu = cham(duLieu, thoId, '2026-08-01', 'Sang');

    // Mới mùng 2 mà báo nghỉ 29 ngày còn lại thì hoảng.
    expect(bao(duLieu, thoId, '2026-08-02').ngayNghis).toEqual(['2026-08-02']);
  });

  test('ngày trước khi thợ vào làm thì không tính là nghỉ', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-08-20');
    duLieu = cham(duLieu, thoId, '2026-08-20', 'Sang');

    expect(bao(duLieu, thoId, '2026-08-22').ngayNghis).toEqual(['2026-08-21', '2026-08-22']);
  });

  test('liệt kê các lần ứng tiền theo thứ tự ngày', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = themUng(duLieu, thoId, '2026-08-20', 300_000, 'ứng lần hai');
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000, 'ứng đổ xăng');

    const ketQua = bao(duLieu, thoId);

    expect(ketQua.ungTiens.map((u) => u.ngay)).toEqual(['2026-08-05', '2026-08-20']);
    expect(ketQua.ungTiens[0].ghiChu).toBe('ứng đổ xăng');
    expect(ketQua.daUng).toBe(800_000);
  });

  test('không lấy công và ứng của tháng khác', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-07-01');
    duLieu = cham(duLieu, thoId, '2026-07-31', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-09-01', 'Sang');
    duLieu = themUng(duLieu, thoId, '2026-09-02', 100_000);

    const ketQua = bao(duLieu, thoId, '2026-08-03');

    expect(ketQua.ngayCongs).toHaveLength(1);
    expect(ketQua.daUng).toBe(0);
  });

  test('còn lại là tiền công trừ đã ứng', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');
    duLieu = themUng(duLieu, thoId, '2026-08-05', 200_000);

    expect(bao(duLieu, thoId).conLai).toBe(400_000);
  });

  test('thợ không có thật thì trả về null', () => {
    expect(baoCaoThang(duLieuRong(), 'khong-co', 2026, 8, '2026-08-31')).toBeNull();
  });
});

describe('báo cáo theo khoảng ngày', () => {
  test('chỉ tính công và ứng tiền nằm trong khoảng', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-20', 'Sang');
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000);
    duLieu = themUng(duLieu, thoId, '2026-08-25', 200_000);

    const bao = baoCaoKhoang(duLieu, thoId, '2026-08-01', '2026-08-15', '2026-08-31')!;

    expect(bao.tongCong).toBe(1);
    expect(bao.ungTiens).toHaveLength(1);
    expect(bao.daUng).toBe(500_000);
    expect(bao.tuNgay).toBe('2026-08-01');
    expect(bao.denNgay).toBe('2026-08-15');
  });

  test('ngày nghỉ cũng chỉ đếm trong khoảng', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-10', 'Sang');

    const bao = baoCaoKhoang(duLieu, thoId, '2026-08-08', '2026-08-12', '2026-08-31')!;

    expect(bao.ngayNghis).toEqual([
      '2026-08-08',
      '2026-08-09',
      '2026-08-11',
      '2026-08-12',
    ]);
  });

  test('khoảng vắt qua hai tháng vẫn liền mạch', () => {
    let { duLieu, thoId } = khoCoTho(300_000, '2026-07-01');
    duLieu = cham(duLieu, thoId, '2026-07-31', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-01', 'Sang');

    const bao = baoCaoKhoang(duLieu, thoId, '2026-07-28', '2026-08-03', '2026-08-31')!;

    expect(bao.tongCong).toBe(2);
    expect(bao.ngayNghis).toHaveLength(5);
  });
});

describe('đã chấm hôm nay chưa', () => {
  test('chưa chấm gì thì false', () => {
    const { duLieu } = khoCoTho();

    expect(daChamHomNay(duLieu, '2026-08-05')).toBe(false);
  });

  test('chấm cho một người là đủ tính đã chấm', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-05', 'Sang');

    expect(daChamHomNay(duLieu, '2026-08-05')).toBe(true);
  });

  test('chấm hôm qua không tính cho hôm nay', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-04', 'Sang');

    expect(daChamHomNay(duLieu, '2026-08-05')).toBe(false);
  });
});
