/**
 * Kỳ lương và quyết toán.
 *
 * Cách làm việc thật ngoài công trình: chấm công một thời gian — hết công trình, hết
 * tuần, hết tháng, tuỳ — rồi ngồi lại trả tiền cả tổ một lượt. Trả xong thì mọi con số
 * về 0 và bắt đầu đếm lại từ đầu. Đó là **quyết toán**.
 *
 * Hai điều quan trọng nhất ở đây:
 *
 * 1. **Quyết toán không xoá gì cả.** Buổi công và ứng tiền vẫn nằm nguyên trong dữ liệu.
 *    Chốt kỳ chỉ là ghi thêm một bản chụp (`KyLuong`) nói rằng "những bản ghi này đã trả
 *    tiền rồi". Bảng lương về 0 vì nó chỉ tính phần *chưa* trả, chứ không phải vì dữ liệu
 *    mất đi. Lỡ tay chốt nhầm thì `boChot` gỡ lại được, không mất một buổi công nào.
 *
 * 2. **Kỳ cắt theo bản ghi, không cắt theo ngày.** Nếu cắt theo ngày thì hôm sau chợt
 *    nhớ ra "thứ Ba tuần trước thằng Bình có đi" — chấm bù vào ngày đã nằm trong kỳ đã
 *    chốt — buổi ấy sẽ lọt ra ngoài cả kỳ cũ lẫn kỳ mới và thợ mất công. Nhớ theo id thì
 *    buổi chấm bù tự rơi vào kỳ đang mở, vì nó chưa được trả tiền.
 *
 * Trả thiếu thì phần thiếu thành *nợ đầu kỳ* của kỳ sau; trả dư thì thành số âm, kỳ sau
 * trừ lại. Nhờ vậy sổ luôn khớp với số tiền thật đã móc ví.
 */

import { DongLuong, tinhTuBanGhi } from './bangLuong';
import { BaoCaoTho, baoCaoTuBanGhi } from './baoCao';
import { BuoiCong, DuLieuChamCong, DongQuyetToan, KyLuong, UngTien } from './kieu';
import { congNgay } from './ngayViet';
import { taoId } from './thaoTac';

/** Kỳ đang mở: phần chưa ai trả tiền, cộng thêm nợ mang sang từ kỳ trước. */
export interface KyDangMo {
  tuNgay: string;
  denNgay: string;
  dongs: DongLuong[];
  /** Tổng số tiền phải móc ví nếu chốt kỳ ngay bây giờ. */
  tongPhaiTra: number;
  /** Kỳ trống trơn thì không có gì để chốt. */
  chotDuoc: boolean;
}

/** Kỳ chốt gần nhất, chưa chốt lần nào thì không có. */
export function kyGanNhat(duLieu: DuLieuChamCong): KyLuong | undefined {
  return duLieu.kyLuongs[duLieu.kyLuongs.length - 1];
}

/** Các kỳ đã chốt, kỳ mới nhất lên đầu — đúng thứ tự người ta muốn đọc. */
export function cacKyMoiTruoc(duLieu: DuLieuChamCong): KyLuong[] {
  return [...duLieu.kyLuongs].reverse();
}

/** Id của những bản ghi đã nằm trong một kỳ đã chốt, tức là đã được trả tiền. */
function idDaChot(duLieu: DuLieuChamCong): { buoiCong: Set<string>; ungTien: Set<string> } {
  const buoiCong = new Set<string>();
  const ungTien = new Set<string>();

  for (const ky of duLieu.kyLuongs) {
    for (const id of ky.buoiCongIds) {
      buoiCong.add(id);
    }
    for (const id of ky.ungTienIds) {
      ungTien.add(id);
    }
  }

  return { buoiCong, ungTien };
}

