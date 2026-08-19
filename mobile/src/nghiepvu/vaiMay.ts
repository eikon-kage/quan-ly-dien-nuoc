/**
 * Vai của máy này: máy chủ chấm công cho cả nhóm, hay máy của một thợ tự chấm cho mình.
 *
 * Lưu tách khỏi dữ liệu chấm công. Vai là chuyện của **cái máy**, không phải của sổ sách:
 * nếu nhét vào `DuLieuChamCong` thì khôi phục một bản sao lưu của máy chủ về máy thợ là
 * máy thợ tự biến thành máy chủ.
 */

import AsyncStorage from '@react-native-async-storage/async-storage';

import { DuLieuChamCong } from './kieu';
import { Vai } from './soCong';
import { themTho } from './thaoTac';

const KHOA = 'chamcong.vaimay.v1';

export interface CaiDatVai {
  vai: Vai;
  /** Máy thợ: id của chính mình, do máy chủ đặt và trao qua mã mời. Máy chủ để null. */
  thoId: string | null;
  /**
   * Ngày máy này bắt đầu chấm — mốc dưới của khoảng mà sổ của nó khai là đầy đủ.
   *
   * Không có mốc này thì sổ của máy thợ mới cài trông như sổ trống suốt ba tháng, và đối
   * chiếu sẽ báo chủ chấm khống mấy chục buổi. Xem ghi chú ở `SoCong.tuNgay`.
   */
  batDauTu: string | null;
}

/**
 * Chưa chọn gì thì là **máy chủ**.
 *
 * Bắt buộc phải thế: mọi máy đang cài app đều là máy của chủ, bản cập nhật này không được
 * làm họ mất màn hình nào. Máy thợ là thứ phải chủ động chọn.
 */
export const MAC_DINH: CaiDatVai = { vai: 'chu', thoId: null, batDauTu: null };

export async function doc(): Promise<CaiDatVai> {
  try {
    const noiDung = await AsyncStorage.getItem(KHOA);
    if (!noiDung) {
      return MAC_DINH;
    }

    const daDoc = JSON.parse(noiDung) as Partial<CaiDatVai>;
    // Máy thợ mà thiếu id thì không chấm cho ai được, cũng không đối chiếu được — coi như
    // chưa cài đặt gì, quay về làm máy chủ để người dùng chọn lại từ đầu.
    if (daDoc.vai !== 'tho' || typeof daDoc.thoId !== 'string' || daDoc.thoId === '') {
      return MAC_DINH;
    }

    return {
      vai: 'tho',
      thoId: daDoc.thoId,
      batDauTu: typeof daDoc.batDauTu === 'string' ? daDoc.batDauTu : null,
    };
  } catch {
    return MAC_DINH;
  }
}

export async function ghi(caiDat: CaiDatVai): Promise<void> {
  await AsyncStorage.setItem(KHOA, JSON.stringify(caiDat));
}

/**
 * Mã mời máy chủ đọc cho thợ: chính id của thợ, thêm tiền tố cho ra dáng một cái mã.
 *
 * Không nhồi tên thợ vào mã: tên có dấu, có khoảng trắng, gõ lại qua Zalo là sai. Máy thợ
 * lấy tên từ chính sổ chủ gửi xuống, khỏi phải gõ.
 */
export function maMoi(thoId: string): string {
  return `CC-${thoId}`;
}

/** Đọc mã người dùng dán vào. Không đúng khuôn thì trả null. */
export function docMaMoi(ma: string): string | null {
  const gon = ma.trim().replace(/^CC-/i, '');
  return /^[A-Za-z0-9_-]+$/.test(gon) ? gon : null;
}

/**
 * Kết nạp máy này thành máy của một thợ: đảm bảo trong sổ có bản ghi thợ mang **đúng id
 * của mã mời**, vì đó là chỗ mọi buổi công chấm trên máy này sẽ móc vào.
 *
 * `xoaNguoiKhac` dành cho máy cũ chuyền tay: một cái điện thoại từng là máy chủ, giờ đưa
 * cho thợ dùng. Bỏ hết bản ghi của người khác và **xoá sạch tiền** — kể cả mốc lương của
 * chính thợ ấy. Máy thợ chỉ được thấy số công, mà cái gì không có trên máy thì không ai
 * xem lén được; ẩn bằng giao diện thì vẫn còn nằm đó.
 */
export function ketNap(
  duLieu: DuLieuChamCong,
  thoId: string,
  homNay: string,
  xoaNguoiKhac: boolean,
): DuLieuChamCong {
  const goc = xoaNguoiKhac
    ? {
        thos: duLieu.thos
          .filter((tho) => tho.id === thoId)
          // Mốc lương là tiền: về 0 hết. Bảng lương trên máy thợ vốn không hiện ra.
          .map((tho) => ({ ...tho, mocLuong: [{ tuNgay: tho.ngayTao, tienMotCong: 0 }] })),
        buoiCongs: duLieu.buoiCongs.filter((b) => b.thoId === thoId),
        // Ứng tiền và kỳ đã chốt đều là tiền, và là chuyện của sổ chủ.
        ungTiens: [],
        kyLuongs: [],
      }
    : duLieu;

  if (goc.thos.some((tho) => tho.id === thoId)) {
    return goc;
  }

  // Tên để tạm; máy thợ lấy tên thật từ sổ chủ gửi xuống chứ không bắt thợ tự gõ.
  return themTho(goc, 'Tôi', 0, homNay, thoId).duLieu;
}
