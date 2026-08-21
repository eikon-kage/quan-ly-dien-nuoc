/**
 * Bản web của [hopThoai](./hopThoai.tsx): tự vẽ hộp hỏi lại, vì `Alert.alert` của
 * react-native-web là **một hàm rỗng** — gọi vào không hiện gì mà cũng không báo lỗi.
 *
 * Vỏ hộp dùng lại [HopDay](./HopDay.tsx) — cùng nền mờ, cùng tay nắm, cùng cách bo góc như
 * mọi hộp khác của app. Đỡ phải dựng lại một kiểu hộp thứ hai, và người dùng thấy đúng thứ
 * họ đã quen.
 *
 * **`hoi()` gọi được từ ngoài React** (trong một `catch`, trong hàm nghiệp vụ), nên câu hỏi
 * đang mở phải nằm ở một chỗ ngoài cây component — đó là `khoCauHoi` — còn `ChoHopThoai` chỉ
 * đứng nghe. Đúng một câu hỏi được mở một lúc: câu mới đè lên câu cũ, y như `Alert` trên máy.
 *
 * **Vì sao `ChoHopThoai` chỉ dựng `HopDay` khi có câu hỏi.** `Modal` của react-native-web
 * gắn một thẻ `div` mới vào cuối `document.body` mỗi lần mở, mà xếp lớp thì lại theo thứ tự
 * trong DOM chứ không theo `z-index`. Dựng muộn nghĩa là nằm trên — nhờ vậy câu hỏi bật ra
 * *từ trong* một hộp đang mở (hộp sao lưu chẳng hạn) vẫn nằm trên hộp ấy chứ không lẩn xuống
 * dưới.
 *
 * `xepNut` và `nutKhiChamRaNgoai` xuất ra để kiểm thử được: chúng là phần *quyết định*, mà
 * phần *vẽ* thì bài kiểm thử không dựng nổi — react-test-renderer không dựng được portal của
 * react-dom mà `Modal` bên web dùng. Xem lời tựa bài [hopThoai.test.tsx](./__tests__/web/hopThoai.test.tsx).
 */

import { useSyncExternalStore } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';

import { HopDay } from './HopDay';
import { Co, Mau, PhongChu } from './thietKe';

/** Giữ đúng dáng `AlertButton` của react-native để hai bản gọi y như nhau. */
export interface NutHopThoai {
  text?: string;
  style?: 'default' | 'cancel' | 'destructive';
  onPress?: (gia?: string) => void;
}

export interface CauHoi {
  nhan: string;
  loi: string;
  nut: NutHopThoai[];
}

let dangHoi: CauHoi | null = null;
const nguoiNghe = new Set<() => void>();

/** Câu hỏi đang mở, nằm ngoài cây component vì `hoi()` gọi được từ bất cứ đâu. */
export const khoCauHoi = {
  dangMo: (): CauHoi | null => dangHoi,

  dat(moi: CauHoi | null): void {
    dangHoi = moi;
    nguoiNghe.forEach((goi) => goi());
  },

  theoDoi(goi: () => void): () => void {
    nguoiNghe.add(goi);
    return () => {
      nguoiNghe.delete(goi);
    };
  },
};

export function hoi(nhan: string, loi: string, nut: NutHopThoai[]): void {
  // Không nút nào thì vẫn phải có một đường ra, kẻo hộp đứng mãi không đóng được.
  khoCauHoi.dat({ nhan, loi, nut: nut.length > 0 ? nut : [{ text: 'Đóng' }] });
}

function bam(n: NutHopThoai): void {
  // Đóng trước rồi mới chạy việc: có việc lại mở tiếp một câu hỏi khác (khôi phục xong hỏi
  // tiếp chẳng hạn), đóng sau thì câu vừa mở bị xoá luôn.
  khoCauHoi.dat(null);
  n.onPress?.();
}

/**
 * Xếp nút *Thôi* xuống cuối.
 *
 * Trên iOS, `Alert` cũng đặt nút huỷ tách khỏi nút việc. Ở đây xếp dọc nên chọn dưới cùng:
 * chỗ ngón tay chạm dễ nhất phải là chỗ **không làm gì**, còn nút ghi đè cả sổ thì phải
 * vươn tay lên mới bấm được.
 */
export function xepNut(nut: NutHopThoai[]): NutHopThoai[] {
  return [...nut].sort((a, b) => Number(a.style === 'cancel') - Number(b.style === 'cancel'));
}

/**
 * Chạm ra nền mờ thì coi như bấm nút nào.
 *
 * Có nút *Thôi* thì là nút ấy. Không có mà chỉ có đúng một nút (mấy câu báo lỗi, nút "Đóng")
 * thì cũng là nút ấy — chạm ra ngoài là ý muốn đóng, mà đóng thì đúng việc của nó. Còn hộp
 * nhiều nút mà không có nút thôi thì **không đoán hộ**: đoán là chọn thay người dùng.
 */
export function nutKhiChamRaNgoai(nut: NutHopThoai[]): NutHopThoai | null {
  const thoi = nut.find((n) => n.style === 'cancel');
  if (thoi !== undefined) {
    return thoi;
  }
  return nut.length === 1 ? nut[0] : null;
}

export function ChoHopThoai() {
  const cauHoi = useSyncExternalStore(
    khoCauHoi.theoDoi,
    khoCauHoi.dangMo,
    () => null,
  );

  if (cauHoi === null) {
    return null;
  }

  const khiChamRaNgoai = () => {
    const n = nutKhiChamRaNgoai(cauHoi.nut);
    if (n !== null) {
      bam(n);
    }
  };

  return (
    <HopDay onDong={khiChamRaNgoai}>
      <Text style={kieu.tieuDe}>{cauHoi.nhan}</Text>
      {cauHoi.loi !== '' && <Text style={kieu.loi}>{cauHoi.loi}</Text>}

      {xepNut(cauHoi.nut).map((n, i) => {
        const chu = n.text ?? 'Đóng';
        const thoi = n.style === 'cancel';
        const nguyHiem = n.style === 'destructive';

        return (
          <Pressable
            key={`${chu}-${i}`}
            style={[kieu.nut, thoi ? kieu.nutThoi : nguyHiem ? kieu.nutXoa : kieu.nutChinh]}
            onPress={() => bam(n)}
            accessibilityRole="button"
          >
            <Text style={[kieu.chuNut, { color: thoi ? Mau.xam : Mau.trang }]}>{chu}</Text>
          </Pressable>
        );
      })}
    </HopDay>
  );
}

const kieu = StyleSheet.create({
  tieuDe: {
    fontSize: Co.chuTieuDe,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    paddingBottom: 2,
  },
  loi: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    lineHeight: 21,
    paddingBottom: 6,
  },
  nut: {
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  nutChinh: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  nutXoa: { backgroundColor: Mau.do, borderColor: Mau.do },
  nutThoi: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
