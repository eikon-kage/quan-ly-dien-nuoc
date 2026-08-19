/**
 * Chỗ giữ phiên đăng nhập Supabase: **SecureStore** (Keychain của iOS, Keystore của Android),
 * không phải AsyncStorage.
 *
 * Vì trong phiên có refresh token. AsyncStorage là file thường, máy đã root hay jailbreak là
 * đọc được; mà cầm refresh token của máy chủ thì đọc được sổ công của cả nhóm cho tới khi bị
 * thu hồi. Đây cũng đúng nguyên tắc token Google đang theo (xem docs/chamcong-sao-luu-drive.md).
 *
 * Rắc rối duy nhất: **SecureStore chỉ nhận giá trị dưới 2048 byte**, mà phiên Supabase gồm
 * JWT, refresh token và cả thông tin người dùng nên thường vượt. Vì vậy phải cắt thành khúc.
 * Không cắt thì SecureStore lặng lẽ ghi hụt, app chạy êm cho tới khi mở lại và thấy mất phiên
 * đăng nhập mà không hiểu vì sao.
 */

import * as SecureStore from 'expo-secure-store';

/**
 * Cắt 1600 ký tự một khúc. Giới hạn là 2048 **byte**, mà chuỗi ở đây là JWT nên gần như
 * toàn ASCII (1 byte một ký tự); để 1600 là còn chỗ dư cho ký tự nhiều byte nếu có.
 */
const CO_KHUC = 1600;

/** Kho khoá–giá trị an toàn, tách ra để bài kiểm thử đưa hàng giả vào. */
export interface KhoAnToan {
  doc(khoa: string): Promise<string | null>;
  ghi(khoa: string, gia: string): Promise<void>;
  xoa(khoa: string): Promise<void>;
}

export function khoSecureStore(): KhoAnToan {
  return {
    doc: (khoa) => SecureStore.getItemAsync(khoa),
    ghi: (khoa, gia) => SecureStore.setItemAsync(khoa, gia),
    xoa: (khoa) => SecureStore.deleteItemAsync(khoa),
  };
}

/** Đúng ba hàm mà `createClient` cần cho `auth.storage`. */
export interface KhoPhien {
  getItem(khoa: string): Promise<string | null>;
  setItem(khoa: string, gia: string): Promise<void>;
  removeItem(khoa: string): Promise<void>;
}

/** Khoá ghi số khúc đang giữ. Đọc số này trước rồi mới biết phải ghép mấy khúc. */
const khoaSoKhuc = (khoa: string) => `${khoa}.so`;
const khoaKhuc = (khoa: string, i: number) => `${khoa}.${i}`;

export function khoPhien(kho: KhoAnToan = khoSecureStore()): KhoPhien {
  async function xoaTuKhuc(khoa: string, tu: number): Promise<void> {
    // Xoá lần lượt cho tới khi gặp khúc trống. Phiên mới ngắn hơn phiên cũ thì phải dọn
    // mấy khúc dư — để lại thì lần đọc sau ghép cả rác vào giữa chuỗi JSON.
    for (let i = tu; ; i += 1) {
      const co = await kho.doc(khoaKhuc(khoa, i));
      if (co === null) {
        return;
      }
      await kho.xoa(khoaKhuc(khoa, i));
    }
  }

  return {
    async getItem(khoa) {
      const soKhuc = Number(await kho.doc(khoaSoKhuc(khoa)));
      if (!Number.isInteger(soKhuc) || soKhuc <= 0) {
        return null;
      }

      const khuc: string[] = [];
      for (let i = 0; i < soKhuc; i += 1) {
        const phan = await kho.doc(khoaKhuc(khoa, i));
        // Thiếu một khúc thì cả chuỗi vô nghĩa — thà coi như chưa đăng nhập còn hơn trả về
        // một mẩu JSON dở dang cho thư viện đọc.
        if (phan === null) {
          return null;
        }
        khuc.push(phan);
      }
      return khuc.join('');
    },

    async setItem(khoa, gia) {
      const khuc: string[] = [];
      for (let i = 0; i < gia.length; i += CO_KHUC) {
        khuc.push(gia.slice(i, i + CO_KHUC));
      }

      for (const [i, phan] of khuc.entries()) {
        await kho.ghi(khoaKhuc(khoa, i), phan);
      }
      // Ghi số khúc **sau cùng**: đứt giữa đường thì số cũ vẫn trỏ vào bộ khúc cũ còn
      // nguyên, chứ không trỏ vào một bộ nửa mới nửa cũ.
      await kho.ghi(khoaSoKhuc(khoa), String(khuc.length));
      await xoaTuKhuc(khoa, khuc.length);
    },

    async removeItem(khoa) {
      await kho.xoa(khoaSoKhuc(khoa));
      await xoaTuKhuc(khoa, 0);
    },
  };
}
