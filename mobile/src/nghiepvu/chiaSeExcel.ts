/**
 * Ghi file Excel ra bộ nhớ máy rồi mở bảng chia sẻ của hệ điều hành, để gửi qua Zalo,
 * gửi mail hay lưu vào Files/Drive.
 *
 * File nằm ở thư mục tạm: gửi xong là xong, hệ điều hành tự dọn khi máy hết chỗ. Không
 * cần xin quyền gì cả — người dùng chọn gửi đi đâu ngay trên bảng chia sẻ.
 */

import { File, Paths } from 'expo-file-system';
import * as Sharing from 'expo-sharing';

import { DuLieuChamCong } from './kieu';
import { tenFileExcel, xuatExcel } from './xuatExcel';

const KIEU_FILE = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

/** Máy không chia sẻ file được (rất hiếm, chủ yếu là lúc chạy trên web). */
export class KhongChiaSeDuoc extends Error {
  constructor() {
    super('Máy này không gửi file đi được.');
  }
}

/**
 * Dựng file rồi mở bảng chia sẻ. Người dùng bấm huỷ thì hàm vẫn kết thúc êm — hệ điều
 * hành không cho biết họ đã gửi hay đã huỷ, nên đừng khoe "đã gửi xong".
 */
export async function chiaSeExcel(duLieu: DuLieuChamCong, homNay: string): Promise<string> {
  const noiDung = xuatExcel(duLieu, homNay);

  const file = new File(Paths.cache, tenFileExcel(homNay));
  // Xuất lần thứ hai trong cùng một ngày thì ghi đè lên file cũ.
  file.create({ overwrite: true });
  file.write(noiDung);

  if (!(await Sharing.isAvailableAsync())) {
    throw new KhongChiaSeDuoc();
  }

  await Sharing.shareAsync(file.uri, {
    mimeType: KIEU_FILE,
    UTI: 'org.openxmlformats.spreadsheetml.sheet',
    dialogTitle: 'Gửi file chấm công',
  });

  return file.uri;
}
