import { unzipSync, strFromU8 } from 'fflate';

import { BuoiLam, DuLieuChamCong, Tho, duLieuRong } from '../kieu';
import { taoId } from '../thaoTac';
import { quyetToan } from '../ky';
import { cacTrangExcel, tenFileExcel, xuatExcel } from '../xuatExcel';
import { soNgayExcel, tenCot, tenTrangHopLe } from '../xlsx';

function themTho(duLieu: DuLieuChamCong, ten: string, tienMotCong: number): Tho {
  const tho: Tho = {
    id: taoId(),
    ten,
    dienThoai: '0900000000',
    mocLuong: [{ tuNgay: '2026-01-01', tienMotCong }],
    dangLam: true,
    ghiChu: '',
    ngayTao: '2026-01-01',
    suaLuc: '2026-01-01T00:00:00.000Z',
  };
  duLieu.thos.push(tho);
  return tho;
}

function cham(
  duLieu: DuLieuChamCong,
  tho: Tho,
  ngay: string,
  buoi: BuoiLam,
  soCong = 1,
  ghiChu = '',
) {
  duLieu.buoiCongs.push({
    id: taoId(),
    thoId: tho.id,
    ngay,
    buoi,
    soCong,
    tienMotCong: null,
    ghiChu,
    suaLuc: '2026-08-01T00:00:00.000Z',
  });
}

function ung(duLieu: DuLieuChamCong, tho: Tho, ngay: string, soTien: number, ghiChu = '') {
  duLieu.ungTiens.push({
    id: taoId(),
    thoId: tho.id,
    ngay,
    soTien,
    ghiChu,
    suaLuc: '2026-08-01T00:00:00.000Z',
  });
}

/** Bộ dữ liệu dùng chung: hai thợ, hai tháng, có ứng tiền và có nửa công. */
function duLieuMau(): DuLieuChamCong {
  const duLieu = duLieuRong();
  const tuan = themTho(duLieu, 'Anh Tuấn', 300000);
  const binh = themTho(duLieu, 'Anh Bình', 250000);

  cham(duLieu, tuan, '2026-07-30', 'Sang');
  cham(duLieu, tuan, '2026-08-03', 'Sang');
  cham(duLieu, tuan, '2026-08-03', 'Chieu', 0.5, 'về sớm');
  cham(duLieu, binh, '2026-08-03', 'Sang');
  ung(duLieu, binh, '2026-08-04', 500000, 'ứng đi chợ');

  return duLieu;
}

/** Đọc một trang trong file .xlsx ra chuỗi XML. */
function docTrang(file: Uint8Array, soTrang: number): string {
  const goi = unzipSync(file);
  return strFromU8(goi[`xl/worksheets/sheet${soTrang}.xml`]);
}

describe('tên cột kiểu Excel', () => {
  it('đếm A, B rồi tới Z, AA', () => {
    expect(tenCot(1)).toBe('A');
    expect(tenCot(8)).toBe('H');
    expect(tenCot(26)).toBe('Z');
    expect(tenCot(27)).toBe('AA');
    expect(tenCot(28)).toBe('AB');
  });
});

describe('ngày của Excel', () => {
  it('đổi ngày ra đúng số Excel dùng', () => {
    // Mấy mốc này tra được trong chính Excel: gõ ngày rồi đổi ô sang kiểu Số.
    expect(soNgayExcel('1900-01-01')).toBe(2);
    expect(soNgayExcel('1970-01-01')).toBe(25569);
    expect(soNgayExcel('2026-08-03')).toBe(46237);
  });

  it('không lệch ngày dù máy để múi giờ nào', () => {
    // Chạy ở múi giờ âm (bên Mỹ) mà tính bằng giờ máy thì ngày lùi lại một hôm.
    expect(soNgayExcel('2026-08-03') - soNgayExcel('2026-08-02')).toBe(1);
  });
});

describe('tên trang', () => {
  it('cắt bớt và bỏ ký tự Excel không nhận', () => {
    expect(tenTrangHopLe('Bảng lương')).toBe('Bảng lương');
    expect(tenTrangHopLe('Công/nợ [2026]')).toBe('Công nợ  2026');
    expect(tenTrangHopLe('x'.repeat(40))).toHaveLength(31);
    expect(tenTrangHopLe('  ')).toBe('Trang');
  });
});

