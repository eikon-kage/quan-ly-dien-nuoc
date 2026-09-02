/**
 * Vỏ chung của mấy màn hình **mở đè lên** màn hình chính: chi tiết một thợ, chi tiết kỳ,
 * quyết toán, nhập từ Excel. Cửa sổ trượt lên từ đáy, bấm nút quay lại của máy là đóng.
 *
 * Gom một chỗ vì **phần khó là lề an toàn**, và bốn chỗ đều phải xử như nhau — y như
 * [HopDay](./HopDay.tsx) gom chuyện bàn phím của mấy hộp trượt.
 *
 * `Modal` của React Native là **một cửa sổ khác của hệ điều hành**, nằm ngoài cây view mà
 * `SafeAreaProvider` của App đo. Bên trong nó, `SafeAreaView` nhận lề bằng 0 nên đầu trang
 * chạy tọt lên nằm dưới đồng hồ và cột sóng — tên thợ bị đồng hồ đè mất một nửa. Bản cũ
 * bốn màn này đều tự viết `Modal` + `SafeAreaView` và đều dính.
 *
 * Chữa bằng **một `SafeAreaProvider` nữa đặt ngay trong cửa sổ**: nó tự đo cửa sổ ấy, nên
 * `SafeAreaView` bên trong lại có số thật. Đây cũng là cách thư viện dặn dùng cho `Modal`.
 * `initialMetrics` mồi sẵn số đo của cửa sổ chính để khung frame đầu tiên đã đúng lề: đo
 * xong mới biết thì đầu trang nhảy xuống một cái ngay khi màn hình vừa hiện.
 *
 * Hai màn hình khác — [sổ công của tôi](./ManHinhSoCuaToi.tsx) và
 * [đối chiếu](./ManHinhDoiChieu.tsx) — chữa cùng chuyện này theo đường khác: vẽ đè thẳng
 * lên chỗ của màn hình chính, không mở cửa sổ nào cả. Đường ấy vẫn đúng, nhưng nó bắt màn
 * hình gọi phải nhường hẳn chỗ; bốn màn ở đây mở ra từ nhiều nơi nên giữ `Modal`.
 */

import { ReactNode } from 'react';
import { Modal, StyleSheet } from 'react-native';
import { SafeAreaProvider, SafeAreaView, initialWindowMetrics } from 'react-native-safe-area-context';

import { Mau } from './thietKe';

export function ManHinhDe({ children, onDong }: { children: ReactNode; onDong: () => void }) {
  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaProvider initialMetrics={initialWindowMetrics}>
        <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
          {children}
        </SafeAreaView>
      </SafeAreaProvider>
    </Modal>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
});
