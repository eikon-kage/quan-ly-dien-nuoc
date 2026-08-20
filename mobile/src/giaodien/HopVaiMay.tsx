import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import * as SoBenKia from '../nghiepvu/soBenKia';
import { CaiDatVai, ketNap } from '../nghiepvu/vaiMay';
import { DieuKhienNhom } from './dungSupabase';
import { HopDay } from './HopDay';
import { ONhap } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

/**
 * Chọn vai cho máy này: máy của chủ, hay máy của một thợ tự chấm.
 *
 * Mặc định mọi máy là máy chủ, nên hộp này thật ra chỉ dùng đúng một lần trên đời — lúc
 * thợ mới cài app. Vì vậy nó nằm sâu trong mục Thợ chứ không chiếm chỗ ở màn hình đầu:
 * đưa một câu hỏi "anh là chủ hay là thợ?" lên ngay lúc mở app lần đầu là chặn đường tất
 * cả những người chỉ muốn chấm công.
 *
 * **Thợ chỉ nhập đúng một mã.** Mã mời do database phát ra (`phat_ma_moi`), và lúc đổi mã
 * (`doi_ma_moi`) database trả về luôn `tho_id` — id của thợ ấy trong sổ chủ. Nên một lần dán
 * mã làm xong cả ba việc: xin tài khoản, vào nhóm, và đặt vai máy kèm đúng id để lúc đối
 * chiếu hai máy ghép được người với người.
 *
 * Bản trước bắt nhập hai mã khác nhau — một mã `CC-<thoId>` để đặt vai máy, một mã 6 ký tự
 * để vào nhóm. Hai mã cho một lần cài, mà thợ thì đang đứng ngoài công trường.
 */

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  caiDat: CaiDatVai;
  datCaiDat: (moi: CaiDatVai) => void;
  nhom: DieuKhienNhom;
  /**
   * Mở thẳng ra ô dán mã, bỏ qua bước chọn vai. Dùng cho đường vào từ dải *Chưa nối nhóm*
   * trên màn hình máy thợ: máy ấy đã là máy thợ rồi, câu hỏi "anh là chủ hay là thợ" ở giữa
   * đường chỉ là một cú bấm vô nghĩa.
   */
  danMaNgay?: boolean;
  onDong: () => void;
}

