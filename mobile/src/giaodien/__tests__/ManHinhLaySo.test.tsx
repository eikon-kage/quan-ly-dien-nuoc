/**
 * Màn hình mời lấy sổ trên tài khoản về — cái chắn ngang khi máy chưa có sổ mà tài khoản thì có.
 *
 * Ba điều phải giữ:
 *   1. **Lấy về vẫn phải hỏi kèm số liệu.** Ghi đè là ghi đè, kể cả khi máy đang trống: người
 *      dùng phải nhìn thấy mình sắp nhận bản nào.
 *   2. **Đường đi tiếp luôn có,** và nó là nút bấm được chứ không phải câu chữ an ủi. App chấm
 *      công phải chạy được cả khi người ta không muốn trả lời câu nào.
 *   3. **Chọn chấm sổ mới thì phải nói ra cái giá của nó** — bản của hôm nay trên tài khoản sẽ
 *      bị sổ máy này thay.
 */

import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { duLieuRong } from '../../nghiepvu/kieu';
import { cham, themTho } from '../../nghiepvu/thaoTac';
import { ManHinhLaySo } from '../ManHinhLaySo';
import { taiKhoanGia } from './chuanBi';

const hoi = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

/** Bấm hộ nút trong hộp thoại xác nhận của hệ điều hành. */
function bamTrongHoiDap(nhan: string) {
  const nut = (hoi.mock.calls[0][2] ?? []).find((n) => n.text === nhan);
  nut?.onPress?.();
}

const BAN = { ngay: '2026-08-19', suaLuc: new Date(2026, 7, 19, 16, 12).toISOString() };

function soCuaChu() {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  return cham(duLieu, tho.id, '2026-08-18', 'Sang');
}

function dung(docBan = jest.fn(() => Promise.resolve(soCuaChu()))) {
  const taiKhoan = taiKhoanGia({ hoTro: true, cacBan: [BAN], banChoLay: BAN });
  taiKhoan.docBan = docBan;

  const capNhat = jest.fn();
  const onDeSau = jest.fn();
  render(
    <ManHinhLaySo
      taiKhoan={taiKhoan}
      email="chu@cuahang.vn"
      capNhat={capNhat}
      onDeSau={onDeSau}
    />,
  );
  return { taiKhoan, capNhat, onDeSau };
}

beforeEach(() => {
  hoi.mockClear();
});

test('nói rõ bản nào, của tài khoản nào', () => {
  dung();

  expect(screen.getByText('Bản Thứ Tư 19/08')).toBeTruthy();
  expect(screen.getByText(/16:12 · chu@cuahang.vn/)).toBeTruthy();
});

test('lấy về thì hỏi kèm số liệu, đồng ý mới ghi xuống máy', async () => {
  const { capNhat, taiKhoan } = dung();

  fireEvent.press(screen.getByText('Lấy sổ về máy này'));

  await waitFor(() => expect(hoi).toHaveBeenCalled());
  expect(hoi.mock.calls[0][0]).toContain('19/08');
  expect(hoi.mock.calls[0][1]).toContain('1 thợ, 1 buổi công');

  // Chưa bấm gì trong hộp thì tuyệt đối chưa ghi.
  expect(capNhat).not.toHaveBeenCalled();

  bamTrongHoiDap('Lấy về');
  expect(capNhat).toHaveBeenCalledTimes(1);
  expect(capNhat.mock.calls[0][0].thos).toHaveLength(1);
  expect(taiKhoan.daTraLoi).toHaveBeenCalled();
});

test('bấm Thôi trong hộp thì không ghi gì, và vẫn còn được mời lần sau', async () => {
  const { capNhat, taiKhoan } = dung();

  fireEvent.press(screen.getByText('Lấy sổ về máy này'));
  await waitFor(() => expect(hoi).toHaveBeenCalled());
  bamTrongHoiDap('Thôi');

  expect(capNhat).not.toHaveBeenCalled();
  expect(taiKhoan.daTraLoi).not.toHaveBeenCalled();
});

test('lấy hụt thì hiện câu lỗi của kho, không ghi gì', async () => {
  const { capNhat } = dung(
    jest.fn(() => Promise.reject(new Error('Không nối được mạng. Kiểm tra 3G hay wifi rồi thử lại.'))),
  );

  fireEvent.press(screen.getByText('Lấy sổ về máy này'));

  expect(await screen.findByText(/Không nối được mạng/)).toBeTruthy();
  expect(capNhat).not.toHaveBeenCalled();
});

test('chọn chấm sổ mới: ghi nhận là đã trả lời, và nói ra cái giá của nó', () => {
  const { taiKhoan, onDeSau } = dung();

  expect(screen.getByText(/bản trên tài khoản của hôm nay sẽ bị sổ máy này thay/i)).toBeTruthy();

  fireEvent.press(screen.getByText('Máy này chấm sổ mới'));

  expect(taiKhoan.daTraLoi).toHaveBeenCalled();
  expect(onDeSau).toHaveBeenCalled();
});

/**
 * *Để sau* khác *chấm sổ mới*: nó không trả lời câu nào cả, nên không được mở đường cho lượt
 * đẩy ngầm — sổ trống mà đẩy lên là xoá đúng bản đang mời lấy về.
 */
test('để sau thì vào app mà không đánh dấu đã trả lời', () => {
  const { taiKhoan, onDeSau } = dung();

  fireEvent.press(screen.getByText('Để sau, vào chấm công đã'));

  expect(onDeSau).toHaveBeenCalled();
  expect(taiKhoan.daTraLoi).not.toHaveBeenCalled();
});
