/**
 * Kho giữ phiên đăng nhập của bản web.
 *
 * Bài quan trọng nhất ở đây là bài cuối: **trình duyệt không cho ghi thì app vẫn phải mở
 * lên được**. Safari ở chế độ riêng tư ném lỗi ngay lúc chạm vào `localStorage`, mà chỗ này
 * thì nằm trên đường khởi động (Supabase đọc phiên lúc dựng máy khách) — ném lỗi ra ở đây là
 * cả app trắng bảng chứ không phải chỉ mất phiên đăng nhập.
 */

import { khoMay } from '../../khoAnToan.web';

const khoThat = window.localStorage;

beforeEach(() => {
  window.localStorage.clear();
});

afterEach(() => {
  Object.defineProperty(window, 'localStorage', { configurable: true, value: khoThat });
});

test('ghi rồi đọc lại được', async () => {
  const kho = khoMay();

  await kho.ghi('phien', 'xin-chao');

  expect(await kho.doc('phien')).toBe('xin-chao');
});

test('chưa có gì thì đọc ra null, không phải chuỗi rỗng', async () => {
  expect(await khoMay().doc('chua-co')).toBeNull();
});

test('xoá rồi thì đọc ra null', async () => {
  const kho = khoMay();
  await kho.ghi('phien', 'xin-chao');

  await kho.xoa('phien');

  expect(await kho.doc('phien')).toBeNull();
});

test('ghi thẳng xuống localStorage để phiên còn sau khi đóng tab', async () => {
  await khoMay().ghi('phien', 'xin-chao');

  expect(window.localStorage.getItem('phien')).toBe('xin-chao');
});

test('trình duyệt chặn localStorage thì giữ trong RAM, không ném lỗi ra ngoài', async () => {
  // Safari riêng tư ném ngay lúc `setItem`. Thay hẳn cả `localStorage` chứ không `spyOn`:
  // `localStorage` của jsdom không phải object thường nên gắn hàng giả lên từng hàm không được.
  Object.defineProperty(window, 'localStorage', {
    configurable: true,
    value: {
      getItem: () => null,
      setItem: () => {
        throw new DOMException('QuotaExceededError');
      },
      removeItem: () => {},
    },
  });

  const kho = khoMay();
  await kho.ghi('phien', 'xin-chao');

  expect(await kho.doc('phien')).toBe('xin-chao');
});
