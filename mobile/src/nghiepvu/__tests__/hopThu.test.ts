/**
 * Hộp thư và gói sổ công.
 *
 * Điều phải giữ, và là lý do file này tồn tại: **hộp thư và bản sao lưu sống chung một chỗ
 * trên Drive mà không được đụng nhau.** Bên sao lưu có hàm dọn bản cũ chỉ giữ 30 bản gần
 * nhất; nếu tên file sổ lỡ khớp khuôn tên bản sao lưu thì cứ sao lưu vài lần là hộp thư
 * bị dọn sạch, mà lỗi ấy chỉ hiện ra sau một tháng dùng.
 */

const layToken = jest.fn(() => Promise.resolve('token-1'));
jest.mock('../dangNhapGoogle', () => ({
  accessToken: () => layToken(),
  boTokenDangGiu: () => {},
}));

jest.mock('../goiDrive', () => {
  const that = jest.requireActual('../goiDrive');
  return {
    LoiDrive: that.LoiDrive,
    danhSach: (...tham: unknown[]) => gioDanhSach(...tham),
    taoFile: (...tham: unknown[]) => gioTaoFile(...tham),
    ghiDe: (...tham: unknown[]) => gioGhiDe(...tham),
    taiVe: (...tham: unknown[]) => gioTaiVe(...tham),
    xoa: jest.fn(),
  };
});

const gioDanhSach = jest.fn();
const gioTaoFile = jest.fn();
const gioGhiDe = jest.fn();
const gioTaiVe = jest.fn();

import { ngayTuTenFile, tenFileSaoLuu } from '../goiSaoLuu';
import { SoHong, dongGoiSo, moGoiSo } from '../goiSo';
import { docTenFileSo, hopThuDrive, tenFileSo } from '../hopThu';
import { SoCong } from '../soCong';

const SO: SoCong = {
  thoId: 'mf3k2a-9xq1',
  tenTho: 'Anh Tuấn',
  nguon: 'tho',
  tuNgay: '2026-08-01',
  denNgay: '2026-08-19',
  dongs: [{ ngay: '2026-08-10', buoi: 'Sang', soCong: 1 }],
  taoLuc: '2026-08-19T08:00:00.000Z',
};

function fileDrive(ten: string) {
  return { id: `id-${ten}`, ten, suaLuc: '2026-08-19T09:00:00Z' };
}

beforeEach(() => {
  [layToken, gioDanhSach, gioTaoFile, gioGhiDe, gioTaiVe].forEach((gia) => gia.mockReset());
  layToken.mockResolvedValue('token-1');
  gioDanhSach.mockResolvedValue([]);
  gioTaoFile.mockImplementation((_t: string, ten: string) => Promise.resolve(fileDrive(ten)));
  gioGhiDe.mockImplementation((_t: string, id: string) => Promise.resolve(fileDrive(id)));
});

describe('tên file trong hộp thư', () => {
  it('đọc lại được bên gửi và thợ', () => {
    expect(docTenFileSo(tenFileSo('chu', SO.thoId))).toEqual({ nguon: 'chu', thoId: SO.thoId });
    expect(docTenFileSo(tenFileSo('tho', SO.thoId))).toEqual({ nguon: 'tho', thoId: SO.thoId });
  });

  it('không lẫn với tên bản sao lưu, theo cả hai chiều', () => {
    // Bản sao lưu không bị nhận là sổ...
    expect(docTenFileSo(tenFileSaoLuu('2026-08-19'))).toBeNull();
    // ...và sổ không bị nhận là bản sao lưu, nên hàm dọn bản cũ không xoá nó.
    expect(ngayTuTenFile(tenFileSo('tho', SO.thoId))).toBeNull();
    expect(ngayTuTenFile(tenFileSo('chu', SO.thoId))).toBeNull();
  });

  it('bỏ qua file lạ', () => {
    expect(docTenFileSo('Cham-cong-so-ai-do-abc.json')).toBeNull();
    expect(docTenFileSo('bang-luong.xlsx')).toBeNull();
  });
});

