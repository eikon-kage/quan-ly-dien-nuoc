import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import * as SoBenKia from '../nghiepvu/soBenKia';
import { CaiDatVai, docMaMoi, ketNap } from '../nghiepvu/vaiMay';
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
 */

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  caiDat: CaiDatVai;
  datCaiDat: (moi: CaiDatVai) => void;
  onDong: () => void;
}

export function HopVaiMay({ duLieu, capNhat, caiDat, datCaiDat, onDong }: Props) {
  /** null = đang xem, chuỗi = đang gõ mã mời. */
  const [ma, datMa] = useState<string | null>(null);
  const [loi, datLoi] = useState<string | null>(null);

  const nguoiKhac = duLieu.thos.length > 1 || duLieu.ungTiens.length > 0;

  async function nhanVaiTho(xoaNguoiKhac: boolean) {
    const thoId = docMaMoi(ma ?? '');
    if (thoId === null) {
      datLoi('Mã mời không đúng. Mã có dạng CC-a1b2c3.');
      return;
    }

    const homNay = Ngay.homNay();
    capNhat(ketNap(duLieu, thoId, homNay, xoaNguoiKhac));
    // Sổ bên kia của vai cũ không còn nghĩa gì — bỏ đi để đối chiếu không so với sổ lạ.
    await SoBenKia.xoaHet();
    datCaiDat({ vai: 'tho', thoId, batDauTu: homNay });
    onDong();
  }

  async function vePhaiLaMayChu() {
    await SoBenKia.xoaHet();
    datCaiDat({ vai: 'chu', thoId: null, batDauTu: null });
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
          <Dong
            icon="user"
            nhan="Máy của thợ"
            phu="Chỉ tự chấm công cho mình và đối chiếu với sổ chủ. Không thấy tiền."
            dangChon={caiDat.vai === 'tho'}
            onPress={caiDat.vai === 'tho' ? undefined : () => datMa('')}
          />

          <Text style={kieu.chuChan}>
            {caiDat.vai === 'tho'
              ? 'Đổi lại thành máy chủ thì những buổi đã chấm trên máy này vẫn còn.'
              : 'Thợ cần mã mời của chủ. Chủ mở mục Thợ → Đối chiếu để đọc mã.'}
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
            placeholder="CC-a1b2c3"
            autoCapitalize="none"
            autoCorrect={false}
            autoFocus
          />

          {loi !== null && <Text style={kieu.chuLoi}>{loi}</Text>}

          <Pressable style={kieu.nutChinh} onPress={() => nhanVaiTho(false)}>
            <Feather name="check" size={17} color={Mau.trang} />
            <Text style={kieu.chuNutChinh}>Xong</Text>
          </Pressable>

          {/*
            Chỉ hiện khi trên máy còn sổ của người khác — tức là một cái máy từng dùng làm
            máy chủ, giờ chuyền cho thợ. Không hiện với máy mới cài, kẻo người ta phải chọn
            giữa hai nút mà cả hai đều làm đúng một việc.
          */}
          {nguoiKhac && (
            <Pressable style={kieu.nutPhu} onPress={() => nhanVaiTho(true)}>
              <Feather name="trash-2" size={16} color={Mau.do} />
              <Text style={kieu.chuNutPhu}>Xoá sổ của người khác trên máy này</Text>
            </Pressable>
          )}

          <Text style={kieu.chuChan}>
            Chủ đọc mã cho thợ qua điện thoại hay Zalo cũng được — mã chỉ dùng để hai máy
            biết đang nói về cùng một người, không phải mật khẩu.
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
