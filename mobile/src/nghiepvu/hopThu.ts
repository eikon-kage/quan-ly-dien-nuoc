/**
 * Hộp thư: chỗ hai máy đặt sổ của mình vào và lấy sổ bên kia ra.
 *
 * **Cố ý là một lớp mỏng có thể thay ruột.** Ruột hiện tại là Google Drive, dùng chung một
 * tài khoản cho cả nhóm — nhanh, không phải dựng máy chủ, nhưng không chặn được ai đọc của
 * ai. Sau này chuyển sang máy chủ có phân quyền thật thì viết một `HopThu` khác và chỉ đổi
 * đúng một dòng nơi tạo ra nó; màn hình đối chiếu không phải sửa gì.
 *
 * Vì vậy giao diện dưới đây chỉ nói bằng lời của việc chấm công — gửi sổ, đọc sổ — không
 * hé một chữ nào về Drive, file hay token.
 */

import * as Drive from './goiDrive';
import { dongGoiSo, moGoiSo } from './goiSo';
import { voiToken } from './phienDrive';
import { SoCong, Vai } from './soCong';

/** Một sổ lấy từ hộp thư, kèm lúc bên kia đặt vào. */
export interface SoDaNhan {
  so: SoCong;
  /** Lúc hộp thư ghi nhận lần đặt cuối, dạng ISO. */
  suaLuc: string;
}

export interface HopThu {
  /** Đặt sổ của máy này vào hộp thư, ghi đè lên sổ cũ của chính nó. */
  gui(so: SoCong): Promise<void>;
  /** Lấy sổ của một thợ do bên `nguon` gửi. Chưa có thì trả null. */
  doc(thoId: string, nguon: Vai): Promise<SoDaNhan | null>;
  /** Máy chủ dùng: lấy sổ của **mọi** thợ đã gửi lên. */
  docSoCacTho(): Promise<SoDaNhan[]>;
}

/**
 * Tên file trong hộp thư. Mỗi (bên gửi, thợ) đúng một file, ghi đè mãi lên nó.
 *
 * Không để mỗi lần gửi một file mới: sổ là bản chụp toàn khoảng, bản mới nói đủ những gì
 * bản cũ nói. Giữ lịch sử ở đây chỉ làm hộp thư đầy dần rồi phải đi dọn — mà lịch sử thì
 * đã có bên sao lưu theo ngày lo rồi.
 */
export function tenFileSo(nguon: Vai, thoId: string): string {
  return `Cham-cong-so-${nguon}-${thoId}.json`;
}

/** Đọc lại tên file. Không đúng khuôn thì trả null — file lạ, đừng đoán. */
export function docTenFileSo(ten: string): { nguon: Vai; thoId: string } | null {
  const khop = /^Cham-cong-so-(chu|tho)-([A-Za-z0-9_-]+)\.json$/.exec(ten);
  return khop ? { nguon: khop[1] as Vai, thoId: khop[2] } : null;
}

export function hopThuDrive(): HopThu {
  /** Tra id file theo tên. Drive không tra được theo tên nên phải liệt kê rồi tự khớp. */
  async function timFile(token: string, ten: string): Promise<Drive.FileDrive | undefined> {
    return (await Drive.danhSach(token)).find((file) => file.ten === ten);
  }

  return {
    async gui(so) {
      const ten = tenFileSo(so.nguon, so.thoId);
      const noiDung = dongGoiSo(so);

      await voiToken(async (token) => {
        const daCo = await timFile(token, ten);
        return daCo
          ? Drive.ghiDe(token, daCo.id, noiDung)
          : Drive.taoFile(token, ten, noiDung);
      });
    },

    async doc(thoId, nguon) {
      const ten = tenFileSo(nguon, thoId);

      return voiToken(async (token) => {
        const file = await timFile(token, ten);
        if (!file) {
          return null;
        }
        return { so: moGoiSo(await Drive.taiVe(token, file.id)), suaLuc: file.suaLuc };
      });
    },

    async docSoCacTho() {
      return voiToken(async (token) => {
        const cacFile = (await Drive.danhSach(token)).filter(
          (file) => docTenFileSo(file.ten)?.nguon === 'tho',
        );

        const daNhan: SoDaNhan[] = [];
        for (const file of cacFile) {
          try {
            daNhan.push({ so: moGoiSo(await Drive.taiVe(token, file.id)), suaLuc: file.suaLuc });
          } catch {
            // Một sổ hỏng thì bỏ qua sổ ấy, đừng làm cả màn hình đối chiếu trắng xoá.
            // Thợ đó vẫn hiện là "chưa gửi sổ", nhìn ra ngay là có chuyện.
          }
        }
        return daNhan;
      });
    },
  };
}