/** Buổi công và ứng tiền chưa được trả tiền — đây chính là nội dung của kỳ đang mở. */
export function banGhiChuaChot(duLieu: DuLieuChamCong): {
  buoiCongs: BuoiCong[];
  ungTiens: UngTien[];
} {
  const daChot = idDaChot(duLieu);
  return {
    buoiCongs: duLieu.buoiCongs.filter((b) => !daChot.buoiCong.has(b.id)),
    ungTiens: duLieu.ungTiens.filter((u) => !daChot.ungTien.has(u.id)),
  };
}

/**
 * Tiền kỳ trước còn thiếu của từng thợ. Chỉ lấy từ kỳ chốt gần nhất: mỗi lần chốt đã
 * cộng luôn nợ của kỳ trước đó vào rồi, nên không phải cộng dồn ngược lại từ đầu.
 */
export function noDauKy(duLieu: DuLieuChamCong): Map<string, number> {
  const no = new Map<string, number>();
  const ky = kyGanNhat(duLieu);
  if (!ky) {
    return no;
  }

  for (const dong of ky.dongs) {
    if (dong.chuyenKySau !== 0) {
      no.set(dong.thoId, dong.chuyenKySau);
    }
  }

  return no;
}

/**
 * Khoảng ngày của kỳ đang mở, chỉ để hiện lên màn hình cho dễ gọi tên.
 *
 * Đầu kỳ là ngày sau hôm chốt kỳ trước; nhưng nếu có buổi chấm bù rơi vào trước đó thì
 * lùi về đúng ngày sớm nhất, kẻo màn hình ghi một khoảng mà bên trong lại có ngày nằm
 * ngoài khoảng ấy.
 */
export function khoangKyHienTai(
  duLieu: DuLieuChamCong,
  homNay: string,
): { tuNgay: string; denNgay: string } {
  const { buoiCongs, ungTiens } = banGhiChuaChot(duLieu);
  const cacNgay = [...buoiCongs.map((b) => b.ngay), ...ungTiens.map((u) => u.ngay)];

  const ky = kyGanNhat(duLieu);
  const sauKyTruoc = ky ? congNgay(ky.denNgay, 1) : undefined;

  const somNhat = cacNgay.length > 0 ? cacNgay.reduce((a, b) => (a < b ? a : b)) : undefined;
  const muonNhat = cacNgay.length > 0 ? cacNgay.reduce((a, b) => (a > b ? a : b)) : undefined;

  const tuNgay =
    somNhat !== undefined && sauKyTruoc !== undefined
      ? somNhat < sauKyTruoc
        ? somNhat
        : sauKyTruoc
      : (somNhat ?? sauKyTruoc ?? homNay);

  // Chấm trước cho ngày mai thì cuối kỳ phải chạy tới ngày mai, không dừng ở hôm nay.
  const cuoi = muonNhat !== undefined && muonNhat > homNay ? muonNhat : homNay;

  // Chốt kỳ hôm nay thì kỳ mới bắt đầu từ mai, mà "mai" lại muộn hơn hôm nay — không kẹp
  // lại thì màn hình ghi ngược thành "06/08 → 05/08".
  return { tuNgay, denNgay: cuoi < tuNgay ? tuNgay : cuoi };
}

/** Bảng lương của kỳ đang mở: phần chưa trả, cộng nợ mang sang từ kỳ trước. */
export function kyHienTai(duLieu: DuLieuChamCong, homNay: string): KyDangMo {
  const { buoiCongs, ungTiens } = banGhiChuaChot(duLieu);
  const dongs = tinhTuBanGhi(duLieu, buoiCongs, ungTiens, noDauKy(duLieu));
  const { tuNgay, denNgay } = khoangKyHienTai(duLieu, homNay);

  return {
    tuNgay,
    denNgay,
    dongs,
    tongPhaiTra: dongs.reduce((tong, d) => tong + d.conLai, 0),
    chotDuoc: dongs.length > 0,
  };
}

