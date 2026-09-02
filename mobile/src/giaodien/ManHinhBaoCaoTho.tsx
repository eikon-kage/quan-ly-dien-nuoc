import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { BaoCaoTho } from '../nghiepvu/baoCao';
import { UngTien } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { HopChonNgay } from './HopChonNgay';
import { CachSuaNgay, HopSuaNgay } from './HopSuaNgay';
import { HopSuaUng } from './HopSuaUng';
import { LichCong } from './LichCong';
import { ManHinhDe } from './ManHinhDe';
import { DauTrang, HangO, TheSo, ThanhDoan, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu } from './thietKe';

interface Props {
  /**
   * Dựng báo cáo cho một khoảng ngày. Truyền hàm vào chứ không truyền thẳng dữ liệu:
   * kỳ đang mở và kỳ đã chốt lấy buổi công theo hai cách khác nhau (theo bản ghi nào đã
   * quyết toán), màn hình này không cần biết chuyện đó.
   */
  dungBaoCao: (tuNgay: string, denNgay: string) => BaoCaoTho | null;
  /** Khoảng mở ra lúc đầu — trọn kỳ đang xem. */
  tuNgayDau: string;
  denNgayDau: string;
  /**
   * Cho sửa lịch sử ứng tiền: chạm một dòng ứng là mở hộp sửa số tiền, ngày, ghi chú,
   * hoặc xoá hẳn lần ứng ấy.
   *
   * Không truyền thì danh sách ứng **chỉ để đọc** — đó là màn hình kỳ đã chốt, nơi mỗi
   * dòng ứng đã được đếm vào một tờ quyết toán đã trao tay. Hai hàm đi thành một cụm chứ
   * không thành hai prop rời: cả hai cùng nằm trong một hộp, có cái này mà thiếu cái kia
   * thì hộp ấy mở ra hỏng một nửa.
   */
  suaUng?: {
    ghi: (ungId: string, ngay: string, soTien: number, ghiChu: string) => void;
    xoa: (ungId: string) => void;
  };
  /**
   * Cho sửa thẳng trên tờ lịch: chạm một ô ngày là mở hộp chấm cho đúng ngày ấy.
   *
   * Không truyền thì tờ lịch **chỉ để đọc**, giống hệt lý do của `suaUng` ở trên: kỳ đã
   * chốt và mấy tháng cũ đều là sổ đã trả tiền xong. Đây cũng chính là chỗ tra khi thợ
   * thắc mắc, nên chữa ngay tại chỗ thấy sai là đường ngắn nhất — trước đây phải thoát ra,
   * sang mục Chấm công rồi lần lại đúng ngày ấy.
   */
  suaNgay?: CachSuaNgay;
  onDong: () => void;
}

/**
 * Các khoảng có sẵn trên thanh phân đoạn. Khoảng mở ra lúc đầu luôn đứng đầu: lỡ lọc hẹp
 * rồi thì đó là đường về.
 *
 * Màn hình này mở ra từ hai chỗ cắt sổ khác nhau — một kỳ lương, hoặc một tháng dương lịch
 * bên Bảng lương — nên viên đầu gọi tên theo đúng thứ đang xem. Mở từ trọn một tháng thì
 * viên *Cả tháng* trùng khít viên đầu, bỏ đi: một viên bấm vào không đổi gì là một viên
 * làm người ta ngờ mình bấm hụt.
 */
function khoangSanCua(laTronThang: boolean) {
  return [
    { ma: 'ky', nhan: laTronThang ? 'Cả tháng' : 'Cả kỳ' },
    ...(laTronThang ? [] : [{ ma: 'thang', nhan: 'Cả tháng' }]),
    { ma: 'dau', nhan: 'Nửa đầu' },
    { ma: 'cuoi', nhan: 'Nửa cuối' },
  ];
}

/** Ngày viết gọn còn "05/08" — trong màn hình này năm đã ghi trên đầu rồi. */
function ngayNgan(ngay: string): string {
  return Ngay.ngayGon(ngay).slice(0, 5);
}

