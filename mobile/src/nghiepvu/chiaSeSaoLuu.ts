/**
 * Gửi một bản sao lưu **ra khỏi app** — qua Zalo, mail, hay lưu vào Files/Drive của người
 * dùng.
 *
 * Đây là đường duy nhất chống được mất máy. Bản trong máy ([saoLuuMay](./saoLuuMay.ts)) nằm
 * trong phần riêng của app: xoá app là mất theo. Nên màn hình sao lưu phải mời người dùng
 * thỉnh thoảng gửi một bản ra ngoài, và nói thẳng vì sao.
 *
 * Đóng gói tại đây từ dữ liệu **đang có**, không đọc lại file bản cũ trong máy: người bấm
 * nút này muốn cầm đi bản mới nhất.
 */

import { KIEU_JSON, guiFile } from './chiaSeFile';
import { dongGoi, tenFileSaoLuu } from './goiSaoLuu';
import { DuLieuChamCong } from './kieu';

export async function chiaSeSaoLuu(duLieu: DuLieuChamCong, homNay: string): Promise<string> {
  return guiFile(
    dongGoi(duLieu, new Date().toISOString()),
    tenFileSaoLuu(homNay),
    KIEU_JSON,
    'Gửi bản sao lưu',
  );
}
