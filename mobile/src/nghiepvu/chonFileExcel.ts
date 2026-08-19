/**
 * Mở bảng chọn file của hệ điều hành rồi đọc file người dùng chọn ra thành byte.
 *
 * Tách riêng khỏi [nhapExcel.ts](./nhapExcel.ts) vì đây là phần **chạm vào máy** — không
 * chạy được trong bài kiểm thử, phải thay bằng hàng giả. Phần hiểu nội dung file thì là
 * TypeScript thuần và kiểm thử được thoải mái.
 *
 * `copyToCacheDirectory` để mặc định (bật): trên Android, file chọn từ Zalo hay Google
 * Drive về là đường dẫn `content://` mà đọc thẳng không được — bật lên thì hệ điều hành
 * chép ra thư mục tạm và trả về `file://` đọc được ngay.
 */

import * as DocumentPicker from 'expo-document-picker';
import { File } from 'expo-file-system';

/** Người dùng chọn nhầm thứ không phải bảng tính. */
export class KhongPhaiFileExcel extends Error {}

export interface FileDaChon {
  ten: string;
  noiDung: Uint8Array;
}

/**
 * Chọn một file .xlsx. Người dùng bấm huỷ thì trả về `null` — huỷ không phải là lỗi.
 *
 * Không lọc theo kiểu file trong bảng chọn mà tự xem đuôi tên sau: nhiều app gửi file
 * đi kèm kiểu "octet-stream" chung chung, lọc chặt thì đúng file cần lại bị làm mờ
 * không bấm được, mà người dùng thì không hiểu vì sao.
 */
export async function chonFileExcel(): Promise<FileDaChon | null> {
  const chon = await DocumentPicker.getDocumentAsync({ multiple: false });
  if (chon.canceled) {
    return null;
  }

  const file = chon.assets[0];
  const ten = file.name ?? '';
  const duoi = ten.toLowerCase();

  if (duoi.endsWith('.xls')) {
    throw new KhongPhaiFileExcel(
      'File .xls là bản Excel đời cũ. Anh mở bằng Excel rồi lưu lại thành .xlsx nhé.',
    );
  }
  if (!duoi.endsWith('.xlsx')) {
    throw new KhongPhaiFileExcel('Anh chọn file Excel đuôi .xlsx nhé.');
  }

  return { ten, noiDung: await new File(file.uri).bytes() };
}
