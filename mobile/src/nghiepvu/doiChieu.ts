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
  | 'lechSoCong';

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
 * Chỉ kết luận ở những buổi mà **cả hai bên đều có ý kiến** — xem `coYKien`. Bên nào không
 * khai ngày ấy là đầy đủ và cũng không có dòng nào thì buổi ấy bỏ qua: thiếu một buổi ở đó
 * không có nghĩa là ai sai, xem ghi chú ở `SoCong.tuNgay`.
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
 * dưới, chứ không thì đầu trang bảo lệch 2 công mà không có dòng nào giải thích. Buổi bỏ qua
 * vì một bên không biết cũng vậy.
 */
export function doiChieu(soMinh: SoCong, soBenKia: SoCong, homNay: string): KetQuaDoiChieu {
  const giaoTu = soMinh.tuNgay > soBenKia.tuNgay ? soMinh.tuNgay : soBenKia.tuNgay;
  const giaoDen = soMinh.denNgay < soBenKia.denNgay ? soMinh.denNgay : soBenKia.denNgay;

  const minh = theoKhoa(soMinh.dongs);
  const benKia = theoKhoa(soBenKia.dongs);

  const lechs: DongLech[] = [];
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

    // Một bên không biết ngày ấy thì không có chuyện hai bên nói khác nhau.
    if (!coYKien(soMinh, a, goc.ngay) || !coYKien(soBenKia, b, goc.ngay)) {
      continue;
    }

    // Hôm nay mà chỉ một bên có: gác lại cả khỏi tổng, xem ghi chú ở đầu hàm.
    if (goc.ngay >= homNay && (!a || !b)) {
      soTamGac += 1;
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

    lechs.push({
      ngay: goc.ngay,
      buoi: goc.buoi,
      soCongMinh: a ? a.soCong : null,
      soCongBenKia: b ? b.soCong : null,
      loai: !b ? 'chiMinhCo' : !a ? 'chiBenKiaCo' : 'lechSoCong',
      // Bên nào khai đã chốt cũng tính là đã chốt: chỉ máy chủ có cờ này, mà chủ mới là
      // bên trả tiền.
      daChot: a?.daChot === true || b?.daChot === true,
    });
  }

  /*
    Chưa so được buổi nào — kể cả buổi tạm gác — thì nói thẳng là hai sổ chưa có ngày nào
    chung. Xét theo *kết quả* chứ theo hai mốc khai: khoảng giao có thể rộng mà vẫn không
    buổi nào được chấm, mà cũng có thể hẹp tới mức rỗng trong lúc một buổi chấm bù ngoài
    khoảng vẫn so được.
  */
  if (soTu === null && soTamGac === 0) {
    return {
      tuNgay: giaoTu,
      denNgay: giaoDen,
      khongTrungKhoang: giaoTu > giaoDen,
      soKhop: 0,
      lechs: [],
      tongCongMinh: 0,
      tongCongBenKia: 0,
      soTamGac: 0,
    };
  }

  lechs.sort((x, y) =>
    x.ngay === y.ngay
      ? THU_TU_BUOI[x.buoi] - THU_TU_BUOI[y.buoi]
      : x.ngay.localeCompare(y.ngay),
  );

  return {
    // Nới khoảng nói ở đầu trang ra cho phủ hết những buổi thật sự đã so: một dòng chấm bù
    // ngoài khoảng khai vẫn hiện bên dưới, mà đầu trang lại ghi khoảng không chứa nó thì
    // người dùng đọc thành hai điều trái nhau.
    tuNgay: soTu !== null && soTu < giaoTu ? soTu : giaoTu,
    denNgay: soDen !== null && soDen > giaoDen ? soDen : giaoDen,
    khongTrungKhoang: false,
    soKhop,
    lechs,
    tongCongMinh,
    tongCongBenKia,
    soTamGac,
  };
}

/** Buổi đã quyết toán thì khoá, không cho sửa theo sổ bên kia nữa. */
export class DaChotKhongSuaDuoc extends Error {
  constructor() {
    super('Buổi này đã nằm trong kỳ đã quyết toán, không sửa được nữa.');
  }
}

/**
 * Lấy theo sổ bên kia cho **một** dòng lệch: ghi vào sổ của chính máy này.
 *
 * Một dòng một lần, không có nút "lấy hết": người bấm phải nhìn từng buổi. Chỗ này là chỗ
 * tiền ra tiền vào, một nút lấy hết là mời người ta bấm cho xong việc.
 */
export function layTheoBenKia(
  duLieu: DuLieuChamCong,
  thoId: string,
  lech: DongLech,
): DuLieuChamCong {
  if (lech.daChot) {
    throw new DaChotKhongSuaDuoc();
  }

  if (lech.soCongBenKia === null) {
    return boCham(duLieu, thoId, lech.ngay, lech.buoi);
  }

  return cham(duLieu, thoId, lech.ngay, lech.buoi, lech.soCongBenKia);
}
