/**
 * Ruột Supabase của [saoLuuTaiKhoan](./saoLuuTaiKhoan.ts): bảng `sao_luu`, mỗi ngày một hàng.
 *
 * Giống hộp thư ở một điểm quan trọng: **không có câu lọc "chỉ lấy của tôi" trong app.** Gọi
 * `select` cả bảng thì Postgres tự cắt còn đúng những hàng của tài khoản đang đăng nhập, theo
 * RLS. App viết sai cũng không đọc được sổ của người khác — chặn nằm ở database.
 *
 * Nhưng lúc **ghi** thì vẫn phải điền `user_id`, vì nó nằm trong khoá chính. Lấy từ phiên đăng
 * nhập đang có, không nhận từ bên gọi: bên gọi mà truyền sai một uuid thì RLS chặn, và câu lỗi
 * hiện ra sẽ là "không có quyền ghi" — đúng chữ nhưng chỉ sai chỗ.
 */

import { taiKhoanDaLuu } from './dangNhapSupabase';
import { docGoi, goiTu, ngayCanXoa } from './goiSaoLuu';
import { khach, hoTro as hoTroSupabase } from './khachSupabase';
import { DuLieuChamCong } from './kieu';
import { BanTaiKhoan, KhoTaiKhoan } from './saoLuuTaiKhoan';

const BANG = 'sao_luu';

/**
 * Giữ 30 ngày gần nhất, đúng bằng bên sao lưu vào máy. Hai con số lệch nhau thì người dùng
 * nhìn hai danh sách dài khác nhau trên cùng một màn hình mà không hiểu vì sao.
 */
const SO_BAN_GIU = 30;

/** Lỗi đã dịch thành câu hiện được lên màn hình. */
export class LoiSaoLuuTaiKhoan extends Error {
  constructor(
    thongDiep: string,
    /** Câu gốc của Supabase, giữ lại để còn lần ra nguyên nhân. */
    readonly goc?: string,
  ) {
    super(thongDiep);
  }
}

function dich(goc: string): LoiSaoLuuTaiKhoan {
  const chu = goc.toLowerCase();

  if (chu.includes('network') || chu.includes('fetch') || chu.includes('timeout')) {
    return new LoiSaoLuuTaiKhoan('Không nối được mạng. Kiểm tra 3G hay wifi rồi thử lại.', goc);
  }
  // Bảng chưa dựng: người dựng app chạy file SQL của bản trước. Nói thẳng, vì người gặp lỗi
  // này chính là người sửa được nó.
  if (chu.includes('does not exist') || chu.includes('schema cache')) {
    return new LoiSaoLuuTaiKhoan(
      'Chỗ sao lưu trên tài khoản chưa được dựng. Cần chạy lại file thiet-lap.sql.',
      goc,
    );
  }
  if (chu.includes('row-level security') || chu.includes('violates')) {
    // Máy thợ đăng nhập ẩn danh rơi vào đây (policy chặn), và đó là chuyện đúng — nói ra vai
    // nào mới đẩy được, đừng để người dùng ngồi thử lại.
    return new LoiSaoLuuTaiKhoan(
      'Tài khoản này không sao lưu được. Đường này chỉ dành cho máy chủ đăng nhập bằng email.',
      goc,
    );
  }
  return new LoiSaoLuuTaiKhoan('Chưa nối được chỗ sao lưu trên tài khoản. Thử lại sau.', goc);
}

function nemNeuLoi(error: { message: string } | null): void {
  if (error) {
    throw dich(error.message);
  }
}

/** Hàng trong bảng `sao_luu`, phần app đọc lên. */
interface Hang {
  ngay: string;
  sua_luc: string;
}

async function userId(): Promise<string> {
  const taiKhoan = await taiKhoanDaLuu();
  if (taiKhoan === null) {
    throw new LoiSaoLuuTaiKhoan('Máy này chưa đăng nhập, chưa sao lưu lên tài khoản được.');
  }
  return taiKhoan.userId;
}

export function saoLuuTaiKhoanSupabase(): KhoTaiKhoan {
  /**
   * Xoá bớt bản cũ, chỉ giữ `SO_BAN_GIU` ngày gần nhất.
   *
   * Hỏng thì nuốt lỗi, y như bên sao lưu vào máy: dọn dẹp là việc phụ, để nó làm hỏng kết quả
   * "đã sao lưu xong" thì người dùng tưởng sổ chưa lên trong khi nó đã lên rồi.
   */
  async function donBanCu(): Promise<void> {
    try {
      const { data, error } = await khach().from(BANG).select('ngay');
      if (error || !data) {
        return;
      }

      const canXoa = ngayCanXoa(
        (data as { ngay: string }[]).map((hang) => hang.ngay),
        SO_BAN_GIU,
      );
      if (canXoa.length > 0) {
        await khach().from(BANG).delete().in('ngay', canXoa);
      }
    } catch {
      // Lượt sao lưu sau dọn tiếp.
    }
  }

  return {
    hoTro: hoTroSupabase,

    async day(duLieu: DuLieuChamCong, ngay: string) {
      // Lấy tài khoản trước rồi mới mở câu ghi, không nhét `await` vào giữa biểu thức: thứ tự
      // gọi mà ngược thứ tự đọc thì ai sửa sau cũng phải ngồi luận.
      const nguoi = await userId();
      const suaLuc = new Date().toISOString();

      const { error } = await khach()
        .from(BANG)
        .upsert(
          { user_id: nguoi, ngay, goi: goiTu(duLieu, suaLuc), sua_luc: suaLuc },
          // Mỗi (tài khoản, ngày) đúng một hàng: sao lưu lần thứ hai trong ngày thì ghi đè lên
          // bản của ngày hôm ấy, giống hệt cách bên kia ghi đè lên file của ngày hôm ấy.
          { onConflict: 'user_id,ngay' },
        );

      nemNeuLoi(error);
      await donBanCu();

      return { ngay, suaLuc };
    },

    async danhSach(): Promise<BanTaiKhoan[]> {
      // Không lấy cột `goi`: danh sách chỉ để hiện "còn giữ tới hôm nào", mà mỗi bản là cả một
      // sổ. Kéo về hết là mỗi lần mở màn hình Sao lưu tốn mấy megabyte 3G.
      const { data, error } = await khach()
        .from(BANG)
        .select('ngay, sua_luc')
        .order('ngay', { ascending: false });

      nemNeuLoi(error);
      return ((data ?? []) as Hang[]).map((hang) => ({ ngay: hang.ngay, suaLuc: hang.sua_luc }));
    },

    async docBan(ngay: string): Promise<DuLieuChamCong> {
      const { data, error } = await khach()
        .from(BANG)
        .select('goi')
        .eq('ngay', ngay)
        .maybeSingle();

      nemNeuLoi(error);
      if (!data) {
        throw new LoiSaoLuuTaiKhoan('Bản này không còn trên tài khoản. Thử lấy bản khác.');
      }

      // Qua đúng bộ kiểm của file sao lưu: hàng này sửa tay được trong SQL Editor, và có thể do
      // một bản app mới hơn ghi lên.
      return docGoi((data as { goi: unknown }).goi).duLieu;
    },
  };
}
