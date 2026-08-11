import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { BuoiLam, DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { CONG_TOI_DA, docSoCong } from '../nghiepvu/nhapSo';
import { dangCham, datCong, thoDangLam } from '../nghiepvu/thaoTac';
import { HopChon } from './HopChon';
import { HopNhapSo } from './HopNhapSo';
import { rungNhe } from './rungNhe';
import { Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './thietKe';

/** Đang mở hộp sửa cho thợ nào: chưa chọn buổi thì buoi là null. */
interface DangSua {
  tho: Tho;
  buoi: BuoiLam | null;
}

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
}

export function ManHinhChamCong({ duLieu, capNhat }: Props) {
  const [ngay, datNgay] = useState(Ngay.homNay());
  const [dangSua, datDangSua] = useState<DangSua | null>(null);
  const [goSoCong, datGoSoCong] = useState(false);

  const thos = thoDangLam(duLieu);
  const dangXemNgayKhac = ngay !== Ngay.homNay();

  const soCongCua = (tho: Tho, buoi: BuoiLam) =>
    dangCham(duLieu, tho.id, ngay, buoi)?.soCong ?? null;
  const diDuCaNgay = (tho: Tho) =>
    soCongCua(tho, 'Sang') !== null && soCongCua(tho, 'Chieu') !== null;

  /** Tổng công cả tổ của từng ngày — dải ngày lấy ở đây ra để hiện ngày nào đã chấm. */
  const congMoiNgay = new Map<string, number>();
  for (const b of duLieu.buoiCongs) {
    if (thos.some((t) => t.id === b.thoId)) {
      congMoiNgay.set(b.ngay, (congMoiNgay.get(b.ngay) ?? 0) + b.soCong);
    }
  }

  const tongCong = congMoiNgay.get(ngay) ?? 0;
  const caToDaDu = thos.length > 0 && thos.every(diDuCaNgay);

  function doiTuan(soTuan: number) {
    rungNhe();
    datNgay(Ngay.congNgay(ngay, soTuan * 7));
  }

  function chonNgay(ngayMoi: string) {
    rungNhe();
    datNgay(ngayMoi);
  }

  /** Chạm ô đang xanh là bỏ chấm — sửa nhầm bằng đúng thao tác vừa rồi. */
  function bamO(tho: Tho, buoi: BuoiLam) {
    rungNhe();
    capNhat(datCong(duLieu, tho.id, ngay, buoi, soCongCua(tho, buoi) === null ? 1 : null));
  }

  /**
   * Bình thường cả tổ đi đủ nên bấm một cái là xong, rồi bỏ chấm vài người nghỉ —
   * nhanh hơn nhiều so với bấm từng ô. Đã đủ hết rồi thì nút này quay ra xoá sạch.
   */
  function bamCaTo() {
    rungNhe();
    const xoaHet = caToDaDu;
    let moi = duLieu;

    for (const tho of thos) {
      for (const buoi of ['Sang', 'Chieu'] as BuoiLam[]) {
        // Người đã chấm nửa công thì giữ nguyên nửa công, không ép thành một công.
        moi = datCong(moi, tho.id, ngay, buoi, xoaHet ? null : soCongCua(tho, buoi) ?? 1);
      }
    }

    capNhat(moi);
  }

  function chonSoCong(ma: string) {
    if (dangSua === null || dangSua.buoi === null) {
      return;
    }

    // Số nào cũng gõ được, nhưng ba mức hay dùng để sẵn thành nút cho khỏi phải gõ.
    if (ma === 'goSo') {
      datGoSoCong(true);
      return;
    }

    const soCong: Record<string, number | null> = { ca: 1, nua: 0.5, ruoi: 1.5, nghi: null };
    rungNhe();
    capNhat(datCong(duLieu, dangSua.tho.id, ngay, dangSua.buoi, soCong[ma]));
    datDangSua(null);
  }

  function ghiSoCong(so: number) {
    if (dangSua === null || dangSua.buoi === null) {
      return;
    }

    rungNhe();
    capNhat(datCong(duLieu, dangSua.tho.id, ngay, dangSua.buoi, so));
    datGoSoCong(false);
    datDangSua(null);
  }

  return (
    <View style={kieu.khung}>
      <View style={kieu.dauTrang}>
        <View style={kieu.dongNgay}>
          <NutTuan huong={-1} onPress={() => doiTuan(-1)} />

          <View style={kieu.giuaDauTrang}>
            <Text style={kieu.chuNgay}>{Ngay.thuVaNgay(ngay)}</Text>
            {dangXemNgayKhac && (
              <Pressable
                style={kieu.nutHomNay}
                onPress={() => chonNgay(Ngay.homNay())}
                accessibilityLabel="Về hôm nay"
              >
                <Feather name="corner-up-left" size={13} color={Mau.chinh} />
                <Text style={kieu.chuHomNay}>Hôm nay</Text>
              </Pressable>
            )}
          </View>

          <NutTuan huong={1} onPress={() => doiTuan(1)} />
        </View>

        <DaiNgay ngayDangXem={ngay} congMoiNgay={congMoiNgay} onChon={chonNgay} />
      </View>

      {thos.length > 0 && (
        <Pressable
          style={[kieu.nutCaTo, caToDaDu ? kieu.nutCaToXoa : kieu.nutCaToThem]}
          onPress={bamCaTo}
        >
          <Feather
            name={caToDaDu ? 'rotate-ccw' : 'check-circle'}
            size={16}
            color={caToDaDu ? Mau.do : Mau.trang}
          />
          <Text style={[kieu.chuNutCaTo, { color: caToDaDu ? Mau.do : Mau.trang }]}>
            {caToDaDu ? 'Xoá hết chấm ngày này' : 'Cả tổ đi đủ cả ngày'}
          </Text>
        </Pressable>
      )}

      <FlatList
        data={thos}
        keyExtractor={(tho) => tho.id}
        extraData={duLieu}
        contentContainerStyle={kieu.danhSach}
        ListEmptyComponent={
          <View style={kieu.trong}>
            <Feather name="users" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Chưa có thợ nào</Text>
            <Text style={kieu.chuTrong}>Bấm mục Thợ ở thanh dưới để thêm thợ trước đã.</Text>
          </View>
        }
        renderItem={({ item: tho }) => (
          <View style={kieu.the}>
            <View style={kieu.dongTen}>
              <Text style={kieu.chuTen} numberOfLines={1}>
                {tho.ten}
              </Text>
              {/* Icon không đứng một mình — người dùng không đoán hình. */}
              <Pressable style={kieu.nutSua} onPress={() => datDangSua({ tho, buoi: null })}>
                <Feather name="edit-2" size={12} color={Mau.chinh} />
                <Text style={kieu.chuNutSua}>Sửa</Text>
              </Pressable>
            </View>

            <View style={kieu.dongO}>
              <OCham
                nhan="Sáng"
                soCong={soCongCua(tho, 'Sang')}
                onPress={() => bamO(tho, 'Sang')}
              />
              <OCham
                nhan="Chiều"
                soCong={soCongCua(tho, 'Chieu')}
                onPress={() => bamO(tho, 'Chieu')}
              />
            </View>
          </View>
        )}
      />

      <View style={kieu.chanTrang}>
        <Text style={kieu.chuTong}>
          {dangXemNgayKhac ? 'Ngày này' : 'Hôm nay'}:{' '}
          <Text style={kieu.chuTongSo}>{Ngay.soCong(tongCong)} công</Text>
        </Text>
      </View>

      {/*
        Nửa công / công rưỡi để riêng sau nút Sửa: chín trên mười lần là một công tròn,
        không được bắt người dùng đi qua bước này mỗi ngày. Hai bước: chọn buổi rồi chọn số công.
      */}
      {dangSua !== null && dangSua.buoi === null && (
        <HopChon
          tieuDe={`${dangSua.tho.ten} — sửa buổi nào?`}
          luaChon={[
            { ma: 'Sang', nhan: 'Buổi sáng', icon: 'sunrise' },
            { ma: 'Chieu', nhan: 'Buổi chiều', icon: 'sunset' },
          ]}
          onChon={(ma) => datDangSua({ tho: dangSua.tho, buoi: ma as BuoiLam })}
          onDong={() => datDangSua(null)}
        />
      )}

      {dangSua !== null && dangSua.buoi !== null && !goSoCong && (
        <HopChon
          tieuDe={`${dangSua.tho.ten} — buổi ${dangSua.buoi === 'Sang' ? 'sáng' : 'chiều'}`}
          luaChon={[
            { ma: 'ca', nhan: 'Cả công (1)', icon: 'check' },
            { ma: 'nua', nhan: 'Nửa công (0,5)', icon: 'clock' },
            { ma: 'ruoi', nhan: 'Công rưỡi (1,5)', icon: 'plus-circle' },
            { ma: 'goSo', nhan: 'Gõ số công khác', icon: 'edit-3' },
            { ma: 'nghi', nhan: 'Nghỉ buổi này', icon: 'x-circle', nguyHiem: true },
          ]}
          onChon={chonSoCong}
          onDong={() => datDangSua(null)}
        />
      )}

      {goSoCong && dangSua !== null && dangSua.buoi !== null && (
        <HopNhapSo
          tieuDe={`${dangSua.tho.ten} — buổi ${dangSua.buoi === 'Sang' ? 'sáng' : 'chiều'}`}
          moTa="Buổi này mấy công?"
          goiY="Ví dụ 0,75"
          doc={docSoCong}
          hienLai={(so) => `${Ngay.soCong(so)} công`}
          banPhim="decimal-pad"
          loi={(so) => (so > CONG_TOI_DA ? `Nhiều nhất ${CONG_TOI_DA} công một buổi.` : null)}
          onGhi={ghiSoCong}
          onDong={() => {
            datGoSoCong(false);
            datDangSua(null);
          }}
        />
      )}
    </View>
  );
}

/**
 * Mũi tên lùi / tới một tuần. Có chữ "Tuần" đi kèm: mũi tên trơ trọi thì người dùng
 * không đoán được nó nhảy một ngày hay cả tuần.
 */
function NutTuan({ huong, onPress }: { huong: -1 | 1; onPress: () => void }) {
  return (
    <Pressable
      style={kieu.nutTuan}
      onPress={onPress}
      accessibilityLabel={huong === -1 ? 'Tuần trước' : 'Tuần sau'}
    >
      <Feather
        name={huong === -1 ? 'chevron-left' : 'chevron-right'}
        size={20}
        color={Mau.chinh}
      />
      <Text style={kieu.chuNutTuan}>Tuần</Text>
    </Pressable>
  );
}

/**
 * Dải bảy ngày của tuần đang xem, mỗi ngày một ô bấm được.
 *
 * Trước đây đầu màn hình chỉ có mỗi ngày đang xem với hai mũi tên lùi / tới một ngày:
 * muốn xem lại thứ Hai tuần này thì bấm bốn năm lần, mà bấm rồi vẫn không biết ngày nào
 * đã chấm ngày nào chưa. Dải này cho thấy cả tuần cùng lúc kèm số công từng ngày, chạm
 * một cái là sang đúng ngày cần.
 */
function DaiNgay({
  ngayDangXem,
  congMoiNgay,
  onChon,
}: {
  ngayDangXem: string;
  congMoiNgay: Map<string, number>;
  onChon: (ngay: string) => void;
}) {
  const homNay = Ngay.homNay();

  return (
    <View style={kieu.daiNgay}>
      {Ngay.tuan(ngayDangXem).map((n) => {
        const dangChon = n === ngayDangXem;
        const laHomNay = n === homNay;
        // Chuỗi "2026-08-03" so sánh thẳng được vì năm đứng trước, tháng rồi mới tới ngày.
        const chuaToi = n > homNay;
        const cong = congMoiNgay.get(n) ?? 0;

        return (
          <Pressable
            key={n}
            style={[
              kieu.oNgay,
              dangChon ? kieu.oNgayChon : laHomNay ? kieu.oNgayHomNay : kieu.oNgayThuong,
              chuaToi && !dangChon && kieu.oNgayChuaToi,
            ]}
            onPress={() => onChon(n)}
            accessibilityLabel={`Chọn ${Ngay.thuVaNgay(n)}`}
            accessibilityHint={cong > 0 ? `${Ngay.soCong(cong)} công` : 'Chưa chấm ngày này'}
            accessibilityState={{ selected: dangChon }}
          >
            {/* Hôm nay ghi hẳn chữ "Nay" thay cho thứ — khỏi phải nhớ hôm nay thứ mấy. */}
            <Text
              style={[kieu.chuThuGon, dangChon && kieu.chuTrenNenXanh]}
              maxFontSizeMultiplier={HeSoChuToiDaLuoi}
            >
              {laHomNay ? 'Nay' : Ngay.thuGon(n)}
            </Text>
            <Text
              style={[kieu.chuSoNgay, dangChon && kieu.chuTrenNenXanh]}
              maxFontSizeMultiplier={HeSoChuToiDaLuoi}
            >
              {Ngay.ngayGon(n).slice(0, 2)}
            </Text>
            {/*
              Ngày chưa chấm để dấu chấm mờ chứ không bỏ trống: bỏ trống thì ô cao thấp
              khác nhau, nhìn dải bị gãy.
            */}
            <Text
              style={[
                kieu.chuCongNgay,
                cong === 0 && kieu.chuChuaCham,
                dangChon && kieu.chuTrenNenXanh,
              ]}
              maxFontSizeMultiplier={HeSoChuToiDaLuoi}
            >
              {cong > 0 ? Ngay.soCong(cong) : '·'}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

/**
 * Ô chấm một buổi. Đã chấm thì vừa đổi nền, vừa đổi dấu tròn thành dấu tích, vừa đổi
 * màu chữ — ba tín hiệu chứ không chỉ mỗi màu.
 */
function OCham({
  nhan,
  soCong,
  onPress,
}: {
  nhan: string;
  soCong: number | null;
  onPress: () => void;
}) {
  const daCham = soCong !== null;

  return (
    <Pressable
      style={[kieu.oCham, daCham ? kieu.oChamBat : kieu.oChamTat]}
      onPress={onPress}
      accessibilityLabel={`${nhan} ${daCham ? 'có đi làm' : 'chưa chấm'}`}
    >
      <Feather
        name={daCham ? 'check-circle' : 'circle'}
        size={17}
        color={daCham ? Mau.xanhLa : Mau.xam}
      />
      <Text style={[kieu.chuOCham, { color: daCham ? Mau.chu : Mau.xam }]}>
        {nhan}
        {daCham && soCong !== 1 ? `  ${Ngay.soCong(soCong)}` : ''}
      </Text>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  dauTrang: {
    backgroundColor: Mau.trang,
    paddingHorizontal: 12,
    paddingVertical: 10,
    gap: 10,
    borderBottomWidth: 1,
    borderBottomColor: Mau.vien,
  },
  dongNgay: { flexDirection: 'row', alignItems: 'center' },
  giuaDauTrang: { flex: 1, alignItems: 'center', gap: 4 },
  nutTuan: {
    minWidth: 52,
    minHeight: 46,
    paddingVertical: 5,
    paddingHorizontal: 6,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinhNhat,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chuNutTuan: { fontSize: 11, fontFamily: PhongChu.vua, color: Mau.chinh },
  chuNgay: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },

  daiNgay: { flexDirection: 'row', gap: 5 },
  oNgay: {
    flex: 1,
    paddingVertical: 7,
    borderRadius: 9,
    borderWidth: 1,
    alignItems: 'center',
    gap: 1,
  },
  // Ngày đang xem tô đặc để nổi hẳn lên giữa sáu ngày còn lại.
  oNgayChon: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  oNgayHomNay: { backgroundColor: Mau.chinhNhat, borderColor: Mau.chinh },
  oNgayThuong: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  oNgayChuaToi: { opacity: 0.45 },

  chuThuGon: { fontSize: 11, fontFamily: PhongChu.vua, color: Mau.xam },
  chuSoNgay: { fontSize: Co.chuSo, fontFamily: PhongChu.dam, color: Mau.chu },
  chuCongNgay: { fontSize: 11, fontFamily: PhongChu.vua, color: Mau.xanhLa },
  chuChuaCham: { color: Mau.xam },
  chuTrenNenXanh: { color: Mau.trang },
  nutHomNay: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    minHeight: 28,
    paddingVertical: 5,
    paddingHorizontal: 12,
    // Bo tròn hẳn chứ không lấy nửa chiều cao: cỡ chữ to thì nút cao lên, số cứng hoá vuông góc.
    borderRadius: 999,
    backgroundColor: Mau.chinhNhat,
  },
  chuHomNay: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  nutCaTo: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    marginHorizontal: 14,
    marginTop: 14,
    borderRadius: Co.bo,
    borderWidth: 1,
  },
  nutCaToThem: { backgroundColor: Mau.xanhLa, borderColor: Mau.xanhLa },
  nutCaToXoa: { backgroundColor: Mau.doNhat, borderColor: Mau.do },
  chuNutCaTo: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },

  danhSach: { padding: 14, paddingBottom: 20 },
  the: {
    backgroundColor: Mau.trang,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Mau.vien,
    padding: 12,
    marginBottom: 10,
    gap: 10,
  },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  nutSua: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: 8,
    backgroundColor: Mau.chinhNhat,
  },
  chuNutSua: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  dongO: { flexDirection: 'row', gap: 10 },
  oCham: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoOCham,
    paddingVertical: 10,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
    borderWidth: 1,
  },
  // Nền xanh nhạt chứ không tô đặc: ô này lặp lại nhiều lần, tô đặc thì cả màn hình rợp màu.
  oChamBat: { backgroundColor: Mau.xanhLaNhat, borderColor: Mau.xanhLa },
  oChamTat: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuOCham: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },

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
