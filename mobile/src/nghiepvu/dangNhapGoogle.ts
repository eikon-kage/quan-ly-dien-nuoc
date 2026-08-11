/**
 * Đăng nhập Google và giữ token để gọi Drive.
 *
 * Dùng luồng chuẩn cho app cài trên máy: mở trình duyệt hệ điều hành cho người dùng bấm
 * Đồng ý, Google trả về một mã dùng-một-lần, app đổi mã ấy lấy token. Có PKCE nên app
 * khác cài trên cùng máy có cướp được mã cũng không đổi ra token được.
 *
 * **Không chạy trong Expo Go.** Google chỉ chấp nhận địa chỉ trả về gắn với bundle ID
 * (iOS) hoặc tên gói (Android), mà Expo Go thì mang địa chỉ `exp://` của riêng nó. Phải
 * dựng development build.
 *
 * Refresh token nằm trong SecureStore (Keychain của iOS, Keystore của Android) chứ không
 * nằm trong AsyncStorage: AsyncStorage là file thường, máy đã root/jailbreak là đọc được,
 * mà cầm refresh token thì vào được Drive của người dùng cho tới khi họ thu hồi.
 */

import * as AuthSession from 'expo-auth-session';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

import { MAY_CHU_GOOGLE, QUYEN, clientId, daCauHinh, diaChiTraVe } from './cauHinhGoogle';

const KHOA_REFRESH = 'chamcong.google.refreshToken';
const KHOA_EMAIL = 'chamcong.google.email';

const DIA_CHI_THONG_TIN = 'https://www.googleapis.com/oauth2/v3/userinfo';

/** Máy chưa nối Drive lần nào, hoặc người dùng đã ngắt nối. */
export class ChuaDangNhap extends Error {
  constructor() {
    super('Chưa nối với Google Drive.');
  }
}

/**
 * Refresh token không còn dùng được: người dùng đổi mật khẩu, thu hồi quyền trong trang
 * Tài khoản Google, hoặc token nằm im quá sáu tháng. Chỉ có cách đăng nhập lại.
 */
export class HetPhien extends Error {
  constructor() {
    super('Kết nối Google Drive đã hết hạn, cần nối lại.');
  }
}

export interface TaiKhoan {
  email: string;
}

/**
 * Access token đang dùng, chỉ giữ trong bộ nhớ.
 *
 * Không cất xuống máy vì nó sống có một tiếng — cất xuống chỉ tổ thêm một chỗ rò rỉ,
 * mà mở app lại thì lấy cái mới từ refresh token cũng chưa tới một giây.
 */
let tokenHienTai: AuthSession.TokenResponse | null = null;

/** Web không có SecureStore, và cũng không phải chỗ app này chạy. */
export function hoTro(): boolean {
  return Platform.OS !== 'web' && daCauHinh();
}

export async function taiKhoanDaLuu(): Promise<TaiKhoan | null> {
  if (!hoTro()) {
    return null;
  }

  const refresh = await SecureStore.getItemAsync(KHOA_REFRESH);
  if (!refresh) {
    return null;
  }

  return { email: (await SecureStore.getItemAsync(KHOA_EMAIL)) ?? '' };
}

/**
 * Mở màn hình đăng nhập Google. Trả về null nếu người dùng bấm huỷ hoặc đóng trình duyệt —
 * đó không phải lỗi, đừng báo đỏ.
 */
export async function dangNhap(): Promise<TaiKhoan | null> {
  const yeuCau = await AuthSession.loadAsync(
    {
      clientId: clientId(),
      redirectUri: diaChiTraVe(),
      // Xin thêm email chỉ để hiện "đang sao lưu vào tài khoản nào" — người dùng có hai
      // tài khoản Google là chuyện thường, không hiện ra thì họ không biết bản sao lưu
      // nằm ở đâu.
      scopes: ['openid', 'email', ...QUYEN],
      usePKCE: true,
      // Bắt buộc để Google chịu phát refresh token, không có thì mỗi tiếng lại phải đăng
      // nhập tay một lần.
      prompt: AuthSession.Prompt.Consent,
      extraParams: { access_type: 'offline' },
    },
    MAY_CHU_GOOGLE,
  );

  const ketQua = await yeuCau.promptAsync(MAY_CHU_GOOGLE);
  if (ketQua.type !== 'success') {
    return null;
  }

  const token = await AuthSession.exchangeCodeAsync(
    {
      clientId: clientId(),
      code: ketQua.params.code,
      redirectUri: diaChiTraVe(),
      // PKCE: gửi kèm chuỗi bí mật đã dùng để tạo code_challenge lúc mở trình duyệt.
      extraParams: { code_verifier: yeuCau.codeVerifier ?? '' },
    },
    MAY_CHU_GOOGLE,
  );

  tokenHienTai = token;

  if (!token.refreshToken) {
    // Hiếm, nhưng nếu rơi vào thì lần mở app sau sẽ mất kết nối mà không hiểu vì sao.
    throw new Error('Google không cấp refresh token. Hãy thử ngắt nối rồi nối lại.');
  }
  await SecureStore.setItemAsync(KHOA_REFRESH, token.refreshToken);

  const email = await layEmail(token.accessToken);
  await SecureStore.setItemAsync(KHOA_EMAIL, email);

  return { email };
}

