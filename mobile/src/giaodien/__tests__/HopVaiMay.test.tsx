/**
 * Hộp "Máy này là của ai" — chỗ thợ dán mã mời.
 *
 * Điều quan trọng nhất được kiểm ở đây: **mã sai thì máy không được đổi vai.** Đổi vai trước
 * rồi mới đổi mã là máy đã thành máy thợ — không còn thấy tiền, không còn danh sách thợ — mà
 * nhóm thì vẫn chưa vào được. Người dùng mắc cạn giữa hai trạng thái.
 *
 * Và `thoId` phải lấy từ **câu trả lời của database**, không phải từ chữ người dùng gõ vào:
 * đó là id của thợ trong sổ chủ, hai máy đặt id khác nhau thì lúc đối chiếu không ghép được
 * ai với ai.
 */

import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { ThanhVien } from '../../nghiepvu/nhomSupabase';
import { cham, themTho, themUng } from '../../nghiepvu/thaoTac';
import { CaiDatVai, MAC_DINH } from '../../nghiepvu/vaiMay';
import { DieuKhienNhom } from '../dungSupabase';
import { HopVaiMay } from '../HopVaiMay';

const THO_TRONG_SO_CHU: ThanhVien = { nhomId: 'n1', vai: 'tho', thoId: 'mf3k2a-9xq1' };

