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
 *   Chủ — email và mật khẩu, **bắt buộc**. Tài khoản này nắm nhóm của cả cửa hàng, không được
 *         để nó chỉ tồn tại trong một cái điện thoại: mất máy thì đăng nhập lại trên máy mới
 *         là nhóm và sổ thợ còn nguyên.
 *   Thợ — một cái mã mời, xong. Không email, không mật khẩu, không mã OTP. Thợ ở công trường,
 *         mỗi thứ phải nhớ thêm là một lý do để họ thôi không dùng app nữa.
 *
 * **Máy thợ không vào nhóm từ đây.** Nó vào bằng mã mời trong hộp *Máy của thợ*, vì cùng một
 * lần dán mã ấy còn đặt luôn vai máy và `thoId`. Tách ra hai chỗ thì thợ vào được nhóm mà máy
 * vẫn là máy chủ — đăng nhập xong vẫn không gửi được sổ nào. Ở đây máy thợ chỉ xem trạng thái
 * và ngắt.
 */

interface Props {
  vai: Vai;
  dieuKhien: DieuKhienNhom;
  onDong: () => void;
}

export function HopNoiNhom({ vai, dieuKhien, onDong }: Props) {
  const { trangThai, noiEmail, taoTaiKhoan, lapNhom, ngat } = dieuKhien;
  const { hoTro, taiKhoan, thanhVien, dangChay, loi, nhac } = trangThai;

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
            <Feather
              name={thanhVien !== null ? 'check-circle' : 'alert-circle'}
              size={19}
              color={thanhVien !== null ? Mau.xanhLa : Mau.do}
            />
            <View style={kieu.giuaDong}>
              <Text style={kieu.chuNhan}>
                {thanhVien !== null ? 'Đã nối' : 'Đã đăng nhập, chưa vào nhóm'}
              </Text>
              <Text style={kieu.chuPhu}>
                {taiKhoan.email ?? 'Tài khoản ẩn danh của máy này'}
              </Text>
            </View>
          </View>

          {/*
            Đăng nhập xong mà chưa vào nhóm là có thật: database chưa dựng bảng, hoặc mất mạng
            đúng lúc lập nhóm. Không có nút thử lại thì người dùng mắc cạn — đăng nhập rồi nên
            nút Nối biến mất, mà nhóm thì vẫn chưa có.
          */}
          {thanhVien === null &&
            (vai === 'chu' ? (
              <Pressable style={kieu.nutChinh} onPress={lapNhom} disabled={dangChay}>
                {dangChay ? (
                  <ActivityIndicator color={Mau.trang} />
                ) : (
                  <Feather name="refresh-cw" size={17} color={Mau.trang} />
                )}
                <Text style={kieu.chuNutChinh}>Lập nhóm, thử lại</Text>
              </Pressable>
            ) : (
              /*
                Không để một cái nút bấm không ăn ở đây như bản trước ("Đợi mã mời của chủ"):
                nút bấm không ăn thì người dùng bấm mãi rồi tưởng app hỏng. Chỉ đường sang
                đúng chỗ dán mã.
              */
              <Text style={kieu.chuNhac}>
                Chưa vào nhóm. Xin chủ phát mã mời, rồi dán vào mục{' '}
                <Text style={kieu.chuDam}>Máy của thợ · đổi lại</Text> ở đáy màn hình.
              </Text>
            ))}

          {/*
            Đường ra, và với máy thợ thì đây **là** cái nút đăng xuất — gọi thẳng là *thoát
            nhóm* chứ không gọi "ngắt": thợ tìm chữ đăng xuất hay chữ thoát, không tìm một từ
            kỹ thuật. Vẫn để nút phụ viền đỏ, không phải nút chính: cả tháng không ai bấm.
          */}
          <Pressable style={kieu.nutPhu} onPress={ngat} disabled={dangChay}>
            <Feather name="log-out" size={16} color={Mau.do} />
            <Text style={kieu.chuNutPhu}>
              {vai === 'tho' ? 'Thoát nhóm, đăng xuất máy này' : 'Ngắt khỏi nhóm'}
            </Text>
          </Pressable>

          <Text style={kieu.chuChan}>
            {vai === 'tho'
              ? 'Thoát thì máy này thôi gửi sổ cho chủ, nhưng những buổi đã chấm vẫn còn ' +
                'nguyên trong máy. Muốn vào lại phải xin chủ một mã mời mới — mã cũ dùng một ' +
                'lần là hết.'
              : 'Ngắt thì máy này thôi nhận sổ của thợ. Sổ của cửa hàng vẫn còn trong máy.'}
          </Text>
        </>
      ) : vai === 'tho' ? (
        <Text style={kieu.chuChan}>
          Máy thợ vào nhóm bằng mã mời của chủ — không cần email hay mật khẩu. Dán mã ở mục{' '}
          <Text style={kieu.chuDam}>Máy của thợ · đổi lại</Text> ở đáy màn hình; một lần dán mã
          là xong cả việc vào nhóm.
        </Text>
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

          {/*
            **Đừng thêm lại đường "nối nhanh, không cần email" ở đây.** Bản trước có, và cái
            giá của nó rơi đúng vào chỗ đau nhất: tài khoản ẩn danh chỉ sống trong một cái
            điện thoại, nên chủ mất máy là mất cả nhóm — mọi thợ trong nhóm phải nhận mã mời
            lại từ một nhóm mới, mà sổ họ đã gửi lên thì nằm ở nhóm cũ không ai vào được nữa.

            Máy thợ thì vẫn ẩn danh, và đúng: sổ thật của thợ nằm trong máy họ, mất máy chỉ
            việc dán mã mời mới.
          */}
          <Text style={kieu.chuChan}>
            Tài khoản này nắm nhóm của cả cửa hàng, nên phải là email — mất máy thì đăng nhập
            lại trên máy mới là nhóm và sổ thợ còn nguyên.
          </Text>
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
  chuDam: { fontFamily: PhongChu.vua, color: Mau.chu },
  chuLoi: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.do },
  chuNhac: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xanhLa },
});
