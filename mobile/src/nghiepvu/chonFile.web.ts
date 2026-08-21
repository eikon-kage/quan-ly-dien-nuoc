/**
 * Bản web của [chonFile](./chonFile.ts): mở đúng bảng chọn file của trình duyệt.
 *
 * `expo-document-picker` **có** bản web (nó dựng một `<input type="file">` ẩn rồi bấm hộ),
 * nên ở đây không phải tự làm gì nhiều. Hai chỗ khác bản trên máy:
 *
 * 1. `base64: false` — mặc định của thư viện là đọc cả file thành chuỗi base64 trước khi
 *    trả về, tức là một bản sao phình 4/3 nằm trong RAM mà mình không dùng tới. Tắt đi thì
 *    nó trả luôn đối tượng `File` của trình duyệt, đọc ra byte hay ra chữ đều nhanh.
 *
 * 2. Đọc từ `muc.file` chứ không từ `muc.uri`: expo-file-system không có bản web nên `uri`
 *    (một địa chỉ `blob:`) ở đây vô dụng. Thu hồi luôn địa chỉ ấy cho đỡ giữ file trong RAM
 *    — nhập vài chục file một buổi thì mỗi lần giữ lại một bản là có lúc hết chỗ.
 *
 * **Bẫy đã lường:** tài liệu Expo nói bản web "không báo được việc người dùng bấm huỷ".
 * Đọc mã nguồn thư viện thì nó *có* nghe sự kiện `cancel` của thẻ `input`, mà sự kiện ấy
 * Safari đã hỗ trợ từ 16.4 (tháng 3/2023). Nên trên iPhone đời còn cập nhật được thì bấm
 * huỷ vẫn trả về `null` đúng như trên máy. Safari cũ hơn thì lời hứa treo, người dùng thấy
 * vòng xoay quay mãi — chấp nhận, vì đường duy nhất để chắc chắn là tự dựng `input` lấy,
 * mà làm vậy thì mất luôn cái lợi dùng chung mã với bản trên máy.
 */

import * as DocumentPicker from 'expo-document-picker';

import { FileNguon } from './kieuFile';

export { FileNguon };

/** Chọn một file. Người dùng bấm huỷ thì trả về `null` — huỷ không phải là lỗi. */
export async function chonFile(): Promise<FileNguon | null> {
  const chon = await DocumentPicker.getDocumentAsync({ multiple: false, base64: false });
  if (chon.canceled) {
    return null;
  }

  const muc = chon.assets[0];
  const file = muc.file;
  if (!file) {
    // Không xảy ra với bản web của thư viện, nhưng thà báo rõ còn hơn nổ ở chỗ khác.
    throw new Error('Trình duyệt không trả về nội dung file vừa chọn.');
  }

  URL.revokeObjectURL(muc.uri);

  return {
    ten: muc.name ?? file.name ?? '',
    bytes: async () => new Uint8Array(await file.arrayBuffer()),
    text: () => file.text(),
  };
}
