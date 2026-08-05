import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { thang } from '../nghiepvu/bangLuong';
import { baoCaoThang } from '../nghiepvu/baoCao';
import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { themUng } from '../nghiepvu/thaoTac';
import { HopNhapSo } from './HopNhapSo';
import { ManHinhBaoCaoTho } from './ManHinhBaoCaoTho';
import { rungNhe } from './rungNhe';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
}

export function ManHinhBangLuong({ duLieu, capNhat }: Props) {
  const homNay = Ngay.tach(Ngay.homNay());
  const [nam, datNam] = useState(homNay.nam);
  const [thangDangXem, datThang] = useState(homNay.thang);
  const [dangUng, datDangUng] = useState<Tho | null>(null);
  const [xemBaoCao, datXemBaoCao] = useState<string | null>(null);

  const bang = thang(duLieu, nam, thangDangXem);
  const tongConLai = bang.reduce((tong, d) => tong + d.conLai, 0);

  /**
   * Nút ứng tiền chỉ hiện ở tháng hiện tại. Ứng thì ghi vào ngày hôm nay, bấm ở tháng cũ
   * sẽ thấy tiền "biến mất" sang tháng khác — rất khó hiểu.
   */
  const laThangNay = nam === homNay.nam && thangDangXem === homNay.thang;

  function doiThang(soThang: number) {
    rungNhe();
    const moc = new Date(Date.UTC(nam, thangDangXem - 1 + soThang, 1));
    datNam(moc.getUTCFullYear());
    datThang(moc.getUTCMonth() + 1);
  }

  function ghiUng(soTien: number) {
    if (dangUng === null) {
      return;
    }

    rungNhe();
    capNhat(themUng(duLieu, dangUng.id, Ngay.homNay(), soTien));
    datDangUng(null);
  }

  return (
    <View style={kieu.khung}>
      <View style={kieu.dauTrang}>
        <Pressable
          style={kieu.nutMuiTen}
          onPress={() => doiThang(-1)}
          accessibilityLabel="Tháng trước"
        >
          <Feather name="chevron-left" size={22} color={Mau.chinh} />
        </Pressable>
        <Text style={kieu.chuThang}>
          Tháng {thangDangXem}/{nam}
        </Text>
        <Pressable
          style={kieu.nutMuiTen}
          onPress={() => doiThang(1)}
          accessibilityLabel="Tháng sau"
        >
          <Feather name="chevron-right" size={22} color={Mau.chinh} />
        </Pressable>
      </View>

      <FlatList
        data={bang}
        keyExtractor={(dong) => dong.tho.id}
        contentContainerStyle={kieu.danhSach}
        ListEmptyComponent={
          <View style={kieu.trong}>
            <Feather name="credit-card" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Tháng này chưa có công nào</Text>
            <Text style={kieu.chuTrong}>Sang mục Chấm công để chấm cho thợ.</Text>
          </View>
        }
        renderItem={({ item: dong }) => (
          // Bấm cả thẻ để xem chi tiết: đi làm ngày nào, nghỉ ngày nào, ứng ngày nào.
          <Pressable style={kieu.the} onPress={() => datXemBaoCao(dong.tho.id)}>
            <View style={kieu.dongTen}>
              <Text style={kieu.chuTen} numberOfLines={1}>
                {dong.tho.ten}
              </Text>
              {laThangNay && (
                <Pressable style={kieu.nutUng} onPress={() => datDangUng(dong.tho)}>
                  <Feather name="arrow-up-right" size={12} color={Mau.chinh} />
                  <Text style={kieu.chuNutUng}>Ứng tiền</Text>
                </Pressable>
              )}
            </View>

            <Text style={kieu.chuPhu}>
              {Ngay.soCong(dong.tongCong)} công · sáng {Ngay.soCong(dong.congSang)}, chiều{' '}
              {Ngay.soCong(dong.congChieu)}
            </Text>

            <View style={kieu.dongSo}>
              <Text style={kieu.chuNhan}>Tiền công</Text>
              <Text style={kieu.chuSo}>{Ngay.tien(dong.tienCong)}</Text>
            </View>

            {dong.daUng > 0 && (
              <View style={kieu.dongSo}>
                <Text style={kieu.chuNhan}>Đã ứng</Text>
                <Text style={kieu.chuSo}>{Ngay.tienTru(dong.daUng)}</Text>
              </View>
            )}

            <View style={kieu.gach} />

            {/* Con số anh cần khi móc ví. */}
            <View style={kieu.dongSo}>
              <Text style={kieu.chuConLai}>Còn phải trả</Text>
              <Text style={[kieu.chuSoConLai, { color: dong.conLai < 0 ? Mau.do : Mau.xanhLa }]}>
                {Ngay.tien(dong.conLai)}
              </Text>
            </View>

            <View style={kieu.dongXem}>
              <Text style={kieu.chuXem}>Xem chi tiết từng ngày</Text>
              <Feather name="chevron-right" size={15} color={Mau.chinh} />
            </View>
          </Pressable>
        )}
      />

      {bang.length > 0 && (
        <View style={kieu.chanTrang}>
          <Text style={kieu.chuTong}>
            Cả tổ còn phải trả: <Text style={kieu.chuTongSo}>{Ngay.tien(tongConLai)}</Text>
          </Text>
        </View>
      )}

      {dangUng !== null && (
        <HopNhapSo
          tieuDe={`${dangUng.ten} ứng tiền`}
          moTa="Thợ ứng bao nhiêu?"
          goiY="Ví dụ 500000"
          onGhi={ghiUng}
          onDong={() => datDangUng(null)}
        />
      )}

      {xemBaoCao !== null &&
        (() => {
          const baoCao = baoCaoThang(duLieu, xemBaoCao, nam, thangDangXem, Ngay.homNay());
          return baoCao === null ? null : (
            <ManHinhBaoCaoTho
              baoCao={baoCao}
              nam={nam}
              thang={thangDangXem}
              onDong={() => datXemBaoCao(null)}
            />
          );
        })()}
    </View>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  dauTrang: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Mau.trang,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: Mau.vien,
  },
  nutMuiTen: {
    width: 44,
    height: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinhNhat,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chuThang: {
    flex: 1,
    textAlign: 'center',
    fontSize: Co.chuTieuDe,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
  },

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
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  nutUng: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    height: Co.caoNutNho,
    paddingHorizontal: 12,
    borderRadius: 8,
    backgroundColor: Mau.chinhNhat,
  },
  chuNutUng: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  dongSo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  chuNhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuSo: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  gach: { height: 1, backgroundColor: Mau.vien, marginVertical: 3 },
  chuConLai: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuSoConLai: { fontSize: Co.chuTen, fontFamily: PhongChu.dam },
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
  },

  chanTrang: {
    backgroundColor: Mau.trang,
    paddingVertical: 12,
    alignItems: 'center',
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
  },
  chuTong: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTongSo: { fontFamily: PhongChu.dam, color: Mau.chu },
});
