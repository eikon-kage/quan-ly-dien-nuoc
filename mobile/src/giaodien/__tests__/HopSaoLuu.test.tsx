/**
 * Màn hình Sao lưu.
 *
 * Hai điều phải giữ:
 *   1. **Khôi phục luôn phải hỏi trước, kèm số liệu trong bản.** Đây là thao tác ghi đè
 *      không lùi lại được; nuốt lặng một file là mất sạch sổ sách.
 *   2. **Câu nhắc gửi bản ra ngoài luôn có mặt.** Bản trong máy mất theo app; không nói ra
 *      thì người dùng thấy "đã sao lưu lúc 16:12" rồi tưởng mình an toàn cả khi mất máy.
 */

import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { chiaSeSaoLuu } from '../../nghiepvu/chiaSeSaoLuu';
import { chonFileSaoLuu } from '../../nghiepvu/chonFileSaoLuu';
import { dongGoi } from '../../nghiepvu/goiSaoLuu';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { danhSachBan, docBan } from '../../nghiepvu/saoLuuMay';
import { themTho } from '../../nghiepvu/thaoTac';
import { DieuKhienSaoLuu, TrangThaiSaoLuu } from '../dungSaoLuu';
import { HopSaoLuu } from '../HopSaoLuu';

// Thư mục trong máy, bảng chia sẻ và bảng chọn file đều là của điện thoại.
jest.mock('../../nghiepvu/saoLuuMay', () => ({
  danhSachBan: jest.fn(() => Promise.resolve([])),
  docBan: jest.fn(),
}));
jest.mock('../../nghiepvu/chiaSeSaoLuu', () => ({
  chiaSeSaoLuu: jest.fn(() => Promise.resolve('file:///tam/Cham-cong.json')),
}));
jest.mock('../../nghiepvu/chonFileSaoLuu', () => ({
  chonFileSaoLuu: jest.fn(),
}));

const gioDanhSach = danhSachBan as jest.MockedFunction<typeof danhSachBan>;
const gioDocBan = docBan as jest.MockedFunction<typeof docBan>;
const gioChiaSe = chiaSeSaoLuu as jest.MockedFunction<typeof chiaSeSaoLuu>;
const gioChonFile = chonFileSaoLuu as jest.MockedFunction<typeof chonFileSaoLuu>;

function saoLuuGia(sua: Partial<TrangThaiSaoLuu> = {}): DieuKhienSaoLuu {
  return {
    trangThai: { hoTro: true, dangChay: false, lucCuoi: null, loi: null, ...sua },
    saoLuuNgay: jest.fn(() => Promise.resolve()),
  };
}

const KHO = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01').duLieu;

function dung(saoLuu: DieuKhienSaoLuu, duLieu: DuLieuChamCong = KHO) {
  const capNhat = jest.fn();
  const onDong = jest.fn();
  render(<HopSaoLuu duLieu={duLieu} saoLuu={saoLuu} capNhat={capNhat} onDong={onDong} />);
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
  gioChiaSe.mockReset().mockResolvedValue('file:///tam/Cham-cong.json');
  gioChonFile.mockReset();
});

