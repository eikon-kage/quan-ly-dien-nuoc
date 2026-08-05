import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';

import * as Ngay from '../nghiepvu/ngayViet';
import { rungNhe } from './rungNhe';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  tieuDe: string;
  nam: number;
  thang: number;
  /** Ngày đang chọn, để tô đậm khi mở hộp lên. */
  ngayDangChon: string;
  onChon: (ngay: string) => void;
  onDong: () => void;
}

/**
 * Hộp chọn một ngày trong tháng, vẽ như tờ lịch treo tường.
 *
 * Tự vẽ chứ không lấy hộp chọn ngày của hệ điều hành: bản của iOS là ba bánh xe quay
 * (ngày / tháng / năm) chữ nhỏ, người có tuổi quay trượt tay; bản Android lại khác hẳn
 * bản iOS. Ở đây chỉ chọn ngày trong đúng một tháng nên tờ lịch vừa gọn vừa quen mắt —
 * chạm một cái là xong, không có nút "Đồng ý".
 */
export function HopChonNgay({ tieuDe, nam, thang, ngayDangChon, onChon, onDong }: Props) {
  function bamNgay(soTrongThang: number) {
    rungNhe();
    onChon(Ngay.ghep(nam, thang, soTrongThang));
  }

  return (
    <Modal visible transparent animationType="fade" onRequestClose={onDong}>
      <View style={kieu.nenMo}>
        <Pressable style={kieu.phuKin} onPress={onDong} />

        <View style={kieu.hop}>
          <View style={kieu.tay} />
          <Text style={kieu.tieuDe}>{tieuDe}</Text>
          <Text style={kieu.moTa}>
            Tháng {thang}/{nam}
          </Text>

          <View style={kieu.hang}>
            {Ngay.COT_LICH.map((ten) => (
              <Text key={ten} style={kieu.chuCot}>
                {ten}
              </Text>
            ))}
          </View>

          {Ngay.oLichThang(nam, thang).map((tuan, hang) => (
            <View key={`tuan-${hang}`} style={kieu.hang}>
              {tuan.map((n, cot) =>
                n === null ? (
                  <View key={`trong-${cot}`} style={kieu.o} />
                ) : (
                  <ONgay
                    key={n}
                    ngay={Ngay.ghep(nam, thang, n)}
                    soTrongThang={n}
                    dangChon={Ngay.ghep(nam, thang, n) === ngayDangChon}
                    onPress={() => bamNgay(n)}
                  />
                ),
              )}
            </View>
          ))}

          <Pressable style={kieu.nutThoi} onPress={onDong}>
            <Text style={kieu.chuNutThoi}>Thôi</Text>
          </Pressable>
        </View>
      </View>
    </Modal>
  );
}

function ONgay({
  ngay,
  soTrongThang,
  dangChon,
  onPress,
}: {
  ngay: string;
  soTrongThang: number;
  dangChon: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      style={[kieu.o, dangChon ? kieu.oChon : kieu.oThuong]}
      onPress={onPress}
      accessibilityLabel={`${Ngay.ngayGon(ngay).slice(0, 5)} ${Ngay.thu(ngay)}`}
      accessibilityState={{ selected: dangChon }}
    >
      <Text style={[kieu.chuNgay, dangChon && kieu.chuNgayChon]}>{soTrongThang}</Text>
    </Pressable>
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
    gap: 5,
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
  moTa: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
    marginBottom: 4,
  },

  hang: { flexDirection: 'row', gap: 5 },
  chuCot: {
    flex: 1,
    textAlign: 'center',
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
    paddingBottom: 2,
  },

  // Ô cao 46pt: trên ngưỡng 44pt Apple khuyên, mà bảy cột vẫn vừa bề ngang máy nhỏ.
  o: {
    flex: 1,
    height: 46,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  oThuong: { backgroundColor: Mau.nen, borderWidth: 1, borderColor: Mau.vien },
  oChon: { backgroundColor: Mau.chinh, borderWidth: 1, borderColor: Mau.chinh },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuNgayChon: { fontFamily: PhongChu.dam, color: Mau.trang },

  nutThoi: {
    height: Co.caoNut,
    marginTop: 10,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.nen,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chuNutThoi: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.xam },
});
