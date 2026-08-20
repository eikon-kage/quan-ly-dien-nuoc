import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { taoId } from '../nghiepvu/thaoTac';
import { CaiDatVai, ketNap } from '../nghiepvu/vaiMay';
import { DieuKhienNhom } from './dungSupabase';
import { HopNoiNhom } from './HopNoiNhom';
import { HopVaiMay } from './HopVaiMay';
import { theTrang } from './ThanhPhan';
import { Co, HeSoChuToiDaLuoi, Mau, PhongChu } from './thietKe';

/**
 * Màn hình đầu tiên khi máy chưa nối được nhóm chấm công: **đăng nhập và vào nhóm ngay lúc
 * mở app**, không phải mò vào mục Thợ mới thấy đường.
 *
 * Trước đây cả hai đường nối đều nằm sâu trong mục Thợ — chủ vào *Thợ → Nhóm chấm công*, thợ
 * vào *Thợ → Máy của thợ*. Hai chỗ ấy vẫn còn (đó là chỗ xem trạng thái và ngắt), nhưng
 * người mới cài app thì không có lý gì để mở mục Thợ ra tìm: họ mở app lên là thấy màn hình
 * chấm công, chấm được, và không bao giờ biết là sổ chẳng đi đâu cả.
 *
 * **Không nối được thì vẫn phải vào chấm công được**, nên có nút *Để sau*. Đây là điều kiện
 * để đưa màn hình này lên trước: app chấm công vẫn chạy trọn vẹn khi không có mạng và không
 * có tài khoản nào, mà chặn đường người chỉ muốn chấm công thì mất nhiều hơn được (điều 8
 * trong docs/chamcong-giao-dien.md). *Để sau* chỉ tắt cho lượt mở app này; lần mở sau lại
 * hỏi, vì sổ chưa nối thì vẫn chưa ai nhận được.
 *
 * Màn hình này **chỉ hiện khi biết chắc là chưa nối**. Máy mất mạng không tra được nhóm thì
 * `traHut` bật và App không dựng màn hình này — xem ghi chú ở `TrangThaiNhom.traHut`.
 *
 * Hai đường nối không viết lại ở đây: bấm vào là mở đúng hai cái hộp đang dùng trong mục Thợ
 * ([HopNoiNhom](HopNoiNhom.tsx) và [HopVaiMay](HopVaiMay.tsx)). Viết lại form đăng nhập lần
 * thứ hai là hai chỗ phải sửa mỗi lần đổi câu chữ, và sớm muộn hai chỗ lệch nhau.
 *
 * **Hỏi hai bước, không gộp một.** Bước một: *máy này là của ai*. Bước hai: cách vào, và mỗi
 * vai có một cách khác nhau — chủ đăng nhập bằng email, thợ dán mã mời. Gộp lại thành hai
 * đường "đăng nhập" và "dán mã" trên cùng một màn hình thì người dùng phải tự dịch từ *tôi là
 * ai* sang *tôi bấm cái nào*, mà đó là câu hỏi khó hơn hẳn câu app đang cần họ trả lời.
 *
 * Bước hai luôn có **đường đi tiếp mà không cần email cũng không cần mã mời**: một nút đăng
 * nhập với một câu "chưa có thì thôi" là hai kết cục khác nhau — nút thì bấm được, còn câu
 * chữ thì người ta đọc rồi ngồi im. Với thợ, đường ấy làm được nhờ `doiThoId`: tự chấm trước
 * bằng id máy tự đặt, tới lúc dán mã thì mọi buổi đã chấm chuyển sang id thật.
 */

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  caiDat: CaiDatVai;
  datCaiDat: (moi: CaiDatVai) => void;
  nhom: DieuKhienNhom;
  /** Để sau — vào thẳng app, không nối. */
  onDeSau: () => void;
}

/** null = chưa mở hộp nào. `vai` là hộp chọn vai máy, xem `layMayChu` bên dưới. */
type HopDangMo = 'chu' | 'tho' | 'vai' | null;

