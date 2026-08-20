/**
 * Màn hình mở đầu — chọn vai máy rồi nối nhóm, ngay lúc mở app.
 *
 * Bốn điều phải giữ:
 *   1. **Hỏi hai bước**: vai máy trước, cách vào sau. Mỗi vai một cách khác nhau, mà người
 *      dùng chỉ trả lời được câu "tôi là ai" chứ không trả lời được câu "tôi bấm cái nào".
 *   2. Bước hai luôn có **đường đi tiếp không cần email cũng không cần mã mời** — và đó là
 *      một cái nút bấm được, không phải một câu chữ an ủi.
 *   3. **Không chặn đường người chỉ muốn chấm công.** Nút *Để sau* luôn có.
 *   4. Đã đăng nhập mà chưa vào nhóm thì hỏi câu khác: máy chủ được nút thử lập nhóm, máy
 *      thợ được đường dán mã mời — chứ không phải bắt đăng nhập lại, vì đăng nhập lại chẳng
 *      thêm được cái nhóm nào.
 */

import { fireEvent, render, screen } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { CaiDatVai, MAC_DINH } from '../../nghiepvu/vaiMay';
import { DieuKhienNhom } from '../dungSupabase';
import { ManHinhMoDau } from '../ManHinhMoDau';

function nhomGia(sua: Partial<DieuKhienNhom['trangThai']> = {}): DieuKhienNhom {
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
    doiMa: jest.fn(() => Promise.resolve(null)),
    ngat: jest.fn(() => Promise.resolve()),
  };
}

const MAY_THO: CaiDatVai = { vai: 'tho', thoId: 'mf3k2a-9xq1', batDauTu: null };

function dung(
  nhom: DieuKhienNhom = nhomGia(),
  caiDat: CaiDatVai = MAC_DINH,
  duLieu: DuLieuChamCong = duLieuRong(),
) {
  const onDeSau = jest.fn();
  const datCaiDat = jest.fn();
  const capNhat = jest.fn();

  render(
    <ManHinhMoDau
      duLieu={duLieu}
      capNhat={capNhat}
      caiDat={caiDat}
      datCaiDat={datCaiDat}
      nhom={nhom}
      onDeSau={onDeSau}
    />,
  );

  return { nhom, onDeSau, datCaiDat, capNhat };
}

describe('bước một: vai máy', () => {
  it('hỏi vai trước, chưa hỏi cách vào', () => {
    dung();

    expect(screen.getByText('Máy này là của ai')).toBeTruthy();
    expect(screen.getByText('Tôi là chủ')).toBeTruthy();
    expect(screen.getByText('Tôi là thợ')).toBeTruthy();
    // Cách vào là câu hỏi của bước sau, chưa hiện ở đây.
    expect(screen.queryByText('Đăng nhập bằng email')).toBeNull();
    expect(screen.queryByText('Dán mã mời của chủ')).toBeNull();
  });

  it('luôn có đường vào chấm công, không nối cũng vào được', () => {
    const { onDeSau } = dung();

    fireEvent.press(screen.getByText('Để sau, vào chấm công đã'));

    expect(onDeSau).toHaveBeenCalled();
  });

  it('chọn vai rồi vẫn quay lại được', () => {
    dung();

    fireEvent.press(screen.getByText('Tôi là thợ'));
    fireEvent.press(screen.getByText('Chọn lại vai máy'));

    expect(screen.getByText('Máy này là của ai')).toBeTruthy();
  });
});

describe('bước hai: chủ', () => {
  it('có ô đăng nhập bằng email', () => {
    dung();

    fireEvent.press(screen.getByText('Tôi là chủ'));
    fireEvent.press(screen.getByText('Đăng nhập bằng email'));

    expect(screen.getByText('Email của chủ')).toBeTruthy();
  });

  /**
   * Nhà có ba thợ, chấm bằng một cái điện thoại, chẳng cần ai đối chiếu. Đó là một *quyết
   * định*, nên nó ghi vào máy — lần mở app sau App không hỏi lại (xem `hoiNoiNhom`).
   */
  it('cho dùng một mình, không cần email, và nhớ luôn lựa chọn ấy', () => {
    const { datCaiDat, onDeSau } = dung();

    fireEvent.press(screen.getByText('Tôi là chủ'));
    fireEvent.press(screen.getByText('Dùng một mình, không cần email'));

    expect(datCaiDat).toHaveBeenCalledWith(expect.objectContaining({ dungMotMinh: true }));
    expect(onDeSau).toHaveBeenCalled();
  });
});

