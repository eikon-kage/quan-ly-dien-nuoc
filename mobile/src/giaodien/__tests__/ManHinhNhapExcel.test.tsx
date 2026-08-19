import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { chiaSeFileMau } from '../../nghiepvu/chiaSeExcel';
import { chonFileExcel, KhongPhaiFileExcel } from '../../nghiepvu/chonFileExcel';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { TEN_TRANG_NHAP } from '../../nghiepvu/nhapExcel';
import { cham, themTho } from '../../nghiepvu/thaoTac';
import { O, taoFileExcel } from '../../nghiepvu/xlsx';
import { ManHinhNhapExcel } from '../ManHinhNhapExcel';

// Bảng chọn file và bảng chia sẻ đều là của điện thoại, máy chạy kiểm thử không có.
jest.mock('../../nghiepvu/chonFileExcel', () => ({
  ...jest.requireActual('../../nghiepvu/chonFileExcel'),
  chonFileExcel: jest.fn(),
}));
jest.mock('../../nghiepvu/chiaSeExcel', () => ({
  chiaSeExcel: jest.fn(() => Promise.resolve('file:///tam/Cham-cong.xlsx')),
  chiaSeFileMau: jest.fn(() => Promise.resolve('file:///tam/Mau.xlsx')),
}));

const chonFile = chonFileExcel as jest.MockedFunction<typeof chonFileExcel>;
const guiMau = chiaSeFileMau as jest.MockedFunction<typeof chiaSeFileMau>;

/** File .xlsx như người dùng vừa điền xong trên máy tính. */
function fileDaDien(dongs: O[][]): Uint8Array {
  return taoFileExcel([
    {
      ten: TEN_TRANG_NHAP,
      cots: ['Ngày', 'Thứ', 'Sáng', 'Chiều', 'Ứng tiền', 'Ghi chú'].map((nhan) => ({
        nhan,
        rong: 12,
        kieu: 'chu' as const,
      })),
      dongs,
    },
  ]);
}

function khoMotTho(): { duLieu: DuLieuChamCong; ten: string } {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  return { duLieu: them.duLieu, ten: them.tho.ten };
}

function khoHaiTho(): DuLieuChamCong {
  const mot = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  return themTho(mot.duLieu, 'Anh Bình', 280_000, '2026-08-01').duLieu;
}

function dung(duLieu: DuLieuChamCong, capNhat = jest.fn()) {
  render(<ManHinhNhapExcel duLieu={duLieu} capNhat={capNhat} onDong={jest.fn()} />);
  return capNhat;
}

beforeEach(() => {
  chonFile.mockReset();
  guiMau.mockClear();
  guiMau.mockResolvedValue('file:///tam/Mau.xlsx');
});

describe('chọn thợ', () => {
  test('chỉ có một thợ thì chọn sẵn, vào thẳng bước lấy file', () => {
    dung(khoMotTho().duLieu);

    expect(screen.getByText('Nhập công cho Anh Tuấn')).toBeTruthy();
    expect(screen.getByText('Chọn file Excel đã điền')).toBeTruthy();
  });

  test('nhiều thợ thì phải chọn trước, chưa chọn thì chưa có nút lấy file', () => {
    dung(khoHaiTho());

    expect(screen.getByText('Chọn thợ trước')).toBeTruthy();
    expect(screen.queryByText('Chọn file Excel đã điền')).toBeNull();

    fireEvent.press(screen.getByText('Anh Bình'));

    expect(screen.getByText('Nhập công cho Anh Bình')).toBeTruthy();
    expect(screen.getByText('Chọn file Excel đã điền')).toBeTruthy();
  });

  test('chưa có thợ nào thì chỉ đường về màn hình Thợ', () => {
    dung(duLieuRong());

    expect(screen.getByText('Chưa có thợ nào')).toBeTruthy();
    expect(screen.queryByText('Chọn file Excel đã điền')).toBeNull();
  });
});

describe('lấy file mẫu', () => {
  test('dựng file mẫu cho đúng thợ đang chọn, trọn tháng này', async () => {
    dung(khoMotTho().duLieu);

    fireEvent.press(screen.getByText('Lấy file mẫu tháng này'));

    await waitFor(() => expect(guiMau).toHaveBeenCalledTimes(1));
    const [tenTho, tuNgay, denNgay] = guiMau.mock.calls[0];
    expect(tenTho).toBe('Anh Tuấn');
    // Trọn một tháng: ngày đầu là mùng 1, ngày cuối cùng tháng cùng năm với ngày đầu.
    expect(tuNgay.endsWith('-01')).toBe(true);
    expect(denNgay.slice(0, 7)).toBe(tuNgay.slice(0, 7));
  });

  test('gửi hụt thì nói bằng tiếng người', async () => {
    guiMau.mockRejectedValueOnce(new Error('hết chỗ'));
    dung(khoMotTho().duLieu);

    fireEvent.press(screen.getByText('Lấy file mẫu tháng này'));

    expect(await screen.findByText('Chưa gửi được file mẫu. Anh thử lại xem.')).toBeTruthy();
  });
});

