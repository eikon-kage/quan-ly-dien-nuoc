/**
 * Báo cáo chi tiết một khoảng ngày của một thợ: đi làm những ngày nào, nghỉ những ngày
 * nào, ứng tiền ngày nào. Đây là chỗ tra khi thợ thắc mắc "sao tháng này ít tiền thế",
 * hoặc khi trả tiền theo kỳ nửa tháng chứ không trọn tháng.
 */

import { BuoiCong, DuLieuChamCong, Tho, UngTien } from './kieu';
import { congNgay, ghep, soNgayTrongThang, tach } from './ngayViet';
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
  /** Khoảng ngày đã tính, để màn hình ghi rõ đang xem từ đâu tới đâu. */
  tuNgay: string;
  denNgay: string;
  /** Các ngày có công, xếp theo ngày tăng dần. */
  ngayCongs: NgayCong[];
  /** Ngày trong kỳ mà thợ không có công nào. */
  ngayNghis: string[];
  /** Các lần ứng tiền trong kỳ, xếp theo ngày. */
  ungTiens: UngTien[];
  tongCong: number;
  tienCong: number;
  daUng: number;
  /** Tiền kỳ trước quyết toán còn thiếu. Xem theo tháng thì luôn là 0. */
  noKyTruoc: number;
  conLai: number;
}

/**
 * Dựng báo cáo trên đúng một tập buổi công và ứng tiền đã lọc sẵn.
 *
 * Tách riêng khỏi `baoCaoKhoang` vì kỳ lương cắt theo *bản ghi nào đã quyết toán* chứ
 * không cắt theo ngày: mở chi tiết một thợ trong kỳ đang mở mà lọc theo khoảng ngày thì
 * sẽ đếm lẫn cả những buổi của kỳ trước đã trả tiền rồi.
 *
 * <paramref name="homNay"/> để cắt phần tương lai: ngày mai chưa tới thì không phải
 * "nghỉ", chỉ là chưa chấm. Không cắt thì mở bảng lương đầu kỳ sẽ thấy báo nghỉ gần
 * trọn kỳ, hoảng.
 */
export function baoCaoTuBanGhi(
  duLieu: DuLieuChamCong,
  thoId: string,
  buoiCongs: BuoiCong[],
  ungTiens: UngTien[],
  tuNgay: string,
  denNgay: string,
  homNay: string,
  noKyTruoc = 0,
): BaoCaoTho | null {
  const tho = duLieu.thos.find((t) => t.id === thoId);
  if (!tho) {
    return null;
  }

  const trongKy = buoiCongs.filter((b) => b.thoId === thoId);

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
  const batDau = tho.ngayTao > tuNgay ? tho.ngayTao : tuNgay;
  const ketThuc = homNay < denNgay ? homNay : denNgay;

  const ngayNghis: string[] = [];
  for (let ngay = batDau; ngay <= ketThuc; ngay = congNgay(ngay, 1)) {
    if (!theoNgay.has(ngay)) {
      ngayNghis.push(ngay);
    }
  }

  const ungCuaTho = ungTiens
    .filter((u) => u.thoId === thoId)
    .sort((a, b) => (a.ngay < b.ngay ? -1 : 1));

  const tienCong = ngayCongs.reduce((tong, d) => tong + d.tien, 0);
  const daUng = ungCuaTho.reduce((tong, u) => tong + u.soTien, 0);

  return {
    tho,
    tuNgay,
    denNgay,
    ngayCongs,
    ngayNghis,
    ungTiens: ungCuaTho,
    tongCong: ngayCongs.reduce((tong, d) => tong + d.tongCong, 0),
    tienCong,
    daUng,
    noKyTruoc,
    conLai: tienCong - daUng + noKyTruoc,
  };
}

/** Dựng báo cáo một khoảng ngày bất kỳ, hai đầu đều tính vào. */
export function baoCaoKhoang(
  duLieu: DuLieuChamCong,
  thoId: string,
  tuNgay: string,
  denNgay: string,
  homNay: string,
): BaoCaoTho | null {
  return baoCaoTuBanGhi(
    duLieu,
    thoId,
    duLieu.buoiCongs.filter((b) => b.ngay >= tuNgay && b.ngay <= denNgay),
    duLieu.ungTiens.filter((u) => u.ngay >= tuNgay && u.ngay <= denNgay),
    tuNgay,
    denNgay,
    homNay,
  );
}

/** Trọn một tháng — khoảng hay dùng nhất nên để sẵn. */
export function baoCaoThang(
  duLieu: DuLieuChamCong,
  thoId: string,
  nam: number,
  thang: number,
  homNay: string,
): BaoCaoTho | null {
  return baoCaoKhoang(
    duLieu,
    thoId,
    ghep(nam, thang, 1),
    ghep(nam, thang, soNgayTrongThang(nam, thang)),
    homNay,
  );
}

/** Kiểm tra nhanh dùng cho lời nhắc: hôm nay đã chấm cho ai chưa. */
export function daChamHomNay(duLieu: DuLieuChamCong, homNay: string): boolean {
  return duLieu.buoiCongs.some((b) => b.ngay === homNay);
}

/** Ngày trong tháng, để hiện gọn "03" thay vì "2026-08-03". */
export function ngayTrongThang(ngay: string): number {
  return tach(ngay).ngay;
}
