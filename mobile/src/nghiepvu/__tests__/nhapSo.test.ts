import { docSoCong, docTien } from '../nhapSo';

describe('đọc số công người dùng gõ', () => {
  test.each([
    ['1', 1],
    ['0,5', 0.5],
    ['0.5', 0.5],
    ['1,25', 1.25],
    ['2', 2],
    [' 1,5 ', 1.5],
    ['0,333', 0.33],
  ])('docSoCong(%s) ra %s', (chu, mongDoi) => {
    expect(docSoCong(chu)).toBe(mongDoi);
  });

  test.each([[''], ['   '], ['abc'], ['0'], ['-1'], ['1,2,3'], ['.'], [null], [undefined]])(
    'docSoCong(%s) không dùng được nên trả về null',
    (chu) => {
      expect(docSoCong(chu)).toBeNull();
    },
  );

  test('số lớn quá vẫn đọc được, để màn hình còn nói được là nhiều quá', () => {
    // Trả null thì người dùng chỉ thấy nút Ghi mờ đi mà không hiểu vì sao.
    expect(docSoCong('10')).toBe(10);
  });
});

describe('đọc số tiền người dùng gõ — gõ kiểu gì cũng phải hiểu', () => {
  test.each([
    ['300000', 300_000],
    ['300.000', 300_000],
    ['300,000', 300_000],
    ['300 000', 300_000],
    ['300.000 đ', 300_000],
  ])('docTien(%s) ra %s', (chu, mongDoi) => {
    expect(docTien(chu)).toBe(mongDoi);
  });

  test.each([[''], ['   '], ['abc'], [null], [undefined]])(
    'docTien(%s) không có chữ số nên trả về null',
    (chu) => {
      expect(docTien(chu)).toBeNull();
    },
  );
});
