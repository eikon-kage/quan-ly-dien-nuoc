import { docFileExcel, timTrang, KhongDocDuocFile } from '../docXlsx';
import { DuLieuChamCong, duLieuRong } from '../kieu';
import { quyetToan } from '../ky';
import {
  FileKhongDungMau,
  TEN_TRANG_NHAP,
  apDungNhap,
  boUngTien,
  cacTrangMau,
  docFileNhap,
  docNgay,
  docTrangNhap,
  khoangNam,
  khoangThang,
  taoFileMau,
  tenFileMau,
  tomTat,
  tomTatDoc,
} from '../nhapExcel';
import { cham, dangCham, datGhiChuNgay, ghiChuNgay, themTho, themUng } from '../thaoTac';
import { O, taoFileExcel } from '../xlsx';

/**
 * Dựng một file .xlsx đúng như file người dùng đưa vào, bằng chính bộ ghi của app rồi
 * đọc lại bằng bộ đọc. Đi vòng qua file thật chứ không gọi thẳng hàm đọc dòng: chỗ hay
 * hỏng nhất là quãng giữa — ngày thành số, ô trống bị Excel bỏ hẳn khỏi file.
 */
const TIEU_DE_MAU = ['Ngày', 'Thứ', 'Sáng', 'Chiều', 'Ứng tiền', 'Ghi chú'];

function fileVoi(dongs: O[][], tieuDe: string[] = TIEU_DE_MAU): Uint8Array {
  return taoFileExcel([
    {
      ten: TEN_TRANG_NHAP,
      cots: tieuDe.map((nhan) => ({
        nhan,
        rong: 12,
        kieu: nhan === 'Ngày' ? ('ngay' as const) : ('chu' as const),
      })),
      dongs,
    },
  ]);
}

function khoMotTho(): { duLieu: DuLieuChamCong; thoId: string } {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  return { duLieu: them.duLieu, thoId: them.tho.id };
}

describe('đọc ngày trong ô Excel', () => {
  test('nhận số ngày của Excel, chữ kiểu Việt và chữ kiểu ISO', () => {
    // 46237 là 03/08/2026 đếm từ 30/12/1899 — đúng cách Excel cất ngày.
    expect(docNgay(46237)).toBe('2026-08-03');
    expect(docNgay('03/08/2026')).toBe('2026-08-03');
    expect(docNgay('3-8-2026')).toBe('2026-08-03');
    expect(docNgay('2026-08-03')).toBe('2026-08-03');
  });

  test('ngày không có thật hay năm vô lý thì trả về null chứ không tự đẩy sang ngày khác', () => {
    expect(docNgay('31/02/2026')).toBeNull();
    expect(docNgay('03/13/2026')).toBeNull();
    expect(docNgay('03/08/1926')).toBeNull();
    expect(docNgay('hôm qua')).toBeNull();
    expect(docNgay(null)).toBeNull();
  });
});

