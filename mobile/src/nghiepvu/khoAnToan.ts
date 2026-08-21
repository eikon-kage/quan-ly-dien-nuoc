/**
 * Kho khoá–giá trị an toàn của máy: **Keychain** của iOS, **Keystore** của Android.
 *
 * Tách khỏi [khoPhienSupabase](./khoPhienSupabase.ts) chỉ vì một lý do: `expo-secure-store`
 * không có bản web (tài liệu v57 ghi đúng ba nền tảng Android, iOS, tvOS). Phần *cắt khúc
 * và ghép lại* thì cả hai nền tảng dùng chung, nên chỉ mỗi chỗ chạm vào máy này là có bản
 * `.web.ts` riêng — và trên Android thì bundle không chứa một dòng nào của bản web ấy.
 */

import * as SecureStore from 'expo-secure-store';

import type { KhoAnToan } from './khoPhienSupabase';

export function khoMay(): KhoAnToan {
  return {
    doc: (khoa) => SecureStore.getItemAsync(khoa),
    ghi: (khoa, gia) => SecureStore.setItemAsync(khoa, gia),
    xoa: (khoa) => SecureStore.deleteItemAsync(khoa),
  };
}
