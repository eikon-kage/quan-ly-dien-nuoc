import { Feather } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { doiChieu } from '../nghiepvu/doiChieu';
import { BuoiLam, CAC_BUOI, DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { soCuaMay } from '../nghiepvu/soCong';
import { boCham, cham, dangCham, timTho } from '../nghiepvu/thaoTac';
import { CaiDatVai } from '../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from './dungDoiChieu';
import { HopChon } from './HopChon';
import { HopVaiMay } from './HopVaiMay';
import { ManHinhDoiChieu } from './ManHinhDoiChieu';
import { DauTrang, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu, Tuoi } from './thietKe';

/**
 * Màn hình riêng của máy thợ.
 *
 * Làm hẳn một màn hình khác chứ không lọc bớt màn hình của chủ. Hai người vào app vì hai
 * việc khác nhau: chủ chấm cho mười người rồi tính tiền, còn thợ chỉ trả lời đúng một câu
 * hỏi — *hôm nay tôi có đi làm không*. Lọc bớt màn hình của chủ thì còn lại một cái lưới
 * một cột, thừa đủ thứ dây nhợ mà vẫn không có cái nút to nhất đáng có.
 *
 * Không có thanh tab: cả máy thợ chỉ có một màn hình này, mở ra là chấm được ngay.
 * Không có một con số tiền nào ở đây — cả app trên máy thợ không biết tiền công là bao
 * nhiêu, xem `ketNap`.
 */

/** Hôm nay và 13 ngày trước. Đủ để chấm bù một hai tuần lỡ quên, không phải cuộn cả tháng. */
const SO_NGAY = 14;

const TEN_BUOI: Record<BuoiLam, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  caiDat: CaiDatVai;
  datCaiDat: (moi: CaiDatVai) => void;
  dieuKhien: DieuKhienDoiChieu;
}

