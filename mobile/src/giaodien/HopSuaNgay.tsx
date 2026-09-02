import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { BuoiLam, CAC_BUOI, CONG_MOT_BUOI } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { CONG_TOI_DA, docSoCong } from '../nghiepvu/nhapSo';
import { HopChon, LuaChon } from './HopChon';
import { HopDay } from './HopDay';
import { HopNhapChu } from './HopNhapChu';
import { HopNhapSo } from './HopNhapSo';
import { NutChip } from './ThanhPhan';
import { Co, Mau, PhongChu, Tuoi } from './thietKe';

/**
 * Sửa **một ngày của một thợ** ngay tại chỗ đang xem — chạm một ô trên tờ lịch là mở
 * hộp này ra.
 *
 * Vì sao cần: tờ lịch ở màn hình thống kê là chỗ người ta *nhìn ra chỗ sai* — "hôm mười
 * bảy tôi có đi mà" — nhưng trước đây nó chỉ để nhìn. Chữa lại phải thoát ra, sang mục
 * Chấm công, chọn đúng ngày ấy, tìm đúng thợ ấy trong danh sách. Bốn bước cho một cái
 * dấu tích, mà lúc quay lại thì đã mất chỗ đang đứng.
 *
 * Dùng chung cho cả hai bên — máy chủ (màn hình thống kê một thợ) và máy thợ (sổ công
 * của tôi) — nên **không nhận `DuLieuChamCong`**: đọc và ghi đều qua mấy hàm chỗ gọi
 * truyền vào. Máy thợ dựng màn hình của nó trên `SoCong`, mẩu dữ liệu cố tình không có
 * đồng tiền nào; bắt nó đưa cả sổ vào đây là mở lại đúng cánh cửa ấy.
 *
 * Mấy bước sau (chọn số công, gõ số, ghi chú) **thay chỗ** hộp này chứ không chồng lên
 * trên, y như [HopSuaUng](./HopSuaUng.tsx): hộp này vốn đã nằm trong modal của màn hình
 * thống kê rồi, chồng thêm tầng nữa trên iOS là chuyện hên xui.
 */

const TEN_BUOI: Record<BuoiLam, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

/** Cách đọc một buổi và cách ghi lại, do chỗ gọi đưa vào. */
export interface CachSuaNgay {
  /** Số công đã chấm của một buổi; null là chưa chấm. */
  cong: (ngay: string, buoi: BuoiLam) => number | null;
  /**
   * Buổi đã nằm trong một kỳ đã chốt. Hộp vẫn hiện nó nhưng **khoá lại**: tiền của buổi
   * ấy đã trao tay, sửa số công bây giờ chỉ làm sổ nói khác tờ quyết toán thợ đang cầm.
   * Không truyền thì không khoá gì — máy thợ không chốt kỳ bao giờ.
   */
  khoa?: (ngay: string, buoi: BuoiLam) => boolean;
  /** Đặt số công cho một buổi; null là cho nghỉ buổi ấy. */
  datCong: (ngay: string, buoi: BuoiLam, soCong: number | null) => void;
  /**
   * Ghi chú cho cả ngày. Không truyền thì hộp không có phần ghi chú: máy thợ không có
   * ghi chú, mà một dòng "Thêm ghi chú" bấm không ăn thì thà đừng vẽ.
   */
  ghiChu?: {
    doc: (ngay: string) => string;
    ghi: (ngay: string, chu: string) => void;
  };
}

interface Props {
  ngay: string;
  /** Tên thợ ghi lên đầu hộp. Máy thợ chỉ có một người nên bỏ trống. */
  tenTho?: string;
  sua: CachSuaNgay;
  onDong: () => void;
}

/** Hộp đang mở tới bước nào. Mỗi bước vẽ ra một hộp, không bao giờ hai hộp cùng lúc. */
type Buoc =
  | { ten: 'chinh' }
  | { ten: 'chonViec' }
  | { ten: 'mucCong'; buoi: BuoiLam }
  | { ten: 'goSo'; buoi: BuoiLam }
  | { ten: 'ghiChu' };

