/**
 * Trạng thái sao lưu **lên tài khoản của chủ**, gói lại thành một hook dùng chung cho cả app.
 *
 * Đặt cạnh `dungSaoLuu` (bản trong máy) chứ không gộp vào nó, vì hai đường chống hai chuyện
 * khác nhau và có hai điều kiện khác nhau: bản trong máy chạy trên mọi máy, không cần mạng,
 * không cần tài khoản; bản trên tài khoản chỉ chạy khi chủ đã đăng nhập bằng email, và nó là
 * đường duy nhất chống được **mất máy**. Xem [saoLuuTaiKhoan](../nghiepvu/saoLuuTaiKhoan.ts).
 *
 * ---
 *
 * ĐIỀU QUAN TRỌNG NHẤT Ở FILE NÀY: **không bao giờ đẩy một sổ trống lên tài khoản khi trên đó
 * đang có bản.**
 *
 * Đây chính là cái bẫy của việc "cho sổ theo tài khoản". Chủ đăng nhập trên máy mới: sổ trong
 * máy trống, mà đẩy tự động thì chạy ngầm sau vài phút — hai phút sau khi đăng nhập, bản sổ
 * thật trên tài khoản bị một sổ trống ghi đè, và người dùng không hề bấm gì cả. Giữ bản theo
 * ngày đỡ được một phần (bản hôm qua còn nguyên), nhưng công của hôm nay thì mất.
 *
 * Nên lượt đẩy phải đợi **biết chắc** một trong ba điều, và đây là toàn bộ luật:
 *
 *   1. Máy này lúc mở app đã có sổ sẵn — sổ của chính nó, bản trên tài khoản chỉ là bản cũ.
 *   2. Trên tài khoản chưa có bản nào — không có gì để mất.
 *   3. Người dùng đã trả lời câu hỏi *lấy sổ trên tài khoản về?* — lấy về, hay tự nói là không.
 *
 * Chưa đọc được danh sách bản (mất mạng, bảng chưa dựng) thì **không đẩy**, cùng một lẽ với
 * `traHut` bên [dungSupabase](./dungSupabase.ts): *không biết* khác hẳn *biết là chưa có*.
 */

import { useCallback, useEffect, useRef, useState } from 'react';

