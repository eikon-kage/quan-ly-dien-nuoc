/**
 * Ghi file Excel (.xlsx) bằng tay: một file .xlsx chỉ là mấy file XML nén lại thành zip.
 *
 * Tự viết thay vì lấy thư viện đọc-ghi Excel có sẵn vì hai lẽ. Một, các thư viện đó nặng
 * vài trăm KB và mang theo cả phần *đọc* file — app này không đọc, chỉ ghi. Hai, bản miễn
 * phí của chúng không kẻ được nét đậm hay định dạng số, mà bảng lương gửi cho chủ thì
 * tiền phải có dấu chấm ngăn nghìn và dòng tiêu đề phải nổi lên.
 *
 * Chỉ dùng đúng phần Excel cần để mở được file: không có công thức, không có biểu đồ,
 * không có sharedStrings (chữ ghi thẳng vào ô cho dễ đọc lúc gỡ lỗi).
 */

import { strToU8, zipSync } from 'fflate';

/** Kiểu của một cột, quyết định cách Excel hiện ô. */
export type KieuCot =
  /** Chữ. */
  | 'chu'
  /** Ngày, hiện thành 03/08/2026 và vẫn sắp xếp/lọc được như ngày. */
  | 'ngay'
  /** Tiền, hiện thành 1.500.000. */
  | 'tien'
  /** Số công, hiện thành 1 hoặc 1,5. */
  | 'so';

export interface Cot {
  nhan: string;
  /** Bề ngang tính theo số ký tự, cỡ chừng bằng độ dài chuỗi dài nhất trong cột. */
  rong: number;
  kieu: KieuCot;
}

/** Ô để trống thì truyền null. Ngày truyền chuỗi "yyyy-MM-dd". */
export type O = string | number | null;

export interface TrangTinh {
  /** Tên hiện ở tab dưới đáy Excel. Tối đa 31 ký tự, không chứa : \ / ? * [ ] */
  ten: string;
  cots: Cot[];
  dongs: O[][];
  /** Dòng tổng cộng in đậm ở cuối trang. Bỏ trống thì không có dòng này. */
  dongTong?: O[];
}

// ---------- Kiểu ô ----------

/**
 * Số thứ tự của kiểu ô trong styles.xml bên dưới. Đổi thứ tự ở đây là phải đổi cả
 * phần <cellXfs>.
 */
const Kieu = {
  thuong: 0,
  tieuDe: 1,
  ngay: 2,
  tien: 3,
  so: 4,
  chuDam: 5,
  tienDam: 6,
  soDam: 7,
} as const;

function kieuCuaO(kieu: KieuCot, dam: boolean): number {
  switch (kieu) {
    case 'ngay':
      return Kieu.ngay;
    case 'tien':
      return dam ? Kieu.tienDam : Kieu.tien;
    case 'so':
      return dam ? Kieu.soDam : Kieu.so;
    default:
      return dam ? Kieu.chuDam : Kieu.thuong;
  }
}

// ---------- Mấy hàm nhỏ ----------

