/**
 * Màu, font và cỡ dùng chung — một nguồn duy nhất cho cả app.
 * Xem docs/chamcong-giao-dien.md trước khi sửa.
 *
 * Bảng màu và hình khối lấy theo bộ *HR Attendance App UI Kit* trên Figma: thẻ bo 16 đổ
 * bóng rất nhẹ thay cho thẻ viền xám, ô tóm tắt nền màu nhạt viền màu tươi, chip bo 8,
 * thanh phân đoạn có viên trượt. Riêng **độ đậm của màu chữ** thì hạ hơn bản Figma một
 * nấc — xem ghi chú ở `Mau` bên dưới.
 */

/**
 * Màu dịu, dùng làm điểm nhấn chứ không tô mảng lớn. Ba màu mang nghĩa rõ ràng,
 * không dùng lẫn lộn:
 *   xanh lá = đã có công (ô đã chấm, tiền còn phải trả)
 *   xanh dương = thao tác và điều hướng
 *   đỏ = xoá, hoặc số tiền âm
 *
 * Xanh ngọc là màu thứ tư, **không mang nghĩa** — chỉ để phân biệt các ô trong lưới tóm
 * tắt bốn ô. Đừng dùng nó để báo trạng thái.
 *
 * **Vì sao không lấy nguyên mã màu Figma.** Bản Figma dùng xanh `#3085FE`, lá mạ
 * `#A3D139`, ngọc `#30BEB6`, san hô `#FF7F74` — trên màn hình máy tính trong nhà thì
 * đẹp, nhưng chữ trắng trên nền `#3085FE` chỉ tương phản 3,6:1, còn chữ lá mạ trên nền
 * trắng chưa tới 2:1. Người dùng app này bấm **ngoài công trình, giữa nắng**, nên mỗi
 * màu dùng để *viết chữ* đều hạ xuống tới khi đạt ngưỡng WCAG AA 4,5:1.
 *
 * Bù lại, `Tuoi` giữ nguyên mã Figma và chỉ dùng cho **viền và mảng tô** — chỗ không có
 * chữ nên không cần tương phản. Nhờ vậy ô tóm tắt (nền màu 5% + viền tươi) nhìn giống
 * hệt bản thiết kế, chỉ con số bên trong là đậm hơn một chút.
 */
export const Mau = {
  chinh: '#2569E9',
  chinhNhat: '#EFF5FF',
  xanhLa: '#4A7D0F',
  xanhLaNhat: '#F5FAEA',
  ngoc: '#0E7A74',
  ngocNhat: '#E9F7F6',
  do: '#CE3F30',
  doNhat: '#FDF0EE',

  chu: '#101317',
  xam: '#696F79',
  nen: '#F6F8FB',
  trang: '#FFFFFF',
  vien: '#EBEDF1',
};

/**
 * Đúng mã màu bản Figma. **Chỉ dùng cho viền và mảng tô, không bao giờ viết chữ bằng
 * mấy màu này** — chúng quá sáng, xem ghi chú ở `Mau`.
 */
export const Tuoi = {
  chinh: '#3085FE',
  xanhLa: '#A3D139',
  ngoc: '#30BEB6',
  do: '#FF7F74',
};

/**
 * Lexend — font của bản thiết kế, và cũng là font vẽ riêng để **dễ đọc**: chữ cái nở
 * ngang, khoảng cách thưa, vốn làm cho người đọc chậm. Hợp với người dùng có tuổi của
 * app này hơn hẳn một font trung tính.
 *
 * Đã soi bảng mã trong file .ttf: có đủ chữ tiếng Việt hai dấu (`ế`, `ộ`, `ữ`, `ằ`).
 *
 * Chỉ dùng tới nét 600 (SemiBold), không dùng 700 (Bold): nét 700 ở cỡ chữ lớn nhìn
 * nặng và thô.
 *
 * Bẫy: khi đã dùng font riêng thì `fontWeight` hết tác dụng — hệ điều hành không tự làm
 * đậm font ngoài được. Muốn đậm phải đổi hẳn fontFamily.
 */
export const PhongChu = {
  /** Nhãn nhỏ trên ô tóm tắt, đúng nét Light của bản thiết kế. Đừng dùng cho câu chữ dài. */
  nhe: 'Lexend_300Light',
  thuong: 'Lexend_400Regular',
  vua: 'Lexend_500Medium',
  dam: 'Lexend_600SemiBold',
};

export const Co = {
  /** Tên thợ. */
  chuTen: 19,
  chuTieuDe: 17,
  chuSo: 16,
  chuNut: 15,
  chuThuong: 15,
  chuPhu: 13,
  /** Nhãn nhỏ nhất — chỉ dùng cho nhãn trên ô tóm tắt và chip trạng thái. */
  chuNho: 12,
  /** Con số lớn giữa ô tóm tắt. */
  chuSoTo: 22,

  /**
   * Chiều cao nút là mức **tối thiểu**, không phải cố định: mọi nút đặt `minHeight` kèm
   * `paddingVertical` chứ đừng đặt `height`. Người dùng có tuổi hay chỉnh cỡ chữ hệ thống
   * lên to; chữ phóng theo mà khung không nở ra thì chữ bị cắt cụt.
   */
  caoNut: 48,
  /** Ô chấm Sáng / Chiều — thứ được bấm nhiều nhất nên vẫn rộng rãi hơn nút thường. */
  caoOCham: 56,
  caoNutNho: 36,

  /** Bo nút và ô nhập. */
  bo: 12,
  /** Bo thẻ — số của bản thiết kế. */
  boThe: 16,
  /** Bo chip và nhãn nhỏ — số của bản thiết kế. */
  boNho: 8,
};

/**
 * Đổ bóng thay cho viền xám. Bản thiết kế dựng thẻ trắng trên nền trắng, phân tách nhau
 * bằng đúng một vệt bóng rất loãng (`0 55px 110px rgba(0,0,0,0.04)`) chứ không có nét viền
 * nào — nhìn nhẹ hơn hẳn lưới thẻ kẻ viền.
 *
 * Trên máy thì bóng ấy dịch thành bóng gần và mờ. Android chỉ nghe `elevation`, iOS chỉ
 * nghe mấy thuộc tính `shadow*`, nên phải khai cả hai. Nền trang vẫn để hơi xám
 * (`Mau.nen`) chứ không trắng như Figma: Android vẽ bóng nhạt hơn iOS nhiều, thẻ trắng
 * trên nền trắng ở đó gần như biến mất.
 */
export const Bong = {
  the: {
    shadowColor: '#101317',
    shadowOpacity: 0.06,
    shadowRadius: 16,
    shadowOffset: { width: 0, height: 6 },
    elevation: 2,
  },
  /** Đậm hơn một nấc, cho thanh tab và thanh chân trang nổi lên trên nội dung cuộn. */
  noi: {
    shadowColor: '#101317',
    shadowOpacity: 0.1,
    shadowRadius: 20,
    shadowOffset: { width: 0, height: -4 },
    elevation: 12,
  },
};

/**
 * Chữ trong lưới chia cột — dải ngày ở màn hình chấm công, tờ lịch, hộp chọn ngày, và thanh
 * tab bốn mục — chỉ phóng tối đa 1,3 lần theo cỡ chữ hệ thống. Bề ngang màn hình chia đều cho
 * các cột nên những ô này *không nở ngang được*, phóng hơn nữa thì chữ không còn chỗ.
 *
 * Chỉ dùng cho lưới. Nút và chữ thường không chặn — chúng cao lên được theo chữ.
 */
export const HeSoChuToiDaLuoi = 1.3;
