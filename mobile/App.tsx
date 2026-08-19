import {
  Lexend_300Light,
  Lexend_400Regular,
  Lexend_500Medium,
  Lexend_600SemiBold,
  useFonts,
} from '@expo-google-fonts/lexend';
import { Feather } from '@expo/vector-icons';
import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StatusBar, StyleSheet, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';

import { ManHinhBangLuong } from './src/giaodien/ManHinhBangLuong';
import { ManHinhChamCong } from './src/giaodien/ManHinhChamCong';
import { ManHinhLichSuKy } from './src/giaodien/ManHinhLichSuKy';
import { ManHinhTho } from './src/giaodien/ManHinhTho';
import { ManHinhThoTuCham } from './src/giaodien/ManHinhThoTuCham';
import { dungDoiChieu } from './src/giaodien/dungDoiChieu';
import { dungSaoLuu } from './src/giaodien/dungSaoLuu';
import { dungSupabase } from './src/giaodien/dungSupabase';
import { Bong, Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './src/giaodien/thietKe';
import { DuLieuChamCong } from './src/nghiepvu/kieu';
import * as LuuTru from './src/nghiepvu/luuTru';
import { CaiDatVai } from './src/nghiepvu/vaiMay';
import * as VaiMay from './src/nghiepvu/vaiMay';

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
  /** null = chưa đọc xong. Đọc rồi mới biết vẽ màn hình của chủ hay của thợ. */
  const [caiDat, datCaiDat] = useState<CaiDatVai | null>(null);
  const [muc, datMuc] = useState<Muc>('cham');

  const [fontDaNap] = useFonts({
    Lexend_300Light,
    Lexend_400Regular,
    Lexend_500Medium,
    Lexend_600SemiBold,
  });

  /**
   * Sao lưu Drive đặt ở đây chứ không trong màn hình Thợ: nó phải theo dõi *mọi* thay đổi
   * dữ liệu, mà dữ liệu thì nằm ở đây. Để trong màn hình Thợ thì lúc người dùng đang ở
   * màn hình Chấm công — tức là lúc dữ liệu đổi nhiều nhất — nó không chạy.
   */
  const saoLuu = dungSaoLuu(duLieu, caiDat?.vai !== 'tho');

  /**
   * Hộp thư đối chiếu cũng đặt ở đây và vì đúng một lý do như sao lưu: nó gửi *dữ liệu*
   * đi, mà dữ liệu nằm ở đây. Cả hai vai đều dùng, nên đừng đẩy xuống một màn hình.
   */
  const doiChieu = dungDoiChieu(duLieu, caiDat ?? VaiMay.MAC_DINH);

  /** Kết nối vào nhóm trên Supabase — cả hai vai đều dùng, nên cũng giữ ở đây. */
  const nhom = dungSupabase();

  useEffect(() => {
    LuuTru.doc().then(datDuLieu);
    VaiMay.doc().then(datCaiDat);
  }, []);

  /** Đổi gì là ghi xuống máy ngay, không có nút Lưu — người dùng không phải nhớ bấm. */
  const capNhat = useCallback((moi: DuLieuChamCong) => {
    datDuLieu(moi);
    LuuTru.ghi(moi).catch(() => {
      // Ghi hụt thì lần đổi sau ghi lại cả khối, không mất gì.
    });
  }, []);

  const doiVai = useCallback((moi: CaiDatVai) => {
    datCaiDat(moi);
    VaiMay.ghi(moi).catch(() => {
      // Ghi hụt thì mở app lần sau lại là máy chủ. Người dùng dán mã mời lại, mất một phút.
    });
  }, []);

  return (
    <SafeAreaProvider>
      <StatusBar barStyle="dark-content" />
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        {/* Chờ cả dữ liệu lẫn font. Hiện trước rồi font nhảy vào sau thì chữ giật một cái. */}
        {duLieu === null || caiDat === null || !fontDaNap ? (
          <View style={kieu.dangMo}>
            <ActivityIndicator size="large" color={Mau.chinh} />
          </View>
        ) : caiDat.vai === 'tho' ? (
          /*
            Máy thợ là một màn hình riêng, không có thanh tab: cả máy chỉ có một việc.
            Xem ghi chú đầu ManHinhThoTuCham.
          */
          <ManHinhThoTuCham
            duLieu={duLieu}
            capNhat={capNhat}
            caiDat={caiDat}
            datCaiDat={doiVai}
            dieuKhien={doiChieu}
            nhom={nhom}
          />
        ) : (
          <>
            <View style={kieu.thanTrang}>
              {muc === 'cham' && <ManHinhChamCong duLieu={duLieu} capNhat={capNhat} />}
              {muc === 'luong' && <ManHinhBangLuong duLieu={duLieu} capNhat={capNhat} />}
              {muc === 'ky' && <ManHinhLichSuKy duLieu={duLieu} capNhat={capNhat} />}
              {muc === 'tho' && (
                <ManHinhTho
                  duLieu={duLieu}
                  capNhat={capNhat}
                  saoLuu={saoLuu}
                  caiDat={caiDat}
                  datCaiDat={doiVai}
                  dieuKhien={doiChieu}
                  nhom={nhom}
                />
              )}
            </View>

            {/*
              Thanh tab tự vẽ chứ không dùng thanh mặc định: tự vẽ mới ép được cỡ chữ và
              để icon đi kèm chữ. Icon không bao giờ đứng một mình — người dùng không đoán hình.

              Bản thiết kế để thanh này **chỉ có icon**, không chữ, và nhét thêm một nút
              tròn nổi ở giữa. Không lấy theo: người dùng app này có tuổi, nhìn hình trơ
              trọi không đoán ra mục nào là mục nào (điều 8 trong docs/chamcong-giao-dien.md).
              Lấy phần hình dáng — nền trắng bo hai góc trên, nổi lên bằng bóng chứ không
              bằng đường kẻ, mục đang chọn có vạch xanh ngắn trên đầu icon.
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
                    <View style={[kieu.vachTab, dangChon && kieu.vachTabChon]} />
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
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    ...Bong.noi,
  },
  nutTab: {
    flex: 1,
    minHeight: 58,
    paddingTop: 4,
    paddingBottom: 10,
    paddingHorizontal: 4,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 3,
  },
  /** Vạch giữ chỗ sẵn kể cả khi không chọn, kẻo bấm sang mục khác thì cả hàng nhích lên. */
  vachTab: { width: 16, height: 3, borderRadius: 2, backgroundColor: 'transparent' },
  vachTabChon: { backgroundColor: Mau.chinh },
  chuTab: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua },
});
