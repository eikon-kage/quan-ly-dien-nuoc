import { act, fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { chiaSeExcel } from '../../nghiepvu/chiaSeExcel';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { cham, luuTho, themTho } from '../../nghiepvu/thaoTac';
import { ManHinhTho } from '../ManHinhTho';

// Máy chạy kiểm thử không có bảng chia sẻ của điện thoại.
jest.mock('../../nghiepvu/chiaSeExcel', () => ({
  chiaSeExcel: jest.fn(() => Promise.resolve('file:///tam/Cham-cong.xlsx')),
}));

const chiaSe = chiaSeExcel as jest.MockedFunction<typeof chiaSeExcel>;
const HOM_NAY = Ngay.homNay();

function khoCoCong(): DuLieuChamCong {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY);
  return cham(them.duLieu, them.tho.id, HOM_NAY, 'Sang');
}

function dung(duLieu: DuLieuChamCong) {
  return render(<ManHinhTho duLieu={duLieu} capNhat={jest.fn()} />);
}

beforeEach(() => {
  chiaSe.mockClear();
  chiaSe.mockResolvedValue('file:///tam/Cham-cong.xlsx');
});

describe('đầu trang màn hình Thợ', () => {
  test('nút Thêm thợ nằm trên đầu trang, kèm icon theo điều 8', () => {
    dung(duLieuRong());

    expect(screen.getByText('Thợ')).toBeTruthy();
    expect(screen.getByText('Thêm thợ')).toBeTruthy();
    expect(screen.getByText('icon:plus')).toBeTruthy();
  });

  test('bấm là mở hộp thêm thợ', () => {
    dung(duLieuRong());

    fireEvent.press(screen.getByText('Thêm thợ'));

    // Hộp thoại cũng mang tên "Thêm thợ" nên nhận ra nó bằng ô nhập bên trong.
    expect(screen.getByText('Tên thợ')).toBeTruthy();
    expect(screen.getByPlaceholderText('Ví dụ: Anh Tuấn')).toBeTruthy();
  });

  test('đếm số thợ ngay dưới tiêu đề', () => {
    const { duLieu } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY);
    const them = themTho(duLieu, 'Anh Bình', 300_000, HOM_NAY);
    dung(them.duLieu);

    expect(screen.getByText('2 đang làm')).toBeTruthy();
  });

  test('thợ đã nghỉ đếm riêng chứ không cộng chung', () => {
    const mot = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY);
    const hai = themTho(mot.duLieu, 'Anh Bình', 300_000, HOM_NAY);
    dung(luuTho(hai.duLieu, { ...hai.tho, dangLam: false }));

    expect(screen.getByText('1 đang làm · 1 đã nghỉ')).toBeTruthy();
  });

  test('chưa có thợ nào thì nói rõ chứ không để trống chỗ đếm', () => {
    dung(duLieuRong());

    expect(screen.getByText('Chưa có ai')).toBeTruthy();
  });
});

describe('xuất Excel ở màn hình Thợ', () => {
  test('có nút xuất kèm icon và câu chỉ dẫn', () => {
    dung(khoCoCong());

    expect(screen.getByText('Xuất toàn bộ ra Excel')).toBeTruthy();
    // Điều 8 của tài liệu giao diện: icon luôn đi kèm chữ.
    expect(screen.getByText('icon:share')).toBeTruthy();
    expect(
      screen.getByText('Gửi qua Zalo, gửi mail hoặc lưu vào máy tính để mở bằng Excel.'),
    ).toBeTruthy();
  });

  test('chưa có gì thì chưa hiện nút, khỏi xuất ra file rỗng', () => {
    dung(duLieuRong());

    expect(screen.queryByText('Xuất toàn bộ ra Excel')).toBeNull();
  });

  test('bấm là dựng file từ đúng dữ liệu đang có', async () => {
    const duLieu = khoCoCong();
    dung(duLieu);

    fireEvent.press(screen.getByText('Xuất toàn bộ ra Excel'));

    await waitFor(() => expect(chiaSe).toHaveBeenCalledWith(duLieu, HOM_NAY));
  });

  test('đang tạo file thì nút đổi chữ và bấm thêm cũng không làm lại', async () => {
    let xong: (uri: string) => void = () => {};
    chiaSe.mockReturnValue(
      new Promise((giaiQuyet) => {
        xong = giaiQuyet;
      }),
    );

    dung(khoCoCong());
    fireEvent.press(screen.getByText('Xuất toàn bộ ra Excel'));

    const dangLam = await screen.findByText('Đang tạo file…');
    fireEvent.press(dangLam);
    expect(chiaSe).toHaveBeenCalledTimes(1);

    await act(async () => {
      xong('file:///tam/Cham-cong.xlsx');
    });
    expect(screen.getByText('Xuất toàn bộ ra Excel')).toBeTruthy();
  });

  test('hỏng thì nói bằng tiếng người và cho bấm lại', async () => {
    chiaSe.mockRejectedValueOnce(new Error('hết chỗ trống'));

    dung(khoCoCong());
    fireEvent.press(screen.getByText('Xuất toàn bộ ra Excel'));

    expect(await screen.findByText('Chưa gửi được file. Bấm nút trên để làm lại.')).toBeTruthy();

    // Bấm lại lần nữa là chạy lại thật, không kẹt ở trạng thái lỗi.
    fireEvent.press(screen.getByText('Xuất toàn bộ ra Excel'));
    await waitFor(() => expect(chiaSe).toHaveBeenCalledTimes(2));
  });
});
