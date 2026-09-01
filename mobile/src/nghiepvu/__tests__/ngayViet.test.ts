import { congNgay, soCong, thu, thuGon, thuVaNgay, tien, tuan } from '../ngayViet';

describe('cách viết ngày, số công và số tiền hiện lên màn hình', () => {
  test.each([
    ['2026-08-02', 'Chủ Nhật'],
    ['2026-08-03', 'Thứ Hai'],
    ['2026-08-08', 'Thứ Bảy'],
  ])('thu(%s) ra %s', (ngay, mongDoi) => {
    expect(thu(ngay)).toBe(mongDoi);
  });

  test('thuVaNgay ra kiểu hiện trên đầu màn hình', () => {
    expect(thuVaNgay('2026-08-03')).toBe('Thứ Hai 03/08');
  });

  test.each([
    ['2026-08-02', 'CN'],
    ['2026-08-03', 'T2'],
    ['2026-08-08', 'T7'],
  ])('thuGon(%s) ra %s', (ngay, mongDoi) => {
    expect(thuGon(ngay)).toBe(mongDoi);
  });

  test('tuần chạy từ Thứ Hai tới Chủ Nhật', () => {
    expect(tuan('2026-08-05')).toEqual([
      '2026-08-03',
      '2026-08-04',
      '2026-08-05',
      '2026-08-06',
      '2026-08-07',
      '2026-08-08',
      '2026-08-09',
    ]);
  });

  test('Chủ Nhật là ngày cuối tuần chứ không phải ngày đầu tuần', () => {
    expect(tuan('2026-08-09')[6]).toBe('2026-08-09');
    expect(tuan('2026-08-09')[0]).toBe('2026-08-03');
  });

  test('tuần vắt qua hai tháng vẫn liền mạch', () => {
    expect(tuan('2026-09-01')).toEqual([
      '2026-08-31',
      '2026-09-01',
      '2026-09-02',
      '2026-09-03',
      '2026-09-04',
      '2026-09-05',
      '2026-09-06',
    ]);
  });

  test.each([
    ['2026-08-03', 1, '2026-08-04'],
    ['2026-08-03', -1, '2026-08-02'],
    ['2026-08-31', 1, '2026-09-01'],
    ['2026-01-01', -1, '2025-12-31'],
    ['2024-02-28', 1, '2024-02-29'],
  ])('congNgay(%s, %s) ra %s', (ngay, so, mongDoi) => {
    expect(congNgay(ngay, so)).toBe(mongDoi);
  });

  test.each([
    [1, '1'],
    [0.5, '0,5'],
    [0.25, '0,25'],
    [0.75, '0,75'],
    [1.5, '1,5'],
    [3, '3'],
  ])('soCong(%s) ra %s', (so, mongDoi) => {
    expect(soCong(so)).toBe(mongDoi);
  });

  test.each([
    [1_500_000, '1.500.000 đ'],
    [300_000, '300.000 đ'],
    [0, '0 đ'],
    [-200_000, '−200.000 đ'],
  ])('tien(%s) ra %s', (so, mongDoi) => {
    expect(tien(so)).toBe(mongDoi);
  });
});