export function HopVaiMay({
  duLieu,
  capNhat,
  caiDat,
  datCaiDat,
  nhom,
  danMaNgay = false,
  onDong,
}: Props) {
  /** null = đang xem, chuỗi = đang gõ mã mời. */
  const [ma, datMa] = useState<string | null>(
    danMaNgay && nhom.trangThai.hoTro ? '' : null,
  );
  const [loi, datLoi] = useState<string | null>(null);

  const nguoiKhac = duLieu.thos.length > 1 || duLieu.ungTiens.length > 0;

  async function nhanVaiTho(xoaNguoiKhac: boolean) {
    const gon = (ma ?? '').trim();
    if (gon === '') {
      datLoi('Anh dán mã mời của chủ vào đây nhé.');
      return;
    }
    datLoi(null);

    // Đổi mã trước, đặt vai sau. Ngược lại thì mã sai cũng đã biến máy này thành máy thợ,
    // mà máy thợ thì không thấy tiền — người dùng mắc cạn không hiểu vì sao.
    const thanhVien = await nhom.doiMa(gon);
    if (thanhVien === null) {
      // Câu lỗi thật đã nằm ở `nhom.trangThai.loi`, hiện ngay dưới ô nhập.
      return;
    }
    if (thanhVien.thoId === null) {
      // Database ràng thợ phải có tho_id (`tho_phai_co_id`), nên tới đây là hàng của một máy
      // chủ. Không được nuốt: kết nạp bằng id rỗng là sinh ra một thợ không tên trong sổ.
      datLoi('Mã này không phải mã mời thợ. Xin chủ phát mã mới.');
      return;
    }

    const homNay = Ngay.homNay();

    /*
      Máy đã tự chấm trước khi có mã mời thì id cũ là id máy tự đặt: chuyển hết bản ghi sang
      id thật, và **giữ nguyên mốc bắt đầu chấm**. Đặt lại mốc thành hôm nay thì mấy buổi
      chấm hồi chưa nối rơi ra ngoài khoảng sổ khai là đầy đủ, đối chiếu bỏ qua sạch.
    */
    const tuTao = caiDat.vai === 'tho' && caiDat.thoIdTuTao === true;
    const idCu = tuTao ? caiDat.thoId : null;
    const batDauTu = tuTao && caiDat.batDauTu !== null ? caiDat.batDauTu : homNay;

    capNhat(ketNap(duLieu, thanhVien.thoId, homNay, xoaNguoiKhac, idCu));
    // Sổ bên kia của vai cũ không còn nghĩa gì — bỏ đi để đối chiếu không so với sổ lạ.
    await SoBenKia.xoaHet();
    datCaiDat({
      vai: 'tho',
      thoId: thanhVien.thoId,
      batDauTu,
      thoIdTuTao: false,
      dungMotMinh: false,
    });
    onDong();
  }

  async function vePhaiLaMayChu() {
    await SoBenKia.xoaHet();
    // `dungMotMinh` giữ nguyên: đó là chuyện của cái máy, không đổi theo vai.
    datCaiDat({
      vai: 'chu',
      thoId: null,
      batDauTu: null,
      thoIdTuTao: false,
      dungMotMinh: caiDat.dungMotMinh,
    });
    onDong();
  }

  return (
    <HopDay onDong={onDong}>
      <Text style={kieu.tieuDe}>Máy này là của ai</Text>

      {ma === null ? (
        <>
          <Dong
            icon="home"
            nhan="Máy của chủ"
            phu="Chấm công cho cả nhóm, xem bảng lương, chốt kỳ."
            dangChon={caiDat.vai === 'chu'}
            onPress={caiDat.vai === 'chu' ? undefined : vePhaiLaMayChu}
          />
          {/*
            Vẫn bấm được khi máy này đã là máy thợ: thợ bị ngắt khỏi nhóm, hay chủ phát lại mã
            mới, thì đây là đường duy nhất để dán mã lần nữa.
          */}
          <Dong
            icon="user"
            nhan={caiDat.vai === 'tho' ? 'Máy của thợ · dán lại mã mời' : 'Máy của thợ'}
            phu="Chỉ tự chấm công cho mình và đối chiếu với sổ chủ. Không thấy tiền."
            dangChon={caiDat.vai === 'tho'}
            onPress={nhom.trangThai.hoTro ? () => datMa('') : undefined}
          />

          <Text style={kieu.chuChan}>
            {!nhom.trangThai.hoTro
              ? 'Bản app này chưa được điền địa chỉ nhóm, nên chưa nhận mã mời được. Xem docs/chamcong-doi-chieu.md.'
              : caiDat.vai === 'tho'
                ? 'Đổi lại thành máy chủ thì những buổi đã chấm trên máy này vẫn còn.'
                : 'Thợ cần mã mời của chủ. Chủ mở mục Thợ → Đối chiếu, chọn thợ ấy rồi bấm Phát mã mời.'}
          </Text>
        </>
      ) : (
        <>
          <ONhap
            nhan="Mã mời của chủ"
            value={ma}
            onChangeText={(chu) => {
              datMa(chu);
              datLoi(null);
            }}
            placeholder="K7MQP4"
            // Mã database phát ra toàn chữ hoa; tự hoa lên thì thợ khỏi phải để ý bàn phím.
            autoCapitalize="characters"
            autoCorrect={false}
            autoFocus
          />

          {(loi ?? nhom.trangThai.loi) !== null && (
            <Text style={kieu.chuLoi}>{loi ?? nhom.trangThai.loi}</Text>
          )}

          <Pressable
            style={kieu.nutChinh}
            onPress={() => nhanVaiTho(false)}
            disabled={nhom.trangThai.dangChay}
          >
            {nhom.trangThai.dangChay ? (
              <ActivityIndicator color={Mau.trang} />
            ) : (
              <Feather name="check" size={17} color={Mau.trang} />
            )}
            <Text style={kieu.chuNutChinh}>
              {nhom.trangThai.dangChay ? 'Đang vào nhóm…' : 'Xong'}
            </Text>
          </Pressable>

          {/*
            Chỉ hiện khi trên máy còn sổ của người khác — tức là một cái máy từng dùng làm
            máy chủ, giờ chuyền cho thợ. Không hiện với máy mới cài, kẻo người ta phải chọn
            giữa hai nút mà cả hai đều làm đúng một việc.
          */}
          {nguoiKhac && (
            <Pressable
              style={kieu.nutPhu}
              onPress={() => nhanVaiTho(true)}
              disabled={nhom.trangThai.dangChay}
            >
              <Feather name="trash-2" size={16} color={Mau.do} />
              <Text style={kieu.chuNutPhu}>Xoá sổ của người khác trên máy này</Text>
            </Pressable>
          )}

          <Text style={kieu.chuChan}>
            Chủ đọc mã cho thợ qua điện thoại hay Zalo cũng được. Mã dùng một lần và sống ba
            ngày; hết hạn thì xin chủ phát mã mới.
          </Text>
        </>
      )}
    </HopDay>
  );
}

function Dong({
  icon,
  nhan,
  phu,
  dangChon,
  onPress,
}: {
  icon: keyof typeof Feather.glyphMap;
  nhan: string;
  phu: string;
  dangChon: boolean;
  onPress?: () => void;
}) {
  return (
    <Pressable
      style={[kieu.dong, dangChon && kieu.dongChon]}
      onPress={onPress}
      disabled={onPress === undefined}
      accessibilityState={{ selected: dangChon }}
    >
      <Feather name={icon} size={19} color={dangChon ? Mau.chinh : Mau.chu} />
      <View style={kieu.giuaDong}>
        <Text style={[kieu.chuNhan, dangChon && kieu.chuNhanChon]}>{nhan}</Text>
        <Text style={kieu.chuPhu}>{phu}</Text>
      </View>
      {dangChon && <Feather name="check" size={18} color={Mau.chinh} />}
    </Pressable>
  );
}

const kieu = StyleSheet.create({
  tieuDe: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu, marginBottom: 4 },

  dong: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    minHeight: Co.caoNut,
    padding: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    borderColor: Mau.vien,
  },
  dongChon: { borderColor: Mau.chinh, backgroundColor: Mau.chinhNhat },
  giuaDong: { flex: 1, gap: 3 },
  chuNhan: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.chu },
  chuNhanChon: { color: Mau.chinh },
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

  chuLoi: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.do },
  chuChan: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
});
