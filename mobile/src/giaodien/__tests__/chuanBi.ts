/** Chuẩn bị cho kiểm thử giao diện: thay các thứ chạm vào phần cứng bằng hàng giả. */

// Bộ nhớ của điện thoại: dùng bản giả sẵn có của thư viện, lưu vào RAM.
jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock'),
);

// Máy chạy kiểm thử không có mô-tơ rung.
jest.mock('expo-haptics', () => ({
  impactAsync: jest.fn(() => Promise.resolve()),
  ImpactFeedbackStyle: { Medium: 'Medium' },
}));

// Icon thật phải nạp font từ file, không cần thiết cho kiểm thử. Thay bằng chữ "icon:tên"
// để bài kiểm thử vẫn tra được là đã vẽ đúng icon nào.
jest.mock('@expo/vector-icons', () => {
  const React = require('react');
  const { Text } = require('react-native');

  return {
    Feather: ({ name }: { name: string }) => React.createElement(Text, null, `icon:${name}`),
  };
});