describe('bước hai: thợ', () => {
  it('có đường dán mã mời', () => {
    dung();

    fireEvent.press(screen.getByText('Tôi là thợ'));
    fireEvent.press(screen.getByText('Dán mã mời của chủ'));

    expect(screen.getByText('Mã mời của chủ')).toBeTruthy();
    // Người dùng vừa nói mình là thợ; hỏi lại vai máy là một cú bấm không mang thêm tin gì.
    expect(screen.queryByText('Máy của chủ')).toBeNull();
  });

  /**
   * Thợ tải app về giữa tuần, chủ đang ngoài công trình: phải chấm được ngay. Máy thành máy
   * thợ với id **tự đặt** — cờ `thoIdTuTao` là thứ để lần dán mã sau kéo hết mấy buổi đã
   * chấm sang id thật của sổ chủ.
   */
  it('chưa có mã cũng chấm được: máy thành máy thợ với id tự đặt', () => {
    const { datCaiDat, capNhat, onDeSau } = dung();

    fireEvent.press(screen.getByText('Tôi là thợ'));
    fireEvent.press(screen.getByText('Chưa có mã, tự chấm trước'));

    const moi = datCaiDat.mock.calls[0][0] as CaiDatVai;
    expect(moi.vai).toBe('tho');
    expect(moi.thoId).toBeTruthy();
    expect(moi.thoIdTuTao).toBe(true);
    expect(moi.batDauTu).toBe(Ngay.homNay());
    // Có bản ghi thợ trong sổ, kẻo chấm vào một id không có ai.
    const duLieuMoi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(duLieuMoi.thos.map((tho) => tho.id)).toContain(moi.thoId);
    expect(onDeSau).toHaveBeenCalled();
  });

  it('máy đã là máy thợ thì giữ nguyên id cũ, không sinh người thứ hai', () => {
    const { datCaiDat } = dung(nhomGia(), MAY_THO);

    fireEvent.press(screen.getByText('Tôi là thợ'));
    fireEvent.press(screen.getByText('Chưa có mã, tự chấm trước'));

    const moi = datCaiDat.mock.calls[0][0] as CaiDatVai;
    expect(moi.thoId).toBe(MAY_THO.thoId);
    // Id ấy là id thật của một nhóm cũ, đừng đánh dấu là tự đặt.
    expect(moi.thoIdTuTao).toBe(false);
  });
});

describe('đã đăng nhập mà chưa vào nhóm', () => {
  const DA_DANG_NHAP = { taiKhoan: { userId: 'u1', email: 'chu@cuahang.vn', anDanh: false } };

  it('máy chủ: cho thử lập nhóm lại, không bắt đăng nhập lại', () => {
    const { nhom } = dung(nhomGia(DA_DANG_NHAP));

    expect(screen.getByText('Đã đăng nhập, chưa vào nhóm')).toBeTruthy();
    expect(screen.getByText('chu@cuahang.vn')).toBeTruthy();
    expect(screen.queryByText('Tôi là chủ')).toBeNull();

    fireEvent.press(screen.getByText('Lập nhóm, thử lại'));

    expect(nhom.lapNhom).toHaveBeenCalled();
  });

  it('máy thợ: chỉ sang đường dán mã, vì tự vào nhóm không được', () => {
    dung(nhomGia({ taiKhoan: { userId: 'u2', email: null, anDanh: true } }), MAY_THO);

    expect(screen.getByText('Tài khoản ẩn danh của máy này')).toBeTruthy();
    expect(screen.queryByText('Lập nhóm, thử lại')).toBeNull();

    fireEvent.press(screen.getByText('Dán mã mời của chủ'));

    expect(screen.getByText('Mã mời của chủ')).toBeTruthy();
  });

  it('hiện câu lỗi của lượt nối vừa hụt', () => {
    const cau = 'Không nối được mạng. Kiểm tra 3G hay wifi rồi thử lại.';
    dung(nhomGia({ ...DA_DANG_NHAP, loi: cau }));

    expect(screen.getByText(cau)).toBeTruthy();
  });
});

/**
 * Máy thợ bị ngắt khỏi nhóm: phiên đăng nhập mất nhưng vai máy vẫn là thợ. Bấm *Tôi là chủ*
 * ở đây mà nhảy thẳng vào `HopNoiNhom` thì gặp một đoạn chữ chỉ đường sang chỗ dán mã, không
 * có ô nào để gõ — mắc cạn giữa màn hình mở đầu.
 */
it('máy thợ đã ngắt: đường đăng nhập của chủ mở hộp chọn vai máy', () => {
  dung(nhomGia(), MAY_THO);

  fireEvent.press(screen.getByText('Tôi là chủ'));
  fireEvent.press(screen.getByText('Đăng nhập bằng email'));

  // Đầu trang bước hai cũng ghi "Máy của chủ", nên soi cái nhãn riêng của hộp chọn vai.
  expect(screen.getByText('Máy này là của ai')).toBeTruthy();
  expect(screen.getByText('Máy của thợ · dán lại mã mời')).toBeTruthy();
});
