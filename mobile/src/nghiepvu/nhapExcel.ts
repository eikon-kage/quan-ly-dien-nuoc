/**
 * Nhập công của **một thợ** từ file Excel.
 *
 * Vì sao có cái này: chấm công trong app là bấm từng buổi từng ngày — nhanh khi chấm
 * hằng ngày, nhưng chậm phát khóc khi nhập bù cả tháng cũ, hoặc khi chủ đã có sẵn một
 * bảng công gõ trên máy tính. Nhập từ file là đường tắt cho hai việc ấy.
 *
 * **Mỗi file một thợ.** File không ghi tên thợ mà người dùng chọn thợ ngay trong app
 * trước khi nhập. Cố ghi tên thợ vào file thì phải dò tên — gõ "A.Tuấn" hay "Tuấn (thợ
 * nề)" là dò trượt, mà dò trượt thì công rơi vào tay người khác. Chọn trong app thì
 * không bao giờ nhầm người.
 *
 * **Một dòng một ngày**, hai cột Sáng và Chiều — đúng như màn hình chấm công, và điền
 * nhanh hơn hẳn kiểu mỗi buổi một dòng.
 *
 * Ba mức của một ô công, phân biệt rõ vì đây là chỗ dễ mất dữ liệu nhất:
 *   - **Để trống** — không đụng tới buổi ấy. Nhập file chỉ có tuần này thì tuần trước
 *     trong máy vẫn nguyên.
 *   - **0, "n", "nghỉ", "-"** — nói rõ là nghỉ, buổi đã chấm trong máy sẽ bị bỏ chấm.
 *   - **Số công, hoặc "x"** — chấm buổi ấy.
 *
 * Đọc và dựng file mẫu để chung một file: hai việc phải dùng **đúng một bộ cột**, tách
 * ra hai nơi thì sớm muộn sửa một bên quên bên kia, và người dùng điền theo file mẫu
 * xong app lại không đọc ra.
 */

import { docFileExcel, timTrang, TrangDaDoc } from './docXlsx';
import { BuoiLam, DuLieuChamCong } from './kieu';
import { banGhiChuaChot } from './ky';
import * as Ngay from './ngayViet';
import { CONG_TOI_DA, docSoCong, docTien } from './nhapSo';
import { boCham, cham, dangCham, datGhiChuNgay, ghiChuNgay, themUng } from './thaoTac';
import { Cot, O, TrangTinh, taoFileExcel } from './xlsx';

/** Tên trang chứa dữ liệu trong file mẫu. */
export const TEN_TRANG_NHAP = 'Chấm công';

/** Tên trang hướng dẫn — trang này chỉ để đọc, app không lấy gì ở đó. */
const TEN_TRANG_HUONG_DAN = 'Hướng dẫn';

// ---------- Bộ cột, dùng chung cho cả lúc dựng file mẫu lẫn lúc đọc ----------

/**
 * Các cột của trang Chấm công, đúng thứ tự trong file mẫu.
 *
 * `coUngTien` là **false trên máy thợ**, và đó không phải chuyện gọn gàng giao diện: cả app
 * trên máy thợ không biết một đồng nào (xem `ketNap` ở vaiMay). Có cột Ứng tiền trong file
 * mẫu của thợ là mời người ta điền vào một con số mà máy này không có đường nào gửi lên cho
 * chủ — `SoCong` cắt tiền ra từ lúc đóng gói — nên tiền ấy nằm im rồi mất. Thà không có cột.
 */
function cotNhap(coUngTien: boolean): Cot[] {
  return [
    { nhan: 'Ngày', rong: 12, kieu: 'ngay' },
    { nhan: 'Thứ', rong: 10, kieu: 'chu' },
    { nhan: 'Sáng', rong: 9, kieu: 'so' },
    { nhan: 'Chiều', rong: 9, kieu: 'so' },
    ...(coUngTien ? ([{ nhan: 'Ứng tiền', rong: 14, kieu: 'tien' }] as Cot[]) : []),
    { nhan: 'Ghi chú', rong: 30, kieu: 'chu' },
  ];
}