export function ManHinhMoDau({ duLieu, capNhat, caiDat, datCaiDat, nhom, onDeSau }: Props) {
  const [dangMo, datDangMo] = useState<HopDangMo>(null);
  /** null = đang ở bước chọn vai; còn lại là bước chọn cách vào của vai ấy. */
  const [vaiChon, datVaiChon] = useState<'chu' | 'tho' | null>(null);
  const { taiKhoan, dangChay, loi } = nhom.trangThai;

  /**
   * Đã đăng nhập mà chưa vào nhóm là một tình huống khác hẳn chưa đăng nhập, nên hỏi một câu
   * khác: đăng nhập lại chẳng giúp gì, thứ còn thiếu là cái nhóm.
   *
   * Máy chủ tới được đây là lượt lập nhóm lúc mở app đã hụt (bảng chưa dựng, hay đứt mạng
   * đúng nhịp ấy) — nên nút ở đây là *thử lại*, không phải *đăng nhập*.
   */
  const daDangNhap = taiKhoan !== null;

  /**
   * Máy đang là máy thợ mà bấm *Tôi là chủ* thì phải đi qua hộp chọn vai, không nhảy thẳng
   * vào ô đăng nhập: `HopNoiNhom` nhận vai của máy, nên với máy thợ nó chỉ hiện một đoạn chỉ
   * đường sang chỗ dán mã — bấm vào là mắc cạn giữa màn hình mở đầu.
   */
  const layMayChu = !daDangNhap && caiDat.vai === 'tho';

  /**
   * Chủ chọn dùng một mình. Máy đang là máy thợ thì phải qua hộp chọn vai — đổi vai còn phải
   * dọn sổ bên kia, mà việc dọn ấy nằm trong `HopVaiMay`, không nhân bản ra đây.
   */
  function dungMotMinh() {
    if (caiDat.vai === 'tho') {
      datDangMo('vai');
      return;
    }

    datCaiDat({ ...caiDat, dungMotMinh: true });
    onDeSau();
  }

  /**
   * Thợ tự chấm trước khi có mã mời. Id do máy tự đặt và **đánh dấu là tự đặt**: tới lúc dán
   * mã, `ketNap` chuyển mọi buổi đã chấm sang id thật của sổ chủ.
   *
   * Máy đã từng là máy thợ thì giữ nguyên id và mốc bắt đầu cũ — chấm tiếp vào sổ đang có,
   * không sinh ra một người thứ hai trong cùng cái máy.
   */
  function tuChamTruoc() {
    const homNay = Ngay.homNay();
    const daLaTho = caiDat.vai === 'tho' && caiDat.thoId !== null;
    const thoId = daLaTho ? (caiDat.thoId as string) : taoId();

    capNhat(ketNap(duLieu, thoId, homNay, false));
    datCaiDat({
      vai: 'tho',
      thoId,
      batDauTu: daLaTho && caiDat.batDauTu !== null ? caiDat.batDauTu : homNay,
      // Id thật của một nhóm cũ thì đừng đánh dấu là tự đặt: đánh dấu sai là lần dán mã sau
      // sẽ kéo sổ của người này sang id của người khác.
      thoIdTuTao: daLaTho ? caiDat.thoIdTuTao === true : true,
      dungMotMinh: true,
    });
    onDeSau();
  }

  return (
    <ScrollView contentContainerStyle={kieu.than}>
      <View style={kieu.dinh}>
        <View style={kieu.vongIcon}>
          <Feather name="users" size={26} color={Mau.chinh} />
        </View>
        <Text style={kieu.tieuDe} maxFontSizeMultiplier={HeSoChuToiDaLuoi}>
          {daDangNhap
            ? 'Nối nhóm chấm công'
            : vaiChon === null
              ? 'Máy này là của ai'
              : vaiChon === 'chu'
                ? 'Máy của chủ'
                : 'Máy của thợ'}
        </Text>
        <Text style={kieu.chuPhu}>
          {daDangNhap
            ? 'Đã đăng nhập nhưng máy này chưa ở trong nhóm nào, nên sổ chưa gửi đi được.'
            : vaiChon === null
              ? 'Chọn một lần thôi. Vào rồi vẫn đổi lại được.'
              : vaiChon === 'chu'
                ? 'Vào bằng cách nào cũng chấm công được ngay.'
                : 'Có mã mời của chủ thì dán vào. Chưa có cũng chấm được.'}
        </Text>
      </View>

      {daDangNhap ? (
        <View style={kieu.the}>
          <View style={kieu.dongTrangThai}>
            <Feather name="alert-circle" size={19} color={Mau.do} />
            <View style={kieu.giuaDong}>
              <Text style={kieu.chuNhan}>Đã đăng nhập, chưa vào nhóm</Text>
              <Text style={kieu.chuNhoPhu}>
                {taiKhoan.email ?? 'Tài khoản ẩn danh của máy này'}
              </Text>
            </View>
          </View>

          {caiDat.vai === 'chu' ? (
            <Pressable style={kieu.nutChinh} onPress={nhom.lapNhom} disabled={dangChay}>
              {dangChay ? (
                <ActivityIndicator color={Mau.trang} />
              ) : (
                <Feather name="refresh-cw" size={17} color={Mau.trang} />
              )}
              <Text style={kieu.chuNutChinh}>{dangChay ? 'Đang nối…' : 'Lập nhóm, thử lại'}</Text>
            </Pressable>
          ) : (
            /*
              Máy thợ không tự vào nhóm được — nó cần một mã mời còn hạn của chủ. Nên ở đây
              là đường dán mã, không phải nút thử lại: bấm thử lại mãi cũng chỉ tra ra đúng
              câu trả lời cũ.
            */
            <Pressable style={kieu.nutChinh} onPress={() => datDangMo('tho')}>
              <Feather name="key" size={17} color={Mau.trang} />
              <Text style={kieu.chuNutChinh}>Dán mã mời của chủ</Text>
            </Pressable>
          )}
        </View>
      ) : vaiChon === null ? (
        <>
          {/*
            Bước một: "anh là chủ hay là thợ?" — chỗ duy nhất trong app hỏi câu này. Chọn sai
            không mất gì: bước hai còn quay lại được, và máy chỉ thật sự đổi vai sau khi dán
            được một mã mời hợp lệ.
          */}
          <Duong
            icon="home"
            nhan="Tôi là chủ"
            phu="Chấm cho cả nhóm, xem bảng lương, chốt kỳ trả tiền."
            onPress={() => datVaiChon('chu')}
          />
          <Duong
            icon="user"
            nhan="Tôi là thợ"
            phu="Chỉ tự chấm công cho mình. Không thấy tiền của ai."
            onPress={() => datVaiChon('tho')}
          />
        </>
      ) : vaiChon === 'chu' ? (
        <>
          <Duong
            icon="mail"
            nhan="Đăng nhập bằng email"
            phu={
              layMayChu
                ? 'Máy này đang là máy của thợ — đổi lại thành máy chủ trước.'
                : 'Tài khoản này nắm nhóm của cả cửa hàng, mất máy vẫn đăng nhập lại được.'
            }
            onPress={() => datDangMo(layMayChu ? 'vai' : 'chu')}
          />
          {/*
            Chủ dùng một mình là chuyện thường: nhà có ba thợ, chấm bằng một cái điện thoại,
            chẳng cần thợ nào đối chiếu. Đây là *quyết định*, không phải hoãn, nên nó ghi vào
            máy (`dungMotMinh`) và lần mở app sau không hỏi lại.
          */}
          <Duong
            icon="smartphone"
            nhan="Dùng một mình, không cần email"
            phu="Chấm công, tính lương, chốt kỳ vẫn đủ. Chỉ là không đối chiếu với máy thợ."
            onPress={dungMotMinh}
          />
        </>
      ) : (
        <>
          <Duong
            icon="key"
            nhan="Dán mã mời của chủ"
            phu="Sáu ký tự chủ đọc cho. Không cần email, không cần mật khẩu."
            onPress={() => datDangMo('tho')}
          />
          {/*
            Chưa xin được mã mà vẫn cho vào: thợ tải app về giữa tuần, chủ thì đang ngoài công
            trình. Chấm trước bằng id máy tự đặt, tới lúc dán mã thì `doiThoId` kéo hết mấy
            buổi ấy sang id thật — không mất buổi nào, nên câu dưới đây nói được là "vẫn còn".
          */}
          <Duong
            icon="edit-3"
            nhan="Chưa có mã, tự chấm trước"
            phu="Chấm luôn hôm nay. Dán mã sau thì mấy buổi đã chấm vẫn còn nguyên."
            onPress={tuChamTruoc}
          />
        </>
      )}

      {loi !== null && <Text style={kieu.chuLoi}>{loi}</Text>}

      {vaiChon !== null && (
        <Pressable style={kieu.nutLui} onPress={() => datVaiChon(null)} accessibilityRole="button">
          <Feather name="chevron-left" size={16} color={Mau.xam} />
          <Text style={kieu.chuNutLui}>Chọn lại vai máy</Text>
        </Pressable>
      )}

      <Pressable style={kieu.nutDeSau} onPress={onDeSau} accessibilityRole="button">
        <Text style={kieu.chuNutDeSau}>Để sau, vào chấm công đã</Text>
      </Pressable>

      <Text style={kieu.chuChan}>
        Chưa nối thì máy vẫn chấm công, tính lương, chốt kỳ như thường — chỉ là chủ với thợ
        không đối chiếu được sổ với nhau.
      </Text>

      {dangMo === 'chu' && (
        <HopNoiNhom vai={caiDat.vai} dieuKhien={nhom} onDong={() => datDangMo(null)} />
      )}

      {/*
        `danMaNgay` để bỏ bước chọn vai ở giữa: ở đây người dùng vừa nói mình là thợ rồi, hỏi
        lại lần nữa là một cú bấm không mang thêm tin gì.
      */}
      {(dangMo === 'tho' || dangMo === 'vai') && (
        <HopVaiMay
          duLieu={duLieu}
          capNhat={capNhat}
          caiDat={caiDat}
          datCaiDat={datCaiDat}
          nhom={nhom}
          danMaNgay={dangMo === 'tho'}
          onDong={() => datDangMo(null)}
        />
      )}
    </ScrollView>
  );
}

