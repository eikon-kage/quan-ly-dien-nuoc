import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { DongLuong } from '../nghiepvu/bangLuong';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import { kyHienTai, quyetToan, traDuKien } from '../nghiepvu/ky';
import * as Ngay from '../nghiepvu/ngayViet';
import { HopNhapSo } from './HopNhapSo';
import { rungNhe } from './rungNhe';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  homNay: string;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

/**
 * Chốt kỳ: ngồi xuống trả tiền cả tổ một lượt, rồi mọi con số về 0 và đếm lại từ đầu.
 *
 * Điền sẵn số phải trả cho từng người vì chín trên mười lần là trả đủ — mở ra bấm một
 * nút là xong. Ai trả thiếu thì sửa lại số, phần thiếu tự thành nợ đầu kỳ sau; sổ khớp
 * với tiền thật trong ví chứ không khớp với tiền đáng lẽ phải trả.
 *
 * Không có ô tích chọn từng người: chốt là chốt cả tổ. Muốn khất hẳn một người thì bấm
 * *Không trả* — người đó vẫn nằm trong tờ quyết toán này với số nợ chuyển sang, chứ
 * không biến mất khỏi sổ.
 */
export function ManHinhQuyetToan({ duLieu, homNay, capNhat, onDong }: Props) {
  const ky = kyHienTai(duLieu, homNay);

  const [daTra, datDaTra] = useState<Map<string, number>>(
    () => new Map(ky.dongs.map((dong) => [dong.tho.id, traDuKien(dong)])),
  );
  const [dangSua, datDangSua] = useState<DongLuong | null>(null);

  const traCuaTho = (dong: DongLuong) => daTra.get(dong.tho.id) ?? 0;
  const tongTra = ky.dongs.reduce((tong, dong) => tong + traCuaTho(dong), 0);
  const tongChuyenTiep = ky.dongs.reduce((tong, dong) => tong + (dong.conLai - traCuaTho(dong)), 0);

  function datTra(thoId: string, soTien: number) {
    rungNhe();
    const moi = new Map(daTra);
    moi.set(thoId, soTien);
    datDaTra(moi);
  }

  function chot() {
    rungNhe();
    capNhat(quyetToan(duLieu, { denNgay: homNay, daTra }));
    onDong();
  }

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <View style={kieu.dauTrang}>
          <Pressable style={kieu.nutDong} onPress={onDong} accessibilityLabel="Đóng">
            <Feather name="chevron-left" size={22} color={Mau.chinh} />
          </Pressable>
          <View style={kieu.giuaDauTrang}>
            <Text style={kieu.chuTieuDe}>Quyết toán kỳ</Text>
            <Text style={kieu.chuPhu}>{Ngay.khoangGon(ky.tuNgay, ky.denNgay)}</Text>
          </View>
          <View style={kieu.nutDong} />
        </View>

        <ScrollView contentContainerStyle={kieu.trong}>
          {ky.dongs.map((dong) => {
            const tra = traCuaTho(dong);
            const chuyenTiep = dong.conLai - tra;

            return (
              <View key={dong.tho.id} style={kieu.the}>
                <View style={kieu.dongTen}>
                  <Text style={kieu.chuTen} numberOfLines={1}>
                    {dong.tho.ten}
                  </Text>
                  <Text style={kieu.chuCong}>{Ngay.soCong(dong.tongCong)} công</Text>
                </View>

                {/*
                  Tách rõ công mới làm, tiền đã ứng và nợ cũ mang sang. Gộp thành một số
                  "phải trả" thì thợ hỏi vì sao ra con số ấy là chịu, không giải thích được.
                */}
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
                  <Text style={kieu.chuNhanPhaiTra}>Phải trả</Text>
                  <Text style={kieu.chuSo}>{Ngay.tien(dong.conLai)}</Text>
                </View>

                {/* Ô thực trả to và bấm được cả ô — đây là thứ duy nhất phải sửa ở màn này. */}
                <Pressable
                  style={kieu.oTra}
                  onPress={() => datDangSua(dong)}
                  accessibilityLabel={`${dong.tho.ten} thực trả ${Ngay.tien(tra)}, chạm để sửa`}
                >
                  <Text style={kieu.chuNhanTra}>Thực trả</Text>
                  <Text style={kieu.chuSoTra}>{Ngay.tien(tra)}</Text>
                  <Feather name="edit-2" size={15} color={Mau.chinh} />
                </Pressable>

                {chuyenTiep !== 0 && (
                  <View style={kieu.dongSo}>
                    <Text style={kieu.chuNhan}>
                      {chuyenTiep > 0 ? 'Còn nợ, chuyển kỳ sau' : 'Trả dư, kỳ sau trừ lại'}
                    </Text>
                    <Text style={[kieu.chuSo, { color: chuyenTiep > 0 ? Mau.do : Mau.xanhLa }]}>
                      {chuyenTiep > 0 ? Ngay.tien(chuyenTiep) : Ngay.tienTru(chuyenTiep)}
                    </Text>
                  </View>
                )}

                {/*
                  Hai nút tắt cho hai đầu hay gặp: trả đủ và khất hẳn. Ở giữa thì mở ô nhập.
                */}
                <View style={kieu.dongNutTat}>
                  <NutTat
                    nhan="Trả đủ"
                    dangDung={tra === traDuKien(dong)}
                    onPress={() => datTra(dong.tho.id, traDuKien(dong))}
                  />
                  <NutTat
                    nhan="Không trả"
                    dangDung={tra === 0}
                    onPress={() => datTra(dong.tho.id, 0)}
                  />
                </View>
              </View>
            );
          })}
        </ScrollView>

        <View style={kieu.chanTrang}>
          <View style={kieu.dongSo}>
            <Text style={kieu.chuNhanTong}>Tổng phải trả</Text>
            <Text style={kieu.chuSo}>{Ngay.tien(ky.tongPhaiTra)}</Text>
          </View>

          <View style={kieu.dongSo}>
            <Text style={kieu.chuNhanTong}>Đưa cho thợ hôm nay</Text>
            <Text style={kieu.chuTongTra}>{Ngay.tien(tongTra)}</Text>
          </View>

          {tongChuyenTiep !== 0 && (
            <View style={kieu.dongSo}>
              <Text style={kieu.chuNhanTong}>
                {tongChuyenTiep > 0 ? 'Còn nợ, chuyển kỳ sau' : 'Trả dư, kỳ sau trừ lại'}
              </Text>
              <Text style={[kieu.chuSo, { color: tongChuyenTiep > 0 ? Mau.do : Mau.xanhLa }]}>
                {tongChuyenTiep > 0 ? Ngay.tien(tongChuyenTiep) : Ngay.tienTru(tongChuyenTiep)}
              </Text>
            </View>
          )}

          <Pressable style={kieu.nutChot} onPress={chot}>
            <Feather name="check-circle" size={18} color={Mau.trang} />
            <Text style={kieu.chuNutChot}>Chốt kỳ, đã trả tiền</Text>
          </Pressable>

          {/*
            Nói trước là gỡ lại được, ngay dưới cái nút đáng sợ nhất app. Người dùng ngần
            ngại ở đây thì gọi điện hỏi, mà hỏi thì mất cả buổi.
          */}
          <Text style={kieu.chuTrackNhe}>
            Chốt xong dữ liệu cũ vẫn còn nguyên. Bấm nhầm thì vào mục Kỳ đã chốt bỏ ra.
          </Text>
        </View>

        {dangSua !== null && (
          <HopNhapSo
            tieuDe={`${dangSua.tho.ten} thực trả`}
            moTa={`Phải trả ${Ngay.tien(dangSua.conLai)}. Đưa bao nhiêu?`}
            goiY="Ví dụ 2000000"
            giaTriDau={String(traCuaTho(dangSua))}
            onGhi={(soTien) => {
              datTra(dangSua.tho.id, soTien);
              datDangSua(null);
            }}
            onDong={() => datDangSua(null)}
          />
        )}
      </SafeAreaView>
    </Modal>
  );
}