describe('đọc file người dùng điền', () => {
  test('số công, chữ x, nửa công và tiền ứng đều đọc được', () => {
    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', 'Thứ Hai', 1, 1, null, ''],
        ['2026-08-04', 'Thứ Ba', 'x', 0.5, 500000, 'về sớm'],
        ['2026-08-05', 'Thứ Tư', '1,5', 'n', '300.000', ''],
      ]),
    );

    expect(doc.lois).toEqual([]);
    expect(doc.dongs).toEqual([
      { soDong: 2, ngay: '2026-08-03', congSang: 1, congChieu: 1, ung: null, ghiChu: '' },
      // Chữ "x" là đi làm cả buổi, tức nửa công — cả ngày mới là một công.
      { soDong: 3, ngay: '2026-08-04', congSang: 0.5, congChieu: 0.5, ung: 500000, ghiChu: 'về sớm' },
      { soDong: 4, ngay: '2026-08-05', congSang: 1.5, congChieu: 0, ung: 300000, ghiChu: '' },
    ]);
  });

  test('ô để trống khác hẳn ô ghi 0: một cái không đụng tới, một cái là nghỉ', () => {
    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', '', null, 1, null, ''],
        ['2026-08-04', '', 0, null, null, ''],
      ]),
    );

    expect(doc.dongs[0]).toMatchObject({ congSang: null, congChieu: 1 });
    expect(doc.dongs[1]).toMatchObject({ congSang: 0, congChieu: null });
  });

  test('dòng chưa điền gì thì bỏ qua im lặng, không kể là lỗi', () => {
    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', 'Thứ Hai', null, null, null, null],
        ['2026-08-04', 'Thứ Ba', 1, null, null, null],
      ]),
    );

    expect(doc.dongs).toHaveLength(1);
    expect(doc.lois).toEqual([]);
  });

  test('dòng sai thì kể ra kèm số dòng, các dòng còn lại vẫn nhập được', () => {
    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', '', 1, 1, null, ''],
        ['hôm kia', '', 1, null, null, ''],
        ['2026-08-05', '', 'hai công', null, null, ''],
        ['2026-08-06', '', 9, null, null, ''],
      ]),
    );

    expect(doc.dongs).toHaveLength(1);
    expect(doc.lois).toEqual([
      { soDong: 3, ly: 'ngày "hôm kia" không đọc được' },
      { soDong: 4, ly: 'cột Sáng: không hiểu ô công "hai công"' },
      { soDong: 5, ly: 'cột Sáng: số công phải từ 0 tới 2,5' },
    ]);
  });

  test('dò cột theo tên nên thêm cột riêng vào giữa bảng vẫn đọc đúng', () => {
    const doc = docFileNhap(
      fileVoi(
        [['2026-08-03', 'Nhà anh Ba', 1, 1]],
        ['Ngày', 'Công trình', 'Buổi sáng', 'Buổi chiều'],
      ),
    );

    expect(doc.dongs).toEqual([
      { soDong: 2, ngay: '2026-08-03', congSang: 1, congChieu: 1, ung: null, ghiChu: '' },
    ]);
  });

  test('file không có cột nào nhận ra được thì báo bằng câu người dùng đọc hiểu', () => {
    expect(() => docFileNhap(fileVoi([['a', 'b']], ['Tên', 'Số tiền']))).toThrow(
      FileKhongDungMau,
    );
  });

  test('chọn nhầm file không phải .xlsx thì báo lỗi chứ không đổ vỡ', () => {
    expect(() => docFileNhap(new Uint8Array([1, 2, 3, 4]))).toThrow(KhongDocDuocFile);
  });
});

