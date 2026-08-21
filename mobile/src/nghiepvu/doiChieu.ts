/**
 * Đối chiếu sổ của mình với sổ bên kia gửi sang.
 *
 * Đây là chỗ **không tự trộn gì cả**. Mỗi máy giữ sổ của mình làm sổ thật; sổ bên kia chỉ
 * là bản chụp để đọc. Hàm ở đây chỉ ra chỗ hai bên nói khác nhau, còn sửa hay không thì
 * người dùng bấm từng dòng. Nếu để dữ liệu bên kia tự chảy vào sổ mình thì thợ tự thêm
 * công cho mình được, và bảng lương của chủ đổi số mà chủ không hề biết.
 *
 * Thuần tính toán, không chạm bộ nhớ máy lẫn mạng — nhờ vậy kiểm thử được hết các nước.
 */

import { BuoiLam, DuLieuChamCong } from './kieu';
import { DongCong, SoCong } from './soCong';
import { boCham, cham } from './thaoTac';

export type LoaiLech =
  /** Sổ mình có chấm, sổ bên kia không. */
  | 'chiMinhCo'
  /** Sổ bên kia có chấm, sổ mình không. */
  | 'chiBenKiaCo'
  /** Hai bên đều có nhưng số công khác nhau. */
  | 'lechSoCong'
  /**
   * Sổ bên kia có chấm, mà **máy mình không biết ngày ấy** — ngoài khoảng mình khai là đầy
   * đủ, xem `coYKien`. Chưa phải lệch: mình chưa nói gì thì chưa trái ý ai. Nhưng phải hiện
   * ra, vì đây là công bên kia đã ghi thật mà sổ mình đang trống — lấy về là chấm bù.
   */
  | 'minhChuaBiet'
  /**
   * Sổ mình có chấm, mà **bên kia không biết ngày ấy**. Cũng chưa phải lệch, và tuyệt đối
   * không được "lấy theo bên kia": im lặng của người không biết không phải là lời nói "hôm
   * ấy nghỉ", lấy theo là xoá mất công thật.
   */
  | 'benKiaChuaBiet';

export interface DongLech {
  ngay: string;
  buoi: BuoiLam;
  soCongMinh: number | null;
  soCongBenKia: number | null;
  loai: LoaiLech;
  /**
   * Buổi này đã nằm trong kỳ đã quyết toán (theo sổ nào có cờ ấy). Vẫn hiện lên để hai
   * bên biết là có lệch, nhưng không cho sửa — tiền trả rồi.
   */
  daChot: boolean;
}

export interface KetQuaDoiChieu {
  /**
   * Khoảng thật sự so được: phần giao của hai khoảng khai, nới ra cho phủ hết những buổi
   * đã so được ngoài khoảng ấy (dòng chấm bù — xem `coYKien`). Nới vì đây là câu đầu trang,
   * mà bên dưới hiện một dòng nằm ngoài khoảng đầu trang ghi là hai câu trái nhau.
   */
  tuNgay: string;
  denNgay: string;
  /**
   * Không so được buổi nào, mà hai khoảng khai cũng không giao nhau. Chưa kết luận được gì —
   * thường là máy thợ mới cài, hoặc sổ bên kia gửi từ lâu quá.
   */
  khongTrungKhoang: boolean;
  /** Số buổi hai bên khớp nhau, để nói được câu "khớp 42 buổi, lệch 3 buổi". */
  soKhop: number;
  lechs: DongLech[];
  /**
   * Những buổi **một bên có chấm mà bên kia không biết ngày ấy**: chưa kết luận được ai
   * đúng, nhưng cũng không được bỏ đi lặng lẽ.
   *
   * Đây là chỗ trước đây bỏ hẳn, và bỏ về đúng phía tệ nhất. Máy thợ khai từ hôm nó nhận vai,
   * nên mấy buổi chủ đã chấm *trước* hôm ấy biến mất khỏi màn hình đối chiếu của thợ: chủ chấm
   * ngày 17 với 18, thợ chấm 18 với 19, mà đầu trang lại đọc thành "sổ tôi 4 công, sổ chủ 2
   * công" — hoá ra chủ chấm thiếu. Rồi thợ chấm bù đúng ngày 17 là buổi kia hiện ra, hai tổng
   * nhảy thành 6 với 4: cùng một sổ chủ mà đọc ra hai con số khác nhau tuỳ theo sổ mình có gì.
   *
   * Không cộng vào hai tổng — giống buổi tạm gác: tổng là để so hai bên trên **cùng** một
   * khoảng, mà mấy buổi này thì chỉ một bên có khoảng. Cộng riêng bằng `tongChuaBiet`.
   */
  chuaBiets: DongLech[];
  tongCongMinh: number;
  tongCongBenKia: number;
  /**
   * Số buổi **của hôm nay** tạm gác lại: một bên đã chấm, bên kia chưa. Không tính là lệch,
   * cũng không cộng vào hai tổng — xem ghi chú ở `doiChieu`.
   */
  soTamGac: number;
}

