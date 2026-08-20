/**
 * Sổ công — mẩu dữ liệu hai máy trao cho nhau để đối chiếu.
 *
 * Cố tình **không phải** `DuLieuChamCong`: sổ này chỉ có ngày, buổi và số công của đúng
 * một thợ, tuyệt đối không có đồng tiền nào. Máy thợ chỉ được xem số công của mình, mà
 * cắt tiền ra phải cắt ngay từ lúc đóng gói chứ không thể trông vào giao diện không hiện:
 * gói đã gửi đi là nằm trong tay người ta, mở file ra đọc được hết.
 *
 * Cũng cố tình dùng **chung cho cả hai chiều** — chủ gửi xuống cho thợ và thợ gửi lên cho
 * chủ đều là kiểu này. Nhờ vậy hàm đối chiếu chỉ cần một bản, chạy đúng ở cả hai máy.
 */

import { BuoiLam, DuLieuChamCong } from './kieu';
import { banGhiChuaChot } from './ky';
import * as Ngay from './ngayViet';
import { timTho } from './thaoTac';

/** Vai của máy: chủ chấm cho cả nhóm, hay thợ tự chấm cho mình. */
export type Vai = 'chu' | 'tho';

export interface DongCong {
  ngay: string;
  buoi: BuoiLam;
  soCong: number;
  /**
   * Buổi này đã nằm trong một kỳ đã quyết toán. Chỉ sổ của chủ mới có — máy thợ không
   * chốt kỳ. Hai bên vẫn thấy để biết, nhưng lệch ở những buổi này thì **không sửa**:
   * tiền đã trả rồi, sửa số công bây giờ là bảng lương cũ nói khác tờ quyết toán đã đưa.
   */
  daChot?: boolean;
}

export interface SoCong {
  /** Sổ này nói về thợ nào. Id do máy chủ đặt, máy thợ nhận được qua mã mời. */
  thoId: string;
  tenTho: string;
  /** Bên nào làm ra sổ này. */
  nguon: Vai;
  /**
   * Khoảng ngày mà sổ này khai là **đầy đủ** — trong khoảng ấy, không có dòng nghĩa là
   * thật sự không chấm.
   *
   * Không có hai mốc này thì đối chiếu vô dụng: máy thợ mới cài hôm qua, sổ chủ có ba
   * tháng trước đó, so cả sổ ra một trăm dòng "thợ thiếu" toàn là ngày thợ chưa có app.
   * Người dùng nhìn một màn hình đỏ rực không sửa được gì rồi thôi, không dùng nữa.
   */
  tuNgay: string;
  denNgay: string;
  dongs: DongCong[];
  /** Lúc làm ra sổ, dạng ISO. Để bên nhận biết sổ đang cầm là cũ hay mới. */
  taoLuc: string;
}

/**
 * Cắt sổ công của một thợ ra khỏi dữ liệu đầy đủ.
 *
 * Dùng cho cả hai máy: máy chủ cắt phần của đúng thợ đó để gửi xuống, máy thợ cắt sổ của
 * chính mình để gửi lên. Buổi nào ngoài khoảng ngày thì bỏ, vì đã khai là chỉ đầy đủ
 * trong khoảng ấy.
 */
export function catSo(
  duLieu: DuLieuChamCong,
  thoId: string,
  nguon: Vai,
  tuNgay: string,
  denNgay: string,
  taoLuc: string,
): SoCong {
  const tho = timTho(duLieu, thoId);
  const chuaChot = new Set(banGhiChuaChot(duLieu).buoiCongs.map((b) => b.id));

  const dongs = duLieu.buoiCongs
    .filter((b) => b.thoId === thoId && b.ngay >= tuNgay && b.ngay <= denNgay)
    .map((b) => {
      const dong: DongCong = { ngay: b.ngay, buoi: b.buoi, soCong: b.soCong };
      // Chỉ ghi cờ khi đúng là đã chốt: gói nào cũng có `daChot: false` thì file to ra
      // mà chẳng nói thêm điều gì.
      if (!chuaChot.has(b.id)) {
        dong.daChot = true;
      }
      return dong;
    })
    .sort((a, b) => (a.ngay === b.ngay ? a.buoi.localeCompare(b.buoi) : a.ngay.localeCompare(b.ngay)));

  return {
    thoId,
    // Tên để bên nhận có cái mà hiện lên; máy thợ nhận tên của mình từ đây luôn.
    tenTho: tho?.ten ?? '',
    nguon,
    tuNgay,
    denNgay,
    dongs,
    taoLuc,
  };
}

/**
 * Cửa sổ ngày máy chủ gửi xuống cho thợ: 90 ngày gần nhất.
 *
 * Không gửi cả sổ từ thuở nào: đối chiếu là việc của kỳ đang làm, mà file càng dài thì
 * mỗi lần gửi càng tốn 3G của cả nhóm. Ba tháng đủ để soát lại kỳ trước nếu thợ thắc mắc.
 */
export const CUA_SO_NGAY = 90;

/** Khoảng ngày máy chủ khai khi gửi sổ xuống cho thợ. */
export function cuaSoCuaChu(homNay: string): { tuNgay: string; denNgay: string } {
  return { tuNgay: Ngay.congNgay(homNay, -CUA_SO_NGAY), denNgay: homNay };
}

