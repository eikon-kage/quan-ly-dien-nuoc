/**
 * Gói sổ công — mẩu dữ liệu hai máy trao cho nhau qua hộp thư.
 *
 * Điều phải giữ: **mọi thứ đọc từ ngoài vào đều phải qua `moGoiSo`**, dù đến từ file hay từ
 * một hàng trong database. Nuốt bừa một gói sai khuôn thì màn hình đối chiếu báo lệch sạch
 * cả tháng, mà không ai biết vì sao.
 *
 * Trước đây nằm trong hopThu.test.ts, chuyển ra khi bỏ hộp thư Drive.
 */

import { SoHong, dongGoiSo, moGoiSo } from '../goiSo';
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
    expect(() => moGoiSo(JSON.stringify({ app: 'cham-cong', phienBan: 1, duLieu: {} }))).toThrow(
      SoHong,
    );
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