/**
 * Chi tiết một thợ: đi làm ngày nào, nghỉ ngày nào, ứng tiền ngày nào. Đây là chỗ tra
 * khi thợ thắc mắc "sao kỳ này ít tiền thế".
 *
 * Mở ra là trọn kỳ, nhưng chọn được khoảng hẹp hơn — nhiều nhà trả một phần giữa chừng
 * chứ không đợi chốt kỳ, lúc ấy con số cần nhìn là của mấy ngày đó chứ không phải cả kỳ.
 *
 * Kỳ chốt lúc nào cũng được nên nó hay vắt qua hai tháng. Mỗi tháng vẽ một tờ lịch riêng
 * xếp dọc, có tên tháng ở trên — gộp hai tháng vào một tờ thì không còn là tờ lịch treo
 * tường nữa, mà đó mới là thứ làm người xem nhìn ra ngay chỗ nghỉ nằm ở đâu.
 */
export function ManHinhBaoCaoTho({
  dungBaoCao,
  tuNgayDau,
  denNgayDau,
  suaUng,
  suaNgay,
  onDong,
}: Props) {
  const [tuNgay, datTuNgay] = useState(tuNgayDau);
  const [denNgay, datDenNgay] = useState(denNgayDau);
  const [dangChon, datDangChon] = useState<'tu' | 'den' | null>(null);
  const [dangSuaUng, datDangSuaUng] = useState<UngTien | null>(null);
  /** Ngày đang mở hộp sửa; null là chưa mở. */
  const [ngaySua, datNgaySua] = useState<string | null>(null);

  const baoCao = dungBaoCao(tuNgay, denNgay);
  if (baoCao === null) {
    return null;
  }

  // Tờ lịch và hộp chọn ngày làm việc theo tháng, lấy tháng của ngày cuối khoảng.
  const { nam, thang } = Ngay.tach(denNgay);
  const dauThang = Ngay.ghep(nam, thang, 1);
  const cuoiThang = Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang));

  const { tho, ngayCongs, ngayNghis, ungTiens } = baoCao;
  const laCaKy = tuNgay === tuNgayDau && denNgay === denNgayDau;
  const cacThang = Ngay.cacThangTrongKhoang(tuNgay, denNgay);

  // Khoảng mở ra lúc đầu có đúng bằng trọn một tháng dương lịch không — quyết định màn hình
  // gọi nó là "kỳ" hay "tháng".
  const dau = Ngay.tach(tuNgayDau);
  const laTronThang =
    tuNgayDau === Ngay.ghep(dau.nam, dau.thang, 1) &&
    denNgayDau === Ngay.ghep(dau.nam, dau.thang, Ngay.soNgayTrongThang(dau.nam, dau.thang));
  const khoangSan = khoangSanCua(laTronThang);
  const tenTronKhoang = laTronThang ? 'Cả tháng' : 'Cả kỳ';
  /** Cách gọi khoảng mở ra lúc đầu trong câu văn — đi cùng `tenTronKhoang` trên đầu trang. */
  const tenKhoangGoc = laTronThang ? 'Tháng này' : 'Kỳ này';

  function khoangCua(ma: string): [string, string] {
    switch (ma) {
      case 'thang':
        return [dauThang, cuoiThang];
      case 'dau':
        return [dauThang, Ngay.ghep(nam, thang, 15)];
      case 'cuoi':
        return [Ngay.ghep(nam, thang, 16), cuoiThang];
      default:
        return [tuNgayDau, denNgayDau];
    }
  }

  /** Rỗng khi khoảng đang xem không trùng khoảng nào có sẵn — lúc ấy không viên nào sáng. */
  const khoangDangDung =
    khoangSan.find(({ ma }) => {
      const [tu, den] = khoangCua(ma);
      return tu === tuNgay && den === denNgay;
    })?.ma ?? '';

  function datKhoang(tu: string, den: string) {
    datTuNgay(tu);
    datDenNgay(den);
  }

  /**
   * Chọn ngày đầu muộn hơn ngày cuối thì kéo luôn đầu kia theo, chứ không khoá ngày lại
   * cho bấm không ăn. Người dùng chỉ thấy một khoảng hợp lệ, không bao giờ gặp ngõ cụt.
   */
  function chonNgay(ngay: string) {
    if (dangChon === 'tu') {
      datTuNgay(ngay);
      if (ngay > denNgay) {
        datDenNgay(ngay);
      }
    } else {
      datDenNgay(ngay);
      if (ngay < tuNgay) {
        datTuNgay(ngay);
      }
    }

    datDangChon(null);
  }

  return (
    <ManHinhDe onDong={onDong}>
      <DauTrang
        tieuDe={tho.ten}
        phu={
          laCaKy
            ? `${tenTronKhoang} · ${Ngay.khoangGon(tuNgay, denNgay)}`
            : Ngay.khoangGon(tuNgay, denNgay)
        }
        onLui={onDong}
      />

      {/*
        Hai nút ngày mở tờ lịch, bốn khoảng hay dùng ở thanh ngay dưới. Có mấy khoảng sẵn
        vì kỳ nửa tháng là chuyện lặp đi lặp lại — bắt chọn tay hai lần mỗi tháng thì phí.
      */}
      <View style={kieu.hangLoc}>
        <View style={kieu.dongNgay}>
          <NutNgay nhan="Từ" ngay={tuNgay} onPress={() => datDangChon('tu')} />
          <Feather name="arrow-right" size={15} color={Mau.xam} />
          <NutNgay nhan="Đến" ngay={denNgay} onPress={() => datDangChon('den')} />
        </View>

        {/*
          Bốn nút viền rời nhau ở bản cũ giờ thành một thanh phân đoạn có viên trượt như
          bản thiết kế: nhìn ra ngay đang ở khoảng nào, thay vì phải soi nút nào đổi màu.
          Lọc bằng tay hai đầu ngày thì không viên nào sáng — đúng vậy, khoảng ấy không
          phải một trong bốn khoảng có sẵn.
        */}
        <ThanhDoan
          cac={khoangSan}
          dangChon={khoangDangDung}
          onChon={(ma) => datKhoang(...khoangCua(ma))}
        />
      </View>

      <ScrollView contentContainerStyle={kieu.trong}>
        {/*
          Bốn con số tóm tắt xếp thành lưới 2×2, mỗi ô một màu — mảnh dễ nhận nhất của
          bản thiết kế. Trước đây là bốn dòng nhãn–số trong một thẻ trắng: đọc thì ra,
          nhưng phải rà mắt từng dòng. Lưới thì con số to nằm giữa ô, nhìn một cái là hết.

          Ô *Đã ứng* hiện cả khi bằng 0 (khác bản cũ) để lưới lúc nào cũng đủ bốn ô: lưới
          2×2 khuyết một góc nhìn như thiếu chỗ chứ không như "không có gì".
        */}
        <View style={kieu.luoiO}>
          <HangO>
            <TheSo nhan="Số công" so={`${Ngay.soCong(baoCao.tongCong)} công`} mau="chinh" />
            <TheSo nhan="Tiền công" so={Ngay.tien(baoCao.tienCong)} mau="ngoc" />
          </HangO>
          <HangO>
            {/* Chưa ứng lần nào thì ghi "0 đ", không ghi "−0 đ" — dấu trừ trước số 0 là vô nghĩa. */}
            <TheSo
              nhan="Đã ứng"
              so={baoCao.daUng > 0 ? Ngay.tienTru(baoCao.daUng) : Ngay.tien(0)}
              mau="do"
            />
            {/* Ứng quá tiền công thì cả ô đổi sang đỏ, không chỉ riêng con số. */}
            <TheSo
              nhan="Còn phải trả"
              so={Ngay.tien(baoCao.conLai)}
              mau={baoCao.conLai < 0 ? 'do' : 'xanhLa'}
            />
          </HangO>
        </View>

        {/*
          Kỳ trước trả thiếu thì phần thiếu đứng thành một dòng riêng dưới lưới, không lẫn
          vào tiền công kỳ này — nhìn ra ngay đâu là công mới làm, đâu là nợ cũ mang sang.
        */}
        {baoCao.noKyTruoc !== 0 && (
          <View style={kieu.theNo}>
            <Dong
              nhan={baoCao.noKyTruoc > 0 ? 'Nợ kỳ trước' : 'Kỳ trước trả dư'}
              gia={
                baoCao.noKyTruoc > 0
                  ? Ngay.tien(baoCao.noKyTruoc)
                  : Ngay.tienTru(baoCao.noKyTruoc)
              }
              dam
            />
          </View>
        )}

        {/*
          Cả đi làm lẫn nghỉ gộp vào một tờ lịch. Ngày nghỉ chỉ đếm phần đã trôi qua và
          từ lúc thợ vào làm — ngày mai chưa tới thì không phải nghỉ, ô đó để trắng.
        */}
        <Text style={kieu.tieuDeMuc}>Lịch đi làm</Text>
        <View style={kieu.the}>
          {/*
            Vẫn vẽ trọn tháng dù đang lọc hẹp: ngày ngoài khoảng thành ô trắng, nhìn ra
            ngay phần nào đang tính. Cắt tờ lịch cho vừa khoảng thì mất chỗ dựa của mắt.
          */}
          {cacThang.map(({ nam: namLich, thang: thangLich }, thuTu) => {
            // So bằng đoạn đầu "2026-08" của chuỗi ngày: so mỗi số tháng thì kỳ vắt qua
            // đúng một năm sẽ trộn tháng 8 năm nay với tháng 8 năm ngoái.
            const moc = Ngay.ghep(namLich, thangLich, 1).slice(0, 7);
            return (
              <View key={moc} style={thuTu > 0 && kieu.lichSau}>
                {/* Kỳ vắt qua nhiều tháng mới ghi tên tháng; một tháng thì đã có trên đầu rồi. */}
                {cacThang.length > 1 && (
                  <Text style={kieu.chuThangLich}>
                    Tháng {thangLich}/{namLich}
                  </Text>
                )}
                <LichCong
                  nam={namLich}
                  thang={thangLich}
                  ngayCongs={ngayCongs.filter((d) => d.ngay.slice(0, 7) === moc)}
                  ngayNghis={ngayNghis.filter((n) => n.slice(0, 7) === moc)}
                  onChonNgay={suaNgay && datNgaySua}
                />
              </View>
            );
          })}
          {ngayCongs.length === 0 && (
            <Text style={kieu.chuTrong}>
              {laCaKy
                ? `${tenKhoangGoc} chưa có ngày công nào.`
                : 'Khoảng này chưa có ngày công nào.'}
            </Text>
          )}

          {/*
            Một dòng mách nhỏ dưới tờ lịch, cùng lý do với dòng dưới danh sách ứng: chạm
            vào ô ngày để sửa là chuyện không nhìn ra được. Kỳ đã chốt thì không có dòng
            này vì cũng không sửa được.
          */}
          {suaNgay !== undefined && (
            <Text style={kieu.chuMach}>Chạm vào một ngày để chấm hoặc sửa ngày ấy.</Text>
          )}
        </View>

        <Text style={kieu.tieuDeMuc}>
          Ứng tiền{ungTiens.length > 0 ? ` (${ungTiens.length} lần)` : ''}
        </Text>
        <View style={[kieu.the, kieu.theCuoi]}>
          {ungTiens.length === 0 ? (
            <Text style={kieu.chuTrong}>
              {laCaKy
                ? `${tenKhoangGoc} chưa ứng lần nào.`
                : 'Khoảng này chưa ứng lần nào.'}
            </Text>
          ) : (
            <>
              {ungTiens.map((ung) => (
                <DongUng key={ung.id} ung={ung} onPress={suaUng && (() => datDangSuaUng(ung))} />
              ))}

              {/*
                Một dòng mách nhỏ dưới danh sách: chạm vào dòng ứng để sửa là chuyện
                không nhìn ra được, mà cái hộp ấy lại là đường duy nhất chữa số tiền gõ
                nhầm. Kỳ đã chốt thì không có dòng này vì cũng không sửa được.
              */}
              {suaUng !== undefined && (
                <Text style={kieu.chuMach}>Chạm vào một dòng để sửa hoặc xoá.</Text>
              )}
            </>
          )}
        </View>
      </ScrollView>

      {dangChon !== null && (
        <HopChonNgay
          tieuDe={dangChon === 'tu' ? 'Tính từ ngày nào?' : 'Tính đến ngày nào?'}
          nam={nam}
          thang={thang}
          ngayDangChon={dangChon === 'tu' ? tuNgay : denNgay}
          onChon={chonNgay}
          onDong={() => datDangChon(null)}
        />
      )}

      {suaNgay !== undefined && ngaySua !== null && (
        <HopSuaNgay
          ngay={ngaySua}
          tenTho={tho.ten}
          sua={suaNgay}
          onDong={() => datNgaySua(null)}
        />
      )}

      {suaUng !== undefined && dangSuaUng !== null && (
        <HopSuaUng
          ung={dangSuaUng}
          tenTho={tho.ten}
          onGhi={(ngay, soTien, ghiChu) => {
            suaUng.ghi(dangSuaUng.id, ngay, soTien, ghiChu);
            datDangSuaUng(null);
          }}
          onXoa={() => {
            suaUng.xoa(dangSuaUng.id);
            datDangSuaUng(null);
          }}
          onDong={() => datDangSuaUng(null)}
        />
      )}
    </ManHinhDe>
  );
}