describe('bản trong máy', () => {
  test('tự tải danh sách các bản, hiện giờ ghi', async () => {
    gioDanhSach.mockResolvedValue([
      { ten: 'Cham-cong-2026-08-05.json', ngay: '2026-08-05', suaLuc: new Date(2026, 7, 5, 16, 12).toISOString() },
    ]);

    dung(saoLuuGia());

    expect(await screen.findByText('Thứ Tư 05/08')).toBeTruthy();
    expect(screen.getByText('Ghi lúc 05/08, 16:12')).toBeTruthy();
  });

  test('chưa có bản nào thì nói rõ, khác hẳn với đọc hụt', async () => {
    dung(saoLuuGia());

    expect(await screen.findByText('Trong máy chưa có bản nào.')).toBeTruthy();
  });

  test('đọc thư mục hụt thì nói rõ là chưa xem được', async () => {
    gioDanhSach.mockRejectedValue(new Error('không mở được thư mục'));

    dung(saoLuuGia());

    expect(await screen.findByText('Chưa xem được danh sách các bản trong máy.')).toBeTruthy();
  });

  test('bấm Sao lưu ngay là ghi luôn', async () => {
    const saoLuu = saoLuuGia();
    dung(saoLuu);
    await screen.findByText('Trong máy chưa có bản nào.');

    fireEvent.press(screen.getByText('Sao lưu ngay'));

    expect(saoLuu.saoLuuNgay).toHaveBeenCalled();
  });

  test('lỗi ghi thì hiện lỗi chứ không hiện giờ sao lưu cũ', async () => {
    dung(
      saoLuuGia({
        lucCuoi: new Date(2026, 7, 5, 16, 12).toISOString(),
        loi: 'Chưa ghi được bản sao lưu. Máy có thể đã hết chỗ trống.',
      }),
    );

    await screen.findByText('Trong máy chưa có bản nào.');
    expect(screen.getByText('Chưa ghi được bản sao lưu. Máy có thể đã hết chỗ trống.')).toBeTruthy();
    expect(screen.queryByText('Sao lưu lần cuối lúc 05/08, 16:12.')).toBeNull();
  });

  /** Giới hạn thật của cách sao lưu vào máy. Bỏ câu này là màn hình đang nói dối người dùng. */
  test('luôn nhắc gửi một bản ra ngoài, kể cả khi vừa sao lưu xong', async () => {
    dung(saoLuuGia({ lucCuoi: new Date(2026, 7, 5, 16, 12).toISOString() }));

    await screen.findByText('Trong máy chưa có bản nào.');
    expect(screen.getByText(/xoá app hay mất máy là mất theo/i)).toBeTruthy();
  });
});

