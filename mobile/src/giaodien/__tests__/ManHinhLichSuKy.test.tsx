import { fireEvent, render, screen } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { kyHienTai, quyetToan } from '../../nghiepvu/ky';
import { cham, themTho, themUng } from '../../nghiepvu/thaoTac';
import { ManHinhLichSuKy } from '../ManHinhLichSuKy';

function kho() {
  let duLieu = duLieuRong();
  const them = themTho(duLieu, 'Anh Tuấn', 300_000, '2026-08-01');
  duLieu = them.duLieu;

  duLieu = cham(duLieu, them.tho.id, '2026-08-03', 'Sang');
  duLieu = cham(duLieu, them.tho.id, '2026-08-03', 'Chieu');
  duLieu = themUng(duLieu, them.tho.id, '2026-08-04', 100_000);

  return { duLieu, tuan: them.tho.id };
}

function dung(duLieu: DuLieuChamCong) {
  let hienTai = duLieu;
  render(
    <ManHinhLichSuKy
      duLieu={duLieu}
      capNhat={(moi) => {
        hienTai = moi;
      }}
    />,
  );
  return { moiNhat: () => hienTai };
}

describe('màn hình kỳ đã chốt', () => {
  test('chưa chốt kỳ nào thì chỉ đường sang chỗ quyết toán', () => {
    const { duLieu } = kho();
    dung(duLieu);

    expect(screen.getAllByText('Chưa chốt kỳ nào').length).toBeGreaterThan(0);
    expect(screen.getByText(/sang mục Bảng lương bấm Quyết toán/i)).toBeTruthy();
  });

  test('kỳ đã chốt hiện đủ khoảng ngày, số công và tiền đã trả', () => {
    let { duLieu } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-05' });

    dung(duLieu);

    expect(screen.getByText('03/08 → 05/08')).toBeTruthy();
    expect(screen.getByText('1 kỳ đã quyết toán')).toBeTruthy();
    expect(screen.getByText('1 thợ · 1 công · chốt 05/08/2026')).toBeTruthy();
    // Tiền công 300.000, đã ứng 100.000, cầm về 200.000.
    expect(screen.getByText('200.000 đ')).toBeTruthy();
  });

  test('kỳ mới nhất có dấu riêng vì chỉ nó bỏ chốt được', () => {
    let { duLieu, tuan } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-05' });
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-12' });

    dung(duLieu);

    expect(screen.getByText('2 kỳ đã quyết toán')).toBeTruthy();
    expect(screen.getAllByText('Mới nhất')).toHaveLength(1);
  });

  test('mở tờ quyết toán rồi bỏ chốt, hỏi lại một lần trước khi làm', () => {
    let { duLieu } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-05' });
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Xem tờ quyết toán'));
    expect(screen.getByText('Chốt ngày 05/08/2026')).toBeTruthy();

    // Bấm một lần chưa làm gì, chỉ đổi thành câu hỏi — không có nút nào làm mất sổ ngay
    // trong một cú chạm.
    fireEvent.press(screen.getByText('Bỏ chốt kỳ này'));
    expect(moiNhat().kyLuongs).toHaveLength(1);

    fireEvent.press(screen.getByText('Chắc chưa? Bấm lần nữa để bỏ chốt'));

    const sau = moiNhat();
    expect(sau.kyLuongs).toEqual([]);
    // Tiền quay lại kỳ đang mở, không mất buổi công nào.
    expect(sau.buoiCongs).toHaveLength(duLieu.buoiCongs.length);
    expect(kyHienTai(sau, '2026-08-06').tongPhaiTra).toBe(200_000);
  });

  test('đổi ý giữa chừng thì bấm Thôi, sổ giữ nguyên', () => {
    let { duLieu } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-05' });
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Xem tờ quyết toán'));
    fireEvent.press(screen.getByText('Bỏ chốt kỳ này'));
    fireEvent.press(screen.getByText('Thôi, giữ nguyên'));

    expect(screen.getByText('Bỏ chốt kỳ này')).toBeTruthy();
    expect(moiNhat().kyLuongs).toHaveLength(1);
  });

  test('kỳ cũ không có nút bỏ chốt', () => {
    let { duLieu, tuan } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-05' });
    duLieu = cham(duLieu, tuan, '2026-08-10', 'Sang');
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-12' });

    dung(duLieu);

    // Kỳ cũ nằm dưới trong danh sách vì kỳ mới nhất lên đầu.
    fireEvent.press(screen.getAllByText('Xem tờ quyết toán')[1]);

    expect(screen.getByText('Chốt ngày 05/08/2026')).toBeTruthy();
    expect(screen.queryByText('Bỏ chốt kỳ này')).toBeNull();
  });

  test('tên thợ trong tờ quyết toán là tên của lúc trả tiền', () => {
    let { duLieu, tuan } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-05' });
    duLieu = {
      ...duLieu,
      thos: duLieu.thos.map((t) => (t.id === tuan ? { ...t, ten: 'Tuấn con' } : t)),
    };

    dung(duLieu);
    fireEvent.press(screen.getByText('Xem tờ quyết toán'));

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.queryByText('Tuấn con')).toBeNull();
  });
});
