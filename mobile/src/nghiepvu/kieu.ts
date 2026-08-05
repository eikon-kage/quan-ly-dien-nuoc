/**
 * Kiểu dữ liệu của app chấm công.
 *
 * Ngày để dạng chuỗi "yyyy-MM-dd" chứ không dùng Date: Date mang theo giờ và múi giờ,
 * chấm công lúc 23h hay lúc 1h sáng dễ nhảy sang ngày khác. Chuỗi thì lưu ra JSON,
 * so sánh và sắp xếp đều đúng.
 */

export type BuoiLam = 'Sang' | 'Chieu';

export const CAC_BUOI: BuoiLam[] = ['Sang', 'Chieu'];

/**
 * Một mốc tiền công: từ ngày này trở đi thợ được trả bằng này một công.
 * Tăng lương là thêm một mốc mới chứ không sửa đè lên mốc cũ — nhờ vậy bảng lương
 * các tháng trước vẫn tính đúng theo giá của lúc đó.
 */
export interface MocLuong {
  tuNgay: string;
  tienMotCong: number;
}

/** Một người thợ. Tiền công lưu thành lịch sử vì lương có thể tăng theo thời gian. */
export interface Tho {
  id: string;
  ten: string;
  dienThoai: string;
  /** Các mốc tiền công, xếp theo tuNgay tăng dần. Luôn có ít nhất một mốc. */
  mocLuong: MocLuong[];
  /** Thợ đã nghỉ thì tắt, không hiện ra màn hình chấm công nữa. */
  dangLam: boolean;
  ghiChu: string;
  ngayTao: string;
  /** Lần sửa gần nhất, để sau này đồng bộ với máy tính. */
  suaLuc: string;
}

/** Một buổi công đã chấm. Mỗi (thợ, ngày, buổi) chỉ có tối đa một bản ghi. */
export interface BuoiCong {
  id: string;
  thoId: string;
  ngay: string;
  buoi: BuoiLam;
  /** Bình thường là 1. Về sớm thì 0,5; làm thêm thì 1,5. */
  soCong: number;
  /**
   * Giá riêng chỉ cho buổi này, dùng khi có ngoại lệ (việc nặng trả thêm chẳng hạn).
   * Để trống — và bình thường luôn để trống — thì tính theo mốc lương của thợ tại ngày đó.
   */
  tienMotCong: number | null;
  ghiChu: string;
  suaLuc: string;
}

/** Một lần thợ ứng tiền trước, cuối kỳ trừ vào tiền công. */
export interface UngTien {
  id: string;
  thoId: string;
  ngay: string;
  soTien: number;
  ghiChu: string;
  suaLuc: string;
}

/** Toàn bộ dữ liệu chấm công, được lưu thành một khối JSON. */
export interface DuLieuChamCong {
  thos: Tho[];
  buoiCongs: BuoiCong[];
  ungTiens: UngTien[];
}

export function duLieuRong(): DuLieuChamCong {
  return { thos: [], buoiCongs: [], ungTiens: [] };
}
