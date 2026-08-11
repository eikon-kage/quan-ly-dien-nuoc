import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { BaoCaoTho } from '../nghiepvu/baoCao';
import * as Ngay from '../nghiepvu/ngayViet';
import { HopChonNgay } from './HopChonNgay';
import { LichCong } from './LichCong';
import { rungNhe } from './rungNhe';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  /**
   * Dựng báo cáo cho một khoảng ngày. Truyền hàm vào chứ không truyền thẳng dữ liệu:
   * kỳ đang mở và kỳ đã chốt lấy buổi công theo hai cách khác nhau (theo bản ghi nào đã
   * quyết toán), màn hình này không cần biết chuyện đó.
   */
  dungBaoCao: (tuNgay: string, denNgay: string) => BaoCaoTho | null;
  /** Khoảng mở ra lúc đầu — trọn kỳ đang xem. */
  tuNgayDau: string;
  denNgayDau: string;
  onDong: () => void;
}

/** Ngày viết gọn còn "05/08" — trong màn hình này năm đã ghi trên đầu rồi. */
function ngayNgan(ngay: string): string {
  return Ngay.ngayGon(ngay).slice(0, 5);
}

/**
 * Chi tiết một thợ: đi làm ngày nào, nghỉ ngày nào, ứng tiền ngày nào. Đây là chỗ tra
 * khi thợ thắc mắc "sao kỳ này ít tiền thế".
 *
 * Mở ra là trọn kỳ, nhưng chọn được khoảng hẹp hơn — nhiều nhà trả một phần giữa chừng
 * chứ không đợi chốt kỳ, lúc ấy con số cần nhìn là của mấy ngày đó chứ không phải cả kỳ.
 *
 * Kỳ chốt lúc nào cũng được nên nó hay vắt qua hai tháng. Mỗi tháng vẽ một tờ lịch riêng
 * xếp dọc, có tên tháng ở trên — gộp hai tháng vào một tờ thì không còn là tờ lịch treo
 * tường nữa, mà đó mới là thứ làm người xem nhìn ra ngay chỗ nghỉ nằm ở đâu.
 */