describe('ghi vào sổ', () => {
  test('chấm mới, sửa buổi đã có, và bỏ chấm buổi file ghi là nghỉ', () => {
    const { duLieu, thoId } = khoMotTho();
    const coSan = cham(duLieu, thoId, '2026-08-04', 'Sang', 1);

    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', '', 1, 1, null, ''],
        ['2026-08-04', '', 0.5, null, null, ''],
        ['2026-08-05', '', 0, null, null, ''],
      ]),
    );
    const ket = apDungNhap(coSan, thoId, doc.dongs);

    expect(ket.themBuoi).toBe(2);
    expect(ket.suaBuoi).toBe(1);
    expect(dangCham(ket.duLieu, thoId, '2026-08-04', 'Sang')?.soCong).toBe(0.5);
    expect(dangCham(ket.duLieu, thoId, '2026-08-05', 'Sang')).toBeUndefined();
  });

  test('buổi đã chấm sẵn mà file để trống thì giữ nguyên', () => {
    const { duLieu, thoId } = khoMotTho();
    const coSan = cham(duLieu, thoId, '2026-08-04', 'Chieu', 1);

    const doc = docFileNhap(fileVoi([['2026-08-04', '', 1, null, null, '']]));
    const ket = apDungNhap(coSan, thoId, doc.dongs);

    expect(dangCham(ket.duLieu, thoId, '2026-08-04', 'Chieu')?.soCong).toBe(1);
    expect(ket.boChamBuoi).toBe(0);
  });

  test('nhập lại đúng file ấy lần nữa thì không đổi gì thêm, tiền ứng cũng không cộng đôi', () => {
    const { duLieu, thoId } = khoMotTho();
    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', '', 1, 1, null, ''],
        ['2026-08-04', '', 1, 1, 500000, 'ứng mua xi'],
      ]),
    );

    const lan1 = apDungNhap(duLieu, thoId, doc.dongs);
    const lan2 = apDungNhap(lan1.duLieu, thoId, doc.dongs);

    expect(lan1.themBuoi).toBe(4);
    expect(lan1.themUng).toBe(1);
    expect(lan2.themBuoi).toBe(0);
    expect(lan2.suaBuoi).toBe(0);
    expect(lan2.themUng).toBe(0);
    expect(lan2.boQuaUngTrung).toBe(1);
    expect(lan2.duLieu.buoiCongs).toHaveLength(4);
    expect(lan2.duLieu.ungTiens).toHaveLength(1);
  });

  test('ứng cùng ngày nhưng khác số tiền thì vẫn là một lần ứng nữa', () => {
    const { duLieu, thoId } = khoMotTho();
    const coSan = themUng(duLieu, thoId, '2026-08-04', 500_000);

    const doc = docFileNhap(fileVoi([['2026-08-04', '', 1, null, 200000, '']]));
    const ket = apDungNhap(coSan, thoId, doc.dongs);

    expect(ket.themUng).toBe(1);
    expect(ket.duLieu.ungTiens).toHaveLength(2);
  });

  test('buổi đã nằm trong kỳ đã chốt thì không đụng vào — tiền ấy trả xong rồi', () => {
    const { duLieu, thoId } = khoMotTho();
    const daCham = cham(duLieu, thoId, '2026-08-03', 'Sang', 1);
    const daChot = quyetToan(daCham, { denNgay: '2026-08-03' });

    const doc = docFileNhap(fileVoi([['2026-08-03', '', 0.5, 1, null, '']]));
    const ket = apDungNhap(daChot, thoId, doc.dongs);

    expect(ket.boQuaDaChot).toBe(1);
    expect(ket.suaBuoi).toBe(0);
    expect(dangCham(ket.duLieu, thoId, '2026-08-03', 'Sang')?.soCong).toBe(1);
    // Buổi chiều chưa ai trả tiền nên vẫn chấm được như thường.
    expect(ket.themBuoi).toBe(1);
  });

  test('ghi chú của dòng thành ghi chú của ngày, không chép đôi sang từng buổi', () => {
    const { duLieu, thoId } = khoMotTho();

    const doc = docFileNhap(fileVoi([['2026-08-03', '', 1, 0.5, null, 'về sớm đi đám cưới']]));
    const ket = apDungNhap(duLieu, thoId, doc.dongs);

    expect(ghiChuNgay(ket.duLieu, thoId, '2026-08-03')).toBe('về sớm đi đám cưới');
    expect(ket.duLieu.ghiChuNgays).toHaveLength(1);
    expect(ket.duLieu.buoiCongs.every((b) => b.ghiChu === '')).toBe(true);
  });

  test('ô ghi chú trống thì giữ nguyên chữ đã có, cả của ngày lẫn của buổi', () => {
    const { duLieu, thoId } = khoMotTho();
    let coSan = cham(duLieu, thoId, '2026-08-03', 'Sang', 1, 'làm trần');
    coSan = datGhiChuNgay(coSan, thoId, '2026-08-03', 'mưa cả buổi chiều');

    const doc = docFileNhap(fileVoi([['2026-08-03', '', 0.5, 1, null, '']]));
    const ket = apDungNhap(coSan, thoId, doc.dongs);

    expect(ghiChuNgay(ket.duLieu, thoId, '2026-08-03')).toBe('mưa cả buổi chiều');
    expect(dangCham(ket.duLieu, thoId, '2026-08-03', 'Sang')?.ghiChu).toBe('làm trần');
  });

  test('ngày nghỉ hẳn trong file vẫn nhận được ghi chú', () => {
    const { duLieu, thoId } = khoMotTho();

    // Cả hai buổi đều 0 công: không có buổi nào để treo chữ, ghi chú ngày vẫn vào.
    const doc = docFileNhap(fileVoi([['2026-08-03', '', 0, 0, null, 'nghỉ đám cưới em gái']]));
    const ket = apDungNhap(duLieu, thoId, doc.dongs);

    expect(ket.duLieu.buoiCongs).toEqual([]);
    expect(ghiChuNgay(ket.duLieu, thoId, '2026-08-03')).toBe('nghỉ đám cưới em gái');
  });
});