/**
 * Số tiền mặc định điền sẵn lúc quyết toán: đúng bằng số còn phải trả, vì chín trên
 * mười lần là trả đủ. Thợ đang cầm dư tiền (số âm) thì mặc định trả 0 chứ không phải
 * đòi lại — đòi hay không là chuyện của người, không phải của máy.
 */
export function traDuKien(dong: DongLuong): number {
  return dong.conLai > 0 ? dong.conLai : 0;
}

export interface YeuCauQuyetToan {
  /** Ngày chốt sổ, thường là hôm nay. */
  denNgay: string;
  /** Số tiền thực đưa cho từng thợ. Thợ nào không ghi thì coi như trả đủ. */
  daTra?: Map<string, number>;
  ghiChu?: string;
  /** Lúc bấm nút, dạng ISO. Để trống thì lấy giờ máy. */
  chotLuc?: string;
}

/**
 * Chốt kỳ: ghi lại một bản chụp rồi khép những bản ghi trong kỳ lại.
 *
 * Không đụng vào `buoiCongs` và `ungTiens` — dữ liệu cũ còn nguyên, chỉ là từ giờ chúng
 * thuộc về một kỳ đã trả tiền nên bảng lương thôi không tính nữa.
 */
export function quyetToan(duLieu: DuLieuChamCong, yeuCau: YeuCauQuyetToan): DuLieuChamCong {
  const { denNgay, daTra = new Map<string, number>(), ghiChu = '', chotLuc } = yeuCau;

  const ky = kyHienTai(duLieu, denNgay);
  if (!ky.chotDuoc) {
    throw new Error('Kỳ này chưa có công nào, chưa có gì để quyết toán.');
  }

  const dongs: DongQuyetToan[] = ky.dongs.map((dong) => {
    const traThucTe = daTra.get(dong.tho.id) ?? traDuKien(dong);
    if (traThucTe < 0) {
      throw new Error('Số tiền trả không được là số âm.');
    }

    const traTron = Math.round(traThucTe);
    return {
      thoId: dong.tho.id,
      tenTho: dong.tho.ten,
      congSang: dong.congSang,
      congChieu: dong.congChieu,
      tongCong: dong.tongCong,
      tienCong: dong.tienCong,
      daUng: dong.daUng,
      noKyTruoc: dong.noKyTruoc,
      phaiTra: dong.conLai,
      daTra: traTron,
      chuyenKySau: dong.conLai - traTron,
    };
  });

  const { buoiCongs, ungTiens } = banGhiChuaChot(duLieu);
  const kyMoi: KyLuong = {
    id: taoId(),
    tuNgay: ky.tuNgay,
    denNgay,
    chotLuc: chotLuc ?? new Date().toISOString(),
    ghiChu: ghiChu.trim(),
    dongs,
    buoiCongIds: buoiCongs.map((b) => b.id),
    ungTienIds: ungTiens.map((u) => u.id),
  };

  return { ...duLieu, kyLuongs: [...duLieu.kyLuongs, kyMoi] };
}

/**
 * Gỡ kỳ vừa chốt, đưa mọi thứ trở lại kỳ đang mở. Dành cho lúc bấm nhầm hoặc ghi sai
 * số tiền — sửa được bằng đúng thao tác vừa rồi, không cần nhập tay lại buổi nào.
 *
 * Chỉ gỡ được kỳ mới nhất: gỡ một kỳ ở giữa thì nợ đầu kỳ của các kỳ sau nó thành sai.
 */
export function boChot(duLieu: DuLieuChamCong, kyId: string): DuLieuChamCong {
  const ky = kyGanNhat(duLieu);
  if (!ky) {
    throw new Error('Chưa quyết toán kỳ nào.');
  }
  if (ky.id !== kyId) {
    throw new Error('Chỉ bỏ chốt được kỳ mới nhất.');
  }

  return { ...duLieu, kyLuongs: duLieu.kyLuongs.slice(0, -1) };
}