/**
 * Tên khác của cùng một cột, để nhận ra cả những file người dùng tự gõ. Viết không dấu,
 * chữ thường — chuỗi tiêu đề đọc lên sẽ được bỏ dấu trước khi so.
 */
const TEN_COT: Record<TenCot, string[]> = {
  ngay: ['ngay', 'ngay thang', 'date'],
  sang: ['sang', 'buoi sang', 'ca sang'],
  chieu: ['chieu', 'buoi chieu', 'ca chieu'],
  ung: ['ung tien', 'ung', 'tam ung', 'so tien ung'],
  ghiChu: ['ghi chu', 'chu thich', 'note'],
};

type TenCot = 'ngay' | 'sang' | 'chieu' | 'ung' | 'ghiChu';

/** Vị trí (đếm từ 0) của từng cột trong file người dùng đưa vào. */
type ViTriCot = Partial<Record<TenCot, number>>;

// ---------- Đọc chữ người dùng gõ ----------

const CO_DAU = 'àáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ';
const KHONG_DAU = 'aaaaaaaaaaaaaaaaadeeeeeeeeeeeiiiiiooooooooooooooooouuuuuuuuuuuyyyyy';

/**
 * Bỏ dấu tiếng Việt, hạ chữ thường, gộp khoảng trắng — để so tên cột cho lỏng tay.
 *
 * Tự thay từng chữ chứ không dùng `normalize('NFD')`: máy Android chạy bằng Hermes,
 * và bảng Unicode đầy đủ không phải bản Hermes nào cũng có.
 */
export function khongDau(chu: string): string {
  let ra = '';
  for (const ky of chu.toLowerCase()) {
    const cho = CO_DAU.indexOf(ky);
    ra += cho === -1 ? ky : KHONG_DAU[cho];
  }
  return ra.replace(/\s+/g, ' ').trim();
}

/** Một ô về dạng chữ đã cắt hai đầu. Ô trống trả về chuỗi rỗng. */
function chu(o: O | undefined): string {
  if (o === null || o === undefined) {
    return '';
  }
  return String(o).trim();
}

/**
 * Ngày trong một ô. Excel để ngày thành số đếm từ 30/12/1899; người gõ tay thì ra chữ
 * "05/08/2026" hoặc "2026-08-05". Nhận cả ba.
 */
export function docNgay(o: O | undefined): string | null {
  if (typeof o === 'number') {
    // Ngược của `soNgayExcel`. Chặn hai đầu cho khỏi nhận nhầm một con số bất kỳ.
    if (!Number.isFinite(o) || o < 1 || o > 2958465) {
      return null;
    }
    const moc = new Date(Math.round((o - 25569) * 86400000));
    return hopLe(
      Ngay.ghep(moc.getUTCFullYear(), moc.getUTCMonth() + 1, moc.getUTCDate()),
    );
  }

  const sach = chu(o);
  if (sach === '') {
    return null;
  }

  // "2026-08-05"
  const gapIso = /^(\d{4})[-/](\d{1,2})[-/](\d{1,2})$/.exec(sach);
  if (gapIso !== null) {
    return hopLe(Ngay.ghep(Number(gapIso[1]), Number(gapIso[2]), Number(gapIso[3])));
  }

  // "05/08/2026", "5-8-2026", "5.8.2026" — ngày trước tháng sau, kiểu người Việt viết.
  const gapViet = /^(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})$/.exec(sach);
  if (gapViet !== null) {
    return hopLe(Ngay.ghep(Number(gapViet[3]), Number(gapViet[2]), Number(gapViet[1])));
  }

  return null;
}

/**
 * Chặn ngày vô nghĩa: 31/02 sẽ bị `Date` đẩy sang 03/03, mà đẩy âm thầm thì công rơi
 * nhầm ngày. Chặn luôn năm quá xa — gõ nhầm "2062" là thấy ngay chứ không lặng lẽ nhận.
 */
function hopLe(ngay: string): string | null {
  const { nam, thang, ngay: n } = Ngay.tach(ngay);
  if (nam < 2000 || nam > 2100 || thang < 1 || thang > 12) {
    return null;
  }
  if (n < 1 || n > Ngay.soNgayTrongThang(nam, thang)) {
    return null;
  }
  return ngay;
}

