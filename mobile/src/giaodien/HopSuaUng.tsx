import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { UngTien } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { docTien } from '../nghiepvu/nhapSo';
import { HopChonNgay } from './HopChonNgay';
import { HopDay } from './HopDay';
import { hoi } from './hopThoai';
import { ONhap } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  ung: UngTien;
  /** Tên thợ, chỉ để ghi lên đầu hộp — sửa ứng không đổi được thợ. */
  tenTho: string;
  onGhi: (ngay: string, soTien: number, ghiChu: string) => void;
  onXoa: () => void;
  onDong: () => void;
}

/**
 * Sửa lại một lần ứng đã ghi, hoặc xoá hẳn nó đi.
 *
 * Sửa được cả **ngày** chứ không chỉ số tiền: lúc thêm, ứng luôn lấy ngày hôm nay, nên
 * nhớ ra hôm sau mới ghi là ngày đã lệch — mà kỳ nửa tháng thì lệch một ngày có khi rơi
 * sang kỳ khác. Đây là đường duy nhất chữa lại chỗ ấy.
 *
 * Tự vẽ chứ không dùng [HopNhapSo](./HopNhapSo.tsx): hộp kia nhập một con số cho một việc
 * mới, còn ở đây có tới ba thứ điền sẵn cộng thêm nút xoá.
 *
 * Tờ lịch chọn ngày **thay chỗ** hộp này chứ không chồng lên trên: hộp này đã nằm trong
 * modal của màn hình báo cáo rồi, mở thêm một modal thứ ba nữa trên iOS là chuyện hên xui.
 * Chữ đang gõ không mất vì hộp vẫn còn đấy, chỉ có phần vẽ ra là đổi.
 *
 * Tờ lịch ấy **lùi / tới tháng được**. Ứng hôm 30 mà mùng 2 mới nhớ ra để ghi thì ngày
 * đúng nằm ở tháng trước — khoá trong đúng một tháng thì chỗ cần sửa nhất lại không với
 * tới được.
 */
export function HopSuaUng({ ung, tenTho, onGhi, onXoa, onDong }: Props) {
  const [ngay, datNgay] = useState(ung.ngay);
  const [chu, datChu] = useState(String(ung.soTien));
  const [ghiChu, datGhiChu] = useState(ung.ghiChu);
  /** Tháng đang mở trên tờ lịch. `null` là chưa mở tờ lịch. */
  const [mocLich, datMocLich] = useState<string | null>(null);

  const soTien = docTien(chu);
  const ghiDuoc = soTien !== null && soTien > 0;

  /**
   * Lùi / tới một tháng trên tờ lịch. Nhảy qua hẳn mép tháng đang xem chứ không cộng 30
   * ngày: cộng ngày thì tháng thiếu tháng thừa sẽ có lúc nhảy vọt qua cả một tháng.
   */
  function doiThangLich(buoc: -1 | 1) {
    if (mocLich === null) {
      return;
    }

    const { nam, thang } = Ngay.tach(mocLich);
    datMocLich(
      buoc === -1
        ? Ngay.congNgay(Ngay.ghep(nam, thang, 1), -1)
        : Ngay.congNgay(Ngay.ghep(nam, thang, Ngay.soNgayTrongThang(nam, thang)), 1),
    );
  }

  function xoa() {
    hoi('Xoá lần ứng này?', `${Ngay.tien(ung.soTien)} ngày ${Ngay.ngayGon(ung.ngay)}.`, [
      { text: 'Thôi', style: 'cancel' },
      { text: 'Xoá', style: 'destructive', onPress: onXoa },
    ]);
  }

  if (mocLich !== null) {
    const { nam, thang } = Ngay.tach(mocLich);
    return (
      <HopChonNgay
        tieuDe="Ứng hôm nào?"
        nam={nam}
        thang={thang}
        ngayDangChon={ngay}
        // Có hai mũi tên lùi / tới tháng: ứng cuối tháng mà mấy hôm sau mới nhớ ra để ghi
        // thì ngày đúng nằm ở tháng trước, khoá trong một tháng là không với tới được.
        onDoiThang={doiThangLich}
        onChon={(chon) => {
          datNgay(chon);
          datMocLich(null);
        }}
        onDong={() => datMocLich(null)}
      />
    );
  }

  return (
    <HopDay onDong={onDong}>
      <Text style={kieu.tieuDe}>{tenTho} — sửa lần ứng</Text>

      <Pressable
        style={kieu.dongNgay}
        onPress={() => datMocLich(ngay)}
        accessibilityLabel={`Ứng ngày ${Ngay.ngayGon(ngay)}, chạm để đổi`}
      >
        <Feather name="calendar" size={15} color={Mau.chinh} />
        <Text style={kieu.chuNhanNgay}>Ngày ứng</Text>
        <Text style={kieu.chuNgay}>{Ngay.ngayGon(ngay)}</Text>
        <Feather name="chevron-right" size={15} color={Mau.xam} />
      </Pressable>

      <ONhap
        nhan="Thợ ứng bao nhiêu?"
        coSo
        value={chu}
        onChangeText={datChu}
        placeholder="Ví dụ 500000"
        accessibilityLabel="Số tiền ứng"
        keyboardType="number-pad"
      />

      {/* Đọc lại số vừa gõ để bắt lỗi thừa hoặc thiếu số 0, y như lúc thêm. */}
      <Text style={kieu.docLai}>{ghiDuoc ? Ngay.tien(soTien) : ' '}</Text>

      <ONhap
        nhan="Ghi chú (không bắt buộc)"
        value={ghiChu}
        onChangeText={datGhiChu}
        placeholder="Ví dụ: ứng đổ xăng"
        maxLength={60}
      />

      <Pressable
        style={[kieu.nut, ghiDuoc ? kieu.nutBat : kieu.nutTat]}
        onPress={() => ghiDuoc && onGhi(ngay, soTien, ghiChu.trim())}
        disabled={!ghiDuoc}
      >
        <Text style={[kieu.chuNut, { color: ghiDuoc ? Mau.trang : Mau.xam }]}>Ghi</Text>
      </Pressable>

      {/*
        Nút xoá viền đỏ chứ không nền đỏ, và còn hỏi lại một câu nữa: nó nằm ngay dưới
        nút Ghi, bấm trượt một dòng là mất luôn lần ứng.
      */}
      <Pressable style={[kieu.nut, kieu.nutXoa]} onPress={xoa}>
        <Feather name="trash-2" size={15} color={Mau.do} />
        <Text style={[kieu.chuNut, { color: Mau.do }]}>Xoá lần ứng này</Text>
      </Pressable>

      <Pressable style={[kieu.nut, kieu.nutThoi]} onPress={onDong}>
        <Text style={[kieu.chuNut, { color: Mau.xam }]}>Thôi</Text>
      </Pressable>
    </HopDay>
  );
}

const kieu = StyleSheet.create({
  tieuDe: {
    fontSize: Co.chuTieuDe,
    fontFamily: PhongChu.dam,
    color: Mau.chu,
    paddingBottom: 2,
  },

  dongNgay: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
    backgroundColor: Mau.nen,
  },
  chuNhanNgay: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNgay: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },

  // Căn trái, thẳng cột với ô nhập ngay trên nó.
  docLai: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xanhLa,
    minHeight: 18,
    marginLeft: 2,
  },

  nut: {
    flexDirection: 'row',
    gap: 7,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  nutBat: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  nutTat: { backgroundColor: Mau.nen, borderColor: Mau.vien },
  nutXoa: { backgroundColor: Mau.doNhat, borderColor: Mau.do },
  nutThoi: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
