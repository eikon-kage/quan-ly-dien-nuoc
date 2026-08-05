/** Đọc và ghi dữ liệu chấm công xuống bộ nhớ của điện thoại. */

import AsyncStorage from '@react-native-async-storage/async-storage';
import { DuLieuChamCong, Tho, duLieuRong } from './kieu';

/** Có số phiên bản trong khoá để sau này đổi cấu trúc dữ liệu còn biết đường chuyển đổi. */
const KHOA = 'chamcong.dulieu.v1';

/** Dáng cũ của Tho: một mức tiền công duy nhất, chưa có lịch sử. */
interface ThoBanCu extends Partial<Tho> {
  tienMotCong?: number;
}

/**
 * Chuyển dữ liệu đã lưu trên máy sang cấu trúc mới.
 * Thợ bản cũ chỉ có một mức `tienMotCong` — biến nó thành mốc lương đầu tiên, tính từ
 * ngày thêm thợ, để mọi buổi công cũ vẫn tính ra đúng số tiền như trước.
 */
function chuyenDoiTho(tho: ThoBanCu): Tho {
  const mocLuong =
    tho.mocLuong && tho.mocLuong.length > 0
      ? tho.mocLuong
      : [{ tuNgay: tho.ngayTao ?? '2000-01-01', tienMotCong: tho.tienMotCong ?? 0 }];

  return {
    id: tho.id ?? '',
    ten: tho.ten ?? '',
    dienThoai: tho.dienThoai ?? '',
    mocLuong,
    dangLam: tho.dangLam ?? true,
    ghiChu: tho.ghiChu ?? '',
    ngayTao: tho.ngayTao ?? '2000-01-01',
    suaLuc: tho.suaLuc ?? new Date().toISOString(),
  };
}

export async function doc(): Promise<DuLieuChamCong> {
  try {
    const noiDung = await AsyncStorage.getItem(KHOA);
    if (!noiDung) {
      return duLieuRong();
    }

    const daDoc = JSON.parse(noiDung) as Partial<DuLieuChamCong> & { thos?: ThoBanCu[] };
    return {
      thos: (daDoc.thos ?? []).map(chuyenDoiTho),
      buoiCongs: daDoc.buoiCongs ?? [],
      ungTiens: daDoc.ungTiens ?? [],
    };
  } catch {
    // Dữ liệu hỏng thì thà mở app lên trống còn hơn không mở được.
    return duLieuRong();
  }
}

export async function ghi(duLieu: DuLieuChamCong): Promise<void> {
  await AsyncStorage.setItem(KHOA, JSON.stringify(duLieu));
}
