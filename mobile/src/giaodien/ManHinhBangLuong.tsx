import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { DongLuong, cacThangXemDuoc, thang as bangLuongThang } from '../nghiepvu/bangLuong';
import { baoCaoKhoang } from '../nghiepvu/baoCao';
import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import { baoCaoKyHienTai, buoiDaChot, kyHienTai } from '../nghiepvu/ky';
import * as Ngay from '../nghiepvu/ngayViet';
import {
  dangCham,
  datCong,
  datGhiChuNgay,
  ghiChuNgay,
  suaUng,
  themUng,
  xoaUng,
} from '../nghiepvu/thaoTac';
import { HopNhapSo } from './HopNhapSo';
import { CachSuaNgay } from './HopSuaNgay';
import { ManHinhBaoCaoTho } from './ManHinhBaoCaoTho';
import { ManHinhQuyetToan } from './ManHinhQuyetToan';
import { DauTrang, NutChip, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
}

/**
 * Bảng lương. Mở ra là **kỳ đang mở**: từ sau lần quyết toán trước tới hôm nay — đó là chỗ
 * làm việc, có ứng tiền và có quyết toán. Hai mũi tên ở đầu trang lùi về **từng tháng
 * dương lịch** đã qua để tra lại.
 *
 * > Có lúc màn hình này bỏ hẳn mũi tên, chỉ còn kỳ đang mở, vì tiền công ngoài công trình
 * > không chạy theo tháng: xong việc là trả, có khi mười ngày, có khi sáu tuần. Chủ dự án
 * > yêu cầu cho lại: kỳ cắt theo lúc trả tiền, nhưng người ta vẫn nhớ việc theo tháng —
 * > *"tháng Tám vừa rồi hết bao nhiêu tiền công"* là câu hỏi có thật, mà cắt theo kỳ thì
 * > không trả lời được.
 *
 * Hai cách cắt không khớp nhau và **không được giả vờ là khớp**. Nên xem theo tháng thì
 * con số to nhất là *tiền công cả tháng*, không phải *còn phải trả*: món nợ giữa chủ và
 * thợ chốt theo kỳ chứ không theo tháng, ghi "còn phải trả" cho một tháng lẻ là ghi ra một
 * con số không ai đòi ai cả. Xem theo tháng cũng không ứng tiền và không sửa gì được —
 * chỗ ấy chỉ để tra sổ.
 */
