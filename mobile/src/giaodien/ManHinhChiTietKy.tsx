import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { DuLieuChamCong, KyLuong } from '../nghiepvu/kieu';
import { baoCaoTrongKy, boChot, tongCuaKy } from '../nghiepvu/ky';
import * as Ngay from '../nghiepvu/ngayViet';
import { ManHinhBaoCaoTho } from './ManHinhBaoCaoTho';
import { DauTrang, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  ky: KyLuong;
  /** Chỉ kỳ mới nhất mới bỏ chốt được — gỡ kỳ ở giữa thì nợ của các kỳ sau nó thành sai. */
  boChotDuoc: boolean;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

/**
 * Tờ quyết toán của một kỳ đã chốt: từng thợ làm bao nhiêu công, cầm về bao nhiêu tiền.
 *
 * Đây là bản chụp lúc chốt, không tính lại bao giờ nữa — sau này có tăng lương thợ hay
 * sửa tên thì tờ này vẫn y nguyên như hôm trả tiền.
 */
export function ManHinhChiTietKy({ duLieu, ky, boChotDuoc, capNhat, onDong }: Props) {
  const [xemTho, datXemTho] = useState<string | null>(null);
  const [hoiBoChot, datHoiBoChot] = useState(false);
  const tong = tongCuaKy(ky);

  function lamBoChot() {
    capNhat(boChot(duLieu, ky.id));
    onDong();
  }

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <DauTrang
          tieuDe={Ngay.khoangGon(ky.tuNgay, ky.denNgay)}
          phu={`Chốt ngày ${Ngay.ngayGon(ky.denNgay)}`}
          onLui={onDong}
        />

        <ScrollView contentContainerStyle={kieu.trong}>
          {ky.dongs.map((dong) => (
            <Pressable key={dong.thoId} style={kieu.the} onPress={() => datXemTho(dong.thoId)}>
              <View style={kieu.dongTen}>
                <Text style={kieu.chuTen} numberOfLines={1}>
                  {dong.tenTho}
                </Text>
                <Text style={kieu.chuCong}>{Ngay.soCong(dong.tongCong)} công</Text>
              </View>

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

              {dong.noKyTruoc !== 0 && (
                <View style={kieu.dongSo}>
                  <Text style={kieu.chuNhan}>
                    {dong.noKyTruoc > 0 ? 'Nợ kỳ trước' : 'Kỳ trước trả dư'}
                  </Text>
                  <Text style={kieu.chuSo}>
                    {dong.noKyTruoc > 0 ? Ngay.tien(dong.noKyTruoc) : Ngay.tienTru(dong.noKyTruoc)}
                  </Text>
                </View>
              )}

              <View style={kieu.gach} />

              <View style={kieu.dongSo}>
                <Text style={kieu.chuDaTra}>Đã trả</Text>
                <Text style={kieu.chuSoDaTra}>{Ngay.tien(dong.daTra)}</Text>
              </View>

              {dong.chuyenKySau !== 0 && (
                <View style={kieu.dongSo}>
                  <Text style={kieu.chuNhan}>
                    {dong.chuyenKySau > 0 ? 'Còn nợ, chuyển kỳ sau' : 'Trả dư, kỳ sau trừ lại'}
                  </Text>
                  <Text
                    style={[kieu.chuSo, { color: dong.chuyenKySau > 0 ? Mau.do : Mau.xanhLa }]}
                  >
                    {dong.chuyenKySau > 0
                      ? Ngay.tien(dong.chuyenKySau)
                      : Ngay.tienTru(dong.chuyenKySau)}
                  </Text>
                </View>
              )}

              <View style={kieu.dongXem}>
                <Text style={kieu.chuXem}>Xem chi tiết từng ngày</Text>
                <Feather name="chevron-right" size={15} color={Mau.chinh} />
              </View>
            </Pressable>
          ))}

          {/*
            Nút bỏ chốt để tận đáy, sau khi đã cuộn qua hết tờ quyết toán — không phải thứ
            bấm trúng lúc đang xem. Viền đỏ, hỏi lại một lần nữa ngay trên chính cái nút.
          */}
          {boChotDuoc && (
            <View style={kieu.khoiBoChot}>
              <Text style={kieu.chuBoChot}>
                Bỏ chốt là kỳ này mở lại, công và tiền ứng quay về mục Bảng lương. Không mất
                buổi công nào. Số tiền đã ghi là đã trả thì mất, phải nhập lại lúc chốt sau.
              </Text>
              <Pressable
                style={[kieu.nutBoChot, hoiBoChot && kieu.nutBoChotChac]}
                onPress={() => {
                  if (hoiBoChot) {
                    lamBoChot();
                  } else {
                    datHoiBoChot(true);
                  }
                }}
              >
                <Feather
                  name={hoiBoChot ? 'alert-triangle' : 'rotate-ccw'}
                  size={16}
                  color={hoiBoChot ? Mau.trang : Mau.do}
                />
                <Text style={[kieu.chuNutBoChot, hoiBoChot && kieu.chuNutBoChotChac]}>
                  {hoiBoChot ? 'Chắc chưa? Bấm lần nữa để bỏ chốt' : 'Bỏ chốt kỳ này'}
                </Text>
              </Pressable>

              {hoiBoChot && (
                <Pressable style={kieu.nutThoi} onPress={() => datHoiBoChot(false)}>
                  <Text style={kieu.chuNutThoi}>Thôi, giữ nguyên</Text>
                </Pressable>
              )}
            </View>
          )}
        </ScrollView>

        <View style={kieu.chanTrang}>
          <View style={kieu.dongSo}>
            <Text style={kieu.chuNhanTong}>
              {ky.dongs.length} thợ · {Ngay.soCong(tong.tongCong)} công
            </Text>
            <Text style={kieu.chuSo}>{Ngay.tien(tong.tienCong)}</Text>
          </View>
          <View style={kieu.dongSo}>
            <Text style={kieu.chuNhanTong}>Cả tổ đã trả</Text>
            <Text style={kieu.chuTongTra}>{Ngay.tien(tong.daTra)}</Text>
          </View>
          {tong.chuyenKySau !== 0 && (
            <View style={kieu.dongSo}>
              <Text style={kieu.chuNhanTong}>
                {tong.chuyenKySau > 0 ? 'Còn nợ, chuyển kỳ sau' : 'Trả dư, kỳ sau trừ lại'}
              </Text>
              <Text style={[kieu.chuSo, { color: tong.chuyenKySau > 0 ? Mau.do : Mau.xanhLa }]}>
                {tong.chuyenKySau > 0
                  ? Ngay.tien(tong.chuyenKySau)
                  : Ngay.tienTru(tong.chuyenKySau)}
              </Text>
            </View>
          )}
        </View>

        {/*
          Không truyền `suaUng`: lịch sử ứng của kỳ đã chốt chỉ để đọc. Tờ quyết toán là
          bản chụp của một lần đã đếm tiền trao tay, sửa số ứng bây giờ chỉ làm sổ nói
          khác tờ thợ đang cầm. Cần sửa thật thì bỏ chốt kỳ ở nút dưới đáy trang này.
        */}
        {xemTho !== null && (
          <ManHinhBaoCaoTho
            dungBaoCao={(tu, den) => baoCaoTrongKy(duLieu, ky, xemTho, tu, den)}
            tuNgayDau={ky.tuNgay}
            denNgayDau={ky.denNgay}
            onDong={() => datXemTho(null)}
          />
        )}
      </SafeAreaView>
    </Modal>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  trong: { padding: 16, paddingTop: 4, paddingBottom: 20 },
  the: { ...theTrang, marginBottom: 12, gap: 7 },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  chuCong: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

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

  khoiBoChot: { marginTop: 10, gap: 8 },
  chuBoChot: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    lineHeight: 19,
  },
  nutBoChot: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.do,
    backgroundColor: Mau.doNhat,
  },
  nutBoChotChac: { backgroundColor: Mau.do },
  chuNutBoChot: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.do,
    textAlign: 'center',
  },
  chuNutBoChotChac: { color: Mau.trang },
  nutThoi: {
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chuNutThoi: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.xam },

  // Không có thanh tab dưới màn hình này nên chân trang vẫn là mảng trắng, nổi bằng bóng.
  chanTrang: {
    backgroundColor: Mau.trang,
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 14,
    gap: 6,
    ...Bong.noi,
  },
  chuNhanTong: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTongTra: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.xanhLa },
});
