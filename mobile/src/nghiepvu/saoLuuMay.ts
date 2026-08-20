/**
 * Sao lưu ra **file trong máy**: mỗi ngày một file, giữ 30 bản gần nhất.
 *
 * Mỗi ngày một file, không phải một file duy nhất, và đó là điểm mấu chốt: nếu chỉ giữ đúng
 * một bản thì hôm nay lỡ tay xoá mấy chục buổi công, bản sao lưu tự động sẽ chép luôn cái
 * sai ấy đè lên bản đúng — sao lưu mà vẫn mất dữ liệu. Trong ngày sao lưu bao nhiêu lần
 * cũng chỉ ghi đè lên file của ngày hôm ấy, nên thư mục không đầy rác.
 *
 * **Phải nói rõ giới hạn.** Thư mục này nằm trong phần riêng của app, nên *xoá app là mất
 * theo*, mà mất máy thì cũng mất. Nó chống được hỏng dữ liệu, lỡ tay xoá, bản cập nhật app
 * làm hỏng sổ — không chống được mất máy. Muốn chống mất máy thì bản sao phải **ra khỏi
 * app**: đó là việc của [chiaSeSaoLuu](./chiaSeSaoLuu.ts), và màn hình sao lưu phải nói câu
 * ấy ra chứ không để người dùng tưởng có bản trong máy là xong.
 *
 * Đây là phần chạm vào máy: mọi thứ kiểm thử được — đóng gói, mở gói, chọn bản để xoá — đều
 * nằm ở [goiSaoLuu](./goiSaoLuu.ts).
 */

import AsyncStorage from '@react-native-async-storage/async-storage';
import { Directory, File, Paths } from 'expo-file-system';

import { banCanXoa, dongGoi, moGoi, ngayTuTenFile, tenFileSaoLuu } from './goiSaoLuu';
import { DuLieuChamCong } from './kieu';

/** Thư mục con trong phần riêng của app. Đặt tên đọc được, kẻo người dùng mở Files ra thấy lạ. */
const TEN_THU_MUC = 'SaoLuu';

/**
 * Giữ lại 30 bản gần nhất. Một tháng là đủ để phát hiện ra "hình như tháng trước sai số" mà
 * quay về; giữ nhiều hơn thì chỉ chiếm chỗ trong máy người dùng.
 */
const SO_BAN_GIU = 30;

const KHOA_LAN_CUOI = 'chamcong.saoluu.may.lancuoi.v1';

export interface BanSaoLuu {
  /** Tên file, cũng là mã của bản — thư mục này không có hai file cùng tên. */
  ten: string;
  /** Ngày của bản sao lưu, dạng yyyy-MM-dd. */
  ngay: string;
  /** Lúc ghi file, dạng ISO. Rỗng nếu máy không cho biết. */
  suaLuc: string;
}

/** Lần sao lưu gần nhất chạy xong, để hiện lên màn hình. */
export async function lanCuoi(): Promise<string | null> {
  return AsyncStorage.getItem(KHOA_LAN_CUOI);
}

/** Thư mục sao lưu, tạo nếu chưa có. `idempotent` để gọi lại nhiều lần không ném lỗi. */
function thuMuc(): Directory {
  const thu = new Directory(Paths.document, TEN_THU_MUC);
  thu.create({ intermediates: true, idempotent: true });
  return thu;
}

/**
 * Ghi toàn bộ dữ liệu xuống một file trong máy. Trả về bản vừa ghi.
 *
 * `homNay` truyền vào chứ không tự lấy để bài kiểm thử chạy được, và để cả app cùng thống
 * nhất một cái "hôm nay".
 */
export async function saoLuu(duLieu: DuLieuChamCong, homNay: string): Promise<BanSaoLuu> {
  const ten = tenFileSaoLuu(homNay);
  const suaLuc = new Date().toISOString();

  const file = new File(thuMuc(), ten);
  // Sao lưu lần thứ hai trong ngày thì ghi đè lên file của ngày hôm ấy.
  file.create({ overwrite: true });
  file.write(dongGoi(duLieu, suaLuc));

  await AsyncStorage.setItem(KHOA_LAN_CUOI, suaLuc);
  donBanCu();

  return { ten, ngay: homNay, suaLuc };
}

/** Các bản đang có trong máy, mới nhất đứng đầu. */
export async function danhSachBan(): Promise<BanSaoLuu[]> {
  const thu = thuMuc();

  return thu
    .list()
    .map((muc) => ({ muc, ngay: ngayTuTenFile(muc.name) }))
    // File nào không đúng khuôn tên thì bỏ qua: không phải bản sao lưu của app này.
    .filter((x): x is { muc: File | Directory; ngay: string } => x.ngay !== null)
    .map(({ muc, ngay }) => ({
      ten: muc.name,
      ngay,
      suaLuc: lucGhi(muc),
    }))
    .sort((a, b) => b.ngay.localeCompare(a.ngay));
}

/** Giờ ghi file, đọc từ hệ điều hành. Không đọc được thì để rỗng, màn hình tự ẩn dòng ấy. */
function lucGhi(muc: File | Directory): string {
  const luc = muc instanceof File ? muc.lastModified : null;
  return luc === null ? '' : new Date(luc).toISOString();
}

/**
 * Đọc một bản ra và mở gói. **Chưa ghi xuống máy** — người gọi phải hỏi lại người dùng đã,
 * vì khôi phục là thao tác ghi đè không lùi lại được.
 */
export async function docBan(ten: string): Promise<DuLieuChamCong> {
  return moGoi(await new File(thuMuc(), ten).text()).duLieu;
}

/**
 * Xoá bớt bản cũ, chỉ giữ `SO_BAN_GIU` bản mới nhất.
 *
 * Hỏng thì nuốt lỗi: dọn dẹp là việc phụ, để nó làm hỏng kết quả "đã sao lưu xong" thì người
 * dùng tưởng dữ liệu chưa được ghi trong khi nó đã ghi rồi.
 */
function donBanCu(): void {
  try {
    // Liệt kê đúng một lần rồi xoá theo danh sách ấy: gọi `list()` lần thứ hai giữa lúc đang
    // xoá là hai bức ảnh khác nhau của cùng thư mục.
    const cacMuc = thuMuc().list();
    const canXoa = new Set(banCanXoa(cacMuc.map((muc) => muc.name), SO_BAN_GIU));

    for (const muc of cacMuc) {
      if (canXoa.has(muc.name)) {
        muc.delete();
      }
    }
  } catch {
    // Lần sao lưu sau dọn tiếp.
  }
}