/**
 * Một dòng trong lịch sử ứng: ngày, ghi chú, số tiền.
 *
 * Không có `onPress` thì vẽ ra một `View` trơ chứ không phải `Pressable` bấm không ăn —
 * kỳ đã chốt thì dòng ấy không được có cả cái vẻ chạm được.
 */
function DongUng({ ung, onPress }: { ung: UngTien; onPress?: () => void }) {
  const noiDung = (
    <>
      <View style={kieu.coNgay}>
        <Text style={kieu.chuNgay}>{ngayNgan(ung.ngay)}</Text>
        <Text style={kieu.chuThu}>{Ngay.thu(ung.ngay)}</Text>
      </View>
      <Text style={kieu.chuGhiChu} numberOfLines={1}>
        {ung.ghiChu}
      </Text>
      <Text style={kieu.chuTienUng}>{Ngay.tienTru(ung.soTien)}</Text>
    </>
  );

  if (onPress === undefined) {
    return <View style={kieu.dongUng}>{noiDung}</View>;
  }

  return (
    <Pressable
      style={kieu.dongUngSua}
      onPress={onPress}
      accessibilityLabel={`Ứng ${Ngay.tien(ung.soTien)} ngày ${Ngay.ngayGon(ung.ngay)}, chạm để sửa`}
    >
      {noiDung}
      <Feather name="edit-3" size={14} color={Mau.xam} />
    </Pressable>
  );
}

