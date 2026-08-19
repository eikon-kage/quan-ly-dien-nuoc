/**
 * Đăng nhập nhóm chấm công.
 *
 * Ở đây không gọi mạng thật: chỉ kiểm app *quyết định gọi cái gì*, và kiểm lỗi của Supabase
 * có được dịch thành câu người dùng đọc được hay không. Câu báo lỗi là phần dễ bị coi nhẹ
 * nhất mà lại là phần người dùng gặp nhiều nhất — "AuthApiError: Invalid login credentials"
 * hiện giữa màn hình thì chủ cửa hàng chỉ biết gọi điện hỏi.
 */

const gioAuth = {
  getSession: jest.fn(),
  signInAnonymously: jest.fn(),
  signInWithPassword: jest.fn(),
  signUp: jest.fn(),
  signOut: jest.fn(),
  startAutoRefresh: jest.fn(),
  stopAutoRefresh: jest.fn(),
};

const boKhach = jest.fn();
let coCauHinh = true;

// Thay hẳn cả file, không lấy bản thật ra dùng lại: bản thật `import` thư viện Supabase, mà
// thư viện ấy phát hành dạng ESM nên bộ chạy kiểm thử nghiệp vụ (Node thuần) không nạp được.
jest.mock('../khachSupabase', () => {
  class ChuaCauHinh extends Error {
    constructor() {
      super('Máy này chưa được cấu hình để nối nhóm chấm công.');
    }
  }

  return {
    ChuaCauHinh,
    hoTro: () => coCauHinh,
    boKhachDangGiu: () => boKhach(),
    khach: () => {
      if (!coCauHinh) {
        throw new ChuaCauHinh();
      }
      return { auth: gioAuth };
    },
  };
});

import {
  ChuaCauHinh,
  LoiDangNhap,
  batTuLamMoiToken,
  dangKyEmail,
  dangNhapAnDanh,
  dangNhapEmail,
  dangXuat,
  taiKhoanDaLuu,
} from '../dangNhapSupabase';

const NGUOI_AN_DANH = { id: 'u-1', email: null, is_anonymous: true };
const NGUOI_CHU = { id: 'u-2', email: 'chu@cuahang.vn', is_anonymous: false };

beforeEach(() => {
  coCauHinh = true;
  [...Object.values(gioAuth), boKhach].forEach((gia) => gia.mockReset());
});

describe('taiKhoanDaLuu', () => {
  it('máy chưa cấu hình thì coi như chưa đăng nhập, không quăng lỗi giữa màn hình', async () => {
    coCauHinh = false;
    await expect(taiKhoanDaLuu()).resolves.toBeNull();
  });

  it('chưa đăng nhập thì trả null', async () => {
    gioAuth.getSession.mockResolvedValue({ data: { session: null } });
    await expect(taiKhoanDaLuu()).resolves.toBeNull();
  });

  it('đang đăng nhập thì trả về tài khoản', async () => {
    gioAuth.getSession.mockResolvedValue({ data: { session: { user: NGUOI_CHU } } });
    await expect(taiKhoanDaLuu()).resolves.toEqual({
      userId: 'u-2',
      email: 'chu@cuahang.vn',
      anDanh: false,
    });
  });
});

describe('máy thợ đăng nhập ẩn danh', () => {
  it('không hỏi người dùng gì cả', async () => {
    gioAuth.signInAnonymously.mockResolvedValue({ data: { user: NGUOI_AN_DANH }, error: null });

    await expect(dangNhapAnDanh()).resolves.toEqual({ userId: 'u-1', email: null, anDanh: true });
    expect(gioAuth.signInAnonymously).toHaveBeenCalledTimes(1);
  });

  it('project chưa bật ẩn danh thì chỉ luôn chỗ phải bật', async () => {
    gioAuth.signInAnonymously.mockResolvedValue({
      data: { user: null },
      error: new Error('Anonymous sign-ins are disabled'),
    });

    await expect(dangNhapAnDanh()).rejects.toThrow(/Authentication → Providers/);
  });
});

