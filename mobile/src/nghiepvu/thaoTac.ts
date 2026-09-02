/**
 * Thao tác trên dữ liệu chấm công. Mọi hàm đều trả về dữ liệu mới chứ không sửa tại chỗ,
 * để React biết là có thay đổi mà vẽ lại màn hình.
 */

import {
  BuoiCong,
  BuoiLam,
  CONG_MOT_BUOI,
  DuLieuChamCong,
  GhiChuNgay,
  MocLuong,
  Tho,
  UngTien,
} from './kieu';

/**
 * Tiền một công của thợ tại một ngày: lấy mốc lương gần nhất có hiệu lực trước hoặc
 * đúng ngày đó. Buổi công chấm trước cả mốc đầu tiên thì lấy chính mốc đầu tiên —
 * thà tính theo giá cũ nhất còn hơn tính thành 0 đồng.
 */
export function luongTaiNgay(tho: Tho, ngay: string): number {
  if (tho.mocLuong.length === 0) {
    return 0;
  }

  let ketQua = tho.mocLuong[0].tienMotCong;
  for (const moc of tho.mocLuong) {
    if (moc.tuNgay > ngay) {
      break;
    }
    ketQua = moc.tienMotCong;
  }

  return ketQua;
}

/** Tiền một công đang áp dụng hôm nay. */
export function luongHienTai(tho: Tho, homNay: string): number {
  return luongTaiNgay(tho, homNay);
}

/** Các mốc lương, mốc mới nhất lên đầu — đúng thứ tự người ta muốn đọc. */
export function lichSuLuong(tho: Tho): MocLuong[] {
  return [...tho.mocLuong].reverse();
}

/**
 * Đặt tiền công áp dụng từ một ngày. Đã có mốc đúng ngày đó thì sửa đè, chưa có thì
 * thêm mốc mới. Đây là cách tăng lương: mốc cũ giữ nguyên nên tháng trước không bị tính lại.
 */
export function datLuong(
  duLieu: DuLieuChamCong,
  thoId: string,
  tuNgay: string,
  tienMotCong: number,
): DuLieuChamCong {
  if (tienMotCong <= 0) {
    throw new Error('Tiền một công phải lớn hơn 0.');
  }

  const tho = timTho(duLieu, thoId);
  if (!tho) {
    throw new Error('Không có thợ này.');
  }

  const conLai = tho.mocLuong.filter((m) => m.tuNgay !== tuNgay);
  const mocLuong = [...conLai, { tuNgay, tienMotCong }].sort((a, b) =>
    a.tuNgay < b.tuNgay ? -1 : a.tuNgay > b.tuNgay ? 1 : 0,
  );

  return luuTho(duLieu, { ...tho, mocLuong });
}

/** Xoá một mốc lương đặt nhầm. Không cho xoá mốc cuối cùng — thợ phải còn một giá. */
export function xoaMocLuong(
  duLieu: DuLieuChamCong,
  thoId: string,
  tuNgay: string,
): DuLieuChamCong {
  const tho = timTho(duLieu, thoId);
  if (!tho) {
    throw new Error('Không có thợ này.');
  }

  const mocLuong = tho.mocLuong.filter((m) => m.tuNgay !== tuNgay);
  if (mocLuong.length === 0) {
    throw new Error('Thợ phải còn ít nhất một mốc tiền công.');
  }

  return luuTho(duLieu, { ...tho, mocLuong });
}