export function ManHinhBangLuong({ duLieu, capNhat }: Props) {
  const homNay = Ngay.homNay();
  const [dangUng, datDangUng] = useState<Tho | null>(null);
  const [xemBaoCao, datXemBaoCao] = useState<string | null>(null);
  const [dangQuyetToan, datDangQuyetToan] = useState(false);
  /** 0 là kỳ đang mở; 1 trở lên là tháng thứ mấy trong `cacThang`, đếm lùi từ tháng này. */
  const [viTri, datViTri] = useState(0);

  const ky = kyHienTai(duLieu, homNay);
  const cacThang = cacThangXemDuoc(duLieu, homNay);

  // Xoá bớt dữ liệu thì danh sách tháng ngắn lại, vị trí đang đứng có thể trỏ ra ngoài.
  const dangO = Math.min(viTri, cacThang.length);
  const oThang = dangO > 0 ? cacThang[dangO - 1] : undefined;

  /** Trọn tháng đang xem. `undefined` khi đang đứng ở kỳ đang mở. */
  const thangDangXem = oThang && {
    ...oThang,
    tuNgay: Ngay.ghep(oThang.nam, oThang.thang, 1),
    denNgay: Ngay.ghep(oThang.nam, oThang.thang, Ngay.soNgayTrongThang(oThang.nam, oThang.thang)),
    dongs: bangLuongThang(duLieu, oThang.nam, oThang.thang),
  };

  const dongs = thangDangXem ? thangDangXem.dongs : ky.dongs;
  const tienCongCaThang = dongs.reduce((tong, dong) => tong + dong.tienCong, 0);
  const congCaThang = dongs.reduce((tong, dong) => tong + dong.tongCong, 0);

  function ghiUng(soTien: number, ghiChu: string) {
    if (dangUng === null) {
      return;
    }

    capNhat(themUng(duLieu, dangUng.id, homNay, soTien, ghiChu));
    datDangUng(null);
  }

  /**
   * Cách sửa thẳng một ngày trên tờ lịch trong màn hình chi tiết một thợ.
   *
   * Chỉ dựng cho **kỳ đang mở**, cùng lý do với `suaUng`: xem theo tháng là chỗ tra sổ cũ.
   * Đọc số công từ cả sổ chứ không từ bản báo cáo — báo cáo của kỳ đang mở đã lọc bỏ buổi
   * của kỳ đã chốt, mà hộp sửa thì phải nói thật là ngày ấy có gì, rồi khoá đúng buổi
   * không được đụng vào.
   */
  function cachSuaNgay(thoId: string): CachSuaNgay {
    return {
      cong: (ngay, buoi) => dangCham(duLieu, thoId, ngay, buoi)?.soCong ?? null,
      khoa: (ngay, buoi) => {
        const buoiCong = dangCham(duLieu, thoId, ngay, buoi);
        return buoiCong !== undefined && buoiDaChot(duLieu, buoiCong.id);
      },
      datCong: (ngay, buoi, soCong) => capNhat(datCong(duLieu, thoId, ngay, buoi, soCong)),
      ghiChu: {
        doc: (ngay) => ghiChuNgay(duLieu, thoId, ngay),
        ghi: (ngay, chu) => capNhat(datGhiChuNgay(duLieu, thoId, ngay, chu)),
      },
    };
  }

  return (
    <View style={kieu.khung}>
      <DauTrang
        tieuDe={thangDangXem ? `Tháng ${thangDangXem.thang}/${thangDangXem.nam}` : 'Kỳ này'}
        phu={
          thangDangXem
            ? `${Ngay.khoangGon(thangDangXem.tuNgay, thangDangXem.denNgay)} · chỉ để xem lại`
            : ky.dongs.length === 0
              ? 'Chưa có công nào'
              : `${Ngay.khoangGon(ky.tuNgay, ky.denNgay)} · ${Ngay.thu(ky.denNgay)}`
        }
        phai={
          cacThang.length > 0 ? (
            <View style={kieu.hangMuiTen}>
              <NutThang
                huong={-1}
                tat={dangO >= cacThang.length}
                onPress={() => datViTri(dangO + 1)}
              />
              <NutThang huong={1} tat={dangO === 0} onPress={() => datViTri(dangO - 1)} />
            </View>
          ) : undefined
        }
      />

      <FlatList
        data={dongs}
        keyExtractor={(dong) => dong.tho.id}
        contentContainerStyle={kieu.danhSach}
        ListEmptyComponent={
          <View style={kieu.trong}>
            <Feather name="credit-card" size={34} color={Mau.xam} />
            <Text style={kieu.chuTrongTo}>
              {thangDangXem
                ? `Tháng ${thangDangXem.thang}/${thangDangXem.nam} không có công nào`
                : 'Kỳ này chưa có công nào'}
            </Text>
            <Text style={kieu.chuTrong}>
              {thangDangXem
                ? 'Tháng ấy không ai đi làm, cũng không ai ứng tiền.'
                : 'Sang mục Chấm công để chấm cho thợ.'}
            </Text>
          </View>
        }
        renderItem={({ item: dong }) =>
          thangDangXem ? (
            <TheThang dong={dong} onPress={() => datXemBaoCao(dong.tho.id)} />
          ) : (
            <TheKy
              dong={dong}
              onUng={() => datDangUng(dong.tho)}
              onPress={() => datXemBaoCao(dong.tho.id)}
            />
          )
        }
      />

      {/*
        Chân trang của tháng chỉ cộng lại tiền công, **không có nút quyết toán**: chốt kỳ
        là chốt những bản ghi chưa ai trả tiền, không phải chốt một khúc lịch.
      */}
      {thangDangXem !== undefined && dongs.length > 0 && (
        <View style={kieu.chanTrang}>
          <Text style={kieu.chuTong}>
            Cả tổ tháng {thangDangXem.thang}: {Ngay.soCong(congCaThang)} công ·{' '}
            <Text style={kieu.chuTongSo}>{Ngay.tien(tienCongCaThang)}</Text> tiền công
          </Text>
        </View>
      )}

      {thangDangXem === undefined && ky.chotDuoc && (
        <View style={kieu.chanTrang}>
          <Text style={kieu.chuTong}>
            Cả tổ còn phải trả: <Text style={kieu.chuTongSo}>{Ngay.tien(ky.tongPhaiTra)}</Text>
          </Text>

          {/*
            Nút quyết toán không chốt luôn mà mở ra màn hình đếm tiền — chốt kỳ là việc
            nặng nhất trong app, phải nhìn thấy từng người bao nhiêu trước khi gật đầu.
          */}
          <Pressable style={kieu.nutQuyetToan} onPress={() => datDangQuyetToan(true)}>
            <Feather name="check-circle" size={17} color={Mau.trang} />
            <Text style={kieu.chuNutQuyetToan}>Quyết toán kỳ này</Text>
          </Pressable>
        </View>
      )}

      {dangUng !== null && (
        <HopNhapSo
          tieuDe={`${dangUng.ten} ứng tiền`}
          moTa="Thợ ứng bao nhiêu?"
          goiY="Ví dụ 500000"
          oChu={{
            nhan: 'Ghi chú (không bắt buộc)',
            goiY: 'Ví dụ: ứng đổ xăng',
          }}
          onGhi={ghiUng}
          onDong={() => datDangUng(null)}
        />
      )}

      {xemBaoCao !== null &&
        (thangDangXem ? (
          /*
            Xem theo tháng thì lấy thẳng theo ngày, không lọc theo bản ghi đã quyết toán:
            tháng nào cũng phải ra đủ số công của tháng ấy, dù tiền đã trả xong từ đời nào.
            Cũng không truyền `suaUng` vào: sửa một lần ứng đã nằm trong kỳ đã chốt là làm
            lệch tờ quyết toán đã in ra đưa thợ.
          */
          <ManHinhBaoCaoTho
            dungBaoCao={(tu, den) => baoCaoKhoang(duLieu, xemBaoCao, tu, den, homNay)}
            tuNgayDau={thangDangXem.tuNgay}
            denNgayDau={thangDangXem.denNgay}
            onDong={() => datXemBaoCao(null)}
          />
        ) : (
          <ManHinhBaoCaoTho
            dungBaoCao={(tu, den) => baoCaoKyHienTai(duLieu, xemBaoCao, homNay, tu, den)}
            tuNgayDau={ky.tuNgay}
            denNgayDau={ky.denNgay}
            // Kỳ này chưa chốt nên sửa lại lịch sử ứng được: gõ nhầm số tiền, ghi muộn nên
            // lệch ngày, hay ghi hai lần cùng một lần đưa tiền.
            suaUng={{
              // Ứng ghi vào hôm nay, y như nút Ứng tiền ngoài danh sách — ghi muộn mấy hôm
              // thì chữa lại ngày bằng chính hộp sửa ngay bên trên.
              them: (soTien, ghiChu) =>
                capNhat(themUng(duLieu, xemBaoCao, homNay, soTien, ghiChu)),
              ghi: (ungId, ngay, soTien, ghiChu) =>
                capNhat(suaUng(duLieu, ungId, ngay, soTien, ghiChu)),
              xoa: (ungId) => capNhat(xoaUng(duLieu, ungId)),
            }}
            // Chấm bù hay chữa một ngày ngay tại chỗ nhìn ra chỗ sai, khỏi thoát ra mục
            // Chấm công rồi lần lại đúng ngày ấy.
            suaNgay={cachSuaNgay(xemBaoCao)}
            onDong={() => datXemBaoCao(null)}
          />
        ))}

      {dangQuyetToan && (
        <ManHinhQuyetToan
          duLieu={duLieu}
          homNay={homNay}
          capNhat={capNhat}
          onDong={() => datDangQuyetToan(false)}
        />
      )}
    </View>
  );
}