/** Sáng đứng trước Chiều, đúng thứ tự người ta đọc một ngày. */
const THU_TU_BUOI: Record<BuoiLam, number> = { Sang: 0, Chieu: 1 };

function khoa(ngay: string, buoi: BuoiLam): string {
  return `${ngay}|${buoi}`;
}

/** Một dòng để hiện lên, ghép từ hai bên. */
function dongLech(
  goc: DongCong,
  a: DongCong | undefined,
  b: DongCong | undefined,
  loai: LoaiLech,
): DongLech {
  return {
    ngay: goc.ngay,
    buoi: goc.buoi,
    soCongMinh: a ? a.soCong : null,
    soCongBenKia: b ? b.soCong : null,
    loai,
    // Bên nào khai đã chốt cũng tính là đã chốt: chỉ máy chủ có cờ này, mà chủ mới là bên
    // trả tiền.
    daChot: a?.daChot === true || b?.daChot === true,
  };
}

/** Xếp theo ngày, trong một ngày thì Sáng trước Chiều — thứ tự người ta đọc một cuốn sổ. */
function xepTheoNgay(dongs: DongLech[]): DongLech[] {
  return [...dongs].sort((x, y) =>
    x.ngay === y.ngay
      ? THU_TU_BUOI[x.buoi] - THU_TU_BUOI[y.buoi]
      : x.ngay.localeCompare(y.ngay),
  );
}

function theoKhoa(dongs: DongCong[]): Map<string, DongCong> {
  const bang = new Map<string, DongCong>();
  for (const dong of dongs) {
    bang.set(khoa(dong.ngay, dong.buoi), dong);
  }
  return bang;
}

/**
 * Sổ này có ý kiến gì về buổi ấy hay không.
 *
 * Có dòng thì hiển nhiên là có. Không có dòng thì chỉ tính là *"nói không có công"* khi ngày
 * ấy nằm trong khoảng sổ khai là đầy đủ; ngoài khoảng ấy là **không biết**, mà không biết thì
 * không phải một lời trái ý bên kia.
 *
 * Đây là chỗ trước đây làm sai. Cũ chỉ so trong phần giao hai khoảng, nên một dòng chấm bù
 * nằm ngoài khoảng khai thì cả buổi ấy biến mất khỏi đối chiếu — và để chữa việc mất ấy, máy
 * thợ phải nới mốc khai xuống tận buổi chấm bù, tức là khai bừa "mấy ngày trống giữa đó tôi
 * nghỉ". Tách ra thành hai câu hỏi riêng thì cả hai việc đều đúng: dòng nào có là được so, còn
 * ngày trống thì chỉ bên nào dám khai mới bị tính.
 */
function coYKien(so: SoCong, dong: DongCong | undefined, ngay: string): boolean {
  return dong !== undefined || (ngay >= so.tuNgay && ngay <= so.denNgay);
}

