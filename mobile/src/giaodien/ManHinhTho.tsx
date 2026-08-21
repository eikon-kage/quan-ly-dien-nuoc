import { Feather } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { chiaSeExcel } from '../nghiepvu/chiaSeExcel';
import { doiChieu } from '../nghiepvu/doiChieu';
import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { soCuaMay } from '../nghiepvu/soCong';
import { luongTaiNgay, tatCaTho } from '../nghiepvu/thaoTac';
import { CaiDatVai } from '../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from './dungDoiChieu';
import { DieuKhienSaoLuu } from './dungSaoLuu';
import { DieuKhienSaoLuuTaiKhoan } from './dungSaoLuuTaiKhoan';
import { DieuKhienNhom } from './dungSupabase';
import { HopSaoLuu } from './HopSaoLuu';
import { HopSuaTho } from './HopSuaTho';
import { HopNoiNhom } from './HopNoiNhom';
import { HopVaiMay } from './HopVaiMay';
import { ManHinhDoiChieu } from './ManHinhDoiChieu';
import { ManHinhNhapExcel } from './ManHinhNhapExcel';
import { DauTrang, NutChip, theTrang } from './ThanhPhan';
import { Co, Mau, PhongChu, Tuoi } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  saoLuu: DieuKhienSaoLuu;
  /** Bản sao lưu trên tài khoản của chủ — màn hình Sao lưu hiện cả trạng thái và danh sách. */
  saoLuuTaiKhoan: DieuKhienSaoLuuTaiKhoan;
  caiDat: CaiDatVai;
  datCaiDat: (moi: CaiDatVai) => void;
  dieuKhien: DieuKhienDoiChieu;
  nhom: DieuKhienNhom;
}

/** Trạng thái của nút xuất Excel. */
type TrangThaiXuat = 'ranh' | 'dangLam' | 'loi';

