import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import { baoCaoKyHienTai, kyHienTai } from '../nghiepvu/ky';
import * as Ngay from '../nghiepvu/ngayViet';
import { themUng } from '../nghiepvu/thaoTac';
import { HopNhapSo } from './HopNhapSo';
import { ManHinhBaoCaoTho } from './ManHinhBaoCaoTho';
import { ManHinhQuyetToan } from './ManHinhQuyetToan';
import { DauTrang, NutChip, theTrang } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
}

/**
 * Bảng lương của **kỳ đang mở**: từ sau lần quyết toán trước tới hôm nay.
 *
 * Trước đây màn hình này xem theo tháng, đổi tháng bằng hai mũi tên. Bỏ đi vì tiền công
 * ngoài công trình không chạy theo tháng: xong việc là trả, có khi mười ngày, có khi
 * sáu tuần. Muốn xem lại tháng nào đã trả bao nhiêu thì sang mục *Kỳ đã chốt*.
 */
export function ManHinhBangLuong({ duLieu, capNhat }: Props) {
  const homNay = Ngay.homNay();
  const [dangUng, datDangUng] = useState<Tho | null>(null);
  const [xemBaoCao, datXemBaoCao] = useState<string | null>(null);
  const [dangQuyetToan, datDangQuyetToan] = useState(false);

  const ky = kyHienTai(duLieu, homNay);

  function ghiUng(soTien: number, ghiChu: string) {
    if (dangUng === null) {
      return;
    }

    capNhat(themUng(duLieu, dangUng.id, homNay, soTien, ghiChu));
    datDangUng(null);
  }

  return (
    <View style={kieu.khung}>
      {/*
        Không còn mũi tên đổi tháng: chỉ có đúng một kỳ đang mở, không có gì để đổi qua
        đổi lại. Khoảng ngày ghi ngay dưới tiêu đề để biết đang tính từ hôm nào.
      */}
      <DauTrang
        tieuDe="Kỳ này"
        phu={
          ky.dongs.length === 0
            ? 'Chưa có công nào'
            : `${Ngay.khoangGon(ky.tuNgay, ky.denNgay)} · ${Ngay.thu(ky.denNgay)}`
        }
      />

      <FlatList
        data={ky.dongs}
        keyExtractor={(dong) => dong.tho.id}
        contentContainerStyle={kieu.danhSach}
        ListEmptyComponent={
          <View style={kieu.trong}>
            <Feather name="credit-card" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Kỳ này chưa có công nào</Text>
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
              <NutChip
                nhan="Ứng tiền"
                icon="arrow-up-right"
                onPress={() => datDangUng(dong.tho)}
              />
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

            {/*
              Nợ kỳ trước đứng thành một dòng riêng, không cộng thầm vào tiền công — thợ
              hỏi "sao kỳ này nhiều thế" thì chỉ đúng vào dòng này mà trả lời.
            */}
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

      {ky.chotDuoc && (
        <View style={kieu.chanTrang}>
          <Text style={kieu.chuTong}>
            Cả tổ còn phải trả: <Text style={kieu.chuTongSo}>{Ngay.tien(ky.tongPhaiTra)}</Text>
          </Text>

          {/*
            Nút quyết toán không chốt luôn mà mở ra màn hình đếm tiền — chốt kỳ là việc
            nặng nhất trong app, phải nhìn thấy từng người bao nhiêu trước khi gật đầu.
          */}
          <Pressable
            style={kieu.nutQuyetToan}
            onPress={() => datDangQuyetToan(true)}
          >
            <Feather name="check-circle" size={17} color={Mau.trang} />
            <Text style={kieu.chuNutQuyetToan}>Quyết toán kỳ này</Text>
          </Pressable>
        </View>
      )}

      {dangUng !== null && (
        <HopNhapSo
          tieuDe={`${dangUng.ten} ứng tiền`}
          moTa="Thợ ứng bao nhiêu?"
          goiY="Ví dụ 500000"
          oChu={{ nhan: 'Ghi chú (không bắt buộc)', goiY: 'Ví dụ: ứng đổ xăng' }}
          onGhi={ghiUng}
          onDong={() => datDangUng(null)}
        />
      )}

      {xemBaoCao !== null && (
        <ManHinhBaoCaoTho
          dungBaoCao={(tu, den) => baoCaoKyHienTai(duLieu, xemBaoCao, homNay, tu, den)}
          tuNgayDau={ky.tuNgay}
          denNgayDau={ky.denNgay}
          onDong={() => datXemBaoCao(null)}
        />
      )}

      {dangQuyetToan && (
        <ManHinhQuyetToan
          duLieu={duLieu}
          homNay={homNay}
          capNhat={capNhat}
          onDong={() => datDangQuyetToan(false)}
        />
      )}
    </View>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  danhSach: { padding: 16, paddingTop: 4, paddingBottom: 20 },
  the: { ...theTrang, marginBottom: 12, gap: 7 },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

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

  // Nằm thẳng trên nền trang: thanh tab ngay dưới đã là mảng trắng nổi bóng rồi.
  chanTrang: { paddingHorizontal: 16, paddingVertical: 12, gap: 10, alignItems: 'center' },
  chuTong: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTongSo: { fontFamily: PhongChu.dam, color: Mau.chu },
  nutQuyetToan: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    alignSelf: 'stretch',
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  chuNutQuyetToan: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.trang,
    textAlign: 'center',
  },
});
