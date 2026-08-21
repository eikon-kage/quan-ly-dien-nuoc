/**
 * Ghi một file ra bộ nhớ máy rồi mở bảng chia sẻ của hệ điều hành, để gửi qua Zalo, gửi
 * mail hay lưu vào Files.
 *
 * Tách riêng khỏi những chỗ *dựng nội dung* file (xuatExcel, goiSaoLuu): đây là phần chạm
 * vào máy, không chạy được trong bài kiểm thử. Phần dựng nội dung là TypeScript thuần và
 * kiểm thử được thoải mái — cùng một cách chia như chonFile với nhapExcel.
 *
 * File nằm ở thư mục tạm: gửi xong là xong, hệ điều hành tự dọn khi máy hết chỗ. Không cần
 * xin quyền gì cả — người dùng chọn gửi đi đâu ngay trên bảng chia sẻ.
 *
 * Kiểu file và lớp lỗi nằm ở [kieuFile](./kieuFile.ts) rồi xuất lại ở đây, để bên gọi vẫn
 * `import ... from './chiaSeFile'` như cũ mà bản web dùng chung được đúng một lớp lỗi.
 */

import { File, Paths } from 'expo-file-system';
import * as Sharing from 'expo-sharing';

import { KIEU_EXCEL, KIEU_JSON, KhongChiaSeDuoc, KieuFile } from './kieuFile';

export { KIEU_EXCEL, KIEU_JSON, KhongChiaSeDuoc, KieuFile };

/**
 * Ghi khối nội dung ra thư mục tạm rồi mở bảng chia sẻ. Người dùng bấm huỷ thì hàm vẫn kết
 * thúc êm — hệ điều hành không cho biết họ đã gửi hay đã huỷ, nên đừng khoe "đã gửi xong".
 */
export async function guiFile(
  noiDung: Uint8Array | string,
  tenFile: string,
  kieu: KieuFile,
  tieuDe: string,
): Promise<string> {
  const file = new File(Paths.cache, tenFile);
  // Gửi lần thứ hai cùng một tên file thì ghi đè lên file cũ.
  file.create({ overwrite: true });
  file.write(noiDung);

  if (!(await Sharing.isAvailableAsync())) {
    throw new KhongChiaSeDuoc();
  }

  await Sharing.shareAsync(file.uri, {
    mimeType: kieu.mime,
    UTI: kieu.uti,
    dialogTitle: tieuDe,
  });

  return file.uri;
}
