import { Feather } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { DongLech, doiChieu } from '../nghiepvu/doiChieu';
import * as Ngay from '../nghiepvu/ngayViet';
import {
  NgayTrongSo,
  SoCong,
  gomTheoNgay,
  khoangCuaSo,
  ngayNghiTrongSo,
} from '../nghiepvu/soCong';
import { LichCong } from './LichCong';
import { DauTrang, HangO, TheSo, ThanhDoan, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu } from './thietKe';

/**
 * Sổ công của chính thợ, xem chi tiết từng ngày — bản dành cho máy thợ của màn hình chi
 * tiết mà chủ vẫn mở từ bảng lương.
 *
 * Vì sao cần: màn hình chính của máy thợ chỉ có 14 ngày gần đây, mà thợ thắc mắc thì hay
 * thắc mắc chuyện *tháng trước* — "hôm mùng mười tôi có đi không". Trước đây thợ không có
 * đường nào xem, phải hỏi chủ mở máy chủ ra tra hộ. Giờ hai bên nhìn **cùng một tờ lịch,
 * cùng một cách chia nửa tháng**, nên ngồi soát với nhau là chỉ tay vào cùng một ô.
 *
 * Khác bên chủ đúng một điều, và là điều bắt buộc: **không có đồng tiền nào**. Chỗ chủ để
 * lưới tiền công / đã ứng / còn phải trả thì đây là số ngày đi làm, số ngày nghỉ và số buổi
 * lệch sổ chủ. Chuyện ấy được đảm bảo từ *dữ liệu vào* chứ không phải từ giao diện: màn hình
 * này dựng trên [SoCong](../nghiepvu/soCong.ts) — mẩu dữ liệu cắt tiền ra từ lúc đóng gói —
 * chứ không dựng trên `DuLieuChamCong` như bên chủ. Không có tiền trong tay thì không có
 * đường nào lỡ hiện tiền ra.
 *
 * Chỉ xem, không sửa: chấm và chấm bù vẫn ở màn hình chính, giữ đúng một chỗ chấm cho một
 * buổi. Hai chỗ chấm được cùng một buổi là hai chỗ để bấm nhầm.
 *
 * Vẽ **đè thẳng lên chỗ của màn hình chính** chứ không bọc trong `Modal`, giống màn hình đối
 * chiếu: cửa sổ của `Modal` là một cửa sổ khác, nằm ngoài `SafeAreaView` của App, nên đầu
 * trang chạy tọt lên dưới thanh trạng thái. Ở đây thì lề an toàn của App vẫn tính cho nó.
 */

/** Ba khoảng có sẵn. *Cả tháng* đứng đầu: lỡ lọc hẹp rồi thì đó là đường về. */
const KHOANG_SAN = [
  { ma: 'thang', nhan: 'Cả tháng' },
  { ma: 'dau', nhan: 'Nửa đầu' },
  { ma: 'cuoi', nhan: 'Nửa cuối' },
];

interface Props {
  /** Sổ của chính máy này, đã cắt đúng khoảng nó khai là đầy đủ. */
  so: SoCong;
  /** Sổ chủ gửi xuống, có thì mỗi ngày lệch được đánh dấu. Chưa nhận được thì null. */
  soChu: SoCong | null;
  homNay: string;
  onDong: () => void;
}

/** Ngày viết gọn còn "05/08" — tháng và năm đã ghi trên đầu rồi. */
function ngayNgan(ngay: string): string {
  return Ngay.ngayGon(ngay).slice(0, 5);
}

