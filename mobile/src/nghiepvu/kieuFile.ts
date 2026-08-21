/**
 * Mấy kiểu dùng chung cho việc **nhận file vào và gửi file ra**.
 *
 * Đặt riêng một file vì cả bản chạy trên máy (`chonFile.ts`, `chiaSeFile.ts`) và bản chạy
 * trên web (`.web.ts` cạnh chúng) đều cần, mà hai bản ấy thì không nhìn thấy nhau — Metro
 * chọn đúng một bản theo nền tảng lúc đóng gói. Khai lặp ở hai bên thì `instanceof
 * KhongChiaSeDuoc` ở bên ngoài sẽ trượt, mà chỗ bắt lỗi ấy là chỗ hiện câu báo cho người
 * dùng.
 */

/** Một file người dùng vừa chọn. Chưa đọc gì cả — bên gọi tự chọn đọc ra byte hay ra chữ. */
export interface FileNguon {
  ten: string;
  bytes(): Promise<Uint8Array>;
  text(): Promise<string>;
}

/** Khai kiểu file cho hệ điều hành: Android xem `mime`, iOS xem `uti`. */
export interface KieuFile {
  mime: string;
  uti: string;
}

export const KIEU_EXCEL: KieuFile = {
  mime: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  uti: 'org.openxmlformats.spreadsheetml.sheet',
};

export const KIEU_JSON: KieuFile = { mime: 'application/json', uti: 'public.json' };

/** Máy không gửi file đi được. Rất hiếm — chủ yếu là mấy trình duyệt cũ trên bản web. */
export class KhongChiaSeDuoc extends Error {
  constructor() {
    super('Máy này không gửi file đi được.');
  }
}
