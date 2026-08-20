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
 * chính mình để gửi lên.
 *
 * `gomTuNgay` là mốc dưới của **những dòng gửi kèm**, tách khỏi `tuNgay` là mốc dưới của
 * **khoảng khai là đầy đủ**. Hai thứ khác nhau và phải để khác nhau được: máy thợ chấm bù
 * ra trước hôm nó nhận vai máy thì buổi ấy vẫn phải đi lên nhóm (không thì công mất hút),
 * nhưng mấy ngày trống quanh đó thì máy này *không biết* chứ không phải "thợ nghỉ" — kéo
 * mốc khai xuống theo là nói dối, và `doiChieu` tin lời khai ấy mà báo lệch cả tuần.
 * Bỏ trống thì hai mốc bằng nhau, đúng như cũ.
 */
export function catSo(
  duLieu: DuLieuChamCong,
  thoId: string,
  nguon: Vai,
  tuNgay: string,
  denNgay: string,
  taoLuc: string,
  gomTuNgay: string = tuNgay,
): SoCong {
  const tho = timTho(duLieu, thoId);
  const chuaChot = new Set(banGhiChuaChot(duLieu).buoiCongs.map((b) => b.id));

  const dongs = duLieu.buoiCongs
    .filter((b) => b.thoId === thoId && b.ngay >= gomTuNgay && b.ngay <= denNgay)
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

/**
 * Khoảng ngày **máy chủ** khai là đầy đủ: 90 ngày gần nhất, nhưng kẹp lại trong phần chủ
 * thật sự đã ghi chép.
 *
 * Kẹp hai đầu chứ không khai thẳng 90 ngày, vì "khai là đầy đủ" là một lời quả quyết — ở
 * trong khoảng ấy, không có dòng nghĩa là *thợ nghỉ*. Khai bừa cả 90 ngày là nói dối hai
 * lần, mà lần nào cũng ra một màn hình đỏ rực đúng như chỗ `SoCong.tuNgay` cảnh báo:
 *
 * - **Đầu dưới.** Chủ mới chuyển từ sổ giấy sang app hôm nay, chấm bù được ba ngày. Sổ chủ
 *   khai đầy đủ 90 ngày thì với một thợ đã dùng app cả tháng, đối chiếu ra hai bảy dòng
 *   "chủ không chấm" — toàn là ngày chủ chưa có app.
 * - **Đầu trên.** Chủ chấm cho cả nhóm theo lô, thường chậm một hai hôm; thợ thì chấm cho
 *   mình ngay trong ngày. Khai tới `homNay` là quả quyết "hôm qua cả nhóm nghỉ" trong lúc
 *   chủ chỉ chưa kịp nhập, nên sáng nào mở app thợ cũng thấy hai dòng đỏ của hôm qua.
 *   Dừng ở ngày chủ ghi cuối cùng thì chỗ ấy thành *chưa so được* — đúng sự thật, và tự
 *   liền lại ngay khi chủ nhập tới đó.
 *
 * Lấy theo ngày của **cả nhóm**, không phải của riêng thợ đang xem: chủ có nhập hôm ấy hay
 * chưa là chuyện của cái máy, còn một thợ nghỉ hôm ấy là chuyện của thợ — trộn hai thứ vào
 * nhau thì thợ nào nghỉ thật cũng bị cắt mất phần sổ quanh ngày nghỉ.
 *
 * Chủ chưa ghi gì trong cửa sổ thì khai đúng một ngày `homNay`: chưa có gì để so, và luật
 * tạm gác buổi của hôm nay lo phần còn lại.
 */
export function cuaSoCuaChu(
  duLieu: DuLieuChamCong,
  homNay: string,
): { tuNgay: string; denNgay: string } {
  const dauCuaSo = Ngay.congNgay(homNay, -CUA_SO_NGAY);

  let som: string | null = null;
  let muon: string | null = null;
  for (const buoi of duLieu.buoiCongs) {
    if (buoi.ngay < dauCuaSo || buoi.ngay > homNay) {
      continue;
    }
    if (som === null || buoi.ngay < som) {
      som = buoi.ngay;
    }
    if (muon === null || buoi.ngay > muon) {
      muon = buoi.ngay;
    }
  }

  if (som === null || muon === null) {
    return { tuNgay: homNay, denNgay: homNay };
  }
  return { tuNgay: som, denNgay: muon };
}

/**
 * Mốc dưới của **những dòng máy thợ gửi kèm** — mốc bắt đầu chấm, nới xuống tới buổi sớm
 * nhất nếu thợ đã chấm bù ra trước mốc ấy.
 *
 * Vì sao phải nới: màn hình máy thợ mời chấm bù 13 ngày trước, mà `batDauTu` lại đặt đúng
 * hôm chọn vai máy. Không nới thì `catSo` cắt bỏ luôn mấy buổi chấm bù — máy thợ hiện ô đã
 * chấm, tổng công tuần cộng cả buổi ấy, nhưng sổ gửi lên nhóm thì không có nó. Chủ không
 * thấy gì mà đối chiếu cũng không báo lệch. Công biến mất lặng lẽ.
 *
 * Nới **mốc gửi**, không nới mốc khai là đầy đủ — hai việc khác nhau, và trước đây gộp làm
 * một chính là chỗ sai: thợ vừa cài app, chấm bù đúng một buổi cách đây năm hôm, thế là mấy
 * ngày trống nằm giữa bị khai thành "thợ nghỉ" và đối chiếu ra chín dòng đỏ ngay lần mở đầu
 * tiên. Máy thợ **không biết** những ngày ấy, nó chưa tồn tại. Buổi chấm bù vẫn đi lên và
 * vẫn được so, vì `doiChieu` so cả những dòng nằm ngoài khoảng khai.
 */
function mocGomCuaTho(duLieu: DuLieuChamCong, thoId: string, batDauTu: string): string {
  let som = batDauTu;
  for (const buoi of duLieu.buoiCongs) {
    if (buoi.thoId === thoId && buoi.ngay < som) {
      som = buoi.ngay;
    }
  }
  return som;
}

/**
 * Sổ của **máy này** về một thợ, khai đúng khoảng mà máy này biết chắc.
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
    // Không cần mốc gom riêng: `denNgay` đã đúng là ngày chủ ghi cuối cùng, sau nó không
    // còn dòng nào để mà cắt mất.
    const { tuNgay, denNgay } = cuaSoCuaChu(duLieu, homNay);
    return catSo(duLieu, thoId, 'chu', tuNgay, denNgay, taoLuc);
  }

  const khaiTu = may.batDauTu ?? homNay;
  return catSo(
    duLieu,
    thoId,
    'tho',
    khaiTu,
    homNay,
    taoLuc,
    mocGomCuaTho(duLieu, thoId, khaiTu),
  );
}

/**
 * Khoảng phủ hết những gì sổ này nói: khoảng khai là đầy đủ, nới ra cho chứa cả những dòng
 * nằm ngoài nó.
 *
 * Dùng để **hiện sổ ra xem**, khác `tuNgay`/`denNgay` là dùng để *kết luận*. Máy thợ chấm bù
 * ra trước hôm nó nhận vai máy: buổi ấy nằm ngoài khoảng khai, nhưng nó là công thật, thợ
 * vừa tự bấm, mà lại không có trong danh sách sổ của chính mình thì thợ tưởng máy làm mất.
 */
export function khoangCuaSo(so: SoCong): { tuNgay: string; denNgay: string } {
  let tuNgay = so.tuNgay;
  let denNgay = so.denNgay;
  for (const dong of so.dongs) {
    if (dong.ngay < tuNgay) {
      tuNgay = dong.ngay;
    }
    if (dong.ngay > denNgay) {
      denNgay = dong.ngay;
    }
  }
  return { tuNgay, denNgay };
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
