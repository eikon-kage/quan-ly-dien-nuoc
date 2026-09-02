import { CONG_MOT_BUOI, DuLieuChamCong, duLieuRong } from '../kieu';
import { quyetToan } from '../ky';
import {
  boCham,
  cham,
  dangCham,
  datCong,
  datGhiChuNgay,
  demCuaTho,
  ghiChuNgay,
  luuTho,
  tatCaTho,
  themTho,
  suaUng,
  themUng,
  thoDangLam,
  xoaTho,
  xoaUng,
} from '../thaoTac';

const NGAY_LAM = '2026-08-03';

function khoCoTho(ten = 'Anh Tuấn', tienMotCong = 300_000) {
  const { duLieu, tho } = themTho(duLieuRong(), ten, tienMotCong, NGAY_LAM);
  return { duLieu, tho };
}

describe('chấm công', () => {
  test('mỗi buổi một dòng, mặc định nửa công — cả ngày mới là một công', () => {
    const { duLieu, tho } = khoCoTho();

    let sau: DuLieuChamCong = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    sau = cham(sau, tho.id, NGAY_LAM, 'Chieu');

    expect(sau.buoiCongs).toHaveLength(2);
    expect(sau.buoiCongs.every((b) => b.soCong === CONG_MOT_BUOI)).toBe(true);
    expect(sau.buoiCongs.reduce((tong, b) => tong + b.soCong, 0)).toBe(1);
  });

  test('chấm lại cùng buổi thì sửa dòng cũ chứ không thêm dòng mới', () => {
    const { duLieu, tho } = khoCoTho();

    const lanDau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    const lanSau = cham(lanDau, tho.id, NGAY_LAM, 'Sang', 0.5, 'về sớm');

    expect(lanSau.buoiCongs).toHaveLength(1);
    expect(lanSau.buoiCongs[0].id).toBe(lanDau.buoiCongs[0].id);
    expect(lanSau.buoiCongs[0].soCong).toBe(0.5);
    expect(lanSau.buoiCongs[0].ghiChu).toBe('về sớm');
  });

  test('không chụp giá vào buổi công — giá lấy theo mốc lương của thợ', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');

    // Chụp giá vào đây thì sửa mốc lương sau này sẽ không tính lại được.
    expect(sau.buoiCongs[0].tienMotCong).toBeNull();
  });

  test('buổi vốn có giá riêng thì chấm lại vẫn giữ giá riêng đó', () => {
    const { duLieu, tho } = khoCoTho();
    const coGiaRieng = {
      ...cham(duLieu, tho.id, NGAY_LAM, 'Sang'),
    };
    coGiaRieng.buoiCongs[0].tienMotCong = 500_000;

    const sau = cham(coGiaRieng, tho.id, NGAY_LAM, 'Sang', 0.5);

    expect(sau.buoiCongs[0].tienMotCong).toBe(500_000);
    expect(sau.buoiCongs[0].soCong).toBe(0.5);
  });

  test('số công không dương thì báo lỗi', () => {
    const { duLieu, tho } = khoCoTho();

    expect(() => cham(duLieu, tho.id, NGAY_LAM, 'Sang', 0)).toThrow();
  });

  test('thợ không có trong danh sách thì báo lỗi', () => {
    expect(() => cham(duLieuRong(), 'khong-co', NGAY_LAM, 'Sang')).toThrow();
  });

  test('dữ liệu cũ không bị sửa tại chỗ', () => {
    const { duLieu, tho } = khoCoTho();

    cham(duLieu, tho.id, NGAY_LAM, 'Sang');

    expect(duLieu.buoiCongs).toHaveLength(0);
  });
});

