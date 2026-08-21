/**
 * Màn hình riêng của máy thợ.
 *
 * Hai điều phải giữ: bấm một lần là chấm được hôm nay, và **trên màn hình này không có
 * một con số tiền nào** — kể cả khi sổ trong máy còn sót mốc lương từ lúc máy này từng là
 * máy chủ.
 */

import { act, fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { chiaSeFileMau, chiaSeSoCong } from '../../nghiepvu/chiaSeExcel';

import { SoDaNhan } from '../../nghiepvu/hopThu';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { SoCong, catSo } from '../../nghiepvu/soCong';
import { cham, dangCham, themTho } from '../../nghiepvu/thaoTac';
import { CaiDatVai } from '../../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from '../dungDoiChieu';
import { DieuKhienNhom } from '../dungSupabase';
import { ManHinhThoTuCham } from '../ManHinhThoTuCham';

// Máy chạy kiểm thử không có bảng chia sẻ của điện thoại, cũng không có bảng chọn file.
jest.mock('../../nghiepvu/chiaSeExcel', () => ({
  chiaSeSoCong: jest.fn(() => Promise.resolve('file:///tam/So-cong.xlsx')),
  chiaSeFileMau: jest.fn(() => Promise.resolve('file:///tam/Mau.xlsx')),
}));
jest.mock('../../nghiepvu/chonFileExcel', () => ({
  ...jest.requireActual('../../nghiepvu/chonFileExcel'),
  chonFileExcel: jest.fn(() => Promise.resolve(null)),
}));

const gioChiaSe = chiaSeSoCong as jest.MockedFunction<typeof chiaSeSoCong>;
const guiFileMau = chiaSeFileMau as jest.MockedFunction<typeof chiaSeFileMau>;

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

/** Nhóm Supabase giả: máy đã điền cấu hình nhưng chưa nối. */
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

function dung(
  duLieu: DuLieuChamCong,
  caiDat: CaiDatVai,
  dieuKhien = dieuKhienGia(),
  capNhat = jest.fn(),
  nhom = nhomGia(),
) {
  render(
    <ManHinhThoTuCham
      duLieu={duLieu}
      capNhat={capNhat}
      caiDat={caiDat}
      datCaiDat={jest.fn()}
      dieuKhien={dieuKhien}
      nhom={nhom}
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

/**
 * Sửa số công **giống hệt bên máy chủ**: ba mức có sẵn cộng một đường gõ số bất kỳ. Hai bên
 * gõ ra hai kiểu số thì đối chiếu báo lệch mà chẳng ai sai — thợ đi một phần tư buổi là
 * chuyện thật, mà máy thợ chỉ cho chọn nửa hay cả thì thợ chấm sai rồi chờ chủ sửa hộ.
 */
describe('sửa số công một buổi', () => {
  test('bấm giữ ra mấy mức có sẵn, chọn nửa công là ghi 0,5', () => {
    const { duLieu, thoId, caiDat } = kho();
    const capNhat = dung(duLieu, caiDat);

    fireEvent(screen.getAllByText('Sáng')[0], 'longPress');
    fireEvent.press(screen.getByText('Nửa công (0,5)'));

    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(dangCham(moi, thoId, HOM_NAY, 'Sang')?.soCong).toBe(0.5);
  });

  test('gõ được số lẻ như bên chủ: 0,25 công', () => {
    const { duLieu, thoId, caiDat } = kho();
    const capNhat = dung(duLieu, caiDat);

    fireEvent(screen.getAllByText('Sáng')[0], 'longPress');
    fireEvent.press(screen.getByText('Gõ số công khác'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 0,75'), '0,25');
    fireEvent.press(screen.getByText('Ghi'));

    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(dangCham(moi, thoId, HOM_NAY, 'Sang')?.soCong).toBe(0.25);
  });

  test('gõ số lớn quá thì chặn lại, cùng mức chặn với máy chủ', () => {
    const { duLieu, caiDat } = kho();
    const capNhat = dung(duLieu, caiDat);

    fireEvent(screen.getAllByText('Sáng')[0], 'longPress');
    fireEvent.press(screen.getByText('Gõ số công khác'));
    // Gõ "10" thay vì "1,0" là lỗi hay gặp.
    fireEvent.changeText(screen.getByLabelText('Ví dụ 0,75'), '10');

    expect(screen.getByText('Nhiều nhất 5 công một buổi.')).toBeTruthy();
    fireEvent.press(screen.getByText('Ghi'));
    expect(capNhat).not.toHaveBeenCalled();
  });

  test('buổi đã chấm thì hộp gõ số điền sẵn số đang có', () => {
    const { duLieu, thoId, caiDat } = kho();
    dung(cham(duLieu, thoId, HOM_NAY, 'Sang', 1.5), caiDat);

    // Tìm theo nhãn trợ năng chứ không theo chữ "Sáng": ô đã chấm 1,5 công thì chữ trên ô
    // là "Sáng  1,5", còn "Sáng" trơ trọi lại là mấy ô của những ngày trước.
    fireEvent(screen.getByLabelText('Sáng có đi làm'), 'longPress');
    fireEvent.press(screen.getByText('Gõ số công khác'));

    expect(screen.getByDisplayValue('1,5')).toBeTruthy();
  });
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

/**
 * Chưa nối nhóm là lúc app **chưa làm được việc gì cả**: thợ chấm mà sổ nằm im trong máy.
 * Máy thợ không có mục Thợ như máy chủ, nên đường vào phải nằm ngay trên màn hình này.
 */
describe('đường vào nhóm chấm công', () => {
  test('chưa vào nhóm thì có dải màu ngay trên đầu, bấm là mở hộp nối nhóm', () => {
    const { duLieu, caiDat } = kho();
    dung(duLieu, caiDat);

    expect(screen.getByText('Chưa nối nhóm')).toBeTruthy();
    fireEvent.press(screen.getByText('Chưa nối nhóm'));

    // Dẫn thẳng tới ô dán mã, không dừng ở câu hỏi "máy này là của ai".
    expect(screen.getByText('Mã mời của chủ')).toBeTruthy();
  });

  test('đăng nhập rồi mà chưa vào nhóm thì nói đúng chỗ đang mắc', () => {
    const { duLieu, caiDat } = kho();
    dung(duLieu, caiDat, dieuKhienGia(), jest.fn(), nhomGia({ taiKhoan: { userId: 'u1', email: null, anDanh: true } }));

    expect(screen.getByText('Chưa vào nhóm')).toBeTruthy();
  });

  test('vào nhóm rồi thì dải ấy biến đi, khỏi chiếm chỗ', () => {
    const { duLieu, thoId, caiDat } = kho();
    dung(
      duLieu,
      caiDat,
      dieuKhienGia(),
      jest.fn(),
      nhomGia({
        taiKhoan: { userId: 'u1', email: null, anDanh: true },
        thanhVien: { nhomId: 'n1', vai: 'tho', thoId },
      }),
    );

    expect(screen.queryByText('Chưa nối nhóm')).toBeNull();
    expect(screen.queryByText('Chưa vào nhóm')).toBeNull();
    expect(screen.getByText('Đã nối nhóm · thoát')).toBeTruthy();
  });

  /**
   * Thợ có quyền đi khỏi nhóm — đổi điện thoại, thôi làm chỗ này. Nút ấy vốn có nhưng nằm
   * kín trong hộp, mà trên màn hình không có chữ nào cho thấy là có đường ra.
   */
  test('vào nhóm rồi thì có đường thoát nhóm, kèm lời nhắc phải xin mã mời mới', () => {
    const { duLieu, thoId, caiDat } = kho();
    dung(
      duLieu,
      caiDat,
      dieuKhienGia(),
      jest.fn(),
      nhomGia({
        taiKhoan: { userId: 'u1', email: null, anDanh: true },
        thanhVien: { nhomId: 'n1', vai: 'tho', thoId },
      }),
    );

    fireEvent.press(screen.getByText('Đã nối nhóm · thoát'));

    expect(screen.getByText('Thoát nhóm, đăng xuất máy này')).toBeTruthy();
    expect(screen.getByText(/xin chủ một mã mời mới/)).toBeTruthy();
    // Buổi đã chấm không mất theo — điều thợ lo nhất khi bấm một cái nút đỏ.
    expect(screen.getByText(/vẫn còn nguyên trong máy/)).toBeTruthy();
  });
});

describe('nhìn tổng quan', () => {
  test('hai ô tóm tắt cho biết tuần này và tháng này được mấy công', () => {
    const { duLieu, thoId, caiDat } = kho();
    dung(cham(duLieu, thoId, HOM_NAY, 'Sang'), caiDat);

    expect(screen.getByText('Công tuần này')).toBeTruthy();
    expect(screen.getByText('Công tháng này')).toBeTruthy();
    // Chấm đúng một buổi hôm nay: cả tuần này lẫn tháng này đều là 1 công.
    expect(screen.getAllByText('1 công').length).toBeGreaterThanOrEqual(2);
  });

  test('mấy ngày trước gom theo tuần, mỗi tuần một thẻ có tổng riêng', () => {
    const { duLieu, caiDat } = kho();
    dung(duLieu, caiDat);

    // 13 ngày trước hôm nay lúc nào cũng vắt qua tuần trước, dù hôm nay là thứ mấy.
    expect(screen.getByText('Tuần trước')).toBeTruthy();
    expect(screen.getByText('Chấm bù mấy ngày trước')).toBeTruthy();
  });

  test('mỗi ô chấm đọc lên kèm ngày, không phải mỗi dòng một câu giống nhau', () => {
    const { duLieu, thoId, caiDat } = kho();
    const capNhat = dung(duLieu, caiDat);

    fireEvent.press(screen.getByLabelText(`${Ngay.thuVaNgay(HOM_QUA)} Sáng chưa chấm`));

    const moi = capNhat.mock.calls[0][0] as DuLieuChamCong;
    expect(dangCham(moi, thoId, HOM_QUA, 'Sang')?.soCong).toBe(1);
  });
});

test('mở được sổ của mình để xem chi tiết từng ngày', () => {
  const { duLieu, thoId, caiDat } = kho();
  dung(cham(duLieu, thoId, HOM_QUA, 'Sang'), caiDat);

  fireEvent.press(screen.getByText('Sổ công của tôi'));

  // Cùng tờ lịch và cùng cách chia nửa tháng như màn hình chi tiết bên máy chủ.
  expect(screen.getByText('Chi tiết từng ngày')).toBeTruthy();
  expect(screen.getByText('Lịch đi làm')).toBeTruthy();
  expect(screen.getByText('Nửa cuối')).toBeTruthy();
  // Vẫn không có đồng nào, dù sổ trong máy còn mốc lương 300.000.
  expect(screen.queryByText(/300\.000/)).toBeNull();
  expect(screen.queryByText(/đ$/)).toBeNull();
});

describe('xuất sổ của tôi ra Excel', () => {
  beforeEach(() => {
    gioChiaSe.mockReset().mockResolvedValue('file:///tam/So-cong.xlsx');
  });

  test('chưa chấm buổi nào thì chưa hiện nút, khỏi gửi đi một trang trống', () => {
    const { duLieu, caiDat } = kho();
    dung(duLieu, caiDat);

    expect(screen.queryByText('Xuất ra Excel')).toBeNull();
  });

  test('nói rõ file không có tiền, kèm icon theo điều 8', () => {
    const { duLieu, thoId, caiDat } = kho();
    dung(cham(duLieu, thoId, HOM_NAY, 'Sang'), caiDat);

    expect(screen.getByText('Xuất ra Excel')).toBeTruthy();
    expect(screen.getByText('icon:share')).toBeTruthy();
    expect(screen.getByText(/chỉ có số công, không có tiền/)).toBeTruthy();
  });

  /**
   * Điều quan trọng nhất của cái nút này: nó gửi đi `SoCong` — kiểu không có tiền — chứ
   * không phải cả sổ. Gọi bản của máy chủ là file mang đủ tiền công ra ngoài.
   */
  test('gửi đi đúng sổ của mình, cắt từ ngày nhận mã mời', async () => {
    const { duLieu, thoId, caiDat } = kho();
    dung(cham(duLieu, thoId, HOM_NAY, 'Sang'), caiDat);

    fireEvent.press(screen.getByText('Xuất ra Excel'));

    await waitFor(() => expect(gioChiaSe).toHaveBeenCalled());
    const so = gioChiaSe.mock.calls[0][0];
    expect(so).toEqual(
      expect.objectContaining({ thoId, nguon: 'tho', tuNgay: BAT_DAU, denNgay: HOM_NAY }),
    );
    expect(so.dongs).toEqual([{ ngay: HOM_NAY, buoi: 'Sang', soCong: 1 }]);
    // Không có một khoá nào mang tiền trong gói gửi đi.
    expect(JSON.stringify(so)).not.toMatch(/300000|tienMotCong|mocLuong/);
  });

  test('đang tạo file thì đổi chữ và bấm thêm cũng không làm lại', async () => {
    let xong: (uri: string) => void = () => {};
    gioChiaSe.mockReturnValue(
      new Promise((giaiQuyet) => {
        xong = giaiQuyet;
      }),
    );

    const { duLieu, thoId, caiDat } = kho();
    dung(cham(duLieu, thoId, HOM_NAY, 'Sang'), caiDat);
    fireEvent.press(screen.getByText('Xuất ra Excel'));

    const dangLam = await screen.findByText('Đang tạo file…');
    fireEvent.press(dangLam);
    expect(gioChiaSe).toHaveBeenCalledTimes(1);

    await act(async () => {
      xong('file:///tam/So-cong.xlsx');
    });
    expect(screen.getByText('Xuất ra Excel')).toBeTruthy();
  });

  test('hỏng thì nói bằng tiếng người và cho bấm lại', async () => {
    gioChiaSe.mockRejectedValueOnce(new Error('hết chỗ trống'));

    const { duLieu, thoId, caiDat } = kho();
    dung(cham(duLieu, thoId, HOM_NAY, 'Sang'), caiDat);
    fireEvent.press(screen.getByText('Xuất ra Excel'));

    expect(await screen.findByText('Chưa gửi được file. Bấm nút trên để làm lại.')).toBeTruthy();

    fireEvent.press(screen.getByText('Xuất ra Excel'));
    await waitFor(() => expect(gioChiaSe).toHaveBeenCalledTimes(2));
  });
});

/**
 * Nhập từ Excel cũng có trên máy thợ: thợ mới cài app giữa tháng thì cả tháng công cũ nằm
 * ngoài mười ba ngày mà danh sách chấm bù mời tới, không có ô nào mà bấm.
 */
describe('nhập từ Excel trên máy thợ', () => {
  test('có nút, và mở ra là màn hình nhập cho chính mình — không phải chọn thợ', () => {
    const { duLieu, caiDat } = kho();
    dung(duLieu, caiDat);

    fireEvent.press(screen.getByText('Nhập từ Excel'));

    expect(screen.getByText('Nhập công của tôi')).toBeTruthy();
    expect(screen.queryByText('1. Nhập cho thợ nào')).toBeNull();
  });

  test('file mẫu lấy ở đây không có cột tiền, đúng như mọi thứ khác trên máy thợ', async () => {
    const { duLieu, caiDat } = kho();
    dung(duLieu, caiDat);

    fireEvent.press(screen.getByText('Nhập từ Excel'));
    fireEvent.press(screen.getByLabelText('Lấy file mẫu cả năm'));

    await waitFor(() => expect(guiFileMau).toHaveBeenCalledTimes(1));
    expect(guiFileMau.mock.calls[0][3]).toBe(false);
  });
});