/**
 * Mũi tên lùi / tới một tháng.
 *
 * Đây là một trong hai chỗ duy nhất trong app có nút **chỉ có hình** (điều 8 trong
 * docs/chamcong-giao-dien.md): mũi tên đổi tháng đã quá quen mặt, mà tiêu đề ngay bên
 * cạnh lúc nào cũng ghi rõ đang đứng ở tháng nào nên không phải đoán.
 *
 * Hết đường thì mũi tên **mờ đi chứ không biến mất**: nút biến mất làm hai nút còn lại
 * nhảy chỗ, bấm lùi liên tục mấy tháng là bấm trượt.
 */
function NutThang({ huong, tat, onPress }: { huong: -1 | 1; tat: boolean; onPress: () => void }) {
  return (
    <Pressable
      style={[kieu.nutThang, tat && kieu.nutThangTat]}
      onPress={onPress}
      disabled={tat}
      accessibilityRole="button"
      accessibilityLabel={huong === -1 ? 'Tháng trước' : 'Tháng sau'}
      accessibilityState={{ disabled: tat }}
    >
      <Feather
        name={huong === -1 ? 'chevron-left' : 'chevron-right'}
        size={22}
        color={tat ? Mau.vien : Mau.chinh}
      />
    </Pressable>
  );
}

