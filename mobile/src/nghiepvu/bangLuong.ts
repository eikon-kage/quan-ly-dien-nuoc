/**
 * Bảng lương: mỗi thợ làm bao nhiêu công, thành bao nhiêu tiền, đã ứng bao nhiêu
 * và còn phải trả bao nhiêu.
 */

import { BuoiCong, DuLieuChamCong, Tho, UngTien } from './kieu';
import { cacThangTrongKhoang, ghep } from './ngayViet';
import { luongTaiNgay } from './thaoTac';

export interface DongLuong {
  tho: Tho;
  congSang: number;
  congChieu: number;
  tongCong: number;
  /** Tiền công đã tính theo giá của lúc chấm từng buổi. */
  tienCong: number;
  daUng: number;
  /**
   * Tiền kỳ trước quyết toán còn thiếu, mang sang kỳ này. Số âm là kỳ trước trả dư,
   * kỳ này trừ lại. Xem khoảng ngày bất kỳ (không phải kỳ lương) thì luôn là 0.
   */
  noKyTruoc: number;
  /** Số tiền còn phải trả thợ. Ứng quá tay thì số này âm. */
  conLai: number;
}

/**
 * Tính bảng lương trên đúng một tập buổi công và ứng tiền đã lọc sẵn.
 *
 * Tách riêng khỏi `tinh` vì kỳ lương không cắt theo ngày mà cắt theo *bản ghi nào đã
 * quyết toán* — chấm bù một ngày cũ thì buổi đó vẫn thuộc kỳ đang mở. Xếp theo tên thợ.
 *
 * `noTheoTho` là tiền kỳ trước còn thiếu của từng thợ; thợ nào không có thì coi như 0.
 * Thợ chỉ có mỗi khoản nợ, kỳ này chưa làm buổi nào, vẫn phải hiện ra — nếu không thì
 * món nợ biến mất khỏi màn hình mà vẫn nằm trong sổ.
 */
export function tinhTuBanGhi(
  duLieu: DuLieuChamCong,
  buoiCongs: BuoiCong[],
  ungTiens: UngTien[],
  noTheoTho: Map<string, number> = new Map(),
): DongLuong[] {
  const ketQua: DongLuong[] = [];

  for (const tho of duLieu.thos) {
    const cuaTho = buoiCongs.filter((b) => b.thoId === tho.id);
    const ungCuaTho = ungTiens.filter((u) => u.thoId === tho.id);
    const daUng = ungCuaTho.reduce((tong, u) => tong + u.soTien, 0);
    const noKyTruoc = noTheoTho.get(tho.id) ?? 0;

    if (cuaTho.length === 0 && daUng === 0 && noKyTruoc === 0) {
      continue;
    }

    const cong = (buoi: 'Sang' | 'Chieu') =>
      cuaTho.filter((b) => b.buoi === buoi).reduce((tong, b) => tong + b.soCong, 0);

    const congSang = cong('Sang');
    const congChieu = cong('Chieu');
    // Giá của từng buổi lấy theo mốc lương tại đúng ngày đó, nên tăng lương giữa tháng
    // thì nửa đầu tháng vẫn tính giá cũ, nửa sau tính giá mới.
    const tienCong = Math.round(
      cuaTho.reduce((tong, b) => tong + b.soCong * (b.tienMotCong ?? luongTaiNgay(tho, b.ngay)), 0),
    );

    ketQua.push({
      tho,
      congSang,
      congChieu,
      tongCong: congSang + congChieu,
      tienCong,
      daUng,
      noKyTruoc,
      conLai: tienCong - daUng + noKyTruoc,
    });
  }

  return ketQua.sort((a, b) => a.tho.ten.localeCompare(b.tho.ten, 'vi', { sensitivity: 'base' }));
}

/**
 * Tính bảng lương trong khoảng ngày, tính cả tuNgay và denNgay.
 * Thợ đã nghỉ vẫn hiện nếu trong kỳ có công hoặc có ứng tiền. Xếp theo tên thợ.
 */
export function tinh(duLieu: DuLieuChamCong, tuNgay: string, denNgay: string): DongLuong[] {
  return tinhTuBanGhi(
    duLieu,
    duLieu.buoiCongs.filter((b) => b.ngay >= tuNgay && b.ngay <= denNgay),
    duLieu.ungTiens.filter((u) => u.ngay >= tuNgay && u.ngay <= denNgay),
  );
}

/** Bảng lương của trọn một tháng. */
export function thang(duLieu: DuLieuChamCong, nam: number, thangTrongNam: number): DongLuong[] {
  const soNgay = new Date(Date.UTC(nam, thangTrongNam, 0)).getUTCDate();
  return tinh(duLieu, ghep(nam, thangTrongNam, 1), ghep(nam, thangTrongNam, soNgay));
}

/**
 * Các tháng xem lại được trên màn hình Bảng lương: từ tháng có bản ghi sớm nhất tới tháng
 * của hôm nay, **tháng mới nhất đứng đầu**.
 *
 * Liền mạch chứ không chỉ lấy tháng có công. Tháng nghỉ trắng vẫn nằm trong danh sách:
 * bấm mũi tên lùi từng tháng mà app tự nhảy qua tháng trống thì người xem tưởng mình bấm
 * hụt, chứ không nghĩ là tháng ấy không có ai đi làm.
 */
export function cacThangXemDuoc(
  duLieu: DuLieuChamCong,
  homNay: string,
): { nam: number; thang: number }[] {
  const cacNgay = [...duLieu.buoiCongs.map((b) => b.ngay), ...duLieu.ungTiens.map((u) => u.ngay)];
  if (cacNgay.length === 0) {
    return [];
  }

  const somNhat = cacNgay.reduce((a, b) => (a < b ? a : b));
  // Chấm nhầm sang ngày tương lai thì tháng ấy vẫn phải tới được, kẻo buổi công biến mất
  // khỏi mọi màn hình xem lại.
  const muonNhat = cacNgay.reduce((a, b) => (a > b ? a : b));
  const den = muonNhat > homNay ? muonNhat : homNay;

  return cacThangTrongKhoang(somNhat, den).reverse();
}
