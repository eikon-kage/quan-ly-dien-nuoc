/**
 * Mấy mảnh giao diện lặp lại ở nhiều màn hình, dựng theo bộ *HR Attendance App UI Kit*.
 *
 * Để chung một chỗ vì đây là **hình dáng chung của cả app**: sửa bo góc hay khoảng cách
 * ở đây là cả bốn màn hình đổi theo, không phải đi sửa từng file rồi lệch nhau.
 */

import { Feather } from '@expo/vector-icons';
import { ReactNode, useState } from 'react';
import {
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  TextInputProps,
  View,
  ViewStyle,
} from 'react-native';

import { Bong, Co, HeSoChuToiDaLuoi, Mau, PhongChu, Tuoi } from './thietKe';

/**
 * Đầu trang: tiêu đề nằm **bên trái**, dòng phụ ngay dưới, nút bấm dồn sang phải.
 *
 * Trước đây đầu trang là một dải trắng kẻ viền dưới, tiêu đề căn giữa. Bản thiết kế bỏ
 * hẳn dải ấy — chữ nằm thẳng trên nền trang, không khung không viền — nên màn hình đỡ
 * một đường kẻ ngang và nội dung bắt đầu cao hơn.
 */
export function DauTrang({
  tieuDe,
  phu,
  phai,
  onLui,
}: {
  tieuDe: string;
  phu?: string;
  /** Nút bấm bên phải, ví dụ *Thêm thợ* hay hai mũi tên đổi tuần. */
  phai?: ReactNode;
  /** Có thì hiện nút quay lại bên trái — dùng cho màn hình mở đè lên. */
  onLui?: () => void;
}) {
  return (
    <View style={kieu.dauTrang}>
      {onLui !== undefined && (
        <Pressable style={kieu.nutLui} onPress={onLui} accessibilityLabel="Đóng">
          <Feather name="chevron-left" size={22} color={Mau.chu} />
        </Pressable>
      )}

      <View style={kieu.giuaDauTrang}>
        <Text style={kieu.chuTieuDe} numberOfLines={1}>
          {tieuDe}
        </Text>
        {phu !== undefined && <Text style={kieu.chuPhuDauTrang}>{phu}</Text>}
      </View>

      {phai}
    </View>
  );
}

/**
 * Nút nhỏ nền màu nhạt, bo 8 — *Sửa*, *Ứng tiền*, *Khôi phục*. Icon luôn đi kèm chữ.
 */
export function NutChip({
  nhan,
  icon,
  mau = Mau.chinh,
  nen = Mau.chinhNhat,
  onPress,
}: {
  nhan: string;
  icon: keyof typeof Feather.glyphMap;
  mau?: string;
  nen?: string;
  onPress: () => void;
}) {
  return (
    <Pressable style={[kieu.nutChip, { backgroundColor: nen }]} onPress={onPress}>
      <Feather name={icon} size={12} color={mau} />
      <Text style={[kieu.chuNutChip, { color: mau }]}>{nhan}</Text>
    </Pressable>
  );
}

/** Nhãn tĩnh, không bấm được — *Mới nhất*, *Đã nghỉ*. */
export function Nhan({
  chu,
  mau = Mau.chinh,
  nen = Mau.chinhNhat,
}: {
  chu: string;
  mau?: string;
  nen?: string;
}) {
  return (
    <View style={[kieu.nhan, { backgroundColor: nen }]}>
      <Text style={[kieu.chuNhan, { color: mau }]}>{chu}</Text>
    </View>
  );
}

/** Bốn màu của ô tóm tắt. Tên là *màu*, không phải *nghĩa* — xem ghi chú ở `Mau`. */
export type MauO = 'chinh' | 'xanhLa' | 'ngoc' | 'do';

const NEN_O: Record<MauO, string> = {
  chinh: Mau.chinhNhat,
  xanhLa: Mau.xanhLaNhat,
  ngoc: Mau.ngocNhat,
  do: Mau.doNhat,
};