/** Nút mở tờ lịch. Ngày hiện luôn trên nút để khỏi phải mở ra mới biết đang lọc từ đâu. */
function NutNgay({ nhan, ngay, onPress }: { nhan: string; ngay: string; onPress: () => void }) {
  return (
    <Pressable
      style={kieu.nutNgay}
      onPress={onPress}
      accessibilityLabel={`${nhan} ngày ${ngayNgan(ngay)}, chạm để đổi`}
    >
      <Text style={kieu.chuNhanNgay}>{nhan}</Text>
      <Text style={kieu.chuNgayLoc}>{ngayNgan(ngay)}</Text>
      <Feather name="calendar" size={14} color={Mau.chinh} />
    </Pressable>
  );
}

function Dong({ nhan, gia, mau, dam }: { nhan: string; gia: string; mau?: string; dam?: boolean }) {
  return (
    <View style={kieu.dongSo}>
      <Text style={[kieu.chuNhan, dam === true && kieu.chuNhanDam]}>{nhan}</Text>
      <Text
        style={[kieu.chuGia, dam === true && kieu.chuGiaDam, mau !== undefined && { color: mau }]}
      >
        {gia}
      </Text>
    </View>
  );
}

const kieu = StyleSheet.create({
  hangLoc: { paddingHorizontal: 16, paddingBottom: 10, gap: 10 },
  dongNgay: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 10 },
  // Nút ngày là thẻ trắng nổi bóng, cùng dáng với nút icon ở đầu trang.
  nutNgay: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 7,
    minHeight: 44,
    paddingVertical: 8,
    paddingHorizontal: 8,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    ...Bong.the,
  },
  chuNhanNgay: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNgayLoc: { fontSize: Co.chuThuong, fontFamily: PhongChu.dam, color: Mau.chu },

  trong: { padding: 16, paddingTop: 4, paddingBottom: 24 },
  luoiO: { gap: 11 },
  theNo: { ...theTrang, marginTop: 12 },
  tieuDeMuc: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xam,
    marginTop: 16,
    marginBottom: 6,
    marginLeft: 2,
  },
  the: { ...theTrang, gap: 6 },
  theCuoi: { marginBottom: 8 },

  lichSau: { marginTop: 14, paddingTop: 12, borderTopWidth: 1, borderTopColor: Mau.vien },
  chuThangLich: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    marginBottom: 8,
  },

  dongSo: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  chuNhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNhanDam: { fontFamily: PhongChu.vua, color: Mau.chu },
  chuGia: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuGiaDam: { fontSize: Co.chuTen, fontFamily: PhongChu.dam },

  dongUng: { flexDirection: 'row', alignItems: 'center', gap: 10, paddingVertical: 5 },
  // Dòng chạm được thì cao hơn một chút cho vừa đầu ngón tay, và có icon bút ở cuối.
  dongUngSua: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    minHeight: 44,
    paddingVertical: 5,
  },
  coNgay: { width: 62 },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  chuThu: { fontSize: 11, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuGhiChu: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTienUng: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.do },

  chuTrong: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuMach: {
    fontSize: Co.chuNho,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    marginTop: 4,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
  },
});
