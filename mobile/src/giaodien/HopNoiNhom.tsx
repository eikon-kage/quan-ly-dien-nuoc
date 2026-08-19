import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { Vai } from '../nghiepvu/soCong';
import { DieuKhienNhom } from './dungSupabase';
import { HopDay } from './HopDay';
import { ONhap } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

/**
 * Nối máy này vào nhóm chấm công.
 *
 * Hai vai hai kiểu đăng nhập, và đó là chủ ý chứ không phải làm cho khác nhau:
 *
 *   Chủ — email và mật khẩu. Tài khoản này nắm sổ của cả nhóm, không được để nó chỉ tồn tại
 *         trong một cái điện thoại; mất máy thì đăng nhập lại trên máy mới là còn nguyên.
 *   Thợ — bấm một nút, xong. Không email, không mật khẩu, không mã OTP. Thợ ở công trường,
 *         mỗi thứ phải nhớ thêm là một lý do để họ thôi không dùng app nữa.
 */

interface Props {
  vai: Vai;
  dieuKhien: DieuKhienNhom;
  onDong: () => void;
}

export function HopNoiNhom({ vai, dieuKhien, onDong }: Props) {
  const { trangThai, noiAnDanh, noiEmail, taoTaiKhoan, ngat } = dieuKhien;
  const { hoTro, taiKhoan, dangChay, loi, nhac } = trangThai;

  const [email, datEmail] = useState('');
  const [matKhau, datMatKhau] = useState('');

  return (
    <HopDay onDong={onDong}>
      <Text style={kieu.tieuDe}>Nhóm chấm công</Text>

      {!hoTro ? (
        <Text style={kieu.chuChan}>
          Bản app này chưa được điền địa chỉ nhóm. Cần dựng lại app sau khi điền cấu hình
          Supabase — xem docs/chamcong-doi-chieu.md.
        </Text>
      ) : taiKhoan !== null ? (
        <>
          <View style={kieu.dongTrangThai}>
            <Feather name="check-circle" size={19} color={Mau.xanhLa} />
            <View style={kieu.giuaDong}>
              <Text style={kieu.chuNhan}>Đã nối</Text>
              <Text style={kieu.chuPhu}>
                {taiKhoan.email ?? 'Tài khoản ẩn danh của máy này'}
              </Text>
            </View>
          </View>

          <Pressable style={kieu.nutPhu} onPress={ngat} disabled={dangChay}>
            <Feather name="log-out" size={16} color={Mau.do} />
            <Text style={kieu.chuNutPhu}>Ngắt khỏi nhóm</Text>
          </Pressable>

          <Text style={kieu.chuChan}>
            {vai === 'tho'
              ? 'Ngắt thì máy này thôi gửi sổ cho chủ. Những buổi đã chấm vẫn còn trong máy.'
              : 'Ngắt thì máy này thôi nhận sổ của thợ. Sổ của cửa hàng vẫn còn trong máy.'}
          </Text>
        </>
      ) : vai === 'tho' ? (
        <>
          <Text style={kieu.chuChan}>
            Bấm nối là xong — không cần email hay mật khẩu. Máy này tự có một tài khoản riêng
            để gửi sổ công cho chủ.
          </Text>

          <Pressable style={kieu.nutChinh} onPress={noiAnDanh} disabled={dangChay}>
            {dangChay ? (
              <ActivityIndicator color={Mau.trang} />
            ) : (
              <Feather name="link" size={17} color={Mau.trang} />
            )}
            <Text style={kieu.chuNutChinh}>{dangChay ? 'Đang nối…' : 'Nối vào nhóm'}</Text>
          </Pressable>
        </>
      ) : (
        <>
          <ONhap
            nhan="Email của chủ"
            value={email}
            onChangeText={datEmail}
            placeholder="chu@cuahang.vn"
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="email-address"
          />
          <ONhap
            nhan="Mật khẩu"
            value={matKhau}
            onChangeText={datMatKhau}
            placeholder="ít nhất 6 ký tự"
            autoCapitalize="none"
            autoCorrect={false}
            secureTextEntry
          />

          <Pressable
            style={kieu.nutChinh}
            onPress={() => noiEmail(email, matKhau)}
            disabled={dangChay}
          >
            {dangChay ? (
              <ActivityIndicator color={Mau.trang} />
            ) : (
              <Feather name="log-in" size={17} color={Mau.trang} />
            )}
            <Text style={kieu.chuNutChinh}>{dangChay ? 'Đang nối…' : 'Đăng nhập'}</Text>
          </Pressable>

          <Pressable
            style={kieu.nutPhuXanh}
            onPress={() => taoTaiKhoan(email, matKhau)}
            disabled={dangChay}
          >
            <Feather name="user-plus" size={16} color={Mau.chinh} />
            <Text style={kieu.chuNutPhuXanh}>Lần đầu — tạo tài khoản</Text>
          </Pressable>
        </>
      )}

      {loi !== null && <Text style={kieu.chuLoi}>{loi}</Text>}
      {nhac !== null && <Text style={kieu.chuNhac}>{nhac}</Text>}
    </HopDay>
  );
}

const kieu = StyleSheet.create({
  tieuDe: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu, marginBottom: 4 },

  dongTrangThai: { flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 4 },
  giuaDong: { flex: 1, gap: 2 },
  chuNhan: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  nutChinh: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  chuNutChinh: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.trang },

  nutPhu: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNutNho,
    paddingVertical: 8,
  },
  chuNutPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.do },

  nutPhuXanh: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNutNho,
    paddingVertical: 8,
  },
  chuNutPhuXanh: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.chinh },

  chuChan: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuLoi: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.do },
  chuNhac: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xanhLa },
});
