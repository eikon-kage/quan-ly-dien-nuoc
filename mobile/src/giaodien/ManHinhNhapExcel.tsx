import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { chiaSeFileMau } from '../nghiepvu/chiaSeExcel';
import { chonFileExcel } from '../nghiepvu/chonFileExcel';
import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import {
  BanNhap,
  KetQuaGhi,
  apDungNhap,
  boUngTien,
  docFileNhap,
  khoangNam,
  khoangThang,
  tomTat,
  tomTatDoc,
} from '../nghiepvu/nhapExcel';
import { tatCaTho } from '../nghiepvu/thaoTac';
import { ManHinhDe } from './ManHinhDe';
import { DauTrang, HangO, NutChip, TheSo, theTrang } from './ThanhPhan';
import { Co, Mau, PhongChu, Tuoi } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
  /**
   * Máy thợ: nhập công cho **chính mình**. Bỏ bước chọn thợ, và không nhận một đồng tiền
   * ứng nào — xem `boUngTien`.
   *
   * Dùng chung màn hình với máy chủ chứ không làm riêng một bản cho thợ, khác hẳn lối
   * `ManHinhThoTuCham` làm màn hình riêng. Ở đây hai bên làm **đúng một việc** — đọc file
   * rồi xem trước rồi ghi — mà đó lại là chỗ nguy nhất trong app: một cú bấm đổi hàng chục
   * buổi công. Hai bản chép tay của cùng cái chốt ấy là sớm muộn sửa một bên quên bên kia.
   */
  choTho?: { thoId: string; ten: string };
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
 * Trên **máy thợ** (`choTho`) chỉ còn hai bước: người nhập là chính chủ máy nên không có
 * bước chọn thợ, và không có một đồng tiền ứng nào đi qua đây.
 *
 * **Luôn xem trước rồi mới ghi.** Đây là chỗ duy nhất trong app đổi hàng chục buổi công
 * chỉ bằng một cú bấm, mà nhập nhầm file của thợ khác thì cả tháng công rơi vào tay người
 * khác. Con số tổng ở bảng xem trước là cái chặn duy nhất trước khi việc đó xảy ra.
 */
