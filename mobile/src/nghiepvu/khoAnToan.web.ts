/**
 * Bản web của [khoAnToan](./khoAnToan.ts): `localStorage`.
 *
 * **Phải nói thẳng đây là một bước lùi về bảo mật.** Lời tựa của
 * [khoPhienSupabase](./khoPhienSupabase.ts) nói rõ vì sao phiên đăng nhập phải nằm trong
 * Keychain chứ không phải file thường: trong phiên có refresh token, cầm được nó là đọc
 * được sổ công của cả nhóm. Trên web thì không có Keychain nào để dùng — trình duyệt chỉ
 * cho `localStorage`, mà `localStorage` thì bất kỳ mã JavaScript nào chạy trên trang cũng
 * đọc được.
 *
 * Đổi lại, mặt tấn công trên web hẹp hơn máy đã jailbreak: trang này không nhúng quảng cáo,
 * không nạp script từ đâu khác, và Supabase thì mỗi trang web một `origin` riêng nên trang
 * khác không đọc trộm được. Nghĩa là chỉ còn một cửa: có kẻ chèn được mã lạ vào chính bản
 * dựng này. Giữ nguyên nguyên tắc "không thêm script ngoài vào trang" là giữ được cửa ấy.
 *
 * `localStorage` có thể **không dùng được** chứ không chỉ là trống: Safari ở chế độ riêng tư
 * hoặc máy đặt chặn dữ liệu website sẽ ném lỗi ngay lúc đọc. Rơi vào đó thì giữ phiên trong
 * RAM: đăng nhập vẫn xong, chỉ là đóng tab là phải đăng nhập lại — thà vậy còn hơn cả app
 * không mở lên được.
 */

import type { KhoAnToan } from './khoPhienSupabase';

/** Phiên giữ trong RAM, dùng khi trình duyệt không cho ghi xuống. */
const trongRam = new Map<string, string>();

function coLocalStorage(): boolean {
  try {
    const thu = '__thu.chamcong__';
    window.localStorage.setItem(thu, '1');
    window.localStorage.removeItem(thu);
    return true;
  } catch {
    return false;
  }
}

export function khoMay(): KhoAnToan {
  if (!coLocalStorage()) {
    return {
      doc: async (khoa) => trongRam.get(khoa) ?? null,
      ghi: async (khoa, gia) => void trongRam.set(khoa, gia),
      xoa: async (khoa) => void trongRam.delete(khoa),
    };
  }

  return {
    doc: async (khoa) => window.localStorage.getItem(khoa),
    ghi: async (khoa, gia) => window.localStorage.setItem(khoa, gia),
    xoa: async (khoa) => window.localStorage.removeItem(khoa),
  };
}
