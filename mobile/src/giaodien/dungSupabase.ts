/**
 * Trạng thái nối nhóm chấm công (Supabase), gói lại thành một hook dùng chung cho cả app.
 *
 * Đặt cạnh `dungSaoLuu` và `dungDoiChieu` vì cùng một loại việc: giữ một kết nối cho toàn
 * app chứ không cho riêng màn hình nào. Máy chủ và máy thợ đều cần, mà hai bên lại ở hai
 * màn hình khác nhau — để trong một màn hình thì bên kia không có.
 */

import { useCallback, useEffect, useRef, useState } from 'react';
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
  /**
   * Lượt nối lúc mở app còn đang chạy. Chưa xong thì **chưa được kết luận là chưa nối** —
   * màn hình mở đầu đợi cờ này tắt, kẻo máy đã nối vẫn bị hỏi đăng nhập một nhịp.
   */
  dangDoc: boolean;
  /**
   * Lúc mở app không tra được nhóm (mất mạng, database chưa dựng bảng). Khác hẳn `thanhVien
   * === null`: một cái là *biết* chưa vào nhóm, một cái là *không biết*.
   *
   * Phân biệt ra để màn hình mở đầu đừng đòi nối lại khi máy chỉ đang mất mạng — máy chủ
   * ngoài vùng phủ sóng vẫn phải mở app ra chấm công được, không phải nhìn màn hình đăng nhập.
   */
  traHut: boolean;
  dangChay: boolean;
  loi: string | null;
  /** Câu nhắc sau khi tạo tài khoản mà project bắt xác nhận email. */
  nhac: string | null;
}

export interface DieuKhienNhom {
  trangThai: TrangThaiNhom;
  /**
   * Máy chủ: đăng nhập bằng email. **Không có đường ẩn danh cho chủ** — tài khoản chủ nắm
   * nhóm của cả cửa hàng, mà tài khoản ẩn danh chỉ sống trong một cái điện thoại.
   *
   * Máy thợ không dùng hai hàm này: nó vào nhóm bằng `doiMa`, và chính `doiMa` xin tài khoản
   * ẩn danh giúp.
   */
  noiEmail: (email: string, matKhau: string) => Promise<void>;
  taoTaiKhoan: (email: string, matKhau: string) => Promise<void>;
  /** Đã đăng nhập nhưng chưa vào nhóm thì thử lại riêng bước lập nhóm. */
  lapNhom: () => Promise<void>;
  /** Máy chủ: phát mã mời cho một thợ. `null` là hụt, câu lỗi đã nằm ở `trangThai.loi`. */
  phatMa: (thoId: string) => Promise<string | null>;
  /**
   * Máy thợ: đổi mã mời lấy chỗ trong nhóm.
   *
   * Trả về cả `thoId` vì đó là **thứ máy thợ cần nhất**: id của nó trong sổ chủ. Nhờ vậy thợ
   * chỉ phải nhập đúng một mã cho cả hai việc — vào nhóm, và biết mình là ai trong sổ chủ.
   */
  doiMa: (ma: string) => Promise<ThanhVien | null>;
  ngat: () => Promise<void>;
}

/**
 * `vai` để biết sau khi đăng nhập thì làm gì tiếp: máy chủ lập nhóm luôn, còn máy thợ phải
 * đợi mã mời của chủ nên không tự vào nhóm nào được.
 */
