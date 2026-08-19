/**
 * Màn hình đối chiếu.
 *
 * Điều quan trọng nhất được kiểm ở đây: **màn hình này không tự sửa sổ**. Nó chỉ gọi
 * `capNhat` khi người dùng bấm đúng vào nút của đúng một dòng, và buổi đã quyết toán thì
 * không có nút để mà bấm.
 */

import { fireEvent, render, screen } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { quyetToan } from '../../nghiepvu/ky';
import * as Ngay from '../../nghiepvu/ngayViet';
import { SoDaNhan } from '../../nghiepvu/hopThu';
import { SoCong, catSo } from '../../nghiepvu/soCong';
import { cham, dangCham, themTho } from '../../nghiepvu/thaoTac';
import { CaiDatVai, MAC_DINH } from '../../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from '../dungDoiChieu';
import { ManHinhDoiChieu } from '../ManHinhDoiChieu';

const HOM_NAY = Ngay.homNay();
const HOM_QUA = Ngay.congNgay(HOM_NAY, -1);

function kho(): { duLieu: DuLieuChamCong; thoId: string } {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, Ngay.congNgay(HOM_NAY, -30));
  return { duLieu: them.duLieu, thoId: them.tho.id };
}

/** Sổ thợ gửi lên, dựng bằng chính bộ cắt sổ để giống đường đi thật. */
function soCuaTho(duLieu: DuLieuChamCong, thoId: string): SoCong {
  return {
    ...catSo(duLieu, thoId, 'tho', Ngay.congNgay(HOM_NAY, -30), HOM_NAY, ''),
    // Sổ thợ không bao giờ mang cờ đã chốt: máy thợ không chốt kỳ.
    dongs: catSo(duLieu, thoId, 'tho', Ngay.congNgay(HOM_NAY, -30), HOM_NAY, '').dongs.map(
      ({ ngay, buoi, soCong }) => ({ ngay, buoi, soCong }),
    ),
  };
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
  dieuKhien: DieuKhienDoiChieu,
  capNhat = jest.fn(),
  caiDat: CaiDatVai = MAC_DINH,
) {
  render(
    <ManHinhDoiChieu
      duLieu={duLieu}
      capNhat={capNhat}
      caiDat={caiDat}
      dieuKhien={dieuKhien}
    />,
  );
  return capNhat;
}

describe('máy chủ', () => {
  test('thợ chưa gửi sổ thì nói rõ, không im lặng', () => {
    const { duLieu } = kho();
    dung(duLieu, dieuKhienGia());

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.getByText('Chưa gửi sổ lên')).toBeTruthy();
  });

  test('hai sổ khớp thì báo khớp', () => {
    const { duLieu, thoId } = kho();
    const daCham = cham(duLieu, thoId, HOM_QUA, 'Sang');

    dung(daCham, dieuKhienGia([{ so: soCuaTho(daCham, thoId), suaLuc: '' }]));
    expect(screen.getByText('Khớp cả 1 buổi')).toBeTruthy();
  });

  test('lệch thì đếm đúng số buổi, mở ra thấy hai con số cạnh nhau', () => {
    const { duLieu, thoId } = kho();
    const soChu = cham(duLieu, thoId, HOM_QUA, 'Sang', 0.5);
    const soTho = cham(duLieu, thoId, HOM_QUA, 'Sang', 1);

    dung(soChu, dieuKhienGia([{ so: soCuaTho(soTho, thoId), suaLuc: '' }]));

    expect(screen.getByText('Lệch 1 buổi · khớp 0')).toBeTruthy();

    fireEvent.press(screen.getByText('Anh Tuấn'));
    expect(screen.getByText('Sổ tôi')).toBeTruthy();
    expect(screen.getByText('Sổ thợ')).toBeTruthy();
    expect(screen.getByText('0,5 công')).toBeTruthy();
    expect(screen.getByText('1 công')).toBeTruthy();
  });

  test('bấm lấy theo sổ thợ thì ghi đúng số công của thợ vào sổ mình', () => {
    const { duLieu, thoId } = kho();
    const soChu = cham(duLieu, thoId, HOM_QUA, 'Sang', 0.5);
    const soTho = cham(duLieu, thoId, HOM_QUA, 'Sang', 1);

    const capNhat = dung(soChu, dieuKhienGia([{ so: soCuaTho(soTho, thoId), suaLuc: '' }]));

    fireEvent.press(screen.getByText('Anh Tuấn'));
    expect(capNhat).not.toHaveBeenCalled();

    fireEvent.press(screen.getByText('Lấy theo sổ thợ'));

    expect(capNhat).toHaveBeenCalledTimes(1);
    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(dangCham(moi, thoId, HOM_QUA, 'Sang')?.soCong).toBe(1);
  });

  test('buổi đã quyết toán thì không có nút để bấm', () => {
    const { duLieu, thoId } = kho();
    let soChu = cham(duLieu, thoId, HOM_QUA, 'Sang', 1);
    soChu = quyetToan(soChu, { denNgay: HOM_QUA, daTra: new Map([[thoId, 300_000]]) });

    const soTho = cham(duLieu, thoId, HOM_QUA, 'Sang', 2);
    dung(soChu, dieuKhienGia([{ so: soCuaTho(soTho, thoId), suaLuc: '' }]));

    fireEvent.press(screen.getByText('Anh Tuấn'));
    expect(screen.getByText('Đã trả tiền')).toBeTruthy();
    expect(screen.queryByText('Lấy theo sổ thợ')).toBeNull();
  });
});

describe('máy thợ', () => {
  const CAI_DAT: CaiDatVai = { vai: 'tho', thoId: '', batDauTu: Ngay.congNgay(HOM_NAY, -10) };

  test('vào thẳng chi tiết, không qua danh sách thợ', () => {
    const { duLieu, thoId } = kho();
    const soTho = cham(duLieu, thoId, HOM_QUA, 'Sang');
    const soChu: SoCong = { ...soCuaTho(soTho, thoId), nguon: 'chu', tenTho: 'Anh Tuấn' };

    dung(soTho, dieuKhienGia([{ so: soChu, suaLuc: '' }]), jest.fn(), {
      ...CAI_DAT,
      thoId,
    });

    // Chữ "Sổ chủ" chỉ có ở phần chi tiết, và gọi bên kia là *chủ* chứ không phải *thợ*.
    expect(screen.getByText('Hai sổ khớp nhau')).toBeTruthy();
    expect(screen.queryByText('Thợ khác')).toBeNull();
  });
});
