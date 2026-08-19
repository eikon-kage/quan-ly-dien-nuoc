/**
 * Đóng gói một sổ công thành file JSON để đặt vào hộp thư, và mở gói ấy ra lúc nhận.
 *
 * Nhãn app khác hẳn nhãn của bản sao lưu (`cham-cong` ở goiSaoLuu): hai loại file nằm
 * cùng một chỗ trên Drive, mà nuốt lẫn nhau thì hậu quả nặng — mở một bản sao lưu ra rồi
 * tưởng là sổ đối chiếu sẽ báo lệch sạch cả tháng, còn ngược lại thì tệ hơn nữa.
 */

import { CAC_BUOI } from './kieu';
import { DongCong, SoCong, Vai } from './soCong';

/** Nhãn nhận dạng. Đừng đổi — file cũ trong hộp thư vẫn mang nhãn cũ. */
const NHAN = 'cham-cong-so';

export const PHIEN_BAN = 1;

/** File nhận được không phải sổ công, hoặc đã hỏng. */
export class SoHong extends Error {}

export function dongGoiSo(so: SoCong): string {
  return JSON.stringify({ app: NHAN, phienBan: PHIEN_BAN, so });
}

function laNgay(gia: unknown): gia is string {
  return typeof gia === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(gia);
}

function docDong(daDoc: unknown): DongCong {
  const dong = (daDoc ?? {}) as Partial<DongCong>;

  if (!laNgay(dong.ngay)) {
    throw new SoHong('Sổ có dòng không ghi rõ ngày.');
  }
  if (dong.buoi === undefined || !CAC_BUOI.includes(dong.buoi)) {
    throw new SoHong('Sổ có dòng không rõ buổi sáng hay chiều.');
  }
  // Số công phải là số dương thật: 0 hay số âm mà nuốt vào thì đối chiếu ra những dòng
  // lệch vô nghĩa, mà bấm lấy theo bên kia là `cham` quăng lỗi giữa màn hình.
  if (typeof dong.soCong !== 'number' || !Number.isFinite(dong.soCong) || dong.soCong <= 0) {
    throw new SoHong('Sổ có dòng ghi số công không hợp lệ.');
  }

  const ket: DongCong = { ngay: dong.ngay, buoi: dong.buoi, soCong: dong.soCong };
  if (dong.daChot === true) {
    ket.daChot = true;
  }
  return ket;
}

/**
 * Mở gói và kiểm kỹ từng dòng rồi mới trả về.
 *
 * Sổ này đi vào một màn hình có nút *ghi vào sổ mình*, nên thà từ chối oan còn hơn nhận
 * bừa một file rác rồi để nó dẫn tới ghi sai số công.
 */
export function moGoiSo(noiDung: string): SoCong {
  let daDoc: unknown;
  try {
    daDoc = JSON.parse(noiDung);
  } catch {
    throw new SoHong('File này không phải JSON đọc được.');
  }

  const goi = (daDoc ?? {}) as { app?: unknown; phienBan?: unknown; so?: unknown };
  if (goi.app !== NHAN) {
    throw new SoHong('File này không phải sổ công của app chấm công.');
  }
  if (typeof goi.phienBan !== 'number' || goi.phienBan > PHIEN_BAN) {
    throw new SoHong('Sổ này của bản app mới hơn. Hãy cập nhật app rồi thử lại.');
  }

  const so = (goi.so ?? {}) as Partial<SoCong>;
  const nguon: Vai | undefined =
    so.nguon === 'chu' || so.nguon === 'tho' ? so.nguon : undefined;

  if (typeof so.thoId !== 'string' || so.thoId === '' || nguon === undefined) {
    throw new SoHong('Sổ không ghi rõ của thợ nào, hay do bên nào gửi.');
  }
  if (!laNgay(so.tuNgay) || !laNgay(so.denNgay) || so.tuNgay > so.denNgay) {
    throw new SoHong('Sổ không ghi rõ khoảng ngày.');
  }
  if (!Array.isArray(so.dongs)) {
    throw new SoHong('Sổ thiếu phần các buổi công.');
  }

  return {
    thoId: so.thoId,
    tenTho: typeof so.tenTho === 'string' ? so.tenTho : '',
    nguon,
    tuNgay: so.tuNgay,
    denNgay: so.denNgay,
    dongs: so.dongs.map(docDong),
    taoLuc: typeof so.taoLuc === 'string' ? so.taoLuc : '',
  };
}
