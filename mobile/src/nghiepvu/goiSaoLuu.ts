/**
 * Đóng gói toàn bộ dữ liệu chấm công thành một file JSON để ghi xuống máy hay gửi đi, và mở
 * gói ấy ra lúc khôi phục.
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

/** Nhãn nhận dạng file của app này. Đừng đổi — các bản sao lưu cũ vẫn mang nhãn cũ. */
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
 * Tên file sao lưu: mỗi ngày một file, ví dụ "Cham-cong-2026-08-05.json".
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

/**
 * Tên các bản cần xoá để chỉ còn `soGiu` bản mới nhất.
 *
 * Tách ra làm hàm thuần, không chạm vào thư mục, để kiểm thử được: đây là chỗ *chọn cái gì
 * để xoá*, mà chọn sai thì mất bản sao lưu — không phải chỗ để đoán rồi tin.
 *
 * File nào không đúng khuôn tên thì không xoá. Thư mục ấy là của app, nhưng lỡ có file lạ
 * nằm đó thì cũng không phải việc của mình đi dọn.
 */
export function banCanXoa(tens: string[], soGiu: number): string[] {
  const ngays = tens.map(ngayTuTenFile).filter((ngay): ngay is string => ngay !== null);
  const canXoa = new Set(ngayCanXoa(ngays, soGiu));

  return (
    tens
      .filter((ten) => {
        const ngay = ngayTuTenFile(ten);
        return ngay !== null && canXoa.has(ngay);
      })
      // Trả về mới nhất trước, không theo thứ tự hệ điều hành liệt kê thư mục: bên gọi xoá
      // theo danh sách này, mà một danh sách có thứ tự thì đọc log ra còn hiểu được.
      .sort((a, b) => b.localeCompare(a))
  );
}

/**
 * Ngày của các bản cần xoá để chỉ còn `soGiu` bản mới nhất — cùng một phép chọn như
 * `banCanXoa`, nhưng tính trên **ngày** chứ trên tên file.
 *
 * Bản trên tài khoản không có tên file: mỗi ngày một hàng trong bảng `sao_luu`. Hai đường sao
 * lưu dùng chung đúng một phép chọn ở đây, không phải hai đoạn `sort` rồi `slice` viết hai
 * lần — chọn sai là xoá mất bản sao lưu, mà lỗi ấy chỉ hiện ra đúng lúc người dùng cần quay
 * về.
 */
export function ngayCanXoa(ngays: string[], soGiu: number): string[] {
  const giu = new Set(ngayCanGiu(ngays, soGiu));
  return [...new Set(ngays)].filter((ngay) => !giu.has(ngay)).sort((a, b) => b.localeCompare(a));
}

/** Những ngày được giữ lại: `soGiu` ngày mới nhất, không trùng nhau. */
function ngayCanGiu(ngays: string[], soGiu: number): string[] {
  return (
    [...new Set(ngays)]
      // Ngày kiểu yyyy-MM-dd nên so chuỗi là đúng thứ tự thời gian, mới nhất trước.
      .sort((a, b) => b.localeCompare(a))
      .slice(0, soGiu)
  );
}

/**
 * Gói dữ liệu lại, **chưa đổi thành chuỗi**.
 *
 * Tách ra vì bản sao lưu lên tài khoản đi vào một cột `jsonb`, không đi vào file: nó cần đúng
 * cái gói này dưới dạng object. Đóng gói ở hai chỗ theo hai kiểu thì sớm muộn hai bên mang
 * hai cấu trúc khác nhau, mà bên đọc lại chỉ có một hàm kiểm.
 */
export function goiTu(duLieu: DuLieuChamCong, taoLuc: string): GoiSaoLuu {
  return { app: NHAN_APP, phienBan: PHIEN_BAN, taoLuc, duLieu };
}

export function dongGoi(duLieu: DuLieuChamCong, taoLuc: string): string {
  // Xuống dòng cho dễ đọc khi mở file ra bằng mắt; file vài trăm KB là cùng.
  return JSON.stringify(goiTu(duLieu, taoLuc), null, 2);
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

  return docGoi(daDoc);
}

/**
 * Mở gói đã là object sẵn — bản lấy từ cột `jsonb` trên tài khoản.
 *
 * **Dữ liệu từ database cũng là dữ liệu từ ngoài vào**, không được tin sẵn chỉ vì nó đến từ
 * Postgres: cùng một tài khoản có thể vừa chạy bản app cũ vừa chạy bản mới, và hàng ấy sửa
 * bằng tay trong SQL Editor được. Nên nó đi qua đúng bộ kiểm của file sao lưu, kể cả chỗ từ
 * chối gói của bản app mới hơn.
 */
export function docGoi(daDoc: unknown): GoiSaoLuu {
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
