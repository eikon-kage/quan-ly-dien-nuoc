/**
 * Mở bảng chọn file của hệ điều hành. Đây là phần **chạm vào máy** của việc nhận file từ
 * ngoài vào — tách riêng để phần hiểu nội dung file (nhapExcel, goiSaoLuu) là TypeScript
 * thuần và kiểm thử được thoải mái.
 *
 * `copyToCacheDirectory` để mặc định (bật): trên Android, file chọn từ Zalo hay Google
 * Drive về là đường dẫn `content://` mà đọc thẳng không được — bật lên thì hệ điều hành
 * chép ra thư mục tạm và trả về `file://` đọc được ngay.
 *
 * Không lọc theo kiểu file trong bảng chọn, để bên gọi tự soát đuôi tên: nhiều app gửi file
 * đi kèm kiểu "octet-stream" chung chung, lọc chặt thì đúng file cần lại bị làm mờ không
 * bấm được, mà người dùng thì không hiểu vì sao.
 *
 * Trả về `FileNguon` (hai hàm `bytes`/`text`) chứ không trả thẳng `File` của
 * expo-file-system: expo-file-system không có bản web, nên nếu để lộ kiểu ấy ra ngoài thì
 * bản web ([chonFile.web.ts](./chonFile.web.ts)) không cách nào khớp kiểu được.
 */

import * as DocumentPicker from 'expo-document-picker';
import { File } from 'expo-file-system';

import { FileNguon } from './kieuFile';

export { FileNguon };

/** Chọn một file. Người dùng bấm huỷ thì trả về `null` — huỷ không phải là lỗi. */
export async function chonFile(): Promise<FileNguon | null> {
  const chon = await DocumentPicker.getDocumentAsync({ multiple: false });
  if (chon.canceled) {
    return null;
  }

  const muc = chon.assets[0];
  const file = new File(muc.uri);
  return {
    ten: muc.name ?? '',
    bytes: () => file.bytes(),
    text: () => file.text(),
  };
}
