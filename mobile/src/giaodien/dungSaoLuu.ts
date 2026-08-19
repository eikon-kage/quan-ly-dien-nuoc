/**
 * Trạng thái sao lưu Drive dùng chung cho cả app, gói lại thành một hook.
 *
 * Sao lưu chạy ngầm, không có nút Lưu — giống hệt cách app ghi xuống bộ nhớ máy. Người
 * dùng nối Drive đúng một lần rồi thôi; từ đó cứ đổi dữ liệu là ít phút sau bản trên
 * Drive tự khớp lại.
 */

import { useCallback, useEffect, useRef, useState } from 'react';

import { ChuaDangNhap, HetPhien, TaiKhoan } from '../nghiepvu/dangNhapGoogle';
import * as Google from '../nghiepvu/dangNhapGoogle';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import * as SaoLuu from '../nghiepvu/saoLuuDrive';

/**
 * Đổi xong chờ 20 giây yên tĩnh mới đẩy lên.
 *
 * Chấm công là bấm liên tiếp mấy chục ô một lượt; đẩy ngay theo từng ô thì tốn pin, tốn
 * 3G của người dùng mà kết quả cuối cùng vẫn thế. 20 giây đủ để một lượt chấm xong hẳn.
 */
const CHO_YEN = 20_000;

export interface TrangThaiSaoLuu {
  /** Máy này có nối Drive được không (Expo Go và web thì không). */
  hoTro: boolean;
  /** null = chưa nối Drive. */
  taiKhoan: TaiKhoan | null;
  dangChay: boolean;
  /** Lần sao lưu xong gần nhất, dạng ISO. */
  lucCuoi: string | null;
  /** Câu báo lỗi để hiện lên, null là đang êm. */
  loi: string | null;
}

export interface DieuKhienSaoLuu {
  trangThai: TrangThaiSaoLuu;
  noiDrive: () => Promise<void>;
  ngatDrive: () => Promise<void>;
  saoLuuNgay: () => Promise<void>;
}

/**
 * `bat` để tắt hẳn sao lưu trên **máy thợ**.
 *
 * Không phải để tiết kiệm: cả nhóm nối chung một tài khoản Google, mà tên file sao lưu chỉ
 * theo ngày ("Cham-cong-2026-08-19.json"). Hai máy cùng sao lưu là ghi đè lên nhau, và bản
 * còn lại trên Drive là của máy bấm sau — mất bản sao lưu của chủ. Sổ máy thợ vốn đã nằm
 * trong hộp thư nên không mất gì.
 */
export function dungSaoLuu(duLieu: DuLieuChamCong | null, bat = true): DieuKhienSaoLuu {
  const [taiKhoan, datTaiKhoan] = useState<TaiKhoan | null>(null);
  const [dangChay, datDangChay] = useState(false);
  const [lucCuoi, datLucCuoi] = useState<string | null>(null);
  const [loi, datLoi] = useState<string | null>(null);

  const hoTro = Google.hoTro() && bat;

  /**
   * Dữ liệu mới nhất, giữ trong ref chứ không bắt các hàm bên dưới phụ thuộc vào nó —
   * nếu phụ thuộc thì mỗi lần chấm một ô là hẹn giờ bị dựng lại từ đầu.
   */
  const duLieuMoiNhat = useRef(duLieu);
  duLieuMoiNhat.current = duLieu;

  useEffect(() => {
    if (!hoTro) {
      return;
    }
    Google.taiKhoanDaLuu().then(datTaiKhoan);
    SaoLuu.lanCuoi().then(datLucCuoi);
  }, [hoTro]);

  const chay = useCallback(async () => {
    const hienTai = duLieuMoiNhat.current;
    if (!hienTai) {
      return;
    }

    datDangChay(true);
    try {
      await SaoLuu.saoLuu(hienTai, Ngay.homNay());
      datLucCuoi(new Date().toISOString());
      datLoi(null);
    } catch (loiChay) {
      if (loiChay instanceof ChuaDangNhap) {
        datTaiKhoan(null);
        datLoi(null);
      } else if (loiChay instanceof HetPhien) {
        datTaiKhoan(null);
        datLoi('Kết nối Google Drive đã hết hạn. Bấm Nối lại.');
      } else {
        // Thường là mất mạng. Không đáng doạ người dùng — lần đổi dữ liệu sau sẽ thử lại.
        datLoi('Chưa đẩy lên Drive được. Sẽ tự thử lại sau.');
      }
    } finally {
      datDangChay(false);
    }
  }, []);

  /**
   * Hẹn giờ đẩy lên sau mỗi lần dữ liệu đổi.
   *
   * Bỏ qua lần chạy đầu: lúc ấy dữ liệu vừa đọc lên từ máy chứ chưa ai sửa gì, sao lưu
   * chỉ để ghi đè đúng cái đang có trên Drive.
   */
  const daBoQuaLanDau = useRef(false);

  useEffect(() => {
    if (!hoTro || !taiKhoan || duLieu === null) {
      return;
    }
    if (!daBoQuaLanDau.current) {
      daBoQuaLanDau.current = true;
      return;
    }

    const hen = setTimeout(chay, CHO_YEN);
    return () => clearTimeout(hen);
  }, [duLieu, hoTro, taiKhoan, chay]);

  const noiDrive = useCallback(async () => {
    datLoi(null);
    try {
      const moi = await Google.dangNhap();
      if (moi) {
        datTaiKhoan(moi);
        // Đẩy luôn bản đầu tiên. Nối xong mà Drive vẫn trống thì người dùng tưởng hỏng —
        // mà đợi tới lần chấm công sau mới có bản đầu thì đúng là đang hở thật.
        await chay();
      }
    } catch {
      datLoi('Chưa nối được với Google. Kiểm tra mạng rồi thử lại.');
    }
  }, [chay]);

  const ngatDrive = useCallback(async () => {
    await Google.dangXuat();
    datTaiKhoan(null);
    datLoi(null);
  }, []);

  return {
    trangThai: { hoTro, taiKhoan, dangChay, lucCuoi, loi },
    noiDrive,
    ngatDrive,
    saoLuuNgay: chay,
  };
}
