/**
 * Đăng nhập vào nhóm chấm công trên Supabase.
 *
 * Hai kiểu đăng nhập, cố ý khác nhau:
 *
 *   Chủ  — email và mật khẩu. Mất máy thì cài máy mới, đăng nhập lại là còn nguyên quyền
 *          của cả nhóm. Đây là tài khoản nắm sổ, không được để nó chỉ tồn tại trong một
 *          cái điện thoại.
 *   Thợ  — **ẩn danh**. Thợ không phải nhớ email, không phải nhớ mật khẩu, không phải chờ
 *          mã OTP: mở app, dán mã mời, xong. Máy tự giữ một tài khoản ẩn danh trong
 *          SecureStore. Đổi lại là mất máy thì mất tài khoản ấy — không sao, chủ phát lại
 *          mã mời, sổ công của thợ thì vẫn nằm trong sổ chủ.
 *
 * Mọi câu báo lỗi ở đây viết cho người dùng đọc, không phải cho lập trình viên: người bấm
 * nút là chủ cửa hàng và thợ xây, "AuthApiError: Invalid login credentials" với họ là vô nghĩa.
 */

import { ChuaCauHinh, boKhachDangGiu, hoTro, khach } from './khachSupabase';

export interface TaiKhoanNhom {
  userId: string;
  /** Thợ đăng nhập ẩn danh thì không có email. */
  email: string | null;
  anDanh: boolean;
}

/** Lỗi đã dịch sẵn thành câu tiếng Việt để hiện thẳng lên màn hình. */
export class LoiDangNhap extends Error {
  constructor(
    thongDiep: string,
    /** Câu gốc của Supabase, giữ lại để còn lần ra nguyên nhân khi cần. */
    readonly goc?: string,
  ) {
    super(thongDiep);
  }
}

export { ChuaCauHinh };

/**
 * Dịch lỗi của Supabase sang câu người dùng hiểu.
 *
 * Nhận diện theo *chữ trong câu báo lỗi* vì Supabase không phát mã lỗi ổn định cho mấy
 * trường hợp này. Không khớp được thì nói chung chung chứ đừng nói bừa — đoán sai nguyên
 * nhân còn tệ hơn nhận là không biết.
 */
function dich(loi: unknown): LoiDangNhap {
  const goc = loi instanceof Error ? loi.message : String(loi);
  const chu = goc.toLowerCase();

  if (chu.includes('invalid login credentials')) {
    return new LoiDangNhap('Email hoặc mật khẩu không đúng.', goc);
  }
  if (chu.includes('anonymous sign-ins are disabled')) {
    return new LoiDangNhap(
      'Nhóm chưa bật đăng nhập ẩn danh. Chủ vào Supabase → Authentication → Providers để bật.',
      goc,
    );
  }
  if (chu.includes('user already registered') || chu.includes('already been registered')) {
    return new LoiDangNhap('Email này đã có tài khoản. Bấm Đăng nhập thay vì Tạo tài khoản.', goc);
  }
  if (chu.includes('password should be at least')) {
    return new LoiDangNhap('Mật khẩu quá ngắn, cần ít nhất 6 ký tự.', goc);
  }
  if (chu.includes('email rate limit') || chu.includes('too many requests')) {
    return new LoiDangNhap('Thử lại quá nhiều lần. Đợi vài phút rồi làm lại.', goc);
  }
  if (chu.includes('network') || chu.includes('fetch') || chu.includes('timeout')) {
    return new LoiDangNhap('Không nối được mạng. Kiểm tra 3G hay wifi rồi thử lại.', goc);
  }

  return new LoiDangNhap('Chưa nối được nhóm chấm công. Thử lại sau.', goc);
}

/**
 * Chạy một lệnh Supabase và **dịch cả hai đường lỗi**.
 *
 * Thư viện báo lỗi theo hai kiểu khác nhau: lỗi nghiệp vụ (sai mật khẩu, chưa bật ẩn danh)
 * thì trả trong trường `error` của kết quả, còn lỗi mạng thì **quăng** ra ngoài. Chỉ xử lý
 * một đường là mất mạng sẽ hiện nguyên câu "Network request failed" giữa màn hình — mà đó
 * lại là lỗi hay gặp nhất ở công trường.
 *
 * `ChuaCauHinh` thì để nguyên, không dịch: đó không phải lỗi của người dùng, và bên gọi cần
 * phân biệt được để mà ẩn cả phần nhóm đi.
 */
