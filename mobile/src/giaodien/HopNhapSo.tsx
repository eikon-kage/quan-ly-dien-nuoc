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
  /** Ô chữ phụ dưới ô số, ví dụ ghi chú cho lần ứng tiền. Không bắt buộc điền. */
  oChu?: { nhan: string; goiY: string };
  onGhi: (so: number, chuThem: string) => void;
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
  oChu,
  onGhi,
  onDong,
}: Props) {
  const [chu, datChu] = useState(giaTriDau);
  const [chuThem, datChuThem] = useState('');

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

          {/*
            Chữ gợi ý tự vẽ chứ không dùng placeholder: Android có lỗi để con nháy
            nhảy ra sát mép phải khi ô căn giữa mà còn trống và có placeholder.
            Ô trống thật thì con nháy nằm đúng giữa.
          */}
          <View>
            <TextInput
              style={kieu.o}
              value={chu}
              onChangeText={datChu}
              accessibilityLabel={goiY}
              keyboardType={banPhim}
              autoFocus
            />
            {chu === '' && (
              <View style={kieu.phuGoiY} pointerEvents="none">
                <Text style={kieu.chuGoiY}>{goiY}</Text>
              </View>
            )}
          </View>

          {/*
            Đọc lại số vừa gõ ("1.500.000 đ") để bắt lỗi thừa hoặc thiếu số 0.
            Gõ quá tay thì chỗ này thành lời nhắc màu đỏ.
          */}
          <Text style={[kieu.docLai, loiHienTai !== null && kieu.docLaiLoi]}>
            {loiHienTai ?? (so !== null && so > 0 ? hienLai(so) : ' ')}
          </Text>

          {/*
            Ô chữ để trắng cũng ghi được — bắt điền thì lần nào vội cũng phải gõ bừa
            một chữ cho xong. Chữ căn trái vì đây là câu chữ, không phải con số.
          */}
          {oChu !== undefined && (
            <>
              <Text style={kieu.nhanOChu}>{oChu.nhan}</Text>
              <TextInput
                style={[kieu.o, kieu.oChu]}
                value={chuThem}
                onChangeText={datChuThem}
                placeholder={oChu.goiY}
                placeholderTextColor={Mau.xam}
                maxLength={60}
              />
            </>
          )}

          <Pressable
            style={[kieu.nut, ghiDuoc ? kieu.nutBat : kieu.nutTat]}
            onPress={() => ghiDuoc && onGhi(so, chuThem.trim())}
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
    minHeight: Co.caoNut,
    paddingVertical: 8,
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
  phuGoiY: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chuGoiY: { fontSize: 20, fontFamily: PhongChu.vua, color: Mau.xam },
  nhanOChu: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xam, marginLeft: 2 },
  oChu: { fontSize: Co.chuThuong, textAlign: 'left' },
  docLai: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xanhLa,
    textAlign: 'center',
    minHeight: 18,
  },
  docLaiLoi: { color: Mau.do },
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
  nutTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  nutThoi: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
