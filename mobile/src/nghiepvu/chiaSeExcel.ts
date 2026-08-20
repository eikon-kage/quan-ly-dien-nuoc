/**
 * Gửi các file Excel của app đi: toàn bộ sổ sách của chủ, file mẫu để gõ trên máy tính, và
 * sổ công của một thợ.
 *
 * Phần ghi file rồi mở bảng chia sẻ nằm ở [chiaSeFile](./chiaSeFile.ts) — ở đây chỉ ghép
 * "dựng nội dung nào" với "gửi đi kèm tên gì".
 */

import { KIEU_EXCEL, guiFile } from './chiaSeFile';
import { DuLieuChamCong } from './kieu';
import { taoFileMau, tenFileMau } from './nhapExcel';
import { SoCong } from './soCong';
import { tenFileExcel, xuatExcel } from './xuatExcel';
import { tenFileSoCong, xuatSoCong } from './xuatSoCong';

/** Toàn bộ sổ sách, gửi đi để mở bằng Excel trên máy tính. Chỉ dùng trên **máy chủ**. */
export async function chiaSeExcel(duLieu: DuLieuChamCong, homNay: string): Promise<string> {
  return guiFile(xuatExcel(duLieu, homNay), tenFileExcel(homNay), KIEU_EXCEL, 'Gửi file chấm công');
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
  return guiFile(
    taoFileMau(tenTho, tuNgay, denNgay),
    tenFileMau(tenTho, tuNgay),
    KIEU_EXCEL,
    'Gửi file mẫu chấm công',
  );
}

/**
 * Sổ công của một thợ — dùng trên **máy thợ**.
 *
 * Nhận `SoCong` chứ không nhận `DuLieuChamCong`, và đó là chỗ chặn tiền lọt ra file:
 * xem ghi chú đầu [xuatSoCong](./xuatSoCong.ts).
 */
export async function chiaSeSoCong(so: SoCong): Promise<string> {
  return guiFile(xuatSoCong(so), tenFileSoCong(so), KIEU_EXCEL, 'Gửi sổ công');
}
