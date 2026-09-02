import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { BuoiLam, CONG_MOT_BUOI, DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { CONG_TOI_DA, docSoCong } from '../nghiepvu/nhapSo';
import {
  dangCham,
  datCong,
  datGhiChuNgay,
  ghiChuNgay,
  thoDangLam,
} from '../nghiepvu/thaoTac';
import { HopChon } from './HopChon';
import { HopChonNgay } from './HopChonNgay';
import { HopNhapChu } from './HopNhapChu';
import { HopNhapSo } from './HopNhapSo';
import { NutChip, theTrang } from './ThanhPhan';
import { Bong, Co, HeSoChuToiDaLuoi, Mau, PhongChu, Tuoi } from './thietKe';

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
  /**
   * Tháng đang mở trong tờ lịch chọn ngày; null là chưa mở. Giữ nguyên một ngày bất kỳ
   * trong tháng ấy chứ không giữ riêng năm với tháng — cộng trừ tháng bằng `congNgay` là
   * xong, khỏi tự lo chuyện tháng 12 sang tháng 1.
   */
  const [mocLich, datMocLich] = useState<string | null>(null);
  const [dangSua, datDangSua] = useState<DangSua | null>(null);
  const [goSoCong, datGoSoCong] = useState(false);
  /** Đang mở hộp ghi chú cho thợ nào, ngày đang xem. */
  const [dangGhiChu, datDangGhiChu] = useState<Tho | null>(null);

  const thos = thoDangLam(duLieu);
  const dangXemNgayKhac = ngay !== Ngay.homNay();

  const soCongCua = (tho: Tho, buoi: BuoiLam) =>
    dangCham(duLieu, tho.id, ngay, buoi)?.soCong ?? null;
  const diDuCaNgay = (tho: Tho) =>
    soCongCua(tho, 'Sang') !== null && soCongCua(tho, 'Chieu') !== null;
  const ghiChuCua = (tho: Tho) => ghiChuNgay(duLieu, tho.id, ngay);

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
    datNgay(Ngay.congNgay(ngay, soTuan * 7));
  }

  function chonNgay(ngayMoi: string) {
    datNgay(ngayMoi);
  }

  /**
   * Lùi / tới một tháng trong tờ lịch. Nhảy qua hẳn mép tháng đang xem chứ không cộng 30
   * ngày: cộng ngày thì tháng thiếu tháng thừa sẽ có lúc nhảy vọt qua cả một tháng.
   */
  function doiThangLich(buoc: -1 | 1) {
    if (mocLich === null) {
      return;
    }

    const { nam, thang } = Ngay.tach(mocLich);
    datMocLich(
      buoc === -1
        ? Ngay.congNgay(Ngay.ghep(nam, thang, 1), -1)
        : Ngay.congNgay(Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang)), 1),
    );
  }

  /** Chạm ô đang xanh là bỏ chấm — sửa nhầm bằng đúng thao tác vừa rồi. */
  function bamO(tho: Tho, buoi: BuoiLam) {
    capNhat(
      datCong(duLieu, tho.id, ngay, buoi, soCongCua(tho, buoi) === null ? CONG_MOT_BUOI : null),
    );
  }

  /**
   * Bình thường cả tổ đi đủ nên bấm một cái là xong, rồi bỏ chấm vài người nghỉ —
   * nhanh hơn nhiều so với bấm từng ô. Đã đủ hết rồi thì nút này quay ra xoá sạch.
   */
  function bamCaTo() {
    const xoaHet = caToDaDu;
    let moi = duLieu;

    for (const tho of thos) {
      for (const buoi of ['Sang', 'Chieu'] as BuoiLam[]) {
        // Người đã chấm nửa buổi thì giữ nguyên, không ép thành cả buổi.
        moi = datCong(
          moi,
          tho.id,
          ngay,
          buoi,
          xoaHet ? null : soCongCua(tho, buoi) ?? CONG_MOT_BUOI,
        );
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

    // Một buổi đi đủ là nửa công, vì cả ngày mới là một công — xem `CONG_MOT_BUOI`.
    const soCong: Record<string, number | null> = {
      ca: CONG_MOT_BUOI,
      nua: CONG_MOT_BUOI / 2,
      ruoi: CONG_MOT_BUOI * 1.5,
      nghi: null,
    };
    capNhat(datCong(duLieu, dangSua.tho.id, ngay, dangSua.buoi, soCong[ma]));
    datDangSua(null);
  }

  function ghiGhiChu(chu: string) {
    if (dangGhiChu === null) {
      return;
    }

    capNhat(datGhiChuNgay(duLieu, dangGhiChu.id, ngay, chu));
    datDangGhiChu(null);
  }

  function ghiSoCong(so: number) {
    if (dangSua === null || dangSua.buoi === null) {
      return;
    }

    capNhat(datCong(duLieu, dangSua.tho.id, ngay, dangSua.buoi, so));
    datGoSoCong(false);
    datDangSua(null);
  }

  return (
    <View style={kieu.khung}>
      {/*
        Đầu trang không còn là dải trắng kẻ viền dưới: chữ nằm thẳng trên nền trang, ngày
        căn trái, hai nút đổi tuần dồn sang phải — đúng dáng đầu trang của bản thiết kế.
      */}
      <View style={kieu.dauTrang}>
        <View style={kieu.giuaDauTrang}>
          {/*
            Ngày đang xem chính là nút mở tờ lịch. Hai nút *Tuần* chỉ đi được từng tuần
            một: muốn xem lại tháng trước thì phải bấm năm sáu lần, mà xa hơn nữa thì
            người dùng bỏ cuộc trước khi tới nơi. Có mũi tên xuống với dấu lịch để nó
            trông ra một thứ bấm được, chứ không phải một dòng tiêu đề.
          */}
          <Pressable
            style={kieu.nutNgay}
            onPress={() => datMocLich(ngay)}
            accessibilityRole="button"
            accessibilityLabel={`${Ngay.thuVaNgay(ngay)}. Chạm để chọn ngày khác.`}
          >
            <Feather name="calendar" size={15} color={Mau.chinh} />
            <Text style={kieu.chuNgay} numberOfLines={1}>
              {Ngay.thuVaNgay(ngay)}
            </Text>
            <Feather name="chevron-down" size={16} color={Mau.xam} />
          </Pressable>
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

        <NutTuan huong={-1} onPress={() => doiTuan(-1)} />
        <NutTuan huong={1} onPress={() => doiTuan(1)} />
      </View>

      <DaiNgay ngayDangXem={ngay} congMoiNgay={congMoiNgay} onChon={chonNgay} />

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
              <NutChip nhan="Sửa" icon="edit-2" onPress={() => datDangSua({ tho, buoi: null })} />
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

            {/*
              Ghi chú chỉ chiếm chỗ khi thật có chữ. Ngày thường thì mười thẻ thợ trên màn
              hình phải gọn hết mức, không thêm một dòng "Thêm ghi chú" trống ở mỗi thẻ —
              đường vào lúc chưa có chữ nằm trong nút *Sửa*.
            */}
            {ghiChuCua(tho) !== '' && (
              <DongGhiChu
                chu={ghiChuCua(tho)}
                tenTho={tho.ten}
                onPress={() => datDangGhiChu(tho)}
              />
            )}
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
        Nửa buổi / buổi rưỡi để riêng sau nút Sửa: chín trên mười lần là một buổi đi đủ,
        không được bắt người dùng đi qua bước này mỗi ngày. Hai bước: chọn buổi rồi chọn số công.
      */}
      {dangSua !== null && dangSua.buoi === null && (
        <HopChon
          tieuDe={`${dangSua.tho.ten} — sửa gì?`}
          luaChon={[
            { ma: 'Sang', nhan: 'Buổi sáng', icon: 'sunrise' },
            { ma: 'Chieu', nhan: 'Buổi chiều', icon: 'sunset' },
            {
              ma: 'ghiChu',
              nhan: ghiChuCua(dangSua.tho) === '' ? 'Ghi chú cho ngày này' : 'Sửa ghi chú',
              icon: 'message-square',
            },
          ]}
          onChon={(ma) => {
            if (ma === 'ghiChu') {
              // Ghi chú là chuyện của cả ngày, không thuộc buổi nào: nhảy hẳn sang hộp
              // khác chứ không đi tiếp bước chọn số công.
              datDangGhiChu(dangSua.tho);
              datDangSua(null);
              return;
            }
            datDangSua({ tho: dangSua.tho, buoi: ma as BuoiLam });
          }}
          onDong={() => datDangSua(null)}
        />
      )}

      {dangSua !== null && dangSua.buoi !== null && !goSoCong && (
        <HopChon
          tieuDe={`${dangSua.tho.ten} — buổi ${dangSua.buoi === 'Sang' ? 'sáng' : 'chiều'}`}
          luaChon={[
            { ma: 'ca', nhan: 'Cả buổi (0,5 công)', icon: 'check' },
            { ma: 'nua', nhan: 'Nửa buổi (0,25 công)', icon: 'clock' },
            { ma: 'ruoi', nhan: 'Buổi rưỡi (0,75 công)', icon: 'plus-circle' },
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
          goiY="Ví dụ 0,5"
          doc={docSoCong}
          hienLai={(so) => `${Ngay.soCong(so)} công`}
          banPhim="decimal-pad"
          loi={(so) => (so > CONG_TOI_DA ? `Nhiều nhất ${Ngay.soCong(CONG_TOI_DA)} công một buổi.` : null)}
          onGhi={ghiSoCong}
          onDong={() => {
            datGoSoCong(false);
            datDangSua(null);
          }}
        />
      )}

      {/*
        Tờ lịch cả tháng, mỗi ô ghi luôn số công cả tổ ngày ấy: mở ra là thấy tháng trước
        ngày nào đã chấm ngày nào chưa, chạm một cái là sang đúng ngày cần xem hay chấm bù.
      */}
      {mocLich !== null && (
        <HopChonNgay
          tieuDe="Xem ngày nào?"
          nam={Ngay.tach(mocLich).nam}
          thang={Ngay.tach(mocLich).thang}
          ngayDangChon={ngay}
          congMoiNgay={congMoiNgay}
          onDoiThang={doiThangLich}
          onChon={(ngayMoi) => {
            chonNgay(ngayMoi);
            datMocLich(null);
          }}
          onDong={() => datMocLich(null)}
        />
      )}

      {dangGhiChu !== null && (
        <HopNhapChu
          tieuDe={`${dangGhiChu.ten} — ${Ngay.thuVaNgay(ngay)}`}
          moTa="Hôm ấy có gì đáng ghi?"
          goiY="Ví dụ: về sớm đi đám cưới"
          giaTriDau={ghiChuCua(dangGhiChu)}
          onGhi={ghiGhiChu}
          onDong={() => datDangGhiChu(null)}
        />
      )}
    </View>
  );
}

/**
 * Dòng ghi chú dưới hai ô chấm. Chạm vào là mở ra sửa ngay — ghi chú đang đọc chính là
 * nút sửa nó, không phải đi vòng qua nút *Sửa* rồi chọn lại.
 *
 * Hiện đủ ba dòng chứ không cắt còn một: ghi chú viết ra để đọc, mà cắt cụt thì lại phải
 * mở hộp lên mới biết trong đó viết gì.
 */
function DongGhiChu({
  chu,
  tenTho,
  onPress,
}: {
  chu: string;
  tenTho: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      style={kieu.dongGhiChu}
      onPress={onPress}
      accessibilityLabel={`Ghi chú của ${tenTho}: ${chu}. Chạm để sửa.`}
    >
      <Feather name="message-square" size={14} color={Mau.chinh} />
      <Text style={kieu.chuGhiChu} numberOfLines={3}>
        {chu}
      </Text>
    </Pressable>
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
        {daCham && soCong !== CONG_MOT_BUOI ? `  ${Ngay.soCong(soCong)}` : ''}
      </Text>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  dauTrang: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 10,
  },
  giuaDauTrang: { flex: 1, alignItems: 'flex-start', gap: 6 },
  // Nút ngày dính sát mép trái như dòng tiêu đề cũ, nên lề trái trừ đi phần đệm của nút.
  nutNgay: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 7,
    minHeight: 34,
    marginLeft: -8,
    paddingVertical: 4,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
  },
  // Nút trắng nổi bằng bóng, giống nút icon bên phải đầu trang của bản thiết kế.
  nutTuan: {
    minWidth: 52,
    minHeight: 46,
    paddingVertical: 5,
    paddingHorizontal: 6,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },
  chuNutTuan: { fontSize: 11, fontFamily: PhongChu.vua, color: Mau.chinh },
  chuNgay: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  daiNgay: { flexDirection: 'row', gap: 6, paddingHorizontal: 16, paddingBottom: 4 },
  oNgay: {
    flex: 1,
    paddingVertical: 9,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    gap: 2,
  },
  // Ngày đang xem tô đặc để nổi hẳn lên giữa sáu ngày còn lại.
  oNgayChon: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  oNgayHomNay: { backgroundColor: Mau.trang, borderColor: Tuoi.chinh },
  oNgayThuong: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  oNgayChuaToi: { opacity: 0.45 },

  chuThuGon: { fontSize: Co.chuNho, fontFamily: PhongChu.thuong, color: Mau.xam },
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
    marginHorizontal: 16,
    marginTop: 10,
    borderRadius: Co.bo,
    borderWidth: 1,
  },
  nutCaToThem: { backgroundColor: Mau.xanhLa, borderColor: Mau.xanhLa },
  nutCaToXoa: { backgroundColor: Mau.doNhat, borderColor: Mau.do },
  chuNutCaTo: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },

  danhSach: { padding: 16, paddingTop: 14, paddingBottom: 20 },
  the: { ...theTrang, marginBottom: 12, gap: 12 },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

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
  // Viền lấy màu tươi của bản thiết kế, chữ và dấu tích thì lấy màu đậm cho đọc được.
  oChamBat: { backgroundColor: Mau.xanhLaNhat, borderColor: Tuoi.xanhLa },
  oChamTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  chuOCham: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },

  /*
    Nền vàng nhạt để ghi chú không bị nhầm với một ô chấm nữa: hai ô ngay trên nó cũng là
    khối bo góc nền nhạt, mà ô thì bấm được để chấm còn dòng này thì không.
  */
  dongGhiChu: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 8,
    padding: 10,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinhNhat,
  },
  chuGhiChu: {
    flex: 1,
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.chu,
    lineHeight: 18,
  },

  trong: { padding: 24, paddingTop: 56, gap: 10, alignItems: 'center' },
  chuTrongTo: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuTrong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },

  /*
    Dòng tổng nằm thẳng trên nền trang, không có dải trắng kẻ viền. Thanh tab ngay dưới đã
    là một mảng trắng nổi bóng rồi — thêm một dải trắng nữa chồng lên là hai tầng bóng.
  */
  chanTrang: { paddingVertical: 12, alignItems: 'center' },
  chuTong: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTongSo: { fontFamily: PhongChu.dam, color: Mau.chu },
});
