/**
 * Địa chỉ project Supabase và khoá công khai, đọc từ biến môi trường lúc dựng app.
 *
 * Khoá này **không phải bí mật** — nó nằm trong mọi app cài trên máy người dùng, ai gỡ app
 * ra cũng đọc được, và Supabase phát nó ra để làm đúng việc ấy. Thứ chặn người này đọc dữ
 * liệu của người kia là **RLS trong database**, không phải việc giữ kín khoá. Nói cách khác:
 * quên bật RLS trên một bảng thì bảng ấy công khai với cả internet, dù khoá kín tới đâu.
 *
 * Tuyệt đối không đưa `service_role` key vào đây: khoá ấy **bỏ qua RLS**, ai moi được là
 * đọc và xoá được cả database.
 */

const DIA_CHI = process.env.EXPO_PUBLIC_SUPABASE_URL ?? '';

/**
 * Bảng điều khiển Supabase bản mới gọi khoá này là *publishable key*, bản cũ gọi là
 * *anon key* — cùng một chỗ điền. Nhận cả hai tên biến để ai theo tài liệu nào cũng chạy.
 */
const KHOA =
  process.env.EXPO_PUBLIC_SUPABASE_ANON_KEY ??
  process.env.EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY ??
  '';

export function diaChi(): string {
  return DIA_CHI;
}

export function khoaCongKhai(): string {
  return KHOA;
}

/**
 * Chưa điền thì coi như tính năng chưa bật — giao diện ẩn hẳn phần nhóm đi thay vì để người
 * dùng bấm vào rồi nhận một lỗi mạng khó hiểu.
 */
export function daCauHinh(): boolean {
  return DIA_CHI !== '' && KHOA !== '';
}