/**
 * Vứt access token đang giữ để lần gọi sau lấy cái mới.
 *
 * Dùng khi Drive trả 401 dù token trên giấy tờ vẫn còn hạn — người dùng vừa thu hồi quyền
 * bên trang Tài khoản Google chẳng hạn. Máy mình không có cách nào biết trước việc đó.
 */
export function boTokenDangGiu(): void {
  tokenHienTai = null;
}

/** Ngắt nối: thu hồi token bên Google rồi xoá sạch dấu vết trên máy. */
export async function dangXuat(): Promise<void> {
  const refresh = await SecureStore.getItemAsync(KHOA_REFRESH);

  if (refresh) {
    try {
      await AuthSession.revokeAsync(
        { clientId: clientId(), token: refresh, tokenTypeHint: AuthSession.TokenTypeHint.RefreshToken },
        MAY_CHU_GOOGLE,
      );
    } catch {
      // Thu hồi hụt (mất mạng chẳng hạn) thì vẫn phải xoá trên máy: người dùng đã bảo
      // ngắt là ngắt. Token còn sống bên Google nhưng máy này không còn giữ nó nữa.
    }
  }

  tokenHienTai = null;
  await SecureStore.deleteItemAsync(KHOA_REFRESH);
  await SecureStore.deleteItemAsync(KHOA_EMAIL);
}

/**
 * Lấy access token còn hạn để gọi Drive. Tự làm mới khi sắp hết hạn.
 *
 * Ném `ChuaDangNhap` nếu máy chưa nối, `HetPhien` nếu refresh token đã chết — hai việc
 * này người dùng phải xử lý khác nhau nên phân biệt ra chứ không gộp thành một lỗi chung.
 */
export async function accessToken(): Promise<string> {
  if (tokenHienTai && !tokenHienTai.shouldRefresh()) {
    return tokenHienTai.accessToken;
  }

  const refresh = await SecureStore.getItemAsync(KHOA_REFRESH);
  if (!refresh) {
    throw new ChuaDangNhap();
  }

  try {
    tokenHienTai = await AuthSession.refreshAsync(
      { clientId: clientId(), refreshToken: refresh },
      MAY_CHU_GOOGLE,
    );
  } catch (loi) {
    // Mất mạng thì để lỗi mạng nổi lên nguyên vẹn — lần sau có mạng là chạy lại được,
    // xoá token lúc này là bắt người dùng đăng nhập lại oan.
    if (mangHong(loi)) {
      throw loi;
    }
    await SecureStore.deleteItemAsync(KHOA_REFRESH);
    tokenHienTai = null;
    throw new HetPhien();
  }

  // Google thỉnh thoảng phát refresh token mới; phát thì thay, không phát thì giữ cái cũ.
  if (tokenHienTai.refreshToken && tokenHienTai.refreshToken !== refresh) {
    await SecureStore.setItemAsync(KHOA_REFRESH, tokenHienTai.refreshToken);
  }

  return tokenHienTai.accessToken;
}

/**
 * Phân biệt "mạng hỏng" với "Google từ chối".
 *
 * expo-auth-session ném TokenError khi máy chủ trả lỗi OAuth thật sự; còn đứt mạng thì
 * `fetch` ném TypeError. Chỉ trường hợp Google từ chối mới đáng vứt refresh token đi.
 */
function mangHong(loi: unknown): boolean {
  return !(loi instanceof AuthSession.TokenError);
}

async function layEmail(token: string): Promise<string> {
  try {
    const traLoi = await fetch(DIA_CHI_THONG_TIN, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!traLoi.ok) {
      return '';
    }
    const thongTin = (await traLoi.json()) as { email?: string };
    return thongTin.email ?? '';
  } catch {
    // Không lấy được email thì thôi, chỉ mất một dòng chữ trên màn hình. Đăng nhập vẫn
    // coi như xong — token mới là thứ cần.
    return '';
  }
}