async function thu<T>(viec: () => Promise<T>): Promise<T> {
  try {
    return await viec();
  } catch (loi) {
    if (loi instanceof ChuaCauHinh || loi instanceof LoiDangNhap) {
      throw loi;
    }
    throw dich(loi);
  }
}

function doiSang(nguoi: { id: string; email?: string | null; is_anonymous?: boolean }): TaiKhoanNhom {
  return {
    userId: nguoi.id,
    email: nguoi.email ?? null,
    // Supabase đánh dấu tài khoản ẩn danh bằng `is_anonymous`. Bản cũ không có cờ này thì
    // suy ra từ chỗ không có email.
    anDanh: nguoi.is_anonymous ?? nguoi.email == null,
  };
}

/** Tài khoản đang đăng nhập trên máy này, chưa đăng nhập thì null. */
export async function taiKhoanDaLuu(): Promise<TaiKhoanNhom | null> {
  try {
    const { data } = await khach().auth.getSession();
    return data.session ? doiSang(data.session.user) : null;
  } catch (loi) {
    // Chưa cấu hình thì coi như chưa đăng nhập, đừng để lỗi này nổi lên giữa màn hình.
    if (loi instanceof ChuaCauHinh) {
      return null;
    }
    throw dich(loi);
  }
}

/** Máy thợ: xin một tài khoản ẩn danh, không hỏi gì người dùng. */
export async function dangNhapAnDanh(): Promise<TaiKhoanNhom> {
  return thu(async () => {
    const { data, error } = await khach().auth.signInAnonymously();
    if (error || !data.user) {
      throw dich(error ?? new Error('Supabase không trả về tài khoản.'));
    }
    return doiSang(data.user);
  });
}

/** Máy chủ: đăng nhập bằng email và mật khẩu. */
export async function dangNhapEmail(email: string, matKhau: string): Promise<TaiKhoanNhom> {
  return thu(async () => {
    const { data, error } = await khach().auth.signInWithPassword({
      email: email.trim(),
      password: matKhau,
    });
    if (error || !data.user) {
      throw dich(error ?? new Error('Supabase không trả về tài khoản.'));
    }
    return doiSang(data.user);
  });
}

/**
 * Máy chủ: tạo tài khoản mới.
 *
 * Nếu project bật xác nhận email thì Supabase gửi thư và **chưa** trả về phiên — nên trả về
 * null để giao diện biết mà nói "mở mail bấm xác nhận rồi quay lại đăng nhập", chứ không
 * đứng im như vừa thất bại.
 */
export async function dangKyEmail(email: string, matKhau: string): Promise<TaiKhoanNhom | null> {
  return thu(async () => {
    const { data, error } = await khach().auth.signUp({ email: email.trim(), password: matKhau });
    if (error) {
      throw dich(error);
    }
    return data.session && data.user ? doiSang(data.user) : null;
  });
}

export async function dangXuat(): Promise<void> {
  try {
    await khach().auth.signOut();
  } catch (loi) {
    if (!(loi instanceof ChuaCauHinh)) {
      throw dich(loi);
    }
  } finally {
    // Dựng lại khách ở lần nối sau: khách cũ còn giữ vòng tự làm mới token của phiên vừa bỏ.
    boKhachDangGiu();
  }
}

/**
 * Bật / tắt vòng tự làm mới token. Bên giao diện gọi hai hàm này theo trạng thái app —
 * xem [dungSupabase](../giaodien/dungSupabase.ts).
 *
 * Phải theo trạng thái app chứ không để chạy suốt: app nằm dưới nền mà vẫn đặt hẹn giờ gọi
 * mạng thì tốn pin, và hệ điều hành có thể giết tiến trình giữa lúc đang gọi.
 *
 * Chuyện "nghe trạng thái app" thì để bên giao diện làm, không làm ở đây: `AppState` là của
 * React Native, kéo nó vào tầng nghiệp vụ là cả tầng này thôi chạy được ngoài máy thật, mà
 * kiểm thử nghiệp vụ của app chạy bằng Node thuần cho nhanh.
 */
export function batTuLamMoiToken(): void {
  if (hoTro()) {
    khach().auth.startAutoRefresh();
  }
}

export function tatTuLamMoiToken(): void {
  if (hoTro()) {
    khach().auth.stopAutoRefresh();
  }
}

export { hoTro as hoTroNoi };
