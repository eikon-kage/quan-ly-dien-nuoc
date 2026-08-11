import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { chiaSeExcel } from '../nghiepvu/chiaSeExcel';
import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { luongTaiNgay, tatCaTho } from '../nghiepvu/thaoTac';
import { DieuKhienSaoLuu } from './dungSaoLuu';
import { HopSaoLuu } from './HopSaoLuu';
import { HopSuaTho } from './HopSuaTho';
import { rungNhe } from './rungNhe';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  saoLuu: DieuKhienSaoLuu;
}

/** Trạng thái của nút xuất Excel. */
type TrangThaiXuat = 'ranh' | 'dangLam' | 'loi';

export function ManHinhTho({ duLieu, capNhat, saoLuu }: Props) {
  /** null = đang đóng, 'them' = thêm mới, còn lại là thợ đang sửa. */
  const [dangMo, datDangMo] = useState<Tho | 'them' | null>(null);
  const [dangXuat, datDangXuat] = useState<TrangThaiXuat>('ranh');
  const [moSaoLuu, datMoSaoLuu] = useState(false);

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

  const { hoTro, taiKhoan, dangChay, lucCuoi, loi } = saoLuu.trangThai;

  /**
   * Một dòng cho biết dữ liệu đang an toàn tới đâu. Xếp theo mức khẩn: máy không nối được
   * → chưa nối → đang lỗi → đang chạy → xong lúc mấy giờ.
   */
  const chuDrive = !hoTro
    ? 'Cần bản app cài thẳng vào máy'
    : taiKhoan === null
      ? 'Chưa nối — dữ liệu chỉ nằm trong máy này'
      : loi !== null
        ? loi
        : dangChay
          ? 'Đang đẩy lên…'
          : lucCuoi !== null
            ? `Đã sao lưu lúc ${Ngay.gioPhut(lucCuoi)}`
            : 'Đã nối, chưa sao lưu lần nào';

  const daAn = hoTro && taiKhoan !== null && loi === null;
  const iconDrive = daAn ? 'cloud' : 'cloud-off';
  const mauDrive = daAn ? Mau.xanhLa : loi !== null ? Mau.do : Mau.xam;

  async function xuatExcel() {
    if (dangXuat === 'dangLam') {
      return;
    }

    rungNhe();
    datDangXuat('dangLam');
    try {
      await chiaSeExcel(duLieu, homNay);
      datDangXuat('ranh');
    } catch {
      // Không báo lỗi máy móc — người dùng chỉ cần biết là chưa xong và bấm lại được.
      datDangXuat('loi');
    }
  }

  return (
    <View style={kieu.khung}>
      {/*
        Nút Thêm thợ nằm trong đầu trang chứ không phải thanh xanh chiếm hết bề ngang như
        trước. Thêm thợ là việc làm vài lần rồi thôi, để nó to bằng cả màn hình thì lấn chỗ
        danh sách — thứ người dùng vào đây để xem. Vào đầu trang thì màn hình này cũng có
        đầu trang trắng giống Chấm công và Bảng lương, ba màn hình nhìn ra một bộ.
      */}
      <View style={kieu.dauTrang}>
        <View style={kieu.giuaDauTrang}>
          <Text style={kieu.chuTieuDe}>Thợ</Text>
          <Text style={kieu.chuDem}>{demTho}</Text>
        </View>

        <Pressable style={kieu.nutThem} onPress={() => datDangMo('them')}>
          <Feather name="plus" size={16} color={Mau.trang} />
          <Text style={kieu.chuNutThem}>Thêm thợ</Text>
        </Pressable>
      </View>

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

            <Pressable style={kieu.nutSua} onPress={() => datDangMo(tho)}>
              <Feather name="edit-2" size={12} color={Mau.chinh} />
              <Text style={kieu.chuNutSua}>Sửa</Text>
            </Pressable>
          </View>
        )}
      />

      {/*
        Xuất Excel và Sao lưu Drive để ở đây chứ không ở Bảng lương: đây là việc thỉnh
        thoảng mới làm, để cạnh bảng lương thì lấn chỗ con số cần nhìn hằng ngày.
      */}
      {coDuLieu && (
        <View style={kieu.chanTrang}>
          {/*
            Sao lưu là một dòng chữ nhỏ có mũi tên chứ không phải nút to như Xuất Excel:
            bình thường nó tự chạy, người dùng chỉ ghé vào lúc muốn xem "đã lên Drive
            chưa". Để thành nút to thì hai nút cạnh nhau, nhìn như hai việc ngang nhau
            trong khi một cái phải bấm còn một cái thì không.
          */}
          <Pressable
            style={kieu.dongDrive}
            onPress={() => datMoSaoLuu(true)}
            accessibilityRole="button"
          >
            <Feather name={iconDrive} size={16} color={mauDrive} />
            <View style={kieu.giuaDongDrive}>
              <Text style={kieu.chuDrive}>Sao lưu Google Drive</Text>
              <Text style={kieu.chuTrangThaiDrive}>{chuDrive}</Text>
            </View>
            <Feather name="chevron-right" size={16} color={Mau.xam} />
          </Pressable>

          <Pressable
            style={[kieu.nutXuat, dangXuat === 'dangLam' && kieu.nutXuatMo]}
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
              {dangXuat === 'dangLam' ? 'Đang tạo file…' : 'Xuất toàn bộ ra Excel'}
            </Text>
          </Pressable>

          <Text style={[kieu.chuChanTrang, dangXuat === 'loi' && kieu.chuLoi]}>
            {dangXuat === 'loi'
              ? 'Chưa gửi được file. Bấm nút trên để làm lại.'
              : 'Gửi qua Zalo, gửi mail hoặc lưu vào máy tính để mở bằng Excel.'}
          </Text>
        </View>
      )}

      {moSaoLuu && (
        <HopSaoLuu saoLuu={saoLuu} capNhat={capNhat} onDong={() => datMoSaoLuu(false)} />
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

  dauTrang: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    backgroundColor: Mau.trang,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: Mau.vien,
  },
  giuaDauTrang: { flex: 1, gap: 2 },
  chuTieuDe: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuDem: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  // Cao 44 bằng mũi tên đổi tháng bên Bảng lương — vẫn đúng mức tối thiểu Apple khuyên.
  nutThem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 7,
    height: 44,
    paddingHorizontal: 16,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  chuNutThem: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.trang },

  danhSach: { padding: 14, paddingBottom: 20 },
  the: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    backgroundColor: Mau.trang,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: Mau.vien,
    padding: 14,
    marginBottom: 10,
  },
  trai: { flex: 1, gap: 3 },
  chuTen: { fontSize: Co.chuTen, fontFamily: PhongChu.dam },
  chuTien: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  nutSua: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    height: Co.caoNutNho,
    paddingHorizontal: 14,
    borderRadius: 8,
    backgroundColor: Mau.chinhNhat,
  },
  chuNutSua: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  chanTrang: {
    backgroundColor: Mau.trang,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
    padding: 14,
    gap: 8,
  },
  dongDrive: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 9,
    height: Co.caoNut,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.nen,
  },
  giuaDongDrive: { flex: 1, gap: 2 },
  chuDrive: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuTrangThaiDrive: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  nutXuat: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    height: Co.caoNut,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.chinh,
    backgroundColor: Mau.chinhNhat,
  },
  nutXuatMo: { opacity: 0.6 },
  chuNutXuat: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chinh },
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