describe('bỏ chấm', () => {
  test('xoá đúng buổi đó thôi', () => {
    const { duLieu, tho } = khoCoTho();
    let sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    sau = cham(sau, tho.id, NGAY_LAM, 'Chieu');

    sau = boCham(sau, tho.id, NGAY_LAM, 'Sang');

    expect(dangCham(sau, tho.id, NGAY_LAM, 'Sang')).toBeUndefined();
    expect(dangCham(sau, tho.id, NGAY_LAM, 'Chieu')).toBeDefined();
  });

  test('buổi chưa chấm thì dữ liệu giữ nguyên', () => {
    const { duLieu, tho } = khoCoTho();

    expect(boCham(duLieu, tho.id, NGAY_LAM, 'Sang').buoiCongs).toHaveLength(0);
  });

  test('chấm ngày này không đụng tới ngày khác', () => {
    const { duLieu, tho } = khoCoTho();
    let sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    sau = cham(sau, tho.id, '2026-08-04', 'Sang');

    sau = boCham(sau, tho.id, NGAY_LAM, 'Sang');

    expect(dangCham(sau, tho.id, '2026-08-04', 'Sang')).toBeDefined();
  });
});

describe('datCong', () => {
  test('null nghĩa là cho nghỉ buổi đó', () => {
    const { duLieu, tho } = khoCoTho();
    const sau = cham(duLieu, tho.id, NGAY_LAM, 'Sang');

    expect(datCong(sau, tho.id, NGAY_LAM, 'Sang', null).buoiCongs).toHaveLength(0);
  });

  test('số công lẻ ghi được', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = datCong(duLieu, tho.id, NGAY_LAM, 'Chieu', 1.5);

    expect(dangCham(sau, tho.id, NGAY_LAM, 'Chieu')?.soCong).toBe(1.5);
  });
});

describe('danh sách thợ', () => {
  test('thoDangLam bỏ qua thợ đã nghỉ', () => {
    let { duLieu } = khoCoTho('Anh Tuấn');
    const them = themTho(duLieu, 'Anh Bình', 280_000, NGAY_LAM);
    duLieu = luuTho(them.duLieu, { ...them.tho, dangLam: false });

    const danhSach = thoDangLam(duLieu);

    expect(danhSach).toHaveLength(1);
    expect(danhSach[0].ten).toBe('Anh Tuấn');
  });

  test('tatCaTho xếp người đang làm lên trước', () => {
    let { duLieu } = khoCoTho('Anh Tuấn');
    const them = themTho(duLieu, 'Anh Bình', 280_000, NGAY_LAM);
    duLieu = luuTho(them.duLieu, { ...them.tho, dangLam: false });

    expect(tatCaTho(duLieu).map((t) => t.ten)).toEqual(['Anh Tuấn', 'Anh Bình']);
  });

  test('thoDangLam xếp theo tên', () => {
    let { duLieu } = khoCoTho('Anh Tuấn');
    duLieu = themTho(duLieu, 'Anh Bình', 280_000, NGAY_LAM).duLieu;

    expect(thoDangLam(duLieu).map((t) => t.ten)).toEqual(['Anh Bình', 'Anh Tuấn']);
  });

  test('themTho cắt khoảng trắng thừa ở tên', () => {
    const { tho } = themTho(duLieuRong(), '  Anh Tuấn  ', 300_000, NGAY_LAM);

    expect(tho.ten).toBe('Anh Tuấn');
  });
});

