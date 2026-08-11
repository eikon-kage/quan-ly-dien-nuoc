import { Feather } from '@expo/vector-icons';
import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';

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
 */
export function HopChon({ tieuDe, luaChon, onChon, onDong }: Props) {
  return (
    <Modal visible transparent animationType="fade" onRequestClose={onDong}>
      <Pressable style={kieu.nenMo} onPress={onDong}>
        {/* Chặn chạm xuyên qua hộp ra nền mờ phía sau. */}
        <Pressable style={kieu.hop} onPress={() => {}}>
          <View style={kieu.tay} />
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
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const kieu = StyleSheet.create({
  nenMo: { flex: 1, justifyContent: 'flex-end', backgroundColor: 'rgba(35,42,53,0.35)' },
  hop: {
    backgroundColor: Mau.trang,
    borderTopLeftRadius: 18,
    borderTopRightRadius: 18,
    padding: 14,
    paddingBottom: 28,
    gap: 8,
  },
  tay: {
    width: 36,
    height: 4,
    borderRadius: 2,
    backgroundColor: Mau.vien,
    alignSelf: 'center',
    marginBottom: 6,
  },
  tieuDe: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    textAlign: 'center',
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
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.trang,
  },
  chuNut: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
  nutThoi: { backgroundColor: Mau.nen, marginTop: 4 },
});
