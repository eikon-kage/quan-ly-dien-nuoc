/**
 * Chuẩn bị cho kiểm thử giao diện: thay các thứ chạm vào phần cứng bằng hàng giả.
 *
 * File này là `setupFilesAfterEnv` nên chạy sẵn cho mọi bài; phần cuối còn vài hàng giả dùng
 * chung, phải `import` mới có.
 */

import { DieuKhienSaoLuuTaiKhoan, TrangThaiSaoLuuTaiKhoan } from '../dungSaoLuuTaiKhoan';

// Bộ nhớ của điện thoại: dùng bản giả sẵn có của thư viện, lưu vào RAM.
jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock'),
);

// Lề an toàn (tai thỏ, vạch home) đo bằng mã máy nên chỉ có số thật lúc chạy trên máy.
// Bài kiểm thử dựng thẳng từng màn hình, không có `SafeAreaProvider` bọc ngoài như app thật,
// nên `useSafeAreaInsets()` sẽ quăng lỗi. Dùng bản giả sẵn có của thư viện: lề bằng 0.
// Bản giả của thư viện khai bằng `export default`, nên phải lấy đúng `.default`.
jest.mock('react-native-safe-area-context', () =>
  require('react-native-safe-area-context/jest/mock').default,
);

// Icon thật phải nạp font từ file, không cần thiết cho kiểm thử. Thay bằng chữ "icon:tên"
// để bài kiểm thử vẫn tra được là đã vẽ đúng icon nào.
jest.mock('@expo/vector-icons', () => {
  const React = require('react');
  const { Text } = require('react-native');

  return {
    Feather: ({ name }: { name: string }) => React.createElement(Text, null, `icon:${name}`),
  };
});


/**
 * Bản giả của điều khiển sao lưu lên tài khoản.
 *
 * Để ở đây vì ba bài kiểm thử khác nhau cần nó, mà cái nó thay thế thì cũng là một thứ chạm ra
 * ngoài máy: mặc định là *máy không có đường này* (`hoTro: false`), tức là dáng của mọi bài
 * kiểm thử không nói gì tới tài khoản.
 */
export function taiKhoanGia(
  sua: Partial<TrangThaiSaoLuuTaiKhoan> = {},
): DieuKhienSaoLuuTaiKhoan {
  return {
    trangThai: {
      hoTro: false,
      dangDoc: false,
      dangChay: false,
      lucCuoi: null,
      loi: null,
      cacBan: null,
      banChoLay: null,
      ...sua,
    },
    dayNgay: jest.fn(() => Promise.resolve()),
    docBan: jest.fn(),
    daTraLoi: jest.fn(),
  };
}
