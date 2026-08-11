/**
 * Màu, font và cỡ dùng chung — một nguồn duy nhất cho cả app.
 * Xem docs/chamcong-giao-dien.md trước khi sửa.
 */

/**
 * Màu dịu, dùng làm điểm nhấn chứ không tô mảng lớn. Ba màu mang nghĩa rõ ràng,
 * không dùng lẫn lộn:
 *   xanh lá = đã có công (ô đã chấm, tiền còn phải trả)
 *   xanh dương = thao tác và điều hướng
 *   đỏ = xoá, hoặc số tiền âm
 */
export const Mau = {
  chinh: '#3B71E8',
  chinhNhat: '#EEF3FE',
  xanhLa: '#2E9E5B',
  xanhLaNhat: '#E9F7EF',
  do: '#D6455D',
  doNhat: '#FDEEF1',
  chu: '#232A35',
  xam: '#7B8494',
  nen: '#F7F8FA',
  trang: '#FFFFFF',
  vien: '#E6E9EF',
};

/**
 * Inter — font trung tính, chữ tiếng Việt có dấu đặt gọn, đọc dịu mắt ở cỡ vừa.
 *
 * Chỉ dùng tới nét 600 (SemiBold), không dùng 700 (Bold): nét 700 ở cỡ chữ lớn nhìn
 * nặng và thô.
 *
 * Bẫy: khi đã dùng font riêng thì `fontWeight` hết tác dụng — hệ điều hành không tự làm
 * đậm font ngoài được. Muốn đậm phải đổi hẳn fontFamily.
 */
export const PhongChu = {
  thuong: 'Inter_400Regular',
  vua: 'Inter_500Medium',
  dam: 'Inter_600SemiBold',
};

export const Co = {
  /** Tên thợ. */
  chuTen: 19,
  chuTieuDe: 17,
  chuSo: 16,
  chuNut: 15,
  chuThuong: 15,
  chuPhu: 13,

  /**
   * Chiều cao nút là mức **tối thiểu**, không phải cố định: mọi nút đặt `minHeight` kèm
   * `paddingVertical` chứ đừng đặt `height`. Người dùng có tuổi hay chỉnh cỡ chữ hệ thống
   * lên to; chữ phóng theo mà khung không nở ra thì chữ bị cắt cụt.
   */
  caoNut: 48,
  /** Ô chấm Sáng / Chiều — thứ được bấm nhiều nhất nên vẫn rộng rãi hơn nút thường. */
  caoOCham: 56,
  caoNutNho: 36,
  bo: 10,
};

/**
 * Chữ trong lưới chia cột — dải ngày ở màn hình chấm công, tờ lịch, hộp chọn ngày, và thanh
 * tab bốn mục — chỉ phóng tối đa 1,3 lần theo cỡ chữ hệ thống. Bề ngang màn hình chia đều cho
 * các cột nên những ô này *không nở ngang được*, phóng hơn nữa thì chữ không còn chỗ.
 *
 * Chỉ dùng cho lưới. Nút và chữ thường không chặn — chúng cao lên được theo chữ.
 */
export const HeSoChuToiDaLuoi = 1.3;
