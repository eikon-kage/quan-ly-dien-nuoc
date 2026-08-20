import {
  Lexend_300Light,
  Lexend_400Regular,
  Lexend_500Medium,
  Lexend_600SemiBold,
  useFonts,
} from '@expo-google-fonts/lexend';
import { Feather } from '@expo/vector-icons';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, StatusBar, StyleSheet, Text, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';

import { ManHinhBangLuong } from './src/giaodien/ManHinhBangLuong';
import { ManHinhChamCong } from './src/giaodien/ManHinhChamCong';
import { ManHinhLichSuKy } from './src/giaodien/ManHinhLichSuKy';
import { ManHinhMoDau } from './src/giaodien/ManHinhMoDau';
import { ManHinhTho } from './src/giaodien/ManHinhTho';
import { ManHinhThoTuCham } from './src/giaodien/ManHinhThoTuCham';
import { KetNoiHopThu, dungDoiChieu } from './src/giaodien/dungDoiChieu';
import { dungSaoLuu } from './src/giaodien/dungSaoLuu';
import { dungSupabase } from './src/giaodien/dungSupabase';
import { Bong, Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './src/giaodien/thietKe';
import { hopThuSupabase } from './src/nghiepvu/hopThuSupabase';
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
  /**
   * Đã bấm *Để sau* ở màn hình nối nhóm. Chỉ nhớ trong lượt mở app này, **không ghi xuống
   * máy**: sổ chưa nối thì vẫn chưa ai nhận được, nên lần mở sau hỏi lại là đúng. Ghi xuống
   * máy là một cú bấm nhầm khiến người dùng không bao giờ thấy màn hình ấy nữa.
   */
  const [deSau, datDeSau] = useState(false);

  const [fontDaNap] = useFonts({
    Lexend_300Light,
    Lexend_400Regular,
    Lexend_500Medium,
    Lexend_600SemiBold,
  });

  /**
   * Sao lưu đặt ở đây chứ không trong màn hình Thợ: nó phải theo dõi *mọi* thay đổi dữ liệu,
   * mà dữ liệu thì nằm ở đây. Để trong màn hình Thợ thì lúc người dùng đang ở màn hình Chấm
   * công — tức là lúc dữ liệu đổi nhiều nhất — nó không chạy.
   *
   * Chạy cho cả hai vai. Bản Drive trước đây phải tắt trên máy thợ vì hai máy ghi đè bản sao
   * lưu của nhau trên tài khoản dùng chung; sao lưu vào máy thì mỗi máy một thư mục riêng.
   */
  const saoLuu = dungSaoLuu(duLieu);

  /**
   * Hộp thư đối chiếu cũng đặt ở đây và vì đúng một lý do như sao lưu: nó gửi *dữ liệu*
   * đi, mà dữ liệu nằm ở đây. Cả hai vai đều dùng, nên đừng đẩy xuống một màn hình.
   */
  /**
   * Kết nối vào nhóm trên Supabase — cả hai vai đều dùng, nên cũng giữ ở đây.
   *
   * Truyền `null` khi chưa đọc xong vai máy, **không truyền tạm `'chu'`**: hook lấy vai để
   * biết có được tự lập nhóm lúc mở app hay không, mà lập nhóm cho một máy thợ là đặt nó vào
   * một nhóm một người rồi mã mời của chủ không đổi được nữa.
   */
  const nhom = dungSupabase(caiDat?.vai ?? null);

  /**
   * Hộp thư: chỉ còn một đường, Supabase. Trước đây còn đường Drive dùng chung một tài khoản
   * Google, đã bỏ — cách ấy không chặn được ai đọc của ai, máy nào cũng xoá được sổ của máy
   * khác.
   *
   * Vẫn dựng ở đây, một chỗ duy nhất, chứ không để màn hình tự gọi: đổi ruột hộp thư lần nữa
   * thì chỉ sửa đúng dòng này.
   */
  const hopThu = useMemo(() => hopThuSupabase(), []);

  /**
   * Hộp thư đang dùng đã nối được chưa, và nếu chưa thì chỉ đường cho người dùng.
   *
   * Chỗ này phải nằm ở đây vì đây là chỗ duy nhất biết đang chạy hộp thư nào. Trước đây hook
   * đối chiếu tự hỏi Google, nên lúc chuyển sang Supabase nó vẫn báo "cần nối Google" rồi ẩn
   * nút đồng bộ — sổ có nơi để gửi mà không có nút để bấm.
   */
  const ketNoi = useMemo<KetNoiHopThu>(() => {
    const { hoTro: coSupabase, taiKhoan: taiKhoanNhom, thanhVien } = nhom.trangThai;

    if (!coSupabase) {
      return {
        sanSang: false,
        chuaSanSang: 'Máy này chưa nối được hộp thư nào. Cần bản app cài thẳng vào máy.',
      };
    }
    if (thanhVien !== null) {
      return { sanSang: true, chuaSanSang: null };
    }
    /*
      Chỉ đường **theo vai**: máy chủ có mục Thợ để mở, còn máy thợ thì không có mục nào, cả
      thanh tab cũng không. Câu chung cho hai vai là câu sai với một vai — thợ đọc "mở mục
      Thợ" rồi ngồi tìm một mục không tồn tại.
    */
    const laTho = caiDat?.vai === 'tho';
    return {
      sanSang: false,
      chuaSanSang:
        taiKhoanNhom !== null
          ? laTho
            ? 'Đã đăng nhập nhưng chưa vào nhóm. Bấm dải Chưa vào nhóm ở đầu trang.'
            : 'Đã đăng nhập nhưng chưa vào nhóm. Mở mục Thợ → Nhóm chấm công.'
          : laTho
            ? 'Chưa nối nhóm — sổ chưa gửi cho chủ. Bấm dải ở đầu trang để dán mã mời.'
            : 'Chưa nối nhóm. Mở mục Thợ → Nhóm chấm công để nối.',
    };
  }, [nhom.trangThai, caiDat?.vai]);

  const doiChieu = dungDoiChieu(duLieu, caiDat ?? VaiMay.MAC_DINH, hopThu, ketNoi);

  /**
   * Mở app ra là hỏi nối nhóm luôn, chứ không đợi người dùng mò vào mục Thợ. Chỉ hỏi khi máy
   * chưa ở trong nhóm nào, và thêm bốn điều kiện nữa — mỗi điều kiện chặn một cách hỏi sai:
   *
   *   `hoTro`     — bản app không có địa chỉ Supabase thì hỏi cũng chẳng nối được gì.
   *   `!dangDoc`  — lượt nối lúc mở app còn đang chạy: chưa biết thì chưa hỏi, kẻo máy đã
   *                 nối rồi vẫn thấy màn hình đăng nhập nhoáng lên một nhịp.
   *   `!traHut`   — mất mạng nên *không biết* đã ở nhóm nào chưa. Máy chủ ngoài vùng phủ
   *                 sóng phải mở app ra chấm công được, không phải nhìn màn hình đăng nhập.
   *   `!deSau`    — người dùng đã nói để sau (chỉ tính cho lượt mở app này).
   *   `!dungMotMinh` — người dùng đã **chọn** dùng app một mình. Khác *để sau*: đó là một
   *                 câu trả lời, ghi vào máy rồi, hỏi lại mỗi lần mở app là phiền đúng người
   *                 đã trả lời xong.
   *
   * Nối được rồi thì `thanhVien` khác null và màn hình này tự biến, không cần ai đóng.
   */
  const hoiNoiNhom =
    nhom.trangThai.hoTro &&
    !nhom.trangThai.dangDoc &&
    !nhom.trangThai.traHut &&
    nhom.trangThai.thanhVien === null &&
    caiDat?.dungMotMinh !== true &&
    !deSau;

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
        ) : hoiNoiNhom ? (
          <ManHinhMoDau
            duLieu={duLieu}
            capNhat={capNhat}
            caiDat={caiDat}
            datCaiDat={doiVai}
            nhom={nhom}
            onDeSau={() => datDeSau(true)}
          />
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