export function dungSupabase(vai: Vai | null): DieuKhienNhom {
  const [taiKhoan, datTaiKhoan] = useState<TaiKhoanNhom | null>(null);
  const [thanhVien, datThanhVien] = useState<ThanhVien | null>(null);
  const [dangChay, datDangChay] = useState(false);
  const [loi, datLoi] = useState<string | null>(null);
  const [nhac, datNhac] = useState<string | null>(null);
  const [traHut, datTraHut] = useState(false);

  const hoTro = DangNhap.hoTroNoi();

  /** Máy không nối được thì chẳng có gì phải đợi — đừng để màn hình mở đầu treo mãi. */
  const [dangDoc, datDangDoc] = useState(hoTro);

  /** Lượt nối lúc mở app chỉ được chạy **một lần** cho cả đời app, kể cả khi `vai` đổi. */
  const daMo = useRef(false);

  useEffect(() => {
    /*
      `vai === null` là *chưa đọc xong* vai máy, không phải máy chủ. Phải đợi: đoạn dưới lập
      nhóm giúp máy chủ, mà lập nhóm cho một máy thợ là đặt nó vào một nhóm một người — sổ nó
      gửi lên không ai nhận, mà mã mời của chủ sau đó cũng không đổi được nữa vì máy đã có nhóm.
    */
    if (!hoTro || vai === null || daMo.current) {
      return;
    }
    daMo.current = true;

    /**
     * Nối ngay lúc mở app, không đợi người dùng vào mục nào bấm nút nào: đọc phiên đăng nhập
     * đã lưu, rồi tra nhóm — và nếu là máy chủ đã đăng nhập mà chưa có nhóm thì lập luôn.
     *
     * Trước đây đoạn này chỉ *đọc*, nên máy chủ đăng nhập rồi mà lượt lập nhóm hụt (mất mạng,
     * bảng chưa dựng) thì mỗi lần mở app lại phải mò vào **Thợ → Nhóm chấm công** bấm "Lập
     * nhóm, thử lại". `tao_nhom` gọi mấy lần cũng ra đúng một nhóm, nên gọi thẳng là an toàn.
     *
     * Hai việc, **hai lần bắt lỗi riêng**: đọc phiên đăng nhập, rồi tra xem đã ở nhóm nào.
     *
     * Gộp vào một `catch` là một lỗi thật đã bắt được lúc chạy trên máy: database chưa dựng
     * bảng nên việc tra nhóm hỏng, và nó xoá luôn trạng thái đăng nhập — app đòi đăng nhập
     * lại trong khi phiên vẫn còn nguyên trong máy. Người dùng bấm nối mãi không xong mà
     * chẳng hiểu vì sao.
     */
    (async () => {
      // `finally` chứ không đặt ở cuối thân hàm: giữa đường có mấy chỗ `return`, mà bỏ sót
      // một chỗ là màn hình mở đầu đợi mãi không hiện.
      try {
        let co: TaiKhoanNhom | null = null;
        try {
          co = await DangNhap.taiKhoanDaLuu();
          datTaiKhoan(co);
        } catch (loiDoc) {
          // Đọc phiên hụt (thường là hỏng kho) thì coi như chưa đăng nhập, đừng doạ người dùng
          // ngay lúc mở app — họ bấm nối lại là xong. Bản dev thì phải in ra: lỗi bị nuốt ở
          // đây thì triệu chứng duy nhất là "app tự nhiên đòi đăng nhập lại".
          if (__DEV__) {
            console.warn('Đọc phiên hụt:', loiDoc);
          }
          datTaiKhoan(null);
          return;
        }

        if (!co) {
          return;
        }

        try {
          const dangCo = await Nhom.thanhVienCuaToi();
          datThanhVien(dangCo ?? (vai === 'chu' ? await Nhom.taoNhom() : null));
        } catch (loiNhom) {
          // Chưa tra được nhóm thì vẫn giữ nguyên trạng thái đăng nhập, và giao diện hiện
          // "Đã đăng nhập, chưa vào nhóm" kèm nút thử lại.
          if (__DEV__) {
            console.warn('Tra nhóm hụt:', loiNhom);
          }
          datThanhVien(null);
          datTraHut(true);
        }
      } finally {
        datDangDoc(false);
      }
    })();
  }, [hoTro, vai]);

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
      /*
        Bản dev in **câu gốc của Supabase**, không chỉ câu đã dịch.

        Câu dịch cố tình gọn cho người dùng đọc, nên lúc gỡ lỗi nó che mất nguyên nhân: một
        lỗi không khớp khuôn nào cũng ra "Chưa nối được nhóm chấm công. Thử lại sau." Câu gốc
        mới nói ra đang vướng gì — "Signups not allowed for this instance", "Email signups are
        disabled", "email rate limit exceeded".
      */
      if (__DEV__) {
        const goc =
          loiChay instanceof LoiDangNhap || loiChay instanceof LoiNhom ? loiChay.goc : undefined;
        console.warn('Nối nhóm hụt:', loiChay, goc !== undefined ? `\n  Supabase: ${goc}` : '');
      }
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
    // Tra lại được rồi thì bỏ cờ "không biết" — tới đây câu trả lời là câu mới.
    datTraHut(false);
    if (vai === 'chu') {
      datThanhVien(await Nhom.taoNhom());
    } else {
      // Máy thợ vào nhóm bằng mã mời của chủ, không tự vào được.
      datThanhVien(await Nhom.thanhVienCuaToi());
    }
  }, [vai]);

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

  const lapNhom = useCallback(() => chay(vaoNhom), [chay, vaoNhom]);

  /**
   * Giữ kết quả trong một hộp chứ không trả thẳng từ `chay`: `chay` nuốt lỗi thành câu hiện
   * lên màn hình, nên bên gọi chỉ cần biết "được hay không được".
   */
  const phatMa = useCallback(
    async (thoId: string) => {
      const hop: { ma: string | null } = { ma: null };
      await chay(async () => {
        hop.ma = await Nhom.phatMaMoi(thoId);
      });
      return hop.ma;
    },
    [chay],
  );

  const doiMa = useCallback(
    async (ma: string) => {
      const hop: { thanhVien: ThanhVien | null } = { thanhVien: null };
      await chay(async () => {
        /*
          Chưa đăng nhập thì xin tài khoản ẩn danh ngay trong cùng lần bấm ấy: thợ chỉ có một
          cái mã trong tay, không email, không mật khẩu.

          Đã có tài khoản rồi thì dùng lại, tuyệt đối không xin thêm một tài khoản ẩn danh
          nữa — hàng `thanh_vien` gắn với tài khoản, nên tài khoản mới là bỏ rơi chỗ cũ trong
          nhóm và để lại một người dùng chết trong database.
        */
        if (taiKhoan === null) {
          datTaiKhoan(await DangNhap.dangNhapAnDanh());
        }
        hop.thanhVien = await Nhom.doiMaMoi(ma);
        datThanhVien(hop.thanhVien);
      });
      return hop.thanhVien;
    },
    [chay, taiKhoan],
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
    trangThai: { hoTro, taiKhoan, thanhVien, dangDoc, traHut, dangChay, loi, nhac },
    noiEmail,
    taoTaiKhoan,
    lapNhom,
    phatMa,
    doiMa,
    ngat,
  };
}
