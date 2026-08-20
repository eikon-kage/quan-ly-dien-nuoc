/**
 * Hộp "Nhóm chấm công".
 *
 * Hai điều phải giữ:
 *   1. **Máy chủ chỉ vào bằng email.** Không có đường nối nhanh ẩn danh — tài khoản chủ nắm
 *      nhóm của cả cửa hàng, mà tài khoản ẩn danh chỉ sống trong một cái điện thoại: chủ mất
 *      máy là cả nhóm mất chỗ, sổ thợ đã gửi lên nằm ở nhóm cũ không ai vào được nữa.
 *   2. **Máy thợ không có nút bấm-không-ăn.** Nó vào nhóm bằng mã mời ở hộp Máy của thợ, nên
 *      ở đây chỉ được chỉ đường sang đó.
 */

import { fireEvent, render, screen } from '@testing-library/react-native';

import { Vai } from '../../nghiepvu/soCong';
import { DieuKhienNhom } from '../dungSupabase';
import { HopNoiNhom } from '../HopNoiNhom';

function nhomGia(sua: Partial<DieuKhienNhom['trangThai']> = {}): DieuKhienNhom {
  return {
    trangThai: {
      hoTro: true,
      taiKhoan: null,
      thanhVien: null,
      dangDoc: false,
      traHut: false,
      dangChay: false,
      loi: null,
      nhac: null,
      ...sua,
    },
    noiEmail: jest.fn(() => Promise.resolve()),
    taoTaiKhoan: jest.fn(() => Promise.resolve()),
    lapNhom: jest.fn(() => Promise.resolve()),
    phatMa: jest.fn(() => Promise.resolve('K7MQP4')),
    doiMa: jest.fn(() => Promise.resolve(null)),
    ngat: jest.fn(() => Promise.resolve()),
  };
}

function dung(vai: Vai, nhom: DieuKhienNhom = nhomGia()) {
  const onDong = jest.fn();
  render(<HopNoiNhom vai={vai} dieuKhien={nhom} onDong={onDong} />);
  return { nhom, onDong };
}

const DA_DANG_NHAP_CHUA_VAO_NHOM = {
  taiKhoan: { userId: 'u1', email: 'chu@cuahang.vn', anDanh: false },
};

describe('máy chủ', () => {
  test('hỏi email và mật khẩu, kèm đường tạo tài khoản lần đầu', () => {
    dung('chu');

    expect(screen.getByPlaceholderText('chu@cuahang.vn')).toBeTruthy();
    expect(screen.getByPlaceholderText('ít nhất 6 ký tự')).toBeTruthy();
    expect(screen.getByText('Đăng nhập')).toBeTruthy();
    expect(screen.getByText('Lần đầu — tạo tài khoản')).toBeTruthy();
  });

  /** Điều 1. Thêm lại đường này là chủ mất máy thì cả nhóm mất chỗ. */
  test('không có đường nối nhanh nào bỏ qua email', () => {
    dung('chu');

    expect(screen.queryByText(/Nối nhanh/)).toBeNull();
    expect(screen.getByText(/phải là email/)).toBeTruthy();
  });

  test('bấm Đăng nhập là gửi đúng email và mật khẩu đã gõ', () => {
    const { nhom } = dung('chu');

    fireEvent.changeText(screen.getByPlaceholderText('chu@cuahang.vn'), 'chu@cuahang.vn');
    fireEvent.changeText(screen.getByPlaceholderText('ít nhất 6 ký tự'), 'matkhau123');
    fireEvent.press(screen.getByText('Đăng nhập'));

    expect(nhom.noiEmail).toHaveBeenCalledWith('chu@cuahang.vn', 'matkhau123');
  });

  test('đăng nhập rồi mà chưa có nhóm thì có nút lập nhóm để thử lại', () => {
    const { nhom } = dung('chu', nhomGia(DA_DANG_NHAP_CHUA_VAO_NHOM));

    expect(screen.getByText('Đã đăng nhập, chưa vào nhóm')).toBeTruthy();
    fireEvent.press(screen.getByText('Lập nhóm, thử lại'));

    expect(nhom.lapNhom).toHaveBeenCalled();
  });
});

describe('máy thợ', () => {
  test('không hỏi email, chỉ đường sang chỗ dán mã mời', () => {
    dung('tho');

    expect(screen.queryByPlaceholderText('chu@cuahang.vn')).toBeNull();
    expect(screen.getByText(/Máy của thợ · đổi lại/)).toBeTruthy();
  });

  /** Điều 2: bản trước có nút "Đợi mã mời của chủ" bấm không ăn. */
  test('đã có tài khoản mà chưa vào nhóm thì chỉ đường, không dựng nút bấm không ăn', () => {
    dung(
      'tho',
      nhomGia({ taiKhoan: { userId: 'u2', email: null, anDanh: true } }),
    );

    expect(screen.getByText('Đã đăng nhập, chưa vào nhóm')).toBeTruthy();
    expect(screen.getByText('Tài khoản ẩn danh của máy này')).toBeTruthy();
    expect(screen.queryByText('Đợi mã mời của chủ')).toBeNull();
    expect(screen.getByText(/Xin chủ phát mã mời/)).toBeTruthy();
  });
});

describe('app chưa điền cấu hình', () => {
  test('nói rõ phải làm gì thay vì hiện ô nhập không dùng được', () => {
    dung('chu', nhomGia({ hoTro: false }));

    expect(screen.queryByPlaceholderText('chu@cuahang.vn')).toBeNull();
    expect(screen.getByText(/chưa được điền địa chỉ nhóm/)).toBeTruthy();
  });
});
