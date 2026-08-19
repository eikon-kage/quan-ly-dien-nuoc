/**
 * Trạng thái nối nhóm chấm công (Supabase), gói lại thành một hook dùng chung cho cả app.
 *
 * Đặt cạnh `dungSaoLuu` và `dungDoiChieu` vì cùng một loại việc: giữ một kết nối cho toàn
 * app chứ không cho riêng màn hình nào. Máy chủ và máy thợ đều cần, mà hai bên lại ở hai
 * màn hình khác nhau — để trong một màn hình thì bên kia không có.
 */

import { useCallback, useEffect, useState } from 'react';
import { AppState } from 'react-native';

import * as Nhom from '../nghiepvu/dangNhapSupabase';
import { LoiDangNhap, TaiKhoanNhom } from '../nghiepvu/dangNhapSupabase';

export interface TrangThaiNhom {
  /** Máy này đã được điền địa chỉ project và khoá công khai chưa. */
  hoTro: boolean;
  /** null = chưa đăng nhập. */
  taiKhoan: TaiKhoanNhom | null;
  dangChay: boolean;
  loi: string | null;
  /** Câu nhắc sau khi tạo tài khoản mà project bắt xác nhận email. */
  nhac: string | null;
}

export interface DieuKhienNhom {
  trangThai: TrangThaiNhom;
  /** Máy thợ: xin tài khoản ẩn danh, không hỏi gì. */
  noiAnDanh: () => Promise<void>;
  noiEmail: (email: string, matKhau: string) => Promise<void>;
  taoTaiKhoan: (email: string, matKhau: string) => Promise<void>;
  ngat: () => Promise<void>;
}

export function dungSupabase(): DieuKhienNhom {
  const [taiKhoan, datTaiKhoan] = useState<TaiKhoanNhom | null>(null);
  const [dangChay, datDangChay] = useState(false);
  const [loi, datLoi] = useState<string | null>(null);
  const [nhac, datNhac] = useState<string | null>(null);

  const hoTro = Nhom.hoTroNoi();

  useEffect(() => {
    if (!hoTro) {
      return;
    }
    Nhom.taiKhoanDaLuu()
      .then(datTaiKhoan)
      // Đọc phiên hụt (thường là hỏng kho) thì coi như chưa đăng nhập, đừng doạ người dùng
      // ngay lúc mở app — họ bấm nối lại là xong.
      .catch(() => datTaiKhoan(null));
  }, [hoTro]);

  /**
   * Vòng tự làm mới token chỉ chạy khi app đang mở.
   *
   * `AppState` nghe ở đây, không nghe trong tầng nghiệp vụ: đây là chuyện vòng đời của giao
   * diện, mà tầng nghiệp vụ thì phải chạy được ngoài máy thật để kiểm thử cho nhanh.
   */
  useEffect(() => {
    if (!hoTro || taiKhoan === null) {
      return;
    }

    Nhom.batTuLamMoiToken();
    const nghe = AppState.addEventListener('change', (trangThai) => {
      if (trangThai === 'active') {
        Nhom.batTuLamMoiToken();
      } else {
        Nhom.tatTuLamMoiToken();
      }
    });

    return () => {
      nghe.remove();
      Nhom.tatTuLamMoiToken();
    };
  }, [hoTro, taiKhoan]);

  /** Gói phần lặp lại: bật cờ đang chạy, dọn lỗi cũ, và dịch lỗi ra câu hiện lên được. */
  const chay = useCallback(async (viec: () => Promise<void>) => {
    datDangChay(true);
    datLoi(null);
    datNhac(null);
    try {
      await viec();
    } catch (loiChay) {
      datLoi(
        loiChay instanceof LoiDangNhap ? loiChay.message : 'Chưa nối được nhóm. Thử lại sau.',
      );
    } finally {
      datDangChay(false);
    }
  }, []);

  const noiAnDanh = useCallback(
    () => chay(async () => datTaiKhoan(await Nhom.dangNhapAnDanh())),
    [chay],
  );

  const noiEmail = useCallback(
    (email: string, matKhau: string) =>
      chay(async () => datTaiKhoan(await Nhom.dangNhapEmail(email, matKhau))),
    [chay],
  );

  const taoTaiKhoan = useCallback(
    (email: string, matKhau: string) =>
      chay(async () => {
        const moi = await Nhom.dangKyEmail(email, matKhau);
        if (moi === null) {
          // Project bắt xác nhận email: nói tiếp cho người dùng biết phải làm gì, chứ đừng
          // để màn hình đứng im như vừa bấm hụt.
          datNhac('Đã gửi thư xác nhận. Mở mail bấm xác nhận rồi quay lại bấm Đăng nhập.');
          return;
        }
        datTaiKhoan(moi);
      }),
    [chay],
  );

  const ngat = useCallback(
    () =>
      chay(async () => {
        await Nhom.dangXuat();
        datTaiKhoan(null);
      }),
    [chay],
  );

  return {
    trangThai: { hoTro, taiKhoan, dangChay, loi, nhac },
    noiAnDanh,
    noiEmail,
    taoTaiKhoan,
    ngat,
  };
}
