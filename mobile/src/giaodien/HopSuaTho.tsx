import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import {
  Alert,
  KeyboardAvoidingView,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { docTien } from '../nghiepvu/nhapSo';
import { datLuong, lichSuLuong, luongTaiNgay, luuTho, themTho, xoaMocLuong } from '../nghiepvu/thaoTac';
import { HopChon } from './HopChon';
import { HopNhapSo } from './HopNhapSo';
import { NutChip, ONhap, theTrang } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  /** Để trống là thêm thợ mới. */
  tho: Tho | null;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

export function HopSuaTho({ duLieu, tho, capNhat, onDong }: Props) {
  const homNay = Ngay.homNay();
  const moiNhat = tho ? duLieu.thos.find((t) => t.id === tho.id) ?? tho : null;

  const [ten, datTen] = useState(tho?.ten ?? '');
  const [tienMoi, datTienMoi] = useState('');
  const [dangLam, datDangLam] = useState(tho?.dangLam ?? true);

  /** Đang ở bước nào của việc đổi lương: nhập số tiền, rồi chọn áp dụng từ bao giờ. */
  const [soTienMoi, datSoTienMoi] = useState<number | null>(null);
  const [dangNhapLuong, datDangNhapLuong] = useState(false);

  function luu() {
    const tenSach = ten.trim();
    if (tenSach.length === 0) {
      Alert.alert('Thiếu tên', 'Anh nhập tên thợ đã.', [{ text: 'Đóng' }]);
      return;
    }

    if (moiNhat === null) {
      const tienMotCong = docTien(tienMoi);
      if (tienMotCong === null || tienMotCong <= 0) {
        Alert.alert('Thiếu tiền công', 'Anh nhập tiền một công của thợ đã.', [{ text: 'Đóng' }]);
        return;
      }

      capNhat(themTho(duLieu, tenSach, tienMotCong, homNay).duLieu);
    } else {
      capNhat(luuTho(duLieu, { ...moiNhat, ten: tenSach, dangLam }));
    }

    onDong();
  }

  /** Đặt mốc lương mới. Mốc cũ giữ nguyên nên bảng lương các tháng trước không đổi. */
  function apDungLuong(ma: string) {
    if (moiNhat === null || soTienMoi === null) {
      return;
    }

    const dauThang = Ngay.ghep(Ngay.tach(homNay).nam, Ngay.tach(homNay).thang, 1);
    const dangApDung =
      [...moiNhat.mocLuong].reverse().find((m) => m.tuNgay <= homNay) ?? moiNhat.mocLuong[0];

    const tuNgay =
      ma === 'homNay' ? homNay : ma === 'dauThang' ? dauThang : dangApDung.tuNgay;

    capNhat(datLuong(duLieu, moiNhat.id, tuNgay, soTienMoi));
    datSoTienMoi(null);
  }

  function xoaMoc(tuNgay: string) {
    if (moiNhat === null) {
      return;
    }

    Alert.alert('Xoá mốc lương này?', `Mốc từ ngày ${Ngay.ngayGon(tuNgay)}.`, [
      { text: 'Thôi', style: 'cancel' },
      {
        text: 'Xoá',
        style: 'destructive',
        onPress: () => {
          try {
            capNhat(xoaMocLuong(duLieu, moiNhat.id, tuNgay));
          } catch {
            Alert.alert('Không xoá được', 'Thợ phải còn ít nhất một mốc tiền công.', [
              { text: 'Đóng' },
            ]);
          }
        },
      },
    ]);
  }

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      {/*
        `behavior="padding"` cho cả iOS lẫn Android, không phân biệt hệ — xem ghi chú dài ở
        [HopDay](./HopDay.tsx). Kèm `ScrollView` có `keyboardShouldPersistTaps="handled"` để
        bấm được nút Lưu ngay khi bàn phím còn mở, không phải đóng bàn phím trước.
      */}
      <KeyboardAvoidingView behavior="padding" style={kieu.khung}>
        <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
          <ScrollView contentContainerStyle={kieu.trong} keyboardShouldPersistTaps="handled">
            <Text style={kieu.tieuDe}>{moiNhat ? 'Sửa thợ' : 'Thêm thợ'}</Text>

            <ONhap
              nhan="Tên thợ"
              value={ten}
              onChangeText={datTen}
              placeholder="Ví dụ: Anh Tuấn"
              autoFocus={moiNhat === null}
            />

            {moiNhat === null ? (
              <View style={kieu.khoi}>
                <ONhap
                  nhan="Tiền một công"
                  coSo
                  value={tienMoi}
                  onChangeText={datTienMoi}
                  placeholder="Ví dụ: 300000"
                  keyboardType="number-pad"
                />
                <Text style={kieu.chuPhu}>Một ngày làm đủ sáng và chiều là 2 công.</Text>
              </View>
            ) : (
              <View style={kieu.theLuong}>
                <View style={kieu.dongNhan}>
                  <Text style={kieu.nhan}>Tiền công</Text>
                  <NutChip
                    nhan="Đổi lương"
                    icon="trending-up"
                    onPress={() => datDangNhapLuong(true)}
                  />
                </View>

                <Text style={kieu.tienLon}>{Ngay.tien(luongTaiNgay(moiNhat, homNay))}</Text>
                <Text style={kieu.chuPhu}>đang áp dụng cho một công</Text>

                {/*
                  Lịch sử để mốc mới nhất lên đầu. Đổi lương là thêm mốc chứ không sửa đè,
                  nên bảng lương các tháng trước vẫn giữ đúng số tiền đã trả.
                */}
                {moiNhat.mocLuong.length > 1 && (
                  <View style={kieu.lichSu}>
                    <Text style={kieu.nhanNho}>Các mốc đã qua</Text>
                    {lichSuLuong(moiNhat).map((moc) => (
                      <View key={moc.tuNgay} style={kieu.dongMoc}>
                        <Text style={kieu.chuMocNgay}>Từ {Ngay.ngayGon(moc.tuNgay)}</Text>
                        <Text style={kieu.chuMocTien}>{Ngay.tien(moc.tienMotCong)}</Text>
                        <Pressable style={kieu.nutXoaMoc} onPress={() => xoaMoc(moc.tuNgay)}>
                          <Feather name="trash-2" size={14} color={Mau.do} />
                        </Pressable>
                      </View>
                    ))}
                  </View>
                )}
              </View>
            )}

            {moiNhat !== null && (
              <View style={kieu.dongDangLam}>
                <View style={kieu.trai}>
                  <Text style={kieu.nhan}>Đang làm</Text>
                  <Text style={kieu.chuPhu}>
                    Tắt đi nếu thợ đã nghỉ. Bảng lương các tháng trước vẫn còn.
                  </Text>
                </View>
                <Switch
                  value={dangLam}
                  onValueChange={datDangLam}
                  trackColor={{ true: Mau.chinh, false: Mau.vien }}
                />
              </View>
            )}

            <Pressable style={[kieu.nut, kieu.nutChinh]} onPress={luu}>
              <Text style={[kieu.chuNut, { color: Mau.trang }]}>Lưu</Text>
            </Pressable>

            <Pressable style={[kieu.nut, kieu.nutPhu]} onPress={onDong}>
              <Text style={[kieu.chuNut, { color: Mau.xam }]}>Thôi, quay lại</Text>
            </Pressable>
          </ScrollView>
        </SafeAreaView>
      </KeyboardAvoidingView>

      {dangNhapLuong && (
        <HopNhapSo
          tieuDe="Đổi tiền công"
          moTa={`${moiNhat?.ten ?? ''} — một công bao nhiêu?`}
          goiY="Ví dụ 350000"
          onGhi={(so) => {
            datDangNhapLuong(false);
            datSoTienMoi(so);
          }}
          onDong={() => datDangNhapLuong(false)}
        />
      )}

      {soTienMoi !== null && (
        <HopChon
          tieuDe={`${Ngay.tien(soTienMoi)} một công — tính từ bao giờ?`}
          luaChon={[
            { ma: 'homNay', nhan: 'Từ hôm nay', icon: 'calendar' },
            { ma: 'dauThang', nhan: 'Từ đầu tháng này', icon: 'calendar' },
            { ma: 'suaDe', nhan: 'Sửa lại giá đang áp dụng', icon: 'edit-3' },
          ]}
          onChon={apDungLuong}
          onDong={() => datSoTienMoi(null)}
        />
      )}
    </Modal>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  trong: { padding: 16, paddingTop: 18, gap: 18 },
  tieuDe: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  khoi: { gap: 7 },
  // Khối tiền công gói vào một thẻ trắng: nó có tới bốn năm dòng, để trần thì trôi vào
  // giữa các khối khác không biết đâu là đầu đâu là cuối.
  theLuong: { ...theTrang, gap: 7 },
  dongNhan: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  nhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  nhanNho: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xam },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  tienLon: { fontSize: 24, fontFamily: PhongChu.dam, color: Mau.chu },

  lichSu: {
    marginTop: 8,
    gap: 4,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
    paddingTop: 10,
  },
  dongMoc: { flexDirection: 'row', alignItems: 'center', gap: 10, minHeight: 34 },
  chuMocNgay: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuMocTien: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  nutXoaMoc: { width: 30, height: 30, alignItems: 'center', justifyContent: 'center' },

  dongDangLam: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  trai: { flex: 1, gap: 2 },

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
  nutPhu: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
