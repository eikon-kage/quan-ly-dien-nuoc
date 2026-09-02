import { act, fireEvent, render, screen, within } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { baoCaoKhoang } from '../../nghiepvu/baoCao';
import { BuoiLam, DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { cham, datCong, themTho, themUng } from '../../nghiepvu/thaoTac';
import { CachSuaNgay } from '../HopSuaNgay';
import { ManHinhBaoCaoTho } from '../ManHinhBaoCaoTho';

const hoi = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

beforeEach(() => hoi.mockClear());

/**
 * Bấm hộ nút trong hộp thoại xác nhận của hệ điều hành. Bọc `act` vì nút ấy nằm ngoài
 * cây React — hộp là của máy — mà bấm vào lại đổi trạng thái màn hình.
 */
function bamNut(nhan: string) {
  const nut = (hoi.mock.calls[0][2] ?? []).find((n) => n.text === nhan);
  act(() => nut?.onPress?.());
}

const NGAY_TAO = '2026-08-01';
const CUOI_KY = '2026-08-05';

function khoCoTho(tienMotCong = 300_000) {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', tienMotCong, NGAY_TAO);
  return { duLieu, thoId: tho.id };
}

/**
 * Kỳ dùng trong bộ kiểm thử này trùng đúng tháng 8 — khoảng quen mắt nhất. Trùng khít một
 * tháng dương lịch thì màn hình gọi nó là *tháng* chứ không phải *kỳ*; kỳ lệch tháng có
 * bài riêng ở dưới.
 */
function dung(
  duLieu: DuLieuChamCong,
  thoId: string,
  homNay = CUOI_KY,
  suaUng?: { ghi: jest.Mock; xoa: jest.Mock },
  suaNgay?: CachSuaNgay,
) {
  return render(
    <ManHinhBaoCaoTho
      dungBaoCao={(tu, den) => baoCaoKhoang(duLieu, thoId, tu, den, homNay)}
      tuNgayDau="2026-08-01"
      denNgayDau="2026-08-31"
      suaUng={suaUng}
      suaNgay={suaNgay}
      onDong={() => {}}
    />,
  );
}

/** Mở tờ lịch của nút Từ hoặc Đến rồi chạm một ngày trong tháng 8. */
function chonNgay(nut: 'Từ' | 'Đến', ngay: string, thu: string) {
  fireEvent.press(screen.getByLabelText(new RegExp(`^${nut} ngày`)));
  fireEvent.press(screen.getByLabelText(`${ngay} ${thu}`));
}

describe('màn hình báo cáo một thợ', () => {
  test('hiện tên thợ và khoảng đang xem', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.getByText('Cả tháng · 01/08 → 31/08')).toBeTruthy();
  });

  test('tóm tắt số công, tiền công và còn phải trả', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');

    dung(duLieu, thoId, '2026-08-03');

    expect(screen.getByText('1 công')).toBeTruthy();
    expect(screen.getAllByText('300.000 đ').length).toBeGreaterThan(0);
  });

  test('vẽ tờ lịch cả tháng, ngày nào cũng có một ô', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByText('Lịch đi làm')).toBeTruthy();
    // Tháng 8/2026 có 31 ngày; cột thì bắt đầu từ Thứ Hai như lịch treo tường.
    expect(screen.getByText('31')).toBeTruthy();
    expect(screen.getByText('T2')).toBeTruthy();
    expect(screen.getByText('CN')).toBeTruthy();
  });

  test('ngày đi làm được tích, ngày nghỉ thì không', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');

    dung(duLieu, thoId, '2026-08-04');

    expect(screen.getByLabelText('03/08 Thứ Hai, đi làm 1 công')).toBeTruthy();
    expect(screen.getByLabelText('04/08 Thứ Ba, nghỉ')).toBeTruthy();
  });

  test('ngày chưa tới để trống chứ không tính là nghỉ', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');

    dung(duLieu, thoId, '2026-08-03');

    expect(screen.getByLabelText('20/08 Thứ Năm, chưa tính')).toBeTruthy();
  });

  test('đếm số ngày đi làm và số ngày nghỉ ngay dưới tờ lịch', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-01', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');

    dung(duLieu, thoId, '2026-08-04');

    expect(screen.getByText('Đi làm 2 ngày')).toBeTruthy();
    expect(screen.getByText('Nghỉ 2 ngày')).toBeTruthy();
  });

  test('ngày đi thiếu công thì ghi rõ mấy công lên ô', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = datCong(duLieu, thoId, '2026-08-03', 'Sang', 0.25);

    dung(duLieu, thoId, '2026-08-03');

    expect(screen.getByText('0,25')).toBeTruthy();
    expect(screen.getByLabelText('03/08 Thứ Hai, đi làm 0,25 công')).toBeTruthy();
  });

  test('ngày đi đủ cả ngày chỉ có dấu tích, không ghi số', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');

    dung(duLieu, thoId, '2026-08-03');

    // Trong ô ngày 3 chỉ có mỗi số 3 — không ghi thêm "1" công, vì đi đủ là chuyện
    // thường ngày, ghi vào thì cả tháng chi chít số.
    const o = screen.getByLabelText('03/08 Thứ Hai, đi làm 1 công');
    expect(within(o).getByText('3')).toBeTruthy();
    expect(within(o).queryByText('1')).toBeNull();
  });

  test('liệt kê từng lần ứng tiền kèm ngày và ghi chú', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000, 'ứng đổ xăng');

    dung(duLieu, thoId);

    expect(screen.getByText('Ứng tiền (1 lần)')).toBeTruthy();
    expect(screen.getByText('ứng đổ xăng')).toBeTruthy();

    // Hiện ba chỗ: "Đã ứng", "Còn phải trả" (chưa có công nên âm đúng bằng tiền ứng),
    // và dòng chi tiết. Cả ba phải dùng chung một ký tự dấu trừ — trước đây "Còn phải trả"
    // dùng dấu gạch nối nên đứng cạnh hai dòng kia nhìn lệch hẳn.
    expect(screen.getAllByText('−500.000 đ')).toHaveLength(3);
  });

  test('chưa ứng lần nào thì nói rõ', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByText('Tháng này chưa ứng lần nào.')).toBeTruthy();
  });

  test('kỳ chưa có công nào thì nói rõ chứ không để bảng trống', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByText('Tháng này chưa có ngày công nào.')).toBeTruthy();
  });

  test('lọc khoảng hẹp thì tóm tắt và tờ lịch chỉ tính trong khoảng', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-20', 'Sang');

    dung(duLieu, thoId, '2026-08-31');
    expect(screen.getByText('1 công')).toBeTruthy();

    chonNgay('Đến', '15/08', 'Thứ Bảy');

    expect(screen.getByText('0,5 công')).toBeTruthy();
    expect(screen.getByText('01/08 → 15/08')).toBeTruthy();
    // Ngày 20 rơi ra ngoài khoảng nên thành ô trắng, không tính là đi làm cũng không là nghỉ.
    expect(screen.getByLabelText('20/08 Thứ Năm, chưa tính')).toBeTruthy();
  });

  test('nút Nửa cuối nhảy thẳng sang kỳ 16 tới cuối tháng', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000, 'ứng đổ xăng');
    duLieu = themUng(duLieu, thoId, '2026-08-20', 200_000, 'ứng mua thuốc');

    dung(duLieu, thoId, '2026-08-31');
    expect(screen.getByText('Ứng tiền (2 lần)')).toBeTruthy();

    fireEvent.press(screen.getByText('Nửa cuối'));

    expect(screen.getByText('16/08 → 31/08')).toBeTruthy();
    expect(screen.getByText('Ứng tiền (1 lần)')).toBeTruthy();
    expect(screen.getByText('ứng mua thuốc')).toBeTruthy();
    expect(screen.queryByText('ứng đổ xăng')).toBeNull();
  });

  test('bấm viên đầu là bỏ lọc, quay về trọn khoảng mở ra lúc đầu', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId, '2026-08-31');

    fireEvent.press(screen.getByText('Nửa đầu'));
    expect(screen.getByText('01/08 → 15/08')).toBeTruthy();

    fireEvent.press(screen.getByText('Cả tháng'));
    expect(screen.getByText('Cả tháng · 01/08 → 31/08')).toBeTruthy();
  });

  /*
    Kỳ lương chốt lúc nào cũng được nên phần lớn kỳ *không* trùng tháng. Lúc ấy màn hình
    phải gọi đúng tên là "kỳ", và viên "Cả tháng" là một khoảng khác hẳn viên đầu nên có
    mặt riêng — trùng khít mới bỏ đi.
  */
  test('kỳ lệch tháng thì gọi là kỳ, và có thêm viên Cả tháng', () => {
    const { duLieu, thoId } = khoCoTho();
    render(
      <ManHinhBaoCaoTho
        dungBaoCao={(tu, den) => baoCaoKhoang(duLieu, thoId, tu, den, '2026-08-31')}
        tuNgayDau="2026-07-20"
        denNgayDau="2026-08-31"
        onDong={() => {}}
      />,
    );

    expect(screen.getByText('Cả kỳ · 20/07 → 31/08')).toBeTruthy();
    expect(screen.getByText('Kỳ này chưa có ngày công nào.')).toBeTruthy();
    expect(screen.getByText('Kỳ này chưa ứng lần nào.')).toBeTruthy();

    // Viên "Cả tháng" ở đây là tháng của ngày cuối kỳ, hẹp hơn cả kỳ.
    fireEvent.press(screen.getByText('Cả tháng'));
    expect(screen.getByText('01/08 → 31/08')).toBeTruthy();
  });

  test('chọn ngày đầu muộn hơn ngày cuối thì kéo luôn ngày cuối theo', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId, '2026-08-31');

    fireEvent.press(screen.getByText('Nửa đầu'));
    chonNgay('Từ', '20/08', 'Thứ Năm');

    // Không khoá ngày lại cho bấm không ăn, mà kéo ngày cuối theo — không có ngõ cụt.
    expect(screen.getByText('20/08 → 20/08')).toBeTruthy();
  });

  test('khoảng chưa có công, chưa ứng thì nói rõ là của khoảng chứ không phải cả tháng', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000);

    dung(duLieu, thoId, '2026-08-31');
    fireEvent.press(screen.getByText('Nửa cuối'));

    expect(screen.getByText('Khoảng này chưa có ngày công nào.')).toBeTruthy();
    expect(screen.getByText('Khoảng này chưa ứng lần nào.')).toBeTruthy();
  });

  test('không cho sửa ứng thì dòng ứng chỉ để đọc', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000, 'ứng đổ xăng');

    dung(duLieu, thoId);

    // Kỳ đã chốt mở qua đây: không mách chạm được, mà chạm cũng không ra hộp nào.
    expect(screen.queryByText('Chạm vào một dòng để sửa hoặc xoá.')).toBeNull();
    expect(screen.queryByLabelText(/chạm để sửa/)).toBeNull();
  });

  test('ứng quá tiền công thì còn phải trả là số âm', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000);

    dung(duLieu, thoId);

    expect(screen.getByText('−350.000 đ')).toBeTruthy();
  });
});

