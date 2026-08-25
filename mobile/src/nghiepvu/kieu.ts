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

/**
 * Ghi chú cho **một ngày của một thợ** — chỗ ghi *vì sao* hôm ấy chấm như thế: "về sớm đi
 * đám cưới", "làm bù hôm mưa", "nghỉ đau chân".
 *
 * Để riêng chứ không dùng `BuoiCong.ghiChu` vì hai lý do:
 *
 * - Ghi chú nói về **cả ngày**, không phải về một buổi. Nhét vào buổi thì phải chép đôi
 *   sang cả sáng lẫn chiều, rồi sửa một bên là hai bên nói khác nhau.
 * - Ngày **nghỉ hẳn** không có buổi công nào để mà treo ghi chú vào, mà đó lại đúng là
 *   ngày cần ghi chú nhất. Cũng vì thế mà bỏ chấm một buổi không được kéo ghi chú đi theo.
 *
 * Không có `id`: khoá là cặp (thợ, ngày), mỗi cặp nhiều nhất một ghi chú. Không có ai trỏ
 * vào bản ghi này — kỳ lương chỉ nhớ id của buổi công và ứng tiền — nên thêm id chỉ là
 * thêm một chỗ để trùng.
 */
export interface GhiChuNgay {
  thoId: string;
  ngay: string;
  /** Luôn khác chuỗi rỗng: xoá hết chữ là xoá luôn bản ghi, xem `datGhiChuNgay`. */
  noiDung: string;
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
  /** Ghi chú của từng (thợ, ngày). Không có bản ghi nghĩa là ngày ấy không ghi gì. */
  ghiChuNgays: GhiChuNgay[];
  /** Các kỳ đã quyết toán, xếp theo thứ tự chốt — kỳ mới nhất nằm cuối. */
  kyLuongs: KyLuong[];
}

export function duLieuRong(): DuLieuChamCong {
  return { thos: [], buoiCongs: [], ungTiens: [], ghiChuNgays: [], kyLuongs: [] };
}

/**
 * Sổ chưa có gì cả — máy vừa cài, hoặc vừa xoá app cài lại.
 *
 * Dùng để phân biệt hai chuyện rất khác nhau: *máy này chưa có sổ* thì bản sao lưu trên tài
 * khoản là thứ đáng lấy về, còn *máy này đã có sổ* thì bản trên tài khoản chỉ là bản cũ của
 * chính nó. Đẩy một sổ trống lên tài khoản là xoá bản đang có ở đó, nên chỗ này phải đếm đủ
 * mọi loại bản ghi, đừng chỉ đếm thợ: sổ có mỗi mấy buổi công mà không có thợ nào vẫn là sổ
 * có dữ liệu.
 */
export function soTrong(duLieu: DuLieuChamCong): boolean {
  return (
    duLieu.thos.length === 0 &&
    duLieu.buoiCongs.length === 0 &&
    duLieu.ungTiens.length === 0 &&
    duLieu.ghiChuNgays.length === 0 &&
    duLieu.kyLuongs.length === 0
  );
}

/** Dáng cũ của Tho: một mức tiền công duy nhất, chưa có lịch sử. */
interface ThoBanCu extends Partial<Tho> {
  tienMotCong?: number;
}

/**
 * Thợ bản cũ chỉ có một mức `tienMotCong` — biến nó thành mốc lương đầu tiên, tính từ
 * ngày thêm thợ, để mọi buổi công cũ vẫn tính ra đúng số tiền như trước.
 */
function chuyenDoiTho(tho: ThoBanCu): Tho {
  const mocLuong =
    tho.mocLuong && tho.mocLuong.length > 0
      ? tho.mocLuong
      : [{ tuNgay: tho.ngayTao ?? '2000-01-01', tienMotCong: tho.tienMotCong ?? 0 }];

  return {
    id: tho.id ?? '',
    ten: tho.ten ?? '',
    dienThoai: tho.dienThoai ?? '',
    mocLuong,
    dangLam: tho.dangLam ?? true,
    ghiChu: tho.ghiChu ?? '',
    ngayTao: tho.ngayTao ?? '2000-01-01',
    suaLuc: tho.suaLuc ?? new Date().toISOString(),
  };
}

/**
 * Vá dữ liệu đọc từ ngoài vào cho đủ hình đủ dạng, và chuyển các dáng cũ sang dáng mới.
 *
 * Dùng chung cho cả hai đường vào: đọc từ bộ nhớ máy, và khôi phục từ một file sao lưu.
 * Hai đường ấy phải chuyển đổi y hệt nhau — tách ra làm hai bản thì sớm muộn một
 * bên quên vá một chỗ, và bản khôi phục về sẽ khác bản đã sao lưu đi.
 *
 * Nằm ở đây, cạnh các kiểu dữ liệu, chứ không nằm trong luuTru: thêm một mảng mới vào
 * `DuLieuChamCong` là phải vá thêm ở đây, để hai chỗ cạnh nhau thì khó quên.
 */
export function chuanHoa(daDoc: unknown): DuLieuChamCong {
  const khoi = (daDoc ?? {}) as Partial<DuLieuChamCong> & { thos?: ThoBanCu[] };
  return {
    thos: (khoi.thos ?? []).map(chuyenDoiTho),
    buoiCongs: khoi.buoiCongs ?? [],
    ungTiens: khoi.ungTiens ?? [],
    // Bản trước chưa có ghi chú ngày: sổ cũ không mất gì, chỉ là chưa ai ghi chú.
    ghiChuNgays: khoi.ghiChuNgays ?? [],
    // Máy đã cài bản trước chưa có quyết toán: coi như chưa chốt kỳ nào, mọi thứ đang
    // nằm trong kỳ đầu tiên. Không mất gì cả.
    kyLuongs: khoi.kyLuongs ?? [],
  };
}
