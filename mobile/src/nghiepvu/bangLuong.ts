/**
 * Bảng lương: mỗi thợ làm bao nhiêu công, thành bao nhiêu tiền, đã ứng bao nhiêu
 * và còn phải trả bao nhiêu.
 */

import { DuLieuChamCong, Tho } from './kieu';
import { ghep } from './ngayViet';
import { luongTaiNgay } from './thaoTac';

export interface DongLuong {
  tho: Tho;
  congSang: number;
  congChieu: number;
  tongCong: number;
  /** Tiền công đã tính theo giá của lúc chấm từng buổi. */
  tienCong: number;
  daUng: number;
  /** Số tiền còn phải trả thợ. Ứng quá tay thì số này âm. */
  conLai: number;
}

/**
 * Tính bảng lương trong khoảng ngày, tính cả tuNgay và denNgay.
 * Thợ đã nghỉ vẫn hiện nếu trong kỳ có công hoặc có ứng tiền. Xếp theo tên thợ.
 */
export function tinh(duLieu: DuLieuChamCong, tuNgay: string, denNgay: string): DongLuong[] {
  const ketQua: DongLuong[] = [];

  for (const tho of duLieu.thos) {
    const buoiCongs = duLieu.buoiCongs.filter(
      (b) => b.thoId === tho.id && b.ngay >= tuNgay && b.ngay <= denNgay,
    );

    const daUng = duLieu.ungTiens
      .filter((u) => u.thoId === tho.id && u.ngay >= tuNgay && u.ngay <= denNgay)
      .reduce((tong, u) => tong + u.soTien, 0);

    if (buoiCongs.length === 0 && daUng === 0) {
      continue;
    }

    const cong = (buoi: 'Sang' | 'Chieu') =>
      buoiCongs.filter((b) => b.buoi === buoi).reduce((tong, b) => tong + b.soCong, 0);

    const congSang = cong('Sang');
    const congChieu = cong('Chieu');
    // Giá của từng buổi lấy theo mốc lương tại đúng ngày đó, nên tăng lương giữa tháng
    // thì nửa đầu tháng vẫn tính giá cũ, nửa sau tính giá mới.
    const tienCong = Math.round(
      buoiCongs.reduce(
        (tong, b) => tong + b.soCong * (b.tienMotCong ?? luongTaiNgay(tho, b.ngay)),
        0,
      ),
    );

    ketQua.push({
      tho,
      congSang,
      congChieu,
      tongCong: congSang + congChieu,
      tienCong,
      daUng,
      conLai: tienCong - daUng,
    });
  }

  return ketQua.sort((a, b) => a.tho.ten.localeCompare(b.tho.ten, 'vi', { sensitivity: 'base' }));
}

/** Bảng lương của trọn một tháng. */
export function thang(duLieu: DuLieuChamCong, nam: number, thangTrongNam: number): DongLuong[] {
  const soNgay = new Date(Date.UTC(nam, thangTrongNam, 0)).getUTCDate();
  return tinh(duLieu, ghep(nam, thangTrongNam, 1), ghep(nam, thangTrongNam, soNgay));
}
