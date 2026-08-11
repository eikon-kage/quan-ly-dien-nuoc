import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { duLieuRong } from '../../nghiepvu/kieu';
import { danhSachBan, docBan } from '../../nghiepvu/saoLuuDrive';
import { themTho } from '../../nghiepvu/thaoTac';
import { DieuKhienSaoLuu, TrangThaiSaoLuu } from '../dungSaoLuu';
import { HopSaoLuu } from '../HopSaoLuu';

jest.mock('../../nghiepvu/saoLuuDrive', () => ({
  danhSachBan: jest.fn(() => Promise.resolve([])),
  docBan: jest.fn(),
}));

const gioDanhSach = danhSachBan as jest.MockedFunction<typeof danhSachBan>;
const gioDocBan = docBan as jest.MockedFunction<typeof docBan>;

function saoLuuGia(sua: Partial<TrangThaiSaoLuu> = {}): DieuKhienSaoLuu {
  return {
    trangThai: { hoTro: true, taiKhoan: null, dangChay: false, lucCuoi: null, loi: null, ...sua },
    noiDrive: jest.fn(() => Promise.resolve()),
    ngatDrive: jest.fn(() => Promise.resolve()),
    saoLuuNgay: jest.fn(() => Promise.resolve()),
  };
}

const DA_NOI = { taiKhoan: { email: 'anh@gmail.com' } };

function dung(saoLuu: DieuKhienSaoLuu, capNhat = jest.fn()) {
  const onDong = jest.fn();
  render(<HopSaoLuu saoLuu={saoLuu} capNhat={capNhat} onDong={onDong} />);
  return { capNhat, onDong };
}

/** Bấm hộ nút trong hộp thoại xác nhận của hệ điều hành. */
function bamTrongHoiDap(nhan: string) {
  const nut = (hoi.mock.calls[0][2] ?? []).find((n) => n.text === nhan);
  nut?.onPress?.();
}

const hoi = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

beforeEach(() => {
  hoi.mockClear();
  gioDanhSach.mockReset().mockResolvedValue([]);
  gioDocBan.mockReset();
});

describe('chưa nối Drive', () => {
  test('mời nối, chưa hỏi Drive lấy danh sách làm gì', () => {
    const saoLuu = saoLuuGia();
    dung(saoLuu);

    expect(screen.getByText('Nối với Google Drive')).toBeTruthy();
    expect(gioDanhSach).not.toHaveBeenCalled();

    fireEvent.press(screen.getByText('Nối với Google Drive'));
    expect(saoLuu.noiDrive).toHaveBeenCalled();
  });
});

describe('đã nối Drive', () => {
  test('hiện email và tự tải danh sách các bản', async () => {
    gioDanhSach.mockResolvedValue([
      { id: 'f1', ngay: '2026-08-05', suaLuc: new Date(2026, 7, 5, 16, 12).toISOString() },
    ]);

    dung(saoLuuGia(DA_NOI));

    expect(screen.getByText('anh@gmail.com')).toBeTruthy();
    expect(await screen.findByText('Thứ Tư 05/08')).toBeTruthy();
    expect(screen.getByText('Ghi lúc 05/08, 16:12')).toBeTruthy();
  });

  test('chưa có bản nào thì nói rõ, khác hẳn với tải hụt', async () => {
    dung(saoLuuGia(DA_NOI));

    expect(await screen.findByText('Trên Drive chưa có bản nào.')).toBeTruthy();
  });

  test('tải danh sách hụt thì nhắc kiểm tra mạng', async () => {
    gioDanhSach.mockRejectedValue(new Error('mất mạng'));

    dung(saoLuuGia(DA_NOI));

    expect(await screen.findByText('Chưa xem được danh sách. Kiểm tra mạng rồi mở lại.')).toBeTruthy();
  });

  test('bấm Sao lưu ngay là đẩy lên luôn', async () => {
    const saoLuu = saoLuuGia(DA_NOI);
    dung(saoLuu);
    await screen.findByText('Trên Drive chưa có bản nào.');

    fireEvent.press(screen.getByText('Sao lưu ngay'));

    expect(saoLuu.saoLuuNgay).toHaveBeenCalled();
  });

  test('ngắt nối phải hỏi lại, và nói rõ dữ liệu không mất', async () => {
    const saoLuu = saoLuuGia(DA_NOI);
    dung(saoLuu);
    await screen.findByText('Trên Drive chưa có bản nào.');

    fireEvent.press(screen.getByText('Ngắt nối'));

    expect(hoi).toHaveBeenCalled();
    expect(hoi.mock.calls[0][1]).toContain('Dữ liệu trên máy vẫn còn nguyên');
    expect(saoLuu.ngatDrive).not.toHaveBeenCalled();

    bamTrongHoiDap('Ngắt nối');
    expect(saoLuu.ngatDrive).toHaveBeenCalled();
  });
});