describe('gói sổ', () => {
  it('đóng rồi mở ra y như cũ', () => {
    expect(moGoiSo(dongGoiSo(SO))).toEqual(SO);
  });

  it('giữ cờ đã chốt', () => {
    const co = { ...SO, dongs: [{ ...SO.dongs[0], daChot: true }] };
    expect(moGoiSo(dongGoiSo(co)).dongs[0].daChot).toBe(true);
  });

  it('từ chối file không phải sổ', () => {
    expect(() => moGoiSo('không phải json')).toThrow(SoHong);
    // Gói sao lưu mang nhãn khác — nuốt vào là báo lệch sạch cả tháng.
    expect(() => moGoiSo(JSON.stringify({ app: 'cham-cong', phienBan: 1, duLieu: {} }))).toThrow(SoHong);
  });

  it('từ chối sổ thiếu khoảng ngày hay ngược ngày', () => {
    expect(() => moGoiSo(dongGoiSo({ ...SO, tuNgay: '' }))).toThrow(SoHong);
    expect(() => moGoiSo(dongGoiSo({ ...SO, tuNgay: '2026-08-20' }))).toThrow(SoHong);
  });

  it('từ chối dòng có số công không hợp lệ', () => {
    for (const soCong of [0, -1, Number.NaN]) {
      const xau = { ...SO, dongs: [{ ...SO.dongs[0], soCong }] };
      expect(() => moGoiSo(dongGoiSo(xau))).toThrow(SoHong);
    }
  });

  it('từ chối sổ của bản app mới hơn', () => {
    const tuTuongLai = JSON.stringify({ app: 'cham-cong-so', phienBan: 99, so: SO });
    expect(() => moGoiSo(tuTuongLai)).toThrow(/cập nhật app/);
  });
});

describe('hopThuDrive', () => {
  it('lần đầu thì tạo file, lần sau ghi đè lên đúng file ấy', async () => {
    const hopThu = hopThuDrive();

    await hopThu.gui(SO);
    expect(gioTaoFile).toHaveBeenCalledWith('token-1', tenFileSo('tho', SO.thoId), dongGoiSo(SO));

    gioDanhSach.mockResolvedValue([fileDrive(tenFileSo('tho', SO.thoId))]);
    await hopThu.gui(SO);

    expect(gioGhiDe).toHaveBeenCalledWith('token-1', `id-${tenFileSo('tho', SO.thoId)}`, dongGoiSo(SO));
    expect(gioTaoFile).toHaveBeenCalledTimes(1);
  });

  it('chưa ai gửi sổ thì trả null chứ không quăng lỗi', async () => {
    await expect(hopThuDrive().doc(SO.thoId, 'chu')).resolves.toBeNull();
  });

  it('đọc sổ chủ gửi cho đúng thợ đó', async () => {
    const soChu = { ...SO, nguon: 'chu' as const };
    gioDanhSach.mockResolvedValue([fileDrive(tenFileSo('chu', SO.thoId))]);
    gioTaiVe.mockResolvedValue(dongGoiSo(soChu));

    const daNhan = await hopThuDrive().doc(SO.thoId, 'chu');
    expect(daNhan?.so).toEqual(soChu);
    expect(daNhan?.suaLuc).toBe('2026-08-19T09:00:00Z');
  });

  it('máy chủ đọc mọi sổ thợ, bỏ qua sổ hỏng và file không phải sổ thợ', async () => {
    const soKhac: SoCong = { ...SO, thoId: 'kh4c-1111', tenTho: 'Anh Bình' };

    gioDanhSach.mockResolvedValue([
      fileDrive(tenFileSo('tho', SO.thoId)),
      fileDrive(tenFileSo('tho', 'hong-0000')),
      fileDrive(tenFileSo('tho', soKhac.thoId)),
      fileDrive(tenFileSo('chu', SO.thoId)), // sổ chủ tự gửi, không phải sổ thợ
      fileDrive(tenFileSaoLuu('2026-08-19')), // bản sao lưu
    ]);
    gioTaiVe.mockImplementation((_t: string, id: string) => {
      if (id.includes('hong-0000')) {
        return Promise.resolve('{}');
      }
      return Promise.resolve(dongGoiSo(id.includes(soKhac.thoId) ? soKhac : SO));
    });

    const cac = await hopThuDrive().docSoCacTho();
    expect(cac.map((d) => d.so.thoId)).toEqual([SO.thoId, soKhac.thoId]);
  });
});
