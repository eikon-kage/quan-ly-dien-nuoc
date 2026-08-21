/**
 * Ba bộ kiểm thử chạy bằng ba bộ máy khác nhau:
 *   nghiepvu — TypeScript thuần, chạy bằng ts-jest, rất nhanh.
 *   giaodien — có JSX và thư viện React Native, phải chạy qua jest-expo.
 *   web      — mấy file `.web.ts` của bản chạy trên trình duyệt: cần `document` và
 *              `localStorage`, nên chạy qua jest-expo/web (jsdom, và `react-native` được
 *              thay bằng react-native-web).
 *
 * Bài của bộ `web` để trong thư mục `__tests__/web/`, và hai bộ kia phải **bỏ qua** thư mục
 * ấy: khuôn tên bài của chúng (`*.test.ts`, `*.test.tsx`) khớp luôn cả bài của bộ web, mà
 * chạy bài web trên máy native thì nó nạp đúng bản không phải bản cần thử.
 *
 * **Vì sao bộ web phải tự khai `transform` chứ không chỉ `preset: 'jest-expo/web'`.** Khuôn
 * mẫu web của jest-expo gọi babel-jest mà **không kèm bộ tiền xử lý của Expo** (khuôn mẫu
 * native thì có). Dự án này lại không có `babel.config.js` — Expo tự lo phần ấy lúc đóng gói
 * — nên babel-jest chạy suông, không đổi `import` thành gì cả, và bài nào cũng chết ngay dòng
 * `import` đầu tiên. Khai đúng bộ tiền xử lý ấy vào đây là xong, và không phải thêm
 * `babel.config.js` — thêm vào là đụng luôn cách đóng gói bản Android.
 */
const khuonWeb = require('jest-expo/web/jest-preset');

const BO_QUA_WEB = ['/__tests__/web/'];

module.exports = {
  projects: [
    {
      displayName: 'nghiepvu',
      preset: 'ts-jest',
      testEnvironment: 'node',
      testMatch: ['<rootDir>/src/nghiepvu/__tests__/**/*.test.ts'],
      testPathIgnorePatterns: BO_QUA_WEB,
    },
    {
      displayName: 'giaodien',
      preset: 'jest-expo',
      testMatch: ['<rootDir>/src/giaodien/__tests__/**/*.test.tsx'],
      setupFilesAfterEnv: ['<rootDir>/src/giaodien/__tests__/chuanBi.ts'],
      testPathIgnorePatterns: BO_QUA_WEB,
    },
    {
      ...khuonWeb,
      displayName: 'web',
      testMatch: ['<rootDir>/src/**/__tests__/web/**/*.test.ts?(x)'],
      transform: {
        ...khuonWeb.transform,
        '\\.[jt]sx?$': [
          'babel-jest',
          {
            presets: [require.resolve('expo/internal/babel-preset.js')],
            caller: { name: 'metro', bundler: 'metro', platform: 'web' },
          },
        ],
      },
    },
  ],
};