describe('ứng tiền', () => {
  test('ghi được một lần ứng', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = themUng(duLieu, tho.id, NGAY_LAM, 500_000, 'ứng đổ xăng');

    expect(sau.ungTiens).toHaveLength(1);
    expect(sau.ungTiens[0].soTien).toBe(500_000);
    expect(sau.ungTiens[0].ghiChu).toBe('ứng đổ xăng');
  });

  test('số tiền không dương thì báo lỗi', () => {
    const { duLieu, tho } = khoCoTho();

    expect(() => themUng(duLieu, tho.id, NGAY_LAM, 0)).toThrow();
  });

  test('sửa lại được số tiền, ngày và ghi chú của một lần ứng', () => {
    const { duLieu, tho } = khoCoTho();
    const daUng = themUng(duLieu, tho.id, NGAY_LAM, 5_000_000, 'ứng đổ xăng');
    const ungId = daUng.ungTiens[0].id;

    // Ghi muộn một hôm nên ngày lệch, mà số tiền thì thừa một số 0.
    const sau = suaUng(daUng, ungId, '2026-08-02', 500_000, '  ứng đổ xăng  ');

    expect(sau.ungTiens).toHaveLength(1);
    expect(sau.ungTiens[0].id).toBe(ungId);
    expect(sau.ungTiens[0].ngay).toBe('2026-08-02');
    expect(sau.ungTiens[0].soTien).toBe(500_000);
    expect(sau.ungTiens[0].ghiChu).toBe('ứng đổ xăng');
    // Giữ nguyên thợ: sửa ứng không phải là chuyển tiền sang người khác.
    expect(sau.ungTiens[0].thoId).toBe(tho.id);
  });

  test('sửa lần này không đụng vào lần ứng khác', () => {
    const { duLieu, tho } = khoCoTho();
    const lanDau = themUng(duLieu, tho.id, NGAY_LAM, 500_000, 'ứng đổ xăng');
    const lanHai = themUng(lanDau, tho.id, '2026-08-20', 200_000, 'ứng mua thuốc');

    const sau = suaUng(lanHai, lanHai.ungTiens[0].id, NGAY_LAM, 300_000, 'ứng đổ xăng');

    expect(sau.ungTiens).toHaveLength(2);
    expect(sau.ungTiens[1].soTien).toBe(200_000);
    expect(sau.ungTiens[1].ghiChu).toBe('ứng mua thuốc');
  });

  test('sửa thành số tiền không dương thì báo lỗi, dữ liệu giữ nguyên', () => {
    const { duLieu, tho } = khoCoTho();
    const daUng = themUng(duLieu, tho.id, NGAY_LAM, 500_000);

    expect(() => suaUng(daUng, daUng.ungTiens[0].id, NGAY_LAM, 0)).toThrow();
    expect(daUng.ungTiens[0].soTien).toBe(500_000);
  });

  test('xoá hẳn một lần ứng ghi nhầm', () => {
    const { duLieu, tho } = khoCoTho();
    const lanDau = themUng(duLieu, tho.id, NGAY_LAM, 500_000);
    const lanHai = themUng(lanDau, tho.id, NGAY_LAM, 500_000, 'lỡ ghi hai lần');

    const sau = xoaUng(lanHai, lanHai.ungTiens[1].id);

    expect(sau.ungTiens).toHaveLength(1);
    expect(sau.ungTiens[0].id).toBe(lanHai.ungTiens[0].id);
  });

  test('ứng đã nằm trong kỳ đã chốt thì không sửa cũng không xoá được', () => {
    const { duLieu, tho } = khoCoTho();
    const daUng = themUng(duLieu, tho.id, NGAY_LAM, 500_000);
    const ungId = daUng.ungTiens[0].id;

    // Kỳ chốt nhớ theo id, nên chỉ cần kỳ ấy có nhắc tới lần ứng này.
    const daChot: DuLieuChamCong = {
      ...daUng,
      kyLuongs: [
        {
          id: 'ky1',
          tuNgay: NGAY_LAM,
          denNgay: NGAY_LAM,
          chotLuc: '2026-08-03T10:00:00.000Z',
          ghiChu: '',
          dongs: [],
          buoiCongIds: [],
          ungTienIds: [ungId],
        },
      ],
    };

    // Tiền đã trao tay theo tờ quyết toán rồi; sửa vào đây là sổ nói khác tờ ấy.
    expect(() => suaUng(daChot, ungId, NGAY_LAM, 100_000)).toThrow();
    expect(() => xoaUng(daChot, ungId)).toThrow();
  });
});

