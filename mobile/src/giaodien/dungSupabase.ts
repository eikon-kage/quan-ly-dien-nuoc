/**
 * Trạng thái nối nhóm chấm công (Supabase), gói lại thành một hook dùng chung cho cả app.
 *
 * Đặt cạnh `dungSaoLuu` và `dungDoiChieu` vì cùng một loại việc: giữ một kết nối cho toàn
 * app chứ không cho riêng màn hình nào. Máy chủ và máy thợ đều cần, mà hai bên lại ở hai
 * màn hình khác nhau — để trong một màn hình thì bên kia không có.
 */

import { useCallback, useEffect, useState } from 'react';
import { AppState } from 'react-native';

import * as DangNhap from '../nghiepvu/dangNhapSupabase';
import { LoiDangNhap, TaiKhoanNhom } from '../nghiepvu/dangNhapSupabase';
import * as Nhom from '../nghiepvu/nhomSupabase';
import { LoiNhom, ThanhVien } from '../nghiepvu/nhomSupabase';
import { Vai } from '../nghiepvu/soCong';

export interface TrangThaiNhom {
  /** Máy này đã được điền địa chỉ project và khoá công khai chưa. */
  hoTro: boolean;
  /** null = chưa đăng nhập. */
  taiKhoan: TaiKhoanNhom | null;
  /** Máy này đã ở trong nhóm nào chưa. Chưa vào nhóm thì đăng nhập rồi cũng chưa gửi sổ được. */
  thanhVien: ThanhVien | null;
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

/**
 * `vai` để biết sau khi đăng nhập thì làm gì tiếp: máy chủ lập nhóm luôn, còn máy thợ phải
 * đợi mã mời của chủ nên không tự vào nhóm nào được.
 */
export function dungSupabase(vai: Vai): DieuKhienNhom {
  const [taiKhoan, datTaiKhoan] = useState<TaiKhoanNhom | null>(null);
  const [thanhVien, datThanhVien] = useState<ThanhVien | null>(null);
  const [dangChay, datDangChay] = useState(false);
  const [loi, datLoi] = useState<string | null>(null);
  const [nhac, datNhac] = useState<string | null>(null);

  const hoTro = DangNhap.hoTroNoi();

  useEffect(() => {
    if (!hoTro) {
      return;
    }
    DangNhap.taiKhoanDaLuu()
      .then(async (co) => {
        datTaiKhoan(co);
        if (co) {
          datThanhVien(await Nhom.thanhVienCuaToi());
        }
      })
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

    DangNhap.batTuLamMoiToken();
    const nghe = AppState.addEventListener('change', (trangThai) => {
      if (trangThai === 'active') {
        DangNhap.batTuLamMoiToken();
      } else {
        DangNhap.tatTuLamMoiToken();
      }
    });

    return () => {
      nghe.remove();
      DangNhap.tatTuLamMoiToken();
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
        loiChay instanceof LoiDangNhap || loiChay instanceof LoiNhom
          ? loiChay.message
          : 'Chưa nối được nhóm. Thử lại sau.',
      );
    } finally {
      datDangChay(false);
    }
  }, []);

  /**
   * Đăng nhập xong thì máy chủ lập nhóm ngay trong cùng một lần bấm.
   *
   * Không tách thành hai nút: với người dùng, "nối vào nhóm" là một việc. Đăng nhập xong mà
   * chưa có nhóm thì bấm đồng bộ sẽ báo "máy này chưa ở trong nhóm nào" — đúng nhưng vô lý,
   * vì họ vừa bấm nối xong.
   */
  const vaoNhom = useCallback(async () => {
    if (vai === 'chu') {
      datThanhVien(await Nhom.taoNhom());
    } else {
      // Máy thợ vào nhóm bằng mã mời của chủ, không tự vào được.
      datThanhVien(await Nhom.thanhVienCuaToi());
    }
  }, [vai]);

  const noiAnDanh = useCallback(
    () =>
      chay(async () => {
        datTaiKhoan(await DangNhap.dangNhapAnDanh());
        await vaoNhom();
      }),
    [chay, vaoNhom],
  );

  const noiEmail = useCallback(
    (email: string, matKhau: string) =>
      chay(async () => {
        datTaiKhoan(await DangNhap.dangNhapEmail(email, matKhau));
        await vaoNhom();
      }),
    [chay, vaoNhom],
  );

  const taoTaiKhoan = useCallback(
    (email: string, matKhau: string) =>
      chay(async () => {
        const moi = await DangNhap.dangKyEmail(email, matKhau);
        if (moi === null) {
          // Project bắt xác nhận email: nói tiếp cho người dùng biết phải làm gì, chứ đừng
          // để màn hình đứng im như vừa bấm hụt.
          datNhac('Đã gửi thư xác nhận. Mở mail bấm xác nhận rồi quay lại bấm Đăng nhập.');
          return;
        }
        datTaiKhoan(moi);
        await vaoNhom();
      }),
    [chay, vaoNhom],
  );

  const ngat = useCallback(
    () =>
      chay(async () => {
        await DangNhap.dangXuat();
        datTaiKhoan(null);
        datThanhVien(null);
      }),
    [chay],
  );

  return {
    trangThai: { hoTro, taiKhoan, thanhVien, dangChay, loi, nhac },
    noiAnDanh,
    noiEmail,
    taoTaiKhoan,
    ngat,
  };
}
