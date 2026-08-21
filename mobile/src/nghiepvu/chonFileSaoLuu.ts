/**
 * Chọn một file sao lưu .json từ trong máy để khôi phục.
 *
 * Chỉ soát đuôi tên ở đây, còn kiểm nội dung là việc của [goiSaoLuu.moGoi](./goiSaoLuu.ts)
 * — chỗ đã soát rất kỹ vì khôi phục là thao tác ghi đè. Soát đuôi trước chỉ để đỡ cho người
 * dùng một lần đọc file vô ích, không phải để tin file.
 */

import { chonFile } from './chonFile';

/** Người dùng chọn nhầm thứ không phải bản sao lưu. */
export class KhongPhaiFileSaoLuu extends Error {}

/** Trả về nội dung file dạng chữ, hoặc `null` nếu người dùng bấm huỷ. */
export async function chonFileSaoLuu(): Promise<string | null> {
  const chon = await chonFile();
  if (!chon) {
    return null;
  }

  if (!chon.ten.toLowerCase().endsWith('.json')) {
    throw new KhongPhaiFileSaoLuu('Bản sao lưu là file đuôi .json. Anh chọn lại nhé.');
  }

  return chon.text();
}
