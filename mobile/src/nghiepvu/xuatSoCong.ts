/**
 * Dựng file Excel cho **sổ công của một thợ** — chỉ ngày, buổi, số công, không một đồng nào.
 *
 * Vì sao không dùng luôn [xuatExcel](./xuatExcel.ts): bản kia là toàn bộ sổ sách của chủ,
 * có tiền một công, tiền ứng, các kỳ đã quyết toán. Máy thợ gọi nó là **file gửi ra ngoài
 * mang đủ tiền công**, kể cả khi màn hình đã ẩn hết — sổ trong máy thợ vẫn còn mốc lương từ
 * lúc máy ấy từng là máy chủ (xem ghi chú đầu ManHinhThoTuCham).
 *
 * Nên hàm này nhận thẳng `SoCong`, kiểu đã cắt tiền ra từ lúc đóng gói, chứ không nhận
 * `DuLieuChamCong`. Cắt ở *kiểu dữ liệu* thì không có đường nào cho tiền lọt ra: muốn thêm
 * cột tiền vào đây cũng không có số mà thêm. Giống hệt lý lẽ của goiSo với hộp thư.
 */

import { CAC_BUOI } from './kieu';
import * as Ngay from './ngayViet';
import { SoCong } from './soCong';
import { Cot, TrangTinh, taoFileExcel } from './xlsx';

const CHU_BUOI: Record<string, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

/**
 * Tên file gửi đi, ví dụ "So-cong-05-08-2026.xlsx".
 *
 * Không nhồi tên thợ vào: tên có dấu, mà tên file có dấu thì gửi qua mạng hay mở trên máy
 * tính khác là lỗi phông. Thợ tự biết đây là sổ của mình.
 */
export function tenFileSoCong(so: SoCong): string {
  const { nam, thang, ngay } = Ngay.tach(so.denNgay);
  const hai = (x: number) => String(x).padStart(2, '0');
  return `So-cong-${hai(ngay)}-${hai(thang)}-${nam}.xlsx`;
}

function cot(nhan: string, rong: number, kieu: Cot['kieu'] = 'chu'): Cot {
  return { nhan, rong, kieu };
}

/**
 * Một trang duy nhất: từng buổi một dòng, cuối trang là tổng số công.
 *
 * Không có cột `daChot`. Cờ ấy chỉ có trên sổ của chủ — máy thợ không chốt kỳ nên cột ấy
 * trống toàn bộ, thêm vào chỉ là một cột trắng cho người ta hỏi "cột này là gì".
 */
export function trangSoCong(so: SoCong): TrangTinh {
  const dongs = [...so.dongs]
    // Xếp buổi sáng lên trước buổi chiều, giống trang Buổi công của máy chủ. Sổ trong hộp thư
    // xếp theo vần nên "Chieu" đứng trước "Sang" — đúng cho máy so sánh, ngược với mắt người.
    .sort((a, b) =>
      a.ngay === b.ngay
        ? CAC_BUOI.indexOf(a.buoi) - CAC_BUOI.indexOf(b.buoi)
        : a.ngay.localeCompare(b.ngay),
    )
    .map((dong) => [
      dong.ngay,
      Ngay.thu(dong.ngay),
      CHU_BUOI[dong.buoi] ?? dong.buoi,
      dong.soCong,
    ]);

  return {
    ten: 'Sổ công',
    cots: [cot('Ngày', 12, 'ngay'), cot('Thứ', 10), cot('Buổi', 9), cot('Số công', 9, 'so')],
    dongs,
    dongTong:
      dongs.length === 0
        ? undefined
        : ['Tổng cộng', null, null, so.dongs.reduce((tong, dong) => tong + dong.soCong, 0)],
  };
}

/** Sổ công của một thợ thành khối byte của một file .xlsx. */
export function xuatSoCong(so: SoCong): Uint8Array {
  return taoFileExcel([trangSoCong(so)]);
}
