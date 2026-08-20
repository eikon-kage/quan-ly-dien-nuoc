/**
 * Hộp thư chạy trên Supabase — bản hiện thực duy nhất của [HopThu](./hopThu.ts).
 *
 * Cùng đúng ba hàm `gui` / `doc` / `docSoCacTho`, nên màn hình đối chiếu và toàn bộ phần tính
 * toán không biết và không cần biết sổ đang được đặt ở đâu.
 *
 * Một chỗ đáng nói: **không có câu lọc "chỉ lấy phần của tôi" trong app.** Máy
 * thợ gọi `select` cả bảng thì Postgres tự cắt còn đúng hai dòng của nó, theo RLS. App viết
 * sai cũng không lộ được dữ liệu người khác — chặn nằm ở database, xem thiet-lap.sql.
 */

import { docSo } from './goiSo';
import { HopThu, SoDaNhan } from './hopThu';
import { khach } from './khachSupabase';
import { LoiNhom, thanhVienCuaToi } from './nhomSupabase';
import { SoCong, Vai } from './soCong';

const BANG = 'so_cong';

/** Một hàng trong bảng `so_cong`. */
interface Hang {
  tho_id: string;
  nguon: string;
  ten_tho: string | null;
  tu_ngay: string;
  den_ngay: string;
  dongs: unknown;
  tao_luc: string;
}

const COT = 'tho_id, nguon, ten_tho, tu_ngay, den_ngay, dongs, tao_luc';

function doiSang(hang: Hang): SoDaNhan {
  // Đi qua đúng bộ kiểm dùng cho file sao lưu: dữ liệu từ database cũng là dữ liệu từ ngoài
  // vào, không được tin sẵn chỉ vì nó đến từ Postgres.
  const so = docSo({
    thoId: hang.tho_id,
    tenTho: hang.ten_tho ?? '',
    nguon: hang.nguon,
    tuNgay: hang.tu_ngay,
    denNgay: hang.den_ngay,
    dongs: hang.dongs,
    taoLuc: hang.tao_luc,
  });

  return { so, suaLuc: hang.tao_luc };
}

async function nhomId(): Promise<string> {
  const toi = await thanhVienCuaToi();
  if (!toi) {
    throw new LoiNhom('Máy này chưa ở trong nhóm nào. Chủ lập nhóm, thợ dán mã mời.');
  }
  return toi.nhomId;
}

function nemNeuLoi(error: { message: string } | null): void {
  if (!error) {
    return;
  }
  const chu = error.message.toLowerCase();
  if (chu.includes('row-level security') || chu.includes('violates')) {
    // Gặp câu này nghĩa là app vừa thử ghi thứ nó không có quyền ghi — lỗi lập trình, không
    // phải lỗi người dùng. Nói rõ để lần ra, đừng che thành "thử lại sau".
    throw new LoiNhom('Máy này không có quyền ghi sổ đó.', error.message);
  }
  throw new LoiNhom('Chưa gửi được sổ lên nhóm. Thử lại sau.', error.message);
}

export function hopThuSupabase(): HopThu {
  return {
    async gui(so) {
      // Lấy nhóm trước rồi mới mở câu ghi: nhét `await` vào giữa biểu thức thì thứ tự gọi
      // ngược với thứ tự đọc, ai sửa sau cũng phải ngồi luận.
      const nhom = await nhomId();

      const { error } = await khach()
        .from(BANG)
        .upsert(
          {
            nhom_id: nhom,
            tho_id: so.thoId,
            nguon: so.nguon,
            ten_tho: so.tenTho,
            tu_ngay: so.tuNgay,
            den_ngay: so.denNgay,
            dongs: so.dongs,
            tao_luc: so.taoLuc,
          },
          // Mỗi (nhóm, thợ, bên gửi) đúng một hàng, ghi đè mãi lên nó — giống hệt cách bên
          // hộp thư cũ ghi đè lên một file.
          { onConflict: 'nhom_id,tho_id,nguon' },
        );

      nemNeuLoi(error);
    },

    async doc(thoId: string, nguon: Vai) {
      const { data, error } = await khach()
        .from(BANG)
        .select(COT)
        .eq('tho_id', thoId)
        .eq('nguon', nguon)
        .maybeSingle();

      nemNeuLoi(error);
      return data ? doiSang(data as Hang) : null;
    },

    async docSoCacTho() {
      const { data, error } = await khach().from(BANG).select(COT).eq('nguon', 'tho');
      nemNeuLoi(error);

      const daNhan: SoDaNhan[] = [];
      for (const hang of (data ?? []) as Hang[]) {
        try {
          daNhan.push(doiSang(hang));
        } catch {
          // Một sổ hỏng thì bỏ sổ ấy, đừng làm cả màn hình đối chiếu trắng xoá. Thợ đó vẫn
          // hiện là "chưa gửi sổ", nhìn ra ngay là có chuyện.
        }
      }
      return daNhan;
    },
  };
}