describe('máy chủ đăng nhập bằng email', () => {
  it('cắt khoảng trắng thừa quanh email — người dùng gõ trên điện thoại hay lỡ dấu cách', async () => {
    gioAuth.signInWithPassword.mockResolvedValue({ data: { user: NGUOI_CHU }, error: null });

    await dangNhapEmail('  chu@cuahang.vn ', 'matkhau');
    expect(gioAuth.signInWithPassword).toHaveBeenCalledWith({
      email: 'chu@cuahang.vn',
      password: 'matkhau',
    });
  });

  it('sai mật khẩu thì nói bằng tiếng Việt, giữ câu gốc để còn lần ra', async () => {
    gioAuth.signInWithPassword.mockResolvedValue({
      data: { user: null },
      error: new Error('Invalid login credentials'),
    });

    const loi = await dangNhapEmail('chu@cuahang.vn', 'sai').then(
      () => null,
      (l: unknown) => l,
    );

    expect(loi).toBeInstanceOf(LoiDangNhap);
    expect((loi as LoiDangNhap).message).toBe('Email hoặc mật khẩu không đúng.');
    // Câu gốc giữ lại để lần ra nguyên nhân, chỉ không hiện lên màn hình.
    expect((loi as LoiDangNhap).goc).toBe('Invalid login credentials');
  });

  it('mất mạng thì bảo kiểm tra mạng, không bảo sai mật khẩu', async () => {
    gioAuth.signInWithPassword.mockRejectedValue(new TypeError('Network request failed'));
    await expect(dangNhapEmail('chu@cuahang.vn', 'matkhau')).rejects.toThrow(/3G hay wifi/);
  });

  it('lỗi lạ thì nói chung chung chứ không đoán bừa nguyên nhân', async () => {
    gioAuth.signInWithPassword.mockResolvedValue({
      data: { user: null },
      error: new Error('nothing anyone has seen before'),
    });
    await expect(dangNhapEmail('a@b.vn', 'x')).rejects.toThrow('Chưa nối được nhóm chấm công. Thử lại sau.');
  });
});

describe('tạo tài khoản chủ', () => {
  it('project bắt xác nhận email thì trả null để giao diện nói tiếp, không đứng im', async () => {
    gioAuth.signUp.mockResolvedValue({ data: { user: NGUOI_CHU, session: null }, error: null });
    await expect(dangKyEmail('chu@cuahang.vn', 'matkhau')).resolves.toBeNull();
  });

  it('email đã có tài khoản thì bảo bấm Đăng nhập', async () => {
    gioAuth.signUp.mockResolvedValue({ data: {}, error: new Error('User already registered') });
    await expect(dangKyEmail('chu@cuahang.vn', 'matkhau')).rejects.toThrow(/Bấm Đăng nhập/);
  });
});

describe('đăng xuất', () => {
  it('bỏ luôn khách đang giữ, kẻo vòng làm mới token của phiên cũ còn chạy', async () => {
    gioAuth.signOut.mockResolvedValue({ error: null });

    await dangXuat();
    expect(boKhach).toHaveBeenCalledTimes(1);
  });

  it('máy chưa cấu hình thì đăng xuất vẫn êm', async () => {
    coCauHinh = false;
    await expect(dangXuat()).resolves.toBeUndefined();
  });
});

describe('tự làm mới token', () => {
  it('máy chưa cấu hình thì không chạm vào khách', () => {
    coCauHinh = false;
    batTuLamMoiToken();
    expect(gioAuth.startAutoRefresh).not.toHaveBeenCalled();
  });

  it('đã cấu hình thì bật vòng làm mới', () => {
    batTuLamMoiToken();
    expect(gioAuth.startAutoRefresh).toHaveBeenCalledTimes(1);
  });
});

test('ChuaCauHinh là lớp lỗi riêng để bên ngoài phân biệt được', () => {
  expect(new ChuaCauHinh()).toBeInstanceOf(Error);
});
