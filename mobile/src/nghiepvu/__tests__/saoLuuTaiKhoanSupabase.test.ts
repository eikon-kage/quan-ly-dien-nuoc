/**
 * Bản sao lưu trên tài khoản, ruột Supabase.
 *
 * Ở đây không gọi mạng: kiểm app **ghi đúng cái gì lên bảng nào**, kiểm nó đọc dữ liệu từ
 * database qua đúng bộ kiểm dùng cho file sao lưu, và kiểm phép dọn bản cũ. Phần chặn quyền —
 * ai đọc được sổ của ai — nằm trong database và có bài riêng chạy trên Postgres thật:
 * mobile/supabase/kiem-tra-rls.sql.
 */

let gioBang: ReturnType<typeof taoGioBang>;
let bangDaGoi: string[];

jest.mock('../khachSupabase', () => ({
  hoTro: () => true,
  khach: () => ({
    from: (ten: string) => {
      bangDaGoi.push(ten);
      return gioBang;
    },
  }),
}));

jest.mock('../dangNhapSupabase', () => ({
  taiKhoanDaLuu: () => Promise.resolve({ userId: 'chu-1', email: 'chu@cuahang.vn', anDanh: false }),
}));

/**
 * Hàng giả của PostgREST: mọi hàm lọc trả về chính nó để nối chuỗi được, và bản thân nó
 * `await` được — đúng cách thư viện thật hoạt động.
 */
function taoGioBang(ketQua: { data: unknown; error: { message: string } | null }) {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- hàng giả tự tham chiếu
  const q: any = {
    cotDaChon: null as string | null,
    daUpsert: null as unknown,
    tuyChonUpsert: null as unknown,
    daXoa: null as unknown,
    coXoa: false,
    select: jest.fn((cot?: string) => {
      q.cotDaChon = cot ?? null;
      return q;
    }),
    order: jest.fn(() => q),
    eq: jest.fn(() => q),
    in: jest.fn((_cot: string, gia: unknown) => {
      q.daXoa = gia;
      return Promise.resolve({ data: null, error: null });
    }),
    delete: jest.fn(() => {
      q.coXoa = true;
      return q;
    }),
    maybeSingle: jest.fn(() => Promise.resolve(ketQua)),
    upsert: jest.fn((hang: unknown, tuyChon: unknown) => {
      q.daUpsert = hang;
      q.tuyChonUpsert = tuyChon;
      return Promise.resolve(ketQua);
    }),
    then: (nhan: (g: unknown) => unknown, hong?: (l: unknown) => unknown) =>
      Promise.resolve(ketQua).then(nhan, hong),
  };
  return q;
}

import { GoiHong, PHIEN_BAN, goiTu } from '../goiSaoLuu';
import { duLieuRong } from '../kieu';
import { LoiSaoLuuTaiKhoan, saoLuuTaiKhoanSupabase } from '../saoLuuTaiKhoanSupabase';
import { themTho } from '../thaoTac';

const KHO = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01').duLieu;

beforeEach(() => {
  bangDaGoi = [];
  gioBang = taoGioBang({ data: null, error: null });
});

describe('đẩy sổ lên tài khoản', () => {
  test('ghi cả gói vào bảng sao_luu, mỗi (tài khoản, ngày) một hàng', async () => {
    const ban = await saoLuuTaiKhoanSupabase().day(KHO, '2026-08-20');

    expect(bangDaGoi[0]).toBe('sao_luu');
    expect(gioBang.daUpsert).toMatchObject({
      user_id: 'chu-1',
      ngay: '2026-08-20',
      goi: { app: 'cham-cong', phienBan: PHIEN_BAN, duLieu: KHO },
    });
    // Ghi đè lên bản của đúng ngày ấy, không sinh hàng thứ hai.
    expect(gioBang.tuyChonUpsert).toEqual({ onConflict: 'user_id,ngay' });
    expect(ban.ngay).toBe('2026-08-20');
    expect(ban.suaLuc).toBe(gioBang.daUpsert.sua_luc);
  });

  test('sau khi đẩy thì dọn, chỉ giữ 30 ngày gần nhất', async () => {
    // 32 ngày: cả tháng tám rồi sang mùng một tháng chín.
    const ngays = [
      ...Array.from({ length: 31 }, (_, i) => `2026-08-${String(i + 1).padStart(2, '0')}`),
      '2026-09-01',
    ];
    gioBang = taoGioBang({ data: ngays.map((ngay) => ({ ngay })), error: null });

    await saoLuuTaiKhoanSupabase().day(KHO, '2026-09-01');

    expect(gioBang.coXoa).toBe(true);
    // Hai ngày cũ nhất, mới nhất đứng đầu.
    expect(gioBang.daXoa).toEqual(['2026-08-02', '2026-08-01']);
  });

  test('bảng chưa dựng thì nói thẳng là phải chạy lại file SQL', async () => {
    gioBang = taoGioBang({
      data: null,
      error: { message: `relation "public.sao_luu" does not exist` },
    });

    await expect(saoLuuTaiKhoanSupabase().day(KHO, '2026-08-20')).rejects.toThrow(
      /chạy lại file thiet-lap.sql/i,
    );
  });

  test('tài khoản không có quyền ghi (máy thợ ẩn danh) thì nói ra vai nào mới đẩy được', async () => {
    gioBang = taoGioBang({
      data: null,
      error: { message: 'new row violates row-level security policy for table "sao_luu"' },
    });

    await expect(saoLuuTaiKhoanSupabase().day(KHO, '2026-08-20')).rejects.toThrow(
      /máy chủ đăng nhập bằng email/i,
    );
  });
});

describe('đọc các bản trên tài khoản', () => {
  test('danh sách không kéo cột goi về — mỗi bản là cả một sổ', async () => {
    gioBang = taoGioBang({
      data: [{ ngay: '2026-08-20', sua_luc: '2026-08-20T09:12:00.000Z' }],
      error: null,
    });

    const cacBan = await saoLuuTaiKhoanSupabase().danhSach();

    expect(gioBang.cotDaChon).toBe('ngay, sua_luc');
    expect(cacBan).toEqual([{ ngay: '2026-08-20', suaLuc: '2026-08-20T09:12:00.000Z' }]);
  });

  test('bản đọc về đi qua đúng bộ kiểm của file sao lưu', async () => {
    gioBang = taoGioBang({ data: { goi: goiTu(KHO, '2026-08-20T09:00:00.000Z') }, error: null });

    const duLieu = await saoLuuTaiKhoanSupabase().docBan('2026-08-20');

    expect(duLieu).toEqual(KHO);
  });

  test('hàng bị sửa tay thành gói của bản app mới hơn thì từ chối, không nuốt bừa', async () => {
    gioBang = taoGioBang({
      data: { goi: { ...goiTu(KHO, ''), phienBan: PHIEN_BAN + 1 } },
      error: null,
    });

    await expect(saoLuuTaiKhoanSupabase().docBan('2026-08-20')).rejects.toBeInstanceOf(GoiHong);
  });

  test('bản đã bị xoá thì nói rõ, không trả về sổ trống', async () => {
    gioBang = taoGioBang({ data: null, error: null });

    await expect(saoLuuTaiKhoanSupabase().docBan('2026-08-20')).rejects.toBeInstanceOf(
      LoiSaoLuuTaiKhoan,
    );
  });
});
