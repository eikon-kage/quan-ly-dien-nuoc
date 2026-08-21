import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { ActivityIndicator, Alert, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { BanTaiKhoan } from '../nghiepvu/saoLuuTaiKhoan';
import { DieuKhienSaoLuuTaiKhoan } from './dungSaoLuuTaiKhoan';
import { hoiGhiDe } from './hoiGhiDe';
import { theTrang } from './ThanhPhan';
import { Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './thietKe';

/**
 * Màn hình mời lấy sổ trên tài khoản về, hiện khi **máy này chưa có sổ mà tài khoản thì có**.
 *
 * Đây là câu trả lời cho một chuyện người dùng gặp thật: chủ đổi điện thoại, đăng nhập đúng
 * tài khoản cũ, vào lại đúng nhóm cũ — mà mở app ra thì sổ trắng trơn. Tài khoản trước đây chỉ
 * mang theo *chỗ trong nhóm*, không mang theo sổ, và **app không nói một chữ nào về chuyện
 * ấy**: không có màn hình nào, không có dòng nhắc nào. Người dùng chỉ thấy dữ liệu mất.
 *
 * Nên nó phải là một *màn hình chắn ngang*, hiện trước cả thanh tab, chứ không phải một dòng
 * chữ trong mục Sao lưu. Lý do không phải để long trọng: chấm vài ô vào sổ trống là máy này có
 * sổ riêng của nó, và lượt đẩy ngầm sau đó ghi đè bản của hôm nay trên tài khoản. Câu hỏi này
 * phải được trả lời **trước khi người dùng gõ dòng đầu tiên**.
 *
 * Ba chỗ cố ý làm như vậy:
 *
 * - **Nút *Để sau* vẫn có,** và chỉ nhớ trong lượt mở app này — cùng một lẽ với màn hình mở
 *   đầu ([ManHinhMoDau](ManHinhMoDau.tsx)): app chấm công phải chạy được cả khi người dùng
 *   không muốn trả lời câu nào. Sổ vẫn trống thì lần mở sau hỏi lại là đúng.
 * - **Lấy về vẫn đi qua hộp xác nhận kèm số liệu** ([hoiGhiDe](hoiGhiDe.ts)), y như khôi phục
 *   từ file. Máy đang trống nên nghe như thừa, nhưng nó là chỗ người dùng nhìn thấy *mình sắp
 *   nhận bản nào*: "4 thợ, 312 buổi công" khác hẳn "1 thợ, 2 buổi công" của một bản hỏng.
 * - **Chỉ mời bản mới nhất.** Muốn bản ngày khác thì vào Thợ → Sao lưu, nơi có cả danh sách.
 *   Bày 30 ngày ra đây là bắt người vừa mất máy chọn một câu họ chưa có cơ sở để chọn.
 */

interface Props {
  taiKhoan: DieuKhienSaoLuuTaiKhoan;
  /** Email của tài khoản đang đăng nhập, để người dùng biết mình đang lấy sổ của ai. */
  email: string | null;
  capNhat: (moi: DuLieuChamCong) => void;
  /** Để sau — vào thẳng app với sổ trống. Chỉ tính cho lượt mở app này. */
  onDeSau: () => void;
}

export function ManHinhLaySo({ taiKhoan, email, capNhat, onDeSau }: Props) {
  const [dangLay, datDangLay] = useState(false);
  const [loi, datLoi] = useState<string | null>(null);
  const ban = taiKhoan.trangThai.banChoLay;

  if (ban === null) {
    return null;
  }

  async function layVe(ban: BanTaiKhoan) {
    datDangLay(true);
    datLoi(null);
    try {
      const duLieuMoi = await taiKhoan.docBan(ban.ngay);
      hoiGhiDe(`Lấy sổ ngày ${Ngay.ngayGon(ban.ngay)} về máy này?`, duLieuMoi, 'Lấy về', (moi) => {
        capNhat(moi);
        // Trả lời rồi thì thôi mời nữa, và từ đây lượt đẩy ngầm mới được chạy.
        taiKhoan.daTraLoi();
      });
    } catch (loiChay) {
      datLoi(loiChay instanceof Error ? loiChay.message : 'Chưa lấy được bản này. Thử lại sau.');
    } finally {
      datDangLay(false);
    }
  }

  /**
   * Người dùng nói máy này chấm sổ mới. Ghi nhận là *đã trả lời* rồi vào app — từ lúc này lượt
   * đẩy ngầm được phép chạy, nên câu nhắc bên dưới nút phải nói thẳng cái giá của nó.
   */
  function chamSoMoi() {
    taiKhoan.daTraLoi();
    onDeSau();
  }

  return (
    <ScrollView contentContainerStyle={kieu.than}>
      <View style={kieu.dinh}>
        <View style={kieu.vongIcon}>
          <Feather name="download-cloud" size={26} color={Mau.chinh} />
        </View>
        <Text style={kieu.tieuDe} maxFontSizeMultiplier={HeSoChuToiDaLuoi}>
          Tài khoản này đã có sổ
        </Text>
        <Text style={kieu.chuPhu}>
          Máy này chưa có buổi công nào. Sổ đã sao lưu lên tài khoản thì lấy về được.
        </Text>
      </View>

      <View style={kieu.the}>
        <View style={kieu.dongTrangThai}>
          <Feather name="archive" size={19} color={Mau.chinh} />
          <View style={kieu.giuaDong}>
            <Text style={kieu.chuNhan}>Bản {Ngay.thuVaNgay(ban.ngay)}</Text>
            <Text style={kieu.chuNhoPhu}>
              {ban.suaLuc !== '' ? `Ghi lúc ${Ngay.gioPhut(ban.suaLuc)}` : 'Bản mới nhất'}
              {email !== null ? ` · ${email}` : ''}
            </Text>
          </View>
        </View>

        <Pressable
          style={[kieu.nutChinh, dangLay && kieu.nutMo]}
          onPress={() => layVe(ban)}
          disabled={dangLay}
          accessibilityRole="button"
        >
          {dangLay ? (
            <ActivityIndicator color={Mau.trang} />
          ) : (
            <Feather name="download" size={17} color={Mau.trang} />
          )}
          <Text style={kieu.chuNutChinh}>{dangLay ? 'Đang lấy…' : 'Lấy sổ về máy này'}</Text>
        </Pressable>

        <Text style={kieu.chuNhac}>
          Muốn lấy bản của ngày khác thì mở mục Thợ → Sao lưu, ở đó có cả danh sách.
        </Text>
      </View>

      {loi !== null && <Text style={kieu.chuLoi}>{loi}</Text>}

      <Pressable style={kieu.nutPhu} onPress={chamSoMoi} accessibilityRole="button">
        <Text style={kieu.chuNutPhu}>Máy này chấm sổ mới</Text>
      </Pressable>
      <Text style={kieu.chuChan}>
        Chấm sổ mới thì bản trên tài khoản của hôm nay sẽ bị sổ máy này thay lúc sao lưu. Các
        bản của những ngày trước vẫn còn.
      </Text>

      <Pressable style={kieu.nutDeSau} onPress={onDeSau} accessibilityRole="button">
        <Text style={kieu.chuNutDeSau}>Để sau, vào chấm công đã</Text>
      </Pressable>
    </ScrollView>
  );
}

const kieu = StyleSheet.create({
  than: { padding: 20, paddingTop: 40, gap: 12, flexGrow: 1, justifyContent: 'center' },

  dinh: { alignItems: 'center', gap: 8, paddingBottom: 12 },
  vongIcon: {
    width: 56,
    height: 56,
    borderRadius: 28,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: Mau.chinhNhat,
  },
  tieuDe: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhu: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },

  the: { ...theTrang, gap: 12 },
  dongTrangThai: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  giuaDong: { flex: 1, gap: 3 },
  chuNhan: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuNhoPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuNhac: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

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
  nutMo: { opacity: 0.6 },
  chuNutChinh: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.trang },

  nutPhu: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: Co.caoNut,
    paddingVertical: 10,
  },
  chuNutPhu: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chinh },

  nutDeSau: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: Co.caoNutNho,
  },
  chuNutDeSau: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  chuLoi: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.do,
    textAlign: 'center',
  },
  chuChan: {
    fontSize: Co.chuPhu,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
});