function thoat(chu: string): string {
  return chu
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/**
 * Tên cột kiểu Excel: 1 thành A, 27 thành AA.
 */
export function tenCot(soThuTu: number): string {
  let ten = '';
  let con = soThuTu;
  while (con > 0) {
    const du = (con - 1) % 26;
    ten = String.fromCharCode(65 + du) + ten;
    con = Math.floor((con - du) / 26);
  }
  return ten;
}

/**
 * Ngày "yyyy-MM-dd" thành số ngày mà Excel dùng: đếm từ 30/12/1899.
 * Tính bằng UTC nên không lệch một ngày vì múi giờ hay giờ mùa hè.
 */
export function soNgayExcel(ngay: string): number {
  const [nam, thang, ngayTrongThang] = ngay.split('-').map(Number);
  return Date.UTC(nam, thang - 1, ngayTrongThang) / 86400000 + 25569;
}

/**
 * Tên trang hợp lệ: Excel không mở được file có tên trang dài quá 31 ký tự hoặc
 * chứa : \ / ? * [ ]
 */
export function tenTrangHopLe(ten: string): string {
  const sach = ten.replace(/[:\\/?*[\]]/g, ' ').trim();
  return (sach.length > 31 ? sach.slice(0, 31) : sach) || 'Trang';
}

// ---------- Dựng XML ----------

const LA_NGAY = /^\d{4}-\d{2}-\d{2}$/;

function oXml(diaChi: string, giaTri: O, kieu: KieuCot, dam: boolean): string {
  if (giaTri === null || giaTri === '') {
    return `<c r="${diaChi}" s="${kieuCuaO(kieu, dam)}"/>`;
  }

  if (kieu === 'ngay' && typeof giaTri === 'string' && LA_NGAY.test(giaTri)) {
    return `<c r="${diaChi}" s="${kieuCuaO('ngay', dam)}"><v>${soNgayExcel(giaTri)}</v></c>`;
  }

  if (typeof giaTri === 'number') {
    // Số không ra số (chia cho 0 chẳng hạn) mà ghi thẳng vào thì Excel báo file hỏng,
    // nên thà để ô trống.
    if (!Number.isFinite(giaTri)) {
      return `<c r="${diaChi}" s="${kieuCuaO(kieu, dam)}"/>`;
    }
    return `<c r="${diaChi}" s="${kieuCuaO(kieu, dam)}"><v>${giaTri}</v></c>`;
  }

  /*
   * Còn lại là chữ, kể cả chữ rơi vào cột ngày hay cột tiền — chữ "Tổng cộng" của dòng
   * cuối chẳng hạn. Chỗ này phải lấy kiểu chữ chứ không lấy kiểu của cột: ép "Tổng cộng"
   * thành ngày thì ô ra số vô nghĩa và Excel kêu file hỏng.
   *
   * Chữ ghi thẳng vào ô (inlineStr) chứ không gom vào bảng chữ dùng chung: file to hơn
   * một chút nhưng mở ra xem bằng mắt là hiểu ngay, đỡ công gỡ lỗi sau này.
   */
  return `<c r="${diaChi}" s="${kieuCuaO('chu', dam)}" t="inlineStr"><is><t xml:space="preserve">${thoat(
    String(giaTri),
  )}</t></is></c>`;
}

function dongXml(soDong: number, o: O[], cots: Cot[], dam: boolean): string {
  const cacO = o
    .map((giaTri, cot) =>
      oXml(`${tenCot(cot + 1)}${soDong}`, giaTri, cots[cot]?.kieu ?? 'chu', dam),
    )
    .join('');
  return `<row r="${soDong}">${cacO}</row>`;
}

function trangXml(trang: TrangTinh): string {
  const soCot = trang.cots.length;
  const soDong = trang.dongs.length + 1 + (trang.dongTong ? 1 : 0);

  const cols = trang.cots
    .map((cot, i) => `<col min="${i + 1}" max="${i + 1}" width="${cot.rong}" customWidth="1"/>`)
    .join('');

  const tieuDe = dongXml(
    1,
    trang.cots.map((cot) => cot.nhan),
    trang.cots.map((cot) => ({ ...cot, kieu: 'chu' as KieuCot })),
    true,
  );

  const dongs = trang.dongs.map((dong, i) => dongXml(i + 2, dong, trang.cots, false)).join('');
  const tong = trang.dongTong
    ? dongXml(trang.dongs.length + 2, trang.dongTong, trang.cots, true)
    : '';

  return (
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
    '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">' +
    `<dimension ref="A1:${tenCot(soCot)}${soDong}"/>` +
    // Khoá dòng tiêu đề lại: cuộn xuống giữa bảng vẫn biết cột nào là cột nào.
    '<sheetViews><sheetView workbookViewId="0">' +
    '<pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/>' +
    '</sheetView></sheetViews>' +
    '<sheetFormatPr defaultRowHeight="15"/>' +
    `<cols>${cols}</cols>` +
    `<sheetData>${tieuDe}${dongs}${tong}</sheetData>` +
    // Nút lọc sẵn trên dòng tiêu đề, khỏi phải tự bật.
    `<autoFilter ref="A1:${tenCot(soCot)}${trang.dongs.length + 1}"/>` +
    '</worksheet>'
  );
}

const STYLES_XML =
  '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
  '<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">' +
  '<numFmts count="3">' +
  '<numFmt numFmtId="164" formatCode="dd/mm/yyyy"/>' +
  // Tiền không có phần lẻ: đồng bạc lẻ không tồn tại trong sổ sách của cửa hàng.
  '<numFmt numFmtId="165" formatCode="#,##0"/>' +
  // Công thì có: nửa công là 0,5.
  '<numFmt numFmtId="166" formatCode="#,##0.##"/>' +
  '</numFmts>' +
  '<fonts count="2">' +
  '<font><sz val="11"/><name val="Calibri"/></font>' +
  '<font><b/><sz val="11"/><name val="Calibri"/></font>' +
  '</fonts>' +
  '<fills count="3">' +
  '<fill><patternFill patternType="none"/></fill>' +
  '<fill><patternFill patternType="gray125"/></fill>' +
  // Nền xanh rất nhạt cho dòng tiêu đề, cùng tông với app.
  '<fill><patternFill patternType="solid"><fgColor rgb="FFEEF3FE"/><bgColor indexed="64"/></patternFill></fill>' +
  '</fills>' +
  '<borders count="2">' +
  '<border><left/><right/><top/><bottom/><diagonal/></border>' +
  '<border><left/><right/><top/><bottom style="thin"><color rgb="FFB4C4E8"/></bottom><diagonal/></border>' +
  '</borders>' +
  '<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>' +
  '<cellXfs count="8">' +
  // 0 thường
  '<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>' +
  // 1 tiêu đề
  '<xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"/>' +
  // 2 ngày
  '<xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>' +
  // 3 tiền
  '<xf numFmtId="165" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>' +
  // 4 số công
  '<xf numFmtId="166" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>' +
  // 5 chữ đậm
  '<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>' +
  // 6 tiền đậm
  '<xf numFmtId="165" fontId="1" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1"/>' +
  // 7 số công đậm
  '<xf numFmtId="166" fontId="1" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1"/>' +
  '</cellXfs>' +
  '<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>' +
  '</styleSheet>';

/**
 * Dựng file .xlsx trong bộ nhớ. Trả về đúng khối byte để ghi thẳng ra file.
 */
export function taoFileExcel(trangs: TrangTinh[]): Uint8Array {
  if (trangs.length === 0) {
    throw new Error('File Excel phải có ít nhất một trang.');
  }

  const ten = trangs.map((trang) => tenTrangHopLe(trang.ten));

  const cacTrang = ten
    .map((t, i) => `<sheet name="${thoat(t)}" sheetId="${i + 1}" r:id="rId${i + 1}"/>`)
    .join('');

  const workbookXml =
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
    '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" ' +
    'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">' +
    `<sheets>${cacTrang}</sheets>` +
    '</workbook>';

  const goc = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships';
  const noiTrang = trangs
    .map(
      (_, i) =>
        `<Relationship Id="rId${i + 1}" Type="${goc}/worksheet" Target="worksheets/sheet${
          i + 1
        }.xml"/>`,
    )
    .join('');

  const workbookRels =
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
    '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' +
    noiTrang +
    `<Relationship Id="rId${trangs.length + 1}" Type="${goc}/styles" Target="styles.xml"/>` +
    '</Relationships>';

  const kieuNoiDung =
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
    '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">' +
    '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>' +
    '<Default Extension="xml" ContentType="application/xml"/>' +
    '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>' +
    trangs
      .map(
        (_, i) =>
          `<Override PartName="/xl/worksheets/sheet${
            i + 1
          }.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`,
      )
      .join('') +
    '<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>' +
    '</Types>';

  const goiTin: Record<string, Uint8Array> = {
    '[Content_Types].xml': strToU8(kieuNoiDung),
    '_rels/.rels':
      strToU8(
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>' +
          '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' +
          `<Relationship Id="rId1" Type="${goc}/officeDocument" Target="xl/workbook.xml"/>` +
          '</Relationships>',
      ),
    'xl/workbook.xml': strToU8(workbookXml),
    'xl/_rels/workbook.xml.rels': strToU8(workbookRels),
    'xl/styles.xml': strToU8(STYLES_XML),
  };

  trangs.forEach((trang, i) => {
    goiTin[`xl/worksheets/sheet${i + 1}.xml`] = strToU8(trangXml(trang));
  });

  return zipSync(goiTin, { level: 6 });
}
