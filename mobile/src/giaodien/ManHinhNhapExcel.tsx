import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import {
  ActivityIndicator,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { chiaSeFileMau } from '../nghiepvu/chiaSeExcel';
import { chonFileExcel } from '../nghiepvu/chonFileExcel';
import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import {
  BanNhap,
  KetQuaGhi,
  apDungNhap,
  docFileNhap,
  khoangThang,
  tomTat,
  tomTatDoc,
} from '../nghiepvu/nhapExcel';
import { tatCaTho } from '../nghiepvu/thaoTac';
import { DauTrang, HangO, NutChip, TheSo, theTrang } from './ThanhPhan';
import { Co, Mau, PhongChu, Tuoi } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

/** Nhiều lỗi quá thì chỉ kể ra bằng này dòng, kể hết thì đọc không nổi. */
const SO_LOI_KE_RA = 5;

/**
 * Nhập công của một thợ từ file Excel.
 *
 * Ba bước, xếp dọc trên cùng một màn hình chứ không phải ba trang nối tiếp: chọn thợ →
 * lấy file → xem trước rồi ghi. Xếp dọc thì lúc nào cũng nhìn thấy mình đang nhập cho
 * ai, và quay lại sửa bước trước không phải bấm lui từng trang.
 *
 * **Luôn xem trước rồi mới ghi.** Đây là chỗ duy nhất trong app đổi hàng chục buổi công
 * chỉ bằng một cú bấm, mà nhập nhầm file của thợ khác thì cả tháng công rơi vào tay người
 * khác. Con số tổng ở bảng xem trước là cái chặn duy nhất trước khi việc đó xảy ra.
 */
export function ManHinhNhapExcel({ duLieu, capNhat, onDong }: Props) {
  const thos = tatCaTho(duLieu);
  const homNay = Ngay.homNay();

  /** Chỉ có một thợ thì khỏi bắt chọn — chọn sẵn luôn. */
  const [tho, datTho] = useState<Tho | null>(thos.length === 1 ? thos[0] : null);
  const [dangDoi, datDangDoi] = useState(false);

  const [tenFile, datTenFile] = useState<string | null>(null);
  const [banNhap, datBanNhap] = useState<BanNhap | null>(null);
  const [daGhi, datDaGhi] = useState<KetQuaGhi | null>(null);

  const [dangLam, datDangLam] = useState<'mau' | 'chon' | null>(null);
  const [loi, datLoi] = useState<string | null>(null);

  const dangChonTho = tho === null || dangDoi;

  function chonTho(chon: Tho) {
    datTho(chon);
    datDangDoi(false);
    // Đổi thợ giữa chừng thì bỏ luôn file đang xem: file ấy đọc cho thợ cũ, để lại
    // trên màn hình thì người dùng dễ tưởng nó đã đổi theo.
    datBanNhap(null);
    datTenFile(null);
    datDaGhi(null);
    datLoi(null);
  }

  async function taiFileMau() {
    if (tho === null || dangLam !== null) {
      return;
    }

    datLoi(null);
    datDangLam('mau');
    try {
      const { tuNgay, denNgay } = khoangThang(homNay);
      await chiaSeFileMau(tho.ten, tuNgay, denNgay);
    } catch {
      datLoi('Chưa gửi được file mẫu. Anh thử lại xem.');
    } finally {
      datDangLam(null);
    }
  }

  async function layFile() {
    if (dangLam !== null) {
      return;
    }

    datLoi(null);
    datDangLam('chon');
    try {
      const file = await chonFileExcel();
      if (file === null) {
        return;
      }

      const doc = docFileNhap(file.noiDung);
      datTenFile(file.ten);
      datDaGhi(null);

      if (doc.dongs.length === 0) {
        datBanNhap(null);
        datLoi(
          doc.lois.length > 0
            ? 'File có dòng nhưng không dòng nào đọc được. Anh xem lại cột Ngày và cột Sáng/Chiều.'
            : 'File này chưa điền công ngày nào cả.',
        );
        return;
      }

      datBanNhap(doc);
    } catch (hong) {
      datBanNhap(null);
      // Mấy lỗi đọc file đều đã có sẵn câu tiếng Việt cho người dùng đọc.
      datLoi(hong instanceof Error ? hong.message : 'Không mở được file này.');
    } finally {
      datDangLam(null);
    }
  }

  function ghiVaoSo() {
    if (tho === null || banNhap === null) {
      return;
    }

    const ket = apDungNhap(duLieu, tho.id, banNhap.dongs);
    capNhat(ket.duLieu);
    datDaGhi(ket);
  }

  const tomTatFile = banNhap === null ? null : tomTatDoc(banNhap.dongs);

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
        <DauTrang
          tieuDe="Nhập từ Excel"
          phu={tho === null ? 'Chọn thợ trước' : `Nhập công cho ${tho.ten}`}
          onLui={onDong}
        />

        <ScrollView contentContainerStyle={kieu.trong}>
          {thos.length === 0 ? (
            <View style={kieu.rong}>
              <Feather name="users" size={34} color={Mau.xam} />
              <Text style={kieu.chuRongTo}>Chưa có thợ nào</Text>
              <Text style={kieu.chuPhu}>Anh thêm thợ ở màn hình Thợ đã, rồi quay lại đây.</Text>
            </View>
          ) : (
            <>
              {/* ---- Bước 1: nhập cho ai ---- */}
              <Text style={kieu.nhanBuoc}>1. Nhập cho thợ nào</Text>

              {dangChonTho ? (
                <View style={kieu.danhSach}>
                  {thos.map((mot) => (
                    <Pressable
                      key={mot.id}
                      style={[kieu.dongTho, mot.id === tho?.id && kieu.dongThoChon]}
                      onPress={() => chonTho(mot)}
                      accessibilityRole="button"
                      accessibilityState={{ selected: mot.id === tho?.id }}
                    >
                      <Feather
                        name={mot.id === tho?.id ? 'check-circle' : 'circle'}
                        size={18}
                        color={mot.id === tho?.id ? Mau.chinh : Mau.xam}
                      />
                      <Text style={kieu.chuTenTho} numberOfLines={1}>
                        {mot.dangLam ? mot.ten : `${mot.ten} (đã nghỉ)`}
                      </Text>
                    </Pressable>
                  ))}
                </View>
              ) : (
                <View style={kieu.the}>
                  <View style={kieu.giua}>
                    <Text style={kieu.chuTenTho} numberOfLines={1}>
                      {tho.ten}
                    </Text>
                    <Text style={kieu.chuPhu}>Công đọc từ file sẽ ghi cho thợ này</Text>
                  </View>
                  <NutChip nhan="Đổi thợ" icon="repeat" onPress={() => datDangDoi(true)} />
                </View>
              )}

              {/* ---- Bước 2: lấy file ---- */}
              {tho !== null && !dangDoi && (
                <>
                  <Text style={kieu.nhanBuoc}>2. Lấy file</Text>

                  <Pressable
                    style={[kieu.nutVien, dangLam === 'mau' && kieu.nutMo]}
                    onPress={taiFileMau}
                    disabled={dangLam !== null}
                    accessibilityRole="button"
                  >
                    {dangLam === 'mau' ? (
                      <ActivityIndicator color={Mau.chinh} />
                    ) : (
                      <Feather name="download" size={16} color={Mau.chinh} />
                    )}
                    <Text style={kieu.chuNutVien}>
                      {dangLam === 'mau' ? 'Đang tạo file mẫu…' : 'Lấy file mẫu tháng này'}
                    </Text>
                  </Pressable>

                  <Text style={kieu.chuPhu}>
                    File mẫu điền sẵn ngày của cả tháng {thangGon(homNay)}, mở bằng Excel trên
                    máy tính rồi gõ số công vào hai cột Sáng và Chiều.
                  </Text>

                  <Pressable
                    style={[kieu.nutXanh, dangLam === 'chon' && kieu.nutMo]}
                    onPress={layFile}
                    disabled={dangLam !== null}
                    accessibilityRole="button"
                  >
                    {dangLam === 'chon' ? (
                      <ActivityIndicator color={Mau.trang} />
                    ) : (
                      <Feather name="upload" size={16} color={Mau.trang} />
                    )}
                    <Text style={kieu.chuNutXanh}>
                      {dangLam === 'chon' ? 'Đang đọc file…' : 'Chọn file Excel đã điền'}
                    </Text>
                  </Pressable>
                </>
              )}

              {loi !== null && (
                <View style={kieu.theLoi}>
                  <Feather name="alert-circle" size={16} color={Mau.do} />
                  <Text style={kieu.chuLoi}>{loi}</Text>
                </View>
              )}

              {/* ---- Bước 3: xem trước rồi ghi ---- */}
              {tho !== null && !dangDoi && banNhap !== null && tomTatFile !== null && (
                <>
                  <Text style={kieu.nhanBuoc}>3. Xem lại rồi ghi vào sổ</Text>

                  {tenFile !== null && (
                    <Text style={kieu.chuPhu} numberOfLines={2}>
                      {tenFile}
                    </Text>
                  )}

                  <HangO>
                    <TheSo
                      nhan="Ngày có công"
                      so={String(tomTatFile.soNgay)}
                      mau="chinh"
                    />
                    <TheSo
                      nhan="Tổng công"
                      so={Ngay.soCong(tomTatFile.tongCong)}
                      mau="xanhLa"
                    />
                  </HangO>
                  <HangO>
                    <TheSo
                      nhan="Từ ngày → đến ngày"
                      so={Ngay.khoangGon(tomTatFile.tuNgay, tomTatFile.denNgay)}
                      mau="ngoc"
                    />
                    <TheSo nhan="Ứng tiền" so={Ngay.tien(tomTatFile.tongUng)} mau="do" />
                  </HangO>

                  {tomTatFile.soNghi > 0 && (
                    <Text style={kieu.chuPhu}>
                      Có {tomTatFile.soNghi} buổi file ghi là nghỉ — buổi ấy trong máy sẽ bị bỏ
                      chấm.
                    </Text>
                  )}

                  {banNhap.lois.length > 0 && (
                    <View style={kieu.theLoi}>
                      <Feather name="alert-circle" size={16} color={Mau.do} />
                      <View style={kieu.giua}>
                        <Text style={kieu.chuLoi}>
                          {banNhap.lois.length} dòng phải bỏ qua:
                        </Text>
                        {banNhap.lois.slice(0, SO_LOI_KE_RA).map((mot) => (
                          <Text key={mot.soDong} style={kieu.chuLoiNho}>
                            Dòng {mot.soDong}: {mot.ly}
                          </Text>
                        ))}
                        {banNhap.lois.length > SO_LOI_KE_RA && (
                          <Text style={kieu.chuLoiNho}>
                            …và {banNhap.lois.length - SO_LOI_KE_RA} dòng nữa.
                          </Text>
                        )}
                      </View>
                    </View>
                  )}

                  {daGhi === null ? (
                    <Pressable
                      style={kieu.nutXanh}
                      onPress={ghiVaoSo}
                      accessibilityRole="button"
                    >
                      <Feather name="check" size={16} color={Mau.trang} />
                      <Text style={kieu.chuNutXanh}>Ghi vào sổ</Text>
                    </Pressable>
                  ) : (
                    <View style={kieu.theXong}>
                      <Feather name="check-circle" size={18} color={Mau.xanhLa} />
                      <View style={kieu.giua}>
                        <Text style={kieu.chuXong}>{tomTat(daGhi)}</Text>
                        {daGhi.boQuaDaChot > 0 && (
                          <Text style={kieu.chuPhu}>
                            {daGhi.boQuaDaChot} buổi đã nằm trong kỳ đã chốt nên giữ nguyên.
                          </Text>
                        )}
                        {daGhi.boQuaUngTrung > 0 && (
                          <Text style={kieu.chuPhu}>
                            {daGhi.boQuaUngTrung} lần ứng đã có sẵn nên không cộng thêm.
                          </Text>
                        )}
                      </View>
                    </View>
                  )}
                </>
              )}
            </>
          )}
        </ScrollView>

        <View style={kieu.chanTrang}>
          <Pressable style={kieu.nutDong} onPress={onDong} accessibilityRole="button">
            <Text style={kieu.chuNutDong}>{daGhi === null ? 'Đóng' : 'Xong'}</Text>
          </Pressable>
        </View>
      </SafeAreaView>
    </Modal>
  );
}

