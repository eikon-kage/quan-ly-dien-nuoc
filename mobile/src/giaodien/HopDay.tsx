/**
 * Vỏ chung của mấy hộp trượt lên từ đáy màn hình: nền mờ, tay nắm, bo hai góc trên.
 *
 * Gom một chỗ vì **phần khó là chuyện bàn phím**, và chỗ nào cũng phải xử như nhau. Lấy
 * đúng cách của `CommonModal` bên trustybot-mobile:
 *
 * 1. `behavior="padding"` cho **cả iOS lẫn Android**, không phân biệt hệ. Bản cũ để Android
 *    là `undefined` cho hệ điều hành tự lo. Không phân biệt vẫn đúng: khi cửa sổ tự co lại
 *    (Android, `adjustResize`) thì `KeyboardAvoidingView` tính ra khoảng đệm bằng 0 nên nó
 *    không đẩy thêm lần nữa; còn khi cửa sổ *không* co — đúng trường hợp hộp này, vì
 *    `statusBarTranslucent` — thì nó là thứ duy nhất đẩy hộp lên.
 * 2. `KeyboardAvoidingView` **phủ kín màn hình** (`absoluteFill`) và dồn nội dung xuống đáy,
 *    chứ không phải chính nó là nền mờ. Nền mờ là một lớp riêng nằm dưới, nhờ vậy bàn phím
 *    đẩy hộp lên mà nền mờ vẫn phủ nguyên cả màn hình.
 * 3. `pointerEvents="box-none"` để chạm vào khoảng trống hai bên hộp vẫn xuyên xuống nền mờ
 *    mà đóng được — không có nó thì lớp phủ kín ăn hết mọi cú chạm.
 * 4. Đệm đáy lấy từ `useSafeAreaInsets()` chứ không viết cứng: máy có vạch home và máy có
 *    nút bấm chừa ra hai khoảng khác nhau.
 *
 * `statusBarTranslucent` kèm `navigationBarTranslucent` để nền mờ phủ luôn thanh trạng thái
 * và thanh điều hướng. Bản cũ chừa hai dải ấy sáng nguyên, nhìn như hộp bị hụt.
 */

import { ReactNode } from 'react';
import { KeyboardAvoidingView, Modal, Pressable, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Mau } from './thietKe';

export function HopDay({
  children,
  khoang = 8,
  onDong,
}: {
  children: ReactNode;
  /** Khoảng cách giữa các mục trong hộp. Tờ lịch cần hẹp hơn nút bấm. */
  khoang?: number;
  onDong: () => void;
}) {
  const le = useSafeAreaInsets();

  return (
    <Modal
      visible
      transparent
      animationType="fade"
      statusBarTranslucent
      navigationBarTranslucent
      onRequestClose={onDong}
    >
      {/* Nền mờ nằm riêng một lớp dưới cùng: chạm vào là đóng. */}
      <Pressable style={kieu.nenMo} onPress={onDong} accessibilityLabel="Đóng" />

      <KeyboardAvoidingView
        style={[StyleSheet.absoluteFill, kieu.donXuongDay]}
        behavior="padding"
        pointerEvents="box-none"
      >
        {/* Chặn chạm xuyên qua hộp ra nền mờ phía sau. */}
        <Pressable
          style={[kieu.hop, { paddingBottom: le.bottom + 16, gap: khoang }]}
          onPress={() => {}}
        >
          <View style={kieu.tay} />
          {children}
        </Pressable>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const kieu = StyleSheet.create({
  nenMo: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(16,19,23,0.4)',
  },
  donXuongDay: { justifyContent: 'flex-end' },
  hop: {
    backgroundColor: Mau.trang,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingHorizontal: 14,
    paddingTop: 14,
  },
  tay: {
    width: 36,
    height: 4,
    borderRadius: 2,
    backgroundColor: Mau.vien,
    alignSelf: 'center',
    marginBottom: 6,
  },
});