describe('nội dung các trang', () => {
  const trangs = cacTrangExcel(duLieuMau(), '2026-08-05');

  it('đủ sáu trang, quyết toán đứng đầu vì đó là sổ tiền đã trả', () => {
    expect(trangs.map((t) => t.ten)).toEqual([
      'Quyết toán',
      'Kỳ này',
      'Buổi công',
      'Ứng tiền',
      'Thợ',
      'Mốc lương',
    ]);
  });

  it('trang Kỳ này khớp đúng màn hình Bảng lương, không cắt theo tháng', () => {
    const kyNay = trangs[1];

    // Kỳ chạy từ buổi sớm nhất chưa quyết toán tới hôm nay, vắt qua hai tháng.
    // Xếp theo tên tiếng Việt nên anh Bình đứng trước.
    expect(kyNay.dongs[0]).toEqual([
      '2026-07-30',
      '2026-08-05',
      'Anh Bình',
      1,
      0,
      1,
      250000,
      500000,
      0,
      -250000,
    ]);

    // Anh Tuấn: hai buổi sáng ở hai tháng khác nhau vẫn nằm chung một dòng, cộng thêm
    // nửa công chiều — đúng con số màn hình Bảng lương đang hiện.
    expect(kyNay.dongs[1]).toEqual([
      '2026-07-30',
      '2026-08-05',
      'Anh Tuấn',
      2,
      0.5,
      2.5,
      750000,
      0,
      0,
      750000,
    ]);
  });

  it('dòng tổng cộng cộng đúng cả cột tiền lẫn cột công', () => {
    const kyNay = trangs[1];
    expect(kyNay.dongTong).toEqual([
      'Tổng cộng',
      null,
      null,
      3,
      0.5,
      3.5,
      1000000,
      500000,
      0,
      500000,
    ]);
  });

  it('chốt kỳ thì tiền chuyển từ trang Kỳ này sang trang Quyết toán', () => {
    const truoc = duLieuMau();
    const daChot = quyetToan(truoc, { denNgay: '2026-08-05' });
    const [quyet, kyNay] = cacTrangExcel(daChot, '2026-08-05');

    // Cả hai thợ nằm trong tờ quyết toán, tổng tiền công y nguyên 1.000.000 — đúng bằng
    // tổng của trang Kỳ này trước lúc chốt.
    expect(quyet.dongs).toHaveLength(2);
    expect(quyet.dongTong?.[6]).toBe(1000000);

    // Trang Kỳ này chỉ còn anh Bình vì anh ấy ứng quá tay 250.000, kỳ sau trừ lại.
    // Công đã trả tiền thì không quay lại nữa: không còn dòng nào có tiền công.
    expect(kyNay.dongs.map((dong) => [dong[2], dong[6], dong[9]])).toEqual([
      ['Anh Bình', 0, -250000],
    ]);
  });

  it('buổi công xếp theo ngày rồi tên thợ, sáng trước chiều sau', () => {
    const buoiCong = trangs[2];
    expect(buoiCong.dongs.map((dong) => [dong[0], dong[2], dong[3]])).toEqual([
      ['2026-07-30', 'Anh Tuấn', 'Sáng'],
      ['2026-08-03', 'Anh Bình', 'Sáng'],
      ['2026-08-03', 'Anh Tuấn', 'Sáng'],
      ['2026-08-03', 'Anh Tuấn', 'Chiều'],
    ]);
  });

  it('buổi công ghi đủ thứ, số công và thành tiền', () => {
    const buoiCong = trangs[2];
    expect(buoiCong.dongs[0]).toEqual([
      '2026-07-30',
      'Thứ Năm',
      'Anh Tuấn',
      'Sáng',
      1,
      300000,
      300000,
      '',
    ]);
    // Nửa công thì thành tiền cũng còn một nửa.
    expect(buoiCong.dongs[3]).toEqual([
      '2026-08-03',
      'Thứ Hai',
      'Anh Tuấn',
      'Chiều',
      0.5,
      300000,
      150000,
      'về sớm',
    ]);
  });

  it('ứng tiền có đủ ngày, tên thợ và ghi chú', () => {
    expect(trangs[3].dongs).toEqual([
      ['2026-08-04', 'Thứ Ba', 'Anh Bình', 500000, 'ứng đi chợ'],
    ]);
  });

  it('thợ đã bị xoá khỏi danh sách thì buổi công vẫn còn, chỉ mất tên', () => {
    const duLieu = duLieuMau();
    duLieu.thos = [];
    const buoiCong = cacTrangExcel(duLieu, '2026-08-05')[2];
    expect(buoiCong.dongs[0][2]).toBe('(thợ đã bị xoá)');
  });

  it('lịch sử tăng lương ra thành từng mốc', () => {
    const duLieu = duLieuMau();
    duLieu.thos[0].mocLuong.push({ tuNgay: '2026-08-01', tienMotCong: 350000 });

    const mocLuong = cacTrangExcel(duLieu, '2026-08-05')[5];
    expect(mocLuong.dongs).toEqual([
      ['Anh Bình', '2026-01-01', 250000],
      ['Anh Tuấn', '2026-01-01', 300000],
      ['Anh Tuấn', '2026-08-01', 350000],
    ]);
  });

  it('chưa có dữ liệu thì vẫn ra đủ trang, chỉ không có dòng tổng', () => {
    const trong = cacTrangExcel(duLieuRong(), '2026-08-05');
    expect(trong).toHaveLength(6);
    expect(trong[0].dongs).toEqual([]);
    expect(trong[0].dongTong).toBeUndefined();
  });
});

