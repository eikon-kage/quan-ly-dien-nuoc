/**
 * File Excel cho sổ công của một thợ.
 *
 * Điều quan trọng nhất, và là lý do file này tồn tại: **trong file không được có một con số
 * tiền nào.** Máy thợ dùng hàm này, mà cả app trên máy thợ không biết tiền công là bao nhiêu.
 * Soát bằng cách đọc thẳng vào XML trong file, không soát qua giao diện: giao diện ẩn được
 * chứ file gửi đi rồi là nằm trong tay người ta.
 */

import { catSo } from '../soCong';
import { cham, themTho, themUng } from '../thaoTac';
import { duLieuRong } from '../kieu';
import { tenFileSoCong, trangSoCong, xuatSoCong } from '../xuatSoCong';
import { unzipSync, strFromU8 } from 'fflate';

function soCuaTho() {
  const them = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  let duLieu = cham(them.duLieu, them.tho.id, '2026-08-03', 'Sang');
  duLieu = cham(duLieu, them.tho.id, '2026-08-03', 'Chieu', 0.25);
  duLieu = themUng(duLieu, them.tho.id, '2026-08-04', 500_000, 'mua xăng');

  return catSo(duLieu, them.tho.id, 'tho', '2026-08-01', '2026-08-19', '2026-08-19T08:00:00Z');
}

/** Toàn bộ chữ trong các file XML của .xlsx, ghép lại để soát cho chắc. */
function chuTrongFile(byte: Uint8Array): string {
  const zip = unzipSync(byte);
  return Object.values(zip)
    .map((noiDung) => strFromU8(noiDung))
    .join('\n');
}

describe('tên file', () => {
  test('theo ngày cuối của sổ, không dấu', () => {
    expect(tenFileSoCong(soCuaTho())).toBe('So-cong-19-08-2026.xlsx');
  });
});

describe('nội dung trang', () => {
  test('mỗi buổi một dòng, cuối trang là tổng số công', () => {
    const trang = trangSoCong(soCuaTho());

    expect(trang.ten).toBe('Sổ công');
    expect(trang.dongs).toEqual([
      ['2026-08-03', 'Thứ Hai', 'Sáng', 0.5],
      ['2026-08-03', 'Thứ Hai', 'Chiều', 0.25],
    ]);
    expect(trang.dongTong).toEqual(['Tổng cộng', null, null, 0.75]);
  });

  test('sổ trống thì không có dòng tổng — cộng số không có gì là vô nghĩa', () => {
    const trang = trangSoCong({ ...soCuaTho(), dongs: [] });

    expect(trang.dongs).toEqual([]);
    expect(trang.dongTong).toBeUndefined();
  });

  test('không có cột nào mang tiền', () => {
    const nhan = trangSoCong(soCuaTho()).cots.map((cot) => cot.nhan);

    expect(nhan).toEqual(['Ngày', 'Thứ', 'Buổi', 'Số công']);
    expect(trangSoCong(soCuaTho()).cots.some((cot) => cot.kieu === 'tien')).toBe(false);
  });
});

describe('file gửi đi', () => {
  test('mở ra được và không có một đồng nào trong đó', () => {
    const chu = chuTrongFile(xuatSoCong(soCuaTho()));

    expect(chu).toContain('Sổ công');
    // Tiền công, tiền ứng, và cả chữ "tiền" lẫn tên thợ đều không được có mặt.
    expect(chu).not.toContain('300000');
    expect(chu).not.toContain('500000');
    expect(chu).not.toMatch(/[Tt]iền/);
  });
});
