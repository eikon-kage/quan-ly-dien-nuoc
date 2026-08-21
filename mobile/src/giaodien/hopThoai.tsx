/**
 * Hộp hỏi lại của hệ điều hành — một lớp bọc rất mỏng quanh `Alert.alert`.
 *
 * **Vì sao phải bọc.** react-native-web không cài `Alert`: bảng tương thích của họ ghi đúng
 * chữ "Not started". Trên bản web mà gọi `Alert.alert` thì lời gọi ấy mất tăm — mà một
 * trong những câu hỏi đi qua đây là [hoiGhiDe](./hoiGhiDe.ts), cửa duy nhất của mọi đường
 * khôi phục sổ. Câu hỏi mất tăm ở đúng chỗ ấy nghĩa là ghi đè cả sổ mà không ai hỏi gì.
 *
 * Nói cho đúng: react-native-web **có** xuất `Alert`, nhưng `Alert.alert` của nó là một hàm
 * rỗng — gọi vào không hiện gì mà cũng không báo lỗi. Đó là kiểu hỏng tệ nhất, vì chạy thử
 * mà không bấm đúng đường ấy thì không ai thấy.
 *
 * Bản [hopThoai.web.tsx](./hopThoai.web.tsx) vẽ lại hộp ấy bằng `Modal`. Dáng tham số giữ
 * **đúng của `Alert.alert`** để trên Android và iOS không có gì thay đổi: vẫn hộp của hệ
 * điều hành, vẫn thứ tự nút ấy, và những bài kiểm thử đang soi `Alert.alert` vẫn soi được.
 *
 * **Vì sao file này đuôi `.tsx` dù trong nó không có JSX nào.** Metro chọn file theo đuôi
 * trước rồi mới xét nền tảng: nó thử hết `.web.ts` → `.ts` xong mới sang `.web.tsx` → `.tsx`.
 * Nên nếu để `hopThoai.ts` cạnh `hopThoai.web.tsx` thì **bản máy này thắng cả trên web** —
 * đã dính đúng cái bẫy ấy một lần, và vì `Alert.alert` của web là hàm rỗng nên nó im lặng
 * không báo gì. Hai bản phải cùng đuôi.
 */

import { Alert, AlertButton } from 'react-native';

/** Đúng kiểu nút của `Alert`: `{ text, style?: 'cancel' | 'destructive', onPress? }`. */
export type NutHopThoai = AlertButton;

export function hoi(nhan: string, loi: string, nut: NutHopThoai[]): void {
  Alert.alert(nhan, loi, nut);
}

/**
 * Chỗ treo hộp thoại, đặt một lần ở [App](../../App.tsx). Trên máy thì hệ điều hành tự vẽ
 * hộp nên đây là hàng rỗng — nó chỉ có việc thật trong bản web.
 */
export function ChoHopThoai(): null {
  return null;
}
