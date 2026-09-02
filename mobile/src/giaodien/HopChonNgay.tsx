import { Feather } from '@expo/vector-icons';
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
  /**
   * Cho lùi / tới tháng ngay trong hộp. Không truyền thì hộp đứng yên ở đúng tháng cha
   * đưa vào — chỗ nào chỉ chọn ngày quanh đây (đổi mốc khoảng báo cáo chẳng hạn) thì hai
   * mũi tên ấy chỉ tổ thêm thứ để bấm nhầm.
   *
   * Sửa lần ứng thì **có** truyền: ứng hôm 30 mà mùng 2 mới nhớ ra để ghi thì ngày đúng
   * nằm ở tháng trước, khoá trong một tháng là chỗ cần sửa nhất lại không với tới được.
   */
  onDoiThang?: (buoc: -1 | 1) => void;
  /**
   * Số công của từng ngày, để mỗi ô lịch nói luôn ngày ấy được mấy công. Có nó thì hộp
   * này vừa là chỗ chọn ngày vừa là chỗ *xem lại* cả tháng — mở ra là thấy tháng trước
   * ngày nào đi ngày nào nghỉ, không phải chọn từng ngày rồi bấm ra xem.
   */
  congMoiNgay?: Map<string, number>;
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
export function HopChonNgay({
  tieuDe,
  nam,
  thang,
  ngayDangChon,
  onDoiThang,
  congMoiNgay,
  onChon,
  onDong,
}: Props) {
  function bamNgay(soTrongThang: number) {
    onChon(Ngay.ghep(nam, thang, soTrongThang));
  }

  return (
    <HopDay khoang={5} onDong={onDong}>
      <Text style={kieu.tieuDe}>{tieuDe}</Text>
      {onDoiThang === undefined ? (
        <Text style={kieu.moTa}>
          Tháng {thang}/{nam}
        </Text>
      ) : (
        <View style={kieu.dongThang}>
          <NutThang huong={-1} onPress={() => onDoiThang(-1)} />
          <Text style={kieu.chuThang}>
            Tháng {thang}/{nam}
          </Text>
          <NutThang huong={1} onPress={() => onDoiThang(1)} />
        </View>
      )}

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
                cong={
                  // Không có bản đồ công thì ô lịch không nói gì về công; có mà ngày ấy
                  // trống thì đó là *chưa chấm*, khác hẳn — nên phải ra số 0, không phải
                  // undefined.
                  congMoiNgay === undefined ? undefined : congMoiNgay.get(Ngay.ghep(nam, thang, n)) ?? 0
                }
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
  cong,
  onPress,
}: {
  ngay: string;
  soTrongThang: number;
  dangChon: boolean;
  /** undefined là hộp này không hiện công; 0 là ngày ấy chưa chấm gì. */
  cong: number | undefined;
  onPress: () => void;
}) {
  return (
    <Pressable
      style={[kieu.o, dangChon ? kieu.oChon : kieu.oThuong]}
      onPress={onPress}
      accessibilityLabel={`${Ngay.ngayGon(ngay).slice(0, 5)} ${Ngay.thu(ngay)}`}
      accessibilityHint={
        cong === undefined ? undefined : cong > 0 ? `${Ngay.soCong(cong)} công` : 'Chưa chấm ngày này'
      }
      accessibilityState={{ selected: dangChon }}
    >
      <Text
        style={[kieu.chuNgay, dangChon && kieu.chuNgayChon]}
        maxFontSizeMultiplier={HeSoChuToiDaLuoi}
      >
        {soTrongThang}
      </Text>
      {/*
        Ngày chưa chấm để dấu chấm mờ chứ không bỏ trống, y như dải ngày ở màn hình chấm
        công: bỏ trống thì ô cao thấp khác nhau, tờ lịch nhìn bị gãy.
      */}
      {cong !== undefined && (
        <Text
          style={[kieu.chuCong, cong === 0 && kieu.chuChuaCham, dangChon && kieu.chuNgayChon]}
          maxFontSizeMultiplier={HeSoChuToiDaLuoi}
        >
          {cong > 0 ? Ngay.soCong(cong) : '·'}
        </Text>
      )}
    </Pressable>
  );
}

/** Mũi tên lùi / tới một tháng, cùng dáng với nút đổi tháng ở sổ công của thợ. */
function NutThang({ huong, onPress }: { huong: -1 | 1; onPress: () => void }) {
  return (
    <Pressable
      style={kieu.nutThang}
      onPress={onPress}
      accessibilityLabel={huong === -1 ? 'Tháng trước' : 'Tháng sau'}
    >
      <Feather
        name={huong === -1 ? 'chevron-left' : 'chevron-right'}
        size={20}
        color={Mau.chu}
      />
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

  dongThang: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 4,
  },
  chuThang: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  nutThang: {
    width: 44,
    height: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.nen,
    borderWidth: 1,
    borderColor: Mau.vien,
    alignItems: 'center',
    justifyContent: 'center',
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
  chuCong: { fontSize: 10, fontFamily: PhongChu.vua, color: Mau.xanhLa },
  chuChuaCham: { color: Mau.xam },

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
