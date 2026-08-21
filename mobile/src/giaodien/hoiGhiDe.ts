/**
 * Hỏi lại kèm số liệu trước khi ghi đè cả sổ.
 *
 * **Mọi đường khôi phục trong app đi qua đúng hàm này** — bản trong máy, file tự chọn, và bản
 * trên tài khoản. Ghi đè là thao tác không lùi lại được, nên câu hỏi phải nói ra *mình sắp nhận
 * cái gì*: nhìn "1 thợ, 12 buổi công" mới biết đây là bản đúng hay bản nhầm. Viết lại câu hỏi
 * ở từng màn hình thì sớm muộn có một đường nuốt lặng, mà đường ấy chính là đường mất sổ.
 */

import { tomTat } from '../nghiepvu/goiSaoLuu';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import { hoi } from './hopThoai';

export function hoiGhiDe(
  /** Câu hỏi trên đầu hộp, ví dụ "Khôi phục bản 05/08?". */
  nhan: string,
  duLieuMoi: DuLieuChamCong,
  /** Chữ trên nút đồng ý — đúng chữ của việc người dùng vừa bấm ở màn hình. */
  nhanDongY: string,
  khiDongY: (moi: DuLieuChamCong) => void,
): void {
  const dem = tomTat(duLieuMoi);

  hoi(
    nhan,
    `Bản này có ${dem.soTho} thợ, ${dem.soBuoiCong} buổi công, ${dem.soUngTien} lần ứng tiền, ${dem.soKy} kỳ đã chốt.\n\nToàn bộ dữ liệu đang có trên máy sẽ bị thay bằng bản này.`,
    [
      { text: 'Thôi', style: 'cancel' },
      {
        text: nhanDongY,
        style: 'destructive',
        onPress: () => khiDongY(duLieuMoi),
      },
    ],
  );
}
