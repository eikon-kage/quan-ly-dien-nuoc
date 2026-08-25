import { useState } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';

import { HopDay } from './HopDay';
import { ONhap } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

/** Ghi chú dài nhất cho một ngày. Đủ một hai câu; dài hơn thì không ai đọc trên màn hình nhỏ. */
export const CHU_TOI_DA = 200;

interface Props {
  tieuDe: string;
  /** Câu hỏi làm nhãn nằm trong ô, ví dụ "Hôm ấy có gì đáng ghi?". */
  moTa: string;
  goiY: string;
  /** Điền sẵn khi mở, dùng lúc sửa chữ đã gõ trước đó. */
  giaTriDau?: string;
  onGhi: (chu: string) => void;
  onDong: () => void;
}

/**
 * Hộp gõ một đoạn chữ ngắn. Sinh đôi với [HopNhapSo](./HopNhapSo.tsx), và cũng tự vẽ vì
 * `Alert.prompt` chỉ có trên iOS.
 *
 * Khác hộp nhập số ở hai chỗ:
 *
 * - **Ô trống cũng ghi được.** Ở hộp số, ghi số 0 là vô nghĩa nên nút Ghi phải tắt; ở đây
 *   xoá sạch chữ rồi bấm Ghi chính là *cách bỏ ghi chú*. Có thêm nút Xoá cho ai không đoán
 *   ra điều đó, nhưng chỉ hiện khi đang thật có chữ để xoá.
 * - Ô nhập nhiều dòng: ghi chú hay dài hơn một dòng, mà ô một dòng thì chữ trôi ngang,
 *   người gõ không đọc lại được câu mình vừa viết.
 */
export function HopNhapChu({ tieuDe, moTa, goiY, giaTriDau = '', onGhi, onDong }: Props) {
  const [chu, datChu] = useState(giaTriDau);
  const coChuCu = giaTriDau.trim() !== '';

  return (
    <HopDay onDong={onDong}>
      <Text style={kieu.tieuDe}>{tieuDe}</Text>

      <ONhap
        nhan={moTa}
        value={chu}
        onChangeText={datChu}
        placeholder={goiY}
        accessibilityLabel={goiY}
        maxLength={CHU_TOI_DA}
        multiline
        numberOfLines={3}
        autoFocus
      />

      {/* Đếm chữ còn lại chỉ hiện khi gần hết, kẻo lúc nào cũng có một con số nhảy nhảy. */}
      <Text style={kieu.demChu}>
        {chu.length > CHU_TOI_DA - 40 ? `Còn ${CHU_TOI_DA - chu.length} chữ` : ' '}
      </Text>

      <Pressable style={[kieu.nut, kieu.nutBat]} onPress={() => onGhi(chu.trim())}>
        <Text style={[kieu.chuNut, { color: Mau.trang }]}>Ghi</Text>
      </Pressable>

      {coChuCu && (
        <Pressable style={[kieu.nut, kieu.nutXoa]} onPress={() => onGhi('')}>
          <Text style={[kieu.chuNut, { color: Mau.do }]}>Xoá ghi chú</Text>
        </Pressable>
      )}

      <Pressable style={[kieu.nut, kieu.nutThoi]} onPress={onDong}>
        <Text style={[kieu.chuNut, { color: Mau.xam }]}>Thôi</Text>
      </Pressable>
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
  demChu: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    minHeight: 18,
    marginLeft: 2,
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
  nutBat: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  nutXoa: { backgroundColor: Mau.doNhat, borderColor: Mau.do },
  nutThoi: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
