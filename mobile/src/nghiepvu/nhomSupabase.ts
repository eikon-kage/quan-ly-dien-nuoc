/**
 * Nhóm chấm công trên Supabase: lập nhóm, phát mã mời, đổi mã mời.
 *
 * Ba việc này đi qua **hàm trong database** (`rpc`) chứ không phải app tự ghi bảng, và có lý
 * do cho từng cái:
 *
 *   tao_nhom    — sinh `nhom_id` phía database, gọi hai lần vẫn một nhóm. Để app tự sinh thì
 *                 bấm hai lần ra hai nhóm, và sổ nằm rải ở hai nơi.
 *   phat_ma_moi — mã phải do database sinh và **không ai đọc được bảng mã**. Cho app đọc bảng
 *                 ấy là máy nào cũng dò được mã mời của người khác rồi vào nhóm.
 *   doi_ma_moi  — kiểm mã còn hạn, chưa dùng, rồi ghi thành viên trong **một** bước. Chia làm
 *                 hai lệnh từ app thì hai máy cầm cùng một mã có thể vào được cả hai.
 *
 * Xem mobile/supabase/thiet-lap.sql, và bài kiểm tra phân quyền cạnh nó.
 */

import { khach } from './khachSupabase';
import { Vai } from './soCong';

export interface ThanhVien {
  nhomId: string;
  vai: Vai;
  /** Máy thợ: id thợ trong sổ chủ. Máy chủ là null. */
  thoId: string | null;
}

/** Lỗi đã dịch thành câu hiện được lên màn hình. */
export class LoiNhom extends Error {
  constructor(
    thongDiep: string,
    readonly goc?: string,
  ) {
    super(thongDiep);
  }
}

interface HangThanhVien {
  nhom_id: string;
  vai: string;
  tho_id: string | null;
}

function doiSang(hang: HangThanhVien): ThanhVien {
  if (hang.vai !== 'chu' && hang.vai !== 'tho') {
    throw new LoiNhom('Dữ liệu nhóm không đúng dạng.', `vai = ${hang.vai}`);
  }
  return { nhomId: hang.nhom_id, vai: hang.vai, thoId: hang.tho_id };
}

function dich(goc: string): LoiNhom {
  const chu = goc.toLowerCase();

  // Câu này do chính hàm doi_ma_moi ném ra, đã viết sẵn cho người dùng đọc.
  if (chu.includes('mã mời')) {
    return new LoiNhom(goc, goc);
  }
  if (chu.includes('network') || chu.includes('fetch') || chu.includes('timeout')) {
    return new LoiNhom('Không nối được mạng. Kiểm tra 3G hay wifi rồi thử lại.', goc);
  }
  // Bảng chưa dựng: người dựng app quên chạy file SQL. Nói thẳng ra, vì người gặp lỗi này
  // là người sửa được nó.
  if (chu.includes('does not exist') || chu.includes('schema cache')) {
    return new LoiNhom('Nhóm chưa được dựng trên Supabase. Cần chạy file thiet-lap.sql.', goc);
  }
  return new LoiNhom('Chưa vào được nhóm. Thử lại sau.', goc);
}

/**
 * Kiểu trả về của thư viện là một "builder" chứ không phải Promise thật — nó chỉ có `then`.
 * Nên khai `PromiseLike`, không khai `Promise`.
 */
type KetQua<T> = PromiseLike<{ data: T | null; error: { message: string } | null }>;

async function goi<T>(viec: () => KetQua<T>) {
  let ket: { data: T | null; error: { message: string } | null };
  try {
    ket = await viec();
  } catch (loi) {
    throw dich(loi instanceof Error ? loi.message : String(loi));
  }

  if (ket.error) {
    throw dich(ket.error.message);
  }
  return ket.data;
}

/** Máy này đã ở trong nhóm nào chưa. Chưa thì null — chưa phải lỗi. */
export async function thanhVienCuaToi(): Promise<ThanhVien | null> {
  // Không cần lọc theo user: RLS chỉ trả về đúng dòng của người đang đăng nhập.
  const hang = await goi<HangThanhVien>(() =>
    khach().from('thanh_vien').select('nhom_id, vai, tho_id').maybeSingle(),
  );
  return hang ? doiSang(hang) : null;
}

/** Máy chủ: lập nhóm mới, hoặc lấy lại nhóm đang có. */
export async function taoNhom(): Promise<ThanhVien> {
  const hang = await goi<HangThanhVien>(() => khach().rpc('tao_nhom'));
  if (!hang) {
    throw new LoiNhom('Chưa lập được nhóm. Thử lại sau.');
  }
  return doiSang(hang);
}

/** Máy chủ: phát mã mời cho một thợ, mặc định sống 3 ngày. */
export async function phatMaMoi(thoId: string): Promise<string> {
  const ma = await goi<string>(() => khach().rpc('phat_ma_moi', { p_tho_id: thoId }));
  if (!ma) {
    throw new LoiNhom('Chưa phát được mã mời. Thử lại sau.');
  }
  return ma;
}

/** Máy thợ: đổi mã mời lấy chỗ trong nhóm. Trả về cả `thoId` để máy thợ biết mình là ai. */
export async function doiMaMoi(ma: string): Promise<ThanhVien> {
  const hang = await goi<HangThanhVien>(() =>
    khach().rpc('doi_ma_moi', { p_ma: ma.trim().toUpperCase() }),
  );
  if (!hang) {
    throw new LoiNhom('Mã mời không dùng được. Xin chủ phát mã mới.');
  }
  return doiSang(hang);
}
