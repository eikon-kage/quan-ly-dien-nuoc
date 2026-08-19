import { Pressable, StyleSheet, Text, View } from 'react-native';

import * as Ngay from '../nghiepvu/ngayViet';
import { HopDay } from './HopDay';
import { Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './thietKe';

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
    onChon(Ngay.ghep(nam, thang, soTrongThang));
  }

  return (
    <HopDay khoang={5} onDong={onDong}>
      <Text style={kieu.tieuDe}>{tieuDe}</Text>
      <Text style={kieu.moTa}>
        Tháng {thang}/{nam}
      </Text>

      <View style={kieu.hang}>
        {Ngay.COT_LICH.map((ten) => (
          <Text key={ten} style={kieu.chuCot} maxFontSizeMultiplier={HeSoChuToiDaLuoi}>
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
    </HopDay>
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
      <Text
        style={[kieu.chuNgay, dangChon && kieu.chuNgayChon]}
        maxFontSizeMultiplier={HeSoChuToiDaLuoi}
      >
        {soTrongThang}
      </Text>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  tieuDe: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  moTa: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    marginBottom: 4,
  },

  hang: { flexDirection: 'row', gap: 5 },
  chuCot: {
    flex: 1,
    textAlign: 'center',
    fontSize: Co.chuNho,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
    paddingBottom: 2,
  },

  // Ô cao 46pt: trên ngưỡng 44pt Apple khuyên, mà bảy cột vẫn vừa bề ngang máy nhỏ.
  o: {
    flex: 1,
    minHeight: 46,
    paddingVertical: 6,
    borderRadius: Co.bo,
    alignItems: 'center',
    justifyContent: 'center',
  },
  oThuong: { backgroundColor: Mau.trang, borderWidth: 1, borderColor: Mau.vien },
  oChon: { backgroundColor: Mau.chinh, borderWidth: 1, borderColor: Mau.chinh },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuNgayChon: { fontFamily: PhongChu.dam, color: Mau.trang },

  nutThoi: {
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
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
