import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { cham, themTho, themUng } from '../../nghiepvu/thaoTac';
import { ManHinhBangLuong } from '../ManHinhBangLuong';

const HOM_NAY = Ngay.homNay();

function khoCoTho() {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY);
  return { duLieu: cham(duLieu, tho.id, HOM_NAY, 'Sang'), thoId: tho.id };
}

function dung(duLieu: DuLieuChamCong) {
  let hienTai = duLieu;
  render(
    <ManHinhBangLuong
      duLieu={duLieu}
      capNhat={(moi) => {
        hienTai = moi;
      }}
    />,
  );
  return { moiNhat: () => hienTai };
}

describe('ứng tiền ở bảng lương', () => {
  test('ghi kèm ghi chú để sau còn nhớ ứng vào việc gì', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Ứng tiền'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 500000'), '500000');
    fireEvent.changeText(screen.getByPlaceholderText('Ví dụ: ứng đổ xăng'), '  ứng mua thuốc  ');
    fireEvent.press(screen.getByText('Ghi'));

    const ung = moiNhat().ungTiens[0];
    expect(ung.thoId).toBe(thoId);
    expect(ung.soTien).toBe(500_000);
    // Khoảng trắng thừa hai đầu bị cắt, kẻo dòng ghi chú trong báo cáo bị thụt vào.
    expect(ung.ghiChu).toBe('ứng mua thuốc');
  });

  test('không điền ghi chú vẫn ứng được', () => {
    const { duLieu } = khoCoTho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Ứng tiền'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 500000'), '500000');
    fireEvent.press(screen.getByText('Ghi'));

    expect(moiNhat().ungTiens).toHaveLength(1);
    expect(moiNhat().ungTiens[0].ghiChu).toBe('');
  });
});

/**
 * Sửa lịch sử ứng: mở chi tiết một thợ rồi chạm vào dòng ứng. Ở đây soi cả đường đi từ
 * bảng lương xuống tận sổ — hai đầu nối đúng vào nhau thì con số mới thật sự đổi.
 */
describe('sửa lịch sử ứng tiền', () => {
  const hoi = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

  beforeEach(() => hoi.mockClear());

  function moHopSua(soTien = 5_000_000) {
    const { duLieu, thoId } = khoCoTho();
    const daUng = themUng(duLieu, thoId, HOM_NAY, soTien, 'ứng đổ xăng');
    const { moiNhat } = dung(daUng);

    fireEvent.press(screen.getByText('Xem chi tiết từng ngày'));
    fireEvent.press(screen.getByLabelText(/chạm để sửa/));

    return { moiNhat, ungId: daUng.ungTiens[0].id };
  }

  test('sửa số tiền gõ thừa một số 0', () => {
    const { moiNhat, ungId } = moHopSua();

    fireEvent.changeText(screen.getByLabelText('Số tiền ứng'), '500000');
    fireEvent.press(screen.getByText('Ghi'));

    // Vẫn đúng lần ứng cũ chứ không đẻ thêm dòng mới.
    expect(moiNhat().ungTiens).toHaveLength(1);
    expect(moiNhat().ungTiens[0].id).toBe(ungId);
    expect(moiNhat().ungTiens[0].soTien).toBe(500_000);
  });

  test('xoá hẳn lần ứng ghi nhầm', () => {
    const { moiNhat } = moHopSua();

    fireEvent.press(screen.getByText('Xoá lần ứng này'));
    // Bọc `act`: nút của hộp thoại nằm ngoài cây React mà bấm vào thì màn hình đổi.
    const nut = (hoi.mock.calls[0][2] ?? []).find((n) => n.text === 'Xoá');
    act(() => nut?.onPress?.());

    expect(moiNhat().ungTiens).toEqual([]);
  });
});
