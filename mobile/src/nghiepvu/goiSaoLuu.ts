/**
 * Đóng gói toàn bộ dữ liệu chấm công thành một file JSON để gửi lên Drive, và mở gói
 * ấy ra lúc khôi phục.
 *
 * Khác hẳn file Excel: file Excel là để *người* đọc, cắt sẵn theo kỳ, làm tròn, bỏ id —
 * nạp ngược lại không ra được dữ liệu cũ. File ở đây là bản chụp nguyên xi, xấu mã nhưng
 * khôi phục về là y như lúc sao lưu.
 *
 * Có `phienBan` trong gói để sau này đổi cấu trúc còn biết đường chuyển đổi, và có `app`
 * để lỡ người dùng chọn nhầm file JSON nào đó thì báo được ngay chứ không nuốt bừa rồi
 * xoá sạch dữ liệu đang có.
 */

import { chuanHoa, DuLieuChamCong } from './kieu';

/** Nhãn nhận dạng file của app này. Đừng đổi — file cũ trên Drive vẫn mang nhãn cũ. */
const NHAN_APP = 'cham-cong';

/** Phiên bản cấu trúc gói hiện tại. */
export const PHIEN_BAN = 1;

export interface GoiSaoLuu {
  app: string;
  phienBan: number;
  /** Lúc bấm sao lưu, dạng ISO. */
  taoLuc: string;
  duLieu: DuLieuChamCong;
}

/** File chọn để khôi phục không phải bản sao lưu của app này, hoặc đã hỏng. */
export class GoiHong extends Error {
  constructor(lyDo: string) {
    super(lyDo);
  }
}

/** Vài con số để hiện lên trước khi người dùng bấm khôi phục, cho họ biết mình sắp nhận gì. */
export interface TomTat {
  soTho: number;
  soBuoiCong: number;
  soUngTien: number;
  soKy: number;
}

export function tomTat(duLieu: DuLieuChamCong): TomTat {
  return {
    soTho: duLieu.thos.length,
    soBuoiCong: duLieu.buoiCongs.length,
    soUngTien: duLieu.ungTiens.length,
    soKy: duLieu.kyLuongs.length,
  };
}

/**
 * Tên file trên Drive: mỗi ngày một file, ví dụ "Cham-cong-2026-08-05.json".
 *
 * Ngày để dạng yyyy-MM-dd chứ không dd-MM-yyyy như file Excel: ở đây tên file còn để
 * *sắp xếp*, mà chỉ kiểu yyyy trước mới sắp đúng thứ tự thời gian.
 */
export function tenFileSaoLuu(ngay: string): string {
  return `Cham-cong-${ngay}.json`;
}

/** Lấy lại ngày từ tên file. Không đúng khuôn thì trả null — file lạ, đừng đoán. */
export function ngayTuTenFile(ten: string): string | null {
  const khop = /^Cham-cong-(\d{4}-\d{2}-\d{2})\.json$/.exec(ten);
  return khop ? khop[1] : null;
}

export function dongGoi(duLieu: DuLieuChamCong, taoLuc: string): string {
  const goi: GoiSaoLuu = { app: NHAN_APP, phienBan: PHIEN_BAN, taoLuc, duLieu };
  // Xuống dòng cho dễ đọc khi mở bằng mắt trên Drive; file vài trăm KB là cùng.
  return JSON.stringify(goi, null, 2);
}

/**
 * Mở gói, kiểm tra kỹ rồi mới trả dữ liệu.
 *
 * Khôi phục là thao tác *ghi đè*: nhận nhầm một file rác mà cứ thế nuốt thì mất sạch sổ
 * sách. Nên ở đây thà từ chối oan còn hơn nhận bừa — mọi thứ không đúng khuôn đều ném lỗi.
 */
export function moGoi(noiDung: string): GoiSaoLuu {
  let daDoc: unknown;
  try {
    daDoc = JSON.parse(noiDung);
  } catch {
    throw new GoiHong('File này không phải JSON đọc được.');
  }

  if (typeof daDoc !== 'object' || daDoc === null || Array.isArray(daDoc)) {
    throw new GoiHong('File này không phải bản sao lưu chấm công.');
  }

  const goi = daDoc as Partial<GoiSaoLuu>;
  if (goi.app !== NHAN_APP) {
    throw new GoiHong('File này không phải bản sao lưu chấm công.');
  }

  // Gói của bản app mới hơn thì cấu trúc có thể đã khác, nuốt vào là hỏng dữ liệu.
  if (typeof goi.phienBan !== 'number' || goi.phienBan > PHIEN_BAN) {
    throw new GoiHong('Bản sao lưu này của phiên bản app mới hơn. Hãy cập nhật app rồi thử lại.');
  }

  if (typeof goi.duLieu !== 'object' || goi.duLieu === null) {
    throw new GoiHong('Bản sao lưu thiếu phần dữ liệu.');
  }

  return {
    app: NHAN_APP,
    phienBan: goi.phienBan,
    taoLuc: typeof goi.taoLuc === 'string' ? goi.taoLuc : '',
    duLieu: chuanHoa(goi.duLieu),
  };
}
