/**
 * Chuyển sổ cũ sang luật **một ngày một công**.
 *
 * Điều phải giữ ở đây là *tiền đã trả thì không đụng vào*: buổi nằm trong kỳ đã chốt giữ
 * nguyên số công của lúc quyết toán, vì tờ quyết toán là bản chụp và tiền đã sang tay.
 * Phần chưa chốt thì ngược lại — nó còn đang được nhân với tiền một công để ra bảng lương
 * kỳ này, nên phải sang luật mới cùng lúc với những buổi sắp chấm.
 */

import { BAN_LUAT_CONG, BuoiCong, chuanHoa, duLieuRong, KyLuong } from '../kieu';

function buoi(id: string, soCong: number): BuoiCong {
  return {
    id,
    thoId: 'tho-1',
    ngay: '2026-08-03',
    buoi: 'Sang',
    soCong,
    tienMotCong: null,
    ghiChu: '',
    suaLuc: '2026-08-03T10:00:00.000Z',
  };
}

function kyChot(buoiCongIds: string[]): KyLuong {
  return {
    id: 'ky-1',
    tuNgay: '2026-08-01',
    denNgay: '2026-08-10',
    chotLuc: '2026-08-10T18:00:00.000Z',
    ghiChu: '',
    dongs: [],
    buoiCongIds,
    ungTienIds: [],
  };
}

describe('sổ cũ đọc lên theo luật một ngày một công', () => {
  test('buổi chưa chốt bị chia đôi: một buổi giờ là nửa công', () => {
    const sau = chuanHoa({ ...duLieuRong(), banLuatCong: undefined, buoiCongs: [buoi('b1', 1)] });

    expect(sau.buoiCongs[0].soCong).toBe(0.5);
    expect(sau.banLuatCong).toBe(BAN_LUAT_CONG);
  });

  test('buổi đã nằm trong kỳ đã chốt thì giữ nguyên — tiền ấy đã trả rồi', () => {
    const sau = chuanHoa({
      ...duLieuRong(),
      banLuatCong: undefined,
      buoiCongs: [buoi('daChot', 1), buoi('chuaChot', 1)],
      kyLuongs: [kyChot(['daChot'])],
    });

    expect(sau.buoiCongs.map((b) => [b.id, b.soCong])).toEqual([
      ['daChot', 1],
      ['chuaChot', 0.5],
    ]);
  });

  test('đọc đi đọc lại không chia đôi thêm lần nữa', () => {
    const lanDau = chuanHoa({ ...duLieuRong(), banLuatCong: undefined, buoiCongs: [buoi('b1', 1)] });
    const lanHai = chuanHoa(JSON.parse(JSON.stringify(lanDau)));

    expect(lanHai.buoiCongs[0].soCong).toBe(0.5);
  });

  test('sổ trống vẫn là sổ trống sau khi chuyển', () => {
    expect(chuanHoa({}).buoiCongs).toEqual([]);
  });
});
