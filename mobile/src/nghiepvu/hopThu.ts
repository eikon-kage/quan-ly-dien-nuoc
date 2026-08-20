/**
 * Hộp thư: chỗ hai máy đặt sổ của mình vào và lấy sổ bên kia ra.
 *
 * **Cố ý chỉ là một giao diện, không có ruột.** Ruột hiện tại là Supabase
 * ([hopThuSupabase](./hopThuSupabase.ts)); trước đây là Google Drive dùng chung một tài
 * khoản, đã bỏ vì cách ấy không chặn được ai đọc của ai — máy nào cũng xoá được file của
 * máy khác. Đổi ruột lần nữa thì viết một `HopThu` khác và chỉ sửa đúng một dòng nơi tạo ra
 * nó; màn hình đối chiếu và phần tính toán không phải sửa gì.
 *
 * Vì vậy giao diện dưới đây chỉ nói bằng lời của việc chấm công — gửi sổ, đọc sổ — không hé
 * một chữ nào về file, bảng hay token.
 */

import { SoCong, Vai } from './soCong';

/** Một sổ lấy từ hộp thư, kèm lúc bên kia đặt vào. */
export interface SoDaNhan {
  so: SoCong;
  /** Lúc hộp thư ghi nhận lần đặt cuối, dạng ISO. */
  suaLuc: string;
}

export interface HopThu {
  /** Đặt sổ của máy này vào hộp thư, ghi đè lên sổ cũ của chính nó. */
  gui(so: SoCong): Promise<void>;
  /** Lấy sổ của một thợ do bên `nguon` gửi. Chưa có thì trả null. */
  doc(thoId: string, nguon: Vai): Promise<SoDaNhan | null>;
  /** Máy chủ dùng: lấy sổ của **mọi** thợ đã gửi lên. */
  docSoCacTho(): Promise<SoDaNhan[]>;
}