/**
 * Ô tóm tắt: nền màu rất nhạt, viền màu tươi, nhãn ở trên, con số to ở dưới. Đây là mảnh
 * dễ nhận nhất của bản thiết kế — bốn ô xếp thành lưới 2×2 là nhìn hết mấy con số quan
 * trọng mà không phải đọc từng dòng.
 *
 * Chữ nhãn nét Light theo đúng bản thiết kế; con số thì màu đậm chứ không tươi như viền,
 * vì con số là **chữ** và phải đọc được ngoài nắng.
 */
export function TheSo({
  nhan,
  so,
  mau,
  mauSo,
}: {
  nhan: string;
  so: string;
  mau: MauO;
  /** Đè màu chữ số, dùng khi con số đổi màu theo dấu âm dương. */
  mauSo?: string;
}) {
  return (
    <View style={[kieu.theSo, { backgroundColor: NEN_O[mau], borderColor: Tuoi[mau] }]}>
      <Text style={kieu.chuNhanO} numberOfLines={2}>
        {nhan}
      </Text>
      <Text style={[kieu.chuSoO, { color: mauSo ?? Mau[mau] }]} numberOfLines={1}>
        {so}
      </Text>
    </View>
  );
}

/** Một hàng ô tóm tắt. Dùng hai hàng chồng lên nhau thành lưới 2×2 như bản thiết kế. */
export function HangO({ children }: { children: ReactNode }) {
  return <View style={kieu.hangO}>{children}</View>;
}

/**
 * Thanh phân đoạn: rãnh xám, mục đang chọn là một viên xanh đặc chữ trắng.
 *
 * Thay cho bốn nút viền rời nhau ở bản cũ. Viên trượt cho thấy **đúng một mục đang chọn**
 * rõ hơn nhiều so với bốn nút mà một cái đổi màu.
 */
