import { Feather } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { chiaSeSoCong } from '../nghiepvu/chiaSeExcel';
import { doiChieu } from '../nghiepvu/doiChieu';
import { BuoiLam, CAC_BUOI, CONG_MOT_BUOI, DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { CONG_TOI_DA, docSoCong } from '../nghiepvu/nhapSo';
import { soCuaMay } from '../nghiepvu/soCong';
import { boCham, cham, dangCham, datCong, timTho } from '../nghiepvu/thaoTac';
import { CaiDatVai } from '../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from './dungDoiChieu';
import { DieuKhienNhom } from './dungSupabase';
import { HopChon } from './HopChon';
import { HopChonNgay } from './HopChonNgay';
import { HopNhapSo } from './HopNhapSo';
import { HopNoiNhom } from './HopNoiNhom';
import { CachSuaNgay } from './HopSuaNgay';
import { HopVaiMay } from './HopVaiMay';
import { ManHinhDoiChieu } from './ManHinhDoiChieu';
import { ManHinhNhapExcel } from './ManHinhNhapExcel';
import { ManHinhSoCuaToi } from './ManHinhSoCuaToi';
import { DauTrang, HangO, TheSo, theTrang } from './ThanhPhan';
import { Bong, Co, HeSoChuToiDaLuoi, Mau, PhongChu, Tuoi } from './thietKe';

/**
 * Màn hình riêng của máy thợ.
 *
 * Làm hẳn một màn hình khác chứ không lọc bớt màn hình của chủ. Hai người vào app vì hai
 * việc khác nhau: chủ chấm cho mười người rồi tính tiền, còn thợ chỉ trả lời đúng một câu
 * hỏi — *hôm nay tôi có đi làm không*. Lọc bớt màn hình của chủ thì còn lại một cái lưới
 * một cột, thừa đủ thứ dây nhợ mà vẫn không có cái nút to nhất đáng có.
 *
 * Không có thanh tab: cả máy thợ chỉ có một màn hình này, mở ra là chấm được ngay.
 * Không có một con số tiền nào ở đây — cả app trên máy thợ không biết tiền công là bao
 * nhiêu, xem `ketNap`. Điều đó ràng cả nút xuất Excel: nó xuất `SoCong` qua
 * [chiaSeSoCong](../nghiepvu/chiaSeExcel.ts), không xuất `DuLieuChamCong` như máy chủ — gọi
 * bản của máy chủ là file gửi ra ngoài mang đủ tiền công, dù màn hình không hiện đồng nào.
 */

/** Hôm nay và 13 ngày trước. Đủ để chấm bù một hai tuần lỡ quên, không phải cuộn cả tháng. */
const SO_NGAY = 14;

/** Trạng thái của nút xuất Excel. Giống bên màn hình của chủ. */
type TrangThaiXuat = 'ranh' | 'dangLam' | 'loi';

const TEN_BUOI: Record<BuoiLam, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

/** Ngày viết gọn còn "05/08" — năm thì thợ không cần trên màn hình này. */
function ngayNgan(ngay: string): string {
  return Ngay.ngayGon(ngay).slice(0, 5);
}

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  caiDat: CaiDatVai;
  datCaiDat: (moi: CaiDatVai) => void;
  dieuKhien: DieuKhienDoiChieu;
  nhom: DieuKhienNhom;
}

