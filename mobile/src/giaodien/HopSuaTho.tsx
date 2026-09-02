import { Feather } from '@expo/vector-icons';
import { useState } from 'react';
import {
  KeyboardAvoidingView,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { DuLieuChamCong, Tho } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { docTien } from '../nghiepvu/nhapSo';
import {
  datLuong,
  demCuaTho,
  lichSuLuong,
  luongTaiNgay,
  luuTho,
  themTho,
  xoaMocLuong,
  xoaTho,
} from '../nghiepvu/thaoTac';
import { HopChon } from './HopChon';
import { HopNhapSo } from './HopNhapSo';
import { hoi } from './hopThoai';
import { NutChip, ONhap, theTrang } from './ThanhPhan';
import { Co, Mau, PhongChu } from './thietKe';

interface Props {
  duLieu: DuLieuChamCong;
  /** Để trống là thêm thợ mới. */
  tho: Tho | null;
  capNhat: (moi: DuLieuChamCong) => void;
  onDong: () => void;
}

/** Nối danh sách kiểu người Việt đọc: "12 buổi công, 2 lần ứng và 1 ghi chú". */
function noiTiengViet(cac: string[]): string {
  if (cac.length <= 1) {
    return cac.join('');
  }
  return `${cac.slice(0, -1).join(', ')} và ${cac[cac.length - 1]}`;
}

export function HopSuaTho({ duLieu, tho, capNhat, onDong }: Props) {
  const homNay = Ngay.homNay();
  const moiNhat = tho ? duLieu.thos.find((t) => t.id === tho.id) ?? tho : null;

  const [ten, datTen] = useState(tho?.ten ?? '');
  const [tienMoi, datTienMoi] = useState('');
  const [dangLam, datDangLam] = useState(tho?.dangLam ?? true);

  /** Đang ở bước nào của việc đổi lương: nhập số tiền, rồi chọn áp dụng từ bao giờ. */
  const [soTienMoi, datSoTienMoi] = useState<number | null>(null);
  const [dangNhapLuong, datDangNhapLuong] = useState(false);

  function luu() {
    const tenSach = ten.trim();
    if (tenSach.length === 0) {
      hoi('Thiếu tên', 'Anh nhập tên thợ đã.', [{ text: 'Đóng' }]);
      return;
    }

    if (moiNhat === null) {
      const tienMotCong = docTien(tienMoi);
      if (tienMotCong === null || tienMotCong <= 0) {
        hoi('Thiếu tiền công', 'Anh nhập tiền một công của thợ đã.', [{ text: 'Đóng' }]);
        return;
      }

      capNhat(themTho(duLieu, tenSach, tienMotCong, homNay).duLieu);
    } else {
      capNhat(luuTho(duLieu, { ...moiNhat, ten: tenSach, dangLam }));
    }

    onDong();
  }

  /** Đặt mốc lương mới. Mốc cũ giữ nguyên nên bảng lương các tháng trước không đổi. */
  function apDungLuong(ma: string) {
    if (moiNhat === null || soTienMoi === null) {
      return;
    }

    const dauThang = Ngay.ghep(Ngay.tach(homNay).nam, Ngay.tach(homNay).thang, 1);
    const dangApDung =
      [...moiNhat.mocLuong].reverse().find((m) => m.tuNgay <= homNay) ?? moiNhat.mocLuong[0];

    const tuNgay =
      ma === 'homNay' ? homNay : ma === 'dauThang' ? dauThang : dangApDung.tuNgay;

    capNhat(datLuong(duLieu, moiNhat.id, tuNgay, soTienMoi));
    datSoTienMoi(null);
  }

  /**
   * Đánh dấu đã nghỉ rồi đóng hộp — lối thoát cho người không xoá được, và là thứ nên chọn
   * cả khi xoá được: thợ nghỉ việc thì bảng lương các tháng trước vẫn phải tra lại được.
   *
   * Giữ luôn cái tên đang gõ dở, kẻo sửa tên xong bấm *Cho nghỉ* là mất phần vừa sửa.
   */
  function choNghi() {
    if (moiNhat === null) {
      return;
    }

    const tenSach = ten.trim();
    capNhat(
      luuTho(duLieu, {
        ...moiNhat,
        ten: tenSach === '' ? moiNhat.ten : tenSach,
        dangLam: false,
      }),
    );
    onDong();
  }

  /**
   * Xoá hẳn thợ. Hỏi lại một câu **nói rõ mất những gì**, và luôn chìa ra lối *Cho nghỉ* —
   * chín trên mười lần người ta muốn cái sau, chỉ là không biết nó nằm ở nút gạt phía trên.
   */
  function xoa() {
    if (moiNhat === null) {
      return;
    }

    const dem = demCuaTho(duLieu, moiNhat.id);

    if (dem.daChot) {
      const loi =
        `${moiNhat.ten} đã có tên trong một kỳ đã chốt. Tiền kỳ ấy trả xong rồi, mà xoá đi ` +
        'thì bấm vào tờ quyết toán cũ không mở ra được nữa.';

      hoi(
        'Không xoá được thợ này',
        moiNhat.dangLam
          ? `${loi}\n\nCho nghỉ thì tên vẫn còn trong sổ cũ, chỉ là không hiện ở màn hình chấm công nữa.`
          : `${loi}\n\nThợ này đã đánh dấu nghỉ rồi nên cũng không hiện ở màn hình chấm công.`,
        moiNhat.dangLam
          ? [{ text: 'Thôi', style: 'cancel' }, { text: 'Cho nghỉ', onPress: choNghi }]
          : [{ text: 'Đóng' }],
      );
      return;
    }

    const mat = [
      dem.soBuoiCong > 0 ? `${dem.soBuoiCong} buổi công` : null,
      dem.soUngTien > 0 ? `${dem.soUngTien} lần ứng` : null,
      dem.soGhiChu > 0 ? `${dem.soGhiChu} ghi chú` : null,
    ].filter((c): c is string => c !== null);

    const loi =
      mat.length === 0
        ? 'Người này chưa có buổi công nào trong sổ nên xoá đi cũng không mất gì.'
        : `Mất luôn ${noiTiengViet(mat)} của người này, không lấy lại được.` +
          (moiNhat.dangLam
            ? '\n\nMuốn giữ lại sổ cũ thì chọn Cho nghỉ: tên vẫn còn để tra, chỉ không hiện ở màn hình chấm công.'
            : '');

    // Android chỉ vẽ được ba nút, nên thợ đã nghỉ rồi thì bỏ luôn nút Cho nghỉ đi.
    hoi(`Xoá ${moiNhat.ten}?`, loi, [
      { text: 'Thôi', style: 'cancel' },
      ...(moiNhat.dangLam ? [{ text: 'Cho nghỉ', onPress: choNghi }] : []),
      {
        text: 'Xoá',
        style: 'destructive' as const,
        onPress: () => {
          capNhat(xoaTho(duLieu, moiNhat.id));
          onDong();
        },
      },
    ]);
  }

  function xoaMoc(tuNgay: string) {
    if (moiNhat === null) {
      return;
    }

    hoi('Xoá mốc lương này?', `Mốc từ ngày ${Ngay.ngayGon(tuNgay)}.`, [
      { text: 'Thôi', style: 'cancel' },
      {
        text: 'Xoá',
        style: 'destructive',
        onPress: () => {
          try {
            capNhat(xoaMocLuong(duLieu, moiNhat.id, tuNgay));
          } catch {
            hoi('Không xoá được', 'Thợ phải còn ít nhất một mốc tiền công.', [
              { text: 'Đóng' },
            ]);
          }
        },
      },
    ]);
  }

  return (
    <Modal visible animationType="slide" onRequestClose={onDong}>
      {/*
        `behavior="padding"` cho cả iOS lẫn Android, không phân biệt hệ — xem ghi chú dài ở
        [HopDay](./HopDay.tsx). Kèm `ScrollView` có `keyboardShouldPersistTaps="handled"` để
        bấm được nút Lưu ngay khi bàn phím còn mở, không phải đóng bàn phím trước.
      */}
      <KeyboardAvoidingView behavior="padding" style={kieu.khung}>
        <SafeAreaView style={kieu.khung} edges={['top', 'bottom']}>
          <ScrollView contentContainerStyle={kieu.trong} keyboardShouldPersistTaps="handled">
            <Text style={kieu.tieuDe}>{moiNhat ? 'Sửa thợ' : 'Thêm thợ'}</Text>

            <ONhap
              nhan="Tên thợ"
              value={ten}
              onChangeText={datTen}
              placeholder="Ví dụ: Anh Tuấn"
              autoFocus={moiNhat === null}
            />

            {moiNhat === null ? (
              <View style={kieu.khoi}>
                <ONhap
                  nhan="Tiền một công"
                  coSo
                  value={tienMoi}
                  onChangeText={datTienMoi}
                  placeholder="Ví dụ: 300000"
                  keyboardType="number-pad"
                />
                <Text style={kieu.chuPhu}>Một ngày làm đủ sáng và chiều là 1 công.</Text>
              </View>
            ) : (
              <View style={kieu.theLuong}>
                <View style={kieu.dongNhan}>
                  <Text style={kieu.nhan}>Tiền công</Text>
                  <NutChip
                    nhan="Đổi lương"
                    icon="trending-up"
                    onPress={() => datDangNhapLuong(true)}
                  />
                </View>

                <Text style={kieu.tienLon}>{Ngay.tien(luongTaiNgay(moiNhat, homNay))}</Text>
                <Text style={kieu.chuPhu}>đang áp dụng cho một công</Text>

                {/*
                  Lịch sử để mốc mới nhất lên đầu. Đổi lương là thêm mốc chứ không sửa đè,
                  nên bảng lương các tháng trước vẫn giữ đúng số tiền đã trả.
                */}
                {moiNhat.mocLuong.length > 1 && (
                  <View style={kieu.lichSu}>
                    <Text style={kieu.nhanNho}>Các mốc đã qua</Text>
                    {lichSuLuong(moiNhat).map((moc) => (
                      <View key={moc.tuNgay} style={kieu.dongMoc}>
                        <Text style={kieu.chuMocNgay}>Từ {Ngay.ngayGon(moc.tuNgay)}</Text>
                        <Text style={kieu.chuMocTien}>{Ngay.tien(moc.tienMotCong)}</Text>
                        <Pressable style={kieu.nutXoaMoc} onPress={() => xoaMoc(moc.tuNgay)}>
                          <Feather name="trash-2" size={14} color={Mau.do} />
                        </Pressable>
                      </View>
                    ))}
                  </View>
                )}
              </View>
            )}

            {moiNhat !== null && (
              <View style={kieu.dongDangLam}>
                <View style={kieu.trai}>
                  <Text style={kieu.nhan}>Đang làm</Text>
                  <Text style={kieu.chuPhu}>
                    Tắt đi nếu thợ đã nghỉ. Bảng lương các tháng trước vẫn còn.
                  </Text>
                </View>
                <Switch
                  value={dangLam}
                  onValueChange={datDangLam}
                  trackColor={{ true: Mau.chinh, false: Mau.vien }}
                />
              </View>
            )}

            <Pressable style={[kieu.nut, kieu.nutChinh]} onPress={luu}>
              <Text style={[kieu.chuNut, { color: Mau.trang }]}>Lưu</Text>
            </Pressable>

            <Pressable style={[kieu.nut, kieu.nutPhu]} onPress={onDong}>
              <Text style={[kieu.chuNut, { color: Mau.xam }]}>Thôi, quay lại</Text>
            </Pressable>

            {/*
              Nút xoá tách hẳn xuống đáy, sau cả nút *Thôi*, và chỉ có viền đỏ chứ không nền
              đỏ: nó không phải việc người ta vào đây để làm. Chỗ nào cũng xoá được thì có
              ngày bấm trượt tay mất cả tháng công của một người.
            */}
            {moiNhat !== null && (
              <View style={kieu.khoiXoa}>
                <Pressable style={[kieu.nut, kieu.nutXoa]} onPress={xoa}>
                  <Feather name="trash-2" size={15} color={Mau.do} />
                  <Text style={[kieu.chuNut, { color: Mau.do }]}>Xoá thợ này</Text>
                </Pressable>
                <Text style={kieu.chuPhu}>
                  Thợ nghỉ việc thì tắt nút Đang làm ở trên, đừng xoá — xoá là mất luôn phần
                  sổ đã đi làm.
                </Text>
              </View>
            )}
          </ScrollView>
        </SafeAreaView>
      </KeyboardAvoidingView>

      {dangNhapLuong && (
        <HopNhapSo
          tieuDe="Đổi tiền công"
          moTa={`${moiNhat?.ten ?? ''} — một công bao nhiêu?`}
          goiY="Ví dụ 350000"
          onGhi={(so) => {
            datDangNhapLuong(false);
            datSoTienMoi(so);
          }}
          onDong={() => datDangNhapLuong(false)}
        />
      )}

      {soTienMoi !== null && (
        <HopChon
          tieuDe={`${Ngay.tien(soTienMoi)} một công — tính từ bao giờ?`}
          luaChon={[
            { ma: 'homNay', nhan: 'Từ hôm nay', icon: 'calendar' },
            { ma: 'dauThang', nhan: 'Từ đầu tháng này', icon: 'calendar' },
            { ma: 'suaDe', nhan: 'Sửa lại giá đang áp dụng', icon: 'edit-3' },
          ]}
          onChon={apDungLuong}
          onDong={() => datSoTienMoi(null)}
        />
      )}
    </Modal>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  trong: { padding: 16, paddingTop: 18, gap: 18 },
  tieuDe: { fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },

  khoi: { gap: 7 },
  // Khối tiền công gói vào một thẻ trắng: nó có tới bốn năm dòng, để trần thì trôi vào
  // giữa các khối khác không biết đâu là đầu đâu là cuối.
  theLuong: { ...theTrang, gap: 7 },
  dongNhan: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  nhan: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  nhanNho: { fontSize: Co.chuPhu, fontFamily: PhongChu.vua, color: Mau.xam },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  tienLon: { fontSize: 24, fontFamily: PhongChu.dam, color: Mau.chu },

  lichSu: {
    marginTop: 8,
    gap: 4,
    borderTopWidth: 1,
    borderTopColor: Mau.vien,
    paddingTop: 10,
  },
  dongMoc: { flexDirection: 'row', alignItems: 'center', gap: 10, minHeight: 34 },
  chuMocNgay: { flex: 1, fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuMocTien: { fontSize: Co.chuThuong, fontFamily: PhongChu.vua, color: Mau.chu },
  nutXoaMoc: { width: 30, height: 30, alignItems: 'center', justifyContent: 'center' },

  dongDangLam: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  trai: { flex: 1, gap: 2 },

  nut: {
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderRadius: Co.bo,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  nutChinh: { backgroundColor: Mau.chinh, borderColor: Mau.chinh },
  nutXoa: { flexDirection: 'row', gap: 7, backgroundColor: Mau.doNhat, borderColor: Mau.do },
  khoiXoa: { gap: 7, marginTop: 6, paddingTop: 16, borderTopWidth: 1, borderTopColor: Mau.vien },
  nutPhu: { backgroundColor: Mau.trang, borderColor: Mau.vien },
  chuNut: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, textAlign: 'center' },
});
