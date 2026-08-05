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

/**
 * Tiền nong của một thợ tại lúc chốt kỳ — bản chụp, không tính lại bao giờ nữa.
 *
 * Chụp cả tên thợ chứ không chỉ id: sau này sửa tên thợ, hay thợ nghỉ hẳn, thì tờ quyết
 * toán cũ vẫn đọc ra đúng tên của lúc trả tiền.
 */
export interface DongQuyetToan {
  thoId: string;
  tenTho: string;
  congSang: number;
  congChieu: number;
  tongCong: number;
  tienCong: number;
  daUng: number;
  /** Tiền còn thiếu mang sang từ kỳ trước. Số âm nghĩa là kỳ trước đã trả dư. */
  noKyTruoc: number;
  /** Số đáng lẽ phải trả: tienCong − daUng + noKyTruoc. */
  phaiTra: number;
  /** Số tiền thực đưa cho thợ hôm chốt kỳ. */
  daTra: number;
  /** phaiTra − daTra. Dương là còn nợ thợ, âm là thợ đã cầm dư, kỳ sau trừ lại. */
  chuyenKySau: number;
}

/**
 * Một kỳ lương đã quyết toán — chốt xong là đóng, không sửa được nữa.
 *
 * Quyết toán **không xoá gì cả**: buổi công và ứng tiền vẫn nằm nguyên trong dữ liệu.
 * Kỳ chỉ ghi lại *những bản ghi nào đã được trả tiền* (`buoiCongIds`, `ungTienIds`) cùng
 * một bản chụp số liệu. Kỳ mới là phần còn lại chưa ai trả — nên bảng lương về 0 mà sổ
 * cũ vẫn còn đủ.
 *
 * Nhớ theo id chứ không theo khoảng ngày: chấm bù một ngày của kỳ đã chốt thì buổi đó
 * chưa được trả tiền, phải rơi vào kỳ đang mở. Nếu cắt theo ngày thì nó lọt ra ngoài cả
 * hai kỳ và thợ mất công.
 */
export interface KyLuong {
  id: string;
  /** Ngày sớm nhất có trong kỳ, chỉ để hiện lên màn hình cho dễ gọi tên. */
  tuNgay: string;
  /** Ngày chốt kỳ. */
  denNgay: string;
  /** Lúc bấm quyết toán, dạng ISO. Kỳ xếp theo thứ tự chốt chứ không theo ngày. */
  chotLuc: string;
  ghiChu: string;
  dongs: DongQuyetToan[];
  buoiCongIds: string[];
  ungTienIds: string[];
}

/** Toàn bộ dữ liệu chấm công, được lưu thành một khối JSON. */
export interface DuLieuChamCong {
  thos: Tho[];
  buoiCongs: BuoiCong[];
  ungTiens: UngTien[];
  /** Các kỳ đã quyết toán, xếp theo thứ tự chốt — kỳ mới nhất nằm cuối. */
  kyLuongs: KyLuong[];
}

export function duLieuRong(): DuLieuChamCong {
  return { thos: [], buoiCongs: [], ungTiens: [], kyLuongs: [] };
}