/** Ô công không có gì để đọc — cả dòng bỏ trống hoặc chỉ có mỗi ghi chú. */
const KHONG_DUNG_TOI = Symbol('khong dung toi');

const CHU_LA_CONG = ['x', 'v', 'co', 'di', 'lam'];
const CHU_LA_NGHI = ['n', 'nghi', '-', 'khong', 'off'];

/**
 * Ô công: trả về `KHONG_DUNG_TOI` khi để trống, 0 khi nghỉ, số công khi có đi làm,
 * hoặc câu lỗi khi không đọc ra.
 */
function docCong(o: O | undefined): number | typeof KHONG_DUNG_TOI | { loi: string } {
  if (o === null || o === undefined || chu(o) === '') {
    return KHONG_DUNG_TOI;
  }

  if (typeof o === 'number') {
    if (o === 0) {
      return 0;
    }
    if (o < 0 || o > CONG_TOI_DA) {
      return { loi: `số công phải từ 0 tới ${CONG_TOI_DA}` };
    }
    return Math.round(o * 100) / 100;
  }

  const sach = khongDau(chu(o));
  if (CHU_LA_CONG.includes(sach)) {
    return 1;
  }
  if (CHU_LA_NGHI.includes(sach)) {
    return 0;
  }

  const so = docSoCong(sach);
  if (so === null) {
    return { loi: `không hiểu ô công "${chu(o)}"` };
  }
  if (so > CONG_TOI_DA) {
    return { loi: `số công phải từ 0 tới ${CONG_TOI_DA}` };
  }
  return so;
}

// ---------- Đọc cả trang ----------

/** Một dòng đã đọc được, sẵn sàng ghi vào sổ. */
export interface DongNhap {
  /** Số dòng trong file Excel, để nói cho người dùng biết lỗi nằm ở đâu. */
  soDong: number;
  ngay: string;
  /** null nghĩa là ô để trống — buổi ấy trong máy giữ nguyên. 0 là nghỉ. */
  congSang: number | null;
  congChieu: number | null;
  /** Tiền ứng trong ngày, không có thì null. */
  ung: number | null;
  ghiChu: string;
}

export interface LoiDong {
  soDong: number;
  ly: string;
}

/** Kết quả đọc file: những dòng dùng được, và những dòng phải bỏ qua kèm lý do. */
export interface BanNhap {
  dongs: DongNhap[];
  lois: LoiDong[];
}

/** File đúng là .xlsx nhưng bên trong không phải bảng chấm công. */
export class FileKhongDungMau extends Error {}

/**
 * Tìm dòng tiêu đề rồi ghi nhớ cột nào nằm ở đâu.
 *
 * Dò theo **tên cột** chứ không đếm theo vị trí: người dùng hay chèn thêm cột của riêng
 * họ (số điện thoại, tên công trình) vào giữa bảng. Đếm vị trí thì thêm một cột là lệch
 * hết, còn dò tên thì thừa bao nhiêu cột cũng không sao.
 */
function timTieuDe(trang: TrangDaDoc): { viTri: ViTriCot; sauDong: number } {
  for (const dong of trang.dongs.slice(0, 20)) {
    const viTri: ViTriCot = {};
    dong.o.forEach((o, cot) => {
      const ten = khongDau(chu(o));
      if (ten === '') {
        return;
      }
      for (const ma of Object.keys(TEN_COT) as TenCot[]) {
        if (viTri[ma] === undefined && TEN_COT[ma].includes(ten)) {
          viTri[ma] = cot;
        }
      }
    });

    // Phải có cột Ngày và ít nhất một cột số liệu, kẻo nhận nhầm một dòng chữ bất kỳ
    // có chữ "ngày" làm dòng tiêu đề.
    if (
      viTri.ngay !== undefined &&
      (viTri.sang !== undefined || viTri.chieu !== undefined || viTri.ung !== undefined)
    ) {
      return { viTri, sauDong: dong.so };
    }
  }

  throw new FileKhongDungMau(
    'Không thấy dòng tiêu đề có các cột Ngày, Sáng, Chiều. Dùng file mẫu của app cho chắc.',
  );
}

