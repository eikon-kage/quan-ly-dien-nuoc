import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { DongLuong } from '../nghiepvu/bangLuong';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import { kyHienTai, quyetToan, traDuKien } from '../nghiepvu/ky';
import * as Ngay from '../nghiepvu/ngayViet';
import { HopNhapSo } from './HopNhapSo';
import { DauTrang, ThanhDoan, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu, Tuoi } from './thietKe';

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
    const moi = new Map(daTra);
    moi.set(thoId, soTien);
    datDaTra(moi);
  }

  function chot() {
    capNhat(quyetToan(duLieu, { denNgay: homNay, daTra }));
    onDong();
  }

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <DauTrang
          tieuDe="Quyết toán kỳ"
          phu={Ngay.khoangGon(ky.tuNgay, ky.denNgay)}
          onLui={onDong}
        />

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
                  Ba lối trả, dựng thành thanh phân đoạn như bản thiết kế: trả đủ, trả một
                  khoản, khất hẳn.

                  *Khoản khác* mở đúng hộp nhập mà chạm vào ô **Thực trả** ở trên cũng mở.
                  Trước đây chỉ có ô Thực trả — muốn trả một khoản nhất định thì phải đoán ra
                  là chạm được vào con số ấy. Đưa nó thành một mục ngang hàng với hai mục kia
                  thì nhìn là thấy có ba lối, không phải mò.

                  Viên *Khoản khác* sáng khi số đang trả không phải trả đủ cũng không phải 0 —
                  tức là một khoản do người dùng tự đặt.
                */}
                <ThanhDoan
                  cac={[
                    { ma: 'du', nhan: 'Trả đủ' },
                    { ma: 'khac', nhan: 'Khoản khác' },
                    { ma: 'khong', nhan: 'Không trả' },
                  ]}
                  dangChon={tra === traDuKien(dong) ? 'du' : tra === 0 ? 'khong' : 'khac'}
                  onChon={(ma) => {
                    if (ma === 'khac') {
                      datDangSua(dong);
                      return;
                    }
                    datTra(dong.tho.id, ma === 'du' ? traDuKien(dong) : 0);
                  }}
                />
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

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  trong: { padding: 16, paddingTop: 4, paddingBottom: 20 },
  the: { ...theTrang, marginBottom: 12, gap: 8 },
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
    borderColor: Tuoi.chinh,
    backgroundColor: Mau.chinhNhat,
  },
  chuNhanTra: { flex: 1, fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuSoTra: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  chanTrang: {
    backgroundColor: Mau.trang,
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 14,
    gap: 8,
    ...Bong.noi,
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
