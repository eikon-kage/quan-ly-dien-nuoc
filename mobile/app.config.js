/**
 * Phần cấu hình phải tính lúc dựng app, không viết cứng vào app.json được.
 *
 * Chỉ có một việc: khai các URL scheme để Google gọi ngược về app sau khi người dùng bấm
 * Đồng ý ở màn hình đăng nhập. Trên iOS scheme ấy chính là client ID viết ngược, mà client
 * ID thì nằm trong biến môi trường nên phải ghép ở đây.
 *
 * app.json vẫn giữ nguyên mọi thứ khác; file này nhận nó vào qua tham số `config` rồi chỉ
 * thêm phần thiếu.
 */

const CLIENT_ID_IOS = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID_IOS ?? '';

/** "123-abc.apps.googleusercontent.com" → "com.googleusercontent.apps.123-abc". */
function schemeIOS(clientId) {
  return clientId.split('.').reverse().join('.');
}

module.exports = ({ config }) => ({
  ...config,

  // Scheme chung cho các đường dẫn sâu vào app sau này. Google không dùng cái này.
  scheme: 'cham-cong',

  ios: {
    ...config.ios,
    // Chưa điền client ID thì đừng khai scheme rỗng — bản dựng sẽ hỏng.
    ...(CLIENT_ID_IOS === '' ? {} : { scheme: schemeIOS(CLIENT_ID_IOS) }),
  },

  android: {
    ...config.android,
    // Android quy ước scheme là chính tên gói app, không phụ thuộc client ID.
    scheme: config.android.package,
  },
});
