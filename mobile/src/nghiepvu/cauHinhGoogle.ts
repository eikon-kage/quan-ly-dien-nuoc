/**
 * Client ID của Google, đọc từ biến môi trường lúc dựng app.
 *
 * Client ID **không phải là bí mật** — nó nằm sẵn trong mọi app cài trên máy người dùng,
 * ai gỡ app ra cũng đọc được. Google chặn kẻ giả mạo bằng cách khác: trên iOS phải khớp
 * bundle ID, trên Android phải khớp cả tên gói lẫn vân tay chữ ký. Nên để client ID trong
 * biến `EXPO_PUBLIC_*` là đúng, không cần máy chủ trung gian.
 *
 * Ngược lại, **client secret thì tuyệt đối không được nhét vào đây**. Vì vậy phải tạo
 * client kiểu iOS và Android (hai kiểu này không có secret), chứ không dùng client kiểu
 * "Web application".
 *
 * Xem docs/chamcong-sao-luu-drive.md để biết cách lấy hai mã này.
 */

import { Platform } from 'react-native';

const CLIENT_ID_IOS = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID_IOS ?? '';
const CLIENT_ID_ANDROID = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID_ANDROID ?? '';

/**
 * Tên gói của app, phải khớp `android.package` trong app.json. Android dùng chính nó
 * làm scheme để Google trả kết quả đăng nhập về app.
 */
const TEN_GOI_ANDROID = 'com.quanlydiennuoc.chamcong';

/**
 * Quyền xin của Google: chỉ `drive.file` — app **chỉ thấy được những file do chính nó
 * tạo ra**, không đọc được gì khác trong Drive của người dùng.
 *
 * Đừng đổi sang `drive` hay `drive.readonly`: hai quyền ấy đọc được cả kho Drive, Google
 * xếp vào loại hạn chế, muốn phát hành rộng phải qua kiểm định bảo mật tốn kém. Sao lưu
 * chỉ cần ghi file của mình nên `drive.file` là vừa đủ.
 */
export const QUYEN = ['https://www.googleapis.com/auth/drive.file'];

/** Địa chỉ máy chủ OAuth của Google. Ghi thẳng chứ không dò tự động cho đỡ một vòng mạng. */
export const MAY_CHU_GOOGLE = {
  authorizationEndpoint: 'https://accounts.google.com/o/oauth2/v2/auth',
  tokenEndpoint: 'https://oauth2.googleapis.com/token',
  revocationEndpoint: 'https://oauth2.googleapis.com/revoke',
};

export function clientId(): string {
  return Platform.OS === 'ios' ? CLIENT_ID_IOS : CLIENT_ID_ANDROID;
}

/**
 * Chưa điền client ID thì coi như tính năng chưa bật — giao diện ẩn hẳn phần Drive đi
 * thay vì để người dùng bấm vào rồi nhận lỗi khó hiểu.
 */
export function daCauHinh(): boolean {
  return clientId() !== '';
}

/**
 * Địa chỉ Google gọi ngược về app sau khi người dùng bấm Đồng ý.
 *
 * Hai nền tảng hai kiểu, đây là quy ước của Google chứ không phải mình tự đặt:
 *   iOS     — client ID viết ngược, ví dụ "com.googleusercontent.apps.123-abc:/oauthredirect".
 *   Android — chính tên gói app, "com.quanlydiennuoc.chamcong:/oauth2redirect".
 *
 * Cả hai scheme này phải được khai trong app.json, nếu không thì bấm Đồng ý xong màn hình
 * đăng nhập đứng im, không quay về app được.
 */
export function diaChiTraVe(): string {
  if (Platform.OS === 'ios') {
    return `${schemeIOS(CLIENT_ID_IOS)}:/oauthredirect`;
  }
  return `${TEN_GOI_ANDROID}:/oauth2redirect`;
}

/** "123-abc.apps.googleusercontent.com" → "com.googleusercontent.apps.123-abc". */
export function schemeIOS(id: string): string {
  return id.split('.').reverse().join('.');
}
