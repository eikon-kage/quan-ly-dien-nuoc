/**
 * Bản web của [dungCaoBanPhim](./dungCaoBanPhim.ts): đo phần bàn phím ảo đang che màn hình,
 * bằng `window.visualViewport`.
 *
 * **Vì sao phải tự đo.** `KeyboardAvoidingView` của react-native-web nghe `Keyboard` của
 * react-native, mà bên web `Keyboard` là **một cái vỏ rỗng** — không sự kiện nào bắn ra khi
 * bàn phím ảo mở. Nên hộp trượt từ đáy cứ dính đáy: gõ số tiền ứng trên iPhone thì bàn phím
 * bật lên che đúng cái ô đang gõ, người dùng gõ mà không thấy mình gõ gì.
 *
 * **Vì sao là `visualViewport` chứ không phải `window.innerHeight`.** Safari trên iOS **không**
 * co khung trang khi bàn phím mở: `innerHeight` giữ nguyên, chỉ có phần *nhìn thấy được*
 * (`visualViewport`) nhỏ lại và đôi khi bị đẩy trôi lên (`offsetTop`). Hộp thoại của
 * react-native-web thì neo theo khung trang, nên phần bị che tính bằng
 *
 *     innerHeight − visualViewport.height − visualViewport.offsetTop
 *
 * — đúng khoảng cần chừa ở đáy để hộp nằm sát ngay trên bàn phím. Android/Chrome co hẳn khung
 * trang nên hiệu ấy ra gần 0, và cũng đúng: ở đó trình duyệt đã đẩy hộ rồi.
 *
 * **Ngưỡng `NGUONG`** để bỏ qua mấy thay đổi không phải bàn phím: cuộn trang một cái là
 * Safari thu gọn thanh địa chỉ và thanh dưới, `visualViewport` cũng hụt đi vài chục điểm.
 * Không có ngưỡng thì hộp nhấp nhổm theo mỗi cú cuộn. Bàn phím thật bao giờ cũng cao hơn
 * nhiều lần khoảng ấy.
 */

import { useSyncExternalStore } from 'react';

/** Dưới mức này thì coi như bàn phím chưa mở — xem lời tựa. */
const NGUONG = 48;

/** Đủ dáng để tính, và để bài kiểm thử dựng ra một cửa sổ giả. */
interface CuaSo {
  innerHeight: number;
  visualViewport: { height: number; offsetTop: number } | null;
}

/**
 * Phần đáy đang bị bàn phím che, tính theo điểm ảnh CSS. Trình duyệt không có
 * `visualViewport` (bản cũ) thì trả 0 — mất phần đẩy lên, nhưng không hỏng gì thêm.
 */
export function caoBanPhimCua(cua: CuaSo): number {
  const oNhinThay = cua.visualViewport;
  if (oNhinThay === null) {
    return 0;
  }

  const che = Math.round(cua.innerHeight - oNhinThay.height - oNhinThay.offsetTop);
  return che > NGUONG ? che : 0;
}

function doc(): number {
  return typeof window === 'undefined' ? 0 : caoBanPhimCua(window);
}

/**
 * Nghe cả `resize` lẫn `scroll` của `visualViewport`: mở bàn phím là `resize`, còn cú trôi
 * trang mà Safari làm kèm theo lại chỉ bắn `scroll`. Thiếu `scroll` thì hộp lên đúng chỗ rồi
 * lệch lại ngay sau đó.
 */
function nghe(goiLai: () => void): () => void {
  const oNhinThay = typeof window === 'undefined' ? null : window.visualViewport;
  if (oNhinThay === null) {
    return () => {};
  }

  oNhinThay.addEventListener('resize', goiLai);
  oNhinThay.addEventListener('scroll', goiLai);
  return () => {
    oNhinThay.removeEventListener('resize', goiLai);
    oNhinThay.removeEventListener('scroll', goiLai);
  };
}

export function dungCaoBanPhim(): number {
  return useSyncExternalStore(nghe, doc, () => 0);
}