/** Nút tắt đổi cả nền lẫn màu chữ lẫn nét chữ khi đang dùng, không chỉ mỗi màu. */
function NutTat({
  nhan,
  dangDung,
  onPress,
}: {
  nhan: string;
  dangDung: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      style={[kieu.nutTat, dangDung ? kieu.nutTatDung : kieu.nutTatThuong]}
      onPress={onPress}
      accessibilityState={{ selected: dangDung }}
    >
      <Text style={[kieu.chuNutTat, dangDung && kieu.chuNutTatDung]}>{nhan}</Text>
    </Pressable>
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
  chuTieuDe: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  trong: { padding: 14, paddingBottom: 20 },
  the: {
    backgroundColor: Mau.trang,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Mau.vien,
    padding: 14,
    marginBottom: 10,
    gap: 8,
  },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  chuCong: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  dongSo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  chuNhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNhanPhaiTra: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuSo: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  gach: { height: 1, backgroundColor: Mau.vien, marginVertical: 1 },

  oTra: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    minHeight: Co.caoNut,
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.chinh,
    backgroundColor: Mau.chinhNhat,
  },
  chuNhanTra: { flex: 1, fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuSoTra: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  dongNutTat: { flexDirection: 'row', gap: 8 },
  nutTat: {
    flex: 1,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  nutTatThuong: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  nutTatDung: { backgroundColor: Mau.chinhNhat, borderColor: Mau.chinh },
  chuNutTat: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
  chuNutTatDung: { fontFamily: PhongChu.dam, color: Mau.chinh },

  chanTrang: {
    backgroundColor: Mau.trang,
    padding: 12,
    gap: 8,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
  },
  chuNhanTong: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTongTra: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.xanhLa },
  nutChot: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    marginTop: 2,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  chuNutChot: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.trang,
    textAlign: 'center',
  },
  chuTrackNhe: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
});
