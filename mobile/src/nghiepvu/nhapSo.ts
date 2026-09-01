/** Đọc số người dùng gõ vào. Gõ kiểu gì cũng hiểu, miễn là có chữ số. */

/**
 * Một buổi nhiều nhất bằng này công. Không phải luật lệ gì, chỉ để chặn gõ nhầm: gõ "5"
 * thay vì "0,5" mà lọt qua thì cuối tháng tiền công sai gấp mười.
 *
 * Bằng năm lần một buổi đi đủ (`CONG_MOT_BUOI`), đúng như hồi mức chặn còn là 5 công của
 * luật cũ — rộng rãi để buổi làm thêm bao nhiêu cũng ghi được, mà vẫn chặn được số lạc.
 */
export const CONG_TOI_DA = 2.5;

/**
 * Đọc số công: "0,5", "0.5", "1", "0,25" đều hiểu. Dấu phẩy và dấu chấm đều là dấu thập
 * phân — người Việt gõ phẩy, bàn phím số của máy cho dấu chấm.
 *
 * Trả về null nếu không đọc được hoặc không lớn hơn 0. Số quá lớn vẫn trả về để màn hình
 * còn nói được là "nhiều quá", chứ trả null thì người dùng chỉ thấy nút Ghi mờ đi mà
 * không hiểu vì sao.
 */
export function docSoCong(chu: string | null | undefined): number | null {
  if (!chu) {
    return null;
  }

  const sach = chu.trim().replace(',', '.');
  if (!/^\d*\.?\d*$/.test(sach) || sach === '' || sach === '.') {
    return null;
  }

  const so = Number(sach);
  if (!Number.isFinite(so) || so <= 0) {
    return null;
  }

  // Làm tròn hai chữ số thập phân: nửa buổi (0,25 công) là đủ nhỏ rồi.
  return Math.round(so * 100) / 100;
}

/**
 * Đọc "300.000", "300000", "300 000" hay "300,000" đều ra 300000.
 * Không có chữ số nào thì trả về null.
 */
export function docTien(chu: string | null | undefined): number | null {
  if (!chu) {
    return null;
  }

  const so = chu.replace(/\D/g, '');
  if (so.length === 0) {
    return null;
  }

  const ketQua = Number(so);
  return Number.isFinite(ketQua) ? ketQua : null;
}