import { DuLieuChamCong, soTrong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { BanTaiKhoan, KhoTaiKhoan } from '../nghiepvu/saoLuuTaiKhoan';

/**
 * Đổi xong chờ 2 phút yên tĩnh mới đẩy.
 *
 * Lâu hơn hộp thư đối chiếu (3 giây) vì mỗi lượt đẩy là **cả sổ**, không phải sổ của một thợ:
 * chấm cho mười thợ trong một buổi thì chờ dài hơn để cả lượt ấy gói vào một lần gọi mạng.
 * Đừng nới thêm nữa — người chấm xong rồi tắt app đi luôn là chuyện thường, mà chờ quá lâu thì
 * lượt ấy không kịp chạy. Phần còn thiếu đã có bản trong máy (20 giây) đỡ.
 */
const CHO_YEN = 120_000;

export interface TrangThaiSaoLuuTaiKhoan {
  /** Máy này có đường sao lưu lên tài khoản không: cần chủ đã đăng nhập bằng email. */
  hoTro: boolean;
  /**
   * Lượt đọc danh sách bản lúc mở app còn đang chạy. Chưa xong thì **chưa được kết luận là
   * tài khoản không có bản nào** — màn hình mời lấy sổ về phải đợi cờ này tắt.
   */
  dangDoc: boolean;
  dangChay: boolean;
  /** Lần đẩy xong gần nhất trong lượt mở app này, dạng ISO. */
  lucCuoi: string | null;
  loi: string | null;
  /** Các bản đang có trên tài khoản, mới nhất đứng đầu. `null` = chưa đọc được. */
  cacBan: BanTaiKhoan[] | null;
  /**
   * Bản đáng mời người dùng lấy về: máy này chưa có sổ mà trên tài khoản thì có. `null` là
   * không có gì phải mời — chưa đọc được cũng là `null`, vì mời dựa trên một câu chưa biết
   * chắc thì thà đừng mời.
   */
  banChoLay: BanTaiKhoan | null;
}

export interface DieuKhienSaoLuuTaiKhoan {
  trangThai: TrangThaiSaoLuuTaiKhoan;
  /** Đẩy ngay, không đợi hết giờ chờ yên. */
  dayNgay: () => Promise<void>;
  /** Đọc một bản ra. Chưa ghi xuống máy — bên gọi phải hỏi lại người dùng đã. */
  docBan: (ngay: string) => Promise<DuLieuChamCong>;
  /**
   * Người dùng đã trả lời câu *lấy sổ trên tài khoản về?* — dù trả lời có hay không.
   *
   * Chỉ nhớ trong lượt mở app này, **không ghi xuống máy**: sổ vẫn trống thì lần mở sau hỏi
   * lại là đúng, vì câu hỏi ấy vẫn còn nguyên giá trị. Ghi xuống máy là một cú bấm nhầm khiến
   * người dùng không bao giờ được mời lấy sổ về nữa.
   */
  daTraLoi: () => void;
}

export function dungSaoLuuTaiKhoan(
  duLieu: DuLieuChamCong | null,
  kho: KhoTaiKhoan,
  /** Máy chủ đã đăng nhập bằng email. Máy thợ ẩn danh không dùng đường này — xem saoLuuTaiKhoan.ts. */
  duocDung: boolean,
): DieuKhienSaoLuuTaiKhoan {
  const hoTro = duocDung && kho.hoTro();

  const [cacBan, datCacBan] = useState<BanTaiKhoan[] | null>(null);
  const [dangDoc, datDangDoc] = useState(false);
  const [dangChay, datDangChay] = useState(false);
  const [lucCuoi, datLucCuoi] = useState<string | null>(null);
  const [loi, datLoi] = useState<string | null>(null);
  const [daHoi, datDaHoi] = useState(false);

  /** Dữ liệu mới nhất giữ trong ref, để hàm đẩy không phải dựng lại mỗi lần chấm một ô. */
  const moiNhat = useRef(duLieu);
  moiNhat.current = duLieu;

  /**
   * Sổ trong máy lúc mở app đã có gì chưa — chốt đúng **một lần**, ở lần đầu đọc xong dữ liệu.
   *
   * Phải chốt lúc ấy chứ không đọc `soTrong(duLieu)` mỗi lượt: người dùng gõ một dòng vào sổ
   * trống là `soTrong` thành false, và nếu lấy nó làm điều kiện thì máy mới lại được phép đẩy
   * sổ một dòng ấy lên đè bản thật. Ở đây câu hỏi là *cái máy này có sổ riêng của nó hay
   * không*, và câu trả lời không đổi giữa lượt mở app.
   */
  const coSoTuDau = useRef<boolean | null>(null);
  if (coSoTuDau.current === null && duLieu !== null) {
    coSoTuDau.current = !soTrong(duLieu);
  }

  /** Dấu của sổ đã đẩy lần trước: nội dung y hệt thì đừng gọi mạng lại. */
  const dauDaDay = useRef<string | null>(null);

  const daQuyetDinh =
    coSoTuDau.current === true || daHoi || (cacBan !== null && cacBan.length === 0);

  /** Toàn bộ luật ở đầu file gói lại đúng một dòng. */
  const duocDay =
    hoTro && duLieu !== null && !soTrong(duLieu) && cacBan !== null && daQuyetDinh;

  /**
   * Bản đáng mời lấy về. Hai điều kiện về sổ trong máy, và **cả hai đều cần**:
   *
   *   `coSoTuDau === false` — máy này mở app lên với sổ trống, tức nó không có sổ riêng.
   *   `soTrong(duLieu)`     — và **ngay lúc này** vẫn còn trống.
   *
   * Thiếu điều kiện thứ hai là một lỗi đã bắt được lúc chạy trên máy thật: người dùng thêm
   * một thợ vào sổ trống, lượt đẩy ngầm gửi sổ ấy lên tài khoản, `cacBan` từ rỗng thành có
   * một bản — và app quay lại mời chính nó lấy về **cái nó vừa ghi**, kèm câu "máy này chưa
   * có buổi công nào" trong lúc trên máy đã có thợ. Chắn ngang sau khi người dùng đã bắt đầu
   * gõ cũng là chắn sai chỗ: tới đó máy này đã có sổ riêng, muốn lấy bản trên tài khoản thì
   * vào Thợ → Sao lưu, ở đó có cả danh sách.
   */
  const banChoLay =
    hoTro &&
    !daHoi &&
    coSoTuDau.current === false &&
    duLieu !== null &&
    soTrong(duLieu) &&
    cacBan !== null &&
    cacBan.length > 0
      ? cacBan[0]
      : null;

  /** Đọc danh sách bản. Hụt thì để `cacBan` là null — *không biết*, chứ không phải *không có*. */
  const docDanhSach = useCallback(async () => {
    datDangDoc(true);
    try {
      datCacBan(await kho.danhSach());
    } catch (loiChay) {
      datCacBan(null);
      datLoi(cauLoi(loiChay));
    } finally {
      datDangDoc(false);
    }
  }, [kho]);

  const daDoc = useRef(false);

  useEffect(() => {
    if (!hoTro || daDoc.current) {
      return;
    }
    daDoc.current = true;
    docDanhSach();
  }, [hoTro, docDanhSach]);

  const day = useCallback(async () => {
    const hienTai = moiNhat.current;
    if (hienTai === null || !hoTro) {
      return;
    }

    /*
      Sổ trống thì không đẩy, kể cả khi người dùng bấm thẳng vào nút.

      Nút *Sao lưu lên tài khoản ngay* nằm ngay trong màn hình Sao lưu, mà máy mới đăng nhập
      xong thì màn hình ấy là chỗ người ta mở ra đầu tiên để tìm sổ cũ. Bấm một cái là xoá
      đúng thứ mình đang đi tìm. Không có cảnh nào mà đẩy sổ trống lên là việc đúng: muốn dọn
      bản trên tài khoản thì dọn trong SQL Editor (xem supabase/xoa-du-lieu.sql).
    */
    if (soTrong(hienTai)) {
      datLoi('Sổ trên máy này đang trống, chưa đẩy lên để khỏi ghi đè bản đang có trên tài khoản.');
      return;
    }

    datDangChay(true);
    try {
      const ban = await kho.day(hienTai, Ngay.homNay());
      dauDaDay.current = JSON.stringify(hienTai);
      datLucCuoi(ban.suaLuc);
      datLoi(null);
      // Ghép bản vừa đẩy vào danh sách đang giữ, khỏi gọi mạng lần nữa chỉ để biết một thứ
      // mình vừa làm.
      datCacBan((cu) => [ban, ...(cu ?? []).filter((x) => x.ngay !== ban.ngay)]);
    } catch (loiChay) {
      datLoi(cauLoi(loiChay));
    } finally {
      datDangChay(false);
    }
  }, [hoTro, kho]);

  /** Đẩy, nhưng bỏ qua nếu sổ y hệt lần đẩy trước — mỗi lượt đẩy là cả sổ. */
  const dayNeuDoi = useCallback(async () => {
    const hienTai = moiNhat.current;
    if (hienTai !== null && dauDaDay.current === JSON.stringify(hienTai)) {
      return;
    }
    await day();
  }, [day]);

  /**
   * Hôm nay chưa có bản nào trên tài khoản thì đẩy ngay lúc mở app, không đợi người dùng đổi
   * dữ liệu.
   *
   * Không đẩy mỗi lần mở app: cả sổ đi lên, mà chủ mở app năm lần một ngày. Mốc "đã có bản của
   * hôm nay chưa" vừa rẻ vừa đúng thứ cần bảo đảm — mỗi ngày trên tài khoản có một bản.
   */
  const daDayLucMo = useRef(false);

  useEffect(() => {
    if (!duocDay || daDayLucMo.current) {
      return;
    }
    if (cacBan !== null && cacBan.some((ban) => ban.ngay === Ngay.homNay())) {
      return;
    }
    daDayLucMo.current = true;
    dayNeuDoi();
  }, [duocDay, cacBan, dayNeuDoi]);

  /**
   * Chấm xong ngồi im một lát là sổ tự lên tài khoản.
   *
   * Bỏ qua lần chạy đầu như bên sao lưu vào máy: lúc ấy dữ liệu vừa đọc lên từ máy chứ chưa ai
   * sửa gì, mà lượt lúc mở app ở trên đã lo phần đó.
   */
  const daBoQuaLanDau = useRef(false);

  useEffect(() => {
    if (!duocDay) {
      return;
    }
    if (!daBoQuaLanDau.current) {
      daBoQuaLanDau.current = true;
      return;
    }

    const hen = setTimeout(dayNeuDoi, CHO_YEN);
    return () => clearTimeout(hen);
  }, [duLieu, duocDay, dayNeuDoi]);

  const docBan = useCallback((ngay: string) => kho.docBan(ngay), [kho]);

  const daTraLoi = useCallback(() => datDaHoi(true), []);

  return {
    trangThai: { hoTro, dangDoc, dangChay, lucCuoi, loi, cacBan, banChoLay },
    dayNgay: day,
    docBan,
    daTraLoi: daTraLoi,
  };
}

/** Lỗi từ kho đã là câu viết cho người dùng (xem saoLuuTaiKhoanSupabase), nên hiện thẳng. */
function cauLoi(loi: unknown): string {
  return loi instanceof Error && loi.message !== ''
    ? loi.message
    : 'Chưa sao lưu được lên tài khoản. Thử lại sau.';
}