describe('ghi chú cho một ngày của một thợ', () => {
  test('chưa ghi gì thì là chuỗi rỗng', () => {
    const { duLieu, tho } = khoCoTho();

    expect(ghiChuNgay(duLieu, tho.id, NGAY_LAM)).toBe('');
  });

  test('ghi rồi đọc lại được, và chữ hai đầu bị cắt', () => {
    const { duLieu, tho } = khoCoTho();

    const sau = datGhiChuNgay(duLieu, tho.id, NGAY_LAM, '  về sớm đi đám cưới  ');

    expect(ghiChuNgay(sau, tho.id, NGAY_LAM)).toBe('về sớm đi đám cưới');
    expect(sau.ghiChuNgays).toHaveLength(1);
  });

  test('ghi lần nữa là đè lên chữ cũ, không thêm dòng mới', () => {
    const { duLieu, tho } = khoCoTho();

    const lanDau = datGhiChuNgay(duLieu, tho.id, NGAY_LAM, 'nghỉ đau chân');
    const lanSau = datGhiChuNgay(lanDau, tho.id, NGAY_LAM, 'đi khám rồi về làm chiều');

    expect(lanSau.ghiChuNgays).toHaveLength(1);
    expect(ghiChuNgay(lanSau, tho.id, NGAY_LAM)).toBe('đi khám rồi về làm chiều');
  });

  test('xoá hết chữ là xoá luôn bản ghi, không để lại dòng rỗng', () => {
    const { duLieu, tho } = khoCoTho();

    const daGhi = datGhiChuNgay(duLieu, tho.id, NGAY_LAM, 'nghỉ đau chân');
    const daXoa = datGhiChuNgay(daGhi, tho.id, NGAY_LAM, '   ');

    expect(daXoa.ghiChuNgays).toEqual([]);
    expect(ghiChuNgay(daXoa, tho.id, NGAY_LAM)).toBe('');
  });

  test('ghi chú của ngày này không lẫn sang ngày khác hay thợ khác', () => {
    const { duLieu, tho } = khoCoTho();
    const themNguoi = themTho(duLieu, 'Anh Bình', 300_000, NGAY_LAM);

    const sau = datGhiChuNgay(themNguoi.duLieu, tho.id, NGAY_LAM, 'nghỉ đau chân');

    expect(ghiChuNgay(sau, tho.id, '2026-08-04')).toBe('');
    expect(ghiChuNgay(sau, themNguoi.tho.id, NGAY_LAM)).toBe('');
  });

  test('ghi chú được cả ngày nghỉ hẳn, và bỏ chấm không kéo nó đi theo', () => {
    const { duLieu, tho } = khoCoTho();

    // Đây là chỗ `BuoiCong.ghiChu` không làm được: không có buổi nào để treo chữ vào.
    const nghiHan = datGhiChuNgay(duLieu, tho.id, NGAY_LAM, 'nghỉ đám cưới em gái');
    expect(ghiChuNgay(nghiHan, tho.id, NGAY_LAM)).toBe('nghỉ đám cưới em gái');

    const daCham = cham(nghiHan, tho.id, NGAY_LAM, 'Sang');
    const boRoi = boCham(daCham, tho.id, NGAY_LAM, 'Sang');

    expect(ghiChuNgay(boRoi, tho.id, NGAY_LAM)).toBe('nghỉ đám cưới em gái');
  });
});

