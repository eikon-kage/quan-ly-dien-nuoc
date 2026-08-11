/**
 * Gọi thẳng Google Drive API v3 bằng `fetch`.
 *
 * Không dùng thư viện googleapis: thư viện ấy nặng, viết cho Node và kéo theo cả đống thứ
 * React Native không chạy được. Ở đây chỉ cần năm lệnh, gọi tay còn ngắn hơn.
 *
 * Cả file này không đụng gì tới native — nhận sẵn access token qua tham số. Nhờ vậy kiểm
 * thử được bằng cách thay mỗi `fetch`.
 */

const API = 'https://www.googleapis.com/drive/v3';
const API_TAI_LEN = 'https://www.googleapis.com/upload/drive/v3';

const KIEU_JSON = 'application/json';

/** Ranh giới giữa hai phần của gói multipart. Chuỗi nào cũng được miễn không có trong nội dung. */
const RANH_GIOI = 'ranh-gioi-cham-cong';

/** Drive trả về mã lỗi. Giữ nguyên mã HTTP để bên ngoài phân biệt 401 với 403, 404. */
export class LoiDrive extends Error {
  constructor(
    readonly ma: number,
    thongDiep: string,
  ) {
    super(thongDiep);
  }
}

export interface FileDrive {
  id: string;
  ten: string;
  /** Lúc Drive ghi nhận lần sửa cuối, dạng ISO. */
  suaLuc: string;
}

/** Các trường xin Drive trả về. Không xin thì Drive chỉ trả id với tên. */
const TRUONG = 'id,name,modifiedTime';

function doiTen(file: { id: string; name: string; modifiedTime?: string }): FileDrive {
  return { id: file.id, ten: file.name, suaLuc: file.modifiedTime ?? '' };
}

async function goi(token: string, diaChi: string, tuyChon: RequestInit = {}): Promise<Response> {
  const traLoi = await fetch(diaChi, {
    ...tuyChon,
    headers: { ...tuyChon.headers, Authorization: `Bearer ${token}` },
  });

  if (!traLoi.ok) {
    throw new LoiDrive(traLoi.status, await moTaLoi(traLoi));
  }
  return traLoi;
}

/** Lấy câu giải thích của Google nếu có; không có thì đành báo mã số. */
async function moTaLoi(traLoi: Response): Promise<string> {
  try {
    const noiDung = (await traLoi.json()) as { error?: { message?: string } };
    if (noiDung.error?.message) {
      return noiDung.error.message;
    }
  } catch {
    // Lỗi không phải JSON — hiếm, thường là trang lỗi của proxy mạng.
  }
  return `Drive trả lỗi ${traLoi.status}.`;
}

/**
 * Liệt kê mọi file app này nhìn thấy trên Drive.
 *
 * Quyền `drive.file` khiến Drive **chỉ trả về những file do chính app tạo ra** — nghĩa là
 * đúng các bản sao lưu, không lẫn tài liệu riêng của người dùng. Vì vậy không cần lọc theo
 * tên trong câu truy vấn, cứ lấy hết rồi lọc bằng tay cho chắc: `name contains` của Drive
 * khớp theo từ, dễ sót file.
 */
export async function danhSach(token: string): Promise<FileDrive[]> {
  const thamSo = new URLSearchParams({
    q: 'trashed = false',
    spaces: 'drive',
    orderBy: 'name desc',
    pageSize: '100',
    fields: `files(${TRUONG})`,
  });

  const traLoi = await goi(token, `${API}/files?${thamSo}`);
  const noiDung = (await traLoi.json()) as { files?: { id: string; name: string; modifiedTime?: string }[] };
  return (noiDung.files ?? []).map(doiTen);
}

/**
 * Tạo file mới kèm nội dung trong đúng một lần gọi (gói multipart: phần đầu là thông tin
 * file, phần sau là nội dung).
 *
 * Một lần gọi chứ không phải hai — tạo file rỗng rồi mới ghi nội dung thì lỡ đứt mạng giữa
 * chừng sẽ để lại một bản sao lưu rỗng nằm trên Drive, nhìn như đã sao lưu mà thật ra trống.
 */
export async function taoFile(token: string, ten: string, noiDung: string): Promise<FileDrive> {
  const than =
    `--${RANH_GIOI}\r\n` +
    `Content-Type: ${KIEU_JSON}; charset=UTF-8\r\n\r\n` +
    `${JSON.stringify({ name: ten, mimeType: KIEU_JSON })}\r\n` +
    `--${RANH_GIOI}\r\n` +
    `Content-Type: ${KIEU_JSON}\r\n\r\n` +
    `${noiDung}\r\n` +
    `--${RANH_GIOI}--`;

  const traLoi = await goi(token, `${API_TAI_LEN}/files?uploadType=multipart&fields=${TRUONG}`, {
    method: 'POST',
    headers: { 'Content-Type': `multipart/related; boundary=${RANH_GIOI}` },
    body: than,
  });

  return doiTen(await traLoi.json());
}

/** Ghi đè nội dung file đã có. Tên và ngày tạo giữ nguyên, chỉ nội dung đổi. */
export async function ghiDe(token: string, id: string, noiDung: string): Promise<FileDrive> {
  const traLoi = await goi(
    token,
    `${API_TAI_LEN}/files/${id}?uploadType=media&fields=${TRUONG}`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': KIEU_JSON },
      body: noiDung,
    },
  );

  return doiTen(await traLoi.json());
}

export async function taiVe(token: string, id: string): Promise<string> {
  const traLoi = await goi(token, `${API}/files/${id}?alt=media`);
  return traLoi.text();
}

export async function xoa(token: string, id: string): Promise<void> {
  await goi(token, `${API}/files/${id}`, { method: 'DELETE' });
}
