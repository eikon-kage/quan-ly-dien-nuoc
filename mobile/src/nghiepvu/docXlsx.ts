/**
 * Đọc file Excel (.xlsx) — chiều ngược lại của [xlsx.ts](./xlsx.ts).
 *
 * Vẫn tự viết chứ không lấy thư viện đọc-ghi Excel có sẵn, vì đúng những lẽ đã ghi ở
 * `xlsx.ts`: thư viện nặng vài trăm KB, mà app chỉ cần lấy ra mấy ô chữ với ô số.
 *
 * Chỉ đọc đúng phần cần: tên trang, và giá trị từng ô. Không đọc màu, không đọc viền,
 * không tính công thức — ô công thức thì lấy **kết quả** Excel đã tính sẵn và ghi kèm
 * trong file.
 *
 * **Ngày trả về nguyên dạng số của Excel.** Trong file .xlsx, ô ngày là một con số đếm
 * từ 30/12/1899, còn "hiện thành 03/08/2026" là việc của định dạng nằm ở file khác. Đọc
 * cả bảng định dạng chỉ để phân biệt số với ngày thì tốn công, nên chỗ này trả về số y
 * nguyên và để bên gọi tự hiểu theo cột — cột *Ngày* thì số ấy là ngày, xem
 * [nhapExcel.ts](./nhapExcel.ts).
 */

import { unzipSync, strFromU8 } from 'fflate';

import { O } from './xlsx';

/** Một trang tính đã đọc ra. */
export interface TrangDaDoc {
  ten: string;
  dongs: DongDaDoc[];
}

/**
 * Một dòng của trang tính. Giữ `so` — số dòng thật trong Excel — để báo lỗi còn chỉ được
 * đúng chỗ: "dòng 14 ngày sai" thì người dùng mở file ra tìm được ngay.
 */
export interface DongDaDoc {
  so: number;
  o: O[];
}

/** File chọn nhầm, hoặc file .xlsx hỏng. */
export class KhongDocDuocFile extends Error {
  constructor(viSao: string) {
    super(viSao);
  }
}

// ---------- Mấy hàm nhỏ ----------

const THUC_THE: Record<string, string> = {
  amp: '&',
  lt: '<',
  gt: '>',
  quot: '"',
  apos: "'",
};

/** Trả các ký tự đã bị thoát trong XML về chữ thật. */
function boThoat(chu: string): string {
  return chu.replace(/&(#x?[0-9a-fA-F]+|[a-z]+);/g, (nguyen, ma: string) => {
    if (ma.startsWith('#x') || ma.startsWith('#X')) {
      return String.fromCodePoint(parseInt(ma.slice(2), 16));
    }
    if (ma.startsWith('#')) {
      return String.fromCodePoint(Number(ma.slice(1)));
    }
    return THUC_THE[ma] ?? nguyen;
  });
}

/** Lấy giá trị một thuộc tính trong đoạn thẻ mở, ví dụ r="B7" ra "B7". */
function thuocTinh(theMo: string, ten: string): string | null {
  const gap = new RegExp(`\\s${ten}="([^"]*)"`).exec(theMo);
  return gap === null ? null : boThoat(gap[1]);
}

/**
 * Ngược của `tenCot`: "B" thành 2, "AA" thành 27. Địa chỉ ô là "B7" nên bỏ phần số đi.
 */
export function soCotTuDiaChi(diaChi: string): number {
  let so = 0;
  for (const ky of diaChi) {
    const ma = ky.charCodeAt(0);
    if (ma < 65 || ma > 90) {
      break;
    }
    so = so * 26 + (ma - 64);
  }
  return so;
}

/** Nội dung mọi thẻ <t> trong một đoạn, nối lại. Chữ có nhiều kiểu bị Excel cắt làm nhiều <t>. */
function chuTrongThe(doan: string): string {
  let ketQua = '';
  const timT = /<t[^>]*\/>|<t[^>]*>([\s\S]*?)<\/t>/g;
  let gap = timT.exec(doan);
  while (gap !== null) {
    ketQua += boThoat(gap[1] ?? '');
    gap = timT.exec(doan);
  }
  return ketQua;
}