/** Bản ghi thuộc về một kỳ đã chốt, dùng để mở lại chi tiết từng ngày của kỳ đó. */
export function banGhiCuaKy(
  duLieu: DuLieuChamCong,
  ky: KyLuong,
): { buoiCongs: BuoiCong[]; ungTiens: UngTien[] } {
  const buoiCong = new Set(ky.buoiCongIds);
  const ungTien = new Set(ky.ungTienIds);
  return {
    buoiCongs: duLieu.buoiCongs.filter((b) => buoiCong.has(b.id)),
    ungTiens: duLieu.ungTiens.filter((u) => ungTien.has(u.id)),
  };
}

/**
 * Chi tiết từng ngày của một thợ trong kỳ đang mở.
 *
 * Xem hẹp hơn cả kỳ cũng được — nhiều nhà trả một phần giữa chừng rồi mới chốt. Lúc ấy
 * *không* cộng nợ kỳ trước vào: món nợ ấy thuộc về cả kỳ chứ không thuộc riêng mấy ngày
 * đang xem, cộng vào thì con số dưới đáy không còn nghĩa gì.
 */
export function baoCaoKyHienTai(
  duLieu: DuLieuChamCong,
  thoId: string,
  homNay: string,
  tuNgay?: string,
  denNgay?: string,
): BaoCaoTho | null {
  const { buoiCongs, ungTiens } = banGhiChuaChot(duLieu);
  const caKy = khoangKyHienTai(duLieu, homNay);
  const tu = tuNgay ?? caKy.tuNgay;
  const den = denNgay ?? caKy.denNgay;
  const laCaKy = tu === caKy.tuNgay && den === caKy.denNgay;

  return baoCaoTuBanGhi(
    duLieu,
    thoId,
    buoiCongs.filter((b) => b.ngay >= tu && b.ngay <= den),
    ungTiens.filter((u) => u.ngay >= tu && u.ngay <= den),
    tu,
    den,
    homNay,
    laCaKy ? (noDauKy(duLieu).get(thoId) ?? 0) : 0,
  );
}

/**
 * Chi tiết từng ngày của một thợ trong một kỳ đã chốt. Cắt phần "nghỉ" ở đúng ngày chốt
 * kỳ chứ không ở hôm nay — kỳ đã đóng rồi, những ngày sau đó không thuộc về nó.
 */
export function baoCaoTrongKy(
  duLieu: DuLieuChamCong,
  ky: KyLuong,
  thoId: string,
  tuNgay?: string,
  denNgay?: string,
): BaoCaoTho | null {
  const { buoiCongs, ungTiens } = banGhiCuaKy(duLieu, ky);
  const tu = tuNgay ?? ky.tuNgay;
  const den = denNgay ?? ky.denNgay;
  const laCaKy = tu === ky.tuNgay && den === ky.denNgay;
  const dong = ky.dongs.find((d) => d.thoId === thoId);

  return baoCaoTuBanGhi(
    duLieu,
    thoId,
    buoiCongs.filter((b) => b.ngay >= tu && b.ngay <= den),
    ungTiens.filter((u) => u.ngay >= tu && u.ngay <= den),
    tu,
    den,
    ky.denNgay,
    laCaKy ? (dong?.noKyTruoc ?? 0) : 0,
  );
}

/** Cộng một cột của cả kỳ, dùng cho dòng tổng ở chân màn hình. */
export function tongCuaKy(ky: KyLuong): {
  tongCong: number;
  tienCong: number;
  daUng: number;
  daTra: number;
  chuyenKySau: number;
} {
  const cong = (lay: (dong: DongQuyetToan) => number) =>
    ky.dongs.reduce((tong, dong) => tong + lay(dong), 0);

  return {
    tongCong: cong((d) => d.tongCong),
    tienCong: cong((d) => d.tienCong),
    daUng: cong((d) => d.daUng),
    daTra: cong((d) => d.daTra),
    chuyenKySau: cong((d) => d.chuyenKySau),
  };
}
