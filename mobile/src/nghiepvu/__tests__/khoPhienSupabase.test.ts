/**
 * Chỗ giữ phiên đăng nhập Supabase trong SecureStore.
 *
 * Điều phải giữ, và là lý do file này tồn tại: **SecureStore chỉ nhận giá trị dưới 2048
 * byte**, mà phiên Supabase thường dài hơn. Cắt khúc sai một chỗ thì app chạy êm cho tới
 * lúc mở lại và thấy mất phiên đăng nhập — loại lỗi không ai lần ra được từ báo cáo
 * "tự nhiên nó đòi đăng nhập lại".
 */

// Kho thật là mã máy (Keychain / Keystore), không nạp được ngoài điện thoại. Bài kiểm thử
// này đưa kho giả vào qua tham số nên không cần bản thật, chỉ cần nó đừng nạp.
jest.mock('expo-secure-store', () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
}));

import { KhoAnToan, khoPhien } from '../khoPhienSupabase';

const KHOA = 'sb-abcdef-auth-token';

/** Kho giả, kèm chốt chặn đúng giới hạn thật của SecureStore để bài kiểm thử bắt được. */
function khoGia(): KhoAnToan & { kho: Map<string, string> } {
  const kho = new Map<string, string>();
  return {
    kho,
    doc: async (khoa) => kho.get(khoa) ?? null,
    ghi: async (khoa, gia) => {
      if (Buffer.byteLength(gia, 'utf8') >= 2048) {
        throw new Error(`SecureStore không nhận giá trị dài ${gia.length} ký tự.`);
      }
      kho.set(khoa, gia);
    },
    xoa: async (khoa) => {
      kho.delete(khoa);
    },
  };
}

/** Phiên thật dài cỡ này: JWT + refresh token + thông tin người dùng. */
function phienDai(coChu = 3500): string {
  return JSON.stringify({ access_token: 'a'.repeat(coChu), refresh_token: 'r'.repeat(40) });
}

test('ghi rồi đọc lại nguyên văn, dù dài hơn giới hạn của SecureStore', async () => {
  const kho = khoGia();
  const phien = khoPhien(kho);
  const gia = phienDai();

  await phien.setItem(KHOA, gia);
  expect(await phien.getItem(KHOA)).toBe(gia);

  // Đúng là đã cắt khúc, không phải nhét cả cục vào một khoá.
  expect(kho.kho.get(`${KHOA}.so`)).toBe('3');
  expect(kho.kho.has(`${KHOA}.0`)).toBe(true);
});

test('giá trị ngắn thì vẫn đọc được, chỉ có một khúc', async () => {
  const kho = khoGia();
  const phien = khoPhien(kho);

  await phien.setItem(KHOA, '{"access_token":"ngan"}');
  expect(kho.kho.get(`${KHOA}.so`)).toBe('1');
  expect(await phien.getItem(KHOA)).toBe('{"access_token":"ngan"}');
});

test('chưa đăng nhập thì trả null chứ không quăng lỗi', async () => {
  expect(await khoPhien(khoGia()).getItem(KHOA)).toBeNull();
});

test('phiên mới ngắn hơn thì dọn hết khúc dư của phiên cũ', async () => {
  const kho = khoGia();
  const phien = khoPhien(kho);

  await phien.setItem(KHOA, phienDai(5000));
  await phien.setItem(KHOA, '{"access_token":"ngan"}');

  // Còn sót khúc cũ là lần đọc sau ghép cả rác vào giữa chuỗi JSON.
  expect([...kho.kho.keys()].sort()).toEqual([`${KHOA}.0`, `${KHOA}.so`]);
  expect(await phien.getItem(KHOA)).toBe('{"access_token":"ngan"}');
});

test('thiếu một khúc thì coi như chưa đăng nhập, không trả về JSON dở dang', async () => {
  const kho = khoGia();
  const phien = khoPhien(kho);

  await phien.setItem(KHOA, phienDai());
  kho.kho.delete(`${KHOA}.1`);

  expect(await phien.getItem(KHOA)).toBeNull();
});

test('xoá thì sạch cả số khúc lẫn mọi khúc', async () => {
  const kho = khoGia();
  const phien = khoPhien(kho);

  await phien.setItem(KHOA, phienDai());
  await phien.removeItem(KHOA);

  expect([...kho.kho.keys()]).toEqual([]);
  expect(await phien.getItem(KHOA)).toBeNull();
});
