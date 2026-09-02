import { cacThangXemDuoc, thang } from '../bangLuong';
import { BuoiLam, DuLieuChamCong, Tho, duLieuRong } from '../kieu';
import { taoId } from '../thaoTac';

function themTho(duLieu: DuLieuChamCong, ten: string, tienMotCong: number): Tho {
  const tho: Tho = {
    id: taoId(),
    ten,
    dienThoai: '',
    mocLuong: [{ tuNgay: '2026-01-01', tienMotCong }],
    dangLam: true,
    ghiChu: '',
    ngayTao: '2026-01-01',
    suaLuc: '2026-01-01T00:00:00.000Z',
  };
  duLieu.thos.push(tho);
  return tho;
}

/** Tăng lương từ một ngày: thêm mốc mới, mốc cũ giữ nguyên. */
function tangLuong(tho: Tho, tuNgay: string, tienMotCong: number) {
  tho.mocLuong = [...tho.mocLuong, { tuNgay, tienMotCong }].sort((a, b) =>
    a.tuNgay < b.tuNgay ? -1 : 1,
  );
}

function cham(
  duLieu: DuLieuChamCong,
  tho: Tho,
  ngay: string,
  buoi: BuoiLam,
  soCong = 1,
  tienMotCong: number | null = null,
) {
  duLieu.buoiCongs.push({
    id: taoId(),
    thoId: tho.id,
    ngay,
    buoi,
    soCong,
    tienMotCong,
    ghiChu: '',
    suaLuc: '2026-01-01T00:00:00.000Z',
  });
}

function ung(duLieu: DuLieuChamCong, tho: Tho, ngay: string, soTien: number) {
  duLieu.ungTiens.push({
    id: taoId(),
    thoId: tho.id,
    ngay,
    soTien,
    ghiChu: '',
    suaLuc: '2026-01-01T00:00:00.000Z',
  });
}

describe('bảng lương', () => {
  test('công sáng và chiều tách riêng, tiền nhân theo tổng công', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-08-03', 'Sang');
    cham(duLieu, tho, '2026-08-03', 'Chieu');
    cham(duLieu, tho, '2026-08-04', 'Sang');

    const [dong] = thang(duLieu, 2026, 8);

    expect(dong.congSang).toBe(2);
    expect(dong.congChieu).toBe(1);
    expect(dong.tongCong).toBe(3);
    expect(dong.tienCong).toBe(900_000);
  });

  test('mỗi thợ một giá khác nhau', () => {
    const duLieu = duLieuRong();
    const tuan = themTho(duLieu, 'Anh Tuấn', 300_000);
    const binh = themTho(duLieu, 'Anh Bình', 250_000);
    cham(duLieu, tuan, '2026-08-03', 'Sang');
    cham(duLieu, binh, '2026-08-03', 'Sang');

    const bang = thang(duLieu, 2026, 8);

    expect(bang.find((d) => d.tho.id === binh.id)!.tienCong).toBe(250_000);
    expect(bang.find((d) => d.tho.id === tuan.id)!.tienCong).toBe(300_000);
  });

  test('tăng lương từ tháng 8 không làm đổi bảng lương tháng 7', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-07-10', 'Sang');
    cham(duLieu, tho, '2026-08-03', 'Sang');

    tangLuong(tho, '2026-08-01', 350_000);

    expect(thang(duLieu, 2026, 7)[0].tienCong).toBe(300_000);
    expect(thang(duLieu, 2026, 8)[0].tienCong).toBe(350_000);
  });

  test('tăng lương giữa tháng thì nửa đầu giá cũ, nửa sau giá mới', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-08-10', 'Sang');
    cham(duLieu, tho, '2026-08-20', 'Sang');

    tangLuong(tho, '2026-08-15', 350_000);

    expect(thang(duLieu, 2026, 8)[0].tienCong).toBe(650_000);
  });

  test('buổi công trước cả mốc lương đầu tiên thì lấy chính mốc đầu tiên', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2025-12-20', 'Sang');

    expect(thang(duLieu, 2025, 12)[0].tienCong).toBe(300_000);
  });

  test('buổi có giá riêng thì giá riêng thắng mốc lương', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-08-03', 'Sang', 1, 500_000);

    expect(thang(duLieu, 2026, 8)[0].tienCong).toBe(500_000);
  });

  test('nửa công và làm thêm', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-08-03', 'Sang', 0.5);
    cham(duLieu, tho, '2026-08-03', 'Chieu', 1.5);

    const [dong] = thang(duLieu, 2026, 8);

    expect(dong.tongCong).toBe(2);
    expect(dong.tienCong).toBe(600_000);
  });

  test('trừ tiền đã ứng', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-08-03', 'Sang');
    cham(duLieu, tho, '2026-08-03', 'Chieu');
    ung(duLieu, tho, '2026-08-05', 200_000);

    const [dong] = thang(duLieu, 2026, 8);

    expect(dong.daUng).toBe(200_000);
    expect(dong.conLai).toBe(400_000);
  });

  test('ứng quá tiền thì còn lại âm', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-08-03', 'Sang');
    ung(duLieu, tho, '2026-08-05', 500_000);

    expect(thang(duLieu, 2026, 8)[0].conLai).toBe(-200_000);
  });

  test('chỉ lấy công trong khoảng đang xem', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2026-07-31', 'Sang');
    cham(duLieu, tho, '2026-08-01', 'Sang');
    cham(duLieu, tho, '2026-08-31', 'Sang');
    cham(duLieu, tho, '2026-09-01', 'Sang');

    expect(thang(duLieu, 2026, 8)[0].tongCong).toBe(2);
  });

  test('tháng hai năm nhuận lấy đủ ngày 29', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tho, '2024-02-29', 'Sang');

    expect(thang(duLieu, 2024, 2)[0].tongCong).toBe(1);
  });

  test('bỏ qua thợ không có công và không ứng', () => {
    const duLieu = duLieuRong();
    themTho(duLieu, 'Anh Tuấn', 300_000);

    expect(thang(duLieu, 2026, 8)).toHaveLength(0);
  });

  test('thợ đã nghỉ nhưng trong kỳ còn công thì vẫn hiện', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    tho.dangLam = false;
    cham(duLieu, tho, '2026-08-03', 'Sang');

    expect(thang(duLieu, 2026, 8)).toHaveLength(1);
  });

  test('chỉ ứng mà không đi làm thì vẫn hiện để biết còn nợ', () => {
    const duLieu = duLieuRong();
    const tho = themTho(duLieu, 'Anh Tuấn', 300_000);
    ung(duLieu, tho, '2026-08-05', 500_000);

    const [dong] = thang(duLieu, 2026, 8);

    expect(dong.tongCong).toBe(0);
    expect(dong.conLai).toBe(-500_000);
  });

  test('xếp theo tên thợ', () => {
    const duLieu = duLieuRong();
    const tuan = themTho(duLieu, 'Anh Tuấn', 300_000);
    const binh = themTho(duLieu, 'Anh Bình', 250_000);
    cham(duLieu, tuan, '2026-08-03', 'Sang');
    cham(duLieu, binh, '2026-08-03', 'Sang');

    expect(thang(duLieu, 2026, 8).map((d) => d.tho.ten)).toEqual(['Anh Bình', 'Anh Tuấn']);
  });
});