export function ManHinhThoTuCham({ duLieu, capNhat, caiDat, datCaiDat, dieuKhien, nhom }: Props) {
  const [moDoiChieu, datMoDoiChieu] = useState(false);
  const [moSoCong, datMoSoCong] = useState(false);
  const [moNhap, datMoNhap] = useState(false);
  /** null = đóng, 'chon' = mở ra hỏi vai máy, 'ma' = mở thẳng ô dán mã mời. */
  const [moVaiMay, datMoVaiMay] = useState<'chon' | 'ma' | null>(null);
  const [moNhom, datMoNhom] = useState(false);
  const [dangXuat, datDangXuat] = useState<TrangThaiXuat>('ranh');
  /** Ô đang mở hộp chọn số công. */
  const [dangSua, datDangSua] = useState<{ ngay: string; buoi: BuoiLam } | null>(null);
  /** Đang gõ số công cho ô ấy, thay vì chọn một trong mấy mức có sẵn. */
  const [goSoCong, datGoSoCong] = useState(false);
  /**
   * Tháng đang xem ở danh sách chấm bù — một ngày bất kỳ trong tháng ấy. `null` là mặc
   * định: mười bốn ngày gần đây, vắt qua cả tháng trước nếu hôm nay mới mùng hai.
   */
  const [thangXem, datThangXem] = useState<string | null>(null);
  /** Tháng đang mở trong tờ lịch chọn ngày; null là chưa mở. */
  const [mocLich, datMocLich] = useState<string | null>(null);

  const thoId = caiDat.thoId ?? '';
  const homNay = Ngay.homNay();

  /**
   * Những ngày mời chấm bù, mới → cũ.
   *
   * Mặc định là mười bốn ngày gần đây. Chọn một tháng thì chạy trọn tháng ấy, dừng ở hôm
   * nay — thợ thắc mắc chuyện *tháng trước* là chuyện thường, mà trước đây quá mười ba
   * ngày là không còn ô nào để bấm: nhớ ra hôm mùng năm mình có đi mà không chấm được,
   * đường duy nhất là nhờ chủ sửa hộ.
   */
  const cacNgay = useMemo(() => {
    if (thangXem === null) {
      return Array.from({ length: SO_NGAY }, (_, i) => Ngay.congNgay(homNay, -i));
    }

    const { nam, thang } = Ngay.tach(thangXem);
    const dauThang = Ngay.ghep(nam, thang, 1);
    const cuoiThang = Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang));
    const cuoi = cuoiThang < homNay ? cuoiThang : homNay;

    const ngays: string[] = [];
    for (let ngay = cuoi; ngay >= dauThang; ngay = Ngay.congNgay(ngay, -1)) {
      ngays.push(ngay);
    }
    return ngays;
  }, [homNay, thangXem]);

  /** Nhãn của bộ lọc, cũng là chữ trên nút mở tờ lịch. */
  const nhanKhoang =
    thangXem === null
      ? `${SO_NGAY} ngày gần đây`
      : `Tháng ${Ngay.tach(thangXem).thang}/${Ngay.tach(thangXem).nam}`;

  /** Tên lấy từ sổ chủ gửi xuống nếu có: chủ mới là bên đặt tên, thợ không phải tự gõ. */
  const tenTho =
    dieuKhien.soBenKia.get(thoId)?.so.tenTho || timTho(duLieu, thoId)?.ten || 'Tôi';

  /**
   * Sổ của chính máy này, cắt đúng khoảng nó khai là đầy đủ.
   *
   * Tính một lần rồi dùng cho cả đối chiếu lẫn nút xuất Excel: hai chỗ tự cắt theo hai kiểu
   * là hai kết quả khác nhau trên cùng một dữ liệu, mà thợ thì đem file đi so với màn hình.
   */
  const soCuaToi = useMemo(
    () => soCuaMay(duLieu, caiDat, thoId, homNay),
    [duLieu, caiDat, thoId, homNay],
  );

  const ket = useMemo(() => {
    const cuaChu = dieuKhien.soBenKia.get(thoId);
    if (!cuaChu) {
      return null;
    }
    return doiChieu(soCuaToi, cuaChu.so, homNay);
  }, [soCuaToi, dieuKhien.soBenKia, thoId]);

  /**
   * Tổng công một khoảng, tính tới hôm nay. Đây là *cái nhìn tổng quan* mà trước đây màn
   * hình này không có: mở app ra chỉ thấy hôm nay và một danh sách dài, thợ muốn biết
   * "tháng này tôi được bao nhiêu công" thì phải tự ngồi đếm ô.
   */
  const congTuNgay = (tuNgay: string) =>
    duLieu.buoiCongs
      .filter((b) => b.thoId === thoId && b.ngay >= tuNgay && b.ngay <= homNay)
      .reduce((tong, b) => tong + b.soCong, 0);

  /**
   * Công của chính thợ này theo từng ngày, để mỗi ô trên tờ lịch nói luôn ngày ấy mấy
   * công. Tờ lịch nhờ vậy vừa là chỗ chọn tháng vừa là chỗ *xem lại*: mở ra là thấy cả
   * tháng ngày nào đi ngày nào nghỉ.
   */
  const congMoiNgay = useMemo(() => {
    const theoNgay = new Map<string, number>();
    for (const buoi of duLieu.buoiCongs) {
      if (buoi.thoId === thoId) {
        theoNgay.set(buoi.ngay, (theoNgay.get(buoi.ngay) ?? 0) + buoi.soCong);
      }
    }
    return theoNgay;
  }, [duLieu, thoId]);

  const dauTuanNay = Ngay.tuan(homNay)[0];
  const { nam, thang } = Ngay.tach(homNay);
  const congTuanNay = congTuNgay(dauTuanNay);
  const congThangNay = congTuNgay(Ngay.ghep(nam, thang, 1));

  /**
   * Mấy ngày trước gom lại theo tuần, mỗi tuần một thẻ. Trước đây 13 ngày là 13 thẻ trắng
   * rời nhau, cuộn mãi không hết mà vẫn không thấy được tuần nào đi nhiều tuần nào đi ít.
   */
  const cacTuan = useMemo(() => {
    const theoTuan = new Map<string, string[]>();
    // cacNgay xếp mới → cũ, nên Map giữ đúng thứ tự ấy cho các tuần. Bỏ đúng hôm nay chứ
    // không bỏ dòng đầu: xem tháng trước thì hôm nay không nằm trong danh sách, cắt dòng
    // đầu là mất ngày cuối tháng ấy.
    for (const ngay of cacNgay) {
      if (ngay === homNay) {
        continue;
      }

      const dau = Ngay.tuan(ngay)[0];
      theoTuan.set(dau, [...(theoTuan.get(dau) ?? []), ngay]);
    }

    return [...theoTuan.entries()].map(([dauTuan, ngays]) => ({ dauTuan, ngays }));
  }, [cacNgay, homNay]);

  /**
   * Tổng công của cả tuần, **kể cả hôm nay** — dù hôm nay không nằm trong danh sách dưới
   * (nó đã có thẻ riêng ở trên). Đếm theo mấy dòng đang hiện thì con số ở thẻ *Tuần này*
   * lại khác con số ở ô tóm tắt, hai chỗ trên cùng một màn hình nói hai kiểu.
   */
  function congCuaTuan(dauTuan: string): number {
    const cuoiTuan = Ngay.congNgay(dauTuan, 6);
    return duLieu.buoiCongs
      .filter(
        (b) =>
          b.thoId === thoId && b.ngay >= dauTuan && b.ngay <= (cuoiTuan < homNay ? cuoiTuan : homNay),
      )
      .reduce((tong, b) => tong + b.soCong, 0);
  }

  /** *Tuần này* / *Tuần trước* nói nhanh hơn hai cái ngày; xa hơn nữa thì ghi rõ khoảng. */
  function nhanTuan(dauTuan: string): string {
    if (dauTuan === dauTuanNay) {
      return 'Tuần này';
    }
    if (dauTuan === Ngay.congNgay(dauTuanNay, -7)) {
      return 'Tuần trước';
    }
    return `${ngayNgan(dauTuan)} → ${ngayNgan(Ngay.congNgay(dauTuan, 6))}`;
  }

  /**
   * Lùi / tới một tháng trong tờ lịch. Nhảy qua hẳn mép tháng chứ không cộng 30 ngày —
   * tháng thiếu tháng thừa thì cộng ngày có lúc nhảy vọt qua cả một tháng.
   *
   * Không cho đi quá tháng này: chấm bù là chuyện của những ngày đã qua, mà tháng sau thì
   * mở ra chỉ có một tờ lịch trắng không bấm được ô nào.
   */
  function doiThangLich(buoc: -1 | 1) {
    if (mocLich === null) {
      return;
    }

    const { nam, thang } = Ngay.tach(mocLich);
    const moi =
      buoc === -1
        ? Ngay.congNgay(Ngay.ghep(nam, thang, 1), -1)
        : Ngay.congNgay(Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang)), 1);

    // Lùi thì bao giờ cũng được; tới thì chỉ khi mồng một tháng ấy đã trôi qua.
    if (moi <= homNay) {
      datMocLich(moi);
    }
  }

  async function xuatExcel() {
    if (dangXuat === 'dangLam') {
      return;
    }

    datDangXuat('dangLam');
    try {
      await chiaSeSoCong(soCuaToi);
      datDangXuat('ranh');
    } catch {
      // Không báo lỗi máy móc — người dùng chỉ cần biết là chưa xong và bấm lại được.
      datDangXuat('loi');
    }
  }

  /** Cách hộp sửa một ngày đọc và ghi buổi công — dùng ở màn hình *Sổ công của tôi*. */
  const cachSuaNgay: CachSuaNgay = {
    cong: (ngay, buoi) => dangCham(duLieu, thoId, ngay, buoi)?.soCong ?? null,
    datCong: (ngay, buoi, soCong) => capNhat(datCong(duLieu, thoId, ngay, buoi, soCong)),
  };

  function doCham(ngay: string, buoi: BuoiLam) {
    const dang = dangCham(duLieu, thoId, ngay, buoi);
    capNhat(dang ? boCham(duLieu, thoId, ngay, buoi) : cham(duLieu, thoId, ngay, buoi));
  }

  function chonSoCong(ma: string) {
    if (dangSua === null) {
      return;
    }
    const { ngay, buoi } = dangSua;

    // Mấy mức hay dùng để sẵn thành nút, nhưng số nào cũng gõ được — y như bên máy chủ.
    // Thợ đi làm nửa buổi, một phần tư buổi là chuyện thật; bắt chọn một trong ba mức thì
    // thợ chấm sai rồi chờ chủ sửa hộ, mà đối chiếu lại báo lệch.
    if (ma === 'goSo') {
      datGoSoCong(true);
      return;
    }

    if (ma === 'nghi') {
      capNhat(boCham(duLieu, thoId, ngay, buoi));
    } else {
      // Một buổi đi đủ là nửa công, vì cả ngày mới là một công — xem `CONG_MOT_BUOI`.
      const soCong =
        ma === 'nua' ? CONG_MOT_BUOI / 2 : ma === 'ruoi' ? CONG_MOT_BUOI * 1.5 : CONG_MOT_BUOI;
      capNhat(cham(duLieu, thoId, ngay, buoi, soCong));
    }
    datDangSua(null);
  }

  function ghiSoCong(so: number) {
    if (dangSua === null) {
      return;
    }

    capNhat(cham(duLieu, thoId, dangSua.ngay, dangSua.buoi, so));
    datGoSoCong(false);
    datDangSua(null);
  }

  /*
    Hai màn hình con này vẽ đè lên chỗ của màn hình chính chứ không bọc trong `Modal`: cửa
    sổ của `Modal` nằm ngoài `SafeAreaView` của App, đầu trang sẽ chạy tọt lên dưới thanh
    trạng thái.
  */
  if (moSoCong) {
    return (
      <ManHinhSoCuaToi
        so={soCuaToi}
        soChu={dieuKhien.soBenKia.get(thoId)?.so ?? null}
        homNay={homNay}
        /*
          Chấm bù thẳng trên tờ lịch của sổ mình: đó là chỗ thợ nhìn ra ngày trống, mà
          trước đây phải lui về màn hình chính rồi dò lại đúng ngày ấy.

          Không có `khoa`: máy thợ không chốt kỳ bao giờ, cờ `daChot` trong sổ chỉ có ở sổ
          chủ gửi xuống. Cũng không có `ghiChu`: cả máy thợ không có chỗ nào ghi chú.
        */
        suaNgay={cachSuaNgay}
        onDong={() => datMoSoCong(false)}
      />
    );
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

  const { dangChay, ketNoi, lucCuoi, loi } = dieuKhien.trangThai;

  /**
   * Chưa vào nhóm thì cái nút ở đầu trang **mở hộp nối nhóm**, chứ không phải bấm không ăn
   * như trước: câu chỉ đường của App viết cho máy chủ — "mở mục Thợ → Nhóm chấm công" — mà
   * máy thợ không có mục Thợ, không có cả thanh tab. Thợ đọc xong không biết bấm vào đâu.
   */
  const daVaoNhom = nhom.trangThai.thanhVien !== null;

  /**
   * Vào nhóm là **dán mã mời**, nên đường vào phải dẫn thẳng tới ô dán mã — không dẫn sang
   * hộp *Nhóm chấm công*: hộp ấy với máy thợ chỉ để xem trạng thái và ngắt, mở ra chỉ đọc
   * được một câu bảo đi chỗ khác. Đúng cái ngõ cụt mà thợ đang gặp.
   */
  const moDanMa = nhom.trangThai.hoTro ? () => datMoVaiMay('ma') : ketNoi.noi;
  const chuDongBo = !ketNoi.sanSang
    ? (ketNoi.chuaSanSang ?? 'Chưa nối nhóm — sổ chưa gửi cho chủ')
    : loi !== null
      ? loi
      : dangChay
        ? 'Đang gửi sổ…'
        : lucCuoi !== null
          ? `Đã gửi sổ lúc ${Ngay.gioPhut(lucCuoi)}`
          : 'Chưa gửi sổ lần nào';

  return (
    <View style={kieu.khung}>
      <DauTrang
        tieuDe={tenTho}
        phu={chuDongBo}
        phai={
          <Pressable
            style={kieu.nutDongBo}
            onPress={ketNoi.sanSang ? dieuKhien.dongBo : moDanMa}
            disabled={dangChay || (!ketNoi.sanSang && moDanMa === undefined)}
            accessibilityLabel={ketNoi.sanSang ? 'Gửi sổ cho chủ' : 'Nối nhóm'}
          >
            {dangChay ? (
              <ActivityIndicator color={Mau.chinh} />
            ) : (
              <Feather
                name={ketNoi.sanSang ? 'refresh-cw' : 'link'}
                size={18}
                color={Mau.chinh}
              />
            )}
          </Pressable>
        }
      />

      <ScrollView contentContainerStyle={kieu.than}>
        {/*
          Hôm nay tách hẳn ra một thẻ với hai ô cao gấp rưỡi những ngày khác. Chín phần
          mười lần mở app là để chấm cho hôm nay — cái nút ấy phải là thứ to nhất màn hình,
          không phải một dòng lẫn trong danh sách.
        */}
        <View style={kieu.theHomNay}>
          <Text style={kieu.chuHomNay}>Hôm nay, {Ngay.thuVaNgay(homNay)}</Text>
          <View style={kieu.hangO}>
            {CAC_BUOI.map((buoi) => (
              <OCham
                key={buoi}
                nhan={TEN_BUOI[buoi]}
                to
                soCong={dangCham(duLieu, thoId, homNay, buoi)?.soCong ?? null}
                onPress={() => doCham(homNay, buoi)}
                onLongPress={() => datDangSua({ ngay: homNay, buoi })}
              />
            ))}
          </View>
        </View>

        {/*
          Chưa vào nhóm thì đây là việc gấp nhất sau chấm công: chấm mà không nối nhóm thì sổ
          nằm im trong máy, chủ không thấy gì. Nên nó là một dải màu nằm ngay dưới thẻ *Hôm
          nay*, không phải một dòng chữ xám cuối trang — chỗ ấy thợ cuộn tới đã là may.
        */}
        {nhom.trangThai.hoTro && !daVaoNhom && (
          <Pressable
            style={kieu.daiNhom}
            onPress={() => datMoVaiMay('ma')}
            accessibilityRole="button"
          >
            <Feather name="link" size={17} color={Mau.chinh} />
            <View style={kieu.giuaDong}>
              <Text style={kieu.chuNhanNhom}>
                {nhom.trangThai.taiKhoan !== null ? 'Chưa vào nhóm' : 'Chưa nối nhóm'}
              </Text>
              <Text style={kieu.chuPhuNhom}>
                Bấm để dán mã mời của chủ — chưa vào nhóm thì sổ chưa gửi đi đâu cả
              </Text>
            </View>
            <Feather name="chevron-right" size={17} color={Mau.chinh} />
          </Pressable>
        )}

        {/*
          Hai ô tóm tắt: mở app là biết ngay tuần này và tháng này được bao nhiêu công, khỏi
          đếm ô. Dùng đúng ô tóm tắt của bản thiết kế như bên máy chủ — chỉ khác là **không
          có ô tiền nào**, máy thợ không biết tiền công.
        */}
        <HangO>
          {/* Gọi là *Công tuần này* chứ không phải *Tuần này*: thẻ tuần dưới cũng mang nhãn ấy. */}
          <TheSo nhan="Công tuần này" so={`${Ngay.soCong(congTuanNay)} công`} mau="chinh" />
          <TheSo nhan="Công tháng này" so={`${Ngay.soCong(congThangNay)} công`} mau="ngoc" />
        </HangO>

        <Pressable
          style={kieu.dongDoiChieu}
          onPress={() => datMoDoiChieu(true)}
          accessibilityRole="button"
        >
          <Feather
            name={ket === null ? 'inbox' : ket.lechs.length === 0 ? 'check-circle' : 'alert-circle'}
            size={17}
            color={ket === null ? Mau.xam : ket.lechs.length === 0 ? Mau.xanhLa : Mau.do}
          />
          <View style={kieu.giuaDong}>
            <Text style={kieu.chuNhan}>Đối chiếu với sổ chủ</Text>
            <Text style={kieu.chuPhu}>
              {ket === null
                ? 'Chưa có sổ của chủ'
                : ket.khongTrungKhoang
                  ? 'Chưa so được'
                  : ket.lechs.length === 0
                    ? // Còn buổi mới một bên có sổ thì đừng nói "khớp cả": phần so được thì
                      // khớp, nhưng sổ chủ đang có công ở ngày máy này chưa có sổ.
                      ket.chuaBiets.length > 0
                      ? `Khớp ${ket.soKhop} buổi · ${ket.chuaBiets.length} buổi chưa so được`
                      : `Khớp cả ${ket.soKhop} buổi`
                    : `Lệch ${ket.lechs.length} buổi`}
            </Text>
          </View>
          <Feather name="chevron-right" size={17} color={Mau.xam} />
        </Pressable>

        {/*
          Hai việc "xem sổ" và "gửi sổ" đứng cạnh nhau thành một hàng, mỗi nút một nửa bề
          ngang — giống chân trang màn hình Thợ bên máy chủ. Trước đây mỗi việc một dòng thẻ
          trắng có mũi tên: ba dòng như thế xếp dọc đẩy danh sách chấm bù xuống quá nửa màn
          hình, mà hai việc này thì cả tuần mới dùng một lần.

          Sổ công là chỗ *tra* — thợ thắc mắc thì hay thắc mắc chuyện tháng trước, mà danh
          sách dưới đây chỉ có 14 ngày.
        */}
        <View style={kieu.hangNut}>
          <Pressable
            style={kieu.nutViec}
            onPress={() => datMoSoCong(true)}
            accessibilityRole="button"
          >
            <Feather name="calendar" size={16} color={Mau.chinh} />
            <Text style={kieu.chuNutViec}>Sổ công của tôi</Text>
          </Pressable>

          {/*
            Chỉ hiện khi đã có buổi nào: chưa chấm gì mà bấm thì file gửi đi là một trang
            trống, người nhận tưởng thợ không đi làm ngày nào.
          */}
          {soCuaToi.dongs.length > 0 && (
            <Pressable
              style={[kieu.nutViec, dangXuat === 'dangLam' && kieu.nutMo]}
              onPress={xuatExcel}
              disabled={dangXuat === 'dangLam'}
              accessibilityRole="button"
            >
              {dangXuat === 'dangLam' ? (
                <ActivityIndicator color={Mau.chinh} />
              ) : (
                <Feather name="share" size={16} color={Mau.chinh} />
              )}
              <Text style={kieu.chuNutViec}>
                {dangXuat === 'dangLam' ? 'Đang tạo file…' : 'Xuất ra Excel'}
              </Text>
            </Pressable>
          )}

          {/*
            Nhập từ Excel cũng có mặt trên máy thợ, không phải việc riêng của chủ. Thợ mới
            cài app giữa tháng thì ở đây là cả tháng công cũ phải chấm bù — mà danh sách
            trên chỉ mời chấm bù mười ba ngày, xa hơn nữa là không có ô nào mà bấm. Gõ một
            bảng trên máy tính rồi nhập vào là đường duy nhất cho quãng ấy.

            Không có tiền ứng ở đường này: xem `choTho` trong ManHinhNhapExcel.
          */}
          <Pressable
            style={kieu.nutViec}
            onPress={() => datMoNhap(true)}
            accessibilityRole="button"
          >
            <Feather name="upload" size={16} color={Mau.chinh} />
            <Text style={kieu.chuNutViec}>Nhập từ Excel</Text>
          </Pressable>
        </View>

        {soCuaToi.dongs.length > 0 && (
          <Text style={[kieu.chuGhiChu, dangXuat === 'loi' && kieu.chuLoi]}>
            {dangXuat === 'loi'
              ? 'Chưa gửi được file. Bấm nút trên để làm lại.'
              : 'Xuất: gửi qua Zalo hay mail — chỉ có số công, không có tiền.'}
          </Text>
        )}

        {/*
          Nhãn nói rõ danh sách dưới đây để làm gì. Trước đây chỗ này ghi "14 ngày gần đây ·
          6 công" — con số ấy giờ đã có ở ô tóm tắt, còn thợ thì cần biết *bấm vào đây được
          gì*, chứ không cần một con số thứ ba.

          Bỏ hôm nay: nó đã ở thẻ trên, để lại đây nữa là hai chỗ chấm cùng một buổi.
        */}
        {/*
          Nhãn nói danh sách dưới đây là gì, cạnh nó là nút đổi khoảng. Nút mang sẵn chữ
          của khoảng đang xem — thợ nhìn một cái là biết mình đang đứng ở đâu, không phải
          mở tờ lịch ra mới biết.
        */}
        <View style={kieu.dongMuc}>
          <Text style={kieu.chuMuc}>Chấm bù mấy ngày trước</Text>

          <View style={kieu.nhomLoc}>
            {/* Lỡ lọc sang tháng cũ rồi thì đây là đường về, giống nút *Hôm nay* bên máy chủ. */}
            {thangXem !== null && (
              <Pressable
                style={kieu.nutGanDay}
                onPress={() => datThangXem(null)}
                accessibilityLabel={`Về ${SO_NGAY} ngày gần đây`}
              >
                <Feather name="corner-up-left" size={13} color={Mau.chinh} />
                <Text style={kieu.chuNutLoc}>Gần đây</Text>
              </Pressable>
            )}

            <Pressable
              style={kieu.nutLoc}
              onPress={() => datMocLich(thangXem ?? homNay)}
              accessibilityRole="button"
              accessibilityLabel={`Đang xem ${nhanKhoang}. Chạm để chọn tháng khác.`}
            >
              <Feather name="calendar" size={14} color={Mau.chinh} />
              <Text style={kieu.chuNutLoc}>{nhanKhoang}</Text>
              <Feather name="chevron-down" size={15} color={Mau.xam} />
            </Pressable>
          </View>
        </View>

        {/* Tháng vừa sang mà chưa qua ngày nào ngoài hôm nay: nói rõ chứ đừng để trống trơn. */}
        {cacTuan.length === 0 && (
          <Text style={kieu.chuTrongThang}>
            Tháng này chưa có ngày nào trước hôm nay để chấm bù.
          </Text>
        )}

        {cacTuan.map(({ dauTuan, ngays }) => (
          <View key={dauTuan} style={kieu.theTuan}>
            <View style={kieu.dauTuan}>
              <Text style={kieu.chuTuan}>{nhanTuan(dauTuan)}</Text>
              <Text style={kieu.chuCongTuan}>{Ngay.soCong(congCuaTuan(dauTuan))} công</Text>
            </View>

            {ngays.map((ngay, thuTu) => (
              <View key={ngay} style={[kieu.dongNgay, thuTu > 0 && kieu.dongSau]}>
                {/*
                  Thứ và ngày xếp dọc thành một cột hẹp bên trái, hai ô chấm nằm cùng dòng
                  chứ không xuống dòng dưới như bản cũ: mỗi ngày cao bằng một nút thay vì
                  gần hai nút, cả tuần lọt trong một tầm mắt.
                */}
                <View style={kieu.coNgay}>
                  <Text
                    style={[kieu.chuThuNgay, Ngay.soThu(ngay) === 0 && kieu.chuChuNhat]}
                    maxFontSizeMultiplier={HeSoChuToiDaLuoi}
                  >
                    {Ngay.thuGon(ngay)}
                  </Text>
                  <Text style={kieu.chuNgay} maxFontSizeMultiplier={HeSoChuToiDaLuoi}>
                    {ngayNgan(ngay)}
                  </Text>
                </View>

                {/*
                  `flex: 1` ở đây là thứ giữ hai ô trong lề: dòng ngày là một hàng ngang, mà
                  hàng ngang thì con không tự co — thiếu nó là ô *Chiều* tràn khỏi thẻ. Thẻ
                  *Hôm nay* không cần vì ở đó hàng ô là con duy nhất của một cột dọc.
                */}
                <View style={[kieu.hangO, kieu.hangOTrongDong]}>
                  {CAC_BUOI.map((buoi) => (
                    <OCham
                      key={buoi}
                      nhan={TEN_BUOI[buoi]}
                      ngayNhan={Ngay.thuVaNgay(ngay)}
                      soCong={dangCham(duLieu, thoId, ngay, buoi)?.soCong ?? null}
                      onPress={() => doCham(ngay, buoi)}
                      onLongPress={() => datDangSua({ ngay, buoi })}
                    />
                  ))}
                </View>
              </View>
            ))}
          </View>
        ))}

        <View style={kieu.hangCaiDat}>
          <Pressable style={kieu.dongCaiDat} onPress={() => datMoVaiMay('chon')}>
            <Feather name="user" size={15} color={Mau.xam} />
            <Text style={kieu.chuPhu}>Máy của thợ · đổi lại</Text>
          </Pressable>

          {nhom.trangThai.hoTro && (
            <Pressable style={kieu.dongCaiDat} onPress={() => datMoNhom(true)}>
              <Feather
                name={daVaoNhom ? 'users' : 'link'}
                size={15}
                color={daVaoNhom ? Mau.xanhLa : Mau.xam}
              />
              {/*
                Nối rồi thì ghi thêm chữ *thoát* ngay trên dòng: đăng xuất là việc thợ có
                quyền làm — đổi điện thoại, thôi làm chỗ này — mà nó chỉ nằm trong hộp thì
                nhìn màn hình không có dấu hiệu nào cho thấy có đường ra. Chưa vào nhóm thì
                dải màu ở trên đã nói rồi, đây chỉ là đường vào để xem lại.
              */}
              <Text style={kieu.chuPhu}>
                {daVaoNhom ? 'Đã nối nhóm · thoát' : 'Nhóm chấm công'}
              </Text>
            </Pressable>
          )}
        </View>
      </ScrollView>

      {dangSua !== null && !goSoCong && (
        <HopChon
          tieuDe={`${Ngay.thuVaNgay(dangSua.ngay)} — buổi ${dangSua.buoi === 'Sang' ? 'sáng' : 'chiều'}`}
          luaChon={[
            { ma: 'ca', nhan: 'Cả buổi (0,5 công)', icon: 'check' },
            { ma: 'nua', nhan: 'Nửa buổi (0,25 công)', icon: 'clock' },
            { ma: 'ruoi', nhan: 'Buổi rưỡi (0,75 công)', icon: 'plus-circle' },
            { ma: 'goSo', nhan: 'Gõ số công khác', icon: 'edit-3' },
            { ma: 'nghi', nhan: 'Nghỉ buổi này', icon: 'x-circle', nguyHiem: true },
          ]}
          onChon={chonSoCong}
          onDong={() => datDangSua(null)}
        />
      )}

      {/* Cùng hộp, cùng cách đọc số và cùng mức chặn như bên máy chủ: hai bên gõ ra hai kiểu
          số thì đối chiếu báo lệch mà chẳng ai sai. */}
      {goSoCong && dangSua !== null && (
        <HopNhapSo
          tieuDe={`${Ngay.thuVaNgay(dangSua.ngay)} — buổi ${dangSua.buoi === 'Sang' ? 'sáng' : 'chiều'}`}
          moTa="Buổi này mấy công?"
          goiY="Ví dụ 0,5"
          giaTriDau={
            dangCham(duLieu, thoId, dangSua.ngay, dangSua.buoi) !== undefined
              ? Ngay.soCong(dangCham(duLieu, thoId, dangSua.ngay, dangSua.buoi)?.soCong ?? 0)
              : ''
          }
          doc={docSoCong}
          hienLai={(so) => `${Ngay.soCong(so)} công`}
          banPhim="decimal-pad"
          loi={(so) => (so > CONG_TOI_DA ? `Nhiều nhất ${Ngay.soCong(CONG_TOI_DA)} công một buổi.` : null)}
          onGhi={ghiSoCong}
          onDong={() => {
            datGoSoCong(false);
            datDangSua(null);
          }}
        />
      )}

      {moNhap && (
        <ManHinhNhapExcel
          duLieu={duLieu}
          capNhat={capNhat}
          choTho={{ thoId, ten: tenTho }}
          onDong={() => datMoNhap(false)}
        />
      )}

      {/*
        Tờ lịch cả tháng, mỗi ô ghi luôn số công của ngày ấy. Chạm một ngày là danh sách
        dưới nhảy sang trọn tháng của nó — chấm bù được ngay, không phải chỉ ngồi xem.
      */}
      {mocLich !== null && (
        <HopChonNgay
          tieuDe="Xem tháng nào?"
          nam={Ngay.tach(mocLich).nam}
          thang={Ngay.tach(mocLich).thang}
          ngayDangChon={thangXem ?? homNay}
          congMoiNgay={congMoiNgay}
          onDoiThang={doiThangLich}
          onChon={(ngay) => {
            datThangXem(ngay);
            datMocLich(null);
          }}
          onDong={() => datMocLich(null)}
        />
      )}

      {moNhom && <HopNoiNhom vai="tho" dieuKhien={nhom} onDong={() => datMoNhom(false)} />}

      {moVaiMay !== null && (
        <HopVaiMay
          duLieu={duLieu}
          capNhat={capNhat}
          caiDat={caiDat}
          datCaiDat={datCaiDat}
          nhom={nhom}
          danMaNgay={moVaiMay === 'ma'}
          onDong={() => datMoVaiMay(null)}
        />
      )}
    </View>
  );
}