describe('khôi phục', () => {
  const BAN = { id: 'f1', ngay: '2026-08-05', suaLuc: '2026-08-05T09:00:00Z' };

  function moiBan() {
    gioDanhSach.mockResolvedValue([BAN]);
  }

  test('hỏi lại kèm số liệu trong bản, chưa ghi đè gì', async () => {
    moiBan();
    const duLieu = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01').duLieu;
    gioDocBan.mockResolvedValue({
      duLieu,
      tomTat: { soTho: 1, soBuoiCong: 12, soUngTien: 2, soKy: 3 },
    });

    const { capNhat } = dung(saoLuuGia(DA_NOI));
    fireEvent.press(await screen.findByText('Khôi phục'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Khôi phục bản 05/08/2026?');
    expect(hoi.mock.calls[0][1]).toContain('1 thợ, 12 buổi công, 2 lần ứng tiền, 3 kỳ đã chốt');
    expect(hoi.mock.calls[0][1]).toContain('sẽ bị thay bằng bản này');

    // Mới chỉ hỏi thôi — chưa ai đồng ý thì dữ liệu trên máy phải còn nguyên.
    expect(capNhat).not.toHaveBeenCalled();
  });

  test('đồng ý rồi mới thay dữ liệu và đóng màn hình', async () => {
    moiBan();
    const duLieu = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01').duLieu;
    gioDocBan.mockResolvedValue({
      duLieu,
      tomTat: { soTho: 1, soBuoiCong: 0, soUngTien: 0, soKy: 0 },
    });

    const { capNhat, onDong } = dung(saoLuuGia(DA_NOI));
    fireEvent.press(await screen.findByText('Khôi phục'));
    await waitFor(() => expect(hoi).toHaveBeenCalled());

    bamTrongHoiDap('Khôi phục');

    expect(capNhat).toHaveBeenCalledWith(duLieu);
    expect(onDong).toHaveBeenCalled();
  });

  test('bấm Thôi thì không đụng gì tới dữ liệu', async () => {
    moiBan();
    gioDocBan.mockResolvedValue({
      duLieu: duLieuRong(),
      tomTat: { soTho: 0, soBuoiCong: 0, soUngTien: 0, soKy: 0 },
    });

    const { capNhat, onDong } = dung(saoLuuGia(DA_NOI));
    fireEvent.press(await screen.findByText('Khôi phục'));
    await waitFor(() => expect(hoi).toHaveBeenCalled());

    bamTrongHoiDap('Thôi');

    expect(capNhat).not.toHaveBeenCalled();
    expect(onDong).not.toHaveBeenCalled();
  });

  /** File trên Drive hỏng: phải báo ra chứ tuyệt đối không được ghi đè bằng dữ liệu rỗng. */
  test('bản hỏng thì báo lỗi, không ghi đè', async () => {
    moiBan();
    gioDocBan.mockRejectedValue(new Error('File này không phải bản sao lưu chấm công.'));

    const { capNhat } = dung(saoLuuGia(DA_NOI));
    fireEvent.press(await screen.findByText('Khôi phục'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Chưa lấy được bản này');
    expect(hoi.mock.calls[0][1]).toBe('File này không phải bản sao lưu chấm công.');
    expect(capNhat).not.toHaveBeenCalled();
  });
});

describe('máy không nối Drive được', () => {
  test('nói rõ vì sao thay vì hiện nút bấm không ăn', () => {
    dung(saoLuuGia({ hoTro: false }));

    expect(screen.getByText('Máy này chưa nối Drive được')).toBeTruthy();
    expect(screen.queryByText('Nối với Google Drive')).toBeNull();
  });
});