describe('file .xlsx dựng ra', () => {
  const file = xuatExcel(duLieuMau(), '2026-08-05');

  it('là một file zip có đủ các phần Excel cần', () => {
    const goi = unzipSync(file);
    expect(Object.keys(goi).sort()).toEqual([
      '[Content_Types].xml',
      '_rels/.rels',
      'xl/_rels/workbook.xml.rels',
      'xl/styles.xml',
      'xl/workbook.xml',
      'xl/worksheets/sheet1.xml',
      'xl/worksheets/sheet2.xml',
      'xl/worksheets/sheet3.xml',
      'xl/worksheets/sheet4.xml',
      'xl/worksheets/sheet5.xml',
      'xl/worksheets/sheet6.xml',
    ]);
  });

  it('tên trang tiếng Việt giữ nguyên dấu', () => {
    const workbook = strFromU8(unzipSync(file)['xl/workbook.xml']);
    expect(workbook).toContain('name="Kỳ này"');
    expect(workbook).toContain('name="Ứng tiền"');
  });

  it('ngày ghi thành số của Excel chứ không phải chữ', () => {
    const trang = docTrang(file, 3);
    expect(trang).toContain(`<v>${soNgayExcel('2026-07-30')}</v>`);
    expect(trang).not.toContain('2026-07-30');
  });

  it('tiền ghi thành số để Excel còn cộng được', () => {
    expect(docTrang(file, 2)).toContain('<v>750000</v>');
  });

  it('chữ có dấu và ký tự đặc biệt không làm hỏng XML', () => {
    const duLieu = duLieuMau();
    duLieu.thos[0].ten = 'Anh Tuấn <con> & "bé"';
    const trang = docTrang(xuatExcel(duLieu, '2026-08-05'), 5);

    expect(trang).toContain('Anh Tuấn &lt;con&gt; &amp; &quot;bé&quot;');
    expect(trang).not.toContain('<con>');
  });

  it('chữ nằm trong cột ngày vẫn là chữ', () => {
    // Dòng cuối trang Buổi công có chữ "Tổng cộng" ngay dưới cột Ngày. Ép nó thành ngày
    // thì ô ra số vô nghĩa và Excel kêu file hỏng.
    const trang = docTrang(file, 3);
    expect(trang).toContain('<t xml:space="preserve">Tổng cộng</t>');
    expect(trang).not.toContain('NaN');
  });

  it('dòng tiêu đề được khoá lại và có nút lọc', () => {
    const trang = docTrang(file, 2);
    expect(trang).toContain('state="frozen"');
    expect(trang).toContain('<autoFilter ref="A1:J3"/>');
  });
});

describe('tên file', () => {
  it('viết theo ngày xuất, không dấu', () => {
    expect(tenFileExcel('2026-08-05')).toBe('Cham-cong-05-08-2026.xlsx');
  });
});
