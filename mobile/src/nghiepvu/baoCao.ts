/**
 * Báo cáo chi tiết một tháng của một thợ: đi làm những ngày nào, nghỉ những ngày nào,
 * ứng tiền ngày nào. Đây là chỗ tra khi thợ thắc mắc "sao tháng này ít tiền thế".
 */

import { DuLieuChamCong, Tho, UngTien } from './kieu';
import { ghep, tach } from './ngayViet';
import { luongTaiNgay } from './thaoTac';

/** Một ngày có đi làm. */
export interface NgayCong {
  ngay: string;
  /** Số công buổi sáng, null là không đi buổi đó. */
  congSang: number | null;
  congChieu: number | null;
  tongCong: number;
  tien: number;
}

export interface BaoCaoTho {
  tho: Tho;
  /** Các ngày có công, xếp theo ngày tăng dần. */
  ngayCongs: NgayCong[];
  /** Ngày trong kỳ mà thợ không có công nào. */
  ngayNghis: string[];
  /** Các lần ứng tiền trong kỳ, xếp theo ngày. */
  ungTiens: UngTien[];
  tongCong: number;
  tienCong: number;
  daUng: number;
  conLai: number;
}

/** Số ngày của một tháng. */
export function soNgayTrongThang(nam: number, thang: number): number {
  return new Date(Date.UTC(nam, thang, 0)).getUTCDate();
}

/**
 * Dựng báo cáo một tháng.
 *
 * <paramref name="homNay"/> để cắt phần tương lai: ngày mai chưa tới thì không phải
 * "nghỉ", chỉ là chưa chấm. Không cắt thì mở bảng lương đầu tháng sẽ thấy báo nghỉ
 * gần trọn tháng, hoảng.
 */
export function baoCaoThang(
  duLieu: DuLieuChamCong,
  thoId: string,
  nam: number,
  thang: number,
  homNay: string,
): BaoCaoTho | null {
  const tho = duLieu.thos.find((t) => t.id === thoId);
  if (!tho) {
    return null;
  }

  const dauThang = ghep(nam, thang, 1);
  const cuoiThang = ghep(nam, thang, soNgayTrongThang(nam, thang));

  const trongKy = duLieu.buoiCongs.filter(
    (b) => b.thoId === thoId && b.ngay >= dauThang && b.ngay <= cuoiThang,
  );

  const theoNgay = new Map<string, NgayCong>();
  for (const buoi of trongKy) {
    const dong = theoNgay.get(buoi.ngay) ?? {
      ngay: buoi.ngay,
      congSang: null,
      congChieu: null,
      tongCong: 0,
      tien: 0,
    };

    if (buoi.buoi === 'Sang') {
      dong.congSang = buoi.soCong;
    } else {
      dong.congChieu = buoi.soCong;
    }

    dong.tongCong += buoi.soCong;
    dong.tien += buoi.soCong * (buoi.tienMotCong ?? luongTaiNgay(tho, buoi.ngay));
    theoNgay.set(buoi.ngay, dong);
  }

  const ngayCongs = [...theoNgay.values()]
    .map((d) => ({ ...d, tien: Math.round(d.tien) }))
    .sort((a, b) => (a.ngay < b.ngay ? -1 : 1));

  // Ngày nghỉ chỉ tính trong khoảng đã trôi qua, và từ lúc thợ vào làm trở đi.
  const batDau = tho.ngayTao > dauThang ? tho.ngayTao : dauThang;
  const ketThuc = homNay < cuoiThang ? homNay : cuoiThang;

  const ngayNghis: string[] = [];
  for (let ngay = 1; ngay <= soNgayTrongThang(nam, thang); ngay++) {
    const chuoi = ghep(nam, thang, ngay);
    if (chuoi >= batDau && chuoi <= ketThuc && !theoNgay.has(chuoi)) {
      ngayNghis.push(chuoi);
    }
  }

  const ungTiens = duLieu.ungTiens
    .filter((u) => u.thoId === thoId && u.ngay >= dauThang && u.ngay <= cuoiThang)
    .sort((a, b) => (a.ngay < b.ngay ? -1 : 1));

  const tienCong = ngayCongs.reduce((tong, d) => tong + d.tien, 0);
  const daUng = ungTiens.reduce((tong, u) => tong + u.soTien, 0);

  return {
    tho,
    ngayCongs,
    ngayNghis,
    ungTiens,
    tongCong: ngayCongs.reduce((tong, d) => tong + d.tongCong, 0),
    tienCong,
    daUng,
    conLai: tienCong - daUng,
  };
}

/** Kiểm tra nhanh dùng cho lời nhắc: hôm nay đã chấm cho ai chưa. */
export function daChamHomNay(duLieu: DuLieuChamCong, homNay: string): boolean {
  return duLieu.buoiCongs.some((b) => b.ngay === homNay);
}

/** Ngày trong tháng, để hiện gọn "03" thay vì "2026-08-03". */
export function ngayTrongThang(ngay: string): number {
  return tach(ngay).ngay;
}