/** Đọc một trang tính thành các dòng nhập. */
export function docTrangNhap(trang: TrangDaDoc): BanNhap {
  const { viTri, sauDong } = timTieuDe(trang);

  const dongs: DongNhap[] = [];
  const lois: LoiDong[] = [];

  for (const dong of trang.dongs) {
    if (dong.so <= sauDong) {
      continue;
    }

    const oNgay = viTri.ngay === undefined ? null : dong.o[viTri.ngay];
    const ngay = docNgay(oNgay);

    const sang = docCong(viTri.sang === undefined ? null : dong.o[viTri.sang]);
    const chieu = docCong(viTri.chieu === undefined ? null : dong.o[viTri.chieu]);
    const oUng = viTri.ung === undefined ? null : dong.o[viTri.ung];
    const ghiChu = viTri.ghiChu === undefined ? '' : chu(dong.o[viTri.ghiChu]);

    const trongCong = sang === KHONG_DUNG_TOI && chieu === KHONG_DUNG_TOI;
    const trongUng = chu(oUng) === '';

    if (ngay === null) {
      // Dòng trống hẳn, hoặc dòng "Tổng cộng" ở cuối bảng: bỏ im, không kêu ca.
      if (!trongCong || !trongUng) {
        lois.push({
          soDong: dong.so,
          ly: chu(oNgay) === '' ? 'thiếu ngày' : `ngày "${chu(oNgay)}" không đọc được`,
        });
      }
      continue;
    }

    if (typeof sang === 'object') {
      lois.push({ soDong: dong.so, ly: `cột Sáng: ${sang.loi}` });
      continue;
    }
    if (typeof chieu === 'object') {
      lois.push({ soDong: dong.so, ly: `cột Chiều: ${chieu.loi}` });
      continue;
    }

    let ung: number | null = null;
    if (!trongUng) {
      const soTien = typeof oUng === 'number' ? Math.round(oUng) : docTien(chu(oUng));
      if (soTien === null || soTien < 0) {
        lois.push({ soDong: dong.so, ly: `tiền ứng "${chu(oUng)}" không đọc được` });
        continue;
      }
      ung = soTien === 0 ? null : soTien;
    }

    if (trongCong && ung === null) {
      // Dòng ngày có sẵn trong file mẫu mà chưa điền gì: không phải lỗi, chỉ là chưa dùng.
      continue;
    }

    dongs.push({
      soDong: dong.so,
      ngay,
      congSang: sang === KHONG_DUNG_TOI ? null : sang,
      congChieu: chieu === KHONG_DUNG_TOI ? null : chieu,
      ung,
      ghiChu,
    });
  }

  return { dongs, lois };
}

/** Đọc thẳng từ khối byte của file. */
export function docFileNhap(noiDung: Uint8Array): BanNhap {
  return docTrangNhap(timTrang(docFileExcel(noiDung), TEN_TRANG_NHAP));
}

// ---------- Ghi vào sổ ----------

export interface KetQuaGhi {
  duLieu: DuLieuChamCong;
  /** Buổi chưa có trong máy, nay thêm mới. */
  themBuoi: number;
  /** Buổi đã có sẵn, file ghi số công khác nên sửa lại. */
  suaBuoi: number;
  /** Buổi đã chấm trong máy mà file ghi là nghỉ, nên bỏ chấm. */
  boChamBuoi: number;
  /** Buổi đã nằm trong kỳ đã chốt: không đụng vào, chỉ báo lại. */
  boQuaDaChot: number;
  /** Ngày được ghi chú mới, hoặc ghi chú cũ bị file sửa lại. */
  ghiChuNgays: number;
  themUng: number;
  /** Lần ứng đã có y hệt trong máy (cùng thợ, cùng ngày, cùng số tiền): không cộng đôi. */
  boQuaUngTrung: number;
}

