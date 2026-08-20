/**
 * Trạng thái sao lưu dùng chung cho cả app, gói lại thành một hook.
 *
 * Sao lưu chạy ngầm, không có nút Lưu — giống hệt cách app ghi xuống bộ nhớ máy. Không phải
 * nối tài khoản nào, không hỏi gì người dùng: bản sao nằm ngay trong máy, cứ đổi dữ liệu là
 * ít phút sau có bản mới của ngày hôm nay.
 *
 * Chạy trên **cả hai vai**. Bản Drive trước đây phải tắt trên máy thợ vì cả nhóm nối chung
 * một tài khoản Google mà tên file chỉ theo ngày — hai máy cùng sao lưu là ghi đè lên nhau.
 * Sao lưu vào máy thì mỗi máy một thư mục riêng, không có chuyện đụng nhau nữa.
 */

import { useCallback, useEffect, useRef, useState } from 'react';
import { Platform } from 'react-native';

import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import * as SaoLuu from '../nghiepvu/saoLuuMay';

/**
 * Đổi xong chờ 20 giây yên tĩnh mới ghi.
 *
 * Chấm công là bấm liên tiếp mấy chục ô một lượt; ghi lại cả sổ theo từng ô thì tốn pin mà
 * kết quả cuối cùng vẫn thế. 20 giây đủ để một lượt chấm xong hẳn.
 */
const CHO_YEN = 20_000;

export interface TrangThaiSaoLuu {
  /** Máy này ghi được file sao lưu không. Bản chạy trên web thì không có thư mục nào để ghi. */
  hoTro: boolean;
  dangChay: boolean;
  /** Lần sao lưu xong gần nhất, dạng ISO. */
  lucCuoi: string | null;
  /** Câu báo lỗi để hiện lên, null là đang êm. */
  loi: string | null;
}

export interface DieuKhienSaoLuu {
  trangThai: TrangThaiSaoLuu;
  saoLuuNgay: () => Promise<void>;
}

export function dungSaoLuu(duLieu: DuLieuChamCong | null): DieuKhienSaoLuu {
  const [dangChay, datDangChay] = useState(false);
  const [lucCuoi, datLucCuoi] = useState<string | null>(null);
  const [loi, datLoi] = useState<string | null>(null);

  const hoTro = Platform.OS !== 'web';

  /**
   * Dữ liệu mới nhất, giữ trong ref chứ không bắt các hàm bên dưới phụ thuộc vào nó — nếu
   * phụ thuộc thì mỗi lần chấm một ô là hẹn giờ bị dựng lại từ đầu.
   */
  const duLieuMoiNhat = useRef(duLieu);
  duLieuMoiNhat.current = duLieu;

  useEffect(() => {
    if (!hoTro) {
      return;
    }
    SaoLuu.lanCuoi().then(datLucCuoi);
  }, [hoTro]);

  const chay = useCallback(async () => {
    const hienTai = duLieuMoiNhat.current;
    if (!hienTai || !hoTro) {
      return;
    }

    datDangChay(true);
    try {
      const ban = await SaoLuu.saoLuu(hienTai, Ngay.homNay());
      datLucCuoi(ban.suaLuc);
      datLoi(null);
    } catch {
      // Ghi hụt gần như chỉ có một lý do: máy hết chỗ. Nói đúng lý do ấy chứ đừng "thử lại
      // sau" — dọn chỗ là việc người dùng làm được, mà không nói thì họ không biết phải dọn.
      datLoi('Chưa ghi được bản sao lưu. Máy có thể đã hết chỗ trống.');
    } finally {
      datDangChay(false);
    }
  }, [hoTro]);

  /**
   * Hẹn giờ ghi sau mỗi lần dữ liệu đổi.
   *
   * Bỏ qua lần chạy đầu: lúc ấy dữ liệu vừa đọc lên từ máy chứ chưa ai sửa gì, sao lưu chỉ
   * để ghi đè đúng cái đang có.
   */
  const daBoQuaLanDau = useRef(false);

  useEffect(() => {
    if (!hoTro || duLieu === null) {
      return;
    }
    if (!daBoQuaLanDau.current) {
      daBoQuaLanDau.current = true;
      return;
    }

    const hen = setTimeout(chay, CHO_YEN);
    return () => clearTimeout(hen);
  }, [duLieu, hoTro, chay]);

  return {
    trangThai: { hoTro, dangChay, lucCuoi, loi },
    saoLuuNgay: chay,
  };
}
