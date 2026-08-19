import { useState } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';

import * as Ngay from '../nghiepvu/ngayViet';
import { docTien } from '../nghiepvu/nhapSo';
import { HopDay } from './HopDay';
import { ONhap } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  tieuDe: string;
  moTa: string;
  goiY: string;
  /** Điền sẵn khi mở, dùng lúc sửa một số đã có. */
  giaTriDau?: string;
  /** Cách đọc chữ người dùng gõ thành số. Mặc định là đọc tiền. */
  doc?: (chu: string) => number | null;
  /** Cách đọc ngược số ra chữ để người dùng soi lại. Mặc định là "1.500.000 đ". */
  hienLai?: (so: number) => string;
  banPhim?: 'number-pad' | 'decimal-pad';
  /** Trả về lời nhắc nếu số đọc được nhưng không dùng được, ví dụ lớn quá. */
  loi?: (so: number) => string | null;
  /** Ô chữ phụ dưới ô số, ví dụ ghi chú cho lần ứng tiền. Không bắt buộc điền. */
  oChu?: { nhan: string; goiY: string };
  onGhi: (so: number, chuThem: string) => void;
  onDong: () => void;
}

/**
 * Hộp nhập một con số. Tự vẽ vì Alert.prompt chỉ có trên iOS, và vì hộp mặc định
 * không chỉnh được cỡ chữ.
 *
 * Phần nền mờ, tay nắm và chuyện đẩy hộp lên khi bàn phím mở nằm hết trong
 * [HopDay](./HopDay.tsx) — dùng chung với hộp chọn và hộp chọn ngày.
 */
export function HopNhapSo({
  tieuDe,
  moTa,
  goiY,
  giaTriDau = '',
  doc = docTien,
  hienLai = Ngay.tien,
  banPhim = 'number-pad',
  loi = () => null,
  oChu,
  onGhi,
  onDong,
}: Props) {
  const [chu, datChu] = useState(giaTriDau);
  const [chuThem, datChuThem] = useState('');

  const so = doc(chu);
  const loiHienTai = so === null ? null : loi(so);
  const ghiDuoc = so !== null && so > 0 && loiHienTai === null;

  return (
    <HopDay onDong={onDong}>
      <Text style={kieu.tieuDe}>{tieuDe}</Text>

      {/*
        Câu hỏi ("Thợ ứng bao nhiêu?") làm nhãn nằm trong ô, không còn là một dòng chữ
        riêng phía trên — mắt đọc câu hỏi rồi gõ luôn, không phải nhảy qua một khoảng trống.
      */}
      <ONhap
        nhan={moTa}
        coSo
        value={chu}
        onChangeText={datChu}
        placeholder={goiY}
        accessibilityLabel={goiY}
        keyboardType={banPhim}
        autoFocus
      />

      {/*
        Đọc lại số vừa gõ ("1.500.000 đ") để bắt lỗi thừa hoặc thiếu số 0.
        Gõ quá tay thì chỗ này thành lời nhắc màu đỏ.
      */}
      <Text style={[kieu.docLai, loiHienTai !== null && kieu.docLaiLoi]}>
        {loiHienTai ?? (so !== null && so > 0 ? hienLai(so) : ' ')}
      </Text>

      {/*
        Ô chữ để trắng cũng ghi được — bắt điền thì lần nào vội cũng phải gõ bừa
        một chữ cho xong.
      */}
      {oChu !== undefined && (
        <ONhap
          nhan={oChu.nhan}
          value={chuThem}
          onChangeText={datChuThem}
          placeholder={oChu.goiY}
          maxLength={60}
        />
      )}

      <Pressable
        style={[kieu.nut, ghiDuoc ? kieu.nutBat : kieu.nutTat]}
        onPress={() => ghiDuoc && onGhi(so, chuThem.trim())}
        disabled={!ghiDuoc}
      >
        <Text style={[kieu.chuNut, { color: ghiDuoc ? Mau.trang : Mau.xam }]}>Ghi</Text>
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
  // Căn trái, thẳng cột với ô nhập ngay trên nó.
  docLai: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.vua,
    color: Mau.xanhLa,
    minHeight: 18,
    marginLeft: 2,
  },
  docLaiLoi: { color: Mau.do },
  nut: {
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
  nutThoi: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