/**
 * So hai sổ, trả về những buổi nói khác nhau.
 *
 * Chỉ **kết luận là lệch** ở những buổi mà cả hai bên đều có ý kiến — xem `coYKien`. Bên nào
 * không khai ngày ấy là đầy đủ và cũng không có dòng nào thì buổi ấy không vào `lechs`: thiếu
 * một buổi ở đó không có nghĩa là ai sai, xem ghi chú ở `SoCong.tuNgay`.
 *
 * Nhưng **không bỏ đi**: bên kia có chấm thật, nên buổi ấy sang `chuaBiets` để vẫn hiện lên.
 * Bỏ hẳn là công của bên kia biến mất khỏi màn hình đối chiếu, mà lại biến mất một chiều —
 * xem ghi chú ở `chuaBiets`.
 *
 * `homNay` để **tạm gác những buổi của hôm nay mà chỉ một bên có**. Ngày đang chạy thì bên
 * chưa chấm không có nghĩa là bên ấy nói "nghỉ": chủ chấm cả nhóm lúc nghỉ trưa, thợ mở app
 * lúc về nhà, mà cùng một buổi ấy hai người ghi cách nhau mấy tiếng. Đếm luôn thì máy thợ vừa
 * cài xong, chưa chấm ô nào, mở đối chiếu ra đã thấy hai dòng đỏ của đúng hôm nay — người
 * dùng không nhập gì mà app báo lệch, đó là chỗ mất lòng tin đầu tiên. Cùng một lẽ với
 * `ngayNghiTrongSo`: ngày chưa qua thì chưa kết luận.
 *
 * Vẫn báo lệch nếu **cả hai bên đều đã chấm** buổi hôm nay mà số công khác nhau — chỗ ấy hai
 * người thật sự nói khác nhau, gác lại là che mất.
 *
 * Buổi tạm gác cũng **không cộng vào hai tổng**: tổng phải nói đúng những dòng đang hiện bên
 * dưới, chứ không thì đầu trang bảo lệch 2 công mà không có dòng nào giải thích. Buổi trong
 * `chuaBiets` cũng vậy — có tổng riêng của nó, và màn hình nói riêng một câu.
 */
export function doiChieu(soMinh: SoCong, soBenKia: SoCong, homNay: string): KetQuaDoiChieu {
  const giaoTu = soMinh.tuNgay > soBenKia.tuNgay ? soMinh.tuNgay : soBenKia.tuNgay;
  const giaoDen = soMinh.denNgay < soBenKia.denNgay ? soMinh.denNgay : soBenKia.denNgay;

  const minh = theoKhoa(soMinh.dongs);
  const benKia = theoKhoa(soBenKia.dongs);

  const lechs: DongLech[] = [];
  const chuaBiets: DongLech[] = [];
  let soKhop = 0;
  let soTamGac = 0;
  let tongCongMinh = 0;
  let tongCongBenKia = 0;
  /** Ngày sớm nhất / muộn nhất thật sự so được, để đầu trang nói đúng khoảng đã so. */
  let soTu: string | null = null;
  let soDen: string | null = null;

  for (const k of new Set([...minh.keys(), ...benKia.keys()])) {
    const a = minh.get(k);
    const b = benKia.get(k);

    const goc = a ?? b;
    if (!goc) {
      continue;
    }

    /*
      Hôm nay mà chỉ một bên có: gác lại cả khỏi tổng, xem ghi chú ở đầu hàm. Xét **trước**
      chuyện bên kia có biết ngày ấy hay không: buổi của hôm nay thì bên chưa chấm chưa nói
      gì cả, gọi nó là "ngày bên kia chưa biết" chỉ thêm một dòng không ai cần đọc, mà câu
      "hôm nay còn dở" mới là câu đúng.
    */
    if (goc.ngay >= homNay && (!a || !b)) {
      soTamGac += 1;
      continue;
    }

    /*
      Một bên không biết ngày ấy thì không có chuyện hai bên nói khác nhau — nhưng bên kia có
      chấm thật, nên để riêng một chỗ chứ không bỏ. Vào được tới đây thì đúng một bên có dòng:
      bên nào có dòng là bên ấy có ý kiến, nên bên không có ý kiến chính là bên không có dòng.
    */
    if (!coYKien(soMinh, a, goc.ngay)) {
      chuaBiets.push(dongLech(goc, a, b, 'minhChuaBiet'));
      continue;
    }
    if (!coYKien(soBenKia, b, goc.ngay)) {
      chuaBiets.push(dongLech(goc, a, b, 'benKiaChuaBiet'));
      continue;
    }

    if (soTu === null || goc.ngay < soTu) {
      soTu = goc.ngay;
    }
    if (soDen === null || goc.ngay > soDen) {
      soDen = goc.ngay;
    }

    tongCongMinh += a ? a.soCong : 0;
    tongCongBenKia += b ? b.soCong : 0;

    if (a && b && a.soCong === b.soCong) {
      soKhop += 1;
      continue;
    }

    lechs.push(dongLech(goc, a, b, !b ? 'chiMinhCo' : !a ? 'chiBenKiaCo' : 'lechSoCong'));
  }

  /*
    Chưa so được buổi nào — kể cả buổi tạm gác — thì nói thẳng là hai sổ chưa có ngày nào
    chung. Xét theo *kết quả* chứ theo hai mốc khai: khoảng giao có thể rộng mà vẫn không
    buổi nào được chấm, mà cũng có thể hẹp tới mức rỗng trong lúc một buổi chấm bù ngoài
    khoảng vẫn so được.

    Buổi trong `chuaBiets` cũng **không tính là so được** — mới một bên có ý kiến — nhưng vẫn
    trả về, vì đây đúng là lúc chúng cần được nhìn nhất: máy thợ vừa cài, chưa so được gì, mà
    sổ chủ thì đã có mấy hôm công.
  */
  if (soTu === null && soTamGac === 0) {
    return {
      tuNgay: giaoTu,
      denNgay: giaoDen,
      khongTrungKhoang: giaoTu > giaoDen,
      soKhop: 0,
      lechs: [],
      chuaBiets: xepTheoNgay(chuaBiets),
      tongCongMinh: 0,
      tongCongBenKia: 0,
      soTamGac: 0,
    };
  }

  return {
    // Nới khoảng nói ở đầu trang ra cho phủ hết những buổi thật sự đã so: một dòng chấm bù
    // ngoài khoảng khai vẫn hiện bên dưới, mà đầu trang lại ghi khoảng không chứa nó thì
    // người dùng đọc thành hai điều trái nhau.
    tuNgay: soTu !== null && soTu < giaoTu ? soTu : giaoTu,
    denNgay: soDen !== null && soDen > giaoDen ? soDen : giaoDen,
    khongTrungKhoang: false,
    soKhop,
    lechs: xepTheoNgay(lechs),
    chuaBiets: xepTheoNgay(chuaBiets),
    tongCongMinh,
    tongCongBenKia,
    soTamGac,
  };
}

