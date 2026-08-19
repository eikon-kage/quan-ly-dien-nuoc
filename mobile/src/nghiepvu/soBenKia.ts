/**
 * Bản chụp sổ bên kia, lưu trong máy để mở app ra là xem đối chiếu được ngay, không phải
 * chờ mạng.
 *
 * Lưu ở một khoá **riêng**, không nhập vào `DuLieuChamCong`. Hai lý do, cả hai đều quan
 * trọng: sổ bên kia không phải sổ của mình nên không được lọt vào bảng lương hay quyết
 * toán; và nó lấy lại được từ hộp thư bất cứ lúc nào nên chẳng cần chiếm chỗ trong bản
 * sao lưu.
 */

import AsyncStorage from '@react-native-async-storage/async-storage';

import { dongGoiSo, moGoiSo } from './goiSo';
import { SoDaNhan } from './hopThu';

const KHOA = 'chamcong.sobenkia.v1';

/**
 * Giữ nguyên dạng gói đã nhận chứ không giữ đối tượng đã mở: lúc đọc lại vẫn đi qua đúng
 * bộ kiểm của `moGoiSo`, khỏi phải viết một bộ kiểm thứ hai cho dữ liệu đọc từ máy — mà
 * hai bộ kiểm thì sớm muộn lệch nhau.
 */
interface DongLuu {
  goi: string;
  suaLuc: string;
}

export async function doc(): Promise<Map<string, SoDaNhan>> {
  const daNhan = new Map<string, SoDaNhan>();

  try {
    const noiDung = await AsyncStorage.getItem(KHOA);
    if (!noiDung) {
      return daNhan;
    }

    for (const [thoId, dong] of Object.entries(JSON.parse(noiDung) as Record<string, DongLuu>)) {
      try {
        daNhan.set(thoId, { so: moGoiSo(dong.goi), suaLuc: dong.suaLuc });
      } catch {
        // Một sổ hỏng thì bỏ sổ ấy thôi. Lần nhận sau ghi đè lại là xong.
      }
    }
  } catch {
    // Cả khối hỏng thì coi như chưa nhận sổ nào — lần nhận sau lấy lại từ hộp thư.
  }

  return daNhan;
}

/** Ghi đè các sổ vừa nhận. Sổ của thợ nào không có trong lần này thì giữ nguyên bản cũ. */
export async function luu(cac: SoDaNhan[]): Promise<Map<string, SoDaNhan>> {
  const dangCo = await doc();
  for (const daNhan of cac) {
    dangCo.set(daNhan.so.thoId, daNhan);
  }

  const khoi: Record<string, DongLuu> = {};
  for (const [thoId, daNhan] of dangCo) {
    khoi[thoId] = { goi: dongGoiSo(daNhan.so), suaLuc: daNhan.suaLuc };
  }
  await AsyncStorage.setItem(KHOA, JSON.stringify(khoi));

  return dangCo;
}

/** Dùng khi đổi vai máy: sổ bên kia của vai cũ không còn nghĩa gì. */
export async function xoaHet(): Promise<void> {
  await AsyncStorage.removeItem(KHOA);
}
