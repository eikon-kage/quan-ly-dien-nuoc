import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { DuLieuChamCong, KyLuong } from '../nghiepvu/kieu';
import { cacKyMoiTruoc, kyGanNhat, tongCuaKy } from '../nghiepvu/ky';
import * as Ngay from '../nghiepvu/ngayViet';
import { ManHinhChiTietKy } from './ManHinhChiTietKy';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
}

/**
 * Các kỳ đã quyết toán, kỳ mới nhất lên đầu.
 *
 * Đây là màn hình thứ tư, thêm vào sau khi có quyết toán. Ba màn hình đầu là chỗ *làm
 * việc hằng ngày*; màn này là chỗ *tra sổ cũ* — hai việc khác nhau, nhét chung vào Bảng
 * lương thì màn hình làm việc hằng ngày bị sổ cũ chen chỗ.
 */
export function ManHinhLichSuKy({ duLieu, capNhat }: Props) {
  const [xemKy, datXemKy] = useState<string | null>(null);

  const cacKy = cacKyMoiTruoc(duLieu);
  const kyMoiNhat = kyGanNhat(duLieu);
  const kyDangXem = cacKy.find((ky) => ky.id === xemKy);

  return (
    <View style={kieu.khung}>
      <View style={kieu.dauTrang}>
        <Text style={kieu.chuTieuDe}>Kỳ đã chốt</Text>
        <Text style={kieu.chuPhu}>
          {cacKy.length === 0 ? 'Chưa chốt kỳ nào' : `${cacKy.length} kỳ đã quyết toán`}
        </Text>
      </View>

      <FlatList
        data={cacKy}
        keyExtractor={(ky) => ky.id}
        contentContainerStyle={kieu.danhSach}
        ListEmptyComponent={
          <View style={kieu.trong}>
            <Feather name="archive" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Chưa chốt kỳ nào</Text>
            <Text style={kieu.chuTrong}>
              Trả tiền xong cả tổ thì sang mục Bảng lương bấm Quyết toán. Kỳ đã trả nằm lại
              đây, không mất đi đâu.
            </Text>
          </View>
        }
        renderItem={({ item: ky }) => (
          <TheKy ky={ky} moiNhat={ky.id === kyMoiNhat?.id} onPress={() => datXemKy(ky.id)} />
        )}
      />

      {kyDangXem !== undefined && (
        <ManHinhChiTietKy
          duLieu={duLieu}
          ky={kyDangXem}
          boChotDuoc={kyDangXem.id === kyMoiNhat?.id}
          capNhat={capNhat}
          onDong={() => datXemKy(null)}
        />
      )}
    </View>
  );
}

/** Kỳ mới nhất có dấu riêng: đó là kỳ duy nhất còn bỏ chốt lại được. */
function TheKy({ ky, moiNhat, onPress }: { ky: KyLuong; moiNhat: boolean; onPress: () => void }) {
  const tong = tongCuaKy(ky);

  return (
    <Pressable style={kieu.the} onPress={onPress}>
      <View style={kieu.dongTen}>
        <Text style={kieu.chuKhoang}>{Ngay.khoangGon(ky.tuNgay, ky.denNgay)}</Text>
        {moiNhat && (
          <View style={kieu.nhan}>
            <Text style={kieu.chuNhanMoi}>Mới nhất</Text>
          </View>
        )}
      </View>

      <Text style={kieu.chuPhuThe}>
        {ky.dongs.length} thợ · {Ngay.soCong(tong.tongCong)} công · chốt{' '}
        {Ngay.ngayGon(ky.denNgay)}
      </Text>

      <View style={kieu.dongSo}>
        <Text style={kieu.chuNhan}>Tiền công</Text>
        <Text style={kieu.chuSo}>{Ngay.tien(tong.tienCong)}</Text>
      </View>

      {tong.daUng > 0 && (
        <View style={kieu.dongSo}>
          <Text style={kieu.chuNhan}>Đã ứng</Text>
          <Text style={kieu.chuSo}>{Ngay.tienTru(tong.daUng)}</Text>
        </View>
      )}

      <View style={kieu.gach} />

      <View style={kieu.dongSo}>
        <Text style={kieu.chuDaTra}>Đã trả</Text>
        <Text style={kieu.chuSoDaTra}>{Ngay.tien(tong.daTra)}</Text>
      </View>

      {tong.chuyenKySau !== 0 && (
        <View style={kieu.dongSo}>
          <Text style={kieu.chuNhan}>
            {tong.chuyenKySau > 0 ? 'Còn nợ, chuyển kỳ sau' : 'Trả dư, kỳ sau trừ lại'}
          </Text>
          <Text style={[kieu.chuSo, { color: tong.chuyenKySau > 0 ? Mau.do : Mau.xanhLa }]}>
            {tong.chuyenKySau > 0 ? Ngay.tien(tong.chuyenKySau) : Ngay.tienTru(tong.chuyenKySau)}
          </Text>
        </View>
      )}

      <View style={kieu.dongXem}>
        <Text style={kieu.chuXem}>Xem tờ quyết toán</Text>
        <Feather name="chevron-right" size={15} color={Mau.chinh} />
      </View>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  dauTrang: {
    backgroundColor: Mau.trang,
    paddingHorizontal: 14,
    paddingVertical: 10,
    alignItems: 'center',
    borderBottomWidth: 1,
    borderBottomColor: Mau.vien,
  },
  chuTieuDe: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam, marginTop: 2 },

  danhSach: { padding: 14, paddingBottom: 20 },
  the: {
    backgroundColor: Mau.trang,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Mau.vien,
    padding: 14,
    marginBottom: 10,
    gap: 7,
  },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuKhoang: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  nhan: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 8,
    backgroundColor: Mau.chinhNhat,
  },
  chuNhanMoi: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },
  chuPhuThe: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  dongSo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  chuNhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuSo: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  gach: { height: 1, backgroundColor: Mau.vien, marginVertical: 3 },
  chuDaTra: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuSoDaTra: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.xanhLa },

  dongXem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
    marginTop: 4,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
  },
  chuXem: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  trong: { padding: 24, paddingTop: 56, gap: 10, alignItems: 'center' },
  chuTrongTo: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuTrong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
    lineHeight: 21,
  },
});
