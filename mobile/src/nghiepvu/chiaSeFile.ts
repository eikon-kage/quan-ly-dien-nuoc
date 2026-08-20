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
 */

import { File, Paths } from 'expo-file-system';
import * as Sharing from 'expo-sharing';

/** Máy không chia sẻ file được (rất hiếm, chủ yếu là lúc chạy trên web). */
export class KhongChiaSeDuoc extends Error {
  constructor() {
    super('Máy này không gửi file đi được.');
  }
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
