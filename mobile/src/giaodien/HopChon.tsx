import { Feather } from '@expo/vector-icons';
import { Pressable, StyleSheet, Text } from 'react-native';

import { HopDay } from './HopDay';
import { Co, Mau, PhongChu } from './thietKe';

export interface LuaChon {
  ma: string;
  nhan: string;
  icon: keyof typeof Feather.glyphMap;
  /** Việc xoá / cho nghỉ thì để đỏ. */
  nguyHiem?: boolean;
}

interface Props {
  tieuDe: string;
  luaChon: LuaChon[];
  onChon: (ma: string) => void;
  onDong: () => void;
}

/**
 * Hộp chọn hiện lên từ đáy màn hình.
 *
 * Tự vẽ chứ không dùng ActionSheetIOS: thứ nhất là ActionSheetIOS không có trên Android,
 * thứ hai là tự vẽ mới đặt được cỡ chữ và icon giống phần còn lại của app.
 *
 * Nền mờ và tay nắm nằm trong [HopDay](./HopDay.tsx), dùng chung với hai hộp kia.
 */
export function HopChon({ tieuDe, luaChon, onChon, onDong }: Props) {
  return (
    <HopDay onDong={onDong}>
      <Text style={kieu.tieuDe}>{tieuDe}</Text>

      {luaChon.map((chon) => {
        const mau = chon.nguyHiem === true ? Mau.do : Mau.chu;
        return (
          <Pressable key={chon.ma} style={kieu.nut} onPress={() => onChon(chon.ma)}>
            <Feather name={chon.icon} size={17} color={mau} />
            <Text style={[kieu.chuNut, { color: mau }]}>{chon.nhan}</Text>
          </Pressable>
        );
      })}

      <Pressable style={[kieu.nut, kieu.nutThoi]} onPress={onDong}>
        <Text style={[kieu.chuNut, { color: Mau.xam }]}>Thôi</Text>
      </Pressable>
    </HopDay>
  );
}

const kieu = StyleSheet.create({
  tieuDe: {
    fontSize: Co.chuTieuDe,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    paddingBottom: 6,
  },
  nut: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 9,
    minHeight: Co.caoNut,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.nen,
  },
  chuNut: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
  nutThoi: { backgroundColor: Mau.trang, borderWidth: 1, borderColor: Mau.vien, marginTop: 4 },
});
