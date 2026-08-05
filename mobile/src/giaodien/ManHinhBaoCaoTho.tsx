import { Feather } from '@expo/vector-icons';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { BaoCaoTho } from '../nghiepvu/baoCao';
import * as Ngay from '../nghiepvu/ngayViet';
import { LichCong } from './LichCong';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  baoCao: BaoCaoTho;
  nam: number;
  thang: number;
  onDong: () => void;
}

/**
 * Chi tiết một tháng của một thợ: đi làm ngày nào, nghỉ ngày nào, ứng tiền ngày nào.
 * Đây là chỗ tra khi thợ thắc mắc "sao tháng này ít tiền thế".
 */
export function ManHinhBaoCaoTho({ baoCao, nam, thang, onDong }: Props) {
  const { tho, ngayCongs, ngayNghis, ungTiens } = baoCao;

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <View style={kieu.dauTrang}>
          <Pressable style={kieu.nutDong} onPress={onDong} accessibilityLabel="Đóng">
            <Feather name="chevron-left" size={22} color={Mau.chinh} />
          </Pressable>
          <View style={kieu.giuaDauTrang}>
            <Text style={kieu.chuTen} numberOfLines={1}>
              {tho.ten}
            </Text>
            <Text style={kieu.chuPhu}>
              Tháng {thang}/{nam}
            </Text>
          </View>
          <View style={kieu.nutDong} />
        </View>

        <ScrollView contentContainerStyle={kieu.trong}>
          {/* Bốn con số tóm tắt, để khỏi phải cộng lại từ bảng bên dưới. */}
          <View style={kieu.the}>
            <Dong nhan="Số công" gia={`${Ngay.soCong(baoCao.tongCong)} công`} />
            <Dong nhan="Tiền công" gia={Ngay.tien(baoCao.tienCong)} />
            {baoCao.daUng > 0 && <Dong nhan="Đã ứng" gia={Ngay.tienTru(baoCao.daUng)} />}
            <View style={kieu.gach} />
            <Dong
              nhan="Còn phải trả"
              gia={Ngay.tien(baoCao.conLai)}
              mau={baoCao.conLai < 0 ? Mau.do : Mau.xanhLa}
              dam
            />
          </View>

          {/*
            Cả đi làm lẫn nghỉ gộp vào một tờ lịch. Ngày nghỉ chỉ đếm phần đã trôi qua và
            từ lúc thợ vào làm — ngày mai chưa tới thì không phải nghỉ, ô đó để trắng.
          */}
          <Text style={kieu.tieuDeMuc}>Lịch đi làm</Text>
          <View style={kieu.the}>
            <LichCong nam={nam} thang={thang} ngayCongs={ngayCongs} ngayNghis={ngayNghis} />
            {ngayCongs.length === 0 && (
              <Text style={kieu.chuTrong}>Tháng này chưa có ngày công nào.</Text>
            )}
          </View>

          <Text style={kieu.tieuDeMuc}>
            Ứng tiền{ungTiens.length > 0 ? ` (${ungTiens.length} lần)` : ''}
          </Text>
          <View style={[kieu.the, kieu.theCuoi]}>
            {ungTiens.length === 0 ? (
              <Text style={kieu.chuTrong}>Tháng này chưa ứng lần nào.</Text>
            ) : (
              ungTiens.map((ung) => (
                <View key={ung.id} style={kieu.dongUng}>
                  <View style={kieu.coNgay}>
                    <Text style={kieu.chuNgay}>{Ngay.ngayGon(ung.ngay).slice(0, 5)}</Text>
                    <Text style={kieu.chuThu}>{Ngay.thu(ung.ngay)}</Text>
                  </View>
                  <Text style={kieu.chuGhiChu} numberOfLines={1}>
                    {ung.ghiChu}
                  </Text>
                  <Text style={kieu.chuTienUng}>{Ngay.tienTru(ung.soTien)}</Text>
                </View>
              ))
            )}
          </View>
        </ScrollView>
      </SafeAreaView>
    </Modal>
  );
}

function Dong({
  nhan,
  gia,
  mau,
  dam,
}: {
  nhan: string;
  gia: string;
  mau?: string;
  dam?: boolean;
}) {
  return (
    <View style={kieu.dongSo}>
      <Text style={[kieu.chuNhan, dam === true && kieu.chuNhanDam]}>{nhan}</Text>
      <Text style={[kieu.chuGia, dam === true && kieu.chuGiaDam, mau !== undefined && { color: mau }]}>
        {gia}
      </Text>
    </View>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  dauTrang: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Mau.trang,
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: Mau.vien,
  },
  nutDong: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  giuaDauTrang: { flex: 1, alignItems: 'center' },
  chuTen: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  trong: { padding: 14, paddingBottom: 24 },
  tieuDeMuc: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
    marginTop: 16,
    marginBottom: 6,
    marginLeft: 2,
  },
  the: {
    backgroundColor: Mau.trang,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Mau.vien,
    padding: 12,
    gap: 6,
  },
  theCuoi: { marginBottom: 8 },

  dongSo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  chuNhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNhanDam: { fontFamily: PhongChu.vua, color: Mau.chu },
  chuGia: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuGiaDam: { fontSize: Co.chuTen, fontFamily: PhongChu.dam },
  gach: { height: 1, backgroundColor: Mau.vien, marginVertical: 3 },

  dongUng: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingVertical: 5 },
  coNgay: { width: 62 },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuThu: { fontSize: 11, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuGhiChu: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTienUng: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.do },

  chuTrong: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
});