describe('đọc file rồi ghi vào sổ', () => {
  test('xem trước con số trước đã, chưa đụng vào dữ liệu', async () => {
    chonFile.mockResolvedValue({
      ten: 'cong-thang-8.xlsx',
      noiDung: fileDaDien([
        ['2026-08-03', '', 1, 1, null, ''],
        ['2026-08-04', '', 1, 0.5, 500000, ''],
      ]),
    });

    const capNhat = dung(khoMotTho().duLieu);
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));

    expect(await screen.findByText('Ghi vào sổ')).toBeTruthy();
    expect(screen.getByText('cong-thang-8.xlsx')).toBeTruthy();
    expect(screen.getByText('3,5')).toBeTruthy();
    expect(screen.getByText('500.000 đ')).toBeTruthy();
    // Xem trước là xem trước: chưa bấm Ghi thì sổ chưa đổi.
    expect(capNhat).not.toHaveBeenCalled();
  });

  test('bấm ghi thì cập nhật dữ liệu và kể lại đã làm gì', async () => {
    chonFile.mockResolvedValue({
      ten: 'cong.xlsx',
      noiDung: fileDaDien([['2026-08-03', '', 1, 1, null, '']]),
    });

    const capNhat = dung(khoMotTho().duLieu);
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));
    fireEvent.press(await screen.findByText('Ghi vào sổ'));

    expect(capNhat).toHaveBeenCalledTimes(1);
    expect(capNhat.mock.calls[0][0].buoiCongs).toHaveLength(2);
    expect(screen.getByText('Đã chấm mới 2 buổi.')).toBeTruthy();
  });

  test('dòng hỏng thì kể ra kèm số dòng, phần còn lại vẫn ghi được', async () => {
    chonFile.mockResolvedValue({
      ten: 'cong.xlsx',
      noiDung: fileDaDien([
        ['2026-08-03', '', 1, 1, null, ''],
        ['hôm kia', '', 1, null, null, ''],
      ]),
    });

    dung(khoMotTho().duLieu);
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));

    expect(await screen.findByText('1 dòng phải bỏ qua:')).toBeTruthy();
    expect(screen.getByText('Dòng 3: ngày "hôm kia" không đọc được')).toBeTruthy();
    expect(screen.getByText('Ghi vào sổ')).toBeTruthy();
  });

  test('báo trước số buổi sắp bị bỏ chấm — đây là chỗ mất dữ liệu', async () => {
    const { duLieu } = khoMotTho();
    const thoId = duLieu.thos[0].id;
    chonFile.mockResolvedValue({
      ten: 'cong.xlsx',
      noiDung: fileDaDien([['2026-08-03', '', 0, 0, null, '']]),
    });

    dung(cham(duLieu, thoId, '2026-08-03', 'Sang', 1));
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));

    expect(
      await screen.findByText(
        'Có 2 buổi file ghi là nghỉ — buổi ấy trong máy sẽ bị bỏ chấm.',
      ),
    ).toBeTruthy();
  });

  test('chọn nhầm file thì hiện đúng câu của lỗi ấy, không phải câu máy móc', async () => {
    chonFile.mockRejectedValueOnce(new KhongPhaiFileExcel('Anh chọn file Excel đuôi .xlsx nhé.'));

    dung(khoMotTho().duLieu);
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));

    expect(await screen.findByText('Anh chọn file Excel đuôi .xlsx nhé.')).toBeTruthy();
  });

  test('người dùng bấm huỷ ở bảng chọn file thì màn hình đứng yên, không báo lỗi', async () => {
    chonFile.mockResolvedValue(null);

    dung(khoMotTho().duLieu);
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));

    await waitFor(() => expect(chonFile).toHaveBeenCalled());
    expect(screen.queryByText('Ghi vào sổ')).toBeNull();
    expect(screen.getByText('Chọn file Excel đã điền')).toBeTruthy();
  });

  test('đổi thợ giữa chừng thì bỏ luôn file đang xem, khỏi ghi nhầm người', async () => {
    chonFile.mockResolvedValue({
      ten: 'cong.xlsx',
      noiDung: fileDaDien([['2026-08-03', '', 1, 1, null, '']]),
    });

    dung(khoHaiTho());
    fireEvent.press(screen.getByText('Anh Bình'));
    fireEvent.press(screen.getByText('Chọn file Excel đã điền'));
    expect(await screen.findByText('Ghi vào sổ')).toBeTruthy();

    fireEvent.press(screen.getByText('Đổi thợ'));
    fireEvent.press(screen.getByText('Anh Tuấn'));

    expect(screen.queryByText('Ghi vào sổ')).toBeNull();
  });
});