export function HopSuaNgay({ ngay, tenTho, sua, onDong }: Props) {
  const [buoc, datBuoc] = useState<Buoc>({ ten: 'chinh' });

  const congCua = (buoi: BuoiLam) => sua.cong(ngay, buoi);
  const khoaCua = (buoi: BuoiLam) => sua.khoa?.(ngay, buoi) ?? false;

  const tongCong = CAC_BUOI.reduce((tong, buoi) => tong + (congCua(buoi) ?? 0), 0);
  const coBuoiKhoa = CAC_BUOI.some(khoaCua);
  const chu = sua.ghiChu?.doc(ngay) ?? '';
  const tieuDe =
    tenTho === undefined ? Ngay.thuVaNgay(ngay) : `${tenTho} — ${Ngay.thuVaNgay(ngay)}`;

  /** Chạm ô đang xanh là bỏ chấm, y như bên màn hình chấm công. */
  function bamO(buoi: BuoiLam) {
    sua.datCong(ngay, buoi, congCua(buoi) === null ? CONG_MOT_BUOI : null);
  }

  function chonMucCong(buoi: BuoiLam, ma: string) {
    if (ma === 'goSo') {
      datBuoc({ ten: 'goSo', buoi });
      return;
    }

    // Một buổi đi đủ là nửa công, vì cả ngày mới là một công — xem `CONG_MOT_BUOI`.
    const soCong: Record<string, number | null> = {
      ca: CONG_MOT_BUOI,
      nua: CONG_MOT_BUOI / 2,
      ruoi: CONG_MOT_BUOI * 1.5,
      nghi: null,
    };
    sua.datCong(ngay, buoi, soCong[ma]);
    datBuoc({ ten: 'chinh' });
  }

  if (buoc.ten === 'ghiChu' && sua.ghiChu !== undefined) {
    const { ghi } = sua.ghiChu;
    return (
      <HopNhapChu
        tieuDe={tieuDe}
        moTa="Hôm ấy có gì đáng ghi?"
        goiY="Ví dụ: về sớm đi đám cưới"
        giaTriDau={chu}
        onGhi={(moi) => {
          ghi(ngay, moi);
          datBuoc({ ten: 'chinh' });
        }}
        onDong={() => datBuoc({ ten: 'chinh' })}
      />
    );
  }

  if (buoc.ten === 'goSo') {
    const { buoi } = buoc;
    const dangCo = congCua(buoi);
    return (
      // Cùng hộp, cùng cách đọc số và cùng mức chặn như bên màn hình chấm công: hai chỗ
      // gõ ra hai kiểu số thì đối chiếu báo lệch mà chẳng ai sai.
      <HopNhapSo
        tieuDe={`${TEN_BUOI[buoi]} · ${Ngay.thuVaNgay(ngay)}`}
        moTa="Buổi này mấy công?"
        goiY="Ví dụ 0,5"
        giaTriDau={dangCo === null ? '' : Ngay.soCong(dangCo)}
        doc={docSoCong}
        hienLai={(so) => `${Ngay.soCong(so)} công`}
        banPhim="decimal-pad"
        loi={(so) =>
          so > CONG_TOI_DA ? `Nhiều nhất ${Ngay.soCong(CONG_TOI_DA)} công một buổi.` : null
        }
        onGhi={(so) => {
          sua.datCong(ngay, buoi, so);
          datBuoc({ ten: 'chinh' });
        }}
        onDong={() => datBuoc({ ten: 'chinh' })}
      />
    );
  }

  if (buoc.ten === 'mucCong') {
    const { buoi } = buoc;
    return (
      <HopChon
        tieuDe={`${TEN_BUOI[buoi]} · ${Ngay.thuVaNgay(ngay)}`}
        luaChon={[
          { ma: 'ca', nhan: 'Cả buổi (0,5 công)', icon: 'check' },
          { ma: 'nua', nhan: 'Nửa buổi (0,25 công)', icon: 'clock' },
          { ma: 'ruoi', nhan: 'Buổi rưỡi (0,75 công)', icon: 'plus-circle' },
          { ma: 'goSo', nhan: 'Gõ số công khác', icon: 'edit-3' },
          { ma: 'nghi', nhan: 'Nghỉ buổi này', icon: 'x-circle', nguyHiem: true },
        ]}
        onChon={(ma) => chonMucCong(buoi, ma)}
        onDong={() => datBuoc({ ten: 'chinh' })}
      />
    );
  }

  if (buoc.ten === 'chonViec') {
    // Buổi đã chốt kỳ không có mặt trong danh sách này: mở ra rồi mới biết không sửa được
    // là bắt người dùng đi hai bước để nhận một câu từ chối.
    const viec: LuaChon[] = CAC_BUOI.filter((buoi) => !khoaCua(buoi)).map((buoi) => ({
      ma: buoi,
      nhan: `Buổi ${TEN_BUOI[buoi].toLowerCase()}`,
      icon: buoi === 'Sang' ? 'sunrise' : 'sunset',
    }));

    if (sua.ghiChu !== undefined) {
      viec.push({
        ma: 'ghiChu',
        nhan: chu === '' ? 'Ghi chú cho ngày này' : 'Sửa ghi chú',
        icon: 'message-square',
      });
    }

    return (
      <HopChon
        tieuDe={`${tieuDe} — sửa gì?`}
        luaChon={viec}
        onChon={(ma) => {
          // Ghi chú là chuyện của cả ngày, không thuộc buổi nào: nhảy hẳn sang hộp khác
          // chứ không đi tiếp bước chọn số công.
          datBuoc(ma === 'ghiChu' ? { ten: 'ghiChu' } : { ten: 'mucCong', buoi: ma as BuoiLam });
        }}
        onDong={() => datBuoc({ ten: 'chinh' })}
      />
    );
  }

  return (
    <HopDay onDong={onDong}>
      <View style={kieu.dongTieuDe}>
        <Text style={kieu.tieuDe} numberOfLines={2}>
          {tieuDe}
        </Text>
        {/*
          Nút *Sửa* mở đường tới nửa buổi, buổi rưỡi và ghi chú — giống hệt thẻ thợ ở màn
          hình chấm công. Chín trên mười lần chỉ cần chạm hai ô dưới đây, không được bắt
          người dùng đi qua thêm một bước mỗi lần.
        */}
        <NutChip nhan="Sửa" icon="edit-2" onPress={() => datBuoc({ ten: 'chonViec' })} />
      </View>

      <View style={kieu.dongO}>
        {CAC_BUOI.map((buoi) => (
          <OCham
            key={buoi}
            nhan={TEN_BUOI[buoi]}
            soCong={congCua(buoi)}
            khoa={khoaCua(buoi)}
            onPress={() => bamO(buoi)}
          />
        ))}
      </View>

      {/*
        Cả ngày mấy công — con số người ta vừa nhìn thấy trên ô lịch, để đối chiếu ngay
        rằng mình vừa sửa đúng chỗ.
      */}
      <Text style={kieu.chuTong}>
        {tongCong > 0 ? `Cả ngày ${Ngay.soCong(tongCong)} công` : 'Ngày này chưa chấm công nào'}
      </Text>

      {coBuoiKhoa && (
        <Text style={kieu.chuKhoa}>
          Buổi có ổ khoá đã nằm trong một kỳ đã chốt — tiền trả rồi nên không sửa được nữa.
          Cần sửa thật thì bỏ chốt kỳ ấy ở mục Kỳ đã chốt.
        </Text>
      )}

      {sua.ghiChu !== undefined && chu !== '' && (
        <Pressable
          style={kieu.dongGhiChu}
          onPress={() => datBuoc({ ten: 'ghiChu' })}
          accessibilityLabel={`Ghi chú: ${chu}. Chạm để sửa.`}
        >
          <Feather name="message-square" size={14} color={Mau.chinh} />
          <Text style={kieu.chuGhiChu} numberOfLines={3}>
            {chu}
          </Text>
        </Pressable>
      )}

      <Pressable style={kieu.nutXong} onPress={onDong}>
        <Text style={kieu.chuNutXong}>Xong</Text>
      </Pressable>
    </HopDay>
  );
}

