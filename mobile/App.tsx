import {
  Inter_400Regular,
  Inter_500Medium,
  Inter_600SemiBold,
  useFonts,
} from '@expo-google-fonts/inter';
import { Feather } from '@expo/vector-icons';
import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StatusBar, StyleSheet, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';

import { ManHinhBangLuong } from './src/giaodien/ManHinhBangLuong';
import { ManHinhChamCong } from './src/giaodien/ManHinhChamCong';
import { ManHinhLichSuKy } from './src/giaodien/ManHinhLichSuKy';
import { ManHinhTho } from './src/giaodien/ManHinhTho';
import { dungSaoLuu } from './src/giaodien/dungSaoLuu';
import { Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './src/giaodien/thietKe';
import { DuLieuChamCong } from './src/nghiepvu/kieu';
import * as LuuTru from './src/nghiepvu/luuTru';

type Muc = 'cham' | 'luong' | 'ky' | 'tho';

/**
 * Bốn mục. Ba mục đầu là việc hằng ngày, mục *Kỳ đã chốt* là chỗ tra sổ cũ — thêm vào
 * khi có quyết toán, vì nhét sổ cũ vào Bảng lương thì màn hình dùng hằng ngày bị chen chỗ.
 * Đừng thêm mục thứ năm: mỗi mục là một chỗ để người dùng lạc.
 */
const CAC_MUC: { ma: Muc; nhan: string; icon: keyof typeof Feather.glyphMap }[] = [
  { ma: 'cham', nhan: 'Chấm công', icon: 'check-square' },
  { ma: 'luong', nhan: 'Bảng lương', icon: 'credit-card' },
  { ma: 'ky', nhan: 'Kỳ đã chốt', icon: 'archive' },
  { ma: 'tho', nhan: 'Thợ', icon: 'users' },
];

export default function App() {
  const [duLieu, datDuLieu] = useState<DuLieuChamCong | null>(null);
  const [muc, datMuc] = useState<Muc>('cham');

  const [fontDaNap] = useFonts({ Inter_400Regular, Inter_500Medium, Inter_600SemiBold });

  /**
   * Sao lưu Drive đặt ở đây chứ không trong màn hình Thợ: nó phải theo dõi *mọi* thay đổi
   * dữ liệu, mà dữ liệu thì nằm ở đây. Để trong màn hình Thợ thì lúc người dùng đang ở
   * màn hình Chấm công — tức là lúc dữ liệu đổi nhiều nhất — nó không chạy.
   */
  const saoLuu = dungSaoLuu(duLieu);

  useEffect(() => {
    LuuTru.doc().then(datDuLieu);
  }, []);

  /** Đổi gì là ghi xuống máy ngay, không có nút Lưu — người dùng không phải nhớ bấm. */
  const capNhat = useCallback((moi: DuLieuChamCong) => {
    datDuLieu(moi);
    LuuTru.ghi(moi).catch(() => {
      // Ghi hụt thì lần đổi sau ghi lại cả khối, không mất gì.
    });
  }, []);

  return (
    <SafeAreaProvider>
      <StatusBar barStyle="dark-content" />
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        {/* Chờ cả dữ liệu lẫn font. Hiện trước rồi font nhảy vào sau thì chữ giật một cái. */}
        {duLieu === null || !fontDaNap ? (
          <View style={kieu.dangMo}>
            <ActivityIndicator size="large" color={Mau.chinh} />
          </View>
        ) : (
          <>
            <View style={kieu.thanTrang}>
              {muc === 'cham' && <ManHinhChamCong duLieu={duLieu} capNhat={capNhat} />}
              {muc === 'luong' && <ManHinhBangLuong duLieu={duLieu} capNhat={capNhat} />}
              {muc === 'ky' && <ManHinhLichSuKy duLieu={duLieu} capNhat={capNhat} />}
              {muc === 'tho' && (
                <ManHinhTho duLieu={duLieu} capNhat={capNhat} saoLuu={saoLuu} />
              )}
            </View>

            {/*
              Thanh tab tự vẽ chứ không dùng thanh mặc định: tự vẽ mới ép được cỡ chữ và
              để icon đi kèm chữ. Icon không bao giờ đứng một mình — người dùng không đoán hình.
            */}
            <View style={kieu.thanhTab}>
              {CAC_MUC.map(({ ma, nhan, icon }) => {
                const dangChon = ma === muc;
                const mau = dangChon ? Mau.chinh : Mau.xam;
                return (
                  <Pressable
                    key={ma}
                    style={kieu.nutTab}
                    onPress={() => datMuc(ma)}
                    accessibilityRole="tab"
                    accessibilityState={{ selected: dangChon }}
                  >
                    <Feather name={icon} size={20} color={mau} />
                    <Text
                      style={[kieu.chuTab, { color: mau }]}
                      numberOfLines={1}
                      maxFontSizeMultiplier={HeSoChuToiDaLuoi}
                    >
                      {nhan}
                    </Text>
                  </Pressable>
                );
              })}
            </View>
          </>
        )}
      </SafeAreaView>
    </SafeAreaProvider>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  dangMo: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  thanTrang: { flex: 1 },

  thanhTab: {
    flexDirection: 'row',
    backgroundColor: Mau.trang,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
  },
  nutTab: {
    flex: 1,
    minHeight: 58,
    paddingVertical: 6,
    paddingHorizontal: 4,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 3,
  },
  chuTab: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua },
});