/**
 * Mốc dưới của sổ máy thợ: mốc bắt đầu chấm, nới ra tới buổi sớm nhất nếu thợ đã chấm bù ra
 * trước mốc ấy.
 *
 * Vì sao phải nới: màn hình máy thợ mời chấm bù 13 ngày trước, mà `batDauTu` lại đặt đúng
 * hôm chọn vai máy. Giữ nguyên mốc thì mọi buổi chấm bù trước hôm ấy nằm ngoài khoảng sổ
 * khai là đầy đủ, và `catSo` cắt bỏ luôn — máy thợ hiện ô đã chấm, tổng công tuần cộng cả
 * buổi ấy, nhưng sổ gửi lên nhóm thì không có nó. Chủ không thấy gì mà đối chiếu cũng không
 * báo lệch, vì buổi ấy rơi ngoài phần giao hai khoảng. Công biến mất lặng lẽ.
 *
 * Nới tới buổi sớm nhất là **nói đúng sự thật**: máy này có biết về ngày ấy, chính người
 * dùng vừa khai. Mấy ngày trống nằm giữa thành "nghỉ" theo sổ thợ, nên chủ có chấm là đối
 * chiếu báo lệch — thà một dòng lệch nhìn thấy được còn hơn một buổi công mất hút.
 */
function mocDuoiCuaTho(duLieu: DuLieuChamCong, thoId: string, batDauTu: string): string {
  let som = batDauTu;
  for (const buoi of duLieu.buoiCongs) {
    if (buoi.thoId === thoId && buoi.ngay < som) {
      som = buoi.ngay;
    }
  }
  return som;
}

/**
 * Sổ của **máy này** về một thợ, cắt theo đúng khoảng mà máy này khai là đầy đủ.
 *
 * Nằm ở đây, cạnh `catSo`, chứ không nằm trong màn hình: cả màn hình đối chiếu, màn hình
 * của thợ và lớp điều phối hộp thư đều phải cắt y hệt nhau. Ba chỗ tự cắt theo ba kiểu là
 * ba kết quả đối chiếu khác nhau trên cùng một dữ liệu.
 *
 * Nhận `{ vai, batDauTu }` theo hình dáng chứ không nhận `CaiDatVai`: kiểu ấy nằm ở
 * vaiMay, mà vaiMay lại cần `Vai` ở đây — nhập vào là hai file gọi vòng nhau.
 */
export function soCuaMay(
  duLieu: DuLieuChamCong,
  may: { vai: Vai; batDauTu: string | null },
  thoId: string,
  homNay: string,
  taoLuc = '',
): SoCong {
  if (may.vai === 'chu') {
    const { tuNgay, denNgay } = cuaSoCuaChu(homNay);
    return catSo(duLieu, thoId, 'chu', tuNgay, denNgay, taoLuc);
  }
  const tuNgay = mocDuoiCuaTho(duLieu, thoId, may.batDauTu ?? homNay);
  return catSo(duLieu, thoId, 'tho', tuNgay, homNay, taoLuc);
}

/**
 * Một ngày trong sổ, hai buổi gộp lại. Sổ lưu theo *buổi* vì đó là đơn vị được chấm, còn
 * người xem lại nghĩ theo *ngày* — "hôm mười tư tôi đi cả ngày hay chỉ buổi sáng".
 */
export interface NgayTrongSo {
  ngay: string;
  /** null là không chấm buổi ấy. */
  congSang: number | null;
  congChieu: number | null;
  tongCong: number;
  /** Có buổi đã nằm trong kỳ chủ quyết toán rồi. Chỉ sổ của chủ mới mang cờ này. */
  daChot: boolean;
}

/** Gộp các dòng của một khoảng thành từng ngày, xếp ngày tăng dần. */
export function gomTheoNgay(so: SoCong, tuNgay: string, denNgay: string): NgayTrongSo[] {
  const theoNgay = new Map<string, NgayTrongSo>();

  for (const dong of so.dongs) {
    if (dong.ngay < tuNgay || dong.ngay > denNgay) {
      continue;
    }

    const ngay = theoNgay.get(dong.ngay) ?? {
      ngay: dong.ngay,
      congSang: null,
      congChieu: null,
      tongCong: 0,
      daChot: false,
    };

    if (dong.buoi === 'Sang') {
      ngay.congSang = dong.soCong;
    } else {
      ngay.congChieu = dong.soCong;
    }
    ngay.tongCong += dong.soCong;
    ngay.daChot = ngay.daChot || dong.daChot === true;

    theoNgay.set(dong.ngay, ngay);
  }

  return [...theoNgay.values()].sort((a, b) => a.ngay.localeCompare(b.ngay));
}

/**
 * Những ngày trong khoảng mà sổ không có buổi nào — tức là **thật sự nghỉ**.
 *
 * Chỉ đếm phần sổ khai là đầy đủ và phần đã trôi qua: ngoài khoảng ấy, không có dòng
 * nghĩa là *không biết* chứ không phải nghỉ, mà ngày mai chưa tới thì cũng chưa nghỉ.
 * Đếm bừa cả hai phần thì mở sổ đầu tháng sẽ thấy báo nghỉ gần trọn tháng, hoảng.
 */
export function ngayNghiTrongSo(
  so: SoCong,
  tuNgay: string,
  denNgay: string,
  homNay: string,
): string[] {
  const coCham = new Set(so.dongs.map((dong) => dong.ngay));
  const batDau = tuNgay > so.tuNgay ? tuNgay : so.tuNgay;
  let ketThuc = denNgay < so.denNgay ? denNgay : so.denNgay;
  if (homNay < ketThuc) {
    ketThuc = homNay;
  }

  const nghis: string[] = [];
  for (let ngay = batDau; ngay <= ketThuc; ngay = Ngay.congNgay(ngay, 1)) {
    if (!coCham.has(ngay)) {
      nghis.push(ngay);
    }
  }
  return nghis;
}