/**
 * Ô chấm một buổi, cùng dáng với ô ở màn hình chấm công: đã chấm thì đổi cả nền, cả dấu
 * tròn thành dấu tích, cả màu chữ — ba tín hiệu chứ không chỉ mỗi màu.
 *
 * Buổi đã chốt kỳ thì vẽ ra một `View` trơ chứ không phải `Pressable` bấm không ăn, và
 * mang ổ khoá thay cho dấu tích.
 */
function OCham({
  nhan,
  soCong,
  khoa,
  onPress,
}: {
  nhan: string;
  soCong: number | null;
  khoa: boolean;
  onPress: () => void;
}) {
  const daCham = soCong !== null;
  const chuTrongO = `${nhan}${daCham && soCong !== CONG_MOT_BUOI ? `  ${Ngay.soCong(soCong)}` : ''}`;

  const noiDung = (
    <>
      <Feather
        name={khoa ? 'lock' : daCham ? 'check-circle' : 'circle'}
        size={17}
        color={khoa ? Mau.xam : daCham ? Mau.xanhLa : Mau.xam}
      />
      <Text style={[kieu.chuOCham, { color: daCham && !khoa ? Mau.chu : Mau.xam }]}>
        {chuTrongO}
      </Text>
    </>
  );

  if (khoa) {
    return (
      <View
        style={[kieu.oCham, kieu.oChamKhoa]}
        accessibilityLabel={`${nhan} ${daCham ? 'có đi làm' : 'chưa chấm'}, đã chốt kỳ nên không sửa được`}
      >
        {noiDung}
      </View>
    );
  }

  return (
    <Pressable
      style={[kieu.oCham, daCham ? kieu.oChamBat : kieu.oChamTat]}
      onPress={onPress}
      accessibilityLabel={`${nhan} ${daCham ? 'có đi làm' : 'chưa chấm'}, chạm để đổi`}
    >
      {noiDung}
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  dongTieuDe: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingBottom: 2 },
  tieuDe: { flex: 1, fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },

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
  oChamBat: { backgroundColor: Mau.xanhLaNhat, borderColor: Tuoi.xanhLa },
  oChamTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  oChamKhoa: { backgroundColor: Mau.nen, borderColor: Mau.vien, opacity: 0.7 },
  chuOCham: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },

  chuTong: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xam, marginLeft: 2 },
  chuKhoa: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    lineHeight: 18,
    marginLeft: 2,
  },

  // Nền xanh nhạt, cùng dáng dòng ghi chú ở màn hình chấm công: không nhầm với một ô chấm nữa.
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

  nutXong: {
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 2,
  },
  chuNutXong: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.xam },
});