describe('các tháng xem lại được', () => {
  test('liền mạch từ tháng có bản ghi sớm nhất tới tháng này, mới nhất đứng đầu', () => {
    const duLieu = duLieuRong();
    const tuan = themTho(duLieu, 'Anh Tuấn', 300_000);
    cham(duLieu, tuan, '2026-06-20', 'Sang');
    // Tháng 7 nghỉ trắng, nhưng vẫn phải nằm trong danh sách: bấm lùi từng tháng mà app
    // nhảy cóc qua thì người xem tưởng mình bấm hụt.
    cham(duLieu, tuan, '2026-08-03', 'Sang');

    expect(cacThangXemDuoc(duLieu, '2026-09-02')).toEqual([
      { nam: 2026, thang: 9 },
      { nam: 2026, thang: 8 },
      { nam: 2026, thang: 7 },
      { nam: 2026, thang: 6 },
    ]);
  });

  test('sang năm mới thì lùi tiếp về tháng 12 năm ngoái', () => {
    const duLieu = duLieuRong();
    cham(duLieu, themTho(duLieu, 'Anh Tuấn', 300_000), '2025-12-30', 'Sang');

    expect(cacThangXemDuoc(duLieu, '2026-01-05')).toEqual([
      { nam: 2026, thang: 1 },
      { nam: 2025, thang: 12 },
    ]);
  });

  test('chỉ có mỗi lần ứng tiền, chưa chấm công buổi nào, vẫn tra lại được tháng ấy', () => {
    const duLieu = duLieuRong();
    ung(duLieu, themTho(duLieu, 'Anh Tuấn', 300_000), '2026-07-10', 500_000);

    expect(cacThangXemDuoc(duLieu, '2026-08-01')).toEqual([
      { nam: 2026, thang: 8 },
      { nam: 2026, thang: 7 },
    ]);
  });

  test('chấm nhầm sang ngày tương lai thì tháng ấy vẫn tới được, kẻo buổi công mất tăm', () => {
    const duLieu = duLieuRong();
    cham(duLieu, themTho(duLieu, 'Anh Tuấn', 300_000), '2026-11-04', 'Sang');

    expect(cacThangXemDuoc(duLieu, '2026-09-02')[0]).toEqual({
      nam: 2026,
      thang: 11,
    });
  });

  test('sổ trắng thì không có tháng nào để lùi về', () => {
    expect(cacThangXemDuoc(duLieuRong(), '2026-09-02')).toEqual([]);
  });
});
