/**
 * Bản web của [chiaSeFile](./chiaSeFile.ts): gửi file đi mà không có thư mục nào để ghi vào.
 *
 * Không dùng `expo-sharing` ở đây. Bản web của nó nói thẳng trong tài liệu là **không chia
 * sẻ được file theo đường dẫn** — phải tải file lên đâu đó rồi chia sẻ địa chỉ, mà đưa sổ
 * công lên một máy chủ lạ chỉ để gửi cho chính mình thì không đáng. Trình duyệt có sẵn hai
 * đường tốt hơn:
 *
 * 1. **Bảng chia sẻ thật của máy** (`navigator.share` kèm file) — trên iPhone đây đúng là
 *    bảng "Chia sẻ" của iOS: gửi Zalo, gửi mail, lưu vào Files, y như bản trên máy. Safari
 *    hỗ trợ từ iOS 15, nên đường này gần như luôn chạy trên điện thoại.
 *
 * 2. **Tải về** (thẻ `a` kèm `download`) — đường lùi cho máy tính và trình duyệt cũ. File
 *    rơi vào thư mục Tải về, người dùng tự biết chỗ.
 *
 * Bẫy đã lường: `navigator.share` đòi *vừa có cú bấm của người dùng*. Dựng file Excel mất
 * vài chục mili giây và có `await` ở giữa nên có máy coi là "cú bấm đã nguội", lúc ấy nó
 * ném `NotAllowedError` — vì vậy mọi lỗi khác `AbortError` (người dùng bấm huỷ) đều rơi
 * xuống đường tải về chứ không báo hỏng.
 */

import { KIEU_EXCEL, KIEU_JSON, KhongChiaSeDuoc, KieuFile } from './kieuFile';

export { KIEU_EXCEL, KIEU_JSON, KhongChiaSeDuoc, KieuFile };

/**
 * Gửi khối nội dung đi. Trả về tên file — bản trên máy trả về đường dẫn file tạm, nhưng
 * trên web không có đường dẫn nào tồn tại sau đó, mà bên gọi thì cũng chỉ dùng giá trị này
 * để ghi nhật ký.
 */
export async function guiFile(
  noiDung: Uint8Array | string,
  tenFile: string,
  kieu: KieuFile,
  tieuDe: string,
): Promise<string> {
  const khoi = new Blob([noiDung as BlobPart], { type: kieu.mime });
  const file = new File([khoi], tenFile, { type: kieu.mime });

  if (navigator.canShare?.({ files: [file] })) {
    try {
      await navigator.share({ files: [file], title: tieuDe });
      return tenFile;
    } catch (loi) {
      // Người dùng bấm huỷ thì cũng coi như xong, đúng như bản trên máy: hệ điều hành
      // không cho biết họ đã gửi hay đã huỷ, nên đừng mở thêm hộp tải về đè lên.
      if (loi instanceof DOMException && loi.name === 'AbortError') {
        return tenFile;
      }
    }
  }

  taiVe(khoi, tenFile);
  return tenFile;
}

/** Bấm hộ một thẻ `a` có `download`. Địa chỉ tạm phải thu hồi, kẻo file nằm mãi trong RAM. */
function taiVe(khoi: Blob, tenFile: string): void {
  if (typeof document === 'undefined') {
    throw new KhongChiaSeDuoc();
  }

  const diaChi = URL.createObjectURL(khoi);
  const the = document.createElement('a');
  the.href = diaChi;
  the.download = tenFile;
  the.style.display = 'none';
  document.body.appendChild(the);
  the.click();
  document.body.removeChild(the);
  // Thu hồi ngay là có máy tải hụt vì chưa kịp đọc. Chờ một nhịp cho chắc.
  setTimeout(() => URL.revokeObjectURL(diaChi), 60_000);
}