export function ManHinhTho({
  duLieu,
  capNhat,
  saoLuu,
  saoLuuTaiKhoan,
  caiDat,
  datCaiDat,
  dieuKhien,
  nhom,
}: Props) {
  /** null = đang đóng, 'them' = thêm mới, còn lại là thợ đang sửa. */
  const [dangMo, datDangMo] = useState<Tho | 'them' | null>(null);
  const [dangXuat, datDangXuat] = useState<TrangThaiXuat>('ranh');
  const [moSaoLuu, datMoSaoLuu] = useState(false);
  const [moNhap, datMoNhap] = useState(false);
  const [moDoiChieu, datMoDoiChieu] = useState(false);
  const [moVaiMay, datMoVaiMay] = useState(false);
  const [moNhom, datMoNhom] = useState(false);

  const thos = tatCaTho(duLieu);
  const homNay = Ngay.homNay();
  const coDuLieu = duLieu.buoiCongs.length > 0 || duLieu.ungTiens.length > 0 || thos.length > 0;

  const soDangLam = thos.filter((tho) => tho.dangLam).length;
  const soDaNghi = thos.length - soDangLam;
  const demTho =
    thos.length === 0
      ? 'Chưa có ai'
      : soDaNghi === 0
        ? `${soDangLam} đang làm`
        : `${soDangLam} đang làm · ${soDaNghi} đã nghỉ`;

  const { hoTro, dangChay, lucCuoi, loi } = saoLuu.trangThai;

  /**
   * Một dòng cho biết dữ liệu đang an toàn tới đâu. Xếp theo mức khẩn: máy không ghi được
   * → đang lỗi → đang chạy → xong lúc mấy giờ → chưa lần nào.
   */
  const chuSaoLuu = !hoTro
    ? 'Cần bản app cài thẳng vào máy'
    : loi !== null
      ? loi
      : dangChay
        ? 'Đang ghi…'
        : lucCuoi !== null
          ? `Đã sao lưu lúc ${Ngay.gioPhut(lucCuoi)}`
          : 'Chưa sao lưu lần nào';

  /**
   * Đếm thợ đang lệch. Chỉ một con số: chi tiết nằm trong màn hình đối chiếu, còn ở đây chỉ
   * cần trả lời "có phải mở ra xem không".
   */
  const soLech = useMemo(
    () =>
      [...dieuKhien.soBenKia.values()].filter(
        (daNhan) =>
          doiChieu(soCuaMay(duLieu, caiDat, daNhan.so.thoId, homNay), daNhan.so, homNay).lechs.length > 0,
      ).length,
    [duLieu, caiDat, dieuKhien.soBenKia, homNay],
  );

  const soDaGui = dieuKhien.soBenKia.size;
  const chuDoiChieu =
    soDaGui === 0
      ? 'Chưa thợ nào gửi sổ lên'
      : soLech > 0
        ? `${soLech} thợ ghi khác sổ mình`
        : `${soDaGui} sổ thợ, khớp cả`;

  const daAn = hoTro && loi === null && lucCuoi !== null;
  const iconSaoLuu = hoTro && loi === null ? 'save' : 'alert-circle';
  const mauSaoLuu = daAn ? Mau.xanhLa : loi !== null || !hoTro ? Mau.do : Mau.xam;

  async function xuatExcel() {
    if (dangXuat === 'dangLam') {
      return;
    }

    datDangXuat('dangLam');
    try {
      await chiaSeExcel(duLieu, homNay);
      datDangXuat('ranh');
    } catch {
      // Không báo lỗi máy móc — người dùng chỉ cần biết là chưa xong và bấm lại được.
      datDangXuat('loi');
    }
  }

  if (moDoiChieu) {
    return (
      <ManHinhDoiChieu
        duLieu={duLieu}
        capNhat={capNhat}
        caiDat={caiDat}
        dieuKhien={dieuKhien}
        nhom={nhom}
        onDong={() => datMoDoiChieu(false)}
      />
    );
  }

  return (
    <View style={kieu.khung}>
      {/*
        Nút Thêm thợ nằm trong đầu trang chứ không phải thanh xanh chiếm hết bề ngang như
        trước. Thêm thợ là việc làm vài lần rồi thôi, để nó to bằng cả màn hình thì lấn chỗ
        danh sách — thứ người dùng vào đây để xem. Vào đầu trang thì màn hình này cũng có
        đầu trang trắng giống Chấm công và Bảng lương, ba màn hình nhìn ra một bộ.
      */}
      <DauTrang
        tieuDe="Thợ"
        phu={demTho}
        phai={
          <Pressable style={kieu.nutThem} onPress={() => datDangMo('them')}>
            <Feather name="plus" size={16} color={Mau.trang} />
            <Text style={kieu.chuNutThem}>Thêm thợ</Text>
          </Pressable>
        }
      />

      <FlatList
        data={thos}
        keyExtractor={(tho) => tho.id}
        extraData={duLieu}
        contentContainerStyle={kieu.danhSach}
        ListEmptyComponent={
          <View style={kieu.trong}>
            <Feather name="users" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>Chưa có thợ nào</Text>
            <Text style={kieu.chuTrong}>Bấm nút Thêm thợ ở trên.</Text>
          </View>
        }
        renderItem={({ item: tho }) => (
          <View style={kieu.the}>
            <View style={kieu.trai}>
              <Text
                style={[kieu.chuTen, { color: tho.dangLam ? Mau.chu : Mau.xam }]}
                numberOfLines={1}
              >
                {tho.dangLam ? tho.ten : `${tho.ten} (đã nghỉ)`}
              </Text>
              <Text style={kieu.chuTien}>
                {Ngay.tien(luongTaiNgay(tho, homNay))} một công
                {tho.mocLuong.length > 1 ? `  ·  ${tho.mocLuong.length} mốc lương` : ''}
              </Text>
            </View>

            <NutChip nhan="Sửa" icon="edit-2" onPress={() => datDangMo(tho)} />
          </View>
        )}
      />

      {/*
        Xuất Excel và Sao lưu để ở đây chứ không ở Bảng lương: đây là việc thỉnh thoảng mới
        làm, để cạnh bảng lương thì lấn chỗ con số cần nhìn hằng ngày.
      */}
      {coDuLieu && (
        <View style={kieu.chanTrang}>
          {/*
            Sao lưu là một dòng chữ nhỏ có mũi tên chứ không phải nút to như Xuất Excel:
            bình thường nó tự chạy, người dùng chỉ ghé vào lúc muốn xem "đã ghi tới hôm nào".
            Để thành nút to thì hai nút cạnh nhau, nhìn như hai việc ngang nhau trong khi một
            cái phải bấm còn một cái thì không.

            Đối chiếu đứng trên sao lưu vì nó là việc *phải nhìn* — có thợ nào ghi khác sổ
            mình không — còn sao lưu thì tự chạy.
          */}
          <Pressable
            style={kieu.dongMuc}
            onPress={() => datMoDoiChieu(true)}
            accessibilityRole="button"
          >
            <Feather name="columns" size={16} color={soLech > 0 ? Mau.do : Mau.xam} />
            <View style={kieu.giuaDongMuc}>
              <Text style={kieu.chuNhanMuc}>Đối chiếu với sổ thợ</Text>
              <Text style={kieu.chuTrangThaiMuc}>{chuDoiChieu}</Text>
            </View>
            <Feather name="chevron-right" size={16} color={Mau.xam} />
          </Pressable>

          <Pressable
            style={kieu.dongMuc}
            onPress={() => datMoSaoLuu(true)}
            accessibilityRole="button"
          >
            <Feather name={iconSaoLuu} size={16} color={mauSaoLuu} />
            <View style={kieu.giuaDongMuc}>
              <Text style={kieu.chuNhanMuc}>Sao lưu</Text>
              <Text style={kieu.chuTrangThaiMuc}>{chuSaoLuu}</Text>
            </View>
            <Feather name="chevron-right" size={16} color={Mau.xam} />
          </Pressable>

          {/*
            Hai chiều của Excel đứng cạnh nhau thành một hàng, mỗi nút một nửa bề ngang:
            xếp dọc thành hai nút to thì chân trang cao thêm gần một nút nữa, mà chân trang
            cao lên là danh sách thợ — thứ người dùng vào đây để xem — ngắn đi.
          */}
          <View style={kieu.hangNut}>
            <Pressable
              style={[kieu.nutXuat, kieu.nutNua]}
              onPress={() => datMoNhap(true)}
              accessibilityRole="button"
            >
              <Feather name="file-plus" size={16} color={Mau.chinh} />
              <Text style={kieu.chuNutXuat}>Nhập từ Excel</Text>
            </Pressable>

            <Pressable
              style={[kieu.nutXuat, kieu.nutNua, dangXuat === 'dangLam' && kieu.nutXuatMo]}
              onPress={xuatExcel}
              disabled={dangXuat === 'dangLam'}
              accessibilityRole="button"
            >
              {dangXuat === 'dangLam' ? (
                <ActivityIndicator color={Mau.chinh} />
              ) : (
                <Feather name="share" size={16} color={Mau.chinh} />
              )}
              <Text style={kieu.chuNutXuat}>
                {dangXuat === 'dangLam' ? 'Đang tạo file…' : 'Xuất ra Excel'}
              </Text>
            </Pressable>
          </View>

          <Text style={[kieu.chuChanTrang, dangXuat === 'loi' && kieu.chuLoi]}>
            {dangXuat === 'loi'
              ? 'Chưa gửi được file. Bấm nút trên để làm lại.'
              : 'Nhập: điền công cả tháng trên máy tính rồi đưa vào app. ' +
                'Xuất: gửi qua Zalo hoặc mail để mở bằng Excel.'}
          </Text>
        </View>
      )}

      {/*
        Dòng này nằm ngoài khối `coDuLieu` ở trên, và đó là điều bắt buộc: máy thợ mới cài
        thì chưa có thợ nào, chưa có buổi nào — nếu ẩn theo `coDuLieu` thì đúng người cần
        nó nhất lại không có đường vào để nhận mã mời.
      */}
      <View style={kieu.hangCaiDat}>
        <Pressable
          style={kieu.dongVaiMay}
          onPress={() => datMoVaiMay(true)}
          accessibilityRole="button"
        >
          <Feather name="smartphone" size={15} color={Mau.xam} />
          <Text style={kieu.chuVaiMay}>Máy của chủ · đổi</Text>
        </Pressable>

        {nhom.trangThai.hoTro && (
          <Pressable
            style={kieu.dongVaiMay}
            onPress={() => datMoNhom(true)}
            accessibilityRole="button"
          >
            <Feather
              name={nhom.trangThai.thanhVien !== null ? 'users' : 'link'}
              size={15}
              color={nhom.trangThai.thanhVien !== null ? Mau.xanhLa : Mau.xam}
            />
            <Text style={kieu.chuVaiMay}>
              {nhom.trangThai.thanhVien !== null
                ? 'Đã nối nhóm'
                : nhom.trangThai.taiKhoan !== null
                  ? 'Chưa vào nhóm'
                  : 'Chưa nối nhóm'}
            </Text>
          </Pressable>
        )}
      </View>

      {moNhom && (
        <HopNoiNhom vai="chu" dieuKhien={nhom} onDong={() => datMoNhom(false)} />
      )}

      {moVaiMay && (
        <HopVaiMay
          duLieu={duLieu}
          capNhat={capNhat}
          caiDat={caiDat}
          datCaiDat={datCaiDat}
          nhom={nhom}
          onDong={() => datMoVaiMay(false)}
        />
      )}

      {moSaoLuu && (
        <HopSaoLuu
          duLieu={duLieu}
          saoLuu={saoLuu}
          taiKhoan={saoLuuTaiKhoan}
          capNhat={capNhat}
          onDong={() => datMoSaoLuu(false)}
        />
      )}

      {moNhap && (
        <ManHinhNhapExcel duLieu={duLieu} capNhat={capNhat} onDong={() => datMoNhap(false)} />
      )}

      {dangMo !== null && (
        <HopSuaTho
          duLieu={duLieu}
          tho={dangMo === 'them' ? null : dangMo}
          capNhat={capNhat}
          onDong={() => datDangMo(null)}
        />
      )}
    </View>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  hangCaiDat: { flexDirection: 'row', justifyContent: 'center', gap: 18, paddingBottom: 8 },
  dongVaiMay: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNutNho,
  },
  chuVaiMay: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  // Cao 44 bằng mũi tên đổi tháng bên Bảng lương — vẫn đúng mức tối thiểu Apple khuyên.
  nutThem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 7,
    minHeight: 44,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  chuNutThem: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.trang },

  danhSach: { padding: 16, paddingTop: 4, paddingBottom: 20 },
  the: { ...theTrang, flexDirection: 'row', alignItems: 'center', gap: 10, marginBottom: 12 },
  trai: { flex: 1, gap: 3 },
  chuTen: { fontSize: Co.chuTen, fontFamily: PhongChu.dam },
  chuTien: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  chanTrang: { paddingHorizontal: 16, paddingBottom: 12, gap: 10 },
  // Mỗi dòng là một thẻ trắng nổi bóng, giống hàng việc trong danh sách của bản thiết kế.
  dongMuc: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    minHeight: Co.caoNut,
    paddingVertical: 10,
  },
  giuaDongMuc: { flex: 1, gap: 2 },
  chuNhanMuc: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuTrangThaiMuc: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  hangNut: { flexDirection: 'row', gap: 10 },
  nutNua: { flex: 1 },
  nutXuat: {
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
  nutXuatMo: { opacity: 0.6 },
  chuNutXuat: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.chinh,
    textAlign: 'center',
  },
  chuChanTrang: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
  chuLoi: { color: Mau.do, fontFamily: PhongChu.vua },

  trong: { padding: 24, paddingTop: 56, gap: 10, alignItems: 'center' },
  chuTrongTo: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuTrong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
});