/**
 * Ô chấm một buổi. Giống ô bên màn hình của chủ — cùng ba tín hiệu (nền, dấu tích, màu
 * chữ) để hai bên nhìn ra cùng một thứ khi ngồi soát với nhau.
 */
function OCham({
  nhan,
  soCong,
  ngayNhan,
  to = false,
  onPress,
  onLongPress,
}: {
  nhan: string;
  soCong: number | null;
  /** Ngày đọc lên kèm tên buổi. Cả tuần trong một thẻ thì "Sáng, chưa chấm" nghe giống nhau hết. */
  ngayNhan?: string;
  to?: boolean;
  onPress: () => void;
  onLongPress: () => void;
}) {
  const daCham = soCong !== null;

  return (
    <Pressable
      style={[kieu.oCham, to && kieu.oChamTo, daCham ? kieu.oChamBat : kieu.oChamTat]}
      onPress={onPress}
      onLongPress={onLongPress}
      accessibilityLabel={`${ngayNhan !== undefined ? `${ngayNhan} ` : ''}${nhan} ${
        daCham ? 'có đi làm' : 'chưa chấm'
      }`}
      accessibilityHint="Bấm để đổi, bấm giữ để chọn nửa buổi"
    >
      <Feather
        name={daCham ? 'check-circle' : 'circle'}
        size={to ? 20 : 16}
        color={daCham ? Mau.xanhLa : Mau.xam}
      />
      <Text
        style={[kieu.chuOCham, to && kieu.chuOChamTo, { color: daCham ? Mau.chu : Mau.xam }]}
      >
        {nhan}
        {daCham && soCong !== CONG_MOT_BUOI ? `  ${Ngay.soCong(soCong)}` : ''}
      </Text>
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  than: { padding: 16, paddingTop: 4, gap: 10, paddingBottom: 28 },

  nutDongBo: {
    width: 44,
    height: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },

  theHomNay: { ...theTrang, gap: 12, marginBottom: 2 },
  chuHomNay: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  // Dải nối nhóm: nền xanh nhạt viền xanh tươi như ô tóm tắt — nổi hơn thẻ trắng thường mà
  // vẫn không hét lên như một lỗi đỏ. Chưa nối nhóm không phải là sai, chỉ là còn một việc
  // chưa làm.
  daiNhom: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 12,
    borderWidth: 1,
    borderColor: Tuoi.chinh,
    backgroundColor: Mau.chinhNhat,
  },
  chuNhanNhom: { fontSize: Co.chuNut, fontFamily: PhongChu.dam, color: Mau.chinh },
  chuPhuNhom: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.chu },

  dongDoiChieu: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 12,
  },
  giuaDong: { flex: 1, gap: 2 },
  chuNhan: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuLoi: { fontFamily: PhongChu.vua, color: Mau.do },

  // Nhãn và nút lọc cùng một dòng, cho xuống dòng khi chữ to: cỡ chữ hệ thống phóng lên
  // thì hai thứ này không nhét vừa một hàng, mà nút bị cắt là mất hẳn đường sang tháng cũ.
  dongMuc: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
    marginTop: 8,
  },
  chuMuc: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
  },
  nhomLoc: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  nutLoc: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Tuoi.chinh,
    backgroundColor: Mau.chinhNhat,
  },
  nutGanDay: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    minHeight: Co.caoNutNho,
    paddingVertical: 6,
    paddingHorizontal: 12,
    // Bo tròn hẳn chứ không lấy nửa chiều cao: chữ to thì nút cao lên, số cứng hoá vuông góc.
    borderRadius: 999,
    backgroundColor: Mau.chinhNhat,
  },
  chuNutLoc: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },
  chuTrongThang: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    paddingVertical: 6,
  },

  // Hai nút việc: viền màu nhạt như bên máy chủ, không phải nút xanh đặc — chúng không
  // phải việc chính của màn hình này.
  // Ba việc thì không nhét vừa một hàng: `minWidth` đẩy cái thứ ba xuống dòng dưới, ở đó
  // `flex: 1` cho nó ăn hết bề ngang. Ba nút chen một hàng là ba nhãn bị cắt còn một chữ.
  hangNut: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  nutViec: {
    flex: 1,
    minWidth: 150,
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
  nutMo: { opacity: 0.6 },
  chuNutViec: {
    flexShrink: 1,
    fontSize: Co.chuNut,
    fontFamily: PhongChu.vua,
    color: Mau.chinh,
    textAlign: 'center',
  },
  chuGhiChu: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },

  // Một tuần một thẻ: các ngày ngăn nhau bằng vạch mảnh trong lòng thẻ, chứ không phải
  // mỗi ngày một thẻ trắng rời — mười ba thẻ rời nhìn như mười ba việc phải làm.
  theTuan: { ...theTrang, paddingVertical: 6 },
  dauTuan: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 10,
    paddingVertical: 6,
  },
  chuTuan: { fontSize: Co.chuNut, fontFamily: PhongChu.dam, color: Mau.chu },
  chuCongTuan: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xanhLa },

  dongNgay: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingVertical: 6 },
  dongSau: { borderTopWidth: 1, borderTopColor: Mau.vien },
  coNgay: { width: 46 },
  chuThuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.dam, color: Mau.chu },
  // Chủ Nhật đỏ như lịch treo tường — nhìn ra ngay đâu là ngày nghỉ mà khỏi đọc chữ.
  chuChuNhat: { color: Mau.do },
  chuNgay: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  hangO: { flexDirection: 'row', gap: 10 },
  hangOTrongDong: { flex: 1 },
  oCham: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 9,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
    borderWidth: 1,
  },
  oChamTo: { minHeight: 72 },
  oChamBat: { backgroundColor: Mau.xanhLaNhat, borderColor: Tuoi.xanhLa },
  oChamTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  chuOCham: { flexShrink: 1, fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
  chuOChamTo: { fontSize: Co.chuTieuDe },

  hangCaiDat: { flexDirection: 'row', justifyContent: 'center', gap: 18, marginTop: 8 },
  dongCaiDat: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNutNho,
  },
});