/**
 * Cộng riêng những buổi chưa kết luận được, để đầu trang nói được câu "sổ chủ còn 2 công ở
 * những ngày máy tôi chưa biết". Hai tổng chính không có mấy công này — xem `chuaBiets`.
 *
 * Mỗi dòng chỉ có đúng một bên có số, nên hai tổng này không bao giờ cùng lớn hơn 0 trên cùng
 * một dòng; cộng cả hai vẫn cần vì hai chiều đều xảy ra được trong cùng một lần đối chiếu.
 */
export function tongChuaBiet(chuaBiets: DongLech[]): { minh: number; benKia: number } {
  let minh = 0;
  let benKia = 0;
  for (const dong of chuaBiets) {
    minh += dong.soCongMinh ?? 0;
    benKia += dong.soCongBenKia ?? 0;
  }
  return { minh, benKia };
}

/** Buổi đã quyết toán thì khoá, không cho sửa theo sổ bên kia nữa. */
export class DaChotKhongSuaDuoc extends Error {
  constructor() {
    super('Buổi này đã nằm trong kỳ đã quyết toán, không sửa được nữa.');
  }
}

/** Bên kia chưa biết ngày ấy thì im lặng của họ không phải là lời "hôm ấy nghỉ". */
export class ChuaBietKhongLayDuoc extends Error {
  constructor() {
    super('Sổ bên kia chưa tới ngày này nên chưa biết — không lấy theo được.');
  }
}

/**
 * Lấy theo sổ bên kia cho **một** dòng lệch: ghi vào sổ của chính máy này.
 *
 * Một dòng một lần, không có nút "lấy hết": người bấm phải nhìn từng buổi. Chỗ này là chỗ
 * tiền ra tiền vào, một nút lấy hết là mời người ta bấm cho xong việc.
 *
 * Dòng `benKiaChuaBiet` thì **không lấy được**, và chặn ngay ở đây chứ không chỉ ẩn nút trên
 * màn hình: bên kia không biết ngày ấy nên sổ họ trống, "lấy theo bên kia" hoá ra là xoá một
 * buổi công thật của mình theo lời một người chưa nói gì.
 */
export function layTheoBenKia(
  duLieu: DuLieuChamCong,
  thoId: string,
  lech: DongLech,
): DuLieuChamCong {
  if (lech.daChot) {
    throw new DaChotKhongSuaDuoc();
  }

  if (lech.loai === 'benKiaChuaBiet') {
    throw new ChuaBietKhongLayDuoc();
  }

  if (lech.soCongBenKia === null) {
    return boCham(duLieu, thoId, lech.ngay, lech.buoi);
  }

  return cham(duLieu, thoId, lech.ngay, lech.buoi, lech.soCongBenKia);
}
