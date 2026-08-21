/**
 * Sao lưu cả sổ lên **chính tài khoản của chủ**, để đổi máy là sổ theo tài khoản mà sang.
 *
 * Vì sao phải có đường này, khi đã có sao lưu vào máy: sao lưu vào máy nằm trong phần riêng
 * của app, nên nó chống hỏng dữ liệu chứ **không chống mất máy**. Chủ mua điện thoại mới,
 * đăng nhập đúng tài khoản cũ, vào nhóm cũ — mà sổ thì trắng trơn, vì tài khoản trước đây chỉ
 * mang theo *chỗ trong nhóm*, không mang theo sổ. Hộp thư đối chiếu không đỡ được chỗ này và
 * cũng không nên đỡ: trong đó chỉ có số công của từng thợ, không có mốc lương, không có ứng
 * tiền, không có kỳ đã chốt — dựng lại sổ từ đó là dựng ra một sổ khác.
 *
 * Ba điều làm nên đường này, và cả ba đều là chủ ý:
 *
 *   1. **Chỉ tài khoản đã ghi mới đọc được** (RLS theo `user_id`, xem supabase/thiet-lap.sql).
 *      Đây là bảng duy nhất trên Supabase có tiền trong đó. Khoá theo nhóm là mở tiền công của
 *      cả cửa hàng cho mọi máy thợ.
 *   2. **Không có gì tự trộn.** Lấy về là *ghi đè*, và chỉ chạy khi người dùng bấm rồi xác
 *      nhận kèm số liệu — đúng một luồng với khôi phục từ file. Tự nhập vào sổ đang có thì hai
 *      máy chủ mở cùng lúc là hai sổ đè nhau mà không ai biết.
 *   3. **Mỗi ngày một bản**, giữ 30 ngày, y như mỗi ngày một file bên sao lưu vào máy. Giữ một
 *      bản duy nhất thì hôm nay lỡ tay xoá mấy chục buổi công là lượt đẩy tự động chép cái sai
 *      ấy đè lên bản đúng.
 *
 * Chỉ **máy chủ đăng nhập bằng email** dùng đường này. Máy thợ đăng nhập ẩn danh, mà tài khoản
 * ẩn danh chỉ sống trong đúng cái điện thoại ấy: sao lưu vào đó là sao lưu vào cái máy sắp
 * mất. Sổ thợ mất máy thì dán mã mời mới, sổ công của họ vẫn nằm trong sổ chủ.
 *
 * Cố ý chỉ là **một giao diện** như [hopThu](./hopThu.ts): ruột hiện tại là
 * [saoLuuTaiKhoanSupabase](./saoLuuTaiKhoanSupabase.ts), và hook giao diện nhận nó từ ngoài
 * nên bài kiểm thử đưa được một cái kho giả vào.
 */

import { DuLieuChamCong } from './kieu';

/** Một bản đang nằm trên tài khoản. Không mang theo cả sổ — danh sách phải nhẹ. */
export interface BanTaiKhoan {
  /** Ngày của bản, dạng yyyy-MM-dd. Cũng là mã của bản: mỗi ngày một hàng. */
  ngay: string;
  /** Lúc ghi lần cuối trong ngày ấy, dạng ISO. */
  suaLuc: string;
}

export interface KhoTaiKhoan {
  /** Máy này đẩy được sổ lên tài khoản không (đã cấu hình Supabase). */
  hoTro(): boolean;
  /** Đẩy cả sổ lên, ghi đè bản của ngày hôm ấy. Trả về bản vừa ghi. */
  day(duLieu: DuLieuChamCong, ngay: string): Promise<BanTaiKhoan>;
  /** Các bản đang có trên tài khoản, mới nhất đứng đầu. Chưa có bản nào thì mảng rỗng. */
  danhSach(): Promise<BanTaiKhoan[]>;
  /**
   * Đọc một bản ra và mở gói. **Chưa ghi xuống máy** — người gọi phải hỏi lại người dùng đã,
   * vì lấy về là thao tác ghi đè không lùi lại được.
   */
  docBan(ngay: string): Promise<DuLieuChamCong>;
}
