/**
 * Màn hình đối chiếu.
 *
 * Điều quan trọng nhất được kiểm ở đây: **màn hình này không tự sửa sổ**. Nó chỉ gọi
 * `capNhat` khi người dùng bấm đúng vào nút của đúng một dòng, và buổi đã quyết toán thì
 * không có nút để mà bấm.
 */

import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { quyetToan } from '../../nghiepvu/ky';
import * as Ngay from '../../nghiepvu/ngayViet';
import { SoDaNhan } from '../../nghiepvu/hopThu';
import { SoCong, catSo } from '../../nghiepvu/soCong';
import { cham, dangCham, themTho } from '../../nghiepvu/thaoTac';
import { CaiDatVai, MAC_DINH } from '../../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from '../dungDoiChieu';
import { DieuKhienNhom } from '../dungSupabase';
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
    trangThai: {
      ketNoi: { sanSang: true, chuaSanSang: null },
      dangChay: false,
      lucCuoi: null,
      loi: null,
    },
    soBenKia: new Map(cac.map((daNhan) => [daNhan.so.thoId, daNhan])),
    dongBo: jest.fn(() => Promise.resolve()),
  };
}

/** Nhóm Supabase giả: máy chủ đã vào nhóm, phát mã được. */
function nhomGia(sua: Partial<DieuKhienNhom['trangThai']> = {}): DieuKhienNhom {
  return {
    trangThai: {
      hoTro: true,
      taiKhoan: { userId: 'u1', email: 'chu@cuahang.vn', anDanh: false },
      thanhVien: { nhomId: 'n1', vai: 'chu', thoId: null },
      dangDoc: false,
      traHut: false,
      dangChay: false,
      loi: null,
      nhac: null,
      ...sua,
    },
    noiEmail: jest.fn(() => Promise.resolve()),
    taoTaiKhoan: jest.fn(() => Promise.resolve()),
    lapNhom: jest.fn(() => Promise.resolve()),
    phatMa: jest.fn(() => Promise.resolve('K7MQP4')),
    doiMa: jest.fn(() => Promise.resolve(null)),
    ngat: jest.fn(() => Promise.resolve()),
  };
}

function dung(
  duLieu: DuLieuChamCong,
  dieuKhien: DieuKhienDoiChieu,
  capNhat = jest.fn(),
  caiDat: CaiDatVai = MAC_DINH,
  nhom: DieuKhienNhom = nhomGia(),
) {
  render(
    <ManHinhDoiChieu
      duLieu={duLieu}
      capNhat={capNhat}
      caiDat={caiDat}
      dieuKhien={dieuKhien}
      nhom={nhom}
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

  /**
   * Máy thợ vừa cài xong, chưa chấm ô nào: **không được hiện dòng lệch nào cả**. Ngày còn
   * đang chạy, thợ chưa chấm không có nghĩa là thợ nói "hôm nay tôi nghỉ" — mà đây lại đúng
   * là màn hình đầu tiên người ta thấy, hiện đỏ ở đấy là mất lòng tin ngay.
   */
  test('chưa chấm gì mà chủ đã chấm hôm nay thì chưa báo lệch', () => {
    const { duLieu, thoId } = kho();
    const cuaChu = cham(cham(duLieu, thoId, HOM_NAY, 'Sang'), thoId, HOM_NAY, 'Chieu');
    const soChu: SoCong = {
      ...catSo(cuaChu, thoId, 'chu', Ngay.congNgay(HOM_NAY, -90), HOM_NAY, ''),
      tenTho: 'Anh Tuấn',
    };

    // Sổ của máy thợ khai đúng từ hôm nay: hôm nay mới nhận mã mời.
    dung(duLieu, dieuKhienGia([{ so: soChu, suaLuc: '' }]), jest.fn(), {
      ...CAI_DAT,
      thoId,
      batDauTu: HOM_NAY,
    });

    expect(screen.queryByText('Lấy theo sổ chủ')).toBeNull();
    expect(screen.queryByText('Chưa chấm')).toBeNull();
    expect(screen.getByText('Chưa có gì để so')).toBeTruthy();
    // Gác thì phải nói ra, kẻo hai tổng ở trên nói khác màn hình chấm công.
    expect(screen.getByText(/Hôm nay còn dở: 2 buổi/)).toBeTruthy();
  });

  test('hôm nay hai bên đều chấm mà lệch số công thì vẫn báo', () => {
    const { duLieu, thoId } = kho();
    const cuaToi = cham(duLieu, thoId, HOM_NAY, 'Sang', 0.5);
    const soChu: SoCong = {
      ...catSo(cham(duLieu, thoId, HOM_NAY, 'Sang', 1), thoId, 'chu', Ngay.congNgay(HOM_NAY, -90), HOM_NAY, ''),
      tenTho: 'Anh Tuấn',
    };

    dung(cuaToi, dieuKhienGia([{ so: soChu, suaLuc: '' }]), jest.fn(), {
      ...CAI_DAT,
      thoId,
      batDauTu: HOM_NAY,
    });

    expect(screen.getByText('Lấy theo sổ chủ')).toBeTruthy();
    expect(screen.getByText('0,5 công')).toBeTruthy();
    expect(screen.getByText('1 công')).toBeTruthy();
  });

  test('lấy tên do chủ đặt, không phải chữ "Tôi" đặt tạm lúc nhận mã mời', () => {
    // Máy thợ để tên nội bộ là "Tôi" cho tới khi sổ chủ về. Nếu màn hình này lấy tên nội bộ
    // trước thì màn hình chính gọi "Anh Tuấn" mà mở đối chiếu ra lại thành "Tôi".
    const them = themTho(duLieuRong(), 'Tôi', 0, Ngay.congNgay(HOM_NAY, -10));
    const thoId = them.tho.id;
    const soChu: SoCong = { ...soCuaTho(them.duLieu, thoId), nguon: 'chu', tenTho: 'Anh Tuấn' };

    dung(them.duLieu, dieuKhienGia([{ so: soChu, suaLuc: '' }]), jest.fn(), {
      ...CAI_DAT,
      thoId,
    });

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.queryByText('Tôi')).toBeNull();
  });
});

