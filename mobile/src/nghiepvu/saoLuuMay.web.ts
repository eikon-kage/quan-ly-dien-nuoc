/**
 * Bản web của [saoLuuMay](./saoLuuMay.ts): **không có bản sao lưu trong máy**.
 *
 * `expo-file-system` chỉ có bản Android/iOS/tvOS, mà trình duyệt thì cũng không cho app một
 * thư mục riêng nào để rải 30 file sao lưu vào. Nên trên web đường này tắt hẳn, và giao diện
 * đã lường sẵn: [dungSaoLuu](../giaodien/dungSaoLuu.ts) trả `hoTro: false` trên web, màn hình
 * sao lưu vẽ đúng cảnh "máy này không ghi được bản sao lưu" thay vì danh sách bản.
 *
 * Vẫn phải có file này chứ không để bản trên máy bị nạp vào: giữ cho bundle web sạch, không
 * kéo theo `expo-file-system`, và nếu sau này có chỗ nào gọi mà quên soát `hoTro` thì nhận
 * được câu báo rõ ràng thay vì một lỗi lạ từ trong thư viện.
 *
 * Bù lại chỗ trống này bằng hai đường vẫn chạy tốt trên web: **sao lưu lên tài khoản**
 * ([saoLuuTaiKhoan](./saoLuuTaiKhoan.ts)) và **gửi bản sao lưu ra ngoài**
 * ([chiaSeSaoLuu](./chiaSeSaoLuu.ts)). Màn hình sao lưu mời người dùng làm đúng hai việc ấy.
 */

import type { DuLieuChamCong } from './kieu';
// Lấy đúng kiểu của bản trên máy để hai bên không lệch nhau. `import type` nên lúc chạy
// không có vòng lặp nạp file nào cả.
import type { BanSaoLuu } from './saoLuuMay';

export type { BanSaoLuu };

class KhongSaoLuuVaoMayDuoc extends Error {
  constructor() {
    super('Bản chạy trên web không ghi được bản sao lưu vào máy.');
  }
}

/** Chưa từng sao lưu vào máy, và sẽ không bao giờ — màn hình ẩn luôn dòng "lần cuối". */
export async function lanCuoi(): Promise<string | null> {
  return null;
}

export async function saoLuu(_duLieu: DuLieuChamCong, _homNay: string): Promise<BanSaoLuu> {
  throw new KhongSaoLuuVaoMayDuoc();
}

/** Không có bản nào. Trả danh sách rỗng chứ không ném lỗi: đây là câu trả lời đúng. */
export async function danhSachBan(): Promise<BanSaoLuu[]> {
  return [];
}

export async function docBan(_ten: string): Promise<DuLieuChamCong> {
  throw new KhongSaoLuuVaoMayDuoc();
}
