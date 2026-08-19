/**
 * Điều phối việc sao lưu: đóng gói dữ liệu, đẩy lên Drive, liệt kê các bản đã có và
 * khôi phục về máy.
 *
 * Mỗi ngày một file. Trong ngày sao lưu bao nhiêu lần cũng chỉ ghi đè lên file của ngày
 * hôm ấy, nên Drive không đầy rác; nhưng hôm qua vẫn còn nguyên bản hôm qua. Đó là điểm
 * mấu chốt: nếu chỉ giữ đúng một file duy nhất thì hôm nay lỡ tay xoá nhầm mấy chục buổi
 * công, bản sao lưu tự động sẽ chép luôn cái sai ấy đè lên bản đúng — sao lưu mà vẫn mất
 * dữ liệu.
 */

import AsyncStorage from '@react-native-async-storage/async-storage';

import * as Drive from './goiDrive';
import { dongGoi, moGoi, ngayTuTenFile, tenFileSaoLuu, tomTat, TomTat } from './goiSaoLuu';
import { DuLieuChamCong } from './kieu';
import { voiToken } from './phienDrive';

/**
 * Giữ lại 30 bản gần nhất. Một tháng là đủ để phát hiện ra "hình như tháng trước sai số"
 * mà quay về, còn giữ nhiều hơn thì chỉ tổ chiếm chỗ Drive của người dùng.
 */
const SO_BAN_GIU = 30;

const KHOA_LAN_CUOI = 'chamcong.saoluu.lancuoi.v1';

export interface BanSaoLuu {
  id: string;
  /** Ngày của bản sao lưu, dạng yyyy-MM-dd. */
  ngay: string;
  /** Lúc Drive ghi nhận lần ghi cuối, dạng ISO. */
  suaLuc: string;
}

/** Lần sao lưu gần nhất chạy xong, để hiện lên màn hình. */
export async function lanCuoi(): Promise<string | null> {
  return AsyncStorage.getItem(KHOA_LAN_CUOI);
}

/**
 * Đẩy toàn bộ dữ liệu lên Drive. Trả về ngày của bản vừa ghi.
 *
 * `homNay` truyền vào chứ không tự lấy để bài kiểm thử chạy được, và để cả app cùng
 * thống nhất một cái "hôm nay".
 */
export async function saoLuu(duLieu: DuLieuChamCong, homNay: string): Promise<BanSaoLuu> {
  const ten = tenFileSaoLuu(homNay);
  const noiDung = dongGoi(duLieu, new Date().toISOString());

  const ban = await voiToken(async (token) => {
    const daCo = (await Drive.danhSach(token)).find((file) => file.ten === ten);
    const file = daCo
      ? await Drive.ghiDe(token, daCo.id, noiDung)
      : await Drive.taoFile(token, ten, noiDung);

    return { id: file.id, ngay: homNay, suaLuc: file.suaLuc };
  });

  await AsyncStorage.setItem(KHOA_LAN_CUOI, new Date().toISOString());
  await donBanCu();

  return ban;
}

/** Các bản sao lưu đang có trên Drive, mới nhất đứng đầu. */
export async function danhSachBan(): Promise<BanSaoLuu[]> {
  return voiToken(async (token) => {
    const cacFile = await Drive.danhSach(token);

    return cacFile
      .map((file) => ({ file, ngay: ngayTuTenFile(file.ten) }))
      // File nào không đúng khuôn tên thì bỏ qua: không phải bản sao lưu của app này.
      .filter((muc): muc is { file: Drive.FileDrive; ngay: string } => muc.ngay !== null)
      .map(({ file, ngay }) => ({ id: file.id, ngay, suaLuc: file.suaLuc }))
      .sort((a, b) => b.ngay.localeCompare(a.ngay));
  });
}

/** Tải một bản về và mở gói. Chưa ghi xuống máy — để người gọi hỏi lại người dùng đã. */
export async function docBan(id: string): Promise<{ duLieu: DuLieuChamCong; tomTat: TomTat }> {
  const noiDung = await voiToken((token) => Drive.taiVe(token, id));
  const goi = moGoi(noiDung);
  return { duLieu: goi.duLieu, tomTat: tomTat(goi.duLieu) };
}

/**
 * Xoá bớt bản cũ, chỉ giữ `SO_BAN_GIU` bản mới nhất.
 *
 * Hỏng thì nuốt lỗi: dọn dẹp là việc phụ, để nó làm hỏng kết quả "đã sao lưu xong" thì
 * người dùng tưởng dữ liệu chưa lên Drive trong khi nó đã lên rồi.
 */
async function donBanCu(): Promise<void> {
  try {
    const cacBan = await danhSachBan();
    const canXoa = cacBan.slice(SO_BAN_GIU);

    for (const ban of canXoa) {
      await voiToken((token) => Drive.xoa(token, ban.id));
    }
  } catch {
    // Lần sao lưu sau dọn tiếp.
  }
}