export function ManHinhSoCuaToi({ so, soChu, homNay, onDong }: Props) {
  /** Ngày nào cũng được, miễn nằm trong tháng đang xem — lấy tháng của nó ra dùng. */
  const [mocThang, datMocThang] = useState(homNay);
  const [doan, datDoan] = useState('thang');

  const { nam, thang } = Ngay.tach(mocThang);
  const dauThang = Ngay.ghep(nam, thang, 1);
  const cuoiThang = Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang));

  const [tuNgay, denNgay] =
    doan === 'dau'
      ? [dauThang, Ngay.ghep(nam, thang, 15)]
      : doan === 'cuoi'
        ? [Ngay.ghep(nam, thang, 16), cuoiThang]
        : [dauThang, cuoiThang];

  /**
   * Chặn hai đầu thay vì để bấm ra tháng trống. Sổ này chỉ đầy đủ trong khoảng nó khai,
   * ngoài ra là *không biết* chứ không phải không đi làm — cho lùi mãi thì thợ xem được
   * mười tờ lịch trắng rồi tưởng máy mất dữ liệu.
   *
   * Chặn theo `khoangCuaSo` chứ không theo đúng khoảng khai: buổi thợ chấm bù ra trước hôm
   * nhận vai máy nằm ngoài khoảng khai, mà nó là công thật thợ vừa tự bấm — chặn trước nó
   * là màn hình chính hiện ô đã chấm còn sổ của chính mình lại không có ngày ấy.
   */
  const khoangSo = khoangCuaSo(so);
  const cuoiSo = khoangSo.denNgay < homNay ? khoangSo.denNgay : homNay;
  const coThangTruoc = Ngay.congNgay(dauThang, -1) >= khoangSo.tuNgay;
  const coThangSau = Ngay.congNgay(cuoiThang, 1) <= cuoiSo;

  const ngayCongs = useMemo(() => gomTheoNgay(so, tuNgay, denNgay), [so, tuNgay, denNgay]);
  const ngayNghis = useMemo(
    () => ngayNghiTrongSo(so, tuNgay, denNgay, homNay),
    [so, tuNgay, denNgay, homNay],
  );
  const tongCong = ngayCongs.reduce((tong, ngay) => tong + ngay.tongCong, 0);

  /**
   * Buổi lệch sổ chủ, gom theo ngày. Không nói ai đúng ai sai — sửa vẫn là việc của màn
   * hình đối chiếu, ở đây chỉ đánh dấu để thợ biết ngày nào cần mở ra soát.
   */
  const lechTheoNgay = useMemo(() => {
    const theoNgay = new Map<string, DongLech[]>();
    if (soChu === null) {
      return theoNgay;
    }

    for (const lech of doiChieu(so, soChu, homNay).lechs) {
      if (lech.ngay < tuNgay || lech.ngay > denNgay) {
        continue;
      }
      theoNgay.set(lech.ngay, [...(theoNgay.get(lech.ngay) ?? []), lech]);
    }
    return theoNgay;
  }, [so, soChu, tuNgay, denNgay]);

  const soBuoiLech = [...lechTheoNgay.values()].reduce((tong, cac) => tong + cac.length, 0);

  /**
   * Danh sách từng ngày, ngày mới nhất lên trên — giống danh sách chấm ở màn hình chính,
   * và cũng là thứ hay phải tra nhất. Chỉ chạy trong phần sổ khai là đầy đủ và đã trôi qua.
   */
  const batDauDong = tuNgay > khoangSo.tuNgay ? tuNgay : khoangSo.tuNgay;
  const ketThucDong = denNgay < cuoiSo ? denNgay : cuoiSo;
  const cacDong: NgayTrongSo[] = [];
  const congTheoNgay = new Map(ngayCongs.map((ngay) => [ngay.ngay, ngay]));
  for (let ngay = batDauDong; ngay <= ketThucDong; ngay = Ngay.congNgay(ngay, 1)) {
    cacDong.push(
      congTheoNgay.get(ngay) ?? {
        ngay,
        congSang: null,
        congChieu: null,
        tongCong: 0,
        daChot: false,
      },
    );
  }
  cacDong.reverse();

  function doiThang(buoc: -1 | 1) {
    datMocThang(buoc === -1 ? Ngay.congNgay(dauThang, -1) : Ngay.congNgay(cuoiThang, 1));
  }

  return (
    <View style={kieu.khung}>
      <DauTrang
        tieuDe="Sổ công của tôi"
        phu={Ngay.khoangGon(tuNgay, denNgay)}
        onLui={onDong}
      />

      <View style={kieu.hangLoc}>
        {/* Đổi tháng bằng hai mũi tên, y như đổi tuần bên màn hình chấm công. */}
        <View style={kieu.dongThang}>
          <NutThang huong={-1} tat={!coThangTruoc} onPress={() => doiThang(-1)} />
          <Text style={kieu.chuThang}>
            Tháng {thang}/{nam}
          </Text>
          <NutThang huong={1} tat={!coThangSau} onPress={() => doiThang(1)} />
        </View>

        {/*
          Nửa đầu / nửa cuối để sẵn vì nhiều nhà trả tiền theo nửa tháng: lúc soát thì con
          số cần nhìn là của mấy ngày ấy, mà bắt chọn tay hai lần mỗi tháng thì phí.
        */}
        <ThanhDoan cac={KHOANG_SAN} dangChon={doan} onChon={datDoan} />
      </View>

      <ScrollView contentContainerStyle={kieu.trong}>
        {/*
          Lưới 2×2 giống bên chủ, nhưng bốn ô là bốn con số **không dính tiền**: công, ngày
          đi làm, ngày nghỉ, và số buổi ghi khác sổ chủ.
        */}
        <View style={kieu.luoiO}>
          <HangO>
            <TheSo nhan="Số công" so={`${Ngay.soCong(tongCong)} công`} mau="chinh" />
            <TheSo nhan="Đi làm" so={`${ngayCongs.length} ngày`} mau="xanhLa" />
          </HangO>
          <HangO>
            <TheSo nhan="Nghỉ" so={`${ngayNghis.length} ngày`} mau="ngoc" />
            <TheSo
              nhan="So với sổ chủ"
              so={
                soChu === null
                  ? 'Chưa có'
                  : soBuoiLech === 0
                    ? 'Khớp cả'
                    : `${soBuoiLech} buổi`
              }
              mau={soBuoiLech > 0 ? 'do' : 'xanhLa'}
            />
          </HangO>
        </View>

        <Text style={kieu.tieuDeMuc}>Lịch đi làm</Text>
        <View style={kieu.the}>
          {/*
            Vẫn vẽ trọn tháng dù đang lọc nửa tháng: ngày ngoài khoảng thành ô trắng, nhìn
            ra ngay phần nào đang tính. Cắt tờ lịch cho vừa khoảng thì mất chỗ dựa của mắt.
          */}
          <LichCong nam={nam} thang={thang} ngayCongs={ngayCongs} ngayNghis={ngayNghis} />
        </View>

        <Text style={kieu.tieuDeMuc}>Chi tiết từng ngày</Text>
        <View style={[kieu.the, kieu.theCuoi]}>
          {cacDong.length === 0 ? (
            <Text style={kieu.chuTrong}>
              Sổ trong máy chỉ có từ {ngayNgan(so.tuNgay)} trở đi.
            </Text>
          ) : (
            cacDong.map((ngay) => (
              <DongNgay
                key={ngay.ngay}
                ngay={ngay}
                laHomNay={ngay.ngay === homNay}
                soLech={lechTheoNgay.get(ngay.ngay)?.length ?? 0}
              />
            ))
          )}
        </View>
      </ScrollView>
    </View>
  );
}