function nhomGia(
  doiMa: DieuKhienNhom['doiMa'],
  sua: Partial<DieuKhienNhom['trangThai']> = {},
): DieuKhienNhom {
  return {
    trangThai: {
      hoTro: true,
      taiKhoan: null,
      thanhVien: null,
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
    doiMa,
    ngat: jest.fn(() => Promise.resolve()),
  };
}

function dung(nhom: DieuKhienNhom, duLieu: DuLieuChamCong = duLieuRong(), caiDat = MAC_DINH) {
  const capNhat = jest.fn();
  const datCaiDat = jest.fn();
  const onDong = jest.fn();

  render(
    <HopVaiMay
      duLieu={duLieu}
      capNhat={capNhat}
      caiDat={caiDat}
      datCaiDat={datCaiDat}
      nhom={nhom}
      onDong={onDong}
    />,
  );

  return { capNhat, datCaiDat, onDong };
}

/** Mở sang bước gõ mã. */
function moONhapMa() {
  fireEvent.press(screen.getByText('Máy của thợ'));
  return screen.getByPlaceholderText('K7MQP4');
}

describe('dán mã mời', () => {
  test('gọi đúng mã người dùng gõ, rồi đặt vai theo id database trả về', async () => {
    const doiMa = jest.fn(() => Promise.resolve(THO_TRONG_SO_CHU));
    const { capNhat, datCaiDat, onDong } = dung(nhomGia(doiMa));

    fireEvent.changeText(moONhapMa(), 'k7mqp4');
    fireEvent.press(screen.getByText('Xong'));

    await waitFor(() => expect(doiMa).toHaveBeenCalledWith('k7mqp4'));

    // Vai máy mang đúng id của sổ chủ, không phải chữ người dùng gõ.
    await waitFor(() =>
      expect(datCaiDat).toHaveBeenCalledWith(
        expect.objectContaining({ vai: 'tho', thoId: 'mf3k2a-9xq1' }),
      ),
    );

    // Và sổ trên máy có bản ghi thợ mang đúng id ấy để buổi công móc vào.
    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(moi.thos.map((tho) => tho.id)).toEqual(['mf3k2a-9xq1']);
    expect(onDong).toHaveBeenCalled();
  });

  test('mã trắng thì không gọi database', () => {
    const doiMa = jest.fn(() => Promise.resolve(THO_TRONG_SO_CHU));
    const { datCaiDat } = dung(nhomGia(doiMa));

    moONhapMa();
    fireEvent.press(screen.getByText('Xong'));

    expect(doiMa).not.toHaveBeenCalled();
    expect(datCaiDat).not.toHaveBeenCalled();
    expect(screen.getByText('Anh dán mã mời của chủ vào đây nhé.')).toBeTruthy();
  });

  /** Chỗ quan trọng nhất của file này. */
  test('mã sai thì máy vẫn là máy chủ, và hiện đúng câu lỗi của nhóm', async () => {
    const doiMa = jest.fn(() => Promise.resolve(null));
    const { capNhat, datCaiDat, onDong } = dung(
      nhomGia(doiMa, { loi: 'Mã mời không dùng được. Xin chủ phát mã mới.' }),
    );

    fireEvent.changeText(moONhapMa(), 'SAI999');
    fireEvent.press(screen.getByText('Xong'));

    await waitFor(() => expect(doiMa).toHaveBeenCalled());
    expect(datCaiDat).not.toHaveBeenCalled();
    expect(capNhat).not.toHaveBeenCalled();
    expect(onDong).not.toHaveBeenCalled();
    expect(screen.getByText('Mã mời không dùng được. Xin chủ phát mã mới.')).toBeTruthy();
  });

  /** Mã của một máy chủ: database ràng thợ phải có tho_id, nên tới đây là nhầm mã. */
  test('hàng không có thoId thì từ chối, không kết nạp thợ rỗng', async () => {
    const doiMa = jest.fn(() => Promise.resolve({ nhomId: 'n1', vai: 'chu', thoId: null } as ThanhVien));
    const { capNhat, datCaiDat } = dung(nhomGia(doiMa));

    fireEvent.changeText(moONhapMa(), 'K7MQP4');
    fireEvent.press(screen.getByText('Xong'));

    expect(await screen.findByText('Mã này không phải mã mời thợ. Xin chủ phát mã mới.')).toBeTruthy();
    expect(datCaiDat).not.toHaveBeenCalled();
    expect(capNhat).not.toHaveBeenCalled();
  });
});

describe('máy cũ của chủ chuyền tay cho thợ', () => {
  function khoCuaChu(): DuLieuChamCong {
    const mot = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
    const hai = themTho(mot.duLieu, 'Anh Bình', 250_000, '2026-08-01');
    return themUng(hai.duLieu, hai.tho.id, '2026-08-02', 500_000);
  }

  test('có nút xoá sổ người khác, và nó xoá thật', async () => {
    const doiMa = jest.fn(() => Promise.resolve(THO_TRONG_SO_CHU));
    const { capNhat } = dung(nhomGia(doiMa), khoCuaChu());

    fireEvent.changeText(moONhapMa(), 'K7MQP4');
    fireEvent.press(screen.getByText('Xoá sổ của người khác trên máy này'));

    await waitFor(() => expect(capNhat).toHaveBeenCalled());
    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(moi.thos.map((tho) => tho.id)).toEqual(['mf3k2a-9xq1']);
    expect(moi.ungTiens).toEqual([]);
  });

  test('máy mới cài thì không có nút ấy — hai nút làm cùng một việc là một chỗ để lạc', () => {
    dung(nhomGia(jest.fn(() => Promise.resolve(THO_TRONG_SO_CHU))));

    moONhapMa();

    expect(screen.queryByText('Xoá sổ của người khác trên máy này')).toBeNull();
  });
});

describe('những chỗ chặn trước', () => {
  test('app chưa điền cấu hình nhóm thì không mở ô nhập mã, và nói vì sao', () => {
    dung(nhomGia(jest.fn(), { hoTro: false }));

    fireEvent.press(screen.getByText('Máy của thợ'));

    expect(screen.queryByPlaceholderText('K7MQP4')).toBeNull();
    expect(screen.getByText(/chưa được điền địa chỉ nhóm/)).toBeTruthy();
  });

  /**
   * Thợ tự chấm trước rồi mới xin được mã: id cũ là id máy tự đặt, phải kéo hết buổi đã chấm
   * sang id thật **và giữ nguyên mốc bắt đầu chấm** — đặt lại mốc thành hôm nay thì mấy buổi
   * ấy rơi ra ngoài khoảng sổ khai là đầy đủ, đối chiếu bỏ qua sạch.
   */
  test('đã tự chấm trước thì buổi cũ theo sang id thật, mốc bắt đầu giữ nguyên', async () => {
    const caiDat: CaiDatVai = {
      vai: 'tho',
      thoId: 'tu-dat',
      batDauTu: '2026-08-10',
      thoIdTuTao: true,
    };
    let duLieu = themTho(duLieuRong(), 'Tôi', 0, '2026-08-10', 'tu-dat').duLieu;
    duLieu = cham(duLieu, 'tu-dat', '2026-08-12', 'Sang');

    const { capNhat, datCaiDat } = dung(
      nhomGia(jest.fn(() => Promise.resolve(THO_TRONG_SO_CHU))),
      duLieu,
      caiDat,
    );

    fireEvent.press(screen.getByText('Máy của thợ · dán lại mã mời'));
    fireEvent.changeText(screen.getByPlaceholderText('K7MQP4'), 'K7MQP4');
    fireEvent.press(screen.getByText('Xong'));

    await waitFor(() => expect(capNhat).toHaveBeenCalled());
    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(moi.thos.map((tho) => tho.id)).toEqual(['mf3k2a-9xq1']);
    expect(moi.buoiCongs.every((b) => b.thoId === 'mf3k2a-9xq1')).toBe(true);

    await waitFor(() =>
      expect(datCaiDat).toHaveBeenCalledWith({
        vai: 'tho',
        thoId: 'mf3k2a-9xq1',
        batDauTu: '2026-08-10',
        thoIdTuTao: false,
        dungMotMinh: false,
      }),
    );
  });

  /** Thợ bị ngắt khỏi nhóm, hay chủ phát lại mã: phải có đường dán mã lần nữa. */
  test('máy đã là máy thợ vẫn dán lại mã được', () => {
    const caiDat: CaiDatVai = { vai: 'tho', thoId: 'cu-roi', batDauTu: '2026-08-01' };
    dung(nhomGia(jest.fn(() => Promise.resolve(THO_TRONG_SO_CHU))), duLieuRong(), caiDat);

    fireEvent.press(screen.getByText('Máy của thợ · dán lại mã mời'));

    expect(screen.getByPlaceholderText('K7MQP4')).toBeTruthy();
  });
});