/**
 * Ghi các dòng đã đọc vào dữ liệu, cho một thợ.
 *
 * Hai điều giữ cho nhập lại cùng một file **không làm hỏng sổ**:
 *
 * 1. Buổi công vốn đã một-buổi-một-bản-ghi theo (thợ, ngày, buổi), nên nhập lại chỉ đè
 *    lên chính nó. Ứng tiền thì không có khoá như vậy, nên chỗ này tự bỏ qua lần ứng
 *    trùng khít cả ngày lẫn số tiền — nhập nhầm file hai lần không thành ứng gấp đôi.
 * 2. Buổi đã nằm trong kỳ đã chốt thì để nguyên. Tiền của kỳ ấy đã trả xong rồi, sửa số
 *    công cũ chỉ làm sổ đã chốt lệch với file Excel đã in ra đưa thợ.
 */
export function apDungNhap(
  duLieu: DuLieuChamCong,
  thoId: string,
  dongs: DongNhap[],
): KetQuaGhi {
  const chuaChot = new Set(banGhiChuaChot(duLieu).buoiCongs.map((b) => b.id));
  const daChot = new Set(
    duLieu.buoiCongs.filter((b) => !chuaChot.has(b.id)).map((b) => b.id),
  );

  let moi = duLieu;
  const ket: Omit<KetQuaGhi, 'duLieu'> = {
    themBuoi: 0,
    suaBuoi: 0,
    boChamBuoi: 0,
    boQuaDaChot: 0,
    ghiChuNgays: 0,
    themUng: 0,
    boQuaUngTrung: 0,
  };

  const ghiBuoi = (ngay: string, buoi: BuoiLam, soCong: number | null) => {
    if (soCong === null) {
      return;
    }

    const cu = dangCham(moi, thoId, ngay, buoi);
    if (cu !== undefined && daChot.has(cu.id)) {
      ket.boQuaDaChot += 1;
      return;
    }

    if (soCong === 0) {
      if (cu !== undefined) {
        moi = boCham(moi, thoId, ngay, buoi);
        ket.boChamBuoi += 1;
      }
      return;
    }

    if (cu !== undefined && cu.soCong === soCong) {
      return;
    }

    // Giữ nguyên ghi chú riêng của buổi (nếu buổi ấy vốn có): file Excel không có cột nào
    // nói về từng buổi, nên nhập lại file không được xoá chữ chỉ vì file không nhắc tới.
    moi = cham(moi, thoId, ngay, buoi, soCong, cu?.ghiChu ?? '');
    if (cu === undefined) {
      ket.themBuoi += 1;
    } else {
      ket.suaBuoi += 1;
    }
  };

  for (const dong of dongs) {
    ghiBuoi(dong.ngay, 'Sang', dong.congSang);
    ghiBuoi(dong.ngay, 'Chieu', dong.congChieu);

    /*
      Một dòng của file là một *ngày*, nên ô ghi chú của dòng là ghi chú của ngày ấy — ghi
      thẳng vào ghi chú ngày, không chép đôi sang cả hai buổi công như bản trước.

      Ô để trống thì giữ chữ cũ, đừng xoá mất ghi chú người ta đã gõ trong app: file Excel
      thường được xuất ra rồi sửa mấy con số công, mà cột ghi chú thì bỏ trắng.
    */
    if (dong.ghiChu !== '' && ghiChuNgay(moi, thoId, dong.ngay) !== dong.ghiChu.trim()) {
      moi = datGhiChuNgay(moi, thoId, dong.ngay, dong.ghiChu);
      ket.ghiChuNgays += 1;
    }

    if (dong.ung !== null) {
      const trung = moi.ungTiens.some(
        (u) => u.thoId === thoId && u.ngay === dong.ngay && u.soTien === dong.ung,
      );
      if (trung) {
        ket.boQuaUngTrung += 1;
      } else {
        moi = themUng(moi, thoId, dong.ngay, dong.ung, dong.ghiChu);
        ket.themUng += 1;
      }
    }
  }

  return { duLieu: moi, ...ket };
}

// ---------- File mẫu ----------

/**
 * Tên file mẫu, ví dụ "Mau-cham-cong-anh-tuan-08-2026.xlsx" cho một tháng và
 * "Mau-cham-cong-anh-tuan-2026.xlsx" cho cả năm. Không dấu cho khỏi lỗi khi gửi.
 *
 * Khoảng nào ra tên nào là chuyện phải phân biệt: hai file mẫu tải về cùng một thư mục mà
 * trùng tên thì cái sau đè cái trước, mà người ta lại vừa gõ nửa tháng vào cái trước.
 */
