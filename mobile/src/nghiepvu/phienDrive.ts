/**
 * Một lần gọi Drive có kèm token, tự lấy token mới khi gặp 401.
 *
 * Tách riêng ra khỏi phần sao lưu vì giờ có hai bên cùng gọi Drive: sao lưu và hộp thư
 * đối chiếu. Để mỗi bên một bản thì sớm muộn một bên quên xử lý 401, và lỗi ấy chỉ hiện
 * ra đúng lúc người dùng vừa đổi mật khẩu Google — chỗ khó đoán nhất.
 */

import { accessToken, boTokenDangGiu } from './dangNhapGoogle';
import * as Drive from './goiDrive';

/**
 * 401 xảy ra khi người dùng thu hồi quyền hoặc đổi mật khẩu giữa chừng — token trong tay
 * mình vẫn còn hạn trên giấy tờ nên không tự biết mà làm mới. Chỉ thử lại **một lần**:
 * 401 lần nữa nghĩa là hỏng thật, thử mãi chỉ làm người dùng ngồi chờ.
 */
export async function voiToken<T>(viec: (token: string) => Promise<T>): Promise<T> {
  try {
    return await viec(await accessToken());
  } catch (loi) {
    if (!(loi instanceof Drive.LoiDrive) || loi.ma !== 401) {
      throw loi;
    }
    boTokenDangGiu();
    return viec(await accessToken());
  }
}
