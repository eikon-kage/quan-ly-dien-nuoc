import { unzipSync, strFromU8 } from 'fflate';

import { BuoiLam, DuLieuChamCong, Tho, duLieuRong } from '../kieu';
import { taoId } from '../thaoTac';
import { cacThangCoDuLieu, cacTrangExcel, tenFileExcel, xuatExcel } from '../xuatExcel';
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

describe('các tháng có dữ liệu', () => {
  it('gom cả tháng chỉ có ứng tiền, xếp từ cũ tới mới', () => {
    const duLieu = duLieuMau();
    ung(duLieu, duLieu.thos[0], '2026-06-15', 100000);

    expect(cacThangCoDuLieu(duLieu)).toEqual([
      { nam: 2026, thang: 6 },
      { nam: 2026, thang: 7 },
      { nam: 2026, thang: 8 },
    ]);
  });

  it('dữ liệu rỗng thì không có tháng nào', () => {
    expect(cacThangCoDuLieu(duLieuRong())).toEqual([]);
  });
});

describe('nội dung các trang', () => {
  const trangs = cacTrangExcel(duLieuMau(), '2026-08-05');

  it('đủ năm trang, bảng lương đứng đầu', () => {
    expect(trangs.map((t) => t.ten)).toEqual([
      'Bảng lương',
      'Buổi công',
      'Ứng tiền',
      'Thợ',
      'Mốc lương',
    ]);
  });

  it('bảng lương tách theo từng tháng và tính đúng tiền', () => {
    const [bangLuong] = trangs;

    // Tháng 7 chỉ có một công của anh Tuấn.
    expect(bangLuong.dongs[0]).toEqual(['07/2026', 'Anh Tuấn', 1, 0, 1, 300000, 0, 300000]);

    // Tháng 8: anh Bình một công, ứng 500.000 nên còn phải trả âm.
    expect(bangLuong.dongs[1]).toEqual([
      '08/2026',
      'Anh Bình',
      1,
      0,
      1,
      250000,
      500000,
      -250000,
    ]);

    // Anh Tuấn: sáng 1 công, chiều nửa công, thành 450.000.
    expect(bangLuong.dongs[2]).toEqual(['08/2026', 'Anh Tuấn', 1, 0.5, 1.5, 450000, 0, 450000]);
  });

  it('dòng tổng cộng cộng đúng cả cột tiền lẫn cột công', () => {
    const [bangLuong] = trangs;
    expect(bangLuong.dongTong).toEqual(['Tổng cộng', null, 3, 0.5, 3.5, 1000000, 500000, 500000]);
  });

  it('buổi công xếp theo ngày rồi tên thợ, sáng trước chiều sau', () => {
    const buoiCong = trangs[1];
    expect(buoiCong.dongs.map((dong) => [dong[0], dong[2], dong[3]])).toEqual([
      ['2026-07-30', 'Anh Tuấn', 'Sáng'],
      ['2026-08-03', 'Anh Bình', 'Sáng'],
      ['2026-08-03', 'Anh Tuấn', 'Sáng'],
      ['2026-08-03', 'Anh Tuấn', 'Chiều'],
    ]);
  });

  it('buổi công ghi đủ thứ, số công và thành tiền', () => {
    const buoiCong = trangs[1];
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
    expect(trangs[2].dongs).toEqual([
      ['2026-08-04', 'Thứ Ba', 'Anh Bình', 500000, 'ứng đi chợ'],
    ]);
  });

  it('thợ đã bị xoá khỏi danh sách thì buổi công vẫn còn, chỉ mất tên', () => {
    const duLieu = duLieuMau();
    duLieu.thos = [];
    const buoiCong = cacTrangExcel(duLieu, '2026-08-05')[1];
    expect(buoiCong.dongs[0][2]).toBe('(thợ đã bị xoá)');
  });

  it('lịch sử tăng lương ra thành từng mốc', () => {
    const duLieu = duLieuMau();
    duLieu.thos[0].mocLuong.push({ tuNgay: '2026-08-01', tienMotCong: 350000 });

    const mocLuong = cacTrangExcel(duLieu, '2026-08-05')[4];
    expect(mocLuong.dongs).toEqual([
      ['Anh Bình', '2026-01-01', 250000],
      ['Anh Tuấn', '2026-01-01', 300000],
      ['Anh Tuấn', '2026-08-01', 350000],
    ]);
  });

  it('chưa có dữ liệu thì vẫn ra đủ trang, chỉ không có dòng tổng', () => {
    const trong = cacTrangExcel(duLieuRong(), '2026-08-05');
    expect(trong).toHaveLength(5);
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
    ]);
  });

  it('tên trang tiếng Việt giữ nguyên dấu', () => {
    const workbook = strFromU8(unzipSync(file)['xl/workbook.xml']);
    expect(workbook).toContain('name="Bảng lương"');
    expect(workbook).toContain('name="Ứng tiền"');
  });

  it('ngày ghi thành số của Excel chứ không phải chữ', () => {
    const trang = docTrang(file, 2);
    expect(trang).toContain(`<v>${soNgayExcel('2026-07-30')}</v>`);
    expect(trang).not.toContain('2026-07-30');
  });

  it('tiền ghi thành số để Excel còn cộng được', () => {
    expect(docTrang(file, 1)).toContain('<v>450000</v>');
  });

  it('chữ có dấu và ký tự đặc biệt không làm hỏng XML', () => {
    const duLieu = duLieuMau();
    duLieu.thos[0].ten = 'Anh Tuấn <con> & "bé"';
    const trang = docTrang(xuatExcel(duLieu, '2026-08-05'), 4);

    expect(trang).toContain('Anh Tuấn &lt;con&gt; &amp; &quot;bé&quot;');
    expect(trang).not.toContain('<con>');
  });

  it('chữ nằm trong cột ngày vẫn là chữ', () => {
    // Dòng cuối trang Buổi công có chữ "Tổng cộng" ngay dưới cột Ngày. Ép nó thành ngày
    // thì ô ra số vô nghĩa và Excel kêu file hỏng.
    const trang = docTrang(file, 2);
    expect(trang).toContain('<t xml:space="preserve">Tổng cộng</t>');
    expect(trang).not.toContain('NaN');
  });

  it('dòng tiêu đề được khoá lại và có nút lọc', () => {
    const trang = docTrang(file, 1);
    expect(trang).toContain('state="frozen"');
    expect(trang).toContain('<autoFilter ref="A1:H4"/>');
  });
});

describe('tên file', () => {
  it('viết theo ngày xuất, không dấu', () => {
    expect(tenFileExcel('2026-08-05')).toBe('Cham-cong-05-08-2026.xlsx');
  });
});
