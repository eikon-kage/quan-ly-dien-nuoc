import { act, fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { chiaSeExcel } from '../../nghiepvu/chiaSeExcel';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { cham, luuTho, themTho } from '../../nghiepvu/thaoTac';
import { MAC_DINH } from '../../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from '../dungDoiChieu';
import { DieuKhienNhom } from '../dungSupabase';
import { DieuKhienSaoLuu, TrangThaiSaoLuu } from '../dungSaoLuu';
import { ManHinhTho } from '../ManHinhTho';

// Máy chạy kiểm thử không có bảng chia sẻ của điện thoại.
jest.mock('../../nghiepvu/chiaSeExcel', () => ({
  chiaSeExcel: jest.fn(() => Promise.resolve('file:///tam/Cham-cong.xlsx')),
}));

/** Trạng thái sao lưu giả. Mặc định: máy nối được nhưng người dùng chưa nối Drive. */
function saoLuuGia(sua: Partial<TrangThaiSaoLuu> = {}): DieuKhienSaoLuu {
  return {
    trangThai: {
      hoTro: true,
      taiKhoan: null,
      dangChay: false,
      lucCuoi: null,
      loi: null,
      ...sua,
    },
    noiDrive: jest.fn(() => Promise.resolve()),
    ngatDrive: jest.fn(() => Promise.resolve()),
    saoLuuNgay: jest.fn(() => Promise.resolve()),
  };
}

/** Hộp thư giả: chưa thợ nào gửi sổ lên. */
function doiChieuGia(sua: Partial<DieuKhienDoiChieu> = {}): DieuKhienDoiChieu {
  return {
    trangThai: { hoTro: true, daNoi: false, dangChay: false, lucCuoi: null, loi: null },
    soBenKia: new Map(),
    dongBo: jest.fn(() => Promise.resolve()),
    noiGoogle: jest.fn(() => Promise.resolve()),
    ...sua,
  };
}

/** Nhóm Supabase giả: máy đã điền cấu hình nhưng chưa nối. */
function nhomGia(sua: Partial<DieuKhienNhom['trangThai']> = {}): DieuKhienNhom {
  return {
    trangThai: {
      hoTro: true,
      taiKhoan: null,
      thanhVien: null,
      dangChay: false,
      loi: null,
      nhac: null,
      ...sua,
    },
    noiAnDanh: jest.fn(() => Promise.resolve()),
    noiEmail: jest.fn(() => Promise.resolve()),
    taoTaiKhoan: jest.fn(() => Promise.resolve()),
    ngat: jest.fn(() => Promise.resolve()),
  };
}

const chiaSe = chiaSeExcel as jest.MockedFunction<typeof chiaSeExcel>;
const HOM_NAY = Ngay.homNay();

function khoCoCong(): DuLieuChamCong {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY);
  return cham(them.duLieu, them.tho.id, HOM_NAY, 'Sang');
}