describe('phát mã mời cho thợ chưa gửi sổ', () => {
  test('máy chủ: bấm là phát cho đúng thợ đang xem, rồi hiện mã ra đọc', async () => {
    const { duLieu, thoId } = kho();
    const nhom = nhomGia();
    // Chưa thợ nào gửi sổ lên: đúng lúc chủ cần cái mã.
    dung(duLieu, dieuKhienGia(), jest.fn(), MAC_DINH, nhom);

    fireEvent.press(screen.getByText('Anh Tuấn'));
    fireEvent.press(screen.getByText('Phát mã mời'));

    await waitFor(() => expect(nhom.phatMa).toHaveBeenCalledWith(thoId));
    expect(await screen.findByText('K7MQP4')).toBeTruthy();
    expect(screen.getByText(/dán mã. Mã dùng một lần và sống ba ngày/)).toBeTruthy();
  });

  test('phát hụt thì hiện câu lỗi của nhóm, không hiện mã nào', async () => {
    const { duLieu } = kho();
    const nhom = nhomGia({ loi: 'Chỉ máy chủ phát được mã mời.' });
    nhom.phatMa = jest.fn(() => Promise.resolve(null));
    dung(duLieu, dieuKhienGia(), jest.fn(), MAC_DINH, nhom);

    fireEvent.press(screen.getByText('Anh Tuấn'));
    fireEvent.press(screen.getByText('Phát mã mời'));

    await waitFor(() => expect(nhom.phatMa).toHaveBeenCalled());
    expect(screen.getByText('Chỉ máy chủ phát được mã mời.')).toBeTruthy();
    expect(screen.queryByText('K7MQP4')).toBeNull();
  });

  /** Máy thợ không phát mã cho ai — nó chỉ đợi sổ chủ gửi xuống. */
  test('máy thợ không có nút phát mã, và nói đúng việc phải làm', () => {
    const { duLieu, thoId } = kho();
    const caiDat: CaiDatVai = { vai: 'tho', thoId, batDauTu: Ngay.congNgay(HOM_NAY, -30) };
    dung(duLieu, dieuKhienGia(), jest.fn(), caiDat);

    expect(screen.queryByText('Phát mã mời')).toBeNull();
    expect(screen.getByText(/Chủ chưa gửi sổ xuống/)).toBeTruthy();
  });
});