describe('sửa lịch sử ứng tiền', () => {
  function moHopSua() {
    let { duLieu, thoId } = khoCoTho();
    duLieu = themUng(duLieu, thoId, '2026-08-05', 5_000_000, 'ứng đổ xăng');
    const suaUng = { ghi: jest.fn(), xoa: jest.fn() };
    const ungId = duLieu.ungTiens[0].id;

    dung(duLieu, thoId, CUOI_KY, suaUng);
    fireEvent.press(screen.getByLabelText(/chạm để sửa/));

    return { suaUng, ungId };
  }

  test('chạm một dòng ứng là mở hộp sửa, điền sẵn số cũ', () => {
    moHopSua();

    expect(screen.getByText('Anh Tuấn — sửa lần ứng')).toBeTruthy();
    expect(screen.getByLabelText('Số tiền ứng').props.value).toBe('5000000');
    expect(screen.getByLabelText('Ứng ngày 05/08/2026, chạm để đổi')).toBeTruthy();
  });

  test('sửa số tiền gõ nhầm rồi bấm Ghi', () => {
    const { suaUng, ungId } = moHopSua();

    fireEvent.changeText(screen.getByLabelText('Số tiền ứng'), '500000');
    fireEvent.press(screen.getByText('Ghi'));

    expect(suaUng.ghi).toHaveBeenCalledWith(ungId, '2026-08-05', 500_000, 'ứng đổ xăng');
  });

  test('sửa cả ngày ứng — ghi muộn mấy hôm nên ngày bị lệch', () => {
    const { suaUng, ungId } = moHopSua();

    // Tờ lịch thay chỗ hộp sửa, chọn xong thì hộp quay lại, số tiền đang gõ vẫn còn.
    fireEvent.press(screen.getByLabelText('Ứng ngày 05/08/2026, chạm để đổi'));
    fireEvent.press(screen.getByLabelText('03/08 Thứ Hai'));
    fireEvent.press(screen.getByText('Ghi'));

    expect(suaUng.ghi).toHaveBeenCalledWith(ungId, '2026-08-03', 5_000_000, 'ứng đổ xăng');
  });

  test('lùi được sang tháng trước — ứng cuối tháng mà mấy hôm sau mới ghi', () => {
    const { suaUng, ungId } = moHopSua();

    fireEvent.press(screen.getByLabelText('Ứng ngày 05/08/2026, chạm để đổi'));
    fireEvent.press(screen.getByLabelText('Tháng trước'));
    expect(screen.getByText('Tháng 7/2026')).toBeTruthy();

    fireEvent.press(screen.getByLabelText('31/07 Thứ Sáu'));
    fireEvent.press(screen.getByText('Ghi'));

    expect(suaUng.ghi).toHaveBeenCalledWith(ungId, '2026-07-31', 5_000_000, 'ứng đổ xăng');
  });

  test('tới được tháng sau rồi lùi lại thì về đúng chỗ cũ', () => {
    moHopSua();

    fireEvent.press(screen.getByLabelText('Ứng ngày 05/08/2026, chạm để đổi'));
    fireEvent.press(screen.getByLabelText('Tháng sau'));
    expect(screen.getByText('Tháng 9/2026')).toBeTruthy();

    fireEvent.press(screen.getByLabelText('Tháng trước'));
    expect(screen.getByText('Tháng 8/2026')).toBeTruthy();
  });

  test('số tiền để trống thì nút Ghi không ăn', () => {
    const { suaUng } = moHopSua();

    fireEvent.changeText(screen.getByLabelText('Số tiền ứng'), '');
    fireEvent.press(screen.getByText('Ghi'));

    expect(suaUng.ghi).not.toHaveBeenCalled();
  });

  test('xoá phải hỏi lại một câu rồi mới xoá', () => {
    const { suaUng, ungId } = moHopSua();

    fireEvent.press(screen.getByText('Xoá lần ứng này'));
    expect(suaUng.xoa).not.toHaveBeenCalled();

    bamNut('Xoá');
    expect(suaUng.xoa).toHaveBeenCalledWith(ungId);
  });

  test('hỏi lại mà bấm Thôi thì không xoá', () => {
    const { suaUng } = moHopSua();

    fireEvent.press(screen.getByText('Xoá lần ứng này'));
    bamNut('Thôi');

    expect(suaUng.xoa).not.toHaveBeenCalled();
  });
});