/** Một đường nối: thẻ trắng, icon, nhãn to, một dòng giải thích. */
function Duong({
  icon,
  nhan,
  phu,
  onPress,
}: {
  icon: keyof typeof Feather.glyphMap;
  nhan: string;
  phu: string;
  onPress: () => void;
}) {
  return (
    <Pressable style={kieu.duong} onPress={onPress} accessibilityRole="button">
      <View style={kieu.vongIconNho}>
        <Feather name={icon} size={19} color={Mau.chinh} />
      </View>
      <View style={kieu.giuaDong}>
        <Text style={kieu.chuNhan} maxFontSizeMultiplier={HeSoChuToiDaLuoi}>
          {nhan}
        </Text>
        <Text style={kieu.chuNhoPhu}>{phu}</Text>
      </View>
      <Feather name="chevron-right" size={20} color={Mau.xam} />
    </Pressable>
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
  duong: {
    ...theTrang,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    minHeight: Co.caoNut,
  },
  vongIconNho: {
    width: 38,
    height: 38,
    borderRadius: 19,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: Mau.chinhNhat,
  },
  dongTrangThai: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  giuaDong: { flex: 1, gap: 3 },
  chuNhan: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuNhoPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

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

  nutLui: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    minHeight: Co.caoNutNho,
  },
  chuNutLui: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  nutDeSau: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: Co.caoNut,
    paddingVertical: 10,
  },
  chuNutDeSau: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chinh },

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
