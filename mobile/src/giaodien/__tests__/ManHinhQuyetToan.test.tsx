import { fireEvent, render, screen } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { kyGanNhat, kyHienTai, quyetToan } from '../../nghiepvu/ky';
import { cham, themTho, themUng } from '../../nghiepvu/thaoTac';
import { ManHinhQuyetToan } from '../ManHinhQuyetToan';

const HOM_NAY = '2026-08-05';

/** Hai thợ, mỗi người một ngày công; anh Tuấn có ứng trước 200.000. */
function kho() {
  let duLieu = duLieuRong();

  const themTuan = themTho(duLieu, 'Anh Tuấn', 300_000, '2026-08-01');
  duLieu = themTuan.duLieu;
  const themBinh = themTho(duLieu, 'Anh Bình', 250_000, '2026-08-01');
  duLieu = themBinh.duLieu;

  duLieu = cham(duLieu, themTuan.tho.id, '2026-08-03', 'Sang');
  duLieu = cham(duLieu, themTuan.tho.id, '2026-08-03', 'Chieu');
  duLieu = themUng(duLieu, themTuan.tho.id, '2026-08-04', 200_000);
  duLieu = cham(duLieu, themBinh.tho.id, '2026-08-03', 'Sang');

  return { duLieu, tuan: themTuan.tho.id, binh: themBinh.tho.id };
}

function dung(duLieu: DuLieuChamCong) {
  let hienTai = duLieu;
  let daDong = false;

  render(
    <ManHinhQuyetToan
      duLieu={duLieu}
      homNay={HOM_NAY}
      capNhat={(moi) => {
        hienTai = moi;
      }}
      onDong={() => {
        daDong = true;
      }}
    />,
  );

  return { moiNhat: () => hienTai, daDong: () => daDong };
}

describe('màn hình quyết toán', () => {
  test('hiện khoảng kỳ và từng thợ phải trả bao nhiêu', () => {
    const { duLieu } = kho();
    dung(duLieu);

    expect(screen.getByText('Quyết toán kỳ')).toBeTruthy();
    expect(screen.getByText('03/08 → 05/08')).toBeTruthy();

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.getByText('Anh Bình')).toBeTruthy();

    // Tuấn: 2 công 600.000 trừ 200.000 đã ứng còn 400.000. Bình: 1 công 250.000.
    expect(screen.getByText('Tổng phải trả').parent).toBeTruthy();
    expect(screen.getAllByText('400.000 đ').length).toBeGreaterThan(0);
    expect(screen.getAllByText('250.000 đ').length).toBeGreaterThan(0);
  });

  test('điền sẵn là trả đủ, bấm một nút là xong', () => {
    const { duLieu, tuan, binh } = kho();
    const { moiNhat, daDong } = dung(duLieu);

    fireEvent.press(screen.getByText('Chốt kỳ, đã trả tiền'));

    const ky = kyGanNhat(moiNhat())!;
    expect(ky.dongs.find((d) => d.thoId === tuan)?.daTra).toBe(400_000);
    expect(ky.dongs.find((d) => d.thoId === binh)?.daTra).toBe(250_000);
    expect(ky.dongs.every((d) => d.chuyenKySau === 0)).toBe(true);
    expect(daDong()).toBe(true);
  });

  test('chốt xong bảng lương về 0 mà buổi công vẫn còn nguyên', () => {
    const { duLieu } = kho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Chốt kỳ, đã trả tiền'));

    const sauKhiChot = moiNhat();
    expect(kyHienTai(sauKhiChot, '2026-08-06').dongs).toEqual([]);
    expect(sauKhiChot.buoiCongs).toHaveLength(duLieu.buoiCongs.length);
    expect(sauKhiChot.ungTiens).toHaveLength(duLieu.ungTiens.length);
  });

  test('bấm Không trả thì người đó nợ nguyên sang kỳ sau', () => {
    const { duLieu, binh } = kho();
    const { moiNhat } = dung(duLieu);

    // Hai thẻ, mỗi thẻ một nút — anh Bình xếp trước vì tên tiếng Việt xếp B trước T.
    fireEvent.press(screen.getAllByText('Không trả')[0]);
    // Hiện hai chỗ: trên thẻ anh Bình và ở dòng tổng dưới chân màn hình.
    expect(screen.getAllByText('Còn nợ, chuyển kỳ sau')).toHaveLength(2);

    fireEvent.press(screen.getByText('Chốt kỳ, đã trả tiền'));

    const dongBinh = kyGanNhat(moiNhat())!.dongs.find((d) => d.thoId === binh)!;
    expect(dongBinh.daTra).toBe(0);
    expect(dongBinh.chuyenKySau).toBe(250_000);
    // Vẫn nằm trong tờ quyết toán chứ không biến mất khỏi sổ.
    expect(kyGanNhat(moiNhat())!.dongs).toHaveLength(2);
  });

  test('sửa số thực trả thì phần thiếu hiện ngay trước khi chốt', () => {
    const { duLieu, tuan } = kho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByLabelText('Anh Tuấn thực trả 400.000 đ, chạm để sửa'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 2000000'), '300000');
    fireEvent.press(screen.getByText('Ghi'));

    expect(screen.getAllByText('Còn nợ, chuyển kỳ sau')).toHaveLength(2);
    expect(screen.getByLabelText('Anh Tuấn thực trả 300.000 đ, chạm để sửa')).toBeTruthy();

    fireEvent.press(screen.getByText('Chốt kỳ, đã trả tiền'));
    expect(kyGanNhat(moiNhat())!.dongs.find((d) => d.thoId === tuan)?.chuyenKySau).toBe(100_000);
  });

  test('kỳ trước còn nợ thì kỳ này hiện thành một dòng riêng', () => {
    let { duLieu, tuan } = kho();
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-04', daTra: new Map([[tuan, 0]]) });
    duLieu = cham(duLieu, tuan, '2026-08-05', 'Sang');

    dung(duLieu);

    expect(screen.getByText('Nợ kỳ trước')).toBeTruthy();
    // 300.000 công mới cộng 400.000 nợ cũ.
    expect(screen.getByLabelText('Anh Tuấn thực trả 700.000 đ, chạm để sửa')).toBeTruthy();
  });

  test('nói trước là chốt xong vẫn bỏ ra được', () => {
    const { duLieu } = kho();
    dung(duLieu);

    expect(
      screen.getByText(/Chốt xong dữ liệu cũ vẫn còn nguyên/),
    ).toBeTruthy();
  });
});
