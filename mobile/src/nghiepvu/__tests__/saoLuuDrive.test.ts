/**
 * Kiểm thử phần điều phối sao lưu. Drive và phần đăng nhập đều là hàng giả — ở đây chỉ
 * quan tâm app *quyết định* gọi cái gì, chứ không gọi mạng thật.
 */

const kho = new Map<string, string>();
jest.mock('@react-native-async-storage/async-storage', () => ({
  getItem: (khoa: string) => Promise.resolve(kho.get(khoa) ?? null),
  setItem: (khoa: string, gia: string) => {
    kho.set(khoa, gia);
    return Promise.resolve();
  },
}));

const layToken = jest.fn(() => Promise.resolve('token-1'));
const boToken = jest.fn();
jest.mock('../dangNhapGoogle', () => ({
  accessToken: () => layToken(),
  boTokenDangGiu: () => boToken(),
}));

jest.mock('../goiDrive', () => {
  // LoiDrive phải là lớp thật vì phần điều phối dùng `instanceof` để nhận ra lỗi 401.
  const that = jest.requireActual('../goiDrive');
  return {
    LoiDrive: that.LoiDrive,
    danhSach: (...tham: unknown[]) => gioDanhSach(...tham),
    taoFile: (...tham: unknown[]) => gioTaoFile(...tham),
    ghiDe: (...tham: unknown[]) => gioGhiDe(...tham),
    taiVe: (...tham: unknown[]) => gioTaiVe(...tham),
    xoa: (...tham: unknown[]) => gioXoa(...tham),
  };
});

const gioDanhSach = jest.fn();
const gioTaoFile = jest.fn();
const gioGhiDe = jest.fn();
const gioTaiVe = jest.fn();
const gioXoa = jest.fn();

import { LoiDrive } from '../goiDrive';
import { dongGoi } from '../goiSaoLuu';
import { duLieuRong } from '../kieu';
import { danhSachBan, docBan, lanCuoi, saoLuu } from '../saoLuuDrive';
import { themTho } from '../thaoTac';

const HOM_NAY = '2026-08-05';
const TEN_HOM_NAY = 'Cham-cong-2026-08-05.json';

function fileDrive(ten: string, id = ten) {
  return { id, ten, suaLuc: '2026-08-05T09:00:00Z' };
}

beforeEach(() => {
  kho.clear();
  [layToken, boToken, gioDanhSach, gioTaoFile, gioGhiDe, gioTaiVe, gioXoa].forEach((gia) =>
    gia.mockReset(),
  );

  layToken.mockResolvedValue('token-1');
  gioDanhSach.mockResolvedValue([]);
  gioTaoFile.mockResolvedValue(fileDrive(TEN_HOM_NAY, 'f-moi'));
  gioGhiDe.mockResolvedValue(fileDrive(TEN_HOM_NAY, 'f-cu'));
  gioXoa.mockResolvedValue(undefined);
});

describe('sao lưu', () => {
  test('ngày chưa có bản nào thì tạo file mới', async () => {
    const ban = await saoLuu(duLieuRong(), HOM_NAY);

    expect(gioTaoFile).toHaveBeenCalledWith('token-1', TEN_HOM_NAY, expect.any(String));
    expect(gioGhiDe).not.toHaveBeenCalled();
    expect(ban.ngay).toBe(HOM_NAY);
  });

  /**
   * Điểm mấu chốt của cách đặt tên theo ngày: sao lưu lần thứ hai trong cùng một ngày chỉ
   * ghi đè lên file hôm nay, không đẻ thêm file mới — Drive không đầy rác.
   */
  test('trong ngày sao lưu lần nữa thì ghi đè, không tạo thêm file', async () => {
    gioDanhSach.mockResolvedValue([fileDrive(TEN_HOM_NAY, 'f-cu')]);

    await saoLuu(duLieuRong(), HOM_NAY);

    expect(gioGhiDe).toHaveBeenCalledWith('token-1', 'f-cu', expect.any(String));
    expect(gioTaoFile).not.toHaveBeenCalled();
  });

  test('bản hôm qua vẫn còn nguyên, không bị đè', async () => {
    gioDanhSach.mockResolvedValue([fileDrive('Cham-cong-2026-08-04.json', 'f-hom-qua')]);

    await saoLuu(duLieuRong(), HOM_NAY);

    expect(gioTaoFile).toHaveBeenCalled();
    expect(gioGhiDe).not.toHaveBeenCalled();
    expect(gioXoa).not.toHaveBeenCalled();
  });

  test('đẩy lên đúng dữ liệu đang có', async () => {
    const duLieu = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY).duLieu;

    await saoLuu(duLieu, HOM_NAY);

    const noiDung = gioTaoFile.mock.calls[0][2] as string;
    expect(JSON.parse(noiDung).duLieu).toEqual(duLieu);
  });

  test('ghi nhận lần sao lưu cuối để màn hình hiện lên', async () => {
    expect(await lanCuoi()).toBeNull();

    await saoLuu(duLieuRong(), HOM_NAY);

    expect(await lanCuoi()).not.toBeNull();
  });
});