/** Thẻ của kỳ đang mở: có ứng tiền, có nợ kỳ trước, có *còn phải trả*. */
function TheKy({
  dong,
  onUng,
  onPress,
}: {
  dong: DongLuong;
  onUng: () => void;
  onPress: () => void;
}) {
  return (
    // Bấm cả thẻ để xem chi tiết: đi làm ngày nào, nghỉ ngày nào, ứng ngày nào.
    <Pressable style={kieu.the} onPress={onPress}>
      <View style={kieu.dongTen}>
        <Text style={kieu.chuTen} numberOfLines={1}>
          {dong.tho.ten}
        </Text>
        <NutChip nhan="Ứng tiền" icon="arrow-up-right" onPress={onUng} />
      </View>

      <Text style={kieu.chuPhu}>
        {Ngay.soCong(dong.tongCong)} công · sáng {Ngay.soCong(dong.congSang)}, chiều{' '}
        {Ngay.soCong(dong.congChieu)}
      </Text>

      <View style={kieu.dongSo}>
        <Text style={kieu.chuNhan}>Tiền công</Text>
        <Text style={kieu.chuSo}>{Ngay.tien(dong.tienCong)}</Text>
      </View>

      {dong.daUng > 0 && (
        <View style={kieu.dongSo}>
          <Text style={kieu.chuNhan}>Đã ứng</Text>
          <Text style={kieu.chuSo}>{Ngay.tienTru(dong.daUng)}</Text>
        </View>
      )}

      {/*
        Nợ kỳ trước đứng thành một dòng riêng, không cộng thầm vào tiền công — thợ
        hỏi "sao kỳ này nhiều thế" thì chỉ đúng vào dòng này mà trả lời.
      */}
      {dong.noKyTruoc !== 0 && (
        <View style={kieu.dongSo}>
          <Text style={kieu.chuNhan}>{dong.noKyTruoc > 0 ? 'Nợ kỳ trước' : 'Kỳ trước trả dư'}</Text>
          <Text style={kieu.chuSo}>
            {dong.noKyTruoc > 0 ? Ngay.tien(dong.noKyTruoc) : Ngay.tienTru(dong.noKyTruoc)}
          </Text>
        </View>
      )}

      <View style={kieu.gach} />

      {/* Con số anh cần khi móc ví. */}
      <View style={kieu.dongSo}>
        <Text style={kieu.chuConLai}>Còn phải trả</Text>
        <Text style={[kieu.chuSoConLai, { color: dong.conLai < 0 ? Mau.do : Mau.xanhLa }]}>
          {Ngay.tien(dong.conLai)}
        </Text>
      </View>

      <View style={kieu.dongXem}>
        <Text style={kieu.chuXem}>Xem chi tiết từng ngày</Text>
        <Feather name="chevron-right" size={15} color={Mau.chinh} />
      </View>
    </Pressable>
  );
}