/** Mũi tên đổi tháng. Hết sổ thì mờ đi và bấm không ăn, không phải bấm rồi mới biết. */
function NutThang({
  huong,
  tat,
  onPress,
}: {
  huong: -1 | 1;
  tat: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      style={[kieu.nutThang, tat && kieu.nutTat]}
      onPress={onPress}
      disabled={tat}
      accessibilityLabel={huong === -1 ? 'Tháng trước' : 'Tháng sau'}
      accessibilityState={{ disabled: tat }}
    >
      <Feather
        name={huong === -1 ? 'chevron-left' : 'chevron-right'}
        size={20}
        color={tat ? Mau.xam : Mau.chu}
      />
    </Pressable>
  );
}

/**
 * Một ngày: ngày và thứ bên trái, hai buổi ở giữa, tổng công bên phải.
 *
 * Buổi không chấm vẫn hiện tên buổi kèm dấu gạch chứ không biến mất — "Sáng 1 · Chiều —"
 * nói rõ là *chỉ đi buổi sáng*, còn để trống một bên thì phải nhớ xem bên nào mất.
 */
function DongNgay({
  ngay,
  laHomNay,
  soLech,
}: {
  ngay: NgayTrongSo;
  laHomNay: boolean;
  soLech: number;
}) {
  const nghi = ngay.tongCong === 0;
  const buoi = (soCong: number | null) => (soCong === null ? '—' : Ngay.soCong(soCong));

  return (
    <View style={[kieu.dongNgay, laHomNay && kieu.dongHomNay]}>
      <View style={kieu.coNgay}>
        <Text style={kieu.chuNgay}>{ngayNgan(ngay.ngay)}</Text>
        <Text style={kieu.chuThu}>{laHomNay ? 'Hôm nay' : Ngay.thu(ngay.ngay)}</Text>
      </View>

      <View style={kieu.giuaDong}>
        <Text style={[kieu.chuBuoi, nghi && kieu.chuNghi]}>
          {nghi ? 'Nghỉ' : `Sáng ${buoi(ngay.congSang)} · Chiều ${buoi(ngay.congChieu)}`}
        </Text>
        {soLech > 0 && (
          <View style={kieu.dongLech}>
            <Feather name="alert-circle" size={12} color={Mau.do} />
            <Text style={kieu.chuLech}>
              {soLech > 1 ? `Sổ chủ ghi khác ${soLech} buổi` : 'Sổ chủ ghi khác'}
            </Text>
          </View>
        )}
      </View>

      {!nghi && <Text style={kieu.chuTongCong}>{Ngay.soCong(ngay.tongCong)} công</Text>}
    </View>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  hangLoc: { paddingHorizontal: 16, paddingBottom: 10, gap: 10 },
  dongThang: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  chuThang: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  nutThang: {
    width: 44,
    height: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },
  nutTat: { opacity: 0.4 },

  trong: { padding: 16, paddingTop: 4, paddingBottom: 24 },
  luoiO: { gap: 11 },
  tieuDeMuc: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
    marginTop: 16,
    marginBottom: 6,
    marginLeft: 2,
  },
  the: { ...theTrang, gap: 6 },
  theCuoi: { marginBottom: 8 },

  dongNgay: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingVertical: 7,
    paddingHorizontal: 6,
    borderRadius: Co.boNho,
  },
  // Hôm nay tô nền nhạt: cuộn xuống một tháng rồi cuộn lên vẫn tìm lại được chỗ mình đứng.
  dongHomNay: { backgroundColor: Mau.chinhNhat },
  coNgay: { width: 66 },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuThu: { fontSize: 11, fontFamily: PhongChu.thuong, color: Mau.xam },
  giuaDong: { flex: 1, gap: 2 },
  chuBuoi: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.chu },
  chuNghi: { color: Mau.xam },
  dongLech: { flexDirection: 'row', alignItems: 'center', gap: 5 },
  chuLech: { fontSize: 11, fontFamily: PhongChu.vua, color: Mau.do },
  chuTongCong: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.xanhLa },

  chuTrong: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
});