export function tenFileMau(tenTho: string, tuNgay: string, denNgay: string): string {
  const ten = khongDau(tenTho)
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
  const cua = `Mau-cham-cong-${ten === '' ? 'tho' : ten}`;

  const dau = Ngay.tach(tuNgay);
  const cuoi = Ngay.tach(denNgay);
  const hai = (so: number) => String(so).padStart(2, '0');
  const cuoiThang = cuoi.ngay === Ngay.soNgayTrongThang(cuoi.nam, cuoi.thang);

  if (dau.nam === cuoi.nam && dau.thang === 1 && dau.ngay === 1 && cuoi.thang === 12 && cuoiThang) {
    return `${cua}-${dau.nam}.xlsx`;
  }
  if (dau.nam === cuoi.nam && dau.thang === cuoi.thang && dau.ngay === 1 && cuoiThang) {
    return `${cua}-${hai(dau.thang)}-${dau.nam}.xlsx`;
  }
  return `${cua}-${tuNgay}-den-${denNgay}.xlsx`;
}

/** Dòng ngăn giữa hai tháng trong file cả năm, ví dụ "Tháng 09/2026". */
function nhanThang(nam: number, thang: number): string {
  return `Tháng ${String(thang).padStart(2, '0')}/${nam}`;
}

/** Các dòng hướng dẫn, viết như nói chuyện với người dùng chứ không phải tài liệu kỹ thuật. */
function trangHuongDan(
  tenTho: string,
  tuNgay: string,
  denNgay: string,
  coUngTien: boolean,
  nhieuThang: boolean,
): TrangTinh {
  const dongs: O[][] = [
    [`File này để nhập công cho thợ: ${tenTho}`],
    [`Khoảng ngày: ${Ngay.ngayGon(tuNgay)} — ${Ngay.ngayGon(denNgay)}`],
    [''],
    ['Cách điền: mở trang "Chấm công" ở tab bên cạnh, mỗi ngày một dòng.'],
  ];

  // File cả năm dài ba trăm mấy dòng: phải nói ngay hai điều, kẻo người ta mở ra thấy dài
  // quá rồi đóng lại — không cần điền hết, và mỗi tháng có một dòng để mà tìm.
  if (nhieuThang) {
    dongs.push(
      [''],
      ['File này có sẵn ngày của cả khoảng trên, không phải điền hết:'],
      ['  • Điền tháng nào thì app nhận tháng ấy, mấy tháng còn lại cứ để trống.'],
      ['  • Đầu mỗi tháng có một dòng ghi "Tháng 09/2026" cho dễ tìm.'],
      ['    Dòng ấy không phải một ngày, đừng điền công vào đó.'],
    );
  }

  dongs.push(
    [''],
    ['Cột Sáng và cột Chiều:'],
    ['  • Đi làm cả buổi thì điền 1, hoặc gõ chữ x.'],
    ['  • Làm nửa buổi thì điền 0,5.'],
    ['  • Nghỉ thì điền 0, hoặc gõ chữ n.'],
    ['  • ĐỂ TRỐNG nghĩa là "không đụng tới" — buổi ấy trong máy vẫn giữ nguyên.'],
    [''],
  );

  if (coUngTien) {
    dongs.push(['Cột Ứng tiền: số tiền thợ ứng hôm đó, ví dụ 500000. Không ứng thì để trống.']);
  }

  dongs.push(
    ['Cột Ghi chú: gõ gì cũng được, sẽ hiện lại trong app.'],
    [''],
    coUngTien
      ? ['Xong thì lưu file lại, mở app, vào mục Thợ rồi bấm "Nhập từ Excel".']
      : ['Xong thì lưu file lại, mở app rồi bấm "Nhập từ Excel".'],
    [''],
    ['Nhập lại đúng file này lần nữa cũng không sao: công không bị cộng đôi,'],
  );

  if (coUngTien) {
    dongs.push(
      ['tiền ứng trùng khít cũng chỉ tính một lần.'],
      ['Riêng những ngày đã quyết toán thì app không sửa — tiền đã trả rồi.'],
    );
  } else {
    // Máy thợ không có tiền nên cũng không có kỳ quyết toán để mà chừa ra. Nói thay điều
    // thợ cần biết: sổ này còn phải đi lên chủ, và chủ mới là bên chốt.
    dongs.push(
      ['buổi nào đã chấm rồi thì chỉ sửa lại số công.'],
      ['Công nhập xong sẽ tự gửi lên cho chủ ở lần đồng bộ sau.'],
    );
  }

  dongs.push(
    [''],
    ['Được phép thêm cột của riêng mình, hoặc xoá bớt dòng. App dò theo tên cột'],
    ['ở dòng tiêu đề chứ không đếm theo thứ tự.'],
  );

  return {
    ten: TEN_TRANG_HUONG_DAN,
    cots: [{ nhan: 'Hướng dẫn điền', rong: 78, kieu: 'chu' }],
    dongs,
  };
}

