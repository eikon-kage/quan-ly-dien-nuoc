/**
 * Màn hình Sao lưu: xem các bản đang có trong máy, khôi phục, và gửi một bản ra ngoài.
 *
 * Mở ra là tự tải danh sách — người dùng vào đây phần lớn là để xem "còn giữ tới hôm nào",
 * câu trả lời ấy phải có sẵn chứ không bắt bấm thêm một nút nữa.
 *
 * Câu quan trọng nhất trên màn hình này là câu nhắc gửi một bản ra ngoài. Bản trong máy nằm
 * trong phần riêng của app: xoá app hay mất máy là mất theo. Không nói ra thì người dùng
 * thấy "đã sao lưu lúc 16:12" rồi tưởng mình đã an toàn trước cả chuyện mất máy.
 */

import { Feather } from '@expo/vector-icons';
import { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { chiaSeSaoLuu } from '../nghiepvu/chiaSeSaoLuu';
import { chonFileSaoLuu } from '../nghiepvu/chonFileSaoLuu';
import { moGoi, tomTat } from '../nghiepvu/goiSaoLuu';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { BanSaoLuu, danhSachBan, docBan } from '../nghiepvu/saoLuuMay';
import { DieuKhienSaoLuu } from './dungSaoLuu';
import { NutChip, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu, Tuoi } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  saoLuu: DieuKhienSaoLuu;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

export function HopSaoLuu({ duLieu, saoLuu, capNhat, onDong }: Props) {
  const { trangThai, saoLuuNgay } = saoLuu;

  const [cacBan, datCacBan] = useState<BanSaoLuu[] | null>(null);
  const [dangTaiDanhSach, datDangTaiDanhSach] = useState(false);
  const [dangKhoiPhuc, datDangKhoiPhuc] = useState<string | null>(null);
  const [dangGui, datDangGui] = useState(false);

  const taiDanhSach = useCallback(async () => {
    datDangTaiDanhSach(true);
    try {
      datCacBan(await danhSachBan());
    } catch {
      // Không đọc được thư mục thì để danh sách trống kèm dòng nhắc bên dưới; không doạ
      // bằng hộp lỗi.
      datCacBan(null);
    } finally {
      datDangTaiDanhSach(false);
    }
  }, []);

  useEffect(() => {
    if (trangThai.hoTro) {
      taiDanhSach();
    }
  }, [trangThai.hoTro, trangThai.lucCuoi, taiDanhSach]);

  async function daySaoLuu() {
    await saoLuuNgay();
  }

  /**
   * Gửi bản mới nhất ra khỏi app. Đóng gói từ dữ liệu đang có chứ không gửi lại file cũ:
   * người bấm nút này muốn cầm đi bản mới nhất.
   */
  async function guiRaNgoai() {
    if (dangGui) {
      return;
    }

    datDangGui(true);
    try {
      await chiaSeSaoLuu(duLieu, Ngay.homNay());
    } catch (loi) {
      Alert.alert('Chưa gửi được bản sao lưu', loi instanceof Error ? loi.message : 'Thử lại sau nhé.', [
        { text: 'Đóng' },
      ]);
    } finally {
      datDangGui(false);
    }
  }

  /**
   * Khôi phục là thao tác *ghi đè*, không lùi lại được. Nên đọc bản ấy ra, đếm xem trong đó
   * có bao nhiêu thợ bao nhiêu buổi công, rồi mới hỏi — người dùng nhìn con số mới biết mình
   * sắp nhận đúng bản hay nhầm bản.
   */
  function hoiTruoc(nhan: string, duLieuMoi: DuLieuChamCong) {
    const dem = tomTat(duLieuMoi);

    Alert.alert(
      nhan,
      `Bản này có ${dem.soTho} thợ, ${dem.soBuoiCong} buổi công, ${dem.soUngTien} lần ứng tiền, ${dem.soKy} kỳ đã chốt.\n\nToàn bộ dữ liệu đang có trên máy sẽ bị thay bằng bản này.`,
      [
        { text: 'Thôi', style: 'cancel' },
        {
          text: 'Khôi phục',
          style: 'destructive',
          onPress: () => {
            capNhat(duLieuMoi);
            onDong();
          },
        },
      ],
    );
  }

  async function hoiKhoiPhuc(ban: BanSaoLuu) {
    datDangKhoiPhuc(ban.ten);
    try {
      hoiTruoc(`Khôi phục bản ${Ngay.ngayGon(ban.ngay)}?`, await docBan(ban.ten));
    } catch (loi) {
      Alert.alert('Chưa lấy được bản này', loi instanceof Error ? loi.message : 'Thử lại sau nhé.', [
        { text: 'Đóng' },
      ]);
    } finally {
      datDangKhoiPhuc(null);
    }
  }

  /** Khôi phục từ một file người dùng tự chọn — bản đã gửi vào Zalo, mail hay Files. */
  async function khoiPhucTuFile() {
    datDangKhoiPhuc('file');
    try {
      const noiDung = await chonFileSaoLuu();
      // Bấm huỷ trong bảng chọn file không phải là lỗi, đừng báo gì cả.
      if (noiDung === null) {
        return;
      }
      hoiTruoc('Khôi phục từ file này?', moGoi(noiDung).duLieu);
    } catch (loi) {
      Alert.alert('Chưa đọc được file', loi instanceof Error ? loi.message : 'Thử lại sau nhé.', [
        { text: 'Đóng' },
      ]);
    } finally {
      datDangKhoiPhuc(null);
    }
  }

  const chuTrangThai =
    trangThai.loi !== null
      ? trangThai.loi
      : trangThai.dangChay
        ? 'Đang ghi…'
        : trangThai.lucCuoi !== null
          ? `Sao lưu lần cuối lúc ${Ngay.gioPhut(trangThai.lucCuoi)}.`
          : 'Chưa sao lưu lần nào. Bấm nút dưới để ghi ngay.';

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <View style={kieu.dauTrang}>
          <Text style={kieu.chuTieuDe}>Sao lưu</Text>
          <Pressable style={kieu.nutDong} onPress={onDong} accessibilityRole="button">
            <Feather name="x" size={20} color={Mau.xam} />
          </Pressable>
        </View>

        {!trangThai.hoTro ? (
          <View style={kieu.trong}>
            <Feather name="hard-drive" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Máy này chưa sao lưu được</Text>
            <Text style={kieu.chuTrong}>
              Sao lưu cần bản app cài thẳng vào máy. Bản chạy thử trên web thì chưa dùng được.
            </Text>
          </View>
        ) : (
          <FlatList
            data={cacBan ?? []}
            keyExtractor={(ban) => ban.ten}
            contentContainerStyle={kieu.danhSach}
            ListHeaderComponent={
              <View style={kieu.dauDanhSach}>
                <View style={kieu.the}>
                  <View style={kieu.hangThe}>
                    <Feather
                      name={trangThai.loi !== null ? 'alert-circle' : 'save'}
                      size={18}
                      color={trangThai.loi !== null ? Mau.do : Mau.xanhLa}
                    />
                    <View style={kieu.giuaThe}>
                      <Text style={kieu.chuThe}>Bản trong máy</Text>
                      <Text style={[kieu.chuPhu, trangThai.loi !== null && kieu.chuLoi]}>
                        {chuTrangThai}
                      </Text>
                    </View>
                  </View>

                  <Pressable
                    style={[kieu.nutChinh, trangThai.dangChay && kieu.nutMo]}
                    onPress={daySaoLuu}
                    disabled={trangThai.dangChay}
                    accessibilityRole="button"
                  >
                    {trangThai.dangChay ? (
                      <ActivityIndicator color={Mau.trang} />
                    ) : (
                      <Feather name="save" size={16} color={Mau.trang} />
                    )}
                    <Text style={kieu.chuNutChinh}>
                      {trangThai.dangChay ? 'Đang ghi…' : 'Sao lưu ngay'}
                    </Text>
                  </Pressable>

                  {/*
                    Câu này không phải chữ nhỏ trang trí: nó là giới hạn thật của cách sao lưu
                    vào máy. Bỏ đi thì màn hình đang nói dối người dùng.
                  */}
                  <Text style={kieu.chuNhac}>
                    Bản sao nằm trong máy này, xoá app hay mất máy là mất theo. Thỉnh thoảng
                    gửi một bản ra ngoài — vào Zalo của mình, mail, hay thư mục Files.
                  </Text>
                </View>

                <View style={kieu.hangNut}>
                  <Pressable
                    style={[kieu.nutPhu, kieu.nutNua, dangGui && kieu.nutMo]}
                    onPress={guiRaNgoai}
                    disabled={dangGui}
                    accessibilityRole="button"
                  >
                    {dangGui ? (
                      <ActivityIndicator color={Mau.chinh} />
                    ) : (
                      <Feather name="share" size={16} color={Mau.chinh} />
                    )}
                    <Text style={kieu.chuNutPhu}>
                      {dangGui ? 'Đang tạo file…' : 'Gửi bản ra ngoài'}
                    </Text>
                  </Pressable>

                  <Pressable
                    style={[kieu.nutPhu, kieu.nutNua, dangKhoiPhuc === 'file' && kieu.nutMo]}
                    onPress={khoiPhucTuFile}
                    disabled={dangKhoiPhuc !== null}
                    accessibilityRole="button"
                  >
                    <Feather name="folder" size={16} color={Mau.chinh} />
                    <Text style={kieu.chuNutPhu}>Khôi phục từ file</Text>
                  </Pressable>
                </View>

                <View style={kieu.hangTieuDe}>
                  <Text style={kieu.chuNhomTieuDe}>Các bản trong máy</Text>
                  {dangTaiDanhSach && <ActivityIndicator size="small" color={Mau.xam} />}
                </View>
              </View>
            }
            ListEmptyComponent={
              !dangTaiDanhSach ? (
                <Text style={kieu.chuTrong}>
                  {cacBan === null
                    ? 'Chưa xem được danh sách các bản trong máy.'
                    : 'Trong máy chưa có bản nào.'}
                </Text>
              ) : null
            }
            renderItem={({ item: ban }) => (
              <View style={kieu.dongBan}>
                <View style={kieu.giuaThe}>
                  <Text style={kieu.chuNgay}>{Ngay.thuVaNgay(ban.ngay)}</Text>
                  {ban.suaLuc !== '' && (
                    <Text style={kieu.chuPhu}>Ghi lúc {Ngay.gioPhut(ban.suaLuc)}</Text>
                  )}
                </View>

                {dangKhoiPhuc === ban.ten ? (
                  <View style={kieu.dangTai}>
                    <ActivityIndicator size="small" color={Mau.chinh} />
                    <Text style={kieu.chuDangTai}>Khôi phục</Text>
                  </View>
                ) : (
                  <NutChip nhan="Khôi phục" icon="download" onPress={() => hoiKhoiPhuc(ban)} />
                )}
              </View>
            )}
          />
        )}
      </SafeAreaView>
    </Modal>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  dauTrang: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 10,
  },
  chuTieuDe: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  nutDong: {
    width: 40,
    height: 40,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },

  danhSach: { padding: 16, paddingTop: 4, paddingBottom: 24 },
  dauDanhSach: { gap: 14 },

  the: { ...theTrang, gap: 10 },
  hangThe: { flexDirection: 'row', alignItems: 'center', gap: 9 },
  giuaThe: { flex: 1, gap: 3 },
  chuThe: { fontSize: Co.chuThuong, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuLoi: { fontFamily: PhongChu.vua, color: Mau.do },
  chuNhac: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  nutChinh: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  nutMo: { opacity: 0.6 },
  chuNutChinh: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.trang,
    textAlign: 'center',
  },

  /* Hai nút thỉnh thoảng mới bấm: viền mảnh, nền nhạt, không tranh chỗ với nút Sao lưu ngay. */
  hangNut: { flexDirection: 'row', gap: 10 },
  nutNua: { flex: 1 },
  nutPhu: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Tuoi.chinh,
    backgroundColor: Mau.chinhNhat,
  },
  chuNutPhu: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.chinh,
    textAlign: 'center',
  },

  hangTieuDe: { flexDirection: 'row', alignItems: 'center', gap: 8, paddingTop: 4 },
  chuNhomTieuDe: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xam },

  dongBan: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    marginTop: 12,
  },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  // Đang đọc thì thay nút bằng vòng xoay kèm đúng chữ ấy, khỏi bấm thêm lần nữa.
  dangTai: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 7,
    minHeight: Co.caoNutNho,
    paddingHorizontal: 12,
  },
  chuDangTai: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xam },

  trong: { padding: 24, paddingTop: 56, gap: 10, alignItems: 'center' },
  chuTrongTo: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuTrong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
});