describe('gửi bản ra ngoài', () => {
  test('đóng gói từ dữ liệu đang có', async () => {
    dung(saoLuuGia());
    await screen.findByText('Trong máy chưa có bản nào.');

    fireEvent.press(screen.getByText('Gửi bản ra ngoài'));

    await waitFor(() => expect(gioChiaSe).toHaveBeenCalledWith(KHO, Ngay.homNay()));
  });

  test('gửi hụt thì báo ra chứ không im lặng', async () => {
    gioChiaSe.mockRejectedValue(new Error('Máy này không gửi file đi được.'));

    dung(saoLuuGia());
    await screen.findByText('Trong máy chưa có bản nào.');

    fireEvent.press(screen.getByText('Gửi bản ra ngoài'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Chưa gửi được bản sao lưu');
    expect(hoi.mock.calls[0][1]).toBe('Máy này không gửi file đi được.');
  });
});

describe('khôi phục một bản trong máy', () => {
  const BAN = { ten: 'Cham-cong-2026-08-05.json', ngay: '2026-08-05', suaLuc: '2026-08-05T09:00:00Z' };

  beforeEach(() => {
    gioDanhSach.mockResolvedValue([BAN]);
  });

  test('hỏi lại kèm số liệu trong bản, chưa ghi đè gì', async () => {
    gioDocBan.mockResolvedValue(KHO);

    const { capNhat } = dung(saoLuuGia());
    fireEvent.press(await screen.findByText('Khôi phục'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Khôi phục bản 05/08/2026?');
    expect(hoi.mock.calls[0][1]).toContain('1 thợ, 0 buổi công, 0 lần ứng tiền, 0 kỳ đã chốt');
    expect(hoi.mock.calls[0][1]).toContain('sẽ bị thay bằng bản này');

    // Mới chỉ hỏi thôi — chưa ai đồng ý thì dữ liệu trên máy phải còn nguyên.
    expect(capNhat).not.toHaveBeenCalled();
  });

  test('đồng ý rồi mới thay dữ liệu và đóng màn hình', async () => {
    gioDocBan.mockResolvedValue(KHO);

    const { capNhat, onDong } = dung(saoLuuGia());
    fireEvent.press(await screen.findByText('Khôi phục'));
    await waitFor(() => expect(hoi).toHaveBeenCalled());

    bamTrongHoiDap('Khôi phục');

    expect(capNhat).toHaveBeenCalledWith(KHO);
    expect(onDong).toHaveBeenCalled();
  });

  test('bấm Thôi thì không đụng gì tới dữ liệu', async () => {
    gioDocBan.mockResolvedValue(duLieuRong());

    const { capNhat, onDong } = dung(saoLuuGia());
    fireEvent.press(await screen.findByText('Khôi phục'));
    await waitFor(() => expect(hoi).toHaveBeenCalled());

    bamTrongHoiDap('Thôi');

    expect(capNhat).not.toHaveBeenCalled();
    expect(onDong).not.toHaveBeenCalled();
  });

  /** File trong máy hỏng: phải báo ra chứ tuyệt đối không được ghi đè bằng dữ liệu rỗng. */
  test('bản hỏng thì báo lỗi, không ghi đè', async () => {
    gioDocBan.mockRejectedValue(new Error('File này không phải bản sao lưu chấm công.'));

    const { capNhat } = dung(saoLuuGia());
    fireEvent.press(await screen.findByText('Khôi phục'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Chưa lấy được bản này');
    expect(hoi.mock.calls[0][1]).toBe('File này không phải bản sao lưu chấm công.');
    expect(capNhat).not.toHaveBeenCalled();
  });
});

describe('khôi phục từ file tự chọn', () => {
  test('đọc gói rồi hỏi kèm số liệu', async () => {
    gioChonFile.mockResolvedValue(dongGoi(KHO, '2026-08-05T09:00:00Z'));

    const { capNhat, onDong } = dung(saoLuuGia());
    await screen.findByText('Trong máy chưa có bản nào.');

    fireEvent.press(screen.getByText('Khôi phục từ file'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Khôi phục từ file này?');
    expect(hoi.mock.calls[0][1]).toContain('1 thợ');
    expect(capNhat).not.toHaveBeenCalled();

    bamTrongHoiDap('Khôi phục');
    expect(capNhat).toHaveBeenCalled();
    expect(onDong).toHaveBeenCalled();
  });

  test('bấm huỷ trong bảng chọn file thì không hỏi gì cả', async () => {
    gioChonFile.mockResolvedValue(null);

    dung(saoLuuGia());
    await screen.findByText('Trong máy chưa có bản nào.');

    fireEvent.press(screen.getByText('Khôi phục từ file'));

    await waitFor(() => expect(gioChonFile).toHaveBeenCalled());
    expect(hoi).not.toHaveBeenCalled();
  });

  /** Chọn nhầm một file JSON nào đó: phải từ chối, không được nuốt vào rồi xoá sạch sổ. */
  test('file không phải bản sao lưu thì báo lỗi, không ghi đè', async () => {
    gioChonFile.mockResolvedValue(JSON.stringify({ mot: 'thu gi khac' }));

    const { capNhat } = dung(saoLuuGia());
    await screen.findByText('Trong máy chưa có bản nào.');

    fireEvent.press(screen.getByText('Khôi phục từ file'));

    await waitFor(() => expect(hoi).toHaveBeenCalled());
    expect(hoi.mock.calls[0][0]).toBe('Chưa đọc được file');
    expect(hoi.mock.calls[0][1]).toBe('File này không phải bản sao lưu chấm công.');
    expect(capNhat).not.toHaveBeenCalled();
  });
});

describe('máy không sao lưu được', () => {
  test('nói rõ vì sao thay vì hiện nút bấm không ăn', () => {
    dung(saoLuuGia({ hoTro: false }));

    expect(screen.getByText('Máy này chưa sao lưu được')).toBeTruthy();
    expect(screen.queryByText('Sao lưu ngay')).toBeNull();
  });
});
