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
 * Một buổi đi làm đầy đủ là bằng này công — **một ngày đi đủ cả sáng lẫn chiều là một
 * công**, không phải hai.
 *
 * Đó là cách cả nghề nói và cũng là cách tiền được tính: `Tho.mocLuong.tienMotCong` là
 * tiền của **một ngày công** (300.000 đ một công), nên đếm mỗi buổi một công thì cuối kỳ
 * thợ nào cũng thành tiền gấp đôi. Sổ vẫn ghi theo *buổi* vì buổi mới là thứ được chấm —
 * chỉ có giá trị của một buổi là nửa công.
 */
export const CONG_MOT_BUOI = 0.5;

/**
 * Bản luật đang dùng để đếm công, ghi kèm trong sổ để biết sổ đọc lên viết theo luật nào.
 *
 * Không có, hay bằng 1: sổ viết hồi mỗi buổi còn tính **một** công, tức một ngày hai công.
 * Bằng 2: một ngày đi đủ là một công, xem `CONG_MOT_BUOI`.
 */
export const BAN_LUAT_CONG = 2;

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
  /** Bình thường là `CONG_MOT_BUOI` (0,5). Về sớm thì 0,25; làm thêm thì 0,75. */
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
  /**
   * Sổ này đếm công theo bản luật nào. Xem `BAN_LUAT_CONG`. Để trống nghĩa là sổ cũ,
   * `chuanHoa` sẽ đổi sang luật mới rồi điền vào.
   */
  banLuatCong?: number;
}

export function duLieuRong(): DuLieuChamCong {
  return {
    thos: [],
    buoiCongs: [],
    ungTiens: [],
    ghiChuNgays: [],
    kyLuongs: [],
    banLuatCong: BAN_LUAT_CONG,
  };
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
  const kyLuongs = khoi.kyLuongs ?? [];
  return {
    thos: (khoi.thos ?? []).map(chuyenDoiTho),
    buoiCongs: doiSangLuatMoi(khoi.buoiCongs ?? [], kyLuongs, khoi.banLuatCong),
    ungTiens: khoi.ungTiens ?? [],
    // Bản trước chưa có ghi chú ngày: sổ cũ không mất gì, chỉ là chưa ai ghi chú.
    ghiChuNgays: khoi.ghiChuNgays ?? [],
    // Máy đã cài bản trước chưa có quyết toán: coi như chưa chốt kỳ nào, mọi thứ đang
    // nằm trong kỳ đầu tiên. Không mất gì cả.
    kyLuongs,
    banLuatCong: BAN_LUAT_CONG,
  };
}

/**
 * Chuyển buổi công của sổ cũ sang luật *một ngày một công*: chia đôi số công đã ghi.
 *
 * **Chỉ đụng vào buổi chưa nằm trong kỳ đã chốt.** Buổi đã chốt là bản ghi của một lần đã
 * trả tiền: `KyLuong.dongs` chụp lại tổng công và số tiền của lúc ấy và không tính lại bao
 * giờ nữa, nên chia đôi buổi cũ chỉ làm sổ nói khác tờ quyết toán đã in đưa thợ, mà đồng
 * tiền thì đã sang tay rồi. Phần chưa chốt thì ngược lại: nó còn đang được nhân với
 * `tienMotCong` để ra bảng lương kỳ này, nên phải sang luật mới cùng với những buổi sắp
 * chấm — không thì nửa kỳ tính gấp đôi nửa kỳ kia.
 *
 * Cờ `banLuatCong` khiến việc này chỉ chạy đúng một lần: sổ đã đổi rồi thì lần mở app sau
 * không chia đôi lần nữa. Mà chuyển là đọc-ghi cả sổ nên bản sao lưu cũ khôi phục về vẫn
 * được đổi đúng, còn bản mới sao lưu ra đã mang cờ mới thì để nguyên.
 */
function doiSangLuatMoi(
  buoiCongs: BuoiCong[],
  kyLuongs: KyLuong[],
  banLuatCu: number | undefined,
): BuoiCong[] {
  if (banLuatCu === BAN_LUAT_CONG) {
    return buoiCongs;
  }

  const daChot = new Set(kyLuongs.flatMap((ky) => ky.buoiCongIds ?? []));
  return buoiCongs.map((buoi) =>
    daChot.has(buoi.id) ? buoi : { ...buoi, soCong: buoi.soCong / 2 },
  );
}
