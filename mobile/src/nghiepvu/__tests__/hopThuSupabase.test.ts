/**
 * Hộp thư chạy trên Supabase.
 *
 * Ở đây không gọi mạng: kiểm app **gửi đúng cái gì lên bảng nào**, và kiểm nó đọc dữ liệu từ
 * database qua đúng bộ kiểm dùng cho file sao lưu. Phần chặn quyền thì không kiểm được bằng
 * hàng giả — nó nằm trong database, và có bài riêng chạy trên Postgres thật:
 * mobile/supabase/kiem-tra-rls.sql.
 */

let gioBang: ReturnType<typeof taoGioBang>;
let gioRpc: jest.Mock;
let bangDaGoi: string[];

jest.mock('../khachSupabase', () => ({
  khach: () => ({
    from: (ten: string) => {
      bangDaGoi.push(ten);
      return gioBang;
    },
    rpc: (...tham: unknown[]) => gioRpc(...tham),
  }),
}));

/**
 * Hàng giả của PostgREST: mọi hàm lọc trả về chính nó để nối chuỗi được, và bản thân nó
 * `await` được — đúng cách thư viện thật hoạt động.
 */
function taoGioBang(ketQua: { data: unknown; error: { message: string } | null }) {
  const loc: [string, unknown][] = [];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- hàng giả tự tham chiếu
  const q: any = {
    loc,
    daUpsert: null as unknown,
    tuyChonUpsert: null as unknown,
    select: jest.fn(() => q),
    eq: jest.fn((cot: string, gia: unknown) => {
      loc.push([cot, gia]);
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

const THANH_VIEN_CHU = { nhom_id: 'nhom-1', vai: 'chu', tho_id: null };

const HANG_SO_THO = {
  tho_id: 'tho-tuan',
  nguon: 'tho',
  ten_tho: 'Anh Tuấn',
  tu_ngay: '2026-08-09',
  den_ngay: '2026-08-19',
  dongs: [{ ngay: '2026-08-18', buoi: 'Sang', soCong: 1 }],
  tao_luc: '2026-08-19T02:00:00.000Z',
};

import { hopThuSupabase } from '../hopThuSupabase';
import { LoiNhom, doiMaMoi, phatMaMoi, taoNhom, thanhVienCuaToi } from '../nhomSupabase';
import { SoCong } from '../soCong';

const SO: SoCong = {
  thoId: 'tho-tuan',
  tenTho: 'Anh Tuấn',
  nguon: 'chu',
  tuNgay: '2026-05-21',
  denNgay: '2026-08-19',
  dongs: [{ ngay: '2026-08-18', buoi: 'Sang', soCong: 1 }],
  taoLuc: '2026-08-19T10:00:00.000Z',
};

beforeEach(() => {
  bangDaGoi = [];
  gioRpc = jest.fn();
  gioBang = taoGioBang({ data: THANH_VIEN_CHU, error: null });
});

describe('nhóm', () => {
  it('chưa vào nhóm nào thì trả null, không phải lỗi', async () => {
    gioBang = taoGioBang({ data: null, error: null });
    await expect(thanhVienCuaToi()).resolves.toBeNull();
  });

  it('đọc dòng thành viên của mình mà không tự lọc theo user — RLS lo việc đó', async () => {
    await expect(thanhVienCuaToi()).resolves.toEqual({
      nhomId: 'nhom-1',
      vai: 'chu',
      thoId: null,
    });
    expect(bangDaGoi).toEqual(['thanh_vien']);
    expect(gioBang.loc).toEqual([]);
  });

  it('lập nhóm đi qua hàm trong database chứ không tự ghi bảng', async () => {
    gioRpc.mockResolvedValue({ data: THANH_VIEN_CHU, error: null });

    await expect(taoNhom()).resolves.toEqual({ nhomId: 'nhom-1', vai: 'chu', thoId: null });
    expect(gioRpc).toHaveBeenCalledWith('tao_nhom');
  });

  it('phát mã mời cho đúng thợ được hỏi', async () => {
    gioRpc.mockResolvedValue({ data: 'K7MQP4', error: null });

    await expect(phatMaMoi('tho-tuan')).resolves.toBe('K7MQP4');
    expect(gioRpc).toHaveBeenCalledWith('phat_ma_moi', { p_tho_id: 'tho-tuan' });
  });

  it('đổi mã mời: tha cho khoảng trắng và chữ thường, vì mã đọc qua điện thoại', async () => {
    gioRpc.mockResolvedValue({
      data: { nhom_id: 'nhom-1', vai: 'tho', tho_id: 'tho-tuan' },
      error: null,
    });

    await expect(doiMaMoi(' k7mqp4 ')).resolves.toEqual({
      nhomId: 'nhom-1',
      vai: 'tho',
      thoId: 'tho-tuan',
    });
    expect(gioRpc).toHaveBeenCalledWith('doi_ma_moi', { p_ma: 'K7MQP4' });
  });

  it('giữ nguyên câu database nói về mã mời, vì câu ấy viết cho người dùng', async () => {
    gioRpc.mockResolvedValue({
      data: null,
      error: { message: 'Mã mời không dùng được. Xin chủ phát mã mới.' },
    });

    await expect(doiMaMoi('SAI123')).rejects.toThrow('Mã mời không dùng được. Xin chủ phát mã mới.');
  });

  it('chưa chạy file SQL thì nói thẳng ra', async () => {
    gioRpc.mockResolvedValue({
      data: null,
      error: { message: 'relation "public.thanh_vien" does not exist' },
    });

    await expect(taoNhom()).rejects.toThrow(/thiet-lap\.sql/);
  });
});

describe('gửi sổ', () => {
  it('ghi đúng một hàng, ghi đè lên hàng cũ của cùng (nhóm, thợ, bên gửi)', async () => {
    await hopThuSupabase().gui(SO);

    expect(bangDaGoi).toEqual(['thanh_vien', 'so_cong']);
    expect(gioBang.daUpsert).toEqual({
      nhom_id: 'nhom-1',
      tho_id: 'tho-tuan',
      nguon: 'chu',
      ten_tho: 'Anh Tuấn',
      tu_ngay: '2026-05-21',
      den_ngay: '2026-08-19',
      dongs: SO.dongs,
      tao_luc: SO.taoLuc,
    });
    expect(gioBang.tuyChonUpsert).toEqual({ onConflict: 'nhom_id,tho_id,nguon' });
  });

  it('không mang theo đồng tiền nào — soát cả hàng sắp ghi', async () => {
    await hopThuSupabase().gui(SO);

    const chu = JSON.stringify(gioBang.daUpsert);
    expect(chu).not.toContain('tienMotCong');
    expect(chu).not.toContain('soTien');
  });

  it('chưa vào nhóm thì nói rõ phải làm gì, không quăng lỗi máy móc', async () => {
    gioBang = taoGioBang({ data: null, error: null });

    await expect(hopThuSupabase().gui(SO)).rejects.toThrow(/chưa ở trong nhóm nào/i);
  });

  it('bị RLS chặn thì báo là lỗi quyền, không che thành "thử lại sau"', async () => {
    gioBang = taoGioBang({
      data: null,
      error: { message: 'new row violates row-level security policy for table "so_cong"' },
    });
    // Lần gọi đầu (đọc thành viên) cũng dùng hàng giả này nên phải cho nó trả về thành viên.
    gioBang.maybeSingle = jest.fn(() => Promise.resolve({ data: THANH_VIEN_CHU, error: null }));

    const loi = await hopThuSupabase()
      .gui(SO)
      .then(() => null, (l: unknown) => l);

    expect(loi).toBeInstanceOf(LoiNhom);
    expect((loi as LoiNhom).message).toMatch(/không có quyền ghi/);
  });
});

describe('đọc sổ', () => {
  it('đọc sổ của một thợ theo bên gửi', async () => {
    gioBang = taoGioBang({ data: HANG_SO_THO, error: null });

    const daNhan = await hopThuSupabase().doc('tho-tuan', 'tho');

    expect(gioBang.loc).toEqual([
      ['tho_id', 'tho-tuan'],
      ['nguon', 'tho'],
    ]);
    expect(daNhan?.so.tenTho).toBe('Anh Tuấn');
    expect(daNhan?.so.dongs).toEqual([{ ngay: '2026-08-18', buoi: 'Sang', soCong: 1 }]);
    expect(daNhan?.suaLuc).toBe('2026-08-19T02:00:00.000Z');
  });

  it('chưa ai gửi thì trả null', async () => {
    gioBang = taoGioBang({ data: null, error: null });
    await expect(hopThuSupabase().doc('tho-tuan', 'chu')).resolves.toBeNull();
  });

  it('máy chủ đọc mọi sổ thợ, bỏ qua hàng hỏng', async () => {
    const hong = { ...HANG_SO_THO, tho_id: 'hong', dongs: [{ ngay: 'không phải ngày' }] };
    gioBang = taoGioBang({ data: [HANG_SO_THO, hong], error: null });

    const cac = await hopThuSupabase().docSoCacTho();

    expect(gioBang.loc).toEqual([['nguon', 'tho']]);
    expect(cac.map((d) => d.so.thoId)).toEqual(['tho-tuan']);
  });
});
