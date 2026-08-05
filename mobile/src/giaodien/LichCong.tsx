import { Feather } from '@expo/vector-icons';
import { StyleSheet, Text, View } from 'react-native';

import { NgayCong, ngayTrongThang } from '../nghiepvu/baoCao';
import * as Ngay from '../nghiepvu/ngayViet';
import { Co, Mau, PhongChu } from './thietKe';

/**
 * Một tháng vẽ ra như tờ lịch treo tường: ngày nào đi làm thì có dấu tích.
 *
 * Trước đây chỗ này là hai danh sách — ngày đi làm và ngày nghỉ — xếp dọc. Đọc thì ra
 * nhưng phải đếm bằng mắt mới biết tháng này nghỉ dày hay thưa, mà đó mới là điều người
 * xem muốn biết. Nhìn tờ lịch là thấy ngay khoảng trống nằm ở đâu.
 *
 * Lịch bắt đầu từ Thứ Hai, giống lịch treo tường bán ngoài hàng, chứ không bắt đầu từ
 * Chủ Nhật kiểu Mỹ.
 */

/** Một ngày đi đủ cả ngày là hai công: một sáng, một chiều. */
const CONG_CA_NGAY = 2;

interface Props {
  nam: number;
  thang: number;
  ngayCongs: NgayCong[];
  /** Ngày trong kỳ mà thợ không có công nào — đã cắt phần tương lai từ trước. */
  ngayNghis: string[];
}

export function LichCong({ nam, thang, ngayCongs, ngayNghis }: Props) {
  const cong = new Map(ngayCongs.map((d) => [ngayTrongThang(d.ngay), d]));
  const nghi = new Set(ngayNghis.map(ngayTrongThang));

  return (
    <View style={kieu.lich}>
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
                cong={cong.get(n)}
                nghi={nghi.has(n)}
              />
            ),
          )}
        </View>
      ))}

      {/*
        Chú thích để khỏi phải đoán ô xanh nghĩa là gì, và tiện thể là chỗ ghi số ngày —
        khỏi ngồi đếm ô.
      */}
      <View style={kieu.gach} />
      <View style={kieu.chuThich}>
        <View style={kieu.mucChuThich}>
          <View style={[kieu.oMau, kieu.oCong]}>
            <Feather name="check" size={11} color={Mau.xanhLa} />
          </View>
          <Text style={kieu.chuMuc}>Đi làm {ngayCongs.length} ngày</Text>
        </View>
        <View style={kieu.mucChuThich}>
          <View style={[kieu.oMau, kieu.oNghi]} />
          <Text style={kieu.chuMuc}>Nghỉ {ngayNghis.length} ngày</Text>
        </View>
      </View>
    </View>
  );
}

/**
 * Ba trạng thái một ô: đi làm, nghỉ, và ngoài kỳ tính công (ngày chưa tới, hoặc ngày
 * thợ chưa vào làm). Mỗi trạng thái khác nhau cả nền, cả viền, cả dấu bên trong — không
 * chỉ dựa vào màu, để người phân biệt màu kém vẫn nhìn ra.
 */
function ONgay({
  ngay,
  soTrongThang,
  cong,
  nghi,
}: {
  ngay: string;
  soTrongThang: number;
  cong: NgayCong | undefined;
  nghi: boolean;
}) {
  const ngayVaThu = `${Ngay.ngayGon(ngay).slice(0, 5)} ${Ngay.thu(ngay)}`;
  const nhan =
    cong !== undefined
      ? `${ngayVaThu}, đi làm ${Ngay.soCong(cong.tongCong)} công`
      : nghi
        ? `${ngayVaThu}, nghỉ`
        : `${ngayVaThu}, chưa tính`;

  return (
    <View
      style={[kieu.o, cong !== undefined ? kieu.oCong : nghi ? kieu.oNghi : kieu.oNgoai]}
      accessibilityLabel={nhan}
    >
      <Text
        style={[
          kieu.chuNgay,
          cong !== undefined ? kieu.chuNgayCong : nghi ? kieu.chuNgayNghi : kieu.chuNgayNgoai,
        ]}
      >
        {soTrongThang}
      </Text>

      {cong !== undefined && (
        <View style={kieu.dauTich}>
          <Feather name="check" size={13} color={Mau.xanhLa} />
          {/* Đi thiếu hoặc quá một ngày thì ghi rõ mấy công, kẻo tưởng ngày nào cũng như nhau. */}
          {cong.tongCong !== CONG_CA_NGAY && (
            <Text style={kieu.chuSoCong}>{Ngay.soCong(cong.tongCong)}</Text>
          )}
        </View>
      )}
    </View>
  );
}

const kieu = StyleSheet.create({
  lich: { gap: 4 },
  hang: { flexDirection: 'row', gap: 4 },

  chuCot: {
    flex: 1,
    textAlign: 'center',
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
    paddingBottom: 2,
  },

  o: {
    flex: 1,
    height: 46,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 1,
  },
  oCong: { backgroundColor: Mau.xanhLaNhat, borderWidth: 1, borderColor: Mau.xanhLa },
  oNghi: { backgroundColor: Mau.nen, borderWidth: 1, borderColor: Mau.vien },
  oNgoai: { backgroundColor: Mau.trang },

  chuNgay: { fontSize: Co.chuThuong },
  chuNgayCong: { fontFamily: PhongChu.dam, color: Mau.chu },
  chuNgayNghi: { fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNgayNgoai: { fontFamily: PhongChu.thuong, color: Mau.xam, opacity: 0.45 },

  dauTich: { flexDirection: 'row', alignItems: 'center', gap: 1 },
  chuSoCong: { fontSize: 10, fontFamily: PhongChu.vua, color: Mau.xanhLa },

  gach: { height: 1, backgroundColor: Mau.vien, marginTop: 6, marginBottom: 2 },
  chuThich: { flexDirection: 'row', flexWrap: 'wrap', gap: 14, paddingTop: 2 },
  mucChuThich: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  oMau: { width: 20, height: 20, borderRadius: 5, alignItems: 'center', justifyContent: 'center' },
  chuMuc: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.chu },
});
