/**
 * Gộp sổ công theo ngày — thứ màn hình *Sổ công của tôi* trên máy thợ dựng lên.
 *
 * Sổ lưu theo buổi, người xem nghĩ theo ngày. Hai chỗ dễ sai: ngày chỉ đi một buổi, và
 * ranh giới "nghỉ" — ngoài khoảng sổ khai là đầy đủ thì không biết, chứ không phải nghỉ.
 */

import { duLieuRong } from '../kieu';
import { SoCong, gomTheoNgay, ngayNghiTrongSo, soCuaMay } from '../soCong';
import { cham, themTho } from '../thaoTac';

const TU = '2026-08-01';
const DEN = '2026-08-31';

function so(dongs: SoCong['dongs'], tuNgay = TU, denNgay = DEN): SoCong {
  return { thoId: 't1', tenTho: 'Anh Tuấn', nguon: 'tho', tuNgay, denNgay, dongs, taoLuc: '' };
}

describe('gomTheoNgay', () => {
  test('hai buổi cùng ngày về một dòng, cộng đúng tổng công', () => {
    const cac = gomTheoNgay(
      so([
        { ngay: '2026-08-03', buoi: 'Sang', soCong: 1 },
        { ngay: '2026-08-03', buoi: 'Chieu', soCong: 0.5 },
      ]),
      TU,
      DEN,
    );

    expect(cac).toEqual([
      { ngay: '2026-08-03', congSang: 1, congChieu: 0.5, tongCong: 1.5, daChot: false },
    ]);
  });

  test('ngày chỉ đi một buổi thì buổi kia là null, không phải 0', () => {
    const [ngay] = gomTheoNgay(so([{ ngay: '2026-08-04', buoi: 'Chieu', soCong: 1 }]), TU, DEN);

    expect(ngay.congSang).toBeNull();
    expect(ngay.congChieu).toBe(1);
  });

  test('bỏ dòng ngoài khoảng đang xem và xếp ngày tăng dần', () => {
    const cac = gomTheoNgay(
      so([
        { ngay: '2026-08-20', buoi: 'Sang', soCong: 1 },
        { ngay: '2026-08-02', buoi: 'Sang', soCong: 1 },
        { ngay: '2026-08-25', buoi: 'Sang', soCong: 1 },
      ]),
      '2026-08-01',
      '2026-08-15',
    );

    expect(cac.map((n) => n.ngay)).toEqual(['2026-08-02']);
  });

  test('có buổi đã chốt thì cả ngày mang cờ đã chốt', () => {
    const [ngay] = gomTheoNgay(
      so([
        { ngay: '2026-08-03', buoi: 'Sang', soCong: 1, daChot: true },
        { ngay: '2026-08-03', buoi: 'Chieu', soCong: 1 },
      ]),
      TU,
      DEN,
    );

    expect(ngay.daChot).toBe(true);
  });
});

describe('ngayNghiTrongSo', () => {
  test('ngày trong khoảng mà không có buổi nào là nghỉ', () => {
    const nghis = ngayNghiTrongSo(
      so([{ ngay: '2026-08-02', buoi: 'Sang', soCong: 1 }], '2026-08-01', '2026-08-03'),
      TU,
      DEN,
      '2026-08-03',
    );

    expect(nghis).toEqual(['2026-08-01', '2026-08-03']);
  });

  test('ngày chưa tới không phải nghỉ, chỉ là chưa chấm', () => {
    const nghis = ngayNghiTrongSo(so([], '2026-08-01', '2026-08-31'), TU, DEN, '2026-08-02');

    expect(nghis).toEqual(['2026-08-01', '2026-08-02']);
  });

  test('ngoài khoảng sổ khai là đầy đủ thì không kết luận là nghỉ', () => {
    // Máy thợ nhận mã mời hôm 10: chín ngày đầu tháng nó không biết gì.
    const nghis = ngayNghiTrongSo(
      so([], '2026-08-10', '2026-08-12'),
      TU,
      DEN,
      '2026-08-12',
    );

    expect(nghis).toEqual(['2026-08-10', '2026-08-11', '2026-08-12']);
  });

  test('xem tháng trước lúc sổ chỉ có từ tháng này thì không có ngày nghỉ nào', () => {
    const nghis = ngayNghiTrongSo(
      so([], '2026-08-01', '2026-08-31'),
      '2026-07-01',
      '2026-07-31',
      '2026-08-12',
    );

    expect(nghis).toEqual([]);
  });
});

/**
 * Sổ mà máy thợ gửi lên nhóm phải nói đủ những gì màn hình của nó đang hiện.
 *
 * Chỗ này từng làm mất công thật: mốc `batDauTu` đặt đúng hôm chọn vai máy, mà màn hình
 * lại mời chấm bù 13 ngày trước — buổi chấm bù rơi ngoài mốc là bị cắt khỏi sổ, chủ không
 * thấy và đối chiếu cũng không báo lệch.
 */
describe('soCuaMay trên máy thợ', () => {
  const HOM_NAY = '2026-08-20';
  const BAT_DAU = '2026-08-18';
  const may = { vai: 'tho' as const, batDauTu: BAT_DAU };

  function duLieuTho(...cac: { ngay: string; buoi: 'Sang' | 'Chieu' }[]) {
    let d = themTho(duLieuRong(), 'Tôi', 0, BAT_DAU, 't1').duLieu;
    for (const mot of cac) {
      d = cham(d, 't1', mot.ngay, mot.buoi, 1);
    }
    return d;
  }

  test('giữ buổi chấm bù trước mốc bắt đầu, và nới mốc dưới của sổ ra tới buổi ấy', () => {
    const soGui = soCuaMay(
      duLieuTho({ ngay: '2026-08-20', buoi: 'Sang' }, { ngay: '2026-08-14', buoi: 'Sang' }),
      may,
      't1',
      HOM_NAY,
    );

    expect(soGui.tuNgay).toBe('2026-08-14');
    expect(soGui.dongs.map((dong) => dong.ngay)).toEqual(['2026-08-14', '2026-08-20']);
  });

  test('không chấm bù gì thì mốc dưới vẫn là ngày bắt đầu, không nới bừa', () => {
    const soGui = soCuaMay(duLieuTho({ ngay: '2026-08-19', buoi: 'Sang' }), may, 't1', HOM_NAY);

    expect(soGui.tuNgay).toBe(BAT_DAU);
  });

  test('buổi của thợ khác trong máy không kéo mốc dưới đi', () => {
    let d = duLieuTho({ ngay: '2026-08-19', buoi: 'Sang' });
    d = themTho(d, 'Người khác', 0, BAT_DAU, 't2').duLieu;
    d = cham(d, 't2', '2026-07-01', 'Sang', 1);

    expect(soCuaMay(d, may, 't1', HOM_NAY).tuNgay).toBe(BAT_DAU);
  });
});
