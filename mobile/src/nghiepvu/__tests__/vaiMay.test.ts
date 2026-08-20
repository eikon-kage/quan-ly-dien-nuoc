/**
 * Vai máy.
 *
 * Chỗ đáng kiểm nhất là `ketNap` với `xoaNguoiKhac`: một cái điện thoại cũ của chủ chuyền
 * cho thợ dùng. Nếu nó chỉ *ẩn* sổ của người khác thay vì xoá thì thợ vẫn cầm trong tay
 * tiền công của cả nhóm.
 */

import { duLieuRong } from '../kieu';
import { quyetToan } from '../ky';
import { cham, dangCham, doiThoId, themTho, themUng, timTho } from '../thaoTac';
import { MAC_DINH, ketNap } from '../vaiMay';

const NGAY_TAO = '2026-07-01';
const HOM_NAY = '2026-08-19';

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

  it('máy mới thì tạo bản ghi thợ mang đúng id database trả về', () => {
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

/**
 * Thợ tự chấm trước khi xin được mã mời: id lúc ấy do máy tự đặt. Tới lúc dán mã, database
 * mới trả về id thật — không chuyển bản ghi sang id ấy thì thợ mở app lên thấy sổ trống trơn,
 * mà đối chiếu lại báo chủ chấm khống mấy chục buổi.
 */
describe('kết nạp sau khi đã tự chấm', () => {
  it('kéo mọi buổi đã chấm sang id thật', () => {
    let duLieu = themTho(duLieuRong(), 'Tôi', 0, '2026-08-10').duLieu;
    const idTuTao = duLieu.thos[0].id;
    duLieu = cham(duLieu, idTuTao, '2026-08-12', 'Sang');
    duLieu = cham(duLieu, idTuTao, '2026-08-12', 'Chieu');

    const sau = ketNap(duLieu, 'idThat', HOM_NAY, false, idTuTao);

    expect(sau.thos.map((t) => t.id)).toEqual(['idThat']);
    expect(dangCham(sau, 'idThat', '2026-08-12', 'Sang')?.soCong).toBe(1);
    expect(sau.buoiCongs).toHaveLength(2);
    expect(sau.buoiCongs.every((b) => b.thoId === 'idThat')).toBe(true);
    // Ngày vào làm giữ nguyên, kẻo buổi cũ rơi ra ngoài mốc lương đầu tiên.
    expect(sau.thos[0].ngayTao).toBe('2026-08-10');
  });

  it('không truyền id cũ thì không chuyển gì — mã của nhóm khác không được gộp sổ', () => {
    let duLieu = themTho(duLieuRong(), 'Tôi', 0, '2026-08-10').duLieu;
    const idCu = duLieu.thos[0].id;
    duLieu = cham(duLieu, idCu, '2026-08-12', 'Sang');

    const sau = ketNap(duLieu, 'idThat', HOM_NAY, false);

    expect(sau.thos.map((t) => t.id).sort()).toEqual([idCu, 'idThat'].sort());
    expect(dangCham(sau, 'idThat', '2026-08-12', 'Sang')).toBeUndefined();
  });
});

describe('doiThoId', () => {
  it('kéo cả ứng tiền và dòng kỳ đã chốt theo id mới', () => {
    let duLieu = themTho(duLieuRong(), 'Anh Tuấn', 300_000, NGAY_TAO).duLieu;
    const cu = duLieu.thos[0].id;
    duLieu = cham(duLieu, cu, '2026-08-10', 'Sang');
    duLieu = themUng(duLieu, cu, '2026-08-10', 500_000);
    duLieu = quyetToan(duLieu, { denNgay: '2026-08-10' });

    const sau = doiThoId(duLieu, cu, 'moi');

    expect(sau.thos[0].id).toBe('moi');
    expect(sau.buoiCongs.every((b) => b.thoId === 'moi')).toBe(true);
    expect(sau.ungTiens.every((u) => u.thoId === 'moi')).toBe(true);
    expect(sau.kyLuongs[0].dongs.every((d) => d.thoId === 'moi')).toBe(true);
  });

  it('id mới đã có bản ghi thợ thì giữ bản thật, bỏ bản tạm', () => {
    let duLieu = themTho(duLieuRong(), 'Tôi', 0, NGAY_TAO, 'tam').duLieu;
    duLieu = themTho(duLieu, 'Anh Tuấn', 0, NGAY_TAO, 'that').duLieu;
    duLieu = cham(duLieu, 'tam', '2026-08-12', 'Sang');

    const sau = doiThoId(duLieu, 'tam', 'that');

    expect(sau.thos.map((t) => t.id)).toEqual(['that']);
    expect(sau.thos[0].ten).toBe('Anh Tuấn');
    expect(dangCham(sau, 'that', '2026-08-12', 'Sang')).toBeDefined();
  });

  it('id cũ bằng id mới thì trả về đúng sổ đang có', () => {
    const duLieu = themTho(duLieuRong(), 'Tôi', 0, NGAY_TAO, 'x').duLieu;

    expect(doiThoId(duLieu, 'x', 'x')).toBe(duLieu);
  });
});

describe('mặc định', () => {
  it('là máy chủ — máy đang cài app không được tự biến thành máy thợ', () => {
    expect(MAC_DINH.vai).toBe('chu');
  });
});
