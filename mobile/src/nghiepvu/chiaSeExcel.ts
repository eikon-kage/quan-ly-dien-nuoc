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
import { taoFileMau, tenFileMau } from './nhapExcel';
import { tenFileExcel, xuatExcel } from './xuatExcel';

const KIEU_FILE = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

/** Máy không chia sẻ file được (rất hiếm, chủ yếu là lúc chạy trên web). */
export class KhongChiaSeDuoc extends Error {
  constructor() {
    super('Máy này không gửi file đi được.');
  }
}

/**
 * Ghi khối byte ra thư mục tạm rồi mở bảng chia sẻ. Người dùng bấm huỷ thì hàm vẫn kết
 * thúc êm — hệ điều hành không cho biết họ đã gửi hay đã huỷ, nên đừng khoe "đã gửi xong".
 */
async function gui(noiDung: Uint8Array, tenFile: string, tieuDe: string): Promise<string> {
  const file = new File(Paths.cache, tenFile);
  // Gửi lần thứ hai cùng một tên file thì ghi đè lên file cũ.
  file.create({ overwrite: true });
  file.write(noiDung);

  if (!(await Sharing.isAvailableAsync())) {
    throw new KhongChiaSeDuoc();
  }

  await Sharing.shareAsync(file.uri, {
    mimeType: KIEU_FILE,
    UTI: 'org.openxmlformats.spreadsheetml.sheet',
    dialogTitle: tieuDe,
  });

  return file.uri;
}

/** Toàn bộ sổ sách, gửi đi để mở bằng Excel trên máy tính. */
export async function chiaSeExcel(duLieu: DuLieuChamCong, homNay: string): Promise<string> {
  return gui(xuatExcel(duLieu, homNay), tenFileExcel(homNay), 'Gửi file chấm công');
}

/**
 * File mẫu để điền công cho một thợ, gửi sang máy tính mà gõ.
 *
 * Gửi qua bảng chia sẻ chứ không lặng lẽ lưu vào máy: người dùng chọn luôn chỗ để —
 * Zalo gửi cho mình, hộp thư, hay thư mục Files — và tự biết file đang nằm đâu mà mở lại.
 */
export async function chiaSeFileMau(
  tenTho: string,
  tuNgay: string,
  denNgay: string,
): Promise<string> {
  return gui(
    taoFileMau(tenTho, tuNgay, denNgay),
    tenFileMau(tenTho, tuNgay),
    'Gửi file mẫu chấm công',
  );
}