// ---------- Các phần bên trong file zip ----------

/** Bảng chữ dùng chung. Excel thật gom hết chữ vào đây, ô chỉ giữ số thứ tự. */
function bangChuChung(goiTin: Record<string, Uint8Array>): string[] {
  const phan = goiTin['xl/sharedStrings.xml'];
  if (!phan) {
    return [];
  }

  const xml = strFromU8(phan);
  const bang: string[] = [];
  const timSi = /<si>([\s\S]*?)<\/si>/g;
  let gap = timSi.exec(xml);
  while (gap !== null) {
    bang.push(chuTrongThe(gap[1]));
    gap = timSi.exec(xml);
  }
  return bang;
}

/**
 * Danh sách trang: tên hiện dưới đáy Excel, kèm đường dẫn tới file XML của trang.
 *
 * Tên nằm ở workbook.xml còn đường dẫn nằm ở file quan hệ bên cạnh, nối với nhau bằng
 * r:id. Thiếu file quan hệ (rất hiếm) thì đoán theo lối đặt tên thường gặp.
 */
function danhSachTrang(goiTin: Record<string, Uint8Array>): { ten: string; duongDan: string }[] {
  const phan = goiTin['xl/workbook.xml'];
  if (!phan) {
    throw new KhongDocDuocFile('File này không phải file Excel (.xlsx).');
  }

  const noi = new Map<string, string>();
  const phanQuanHe = goiTin['xl/_rels/workbook.xml.rels'];
  if (phanQuanHe) {
    const xmlQuanHe = strFromU8(phanQuanHe);
    const tim = /<Relationship\b([^>]*)\/>/g;
    let gap = tim.exec(xmlQuanHe);
    while (gap !== null) {
      const ma = thuocTinh(gap[1], 'Id');
      const dich = thuocTinh(gap[1], 'Target');
      if (ma !== null && dich !== null) {
        // Đường dẫn có thể ghi tương đối ("worksheets/sheet1.xml") hoặc tuyệt đối
        // ("/xl/worksheets/sheet1.xml") — Excel và mấy phần mềm khác ghi mỗi bên một kiểu.
        noi.set(ma, dich.startsWith('/') ? dich.slice(1) : `xl/${dich}`);
      }
      gap = tim.exec(xmlQuanHe);
    }
  }

  const xml = strFromU8(phan);
  const cac: { ten: string; duongDan: string }[] = [];
  const timSheet = /<sheet\b([^>]*)\/>/g;
  let gapSheet = timSheet.exec(xml);
  let thuTu = 0;
  while (gapSheet !== null) {
    thuTu += 1;
    const ten = thuocTinh(gapSheet[1], 'name') ?? `Trang ${thuTu}`;
    const ma = thuocTinh(gapSheet[1], 'r:id');
    const duongDan =
      (ma !== null ? noi.get(ma) : undefined) ?? `xl/worksheets/sheet${thuTu}.xml`;
    if (goiTin[duongDan]) {
      cac.push({ ten, duongDan });
    }
    gapSheet = timSheet.exec(xml);
  }

  return cac;
}

/** Giá trị một ô, đã quy về chữ hoặc số. */
function giaTriO(theMo: string, ruot: string, chuChung: string[]): O {
  const kieu = thuocTinh(theMo, 't');

  if (kieu === 'inlineStr') {
    const chu = chuTrongThe(ruot);
    return chu === '' ? null : chu;
  }

  const gapV = /<v[^>]*>([\s\S]*?)<\/v>/.exec(ruot);
  if (gapV === null) {
    return null;
  }
  const tho = boThoat(gapV[1]);

  if (kieu === 's') {
    // Số thứ tự trong bảng chữ dùng chung.
    const chu = chuChung[Number(tho)];
    return chu === undefined || chu === '' ? null : chu;
  }

  // t="str" là kết quả chữ của một ô công thức; t="b" là Đúng/Sai.
  if (kieu === 'str') {
    return tho === '' ? null : tho;
  }
  if (kieu === 'b') {
    return tho === '1' ? 'TRUE' : 'FALSE';
  }

  const so = Number(tho);
  return Number.isFinite(so) ? so : tho === '' ? null : tho;
}