export function taoId(): string {
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

function bayGio(): string {
  return new Date().toISOString();
}

/** Xếp tên theo kiểu tiếng Việt (à, ă, â... đúng chỗ). */
function soSanhTen(a: string, b: string): number {
  return a.localeCompare(b, 'vi', { sensitivity: 'base' });
}

/** Thợ đang còn làm — đây là danh sách của màn hình chấm công. */
export function thoDangLam(duLieu: DuLieuChamCong): Tho[] {
  return duLieu.thos.filter((t) => t.dangLam).sort((a, b) => soSanhTen(a.ten, b.ten));
}

/**
 * Tất cả thợ, người đang làm xếp trước rồi mới tới người đã nghỉ.
 * Không có hàm xoá thợ: xoá là mất luôn bảng lương các tháng trước, nghỉ việc thì tắt dangLam.
 */
export function tatCaTho(duLieu: DuLieuChamCong): Tho[] {
  return [...duLieu.thos].sort((a, b) => {
    if (a.dangLam !== b.dangLam) {
      return a.dangLam ? -1 : 1;
    }
    return soSanhTen(a.ten, b.ten);
  });
}

export function timTho(duLieu: DuLieuChamCong, thoId: string): Tho | undefined {
  return duLieu.thos.find((t) => t.id === thoId);
}

/**
 * Thêm thợ.
 *
 * `id` nhận từ ngoài được, và chỉ dùng cho đúng một việc: máy của thợ tự chấm phải tạo
 * bản ghi thợ mang **đúng id do máy chủ đặt** (nhận qua mã mời). Hai máy đặt id khác nhau
 * thì lúc đối chiếu không ghép được ai với ai. Bình thường cứ để trống cho nó tự sinh.
 */
export function themTho(
  duLieu: DuLieuChamCong,
  ten: string,
  tienMotCong: number,
  ngayTao: string,
  id: string = taoId(),
): { duLieu: DuLieuChamCong; tho: Tho } {
  const tho: Tho = {
    id,
    ten: ten.trim(),
    dienThoai: '',
    // Mốc lương đầu tiên tính từ ngày thêm thợ, không phải từ hôm nay — nhập thợ cũ
    // vào sau vẫn tính đúng các buổi công trước đó.
    mocLuong: [{ tuNgay: ngayTao, tienMotCong }],
    dangLam: true,
    ghiChu: '',
    ngayTao,
    suaLuc: bayGio(),
  };

  return { duLieu: { ...duLieu, thos: [...duLieu.thos, tho] }, tho };
}

/**
 * Đổi id của một thợ, kéo theo mọi bản ghi móc vào id ấy.
 *
 * Dùng cho đúng một việc: máy thợ **tự chấm trước khi có mã mời**. Lúc ấy id là do máy tự
 * đặt, chưa có trong sổ chủ; tới khi dán được mã, database mới trả về id thật. Không đổi thì
 * mấy buổi chấm hồi chưa nối treo lại ở id cũ — thợ mở app lên thấy sổ trống trơn, mà đối
 * chiếu thì báo chủ chấm khống.
 *
 * Chỉ đúng khi **một bên không có gì trùng bên kia** — trên máy thợ thì đúng, vì cả máy chỉ
 * có một người chấm. Gộp hai người thật vào một id là chuyện khác, đừng dùng hàm này.
 */
export function doiThoId(duLieu: DuLieuChamCong, cu: string, moi: string): DuLieuChamCong {
  if (cu === moi) {
    return duLieu;
  }

  // Id mới đã có bản ghi thợ rồi (chủ gửi sổ xuống trước) thì bỏ bản ghi tạm, giữ bản thật.
  const daCoMoi = duLieu.thos.some((tho) => tho.id === moi);

  return {
    ...duLieu,
    thos: daCoMoi
      ? duLieu.thos.filter((tho) => tho.id !== cu)
      : duLieu.thos.map((tho) => (tho.id === cu ? { ...tho, id: moi, suaLuc: bayGio() } : tho)),
    buoiCongs: duLieu.buoiCongs.map((b) => (b.thoId === cu ? { ...b, thoId: moi } : b)),
    ungTiens: duLieu.ungTiens.map((u) => (u.thoId === cu ? { ...u, thoId: moi } : u)),
    ghiChuNgays: duLieu.ghiChuNgays.map((g) => (g.thoId === cu ? { ...g, thoId: moi } : g)),
    // Kỳ đã chốt là bản chụp của quá khứ, nhưng id trong đó cũng phải trỏ đúng người.
    kyLuongs: duLieu.kyLuongs.map((ky) => ({
      ...ky,
      dongs: ky.dongs.map((dong) => (dong.thoId === cu ? { ...dong, thoId: moi } : dong)),
    })),
  };
}

/** Ghi lại thợ sau khi sửa tên, tiền công hay đánh dấu đã nghỉ. */
export function luuTho(duLieu: DuLieuChamCong, tho: Tho): DuLieuChamCong {
  const daSua: Tho = { ...tho, ten: tho.ten.trim(), suaLuc: bayGio() };
  return {
    ...duLieu,
    thos: duLieu.thos.map((t) => (t.id === daSua.id ? daSua : t)),
  };
}

/** Buổi công đã chấm của một thợ trong một buổi, chưa chấm thì trả về undefined. */
export function dangCham(
  duLieu: DuLieuChamCong,
  thoId: string,
  ngay: string,
  buoi: BuoiLam,
): BuoiCong | undefined {
  return duLieu.buoiCongs.find((b) => b.thoId === thoId && b.ngay === ngay && b.buoi === buoi);
}

/**
 * Chấm một buổi cho thợ. Chấm lại buổi đã chấm thì sửa số công chứ không thêm dòng mới —
 * bấm nhầm hai lần không thành hai công.
 * Tiền một công được chụp lại theo giá hiện tại của thợ.
 */
export function cham(
  duLieu: DuLieuChamCong,
  thoId: string,
  ngay: string,
  buoi: BuoiLam,
  soCong = CONG_MOT_BUOI,
  ghiChu = '',
): DuLieuChamCong {
  if (soCong <= 0) {
    throw new Error('Số công phải lớn hơn 0. Muốn bỏ chấm thì dùng boCham.');
  }

  const tho = timTho(duLieu, thoId);
  if (!tho) {
    throw new Error('Không có thợ này.');
  }

  const cu = dangCham(duLieu, thoId, ngay, buoi);
  const moi: BuoiCong = {
    id: cu?.id ?? taoId(),
    thoId,
    ngay,
    buoi,
    soCong,
    // Không chụp giá vào đây nữa: giá lấy từ mốc lương của thợ tại ngày đó. Nhờ vậy
    // sửa lại mốc lương (tăng lương tính từ đầu tháng chẳng hạn) là cả tháng tự tính lại,
    // không phải sửa tay từng buổi. Giữ lại giá cũ nếu buổi này vốn có giá riêng.
    tienMotCong: cu?.tienMotCong ?? null,
    ghiChu,
    suaLuc: bayGio(),
  };

  return {
    ...duLieu,
    buoiCongs: cu
      ? duLieu.buoiCongs.map((b) => (b.id === cu.id ? moi : b))
      : [...duLieu.buoiCongs, moi],
  };
}

/** Bỏ chấm một buổi. Buổi vốn chưa chấm thì dữ liệu giữ nguyên. */
export function boCham(
  duLieu: DuLieuChamCong,
  thoId: string,
  ngay: string,
  buoi: BuoiLam,
): DuLieuChamCong {
  return {
    ...duLieu,
    buoiCongs: duLieu.buoiCongs.filter(
      (b) => !(b.thoId === thoId && b.ngay === ngay && b.buoi === buoi),
    ),
  };
}

/** Đặt số công cho một buổi. soCong là null nghĩa là cho nghỉ buổi đó. */
export function datCong(
  duLieu: DuLieuChamCong,
  thoId: string,
  ngay: string,
  buoi: BuoiLam,
  soCong: number | null,
): DuLieuChamCong {
  return soCong === null
    ? boCham(duLieu, thoId, ngay, buoi)
    : cham(duLieu, thoId, ngay, buoi, soCong);
}

/**
 * Ghi chú của một thợ trong một ngày. Chưa ghi gì thì trả về chuỗi rỗng — chỗ gọi chỉ cần
 * hỏi "có chữ hay không", không phải phân biệt thêm cái `undefined`.
 */
export function ghiChuNgay(duLieu: DuLieuChamCong, thoId: string, ngay: string): string {
  return duLieu.ghiChuNgays.find((g) => g.thoId === thoId && g.ngay === ngay)?.noiDung ?? '';
}

/**
 * Đặt ghi chú cho một ngày của một thợ. Gõ đè lên ghi chú cũ chứ không thêm dòng mới —
 * mỗi (thợ, ngày) chỉ có một ghi chú.
 *
 * **Xoá hết chữ là xoá bản ghi**, không giữ lại một bản ghi rỗng: sổ mang theo mấy trăm
 * bản ghi trống thì file sao lưu phình ra mà không nói thêm điều gì, và `soTrong` sẽ tưởng
 * sổ này đã có dữ liệu.
 *
 * Không móc vào buổi công: ngày thợ nghỉ hẳn vẫn ghi chú được, và bỏ chấm một buổi không
 * làm mất chữ người ta đã gõ. Xem [GhiChuNgay](./kieu.ts).
 */
export function datGhiChuNgay(
  duLieu: DuLieuChamCong,
  thoId: string,
  ngay: string,
  noiDung: string,
): DuLieuChamCong {
  const chu = noiDung.trim();
  const conLai = duLieu.ghiChuNgays.filter((g) => !(g.thoId === thoId && g.ngay === ngay));

  if (chu === '') {
    return { ...duLieu, ghiChuNgays: conLai };
  }

  const moi: GhiChuNgay = { thoId, ngay, noiDung: chu, suaLuc: bayGio() };
  return { ...duLieu, ghiChuNgays: [...conLai, moi] };
}

export function themUng(
  duLieu: DuLieuChamCong,
  thoId: string,
  ngay: string,
  soTien: number,
  ghiChu = '',
): DuLieuChamCong {
  if (soTien <= 0) {
    throw new Error('Số tiền ứng phải lớn hơn 0.');
  }

  const ung: UngTien = {
    id: taoId(),
    thoId,
    ngay,
    soTien,
    ghiChu: ghiChu.trim(),
    suaLuc: bayGio(),
  };

  return { ...duLieu, ungTiens: [...duLieu.ungTiens, ung] };
}

/**
 * Lần ứng này đã nằm trong một kỳ đã chốt hay chưa.
 *
 * Kỳ nhớ theo id (`KyLuong.ungTienIds`) chứ không theo ngày — xem [ky.ts](./ky.ts) — nên
 * hỏi được chính xác từng lần ứng một.
 */
function ungDaChot(duLieu: DuLieuChamCong, ungId: string): boolean {
  return duLieu.kyLuongs.some((ky) => (ky.ungTienIds ?? []).includes(ungId));
}

/**
 * Sửa lại một lần ứng đã ghi: gõ nhầm số tiền, ghi chú sai, hay ghi muộn mấy hôm nên
 * ngày bị lệch (lúc thêm, ứng luôn lấy ngày hôm nay).
 *
 * **Chặn sửa lần ứng đã nằm trong kỳ đã chốt.** `KyLuong.dongs` là bản chụp của một lần
 * đã đếm tiền trao tay, không tính lại bao giờ nữa; sửa số tiền ứng bây giờ chỉ làm sổ
 * nói khác tờ quyết toán thợ đang cầm, mà tiền thì đã trao rồi. Sửa thật thì bỏ chốt kỳ
 * ấy đã — `boChot` gỡ lại được, không mất buổi công nào.
 */
export function suaUng(
  duLieu: DuLieuChamCong,
  ungId: string,
  ngay: string,
  soTien: number,
  ghiChu = '',
): DuLieuChamCong {
  if (soTien <= 0) {
    throw new Error('Số tiền ứng phải lớn hơn 0.');
  }
  if (ungDaChot(duLieu, ungId)) {
    throw new Error('Lần ứng này đã nằm trong kỳ đã chốt, bỏ chốt kỳ ấy rồi mới sửa được.');
  }

  return {
    ...duLieu,
    ungTiens: duLieu.ungTiens.map((u) =>
      u.id === ungId ? { ...u, ngay, soTien, ghiChu: ghiChu.trim(), suaLuc: bayGio() } : u,
    ),
  };
}

/**
 * Xoá hẳn một lần ứng — ghi nhầm sang thợ khác, hay ghi hai lần cùng một lần đưa tiền.
 *
 * Xoá hẳn chứ không đánh dấu đã huỷ: dòng ứng là chuyện tiền nong giữa hai người, để lại
 * một dòng gạch ngang trong sổ chỉ tổ làm người xem phân vân nó có được trừ hay không.
 * Chặn xoá lần ứng đã chốt, cùng một lý do với `suaUng`.
 */
export function xoaUng(duLieu: DuLieuChamCong, ungId: string): DuLieuChamCong {
  if (ungDaChot(duLieu, ungId)) {
    throw new Error('Lần ứng này đã nằm trong kỳ đã chốt, bỏ chốt kỳ ấy rồi mới xoá được.');
  }

  return { ...duLieu, ungTiens: duLieu.ungTiens.filter((u) => u.id !== ungId) };
}

/**
 * Đếm những gì đang treo vào một thợ, để hỏi lại cho rõ trước khi xoá.
 *
 * Đếm ở đây chứ không để màn hình tự lọc: câu hỏi *"xoá là mất những gì"* và việc xoá thật
 * phải nhìn cùng một tập bản ghi, tách ra hai chỗ thì sớm muộn câu hỏi nói một đằng mà
 * `xoaTho` làm một nẻo.
 */
export interface DemCuaTho {
  soBuoiCong: number;
  soUngTien: number;
  soGhiChu: number;
  /**
   * Thợ đã có tên trong một kỳ đã chốt — không xoá được nữa, chỉ cho nghỉ.
   * Xem `xoaTho`.
   */
  daChot: boolean;
}

export function demCuaTho(duLieu: DuLieuChamCong, thoId: string): DemCuaTho {
  const buoiCongs = duLieu.buoiCongs.filter((b) => b.thoId === thoId);
  const ungTiens = duLieu.ungTiens.filter((u) => u.thoId === thoId);

  // Hỏi cả ba đường: tên trong bản chụp, buổi công đã trả tiền, lần ứng đã trừ. Bình thường
  // cả ba cùng đúng hoặc cùng sai, nhưng chỉ cần một cái dính là kỳ ấy có nhắc tới người này.
  const daChot = duLieu.kyLuongs.some(
    (ky) =>
      ky.dongs.some((dong) => dong.thoId === thoId) ||
      buoiCongs.some((b) => (ky.buoiCongIds ?? []).includes(b.id)) ||
      ungTiens.some((u) => (ky.ungTienIds ?? []).includes(u.id)),
  );

  return {
    soBuoiCong: buoiCongs.length,
    soUngTien: ungTiens.length,
    soGhiChu: duLieu.ghiChuNgays.filter((g) => g.thoId === thoId).length,
    daChot,
  };
}

/**
 * Xoá hẳn một thợ, kéo theo mọi buổi công, lần ứng và ghi chú ngày của người ấy.
 *
 * **Chỉ dùng cho người gõ nhầm hoặc gõ trùng.** Thợ nghỉ việc thì tắt `Tho.dangLam` —
 * xoá là mất luôn phần sổ đã đi làm, mà thợ nghỉ rồi vẫn phải tra lại được tháng trước
 * cầm về bao nhiêu.
 *
 * **Chặn xoá thợ đã có tên trong kỳ đã chốt.** Không phải để giữ cho đẹp: tờ quyết toán cũ
 * chụp sẵn tên và số tiền nên vẫn đọc được, nhưng bấm vào dòng ấy để mở chi tiết từng ngày
 * thì `baoCaoTuBanGhi` tìm thợ không ra và trả về null — màn hình mở ra trắng trơn, không
 * báo gì. Một chứng từ đã trả tiền mà bấm vào không ra gì là hỏng nặng hơn hẳn cái tiện
 * của việc xoá được một cái tên.
 */
export function xoaTho(duLieu: DuLieuChamCong, thoId: string): DuLieuChamCong {
  if (demCuaTho(duLieu, thoId).daChot) {
    throw new Error('Thợ đã có tên trong kỳ đã chốt, chỉ cho nghỉ được chứ không xoá được.');
  }

  return {
    ...duLieu,
    thos: duLieu.thos.filter((t) => t.id !== thoId),
    buoiCongs: duLieu.buoiCongs.filter((b) => b.thoId !== thoId),
    ungTiens: duLieu.ungTiens.filter((u) => u.thoId !== thoId),
    ghiChuNgays: duLieu.ghiChuNgays.filter((g) => g.thoId !== thoId),
  };
}
