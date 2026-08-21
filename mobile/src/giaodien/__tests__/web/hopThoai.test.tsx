/**
 * Hộp hỏi lại của bản web — phần **quyết định**.
 *
 * Phải có bài cho chỗ này vì bản web không dùng `Alert` mà tự vẽ, mà `Alert.alert` của
 * react-native-web thì lại là một hàm rỗng: hỏng ở đây là hỏng lặng thinh, không lỗi nào
 * hiện ra. Mà một trong những câu hỏi đi qua đây là câu trước khi ghi đè cả sổ.
 *
 * **Không có bài dựng hộp ra để bấm.** `Modal` của react-native-web gắn hộp vào
 * `document.body` bằng portal của react-dom, mà react-test-renderer — bộ máy mà
 * `@testing-library/react-native` dùng — không dựng được portal ấy: `render()` ném
 * `AggregateError` trống. Nên phần vẽ được soi bằng cách khác: mở bản dựng thật trong Chrome,
 * bấm cho hộp bật ra và chụp ảnh lại (xem docs/chamcong-pwa.md). Còn ở đây thì thử đúng mấy
 * chỗ *có thể sai mà mắt không thấy*: thứ tự nút, và chạm ra ngoài thì coi như bấm nút nào.
 */

import { hoi, khoCauHoi, nutKhiChamRaNgoai, xepNut } from '../../hopThoai.web';

beforeEach(() => {
  khoCauHoi.dat(null);
});

test('chưa hỏi gì thì không có câu hỏi nào đang mở', () => {
  expect(khoCauHoi.dangMo()).toBeNull();
});

test('hoi() mở câu hỏi kèm nguyên văn nhãn, lời và nút', () => {
  hoi('Khôi phục bản 05/08?', 'Bản này có 1 thợ, 12 buổi công.', [
    { text: 'Thôi', style: 'cancel' },
    { text: 'Khôi phục', style: 'destructive' },
  ]);

  expect(khoCauHoi.dangMo()).toEqual({
    nhan: 'Khôi phục bản 05/08?',
    loi: 'Bản này có 1 thợ, 12 buổi công.',
    nut: [
      { text: 'Thôi', style: 'cancel' },
      { text: 'Khôi phục', style: 'destructive' },
    ],
  });
});

test('câu hỏi mới đè câu cũ, y như Alert trên máy', () => {
  hoi('Câu cũ', '', [{ text: 'Đóng' }]);

  hoi('Câu mới', '', [{ text: 'Đóng' }]);

  expect(khoCauHoi.dangMo()?.nhan).toBe('Câu mới');
});

test('không nút nào thì tự thêm nút Đóng, kẻo hộp đứng mãi không đóng được', () => {
  hoi('Câu hỏi trơ', '', []);

  expect(khoCauHoi.dangMo()?.nut).toEqual([{ text: 'Đóng' }]);
});

test('ChoHopThoai được gọi lại mỗi lần câu hỏi đổi', () => {
  const goi = jest.fn();
  const thoiTheoDoi = khoCauHoi.theoDoi(goi);

  hoi('Câu hỏi', '', [{ text: 'Đóng' }]);
  khoCauHoi.dat(null);

  expect(goi).toHaveBeenCalledTimes(2);

  thoiTheoDoi();
  hoi('Câu nữa', '', [{ text: 'Đóng' }]);
  expect(goi).toHaveBeenCalledTimes(2);
});

test('nút Thôi xếp dưới cùng: chỗ ngón tay chạm dễ nhất là chỗ không làm gì', () => {
  const nut = xepNut([
    { text: 'Thôi', style: 'cancel' },
    { text: 'Xoá mốc', style: 'destructive' },
  ]);

  expect(nut.map((n) => n.text)).toEqual(['Xoá mốc', 'Thôi']);
});

test('không có nút Thôi thì giữ nguyên thứ tự bên gọi đã xếp', () => {
  const nut = xepNut([{ text: 'Đường A' }, { text: 'Đường B' }, { text: 'Đường C' }]);

  expect(nut.map((n) => n.text)).toEqual(['Đường A', 'Đường B', 'Đường C']);
});

test('chạm nền mờ: có nút Thôi thì là nút Thôi', () => {
  const n = nutKhiChamRaNgoai([
    { text: 'Thôi', style: 'cancel' },
    { text: 'Khôi phục', style: 'destructive' },
  ]);

  expect(n?.text).toBe('Thôi');
});

test('chạm nền mờ: hộp một nút thì là nút ấy', () => {
  expect(nutKhiChamRaNgoai([{ text: 'Đóng' }])?.text).toBe('Đóng');
});

test('chạm nền mờ: nhiều nút mà không có nút Thôi thì không đoán hộ', () => {
  expect(nutKhiChamRaNgoai([{ text: 'Đường A' }, { text: 'Đường B' }])).toBeNull();
});