export function ManHinhThoTuCham({ duLieu, capNhat, caiDat, datCaiDat, dieuKhien }: Props) {
  const [moDoiChieu, datMoDoiChieu] = useState(false);
  const [moVaiMay, datMoVaiMay] = useState(false);
  /** Ô đang mở hộp chọn số công. */
  const [dangSua, datDangSua] = useState<{ ngay: string; buoi: BuoiLam } | null>(null);

  const thoId = caiDat.thoId ?? '';
  const homNay = Ngay.homNay();
  const cacNgay = useMemo(
    () => Array.from({ length: SO_NGAY }, (_, i) => Ngay.congNgay(homNay, -i)),
    [homNay],
  );

  /** Tên lấy từ sổ chủ gửi xuống nếu có: chủ mới là bên đặt tên, thợ không phải tự gõ. */
  const tenTho =
    dieuKhien.soBenKia.get(thoId)?.so.tenTho || timTho(duLieu, thoId)?.ten || 'Tôi';

  const ket = useMemo(() => {
    const cuaChu = dieuKhien.soBenKia.get(thoId);
    if (!cuaChu) {
      return null;
    }
    return doiChieu(soCuaMay(duLieu, caiDat, thoId, homNay), cuaChu.so);
  }, [duLieu, caiDat, dieuKhien.soBenKia, thoId, homNay]);

  const tongCong = duLieu.buoiCongs
    .filter((b) => b.thoId === thoId && b.ngay >= cacNgay[SO_NGAY - 1])
    .reduce((tong, b) => tong + b.soCong, 0);

  function doCham(ngay: string, buoi: BuoiLam) {
    const dang = dangCham(duLieu, thoId, ngay, buoi);
    capNhat(dang ? boCham(duLieu, thoId, ngay, buoi) : cham(duLieu, thoId, ngay, buoi));
  }

  function chonSoCong(ma: string) {
    if (dangSua === null) {
      return;
    }
    const { ngay, buoi } = dangSua;

    if (ma === 'nghi') {
      capNhat(boCham(duLieu, thoId, ngay, buoi));
    } else {
      const soCong = ma === 'nua' ? 0.5 : ma === 'ruoi' ? 1.5 : 1;
      capNhat(cham(duLieu, thoId, ngay, buoi, soCong));
    }
    datDangSua(null);
  }

  if (moDoiChieu) {
    return (
      <ManHinhDoiChieu
        duLieu={duLieu}
        capNhat={capNhat}
        caiDat={caiDat}
        dieuKhien={dieuKhien}
        onDong={() => datMoDoiChieu(false)}
      />
    );
  }

  const { dangChay, daNoi, hoTro, lucCuoi, loi } = dieuKhien.trangThai;
  const chuDongBo = !hoTro
    ? 'Cần bản app cài thẳng vào máy'
    : !daNoi
      ? 'Chưa nối Google — sổ chưa gửi cho chủ'
      : loi !== null
        ? loi
        : dangChay
          ? 'Đang gửi sổ…'
          : lucCuoi !== null
            ? `Đã gửi sổ lúc ${Ngay.gioPhut(lucCuoi)}`
            : 'Chưa gửi sổ lần nào';

  return (
    <View style={kieu.khung}>
      <DauTrang
        tieuDe={tenTho}
        phu={chuDongBo}
        phai={
          <Pressable
            style={kieu.nutDongBo}
            onPress={daNoi ? dieuKhien.dongBo : dieuKhien.noiGoogle}
            disabled={!hoTro || dangChay}
            accessibilityLabel={daNoi ? 'Gửi sổ cho chủ' : 'Nối Google'}
          >
            {dangChay ? (
              <ActivityIndicator color={Mau.chinh} />
            ) : (
              <Feather name={daNoi ? 'refresh-cw' : 'cloud'} size={18} color={Mau.chinh} />
            )}
          </Pressable>
        }
      />

      <ScrollView contentContainerStyle={kieu.than}>
        {/*
          Hôm nay tách hẳn ra một thẻ với hai ô cao gấp rưỡi những ngày khác. Chín phần
          mười lần mở app là để chấm cho hôm nay — cái nút ấy phải là thứ to nhất màn hình,
          không phải một dòng lẫn trong danh sách.
        */}
        <View style={kieu.theHomNay}>
          <Text style={kieu.chuHomNay}>Hôm nay, {Ngay.thuVaNgay(homNay)}</Text>
          <View style={kieu.hangO}>
            {CAC_BUOI.map((buoi) => (
              <OCham
                key={buoi}
                nhan={TEN_BUOI[buoi]}
                to
                soCong={dangCham(duLieu, thoId, homNay, buoi)?.soCong ?? null}
                onPress={() => doCham(homNay, buoi)}
                onLongPress={() => datDangSua({ ngay: homNay, buoi })}
              />
            ))}
          </View>
        </View>

        <Pressable
          style={kieu.dongDoiChieu}
          onPress={() => datMoDoiChieu(true)}
          accessibilityRole="button"
        >
          <Feather
            name={ket === null ? 'inbox' : ket.lechs.length === 0 ? 'check-circle' : 'alert-circle'}
            size={17}
            color={ket === null ? Mau.xam : ket.lechs.length === 0 ? Mau.xanhLa : Mau.do}
          />
          <View style={kieu.giuaDong}>
            <Text style={kieu.chuNhan}>Đối chiếu với sổ chủ</Text>
            <Text style={kieu.chuPhu}>
              {ket === null
                ? 'Chưa có sổ của chủ'
                : ket.khongTrungKhoang
                  ? 'Chưa so được'
                  : ket.lechs.length === 0
                    ? `Khớp cả ${ket.soKhop} buổi`
                    : `Lệch ${ket.lechs.length} buổi`}
            </Text>
          </View>
          <Feather name="chevron-right" size={17} color={Mau.xam} />
        </Pressable>

        <Text style={kieu.chuMuc}>
          {SO_NGAY} ngày gần đây · {Ngay.soCong(tongCong)} công
        </Text>

        {/* Bỏ hôm nay: nó đã ở thẻ trên, để lại đây nữa là hai chỗ chấm cùng một buổi. */}
        {cacNgay.slice(1).map((ngay) => (
          <View key={ngay} style={kieu.dongNgay}>
            <Text style={kieu.chuNgay}>{Ngay.thuVaNgay(ngay)}</Text>
            <View style={kieu.hangO}>
              {CAC_BUOI.map((buoi) => (
                <OCham
                  key={buoi}
                  nhan={TEN_BUOI[buoi]}
                  soCong={dangCham(duLieu, thoId, ngay, buoi)?.soCong ?? null}
                  onPress={() => doCham(ngay, buoi)}
                  onLongPress={() => datDangSua({ ngay, buoi })}
                />
              ))}
            </View>
          </View>
        ))}

        <Pressable style={kieu.dongCaiDat} onPress={() => datMoVaiMay(true)}>
          <Feather name="user" size={15} color={Mau.xam} />
          <Text style={kieu.chuPhu}>Máy của thợ · đổi lại</Text>
        </Pressable>
      </ScrollView>

      {dangSua !== null && (
        <HopChon
          tieuDe={`${Ngay.thuVaNgay(dangSua.ngay)} — buổi ${dangSua.buoi === 'Sang' ? 'sáng' : 'chiều'}`}
          luaChon={[
            { ma: 'ca', nhan: 'Cả công (1)', icon: 'check' },
            { ma: 'nua', nhan: 'Nửa công (0,5)', icon: 'clock' },
            { ma: 'ruoi', nhan: 'Công rưỡi (1,5)', icon: 'plus-circle' },
            { ma: 'nghi', nhan: 'Nghỉ buổi này', icon: 'x-circle', nguyHiem: true },
          ]}
          onChon={chonSoCong}
          onDong={() => datDangSua(null)}
        />
      )}

      {moVaiMay && (
        <HopVaiMay
          duLieu={duLieu}
          capNhat={capNhat}
          caiDat={caiDat}
          datCaiDat={datCaiDat}
          onDong={() => datMoVaiMay(false)}
        />
      )}
    </View>
  );
}