export function ManHinhBaoCaoTho({ dungBaoCao, tuNgayDau, denNgayDau, onDong }: Props) {
  const [tuNgay, datTuNgay] = useState(tuNgayDau);
  const [denNgay, datDenNgay] = useState(denNgayDau);
  const [dangChon, datDangChon] = useState<'tu' | 'den' | null>(null);

  const baoCao = dungBaoCao(tuNgay, denNgay);
  if (baoCao === null) {
    return null;
  }

  // Tờ lịch và hộp chọn ngày làm việc theo tháng, lấy tháng của ngày cuối khoảng.
  const { nam, thang } = Ngay.tach(denNgay);
  const dauThang = Ngay.ghep(nam, thang, 1);
  const cuoiThang = Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang));

  const { tho, ngayCongs, ngayNghis, ungTiens } = baoCao;
  const laCaKy = tuNgay === tuNgayDau && denNgay === denNgayDau;
  const laCaThang = tuNgay === dauThang && denNgay === cuoiThang;
  const cacThang = Ngay.cacThangTrongKhoang(tuNgay, denNgay);

  function datKhoang(tu: string, den: string) {
    rungNhe();
    datTuNgay(tu);
    datDenNgay(den);
  }

  /**
   * Chọn ngày đầu muộn hơn ngày cuối thì kéo luôn đầu kia theo, chứ không khoá ngày lại
   * cho bấm không ăn. Người dùng chỉ thấy một khoảng hợp lệ, không bao giờ gặp ngõ cụt.
   */
  function chonNgay(ngay: string) {
    rungNhe();

    if (dangChon === 'tu') {
      datTuNgay(ngay);
      if (ngay > denNgay) {
        datDenNgay(ngay);
      }
    } else {
      datDenNgay(ngay);
      if (ngay < tuNgay) {
        datTuNgay(ngay);
      }
    }

    datDangChon(null);
  }

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
              {laCaKy ? `Cả kỳ · ${Ngay.khoangGon(tuNgay, denNgay)}` : Ngay.khoangGon(tuNgay, denNgay)}
            </Text>
          </View>
          <View style={kieu.nutDong} />
        </View>

        {/*
          Hai nút ngày mở tờ lịch, mấy nút tắt bên dưới cho những khoảng hay dùng. Có nút
          tắt vì kỳ nửa tháng là chuyện lặp đi lặp lại — bắt chọn tay hai lần mỗi tháng
          thì phí. Nút "Cả kỳ" luôn đứng đầu: lỡ lọc hẹp rồi thì đó là đường về.
        */}
        <View style={kieu.hangLoc}>
          <View style={kieu.dongNgay}>
            <NutNgay nhan="Từ" ngay={tuNgay} onPress={() => datDangChon('tu')} />
            <Feather name="arrow-right" size={15} color={Mau.xam} />
            <NutNgay nhan="Đến" ngay={denNgay} onPress={() => datDangChon('den')} />
          </View>

          <View style={kieu.dongChip}>
            <Chip
              nhan="Cả kỳ"
              dangDung={laCaKy}
              onPress={() => datKhoang(tuNgayDau, denNgayDau)}
            />
            <Chip
              nhan="Cả tháng"
              dangDung={laCaThang}
              onPress={() => datKhoang(dauThang, cuoiThang)}
            />
            <Chip
              nhan="Nửa đầu"
              dangDung={tuNgay === dauThang && denNgay === Ngay.ghep(nam, thang, 15)}
              onPress={() => datKhoang(dauThang, Ngay.ghep(nam, thang, 15))}
            />
            <Chip
              nhan="Nửa cuối"
              dangDung={tuNgay === Ngay.ghep(nam, thang, 16) && denNgay === cuoiThang}
              onPress={() => datKhoang(Ngay.ghep(nam, thang, 16), cuoiThang)}
            />
          </View>
        </View>

        <ScrollView contentContainerStyle={kieu.trong}>
          {/* Mấy con số tóm tắt, để khỏi phải cộng lại từ bảng bên dưới. */}
          <View style={kieu.the}>
            <Dong nhan="Số công" gia={`${Ngay.soCong(baoCao.tongCong)} công`} />
            <Dong nhan="Tiền công" gia={Ngay.tien(baoCao.tienCong)} />
            {baoCao.daUng > 0 && <Dong nhan="Đã ứng" gia={Ngay.tienTru(baoCao.daUng)} />}
            {/*
              Kỳ trước trả thiếu thì phần thiếu nằm ở đây, không lẫn vào tiền công kỳ này —
              nhìn ra ngay đâu là công mới làm, đâu là nợ cũ mang sang.
            */}
            {baoCao.noKyTruoc !== 0 && (
              <Dong
                nhan={baoCao.noKyTruoc > 0 ? 'Nợ kỳ trước' : 'Kỳ trước trả dư'}
                gia={
                  baoCao.noKyTruoc > 0
                    ? Ngay.tien(baoCao.noKyTruoc)
                    : Ngay.tienTru(baoCao.noKyTruoc)
                }
              />
            )}
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
            {/*
              Vẫn vẽ trọn tháng dù đang lọc hẹp: ngày ngoài khoảng thành ô trắng, nhìn ra
              ngay phần nào đang tính. Cắt tờ lịch cho vừa khoảng thì mất chỗ dựa của mắt.
            */}
            {cacThang.map(({ nam: namLich, thang: thangLich }, thuTu) => {
              // So bằng đoạn đầu "2026-08" của chuỗi ngày: so mỗi số tháng thì kỳ vắt qua
              // đúng một năm sẽ trộn tháng 8 năm nay với tháng 8 năm ngoái.
              const moc = Ngay.ghep(namLich, thangLich, 1).slice(0, 7);
              return (
                <View key={moc} style={thuTu > 0 && kieu.lichSau}>
                  {/* Kỳ vắt qua nhiều tháng mới ghi tên tháng; một tháng thì đã có trên đầu rồi. */}
                  {cacThang.length > 1 && (
                    <Text style={kieu.chuThangLich}>
                      Tháng {thangLich}/{namLich}
                    </Text>
                  )}
                  <LichCong
                    nam={namLich}
                    thang={thangLich}
                    ngayCongs={ngayCongs.filter((d) => d.ngay.slice(0, 7) === moc)}
                    ngayNghis={ngayNghis.filter((n) => n.slice(0, 7) === moc)}
                  />
                </View>
              );
            })}
            {ngayCongs.length === 0 && (
              <Text style={kieu.chuTrong}>
                {laCaKy ? 'Kỳ này chưa có ngày công nào.' : 'Khoảng này chưa có ngày công nào.'}
              </Text>
            )}
          </View>

          <Text style={kieu.tieuDeMuc}>
            Ứng tiền{ungTiens.length > 0 ? ` (${ungTiens.length} lần)` : ''}
          </Text>
          <View style={[kieu.the, kieu.theCuoi]}>
            {ungTiens.length === 0 ? (
              <Text style={kieu.chuTrong}>
                {laCaKy ? 'Kỳ này chưa ứng lần nào.' : 'Khoảng này chưa ứng lần nào.'}
              </Text>
            ) : (
              ungTiens.map((ung) => (
                <View key={ung.id} style={kieu.dongUng}>
                  <View style={kieu.coNgay}>
                    <Text style={kieu.chuNgay}>{ngayNgan(ung.ngay)}</Text>
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

        {dangChon !== null && (
          <HopChonNgay
            tieuDe={dangChon === 'tu' ? 'Tính từ ngày nào?' : 'Tính đến ngày nào?'}
            nam={nam}
            thang={thang}
            ngayDangChon={dangChon === 'tu' ? tuNgay : denNgay}
            onChon={chonNgay}
            onDong={() => datDangChon(null)}
          />
        )}
      </SafeAreaView>
    </Modal>
  );
}

/** Nút mở tờ lịch. Ngày hiện luôn trên nút để khỏi phải mở ra mới biết đang lọc từ đâu. */
function NutNgay({ nhan, ngay, onPress }: { nhan: string; ngay: string; onPress: () => void }) {
  return (
    <Pressable
      style={kieu.nutNgay}
      onPress={onPress}
      accessibilityLabel={`${nhan} ngày ${ngayNgan(ngay)}, chạm để đổi`}
    >
      <Text style={kieu.chuNhanNgay}>{nhan}</Text>
      <Text style={kieu.chuNgayLoc}>{ngayNgan(ngay)}</Text>
      <Feather name="calendar" size={14} color={Mau.chinh} />
    </Pressable>
  );
}

/** Khoảng đang dùng thì nút đổi cả nền lẫn màu chữ lẫn nét chữ, không chỉ mỗi màu. */
function Chip({
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
      style={[kieu.chip, dangDung ? kieu.chipDung : kieu.chipThuong]}
      onPress={onPress}
      accessibilityState={{ selected: dangDung }}
    >
      <Text style={[kieu.chuChip, dangDung && kieu.chuChipDung]}>{nhan}</Text>
    </Pressable>
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

  hangLoc: {
    backgroundColor: Mau.trang,
    paddingHorizontal: 12,
    paddingBottom: 10,
    gap: 8,
    borderBottomWidth: 1,
    borderBottomColor: Mau.vien,
  },
  dongNgay: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 10 },
  nutNgay: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 7,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.nen,
  },
  chuNhanNgay: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNgayLoc: { fontSize: Co.chuThuong, fontFamily: PhongChu.dam, color: Mau.chu },

  dongChip: { flexDirection: 'row', gap: 6 },
  chip: {
    flex: 1,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 4,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chipThuong: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chipDung: { backgroundColor: Mau.chinhNhat, borderColor: Mau.chinh },
  chuChip: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
  chuChipDung: { fontFamily: PhongChu.dam, color: Mau.chinh },

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

  lichSau: { marginTop: 14, paddingTop: 12, borderTopWidth: 1, borderTopColor: Mau.vien },
  chuThangLich: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    marginBottom: 8,
  },

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
