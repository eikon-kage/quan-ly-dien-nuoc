import { fireEvent, render, screen, within } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { cham, datCong, themTho, themUng } from '../../nghiepvu/thaoTac';
import { ManHinhBaoCaoTho } from '../ManHinhBaoCaoTho';

const NGAY_TAO = '2026-08-01';
const CUOI_KY = '2026-08-05';

function khoCoTho(tienMotCong = 300_000) {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', tienMotCong, NGAY_TAO);
  return { duLieu, thoId: tho.id };
}

function dung(duLieu: DuLieuChamCong, thoId: string, homNay = CUOI_KY) {
  return render(
    <ManHinhBaoCaoTho
      duLieu={duLieu}
      thoId={thoId}
      nam={2026}
      thang={8}
      homNay={homNay}
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
  test('hiện tên thợ và tháng đang xem', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.getByText('Tháng 8/2026')).toBeTruthy();
  });

  test('tóm tắt số công, tiền công và còn phải trả', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');

    dung(duLieu, thoId, '2026-08-03');

    expect(screen.getByText('2 công')).toBeTruthy();
    expect(screen.getAllByText('600.000 đ').length).toBeGreaterThan(0);
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

    expect(screen.getByLabelText('03/08 Thứ Hai, đi làm 2 công')).toBeTruthy();
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
    duLieu = datCong(duLieu, thoId, '2026-08-03', 'Sang', 0.5);

    dung(duLieu, thoId, '2026-08-03');

    expect(screen.getByText('0,5')).toBeTruthy();
    expect(screen.getByLabelText('03/08 Thứ Hai, đi làm 0,5 công')).toBeTruthy();
  });

  test('ngày đi đủ cả ngày chỉ có dấu tích, không ghi số', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Chieu');

    dung(duLieu, thoId, '2026-08-03');

    // Trong ô ngày 3 chỉ có mỗi số 3 — không ghi thêm "2" công, vì đi đủ là chuyện
    // thường ngày, ghi vào thì cả tháng chi chít số.
    const o = screen.getByLabelText('03/08 Thứ Hai, đi làm 2 công');
    expect(within(o).getByText('3')).toBeTruthy();
    expect(within(o).queryByText('2')).toBeNull();
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

  test('tháng chưa có công nào thì nói rõ chứ không để bảng trống', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId);

    expect(screen.getByText('Tháng này chưa có ngày công nào.')).toBeTruthy();
  });

  test('lọc khoảng hẹp thì tóm tắt và tờ lịch chỉ tính trong khoảng', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = cham(duLieu, thoId, '2026-08-20', 'Sang');

    dung(duLieu, thoId, '2026-08-31');
    expect(screen.getByText('2 công')).toBeTruthy();

    chonNgay('Đến', '15/08', 'Thứ Bảy');

    expect(screen.getByText('1 công')).toBeTruthy();
    expect(screen.getByText('01/08 – 15/08')).toBeTruthy();
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

    expect(screen.getByText('16/08 – 31/08')).toBeTruthy();
    expect(screen.getByText('Ứng tiền (1 lần)')).toBeTruthy();
    expect(screen.getByText('ứng mua thuốc')).toBeTruthy();
    expect(screen.queryByText('ứng đổ xăng')).toBeNull();
  });

  test('bấm Cả tháng là bỏ lọc, quay về trọn tháng', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId, '2026-08-31');

    fireEvent.press(screen.getByText('Nửa đầu'));
    expect(screen.getByText('01/08 – 15/08')).toBeTruthy();

    fireEvent.press(screen.getByText('Cả tháng'));
    expect(screen.getByText('Tháng 8/2026')).toBeTruthy();
  });

  test('chọn ngày đầu muộn hơn ngày cuối thì kéo luôn ngày cuối theo', () => {
    const { duLieu, thoId } = khoCoTho();
    dung(duLieu, thoId, '2026-08-31');

    fireEvent.press(screen.getByText('Nửa đầu'));
    chonNgay('Từ', '20/08', 'Thứ Năm');

    // Không khoá ngày lại cho bấm không ăn, mà kéo ngày cuối theo — không có ngõ cụt.
    expect(screen.getByText('20/08 – 20/08')).toBeTruthy();
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

  test('ứng quá tiền công thì còn phải trả là số âm', () => {
    let { duLieu, thoId } = khoCoTho(300_000);
    duLieu = cham(duLieu, thoId, '2026-08-03', 'Sang');
    duLieu = themUng(duLieu, thoId, '2026-08-05', 500_000);

    dung(duLieu, thoId);

    expect(screen.getByText('−200.000 đ')).toBeTruthy();
  });
});
