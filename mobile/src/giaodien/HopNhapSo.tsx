import { useState } from 'react';
import {
  KeyboardAvoidingView,
  Modal,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';

import * as Ngay from '../nghiepvu/ngayViet';
import { docTien } from '../nghiepvu/nhapSo';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  tieuDe: string;
  moTa: string;
  goiY: string;
  /** Điền sẵn khi mở, dùng lúc sửa một số đã có. */
  giaTriDau?: string;
  /** Cách đọc chữ người dùng gõ thành số. Mặc định là đọc tiền. */
  doc?: (chu: string) => number | null;
  /** Cách đọc ngược số ra chữ để người dùng soi lại. Mặc định là "1.500.000 đ". */
  hienLai?: (so: number) => string;
  banPhim?: 'number-pad' | 'decimal-pad';
  /** Trả về lời nhắc nếu số đọc được nhưng không dùng được, ví dụ lớn quá. */
  loi?: (so: number) => string | null;
  onGhi: (so: number) => void;
  onDong: () => void;
}

/**
 * Hộp nhập một con số. Tự vẽ vì Alert.prompt chỉ có trên iOS, và vì hộp mặc định
 * không chỉnh được cỡ chữ.
 */
export function HopNhapSo({
  tieuDe,
  moTa,
  goiY,
  giaTriDau = '',
  doc = docTien,
  hienLai = Ngay.tien,
  banPhim = 'number-pad',
  loi = () => null,
  onGhi,
  onDong,
}: Props) {
  const [chu, datChu] = useState(giaTriDau);

  const so = doc(chu);
  const loiHienTai = so === null ? null : loi(so);
  const ghiDuoc = so !== null && so > 0 && loiHienTai === null;

  return (
    <Modal visible transparent animationType="fade" onRequestClose={onDong}>
      <KeyboardAvoidingView
        style={kieu.nenMo}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <Pressable style={kieu.phuKin} onPress={onDong} />

        <Pressable style={kieu.hop} onPress={() => {}}>
          <View style={kieu.tay} />
          <Text style={kieu.tieuDe}>{tieuDe}</Text>
          <Text style={kieu.moTa}>{moTa}</Text>

          <TextInput
            style={kieu.o}
            value={chu}
            onChangeText={datChu}
            placeholder={goiY}
            placeholderTextColor={Mau.xam}
            keyboardType={banPhim}
            autoFocus
          />

          {/*
            Đọc lại số vừa gõ ("1.500.000 đ") để bắt lỗi thừa hoặc thiếu số 0.
            Gõ quá tay thì chỗ này thành lời nhắc màu đỏ.
          */}
          <Text style={[kieu.docLai, loiHienTai !== null && kieu.docLaiLoi]}>
            {loiHienTai ?? (so !== null && so > 0 ? hienLai(so) : ' ')}
          </Text>

          <Pressable
            style={[kieu.nut, ghiDuoc ? kieu.nutBat : kieu.nutTat]}
            onPress={() => ghiDuoc && onGhi(so)}
            disabled={!ghiDuoc}
          >
            <Text style={[kieu.chuNut, { color: ghiDuoc ? Mau.trang : Mau.xam }]}>Ghi</Text>
          </Pressable>

          <Pressable style={[kieu.nut, kieu.nutThoi]} onPress={onDong}>
            <Text style={[kieu.chuNut, { color: Mau.xam }]}>Thôi</Text>
          </Pressable>
        </Pressable>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const kieu = StyleSheet.create({
  nenMo: { flex: 1, justifyContent: 'flex-end', backgroundColor: 'rgba(35,42,53,0.35)' },
  phuKin: { flex: 1 },
  hop: {
    backgroundColor: Mau.trang,
    borderTopLeftRadius: 18,
    borderTopRightRadius: 18,
    padding: 14,
    paddingBottom: 28,
    gap: 8,
  },
  tay: {
    width: 36,
    height: 4,
    borderRadius: 2,
    backgroundColor: Mau.vien,
    alignSelf: 'center',
    marginBottom: 6,
  },
  tieuDe: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    textAlign: 'center',
  },
  moTa: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam, textAlign: 'center' },
  o: {
    height: Co.caoNut,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.nen,
    paddingHorizontal: 14,
    fontSize: 20,
    fontFamily: PhongChu.vua,
    color: Mau.chu,
    textAlign: 'center',
  },
  docLai: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xanhLa,
    textAlign: 'center',
    minHeight: 18,
  },
  docLaiLoi: { color: Mau.do },
  nut: {
    height: Co.caoNut,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  nutBat: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  nutTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  nutThoi: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua },
});