describe('dọn bản cũ', () => {
  function nhieuBan(so: number) {
    // Đếm ngược từ 2026-08-05 về trước, mỗi ngày một file.
    return Array.from({ length: so }, (_, i) => {
      const ngay = new Date(Date.UTC(2026, 7, 5 - i)).toISOString().slice(0, 10);
      return fileDrive(`Cham-cong-${ngay}.json`, `f-${i}`);
    });
  }

  test('dưới 30 bản thì không xoá gì', async () => {
    gioDanhSach.mockResolvedValue(nhieuBan(30));

    await saoLuu(duLieuRong(), HOM_NAY);

    expect(gioXoa).not.toHaveBeenCalled();
  });

  test('quá 30 bản thì xoá bớt bản cũ nhất, giữ đúng 30 bản mới', async () => {
    gioDanhSach.mockResolvedValue(nhieuBan(33));

    await saoLuu(duLieuRong(), HOM_NAY);

    expect(gioXoa).toHaveBeenCalledTimes(3);
    expect(gioXoa.mock.calls.map((goi) => goi[1])).toEqual(['f-30', 'f-31', 'f-32']);
  });

  /** Dọn dẹp là việc phụ. Hỏng thì kệ, đừng báo "chưa sao lưu được" trong khi đã lên rồi. */
  test('xoá hụt thì việc sao lưu vẫn coi như xong', async () => {
    gioDanhSach.mockResolvedValue(nhieuBan(33));
    gioXoa.mockRejectedValue(new LoiDrive(403, 'không cho xoá'));

    await expect(saoLuu(duLieuRong(), HOM_NAY)).resolves.toMatchObject({ ngay: HOM_NAY });
  });
});

describe('danh sách bản sao lưu', () => {
  test('bỏ qua file không đúng khuôn tên', async () => {
    gioDanhSach.mockResolvedValue([
      fileDrive('Cham-cong-2026-08-05.json'),
      fileDrive('Ghi chu cua toi.json'),
      fileDrive('Cham-cong-05-08-2026.xlsx'),
    ]);

    const cacBan = await danhSachBan();

    expect(cacBan.map((ban) => ban.ngay)).toEqual(['2026-08-05']);
  });

  test('mới nhất đứng đầu', async () => {
    gioDanhSach.mockResolvedValue([
      fileDrive('Cham-cong-2026-07-31.json'),
      fileDrive('Cham-cong-2026-08-05.json'),
      fileDrive('Cham-cong-2026-08-01.json'),
    ]);

    const cacBan = await danhSachBan();

    expect(cacBan.map((ban) => ban.ngay)).toEqual(['2026-08-05', '2026-08-01', '2026-07-31']);
  });
});

describe('đọc một bản về', () => {
  test('mở gói ra dữ liệu kèm mấy con số để hỏi lại người dùng', async () => {
    const duLieu = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY).duLieu;
    gioTaiVe.mockResolvedValue(dongGoi(duLieu, '2026-08-05T09:00:00.000Z'));

    const { duLieu: daDoc, tomTat } = await docBan('f1');

    expect(daDoc).toEqual(duLieu);
    expect(tomTat).toEqual({ soTho: 1, soBuoiCong: 0, soUngTien: 0, soKy: 0 });
  });

  test('file trên Drive hỏng thì báo lỗi chứ không trả dữ liệu rỗng', async () => {
    gioTaiVe.mockResolvedValue('{"app":"app-khac"}');

    await expect(docBan('f1')).rejects.toThrow(/không phải bản sao lưu/);
  });
});

describe('token hết hạn giữa chừng', () => {
  /**
   * Người dùng thu hồi quyền bên trang Tài khoản Google: token trong tay mình vẫn còn hạn
   * trên giấy tờ nên không tự biết mà làm mới, phải đợi Drive trả 401 mới biết.
   */
  test('gặp 401 thì lấy token mới rồi thử lại', async () => {
    layToken.mockResolvedValueOnce('token-cu').mockResolvedValueOnce('token-moi');
    gioDanhSach.mockRejectedValueOnce(new LoiDrive(401, 'Invalid Credentials'));
    gioDanhSach.mockResolvedValue([]);

    await danhSachBan();

    expect(boToken).toHaveBeenCalled();
    expect(gioDanhSach).toHaveBeenNthCalledWith(1, 'token-cu');
    expect(gioDanhSach).toHaveBeenNthCalledWith(2, 'token-moi');
  });

  test('thử lại vẫn 401 thì chịu, không quay vòng mãi', async () => {
    gioDanhSach.mockRejectedValue(new LoiDrive(401, 'Invalid Credentials'));

    await expect(danhSachBan()).rejects.toMatchObject({ ma: 401 });
    expect(gioDanhSach).toHaveBeenCalledTimes(2);
  });

  test('lỗi khác 401 thì không thử lại — mạng hỏng thì thử ngay cũng hỏng', async () => {
    gioDanhSach.mockRejectedValue(new LoiDrive(500, 'Backend Error'));

    await expect(danhSachBan()).rejects.toMatchObject({ ma: 500 });
    expect(gioDanhSach).toHaveBeenCalledTimes(1);
    expect(boToken).not.toHaveBeenCalled();
  });
});