describe('tóm tắt cho người dùng liếc qua', () => {
  test('đếm ngày, cộng công và cộng tiền ứng của file', () => {
    const doc = docFileNhap(
      fileVoi([
        ['2026-08-03', '', 1, 1, null, ''],
        ['2026-08-04', '', 0.5, 0, 500000, ''],
      ]),
    );

    expect(tomTatDoc(doc.dongs)).toEqual({
      soNgay: 2,
      tongCong: 2.5,
      soNghi: 1,
      tongUng: 500000,
      tuNgay: '2026-08-03',
      denNgay: '2026-08-04',
    });
  });

  test('kể ra đúng những việc đã làm', () => {
    const { duLieu, thoId } = khoMotTho();
    const doc = docFileNhap(fileVoi([['2026-08-03', '', 1, 1, 500000, '']]));

    expect(tomTat(apDungNhap(duLieu, thoId, doc.dongs))).toBe(
      'Đã chấm mới 2 buổi, thêm 1 lần ứng tiền.',
    );
  });

  test('kể cả số ngày được ghi chú, và không kể lại ghi chú y như cũ', () => {
    const { duLieu, thoId } = khoMotTho();
    const doc = docFileNhap(fileVoi([['2026-08-03', '', 1, 1, null, 'mưa, làm nửa ngày']]));

    const lanDau = apDungNhap(duLieu, thoId, doc.dongs);
    expect(tomTat(lanDau)).toBe('Đã chấm mới 2 buổi, ghi chú cho 1 ngày.');

    // Nhập lại đúng file ấy thì chẳng có gì đổi — kể cả ghi chú.
    expect(tomTat(apDungNhap(lanDau.duLieu, thoId, doc.dongs))).toBe('Không có gì thay đổi.');
  });
});

describe('file mẫu', () => {
  test('điền sẵn ngày và thứ của cả tháng, để trống hai cột công', () => {
    const { tuNgay, denNgay } = khoangThang('2026-08-19');
    const trang = timTrang(docFileExcel(taoFileMau('Anh Tuấn', tuNgay, denNgay)), TEN_TRANG_NHAP);

    // Một dòng tiêu đề cộng 31 ngày của tháng 8.
    expect(trang.dongs).toHaveLength(32);
    expect(trang.dongs[0].o.slice(0, 4)).toEqual(['Ngày', 'Thứ', 'Sáng', 'Chiều']);
    expect(trang.dongs[1].o[1]).toBe('Thứ Bảy');
  });

  test('file mẫu chưa điền gì thì đọc ra không dòng nào và cũng không lỗi nào', () => {
    const { tuNgay, denNgay } = khoangThang('2026-08-19');
    const doc = docFileNhap(taoFileMau('Anh Tuấn', tuNgay, denNgay));

    expect(doc.dongs).toEqual([]);
    expect(doc.lois).toEqual([]);
  });

  test('có trang hướng dẫn kèm tên thợ, để mở ra là biết file của ai', () => {
    const trangs = cacTrangMau('Anh Tuấn', '2026-08-01', '2026-08-31');
    const huongDan = trangs[1];

    expect(huongDan.ten).toBe('Hướng dẫn');
    expect(huongDan.dongs[0][0]).toContain('Anh Tuấn');
  });

  test('tên file bỏ dấu để gửi qua mạng không lỗi', () => {
    expect(tenFileMau('Anh Tuấn', '2026-08-01', '2026-08-31')).toBe(
      'Mau-cham-cong-anh-tuan-08-2026.xlsx',
    );
  });

  test('file cả năm và file một tháng không trùng tên nhau', () => {
    // Trùng tên là cái sau đè cái trước trong thư mục Tải về, mà cái trước vừa gõ nửa tháng.
    expect(tenFileMau('Anh Tuấn', '2026-01-01', '2026-12-31')).toBe(
      'Mau-cham-cong-anh-tuan-2026.xlsx',
    );
    // Khoảng lẻ thì ghi cả hai đầu, không giả vờ là một tháng.
    expect(tenFileMau('Anh Tuấn', '2026-08-03', '2026-09-05')).toBe(
      'Mau-cham-cong-anh-tuan-2026-08-03-den-2026-09-05.xlsx',
    );
  });
});

