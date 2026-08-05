/**
 * Hai bộ kiểm thử chạy bằng hai bộ máy khác nhau:
 *   nghiepvu — TypeScript thuần, chạy bằng ts-jest, rất nhanh.
 *   giaodien — có JSX và thư viện React Native, phải chạy qua jest-expo.
 */
module.exports = {
  projects: [
    {
      displayName: 'nghiepvu',
      preset: 'ts-jest',
      testEnvironment: 'node',
      testMatch: ['<rootDir>/src/nghiepvu/__tests__/**/*.test.ts'],
    },
    {
      displayName: 'giaodien',
      preset: 'jest-expo',
      testMatch: ['<rootDir>/src/giaodien/__tests__/**/*.test.tsx'],
      setupFilesAfterEnv: ['<rootDir>/src/giaodien/__tests__/chuanBi.ts'],
    },
  ],
};
