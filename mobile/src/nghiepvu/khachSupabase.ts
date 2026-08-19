/**
 * Cái "khách" nối vào Supabase — dựng đúng một lần cho cả app.
 *
 * Dựng **muộn**, chỉ khi thật sự cần: chưa điền địa chỉ project thì không tạo gì cả. Tạo sẵn
 * một khách với địa chỉ rỗng thì thư viện quăng lỗi ngay lúc app khởi động, mà máy chưa dùng
 * Supabase cũng chết theo — trong khi cả tính năng này lẽ ra chỉ nên im lặng ẩn đi.
 *
 * Cũng chỉ dựng **một lần** rồi giữ lại: mỗi khách mang theo một vòng tự làm mới token và
 * một kênh nghe thay đổi đăng nhập; dựng hai lần là hai vòng chạy song song, tranh nhau ghi
 * phiên vào cùng một chỗ.
 */

import { SupabaseClient, createClient } from '@supabase/supabase-js';

import { daCauHinh, diaChi, khoaCongKhai } from './cauHinhSupabase';
import { KhoPhien, khoPhien } from './khoPhienSupabase';

/** Máy này chưa được điền địa chỉ project và khoá công khai. */
export class ChuaCauHinh extends Error {
  constructor() {
    super('Máy này chưa được cấu hình để nối nhóm chấm công.');
  }
}

let dangGiu: SupabaseClient | null = null;

export function hoTro(): boolean {
  return daCauHinh();
}

/**
 * `kho` nhận từ ngoài để bài kiểm thử khỏi phải chạm vào SecureStore thật.
 */
export function khach(kho: KhoPhien = khoPhien()): SupabaseClient {
  if (!daCauHinh()) {
    throw new ChuaCauHinh();
  }

  if (dangGiu === null) {
    dangGiu = createClient(diaChi(), khoaCongKhai(), {
      auth: {
        storage: kho,
        persistSession: true,
        autoRefreshToken: true,
        // Không có URL để mà đọc phiên từ đó: đây là app trên máy, không phải trang web.
        detectSessionInUrl: false,
      },
    });
  }

  return dangGiu;
}

/** Dùng trong kiểm thử, và sau khi đăng xuất hẳn để lần nối sau dựng lại từ đầu. */
export function boKhachDangGiu(): void {
  dangGiu = null;
}