/**
 * File mẫu cho một thợ, đã điền sẵn cột Ngày và cột Thứ của cả khoảng — người dùng chỉ
 * việc gõ số vào hai cột Sáng, Chiều.
 *
 * Điền sẵn ngày chứ không để bảng trống: gõ tay ba mươi cái ngày là ba mươi cơ hội gõ
 * sai định dạng, mà sai định dạng thì app không đọc ra.
 *
 * Khoảng dài hơn một tháng thì chen thêm một **dòng tên tháng** trước mỗi tháng. File cả
 * năm là ba trăm sáu mươi mấy dòng ngày giống hệt nhau: không có mốc thì tìm tháng 9 phải
 * cuộn mà đoán. Dòng ấy không có ngày nên lúc đọc lại nó tự bị bỏ qua, không thành lỗi —
 * xem `docTrangNhap`.
 *
 * Vẫn **một trang duy nhất** dù có cả năm, không phải mười hai trang mỗi tháng một trang:
 * bộ đọc lấy đúng trang "Chấm công" (`timTrang`), chia ra mười hai trang là mười một tháng
 * người ta gõ xong mà app không đọc tới.
 */
export function cacTrangMau(
  tenTho: string,
  tuNgay: string,
  denNgay: string,
  coUngTien: boolean = true,
): TrangTinh[] {
  const cots = cotNhap(coUngTien);
  const cacThang = Ngay.cacThangTrongKhoang(tuNgay, denNgay);
  const nhieuThang = cacThang.length > 1;
  /** Mấy ô để trống sau cột Ngày và cột Thứ, đúng bằng số cột còn lại. */
  const conLai = cots.slice(2).map(() => null);

  const dongs: O[][] = [];
  for (const { nam, thang } of cacThang) {
    if (nhieuThang) {
      dongs.push([nhanThang(nam, thang), ...cots.slice(1).map(() => null)]);
    }

    const dauThang = Ngay.ghep(nam, thang, 1);
    const cuoiThang = Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang));
    const dau = dauThang > tuNgay ? dauThang : tuNgay;
    const cuoi = cuoiThang < denNgay ? cuoiThang : denNgay;

    for (let ngay = dau; ngay <= cuoi; ngay = Ngay.congNgay(ngay, 1)) {
      dongs.push([ngay, Ngay.thu(ngay), ...conLai]);
    }
  }

  return [
    { ten: TEN_TRANG_NHAP, cots, dongs },
    trangHuongDan(tenTho, tuNgay, denNgay, coUngTien, nhieuThang),
  ];
}

/** File mẫu thành khối byte của một file .xlsx. */
export function taoFileMau(
  tenTho: string,
  tuNgay: string,
  denNgay: string,
  coUngTien: boolean = true,
): Uint8Array {
  return taoFileExcel(cacTrangMau(tenTho, tuNgay, denNgay, coUngTien));
}

/** Cả tháng chứa ngày này — khoảng mặc định của file mẫu. */
export function khoangThang(trongThang: string): { tuNgay: string; denNgay: string } {
  const { nam, thang } = Ngay.tach(trongThang);
  return {
    tuNgay: Ngay.ghep(nam, thang, 1),
    denNgay: Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang)),
  };
}