/** "tháng 08/2026" — viết đủ chữ tháng cho khỏi nhầm với ngày. */
function thangGon(ngay: string): string {
  const { nam, thang } = Ngay.tach(ngay);
  return `${String(thang).padStart(2, '0')}/${nam}`;
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  trong: { padding: 16, paddingTop: 4, paddingBottom: 24, gap: 10 },

  nhanBuoc: {
    marginTop: 8,
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
  },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  danhSach: { gap: 8 },
  dongTho: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    minHeight: Co.caoNut,
    paddingVertical: 10,
  },
  dongThoChon: { borderWidth: 1, borderColor: Tuoi.chinh, backgroundColor: Mau.chinhNhat },
  chuTenTho: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  the: { ...theTrang, flexDirection: 'row', alignItems: 'center', gap: 10 },
  giua: { flex: 1, gap: 3 },

  nutVien: {
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
  chuNutVien: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.chinh,
    textAlign: 'center',
  },
  nutXanh: {
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
  chuNutXanh: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.trang,
    textAlign: 'center',
  },
  nutMo: { opacity: 0.6 },

  theLoi: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 9,
    padding: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.doNhat,
  },
  chuLoi: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.do },
  chuLoiNho: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.do },

  theXong: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 9,
    padding: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.xanhLaNhat,
  },
  chuXong: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.xanhLa },

  rong: { padding: 24, paddingTop: 48, gap: 10, alignItems: 'center' },
  chuRongTo: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },

  chanTrang: { paddingHorizontal: 16, paddingBottom: 12, paddingTop: 6 },
  nutDong: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: Co.caoNut,
    paddingVertical: 10,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.trang,
  },
  chuNutDong: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
});
