/**
 * Bàn phím đang che mất bao nhiêu chiều cao màn hình. Bản chạy trên máy: **luôn 0**.
 *
 * Trên iOS và Android, việc đẩy hộp lên là của `KeyboardAvoidingView` trong
 * [HopDay](./HopDay.tsx) — nó nghe đúng sự kiện bàn phím của hệ điều hành, kèm cả thời
 * lượng và nhịp của cú trượt nên hộp lên xuống mượt cùng bàn phím. Cộng thêm một khoảng
 * đệm nữa ở đây là đẩy hai lần, hộp bay quá đầu bàn phím.
 *
 * Cái thiếu là bản web: xem [dungCaoBanPhim.web.ts](./dungCaoBanPhim.web.ts).
 */
export function dungCaoBanPhim(): number {
  return 0;
}
