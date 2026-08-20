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
 */

import * as DocumentPicker from 'expo-document-picker';
import { File } from 'expo-file-system';

export interface FileNguon {
  ten: string;
  /** Chưa đọc gì cả — bên gọi tự chọn đọc ra byte (`bytes()`) hay ra chữ (`text()`). */
  file: File;
}

/** Chọn một file. Người dùng bấm huỷ thì trả về `null` — huỷ không phải là lỗi. */
export async function chonFile(): Promise<FileNguon | null> {
  const chon = await DocumentPicker.getDocumentAsync({ multiple: false });
  if (chon.canceled) {
    return null;
  }

  const muc = chon.assets[0];
  return { ten: muc.name ?? '', file: new File(muc.uri) };
}