export function ThanhDoan({
  cac,
  dangChon,
  onChon,
}: {
  cac: { ma: string; nhan: string }[];
  dangChon: string;
  onChon: (ma: string) => void;
}) {
  return (
    <View style={kieu.ranh}>
      {cac.map((muc) => {
        const chon = muc.ma === dangChon;
        return (
          <Pressable
            key={muc.ma}
            style={[kieu.doan, chon && kieu.doanChon]}
            onPress={() => onChon(muc.ma)}
            accessibilityState={{ selected: chon }}
          >
            <Text
              style={[kieu.chuDoan, chon && kieu.chuDoanChon]}
              numberOfLines={1}
              maxFontSizeMultiplier={HeSoChuToiDaLuoi}
            >
              {muc.nhan}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

/**
 * Ô nhập theo đúng mẫu `Input` của bản thiết kế: **nhãn nằm bên trong ô**, ở trên, cỡ nhỏ;
 * chữ người dùng gõ nằm ngay dưới, cỡ lớn hơn. Cả hai **căn trái**.
 *
 * Bản cũ để nhãn nằm ngoài ô, còn ô nhập số thì căn giữa — mà căn giữa thì con nháy đứng
 * giữa ô lúc còn trống, người gõ không biết chữ sẽ chạy ra đâu. Căn trái là chỗ con nháy
 * lúc nào cũng đứng, giống mọi ô nhập khác trên máy.
 *
 * Căn trái cũng gỡ được luôn một mớ code: bản cũ phải tự vẽ chữ gợi ý bằng một lớp phủ,
 * vì Android có lỗi đẩy con nháy ra sát mép phải khi ô căn giữa mà còn trống và có
 * `placeholder`. Ô căn trái không gặp lỗi ấy nên dùng `placeholder` thật.
 *
 * **Ô đang gõ thì đổi viền sang xanh**, chứ không để trình duyệt tự vẽ dấu focus của nó.
 * Trên bản web, Chrome vẽ quanh `<input>` một vòng xanh dày, bo góc không theo bo góc của ô
 * — nhìn như một nét lệch chồng lên thẻ, mà máy iOS/Android lại không có vòng ấy nên ba bản
 * khác hẳn nhau. Vòng của trình duyệt bỏ ở [index.html](../../public/index.html); dấu đang
 * gõ thì vẽ ở đây, bằng cách đổi màu đúng nét viền 1px vốn có nên ô không xê dịch.
 */
export function ONhap({
  nhan,
  coSo = false,
  ...props
}: { nhan: string; /** Chữ to hơn, dùng cho ô nhập số tiền và số công. */ coSo?: boolean } & TextInputProps) {
  const [dangGo, datDangGo] = useState(false);

  return (
    <View style={[kieu.oNhap, dangGo && kieu.oNhapDangGo]}>
      <Text style={kieu.nhanTrongO}>{nhan}</Text>
      <TextInput
        style={[kieu.chuTrongO, coSo && kieu.chuSoTrongO]}
        placeholderTextColor={Mau.xam}
        {...props}
        // Sau `...props` để người gọi truyền thêm `onFocus`/`onBlur` thì vẫn gọi được cả hai.
        onFocus={(bien) => {
          datDangGo(true);
          props.onFocus?.(bien);
        }}
        onBlur={(bien) => {
          datDangGo(false);
          props.onBlur?.(bien);
        }}
      />
    </View>
  );
}

/** Thẻ trắng bo 16, tách khỏi nền bằng vệt bóng chứ không bằng viền. */
export const theTrang: ViewStyle = {
  backgroundColor: Mau.trang,
  borderRadius: Co.boThe,
  padding: 14,
  ...Bong.the,
};

const kieu = StyleSheet.create({
  dauTrang: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 10,
  },
  nutLui: {
    width: 40,
    height: 40,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },
  giuaDauTrang: { flex: 1, gap: 2 },
  chuTieuDe: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhuDauTrang: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  nutChip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: Co.boNho,
  },
  chuNutChip: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua },

  nhan: { paddingHorizontal: 10, paddingVertical: 5, borderRadius: Co.boNho },
  chuNhan: { fontSize: Co.chuNho, fontFamily: PhongChu.vua },

  hangO: { flexDirection: 'row', gap: 11 },
  theSo: {
    flex: 1,
    minHeight: 100,
    justifyContent: 'space-between',
    gap: 10,
    padding: 14,
    borderRadius: Co.boThe,
    borderWidth: 1,
  },
  chuNhanO: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuSoO: { fontSize: Co.chuSoTo, fontFamily: PhongChu.dam },

  oNhap: {
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.trang,
    paddingHorizontal: 16,
    paddingTop: 9,
    paddingBottom: 6,
  },
  oNhapDangGo: { borderColor: Mau.chinh },
  nhanTrongO: { fontSize: Co.chuNho, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTrongO: {
    // Không đặt height: chữ hệ thống to lên thì ô phải cao theo, kẻo cắt cụt.
    minHeight: 34,
    padding: 0,
    fontSize: Co.chuTieuDe,
    fontFamily: PhongChu.vua,
    color: Mau.chu,
    textAlign: 'left',
  },
  chuSoTrongO: { fontSize: 22, fontFamily: PhongChu.dam },

  ranh: {
    flexDirection: 'row',
    gap: 4,
    padding: 4,
    borderRadius: Co.bo,
    backgroundColor: Mau.nen,
  },
  doan: {
    flex: 1,
    minHeight: 42,
    paddingVertical: 8,
    paddingHorizontal: 4,
    borderRadius: Co.boNho,
    alignItems: 'center',
    justifyContent: 'center',
  },
  doanChon: { backgroundColor: Mau.chinh },
  chuDoan: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.chu,
    textAlign: 'center',
  },
  chuDoanChon: { fontFamily: PhongChu.vua, color: Mau.trang },
});
