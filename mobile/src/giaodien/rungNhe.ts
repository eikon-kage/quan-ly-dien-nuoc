import * as Haptics from 'expo-haptics';

/**
 * Rung nhẹ khi chạm trúng. Bấm xong có phản hồi ở tay thì mới yên tâm là máy đã nhận —
 * nhất là khi đang ở ngoài nắng, nhìn màn hình không rõ.
 */
export function rungNhe(): void {
  Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium).catch(() => {
    // Máy giả lập không rung được, kệ.
  });
}