/**
 * Chạm thẳng vào một ô ngày trên tờ lịch để chấm hay chữa lại ngày ấy — đường ngắn nhất
 * từ chỗ *nhìn ra chỗ sai* tới chỗ sửa. Trước đây phải thoát ra, sang mục Chấm công rồi
 * lần lại đúng ngày.
 */
describe('sửa thẳng một ngày trên tờ lịch', () => {
  /** Sổ giả cho hộp sửa: đọc từ một Map, ghi thì nhớ lại lời gọi. */
  function cachSua(
    daCham: Record<string, number> = {},
    khoa: string[] = [],
  ): CachSuaNgay & { datCong: jest.Mock; ghi: jest.Mock } {
    const datCongGia = jest.fn();
    const ghiGia = jest.fn();
    return {
      cong: (ngay: string, buoi: BuoiLam) => daCham[`${ngay} ${buoi}`] ?? null,
      khoa: (ngay: string, buoi: BuoiLam) => khoa.includes(`${ngay} ${buoi}`),
      datCong: datCongGia,
      ghiChu: { doc: () => '', ghi: ghiGia },
      ghi: ghiGia,
    };
  }

  /** Mở hộp sửa của ngày 03/08 ra. */
  function moNgay(sua: CachSuaNgay) {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId, '2026-08-31', undefined, sua);
    fireEvent.press(screen.getByLabelText('03/08 Thứ Hai, nghỉ. Chạm để sửa.'));
  }

  test('không truyền đường sửa thì ô ngày chỉ để đọc', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByLabelText('03/08 Thứ Hai, nghỉ')).toBeTruthy();
    expect(screen.queryByText('Chạm vào một ngày để chấm hoặc sửa ngày ấy.')).toBeNull();
  });

  test('chạm một ô ngày là mở hộp của đúng ngày ấy, kèm tên thợ', () => {
    moNgay(cachSua());

    expect(screen.getByText('Anh Tuấn — Thứ Hai 03/08')).toBeTruthy();
    expect(screen.getByText('Ngày này chưa chấm công nào')).toBeTruthy();
    expect(screen.getByLabelText('Sáng chưa chấm, chạm để đổi')).toBeTruthy();
    expect(screen.getByLabelText('Chiều chưa chấm, chạm để đổi')).toBeTruthy();
  });

  test('chạm ô Sáng đang trống là chấm nửa công cho buổi ấy', () => {
    const sua = cachSua();
    moNgay(sua);

    fireEvent.press(screen.getByLabelText('Sáng chưa chấm, chạm để đổi'));

    // Một buổi đi đủ là nửa công — cả ngày mới là một công.
    expect(sua.datCong).toHaveBeenCalledWith('2026-08-03', 'Sang', 0.5);
  });

  test('chạm ô đang xanh là bỏ chấm, đúng thao tác vừa rồi', () => {
    const sua = cachSua({ '2026-08-03 Sang': 0.5 });
    moNgay(sua);

    fireEvent.press(screen.getByLabelText('Sáng có đi làm, chạm để đổi'));

    expect(sua.datCong).toHaveBeenCalledWith('2026-08-03', 'Sang', null);
  });

  test('nút Sửa mở đường tới nửa buổi', () => {
    const sua = cachSua();
    moNgay(sua);

    fireEvent.press(screen.getByText('Sửa'));
    fireEvent.press(screen.getByText('Buổi chiều'));
    fireEvent.press(screen.getByText('Nửa buổi (0,25 công)'));

    expect(sua.datCong).toHaveBeenCalledWith('2026-08-03', 'Chieu', 0.25);
  });

  test('gõ được số công khác, chặn ở mức tối đa một buổi', () => {
    const sua = cachSua();
    moNgay(sua);

    fireEvent.press(screen.getByText('Sửa'));
    fireEvent.press(screen.getByText('Buổi sáng'));
    fireEvent.press(screen.getByText('Gõ số công khác'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 0,5'), '0,75');
    fireEvent.press(screen.getByText('Ghi'));

    expect(sua.datCong).toHaveBeenCalledWith('2026-08-03', 'Sang', 0.75);
  });

  test('ghi chú cho cả ngày cũng sửa được từ đây', () => {
    const sua = cachSua();
    moNgay(sua);

    fireEvent.press(screen.getByText('Sửa'));
    fireEvent.press(screen.getByText('Ghi chú cho ngày này'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ: về sớm đi đám cưới'), 'về sớm');
    fireEvent.press(screen.getByText('Ghi'));

    expect(sua.ghi).toHaveBeenCalledWith('2026-08-03', 'về sớm');
  });

  test('buổi đã nằm trong kỳ đã chốt thì khoá lại, bấm không được', () => {
    const sua = cachSua({ '2026-08-03 Sang': 0.5 }, ['2026-08-03 Sang']);
    moNgay(sua);

    expect(
      screen.getByLabelText('Sáng có đi làm, đã chốt kỳ nên không sửa được'),
    ).toBeTruthy();
    expect(screen.getByText(/Buổi có ổ khoá/)).toBeTruthy();

    // Không có cả trong danh sách của nút Sửa: mở ra rồi mới biết không sửa được là bắt
    // người dùng đi hai bước để nhận một câu từ chối.
    fireEvent.press(screen.getByText('Sửa'));
    expect(screen.queryByText('Buổi sáng')).toBeNull();
    expect(screen.getByText('Buổi chiều')).toBeTruthy();
  });
});