export function ManHinhNhapExcel({ duLieu, capNhat, onDong, choTho }: Props) {
  /** Máy thợ nhập cho chính mình nên không có danh sách để chọn. */
  const thos = choTho === undefined ? tatCaTho(duLieu) : [];
  const homNay = Ngay.homNay();

  /** Chỉ có một thợ thì khỏi bắt chọn — chọn sẵn luôn. */
  const [tho, datTho] = useState<Tho | null>(thos.length === 1 ? thos[0] : null);
  const [dangDoi, datDangDoi] = useState(false);

  const [tenFile, datTenFile] = useState<string | null>(null);
  const [banNhap, datBanNhap] = useState<BanNhap | null>(null);
  const [daGhi, datDaGhi] = useState<KetQuaGhi | null>(null);

  const [dangLam, datDangLam] = useState<'thang' | 'nam' | 'chon' | null>(null);
  const [loi, datLoi] = useState<string | null>(null);

  /** Ai đang được nhập công: thợ vừa chọn, hoặc chính chủ máy nếu đây là máy thợ. */
  const nhapCho = choTho ?? (tho === null ? null : { thoId: tho.id, ten: tho.ten });
  const dangChonTho = choTho === undefined && (tho === null || dangDoi);
  /** Máy thợ không có bước chọn thợ, nên mấy bước sau lùi số xuống một. */
  const buoc = (thuTu: number) => (choTho === undefined ? thuTu : thuTu - 1);

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

  /**
   * File mẫu cho trọn một tháng hay trọn cả năm.
   *
   * Có cả hai chứ không chỉ tháng này: người chuyển từ sổ giấy sang app giữa năm phải nhập
   * bù mấy tháng liền, mà lấy file mẫu tám lần rồi gõ vào tám file cùng tên khác tháng là
   * tự dựng ra chỗ để nhầm.
   */
  async function taiFileMau(khoang: 'thang' | 'nam') {
    if (nhapCho === null || dangLam !== null) {
      return;
    }

    datLoi(null);
    datDangLam(khoang);
    try {
      const { tuNgay, denNgay } =
        khoang === 'nam' ? khoangNam(homNay) : khoangThang(homNay);
      // Máy thợ: file mẫu không có cột Ứng tiền. Xem ghi chú ở `cotNhap`.
      await chiaSeFileMau(nhapCho.ten, tuNgay, denNgay, choTho === undefined);
    } catch {
      datLoi('Chưa gửi được file mẫu. Thử lại xem.');
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
            ? 'File có dòng nhưng không dòng nào đọc được. Xem lại cột Ngày và cột Sáng/Chiều.'
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

  /**
   * Những dòng thật sự sẽ ghi. Trên máy thợ tiền ứng bị bỏ ngay ở đây, trước cả bảng xem
   * trước: bảng xem trước phải nói đúng cái sắp xảy ra, hiện một con số tiền rồi không ghi
   * là nói dối người đang soát.
   */
  const dongsGhi =
    banNhap === null ? null : choTho === undefined ? banNhap.dongs : boUngTien(banNhap.dongs);
  const tomTatFile = dongsGhi === null ? null : tomTatDoc(dongsGhi);
  /** Máy thợ: file có cột tiền (file của chủ chẳng hạn) thì nói rõ là không nhận. */
  const soDongCoUng =
    banNhap === null || choTho === undefined
      ? 0
      : banNhap.dongs.filter((dong) => dong.ung !== null).length;

  function ghiVaoSo() {
    if (nhapCho === null || dongsGhi === null) {
      return;
    }

    const ket = apDungNhap(duLieu, nhapCho.thoId, dongsGhi);
    capNhat(ket.duLieu);
    datDaGhi(ket);
  }

  return (
    <ManHinhDe onDong={onDong}>
      <DauTrang
        tieuDe="Nhập từ Excel"
        phu={
          choTho !== undefined
            ? 'Nhập công của tôi'
            : nhapCho === null
              ? 'Chọn thợ trước'
              : `Nhập công cho ${nhapCho.ten}`
        }
        onLui={onDong}
      />

      <ScrollView contentContainerStyle={kieu.trong}>
        {choTho === undefined && thos.length === 0 ? (
          <View style={kieu.rong}>
            <Feather name="users" size={34} color={Mau.xam} />
            <Text style={kieu.chuRongTo}>Chưa có thợ nào</Text>
            <Text style={kieu.chuPhu}>Anh thêm thợ ở màn hình Thợ đã, rồi quay lại đây.</Text>
          </View>
        ) : (
          <>
            {/* ---- Bước 1: nhập cho ai. Máy thợ bỏ hẳn bước này: chỉ có một người. ---- */}
            {choTho === undefined && <Text style={kieu.nhanBuoc}>1. Nhập cho thợ nào</Text>}

            {/* Điều kiện viết thẳng ra `tho === null || dangDoi` chứ không dùng `dangChonTho`:
                hai cái luôn bằng nhau ở nhánh này, nhưng viết thẳng thì nhánh dưới mới
                chắc chắn có `tho`. */}
            {choTho !== undefined ? null : tho === null || dangDoi ? (
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
            {nhapCho !== null && !dangDoi && (
              <>
                <Text style={kieu.nhanBuoc}>{buoc(2)}. Lấy file</Text>

                {/*
                  Hai nút cạnh nhau, không phải một nút rồi một mục chọn khoảng: đây là
                  chỗ người dùng đứng lại hỏi "sao chỉ có tháng này", mà hỏi thì phải
                  thấy ngay câu trả lời chứ không phải mở thêm một hộp nữa.
                */}
                <View style={kieu.hangMau}>
                  <Pressable
                    style={[kieu.nutVien, dangLam === 'thang' && kieu.nutMo]}
                    onPress={() => taiFileMau('thang')}
                    disabled={dangLam !== null}
                    accessibilityRole="button"
                    accessibilityLabel="Lấy file mẫu tháng này"
                  >
                    {dangLam === 'thang' ? (
                      <ActivityIndicator color={Mau.chinh} />
                    ) : (
                      <Feather name="download" size={16} color={Mau.chinh} />
                    )}
                    <Text style={kieu.chuNutVien}>
                      {dangLam === 'thang' ? 'Đang tạo…' : `Mẫu tháng ${thangGon(homNay)}`}
                    </Text>
                  </Pressable>

                  <Pressable
                    style={[kieu.nutVien, dangLam === 'nam' && kieu.nutMo]}
                    onPress={() => taiFileMau('nam')}
                    disabled={dangLam !== null}
                    accessibilityRole="button"
                    accessibilityLabel="Lấy file mẫu cả năm"
                  >
                    {dangLam === 'nam' ? (
                      <ActivityIndicator color={Mau.chinh} />
                    ) : (
                      <Feather name="download" size={16} color={Mau.chinh} />
                    )}
                    <Text style={kieu.chuNutVien}>
                      {dangLam === 'nam' ? 'Đang tạo…' : `Mẫu cả năm ${Ngay.tach(homNay).nam}`}
                    </Text>
                  </Pressable>
                </View>

                <Text style={kieu.chuPhu}>
                  File mẫu điền sẵn ngày, mở bằng Excel trên máy tính rồi gõ số công vào hai
                  cột Sáng và Chiều. File cả năm có sẵn ngày của mười hai tháng — điền tháng
                  nào cũng được, tháng để trống thì trong máy vẫn nguyên.
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
            {nhapCho !== null && !dangDoi && banNhap !== null && tomTatFile !== null && (
              <>
                <Text style={kieu.nhanBuoc}>{buoc(3)}. Xem lại rồi ghi vào sổ</Text>

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
                  {/* Máy thợ không có ô tiền nào, kể cả một ô "0 đ": cả app này không biết tiền. */}
                  {choTho === undefined && (
                    <TheSo nhan="Ứng tiền" so={Ngay.tien(tomTatFile.tongUng)} mau="do" />
                  )}
                </HangO>

                {tomTatFile.soNghi > 0 && (
                  <Text style={kieu.chuPhu}>
                    Có {tomTatFile.soNghi} buổi file ghi là nghỉ — buổi ấy trong máy sẽ bị bỏ
                    chấm.
                  </Text>
                )}

                {/*
                  Nói ra chứ không lặng lẽ bỏ: thợ chọn đúng cái file chủ gửi thì trong đó
                  có cột tiền, mà bỏ im thì thợ tưởng đã khai ứng rồi.
                */}
                {soDongCoUng > 0 && (
                  <Text style={kieu.chuPhu}>
                    File có {soDongCoUng} dòng ghi tiền ứng. App này chỉ nhận số công — tiền
                    ứng thì nói với chủ, chủ ghi trên máy của chủ.
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
    </ManHinhDe>
  );
}

/** "tháng 08/2026" — viết đủ chữ tháng cho khỏi nhầm với ngày. */
function thangGon(ngay: string): string {
  const { nam, thang } = Ngay.tach(ngay);
  return `${String(thang).padStart(2, '0')}/${nam}`;
}

const kieu = StyleSheet.create({
  trong: { padding: 16, paddingTop: 4, paddingBottom: 24, gap: 10 },

  nhanBuoc: {
    marginTop: 8,
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
  },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  // Hai nút file mẫu chia đôi bề ngang. Chữ dài thì tự xuống dòng trong nút — `minHeight`
  // của nút chỉ là mức thấp nhất, nút cao thêm theo chữ.
  hangMau: { flexDirection: 'row', gap: 10 },

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
    flex: 1,
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
