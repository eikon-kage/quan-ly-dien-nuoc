/**
 * Màn hình riêng của máy thợ.
 *
 * Hai điều phải giữ: bấm một lần là chấm được hôm nay, và **trên màn hình này không có
 * một con số tiền nào** — kể cả khi sổ trong máy còn sót mốc lương từ lúc máy này từng là
 * máy chủ.
 */

import { fireEvent, render, screen } from '@testing-library/react-native';

import { SoDaNhan } from '../../nghiepvu/hopThu';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { SoCong, catSo } from '../../nghiepvu/soCong';
import { cham, dangCham, themTho } from '../../nghiepvu/thaoTac';
import { CaiDatVai } from '../../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from '../dungDoiChieu';
import { ManHinhThoTuCham } from '../ManHinhThoTuCham';

const HOM_NAY = Ngay.homNay();
const HOM_QUA = Ngay.congNgay(HOM_NAY, -1);
const BAT_DAU = Ngay.congNgay(HOM_NAY, -10);

/** Máy thợ với đúng một thợ — nhưng cố tình để tiền công 300.000 để soi chuyện ẩn tiền. */
function kho(): { duLieu: DuLieuChamCong; thoId: string; caiDat: CaiDatVai } {
  const them = themTho(duLieuRong(), 'Tôi', 300_000, BAT_DAU);
  return {
    duLieu: them.duLieu,
    thoId: them.tho.id,
    caiDat: { vai: 'tho', thoId: them.tho.id, batDauTu: BAT_DAU },
  };
}

function soChuGuiXuong(duLieu: DuLieuChamCong, thoId: string, tenTho: string): SoCong {
  return { ...catSo(duLieu, thoId, 'chu', BAT_DAU, HOM_NAY, ''), nguon: 'chu', tenTho };
}

function dieuKhienGia(cac: SoDaNhan[] = []): DieuKhienDoiChieu {
  return {
    trangThai: { hoTro: true, daNoi: true, dangChay: false, lucCuoi: null, loi: null },
    soBenKia: new Map(cac.map((daNhan) => [daNhan.so.thoId, daNhan])),
    dongBo: jest.fn(() => Promise.resolve()),
    noiGoogle: jest.fn(() => Promise.resolve()),
  };
}

function dung(
  duLieu: DuLieuChamCong,
  caiDat: CaiDatVai,
  dieuKhien = dieuKhienGia(),
  capNhat = jest.fn(),
) {
  render(
    <ManHinhThoTuCham
      duLieu={duLieu}
      capNhat={capNhat}
      caiDat={caiDat}
      datCaiDat={jest.fn()}
      dieuKhien={dieuKhien}
    />,
  );
  return capNhat;
}

test('hôm nay đứng riêng một thẻ, bấm một lần là chấm', () => {
  const { duLieu, thoId, caiDat } = kho();
  const capNhat = dung(duLieu, caiDat);

  expect(screen.getByText(`Hôm nay, ${Ngay.thuVaNgay(HOM_NAY)}`)).toBeTruthy();

  fireEvent.press(screen.getAllByText('Sáng')[0]);

  const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
  expect(dangCham(moi, thoId, HOM_NAY, 'Sang')?.soCong).toBe(1);
});

test('bấm lại vào buổi đã chấm là bỏ chấm', () => {
  const { duLieu, thoId, caiDat } = kho();
  const daCham = cham(duLieu, thoId, HOM_NAY, 'Sang');
  const capNhat = dung(daCham, caiDat);

  fireEvent.press(screen.getAllByText('Sáng')[0]);

  const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
  expect(dangCham(moi, thoId, HOM_NAY, 'Sang')).toBeUndefined();
});

test('không hiện một con số tiền nào, dù trong sổ vẫn còn mốc lương', () => {
  const { duLieu, caiDat } = kho();
  dung(duLieu, caiDat);

  expect(screen.queryByText(/300\.000/)).toBeNull();
  expect(screen.queryByText(/đ$/)).toBeNull();
  expect(screen.queryByText(/lương/i)).toBeNull();
});

test('lấy tên từ sổ chủ gửi xuống, không bắt thợ tự gõ', () => {
  const { duLieu, thoId, caiDat } = kho();
  dung(duLieu, caiDat, dieuKhienGia([
    { so: soChuGuiXuong(duLieu, thoId, 'Anh Tuấn'), suaLuc: '' },
  ]));

  expect(screen.getByText('Anh Tuấn')).toBeTruthy();
});

test('dải đối chiếu đếm số buổi lệch với sổ chủ', () => {
  const { duLieu, thoId, caiDat } = kho();
  const soToi = cham(duLieu, thoId, HOM_QUA, 'Sang');

  dung(soToi, caiDat, dieuKhienGia([
    // Chủ không chấm hôm qua: lệch một buổi.
    { so: soChuGuiXuong(duLieu, thoId, 'Anh Tuấn'), suaLuc: '' },
  ]));

  expect(screen.getByText('Đối chiếu với sổ chủ')).toBeTruthy();
  expect(screen.getByText('Lệch 1 buổi')).toBeTruthy();
});

test('chưa có sổ chủ thì nói rõ chứ không hiện số 0 buổi lệch', () => {
  const { duLieu, caiDat } = kho();
  dung(duLieu, caiDat);

  expect(screen.getByText('Chưa có sổ của chủ')).toBeTruthy();
});
