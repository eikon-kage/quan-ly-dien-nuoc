import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { quyetToan } from '../../nghiepvu/ky';
import { cham, luuTho, themTho, themUng } from '../../nghiepvu/thaoTac';
import { HopSuaTho } from '../HopSuaTho';

const NGAY_LAM = '2026-08-03';

const hoi = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

beforeEach(() => hoi.mockClear());

/**
 * Bấm hộ nút trong hộp thoại của hệ điều hành. Bọc `act` vì nút ấy nằm ngoài cây React —
 * hộp là của máy — mà bấm vào lại đổi trạng thái màn hình.
 */
function bamNut(nhan: string) {
  const nut = (hoi.mock.calls[0][2] ?? []).find((n) => n.text === nhan);
  act(() => nut?.onPress?.());
}

/** Các nút hộp thoại vừa hỏi đang chìa ra. */
function cacNut(): string[] {
  return (hoi.mock.calls[0][2] ?? []).map((n) => n.text ?? '');
}

function khoCoTho(ten = 'Anh Tuấn') {
  const { duLieu, tho } = themTho(duLieuRong(), ten, 300_000, NGAY_LAM);
  return { duLieu, thoId: tho.id };
}

function dung(duLieu: DuLieuChamCong, thoId: string) {
  let hienTai = duLieu;
  let daDong = false;
  const tho = duLieu.thos.find((t) => t.id === thoId) ?? null;

  render(
    <HopSuaTho
      duLieu={duLieu}
      tho={tho}
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

describe('xoá thợ', () => {
  test('thêm thợ mới thì chưa có nút xoá — chưa có gì để xoá', () => {
    let hienTai = duLieuRong();
    render(
      <HopSuaTho
        duLieu={hienTai}
        tho={null}
        capNhat={(moi) => {
          hienTai = moi;
        }}
        onDong={() => {}}
      />,
    );

    expect(screen.queryByText('Xoá thợ này')).toBeNull();
  });

  test('thợ chưa có gì trong sổ thì xoá thẳng, chỉ hỏi lại một câu', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat, daDong } = dung(duLieu, thoId);

    fireEvent.press(screen.getByText('Xoá thợ này'));
    expect(moiNhat().thos).toHaveLength(1);
    expect(hoi.mock.calls[0][1]).toMatch(/chưa có buổi công nào/i);

    bamNut('Xoá');

    expect(moiNhat().thos).toEqual([]);
    expect(daDong()).toBe(true);
  });

  test('hỏi lại nói rõ mất những gì', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, NGAY_LAM, 'Sang');
    duLieu = cham(duLieu, thoId, NGAY_LAM, 'Chieu');
    duLieu = themUng(duLieu, thoId, NGAY_LAM, 500_000);

    const { moiNhat } = dung(duLieu, thoId);
    fireEvent.press(screen.getByText('Xoá thợ này'));

    expect(hoi.mock.calls[0][0]).toBe('Xoá Anh Tuấn?');
    expect(hoi.mock.calls[0][1]).toMatch(/2 buổi công và 1 lần ứng/);

    bamNut('Xoá');

    // Xoá thợ là xoá cả phần sổ của người ấy, không để lại buổi công mồ côi.
    expect(moiNhat().thos).toEqual([]);
    expect(moiNhat().buoiCongs).toEqual([]);
    expect(moiNhat().ungTiens).toEqual([]);
  });

  test('bấm Thôi thì không mất gì', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat, daDong } = dung(duLieu, thoId);

    fireEvent.press(screen.getByText('Xoá thợ này'));
    bamNut('Thôi');

    expect(moiNhat().thos).toHaveLength(1);
    expect(daDong()).toBe(false);
  });

  test('luôn chìa ra lối Cho nghỉ, và chọn nó thì sổ cũ còn nguyên', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, NGAY_LAM, 'Sang');

    const { moiNhat, daDong } = dung(duLieu, thoId);
    fireEvent.press(screen.getByText('Xoá thợ này'));

    expect(cacNut()).toEqual(['Thôi', 'Cho nghỉ', 'Xoá']);

    bamNut('Cho nghỉ');

    expect(moiNhat().thos).toHaveLength(1);
    expect(moiNhat().thos[0].dangLam).toBe(false);
    expect(moiNhat().buoiCongs).toHaveLength(1);
    expect(daDong()).toBe(true);
  });

  test('Cho nghỉ giữ luôn cái tên vừa sửa dở', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat } = dung(duLieu, thoId);

    fireEvent.changeText(screen.getByPlaceholderText('Ví dụ: Anh Tuấn'), '  Anh Tuấn Anh  ');
    fireEvent.press(screen.getByText('Xoá thợ này'));
    bamNut('Cho nghỉ');

    expect(moiNhat().thos[0].ten).toBe('Anh Tuấn Anh');
    expect(moiNhat().thos[0].dangLam).toBe(false);
  });

  test('thợ đã nghỉ rồi thì bỏ nút Cho nghỉ đi — Android chỉ vẽ được ba nút', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = luuTho(duLieu, { ...duLieu.thos[0], dangLam: false });

    dung(duLieu, thoId);
    fireEvent.press(screen.getByText('Xoá thợ này'));

    expect(cacNut()).toEqual(['Thôi', 'Xoá']);
  });

  test('thợ đã có tên trong kỳ đã chốt thì chặn xoá, mời cho nghỉ', () => {
    let { duLieu, thoId } = khoCoTho();
    duLieu = cham(duLieu, thoId, NGAY_LAM, 'Sang');
    duLieu = quyetToan(duLieu, { denNgay: NGAY_LAM });

    const { moiNhat } = dung(duLieu, thoId);
    fireEvent.press(screen.getByText('Xoá thợ này'));

    expect(hoi.mock.calls[0][0]).toBe('Không xoá được thợ này');
    expect(cacNut()).toEqual(['Thôi', 'Cho nghỉ']);

    bamNut('Cho nghỉ');

    // Tờ quyết toán cũ còn nguyên, chỉ là người ấy thôi không hiện ở màn hình chấm công.
    expect(moiNhat().thos[0].dangLam).toBe(false);
    expect(moiNhat().kyLuongs).toHaveLength(1);
  });
});
