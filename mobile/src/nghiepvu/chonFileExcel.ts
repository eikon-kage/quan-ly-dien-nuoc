/**
 * Chọn một file .xlsx để nhập công vào app.
 *
 * Phần mở bảng chọn của hệ điều hành nằm ở [chonFile](./chonFile.ts); ở đây chỉ soát đuôi
 * tên trước khi đọc, để nói cho người dùng biết họ chọn nhầm thứ gì.
 */

import { chonFile } from './chonFile';

/** Người dùng chọn nhầm thứ không phải bảng tính. */
export class KhongPhaiFileExcel extends Error {}

export interface FileDaChon {
  ten: string;
  noiDung: Uint8Array;
}

export async function chonFileExcel(): Promise<FileDaChon | null> {
  const chon = await chonFile();
  if (!chon) {
    return null;
  }

  const duoi = chon.ten.toLowerCase();

  if (duoi.endsWith('.xls')) {
    throw new KhongPhaiFileExcel(
      'File .xls là bản Excel đời cũ. Anh mở bằng Excel rồi lưu lại thành .xlsx nhé.',
    );
  }
  if (!duoi.endsWith('.xlsx')) {
    throw new KhongPhaiFileExcel('Anh chọn file Excel đuôi .xlsx nhé.');
  }

  return { ten: chon.ten, noiDung: await chon.bytes() };
}
