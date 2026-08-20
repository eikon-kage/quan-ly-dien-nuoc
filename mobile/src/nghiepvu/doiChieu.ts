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
  /** Khoảng thật sự so được: phần giao của hai sổ. */
  tuNgay: string;
  denNgay: string;
  /**
   * Hai sổ không có ngày nào chung. Chưa kết luận được gì — thường là máy thợ mới cài,
   * hoặc sổ bên kia gửi từ lâu quá.
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

function trongKhoang(dongs: DongCong[], tuNgay: string, denNgay: string): Map<string, DongCong> {
  const theoKhoa = new Map<string, DongCong>();
  for (const dong of dongs) {
    if (dong.ngay >= tuNgay && dong.ngay <= denNgay) {
      theoKhoa.set(khoa(dong.ngay, dong.buoi), dong);
    }
  }
  return theoKhoa;
}

/**
 * So hai sổ, trả về những buổi nói khác nhau.
 *
 * Chỉ so trong **phần giao** của hai khoảng ngày. Ngoài phần giao thì có bên không khai là
 * đầy đủ, thiếu một buổi ở đó không có nghĩa là ai sai — xem ghi chú ở `SoCong.tuNgay`.
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
 * dưới, chứ không thì đầu trang bảo lệch 2 công mà không có dòng nào giải thích.
 */
export function doiChieu(soMinh: SoCong, soBenKia: SoCong, homNay: string): KetQuaDoiChieu {
  const tuNgay = soMinh.tuNgay > soBenKia.tuNgay ? soMinh.tuNgay : soBenKia.tuNgay;
  const denNgay = soMinh.denNgay < soBenKia.denNgay ? soMinh.denNgay : soBenKia.denNgay;

  if (tuNgay > denNgay) {
    return {
      tuNgay,
      denNgay,
      khongTrungKhoang: true,
      soKhop: 0,
      lechs: [],
      tongCongMinh: 0,
      tongCongBenKia: 0,
      soTamGac: 0,
    };
  }

  const minh = trongKhoang(soMinh.dongs, tuNgay, denNgay);
  const benKia = trongKhoang(soBenKia.dongs, tuNgay, denNgay);

  const lechs: DongLech[] = [];
  let soKhop = 0;
  let soTamGac = 0;
  let tongCongMinh = 0;
  let tongCongBenKia = 0;

  for (const k of new Set([...minh.keys(), ...benKia.keys()])) {
    const a = minh.get(k);
    const b = benKia.get(k);

    const goc = a ?? b;
    if (!goc) {
      continue;
    }

    // Hôm nay mà chỉ một bên có: gác lại cả khỏi tổng, xem ghi chú ở đầu hàm.
    if (goc.ngay >= homNay && (!a || !b)) {
      soTamGac += 1;
      continue;
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

  lechs.sort((x, y) =>
    x.ngay === y.ngay
      ? THU_TU_BUOI[x.buoi] - THU_TU_BUOI[y.buoi]
      : x.ngay.localeCompare(y.ngay),
  );

  return {
    tuNgay,
    denNgay,
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