describe('file mẫu cả năm', () => {
  test('có đủ ngày của mười hai tháng, thêm một dòng tên tháng ngăn giữa', () => {
    const { tuNgay, denNgay } = khoangNam('2026-08-19');
    expect([tuNgay, denNgay]).toEqual(['2026-01-01', '2026-12-31']);

    const trang = timTrang(docFileExcel(taoFileMau('Anh Tuấn', tuNgay, denNgay)), TEN_TRANG_NHAP);

    // Một dòng tiêu đề + 365 ngày của 2026 + 12 dòng tên tháng.
    expect(trang.dongs).toHaveLength(1 + 365 + 12);
    expect(trang.dongs[1].o[0]).toBe('Tháng 01/2026');
    expect(trang.dongs[2].o[1]).toBe('Thứ Năm');
  });

  test('dòng tên tháng không thành lỗi lúc đọc lại, và cũng không thành một ngày', () => {
    const { tuNgay, denNgay } = khoangNam('2026-08-19');
    const doc = docFileNhap(taoFileMau('Anh Tuấn', tuNgay, denNgay));

    expect(doc.dongs).toEqual([]);
    expect(doc.lois).toEqual([]);
  });

  test('file một tháng thì không chen dòng tên tháng — chỉ có một tháng, nói lại là thừa', () => {
    const { tuNgay, denNgay } = khoangThang('2026-08-19');
    const trang = timTrang(docFileExcel(taoFileMau('Anh Tuấn', tuNgay, denNgay)), TEN_TRANG_NHAP);

    expect(trang.dongs).toHaveLength(32);
  });

  test('điền công vào một tháng thì mấy tháng để trống vẫn nguyên', () => {
    const { duLieu, thoId } = khoMotTho();
    const { tuNgay, denNgay } = khoangNam('2026-08-19');

    // Chép lại đúng lối người dùng làm: mở file mẫu cả năm ra, gõ vào một ngày tháng 9.
    const trangs = cacTrangMau('Anh Tuấn', tuNgay, denNgay);
    const cham9 = trangs[0].dongs.map((dong) =>
      dong[0] === '2026-09-10' ? [dong[0], dong[1], 1, 1, null, ''] : dong,
    );
    const doc = docFileNhap(taoFileExcel([{ ...trangs[0], dongs: cham9 }]));

    expect(doc.lois).toEqual([]);
    expect(doc.dongs.map((dong) => dong.ngay)).toEqual(['2026-09-10']);

    const ket = apDungNhap(duLieu, thoId, doc.dongs);
    expect(ket.themBuoi).toBe(2);
    expect(ket.duLieu.buoiCongs).toHaveLength(2);
  });
});

describe('file mẫu của máy thợ', () => {
  test('không có cột Ứng tiền — cả app trên máy thợ không biết tiền', () => {
    const trangs = cacTrangMau('Tôi', '2026-08-01', '2026-08-31', false);

    expect(trangs[0].cots.map((cot) => cot.nhan)).toEqual([
      'Ngày',
      'Thứ',
      'Sáng',
      'Chiều',
      'Ghi chú',
    ]);
    expect(trangs[1].dongs.flat().join(' ')).not.toContain('Ứng tiền');
  });

  test('bỏ tiền ứng khỏi những dòng đã đọc, giữ nguyên số công và ghi chú', () => {
    const doc = docFileNhap(fileVoi([['2026-08-03', '', 1, 0.5, 500000, 'về sớm']]));
    const sach = boUngTien(doc.dongs);

    expect(sach).toEqual([
      { soDong: 2, ngay: '2026-08-03', congSang: 1, congChieu: 0.5, ung: null, ghiChu: 'về sớm' },
    ]);
  });

  test('chọn đúng file của chủ thì công vẫn vào, tiền ứng thì không', () => {
    const { duLieu, thoId } = khoMotTho();
    const doc = docFileNhap(fileVoi([['2026-08-03', '', 1, 1, 500000, '']]));

    const ket = apDungNhap(duLieu, thoId, boUngTien(doc.dongs));

    expect(ket.themBuoi).toBe(2);
    expect(ket.themUng).toBe(0);
    expect(ket.duLieu.ungTiens).toEqual([]);
  });
});