/**
 * Thẻ của một tháng đã qua. Không có nút *Ứng tiền* — ứng tiền bao giờ cũng ghi vào hôm
 * nay, để nút ấy trên một tháng cũ là mời người dùng ghi nhầm ngày.
 *
 * Con số to nhất là **tiền công cả tháng**. Không có dòng *còn phải trả*: xem ghi chú đầu
 * tệp — tháng và kỳ cắt khác nhau, số dư chỉ có nghĩa ở kỳ.
 */
function TheThang({ dong, onPress }: { dong: DongLuong; onPress: () => void }) {
  return (
    <Pressable style={kieu.the} onPress={onPress}>
      <Text style={kieu.chuTen} numberOfLines={1}>
        {dong.tho.ten}
      </Text>

      <Text style={kieu.chuPhu}>
        {Ngay.soCong(dong.tongCong)} công · sáng {Ngay.soCong(dong.congSang)}, chiều{' '}
        {Ngay.soCong(dong.congChieu)}
      </Text>

      {dong.daUng > 0 && (
        <View style={kieu.dongSo}>
          <Text style={kieu.chuNhan}>Đã ứng trong tháng</Text>
          <Text style={kieu.chuSo}>{Ngay.tienTru(dong.daUng)}</Text>
        </View>
      )}

      <View style={kieu.gach} />

      <View style={kieu.dongSo}>
        <Text style={kieu.chuConLai}>Tiền công cả tháng</Text>
        <Text style={[kieu.chuSoConLai, { color: Mau.chu }]}>{Ngay.tien(dong.tienCong)}</Text>
      </View>

      <View style={kieu.dongXem}>
        <Text style={kieu.chuXem}>Xem chi tiết từng ngày</Text>
        <Feather name="chevron-right" size={15} color={Mau.chinh} />
      </View>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },

  hangMuiTen: { flexDirection: 'row', gap: 8 },
  // Cao 44 chứ không phải 48 như nút thường, bằng nút Thêm thợ bên màn hình Thợ.
  nutThang: {
    minWidth: 44,
    minHeight: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },
  // Tắt thì bỏ luôn nền trắng và bóng: nút phẳng lì vào nền trang, nhìn là biết
  // không bấm được, không phải bấm thử mới biết.
  nutThangTat: {
    backgroundColor: 'transparent',
    shadowOpacity: 0,
    elevation: 0,
  },

  danhSach: { padding: 16, paddingTop: 4, paddingBottom: 20 },
  the: { ...theTrang, marginBottom: 12, gap: 7 },
  dongTen: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  chuTen: {
    flex: 1,
    fontSize: Co.chuTen,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
  },

  chuPhu: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
  },
  dongSo: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  chuNhan: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
  },
  chuSo: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  gach: { height: 1, backgroundColor: Mau.vien, marginVertical: 3 },
  chuConLai: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.vua,
    color: Mau.chu,
  },
  chuSoConLai: { fontSize: Co.chuTen, fontFamily: PhongChu.dam },
  dongXem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
    marginTop: 4,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
  },
  chuXem: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  trong: { padding: 24, paddingTop: 56, gap: 10, alignItems: 'center' },
  chuTrongTo: {
    fontSize: Co.chuTieuDe,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    textAlign: 'center',
  },
  chuTrong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },

  // Nằm thẳng trên nền trang: thanh tab ngay dưới đã là mảng trắng nổi bóng rồi.
  chanTrang: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    gap: 10,
    alignItems: 'center',
  },
  chuTong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
  chuTongSo: { fontFamily: PhongChu.dam, color: Mau.chu },
  nutQuyetToan: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    alignSelf: 'stretch',
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  chuNutQuyetToan: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.trang,
    textAlign: 'center',
  },
});