/**
 * Cả năm chứa ngày này — khoảng của file mẫu cả năm.
 *
 * Có nó vì file một tháng chỉ đủ cho người chấm hằng tháng. Chủ chuyển từ sổ giấy sang app
 * giữa năm thì phải lấy file mẫu tám lần, đổi thợ tám lần, gửi tám file — mà tám file cùng
 * tên khác tháng nằm cạnh nhau trong Zalo là cơ hội gõ vào file tháng khác.
 *
 * Cả năm chứ không phải "từ đầu năm tới hôm nay": người ta còn chấm tiếp mấy tháng sau, mà
 * ngày chưa tới để trống thì app không đụng tới ngày ấy.
 */
export function khoangNam(trongNam: string): { tuNgay: string; denNgay: string } {
  const { nam } = Ngay.tach(trongNam);
  return { tuNgay: Ngay.ghep(nam, 1, 1), denNgay: Ngay.ghep(nam, 12, 31) };
}

/**
 * Bỏ tiền ứng khỏi những dòng đã đọc — dùng trên **máy thợ**.
 *
 * File mẫu của thợ vốn không có cột Ứng tiền, nhưng người ta vẫn có thể chọn đúng cái file
 * chủ đã gửi, hoặc tự gõ thêm cột ấy. Chặn ở đây chứ không trông vào việc file không có
 * cột: ứng tiền ghi vào máy thợ thì `SoCong` cắt tiền ra lúc đóng gói, chủ không bao giờ
 * thấy, mà thợ thì tưởng đã khai. Không nhận còn hơn nhận rồi để rơi mất.
 */
export function boUngTien(dongs: DongNhap[]): DongNhap[] {
  return dongs.map((dong) => (dong.ung === null ? dong : { ...dong, ung: null }));
}

/** Tóm tắt một lần nhập, viết thành câu để hiện lên màn hình. */
export function tomTat(ket: KetQuaGhi): string {
  const cau: string[] = [];
  if (ket.themBuoi > 0) {
    cau.push(`chấm mới ${ket.themBuoi} buổi`);
  }
  if (ket.suaBuoi > 0) {
    cau.push(`sửa ${ket.suaBuoi} buổi`);
  }
  if (ket.boChamBuoi > 0) {
    cau.push(`bỏ chấm ${ket.boChamBuoi} buổi`);
  }
  if (ket.ghiChuNgays > 0) {
    cau.push(`ghi chú cho ${ket.ghiChuNgays} ngày`);
  }
  if (ket.themUng > 0) {
    cau.push(`thêm ${ket.themUng} lần ứng tiền`);
  }
  return cau.length === 0 ? 'Không có gì thay đổi.' : `Đã ${cau.join(', ')}.`;
}

/** Vài con số để người dùng liếc qua trước khi bấm ghi vào sổ. */
export interface TomTatDoc {
  soNgay: number;
  /** Tổng số công sẽ chấm. */
  tongCong: number;
  /** Số buổi file ghi là nghỉ. */
  soNghi: number;
  tongUng: number;
  tuNgay: string;
  denNgay: string;
}

/**
 * Tóm tắt những gì đọc được, để hiện lên **trước khi** ghi.
 *
 * Xem trước rồi mới ghi chứ không ghi thẳng: nhập nhầm file của thợ khác thì cả tháng
 * công rơi vào tay người khác, mà nhìn con số tổng là thấy ngay không phải file mình cần.
 */
export function tomTatDoc(dongs: DongNhap[]): TomTatDoc {
  const ngays = dongs.map((d) => d.ngay).sort();
  const cong = (so: number | null) => (so !== null && so > 0 ? so : 0);
  const nghi = (so: number | null) => (so === 0 ? 1 : 0);

  return {
    soNgay: new Set(ngays).size,
    tongCong: dongs.reduce((t, d) => t + cong(d.congSang) + cong(d.congChieu), 0),
    soNghi: dongs.reduce((t, d) => t + nghi(d.congSang) + nghi(d.congChieu), 0),
    tongUng: dongs.reduce((t, d) => t + (d.ung ?? 0), 0),
    tuNgay: ngays[0] ?? '',
    denNgay: ngays[ngays.length - 1] ?? '',
  };
}
