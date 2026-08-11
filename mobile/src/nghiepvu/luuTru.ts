/** Đọc và ghi dữ liệu chấm công xuống bộ nhớ của điện thoại. */

import AsyncStorage from '@react-native-async-storage/async-storage';
import { chuanHoa, DuLieuChamCong, duLieuRong } from './kieu';

/** Có số phiên bản trong khoá để sau này đổi cấu trúc dữ liệu còn biết đường chuyển đổi. */
const KHOA = 'chamcong.dulieu.v1';

export async function doc(): Promise<DuLieuChamCong> {
  try {
    const noiDung = await AsyncStorage.getItem(KHOA);
    if (!noiDung) {
      return duLieuRong();
    }

    return chuanHoa(JSON.parse(noiDung));
  } catch {
    // Dữ liệu hỏng thì thà mở app lên trống còn hơn không mở được.
    return duLieuRong();
  }
}

export async function ghi(duLieu: DuLieuChamCong): Promise<void> {
  await AsyncStorage.setItem(KHOA, JSON.stringify(duLieu));
}
