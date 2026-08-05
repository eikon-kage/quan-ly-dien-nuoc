import { fireEvent, render, screen } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { cham, themTho } from '../../nghiepvu/thaoTac';
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