function dung(duLieu: DuLieuChamCong, saoLuu: DieuKhienSaoLuu = saoLuuGia()) {
  return render(
    <ManHinhTho
      duLieu={duLieu}
      capNhat={jest.fn()}
      saoLuu={saoLuu}
      caiDat={MAC_DINH}
      datCaiDat={jest.fn()}
      dieuKhien={doiChieuGia()}
      nhom={nhomGia()}
    />,
  );
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

    expect(screen.getByText('Xuất ra Excel')).toBeTruthy();
    // Điều 8 của tài liệu giao diện: icon luôn đi kèm chữ.
    expect(screen.getByText('icon:share')).toBeTruthy();
    expect(
      screen.getByText(
        'Nhập: điền công cả tháng trên máy tính rồi đưa vào app. Xuất: gửi qua Zalo hoặc mail để mở bằng Excel.',
      ),
    ).toBeTruthy();
  });

  test('chưa có gì thì chưa hiện nút, khỏi xuất ra file rỗng', () => {
    dung(duLieuRong());

    expect(screen.queryByText('Xuất ra Excel')).toBeNull();
  });

  test('bấm là dựng file từ đúng dữ liệu đang có', async () => {
    const duLieu = khoCoCong();
    dung(duLieu);

    fireEvent.press(screen.getByText('Xuất ra Excel'));

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
    fireEvent.press(screen.getByText('Xuất ra Excel'));

    const dangLam = await screen.findByText('Đang tạo file…');
    fireEvent.press(dangLam);
    expect(chiaSe).toHaveBeenCalledTimes(1);

    await act(async () => {
      xong('file:///tam/Cham-cong.xlsx');
    });
    expect(screen.getByText('Xuất ra Excel')).toBeTruthy();
  });

  test('hỏng thì nói bằng tiếng người và cho bấm lại', async () => {
    chiaSe.mockRejectedValueOnce(new Error('hết chỗ trống'));

    dung(khoCoCong());
    fireEvent.press(screen.getByText('Xuất ra Excel'));

    expect(await screen.findByText('Chưa gửi được file. Bấm nút trên để làm lại.')).toBeTruthy();

    // Bấm lại lần nữa là chạy lại thật, không kẹt ở trạng thái lỗi.
    fireEvent.press(screen.getByText('Xuất ra Excel'));
    await waitFor(() => expect(chiaSe).toHaveBeenCalledTimes(2));
  });
});

describe('dòng sao lưu Drive ở màn hình Thợ', () => {
  test('chưa có dữ liệu thì chưa hiện — không có gì để sao lưu', () => {
    dung(duLieuRong());

    expect(screen.queryByText('Sao lưu Google Drive')).toBeNull();
  });

  test('chưa nối thì nói thẳng dữ liệu đang chỉ nằm trong máy', () => {
    dung(khoCoCong(), saoLuuGia({ taiKhoan: null }));

    expect(screen.getByText('Chưa nối — dữ liệu chỉ nằm trong máy này')).toBeTruthy();
    expect(screen.getByText('icon:cloud-off')).toBeTruthy();
  });

  test('đã sao lưu thì hiện giờ của lần cuối', () => {
    const luc = new Date(2026, 7, 5, 16, 12).toISOString();
    dung(khoCoCong(), saoLuuGia({ taiKhoan: { email: 'anh@gmail.com' }, lucCuoi: luc }));

    expect(screen.getByText('Đã sao lưu lúc 05/08, 16:12')).toBeTruthy();
    expect(screen.getByText('icon:cloud')).toBeTruthy();
  });

  test('đang đẩy lên thì nói đang chạy', () => {
    dung(khoCoCong(), saoLuuGia({ taiKhoan: { email: 'anh@gmail.com' }, dangChay: true }));

    expect(screen.getByText('Đang đẩy lên…')).toBeTruthy();
  });

  /** Lỗi phải đè lên cả giờ sao lưu cũ: giờ cũ mà đứng một mình thì nhìn như vẫn đang êm. */
  test('có lỗi thì hiện lỗi chứ không hiện giờ sao lưu cũ', () => {
    dung(
      khoCoCong(),
      saoLuuGia({
        taiKhoan: { email: 'anh@gmail.com' },
        lucCuoi: new Date(2026, 7, 5, 16, 12).toISOString(),
        loi: 'Chưa đẩy lên Drive được. Sẽ tự thử lại sau.',
      }),
    );

    expect(screen.getByText('Chưa đẩy lên Drive được. Sẽ tự thử lại sau.')).toBeTruthy();
    expect(screen.queryByText('Đã sao lưu lúc 05/08, 16:12')).toBeNull();
  });

  test('máy không nối Drive được thì nói rõ vì sao', () => {
    dung(khoCoCong(), saoLuuGia({ hoTro: false }));

    expect(screen.getByText('Cần bản app cài thẳng vào máy')).toBeTruthy();
  });

  test('bấm vào là mở màn hình sao lưu', () => {
    dung(khoCoCong());
    fireEvent.press(screen.getByText('Sao lưu Google Drive'));

    expect(screen.getByText('Nối với Google Drive')).toBeTruthy();
  });
});
