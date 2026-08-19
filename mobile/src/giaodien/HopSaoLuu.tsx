/**
 * Màn hình Sao lưu Google Drive: nối tài khoản, xem các bản đã có, khôi phục.
 *
 * Mở ra là tự tải danh sách bản sao lưu — người dùng vào đây phần lớn là để xem "Drive
 * còn giữ tới hôm nào", câu trả lời ấy phải có sẵn chứ không bắt bấm thêm một nút nữa.
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

import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { BanSaoLuu, danhSachBan, docBan } from '../nghiepvu/saoLuuDrive';
import { DieuKhienSaoLuu } from './dungSaoLuu';
import { NutChip, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu } from './thietKe';

interface Props {
  saoLuu: DieuKhienSaoLuu;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

export function HopSaoLuu({ saoLuu, capNhat, onDong }: Props) {
  const { trangThai, noiDrive, ngatDrive, saoLuuNgay } = saoLuu;
  const daNoi = trangThai.taiKhoan !== null;

  const [cacBan, datCacBan] = useState<BanSaoLuu[] | null>(null);
  const [dangTaiDanhSach, datDangTaiDanhSach] = useState(false);
  const [dangKhoiPhuc, datDangKhoiPhuc] = useState<string | null>(null);

  const taiDanhSach = useCallback(async () => {
    datDangTaiDanhSach(true);
    try {
      datCacBan(await danhSachBan());
    } catch {
      // Không tải được thì để danh sách trống kèm dòng nhắc bên dưới; không doạ hộp lỗi.
      datCacBan(null);
    } finally {
      datDangTaiDanhSach(false);
    }
  }, []);

  useEffect(() => {
    if (daNoi) {
      taiDanhSach();
    }
  }, [daNoi, trangThai.lucCuoi, taiDanhSach]);

  async function noi() {
    await noiDrive();
  }

  function hoiNgat() {
    Alert.alert(
      'Ngắt nối Google Drive?',
      'Dữ liệu trên máy vẫn còn nguyên, các bản đã sao lưu trên Drive cũng vậy. Chỉ là từ giờ app không tự đẩy lên nữa.',
      [
        { text: 'Thôi', style: 'cancel' },
        { text: 'Ngắt nối', style: 'destructive', onPress: () => void ngatDrive() },
      ],
    );
  }

  async function daySaoLuu() {
    await saoLuuNgay();
  }

  /**
   * Khôi phục là thao tác *ghi đè*, không lùi lại được. Nên tải bản ấy về, đếm xem trong
   * đó có bao nhiêu thợ bao nhiêu buổi công, rồi mới hỏi — người dùng nhìn con số mới
   * biết mình sắp nhận đúng bản hay nhầm bản.
   */
  async function hoiKhoiPhuc(ban: BanSaoLuu) {
    datDangKhoiPhuc(ban.id);
    try {
      const { duLieu, tomTat } = await docBan(ban.id);

      Alert.alert(
        `Khôi phục bản ${Ngay.ngayGon(ban.ngay)}?`,
        `Bản này có ${tomTat.soTho} thợ, ${tomTat.soBuoiCong} buổi công, ${tomTat.soUngTien} lần ứng tiền, ${tomTat.soKy} kỳ đã chốt.\n\nToàn bộ dữ liệu đang có trên máy sẽ bị thay bằng bản này.`,
        [
          { text: 'Thôi', style: 'cancel' },
          {
            text: 'Khôi phục',
            style: 'destructive',
            onPress: () => {
              capNhat(duLieu);
              onDong();
            },
          },
        ],
      );
    } catch (loi) {
      Alert.alert('Chưa lấy được bản này', loi instanceof Error ? loi.message : 'Thử lại sau nhé.', [
        { text: 'Đóng' },
      ]);
    } finally {
      datDangKhoiPhuc(null);
    }
  }

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <View style={kieu.dauTrang}>
          <Text style={kieu.chuTieuDe}>Sao lưu Google Drive</Text>
          <Pressable style={kieu.nutDong} onPress={onDong} accessibilityRole="button">
            <Feather name="x" size={20} color={Mau.xam} />
          </Pressable>
        </View>

        {!trangThai.hoTro ? (
          <View style={kieu.trong}>
            <Feather name="cloud-off" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Máy này chưa nối Drive được</Text>
            <Text style={kieu.chuTrong}>
              Sao lưu Drive cần bản app cài thẳng vào máy. Bản chạy thử trong Expo Go thì
              chưa dùng được.
            </Text>
          </View>
        ) : (
          <FlatList
            data={cacBan ?? []}
            keyExtractor={(ban) => ban.id}
            contentContainerStyle={kieu.danhSach}
            ListHeaderComponent={
              <View style={kieu.dauDanhSach}>
                {/* Thẻ tài khoản: nối rồi thì hiện email, chưa nối thì hiện nút to. */}
                <View style={kieu.the}>
                  <View style={kieu.hangThe}>
                    <Feather
                      name={daNoi ? 'cloud' : 'cloud-off'}
                      size={18}
                      color={daNoi ? Mau.xanhLa : Mau.xam}
                    />
                    <View style={kieu.giuaThe}>
                      <Text style={kieu.chuThe}>{daNoi ? 'Đã nối Drive' : 'Chưa nối Drive'}</Text>
                      {daNoi && trangThai.taiKhoan?.email !== '' && (
                        <Text style={kieu.chuPhu} numberOfLines={1}>
                          {trangThai.taiKhoan?.email}
                        </Text>
                      )}
                    </View>
                  </View>

                  {daNoi ? (
                    <>
                      <Text style={kieu.chuPhu}>
                        {trangThai.lucCuoi
                          ? `Sao lưu lần cuối lúc ${Ngay.gioPhut(trangThai.lucCuoi)}.`
                          : 'Chưa sao lưu lần nào. Bấm nút dưới để đẩy lên ngay.'}
                      </Text>

                      <Pressable
                        style={[kieu.nutChinh, trangThai.dangChay && kieu.nutMo]}
                        onPress={daySaoLuu}
                        disabled={trangThai.dangChay}
                        accessibilityRole="button"
                      >
                        {trangThai.dangChay ? (
                          <ActivityIndicator color={Mau.trang} />
                        ) : (
                          <Feather name="upload-cloud" size={16} color={Mau.trang} />
                        )}
                        <Text style={kieu.chuNutChinh}>
                          {trangThai.dangChay ? 'Đang đẩy lên…' : 'Sao lưu ngay'}
                        </Text>
                      </Pressable>

                      <Pressable style={kieu.nutPhu} onPress={hoiNgat} accessibilityRole="button">
                        <Text style={kieu.chuNutNgat}>Ngắt nối</Text>
                      </Pressable>
                    </>
                  ) : (
                    <>
                      <Text style={kieu.chuPhu}>
                        Nối một lần rồi thôi. Từ đó cứ chấm công, sửa lương là ít phút sau
                        app tự đẩy bản mới lên Drive.
                      </Text>

                      <Pressable style={kieu.nutChinh} onPress={noi} accessibilityRole="button">
                        <Feather name="log-in" size={16} color={Mau.trang} />
                        <Text style={kieu.chuNutChinh}>Nối với Google Drive</Text>
                      </Pressable>
                    </>
                  )}

                  {trangThai.loi !== null && <Text style={kieu.chuLoi}>{trangThai.loi}</Text>}
                </View>

                {daNoi && (
                  <View style={kieu.hangTieuDe}>
                    <Text style={kieu.chuNhomTieuDe}>Các bản trên Drive</Text>
                    {dangTaiDanhSach && <ActivityIndicator size="small" color={Mau.xam} />}
                  </View>
                )}
              </View>
            }
            ListEmptyComponent={
              daNoi && !dangTaiDanhSach ? (
                <Text style={kieu.chuTrong}>
                  {cacBan === null
                    ? 'Chưa xem được danh sách. Kiểm tra mạng rồi mở lại.'
                    : 'Trên Drive chưa có bản nào.'}
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

                {dangKhoiPhuc === ban.id ? (
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
  chuLoi: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.do },

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
  nutPhu: {
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    alignItems: 'center',
    justifyContent: 'center',
  },
  chuNutNgat: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.do },

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
  // Đang tải thì thay nút bằng vòng xoay kèm đúng chữ ấy, khỏi bấm thêm lần nữa.
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