/**
 * Ô chấm một buổi. Giống ô bên màn hình của chủ — cùng ba tín hiệu (nền, dấu tích, màu
 * chữ) để hai bên nhìn ra cùng một thứ khi ngồi soát với nhau.
 */
function OCham({
  nhan,
  soCong,
  to = false,
  onPress,
  onLongPress,
}: {
  nhan: string;
  soCong: number | null;
  to?: boolean;
  onPress: () => void;
  onLongPress: () => void;
}) {
  const daCham = soCong !== null;

  return (
    <Pressable
      style={[kieu.oCham, to && kieu.oChamTo, daCham ? kieu.oChamBat : kieu.oChamTat]}
      onPress={onPress}
      onLongPress={onLongPress}
      accessibilityLabel={`${nhan} ${daCham ? 'có đi làm' : 'chưa chấm'}`}
      accessibilityHint="Bấm để đổi, bấm giữ để chọn nửa công"
    >
      <Feather
        name={daCham ? 'check-circle' : 'circle'}
        size={to ? 20 : 16}
        color={daCham ? Mau.xanhLa : Mau.xam}
      />
      <Text
        style={[kieu.chuOCham, to && kieu.chuOChamTo, { color: daCham ? Mau.chu : Mau.xam }]}
      >
        {nhan}
        {daCham && soCong !== 1 ? `  ${Ngay.soCong(soCong)}` : ''}
      </Text>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  than: { padding: 16, paddingTop: 4, gap: 10, paddingBottom: 28 },

  nutDongBo: {
    width: 44,
    height: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },

  theHomNay: { ...theTrang, gap: 12, marginBottom: 2 },
  chuHomNay: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  dongDoiChieu: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 12,
  },
  giuaDong: { flex: 1, gap: 2 },
  chuNhan: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  chuMuc: {
    marginTop: 8,
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
  },

  dongNgay: { ...theTrang, gap: 8, paddingVertical: 10 },
  chuNgay: { fontSize: Co.chuSo, fontFamily: PhongChu.vua, color: Mau.chu },

  hangO: { flexDirection: 'row', gap: 10 },
  oCham: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 9,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
    borderWidth: 1,
  },
  oChamTo: { minHeight: 72 },
  oChamBat: { backgroundColor: Mau.xanhLaNhat, borderColor: Tuoi.xanhLa },
  oChamTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  chuOCham: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
  chuOChamTo: { fontSize: Co.chuTieuDe },

  dongCaiDat: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNutNho,
    marginTop: 8,
  },
});
