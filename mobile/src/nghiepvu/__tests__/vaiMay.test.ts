/**
 * Vai máy và mã mời.
 *
 * Chỗ đáng kiểm nhất là `ketNap` với `xoaNguoiKhac`: một cái điện thoại cũ của chủ chuyền
 * cho thợ dùng. Nếu nó chỉ *ẩn* sổ của người khác thay vì xoá thì thợ vẫn cầm trong tay
 * tiền công của cả nhóm.
 */

import { duLieuRong } from '../kieu';
import { quyetToan } from '../ky';
import { cham, themTho, themUng, timTho } from '../thaoTac';
import { MAC_DINH, docMaMoi, ketNap, maMoi } from '../vaiMay';

const NGAY_TAO = '2026-07-01';
const HOM_NAY = '2026-08-19';

describe('mã mời', () => {
  it('đọc lại được id của thợ', () => {
    expect(docMaMoi(maMoi('mf3k2a-9xq1'))).toBe('mf3k2a-9xq1');
  });

  it('tha cho khoảng trắng và mã gõ thiếu tiền tố', () => {
    expect(docMaMoi('  CC-abc123 ')).toBe('abc123');
    expect(docMaMoi('abc123')).toBe('abc123');
    expect(docMaMoi('cc-abc123')).toBe('abc123');
  });

  it('từ chối mã có dấu, có khoảng trắng ở giữa, hay rỗng', () => {
    expect(docMaMoi('CC-anh tuấn')).toBeNull();
    expect(docMaMoi('')).toBeNull();
    expect(docMaMoi('CC-')).toBeNull();
  });
});

describe('ketNap', () => {
  function khoCuaChu() {
    let duLieu = duLieuRong();

    const tuan = themTho(duLieu, 'Anh Tuấn', 300_000, NGAY_TAO);
    duLieu = tuan.duLieu;
    const binh = themTho(duLieu, 'Anh Bình', 250_000, NGAY_TAO);
    duLieu = binh.duLieu;

    duLieu = cham(duLieu, tuan.tho.id, '2026-08-10', 'Sang');
    duLieu = cham(duLieu, binh.tho.id, '2026-08-10', 'Sang');
    duLieu = themUng(duLieu, tuan.tho.id, '2026-08-10', 500_000);
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-10' });

    return { duLieu, tuan: tuan.tho.id, binh: binh.tho.id };
  }

  it('máy mới thì tạo bản ghi thợ mang đúng id của mã mời', () => {
    const duLieu = ketNap(duLieuRong(), 'abc123', HOM_NAY, false);

    expect(duLieu.thos).toHaveLength(1);
    expect(duLieu.thos[0].id).toBe('abc123');
    // Máy thợ không biết tiền công: mốc lương bằng 0.
    expect(duLieu.thos[0].mocLuong).toEqual([{ tuNgay: HOM_NAY, tienMotCong: 0 }]);
  });

  it('thợ đã có trong sổ thì không thêm lần nữa', () => {
    const { duLieu, tuan } = khoCuaChu();
    const sau = ketNap(duLieu, tuan, HOM_NAY, false);

    expect(sau.thos).toHaveLength(2);
    expect(sau).toBe(duLieu);
  });

  it('xoá sổ người khác thì chỉ còn buổi công của mình, sạch tiền', () => {
    const { duLieu, tuan, binh } = khoCuaChu();
    const sau = ketNap(duLieu, tuan, HOM_NAY, true);

    expect(sau.thos.map((t) => t.id)).toEqual([tuan]);
    expect(timTho(sau, binh)).toBeUndefined();
    expect(sau.buoiCongs.every((b) => b.thoId === tuan)).toBe(true);

    // Ứng tiền, kỳ đã chốt và mốc lương đều là tiền — không được còn lại gì.
    expect(sau.ungTiens).toEqual([]);
    expect(sau.kyLuongs).toEqual([]);
    expect(sau.thos[0].mocLuong).toEqual([{ tuNgay: NGAY_TAO, tienMotCong: 0 }]);
    expect(JSON.stringify(sau)).not.toContain('300000');
    expect(JSON.stringify(sau)).not.toContain('500000');
  });
});

describe('mặc định', () => {
  it('là máy chủ — máy đang cài app không được tự biến thành máy thợ', () => {
    expect(MAC_DINH.vai).toBe('chu');
  });
});