/** Các dòng của một trang tính. Dòng trống hẳn thì bỏ luôn, khỏi phải lọc ở bên gọi. */
function docDong(xml: string, chuChung: string[]): DongDaDoc[] {
  const dongs: DongDaDoc[] = [];

  const timRow = /<row\b([^>]*)>([\s\S]*?)<\/row>/g;
  let gapRow = timRow.exec(xml);
  let ngam = 0;
  while (gapRow !== null) {
    ngam += 1;
    const so = Number(thuocTinh(gapRow[1], 'r') ?? ngam);
    const ruotDong = gapRow[2];

    const o: O[] = [];
    // Hai dáng của một ô: rỗng thì tự đóng <c .../>, có ruột thì <c ...>…</c>.
    const timO = /<c\b([^>]*)\/>|<c\b([^>]*)>([\s\S]*?)<\/c>/g;
    let gapO = timO.exec(ruotDong);
    let cotNgam = 0;
    while (gapO !== null) {
      const theMo = gapO[1] ?? gapO[2];
      const ruot = gapO[3] ?? '';
      const diaChi = thuocTinh(theMo, 'r');
      // Ô trống ở giữa dòng bị Excel bỏ hẳn khỏi file, nên phải nhìn địa chỉ mà đặt
      // đúng cột — đếm tuần tự thì cả dòng bị dồn sang trái.
      const cot = diaChi !== null ? soCotTuDiaChi(diaChi) : cotNgam + 1;
      cotNgam = cot;

      while (o.length < cot - 1) {
        o.push(null);
      }
      o[cot - 1] = gapO[1] !== undefined ? null : giaTriO(theMo, ruot, chuChung);

      gapO = timO.exec(ruotDong);
    }

    if (o.some((giaTri) => giaTri !== null && giaTri !== '')) {
      dongs.push({ so, o });
    }
    gapRow = timRow.exec(xml);
  }

  return dongs;
}

// ---------- Đường vào ----------

/**
 * Đọc toàn bộ file .xlsx thành các trang tính.
 *
 * Nhận đúng khối byte đọc từ file. Chọn nhầm file khác — file ảnh, file .xls bản cũ —
 * thì quăng `KhongDocDuocFile` với câu người thường đọc được, vì đây là lỗi hay gặp
 * nhất: người dùng bấm nhầm file trong máy.
 */
export function docFileExcel(noiDung: Uint8Array): TrangDaDoc[] {
  let goiTin: Record<string, Uint8Array>;
  try {
    goiTin = unzipSync(noiDung);
  } catch {
    // File .xls đời cũ và file .csv đều rơi vào đây: chúng không phải zip.
    throw new KhongDocDuocFile('File này không phải file Excel .xlsx.');
  }

  const chuChung = bangChuChung(goiTin);
  const trangs = danhSachTrang(goiTin).map(({ ten, duongDan }) => ({
    ten,
    dongs: docDong(strFromU8(goiTin[duongDan]), chuChung),
  }));

  if (trangs.length === 0) {
    throw new KhongDocDuocFile('File Excel này không có trang nào.');
  }

  return trangs;
}

/**
 * Trang cần lấy trong file: tìm theo tên trước, không thấy thì lấy trang đầu.
 *
 * Lấy trang đầu chứ không báo lỗi, vì người dùng hay chép nội dung sang file mới của
 * riêng họ và đặt tên trang khác — miễn các cột còn đúng thì vẫn nhập được.
 */
export function timTrang(trangs: TrangDaDoc[], ten: string): TrangDaDoc {
  const hoa = ten.trim().toLowerCase();
  return trangs.find((trang) => trang.ten.trim().toLowerCase() === hoa) ?? trangs[0];
}