describe('xoá thợ', () => {
  test('xoá thợ gõ nhầm, chưa có gì trong sổ', () => {
    const { duLieu, tho } = khoCoTho();
    const themNguoi = themTho(duLieu, 'Anh Tuân', 300_000, NGAY_LAM);

    const sau = xoaTho(themNguoi.duLieu, themNguoi.tho.id);

    expect(sau.thos).toHaveLength(1);
    expect(sau.thos[0].id).toBe(tho.id);
  });

  test('xoá kéo theo buổi công, lần ứng và ghi chú của đúng người ấy', () => {
    let { duLieu, tho } = khoCoTho();
    const themNguoi = themTho(duLieu, 'Anh Bình', 300_000, NGAY_LAM);
    duLieu = themNguoi.duLieu;
    const binh = themNguoi.tho.id;

    duLieu = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    duLieu = themUng(duLieu, tho.id, NGAY_LAM, 500_000);
    duLieu = datGhiChuNgay(duLieu, tho.id, NGAY_LAM, 'về sớm');
    duLieu = cham(duLieu, binh, NGAY_LAM, 'Sang');
    duLieu = themUng(duLieu, binh, NGAY_LAM, 200_000);
    duLieu = datGhiChuNgay(duLieu, binh, NGAY_LAM, 'nghỉ đau chân');

    const sau = xoaTho(duLieu, tho.id);

    // Người bị xoá sạch bóng...
    expect(sau.buoiCongs.filter((b) => b.thoId === tho.id)).toEqual([]);
    expect(sau.ungTiens.filter((u) => u.thoId === tho.id)).toEqual([]);
    expect(sau.ghiChuNgays.filter((g) => g.thoId === tho.id)).toEqual([]);

    // ...còn người ở lại thì không suy suyển dòng nào.
    expect(sau.buoiCongs).toHaveLength(1);
    expect(sau.ungTiens).toHaveLength(1);
    expect(ghiChuNgay(sau, binh, NGAY_LAM)).toBe('nghỉ đau chân');
  });

  test('đếm đúng những gì sẽ mất, để còn hỏi lại cho rõ', () => {
    let { duLieu, tho } = khoCoTho();
    duLieu = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    duLieu = cham(duLieu, tho.id, NGAY_LAM, 'Chieu');
    duLieu = themUng(duLieu, tho.id, NGAY_LAM, 500_000);
    duLieu = datGhiChuNgay(duLieu, tho.id, NGAY_LAM, 'về sớm');

    expect(demCuaTho(duLieu, tho.id)).toEqual({
      soBuoiCong: 2,
      soUngTien: 1,
      soGhiChu: 1,
      daChot: false,
    });
  });

  test('thợ đã có tên trong kỳ đã chốt thì không xoá được', () => {
    let { duLieu, tho } = khoCoTho();
    duLieu = cham(duLieu, tho.id, NGAY_LAM, 'Sang');
    duLieu = quyetToan(duLieu, { denNgay: NGAY_LAM });

    // Tờ quyết toán cũ vẫn phải mở ra được chi tiết từng ngày; mất thợ là mở ra trắng trơn.
    expect(demCuaTho(duLieu, tho.id).daChot).toBe(true);
    expect(() => xoaTho(duLieu, tho.id)).toThrow(/kỳ đã chốt/i);

    // Cho nghỉ thì vẫn được — đó là lối ra đúng cho người nghỉ việc.
    const daNghi = luuTho(duLieu, { ...duLieu.thos[0], dangLam: false });
    expect(daNghi.thos[0].dangLam).toBe(false);
    expect(tatCaTho(daNghi)).toHaveLength(1);
  });

  test('chốt kỳ của người này không chặn xoá người khác', () => {
    let { duLieu, tho } = khoCoTho();
    const themNguoi = themTho(duLieu, 'Anh Bình', 300_000, NGAY_LAM);
    duLieu = cham(themNguoi.duLieu, tho.id, NGAY_LAM, 'Sang');
    duLieu = quyetToan(duLieu, { denNgay: NGAY_LAM });

    // Anh Bình chưa có công nào nên không có tên trong tờ quyết toán ấy.
    expect(demCuaTho(duLieu, themNguoi.tho.id).daChot).toBe(false);
    expect(xoaTho(duLieu, themNguoi.tho.id).thos).toHaveLength(1);
  });
});
