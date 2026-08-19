import { Feather } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { DongLech, KetQuaDoiChieu, doiChieu, layTheoBenKia } from '../nghiepvu/doiChieu';
import { BuoiLam, DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { soCuaMay } from '../nghiepvu/soCong';
import { thoDangLam, timTho } from '../nghiepvu/thaoTac';
import { CaiDatVai, maMoi } from '../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from './dungDoiChieu';
import { DauTrang, NutChip, theTrang } from './ThanhPhan';
import { Bong, Co, Mau, PhongChu, Tuoi } from './thietKe';

/**
 * Đối chiếu sổ hai bên: chỗ nào hai máy nói khác nhau thì hiện ra, người dùng bấm từng
 * dòng để lấy theo bên kia.
 *
 * **Không có nút lấy tất cả.** Đây là chỗ tiền ra tiền vào; một nút lấy hết là mời người ta
 * bấm cho xong việc mà không đọc. Lệch nhiều nhất cũng chỉ vài buổi một kỳ, bấm từng dòng
 * không mệt — mà mỗi lần bấm là một lần thật sự nhìn vào ngày nào, buổi nào.
 *
 * Dùng cho cả hai vai. Máy chủ vào thấy danh sách thợ rồi chọn một người; máy thợ chỉ có
 * mình nên vào thẳng phần chi tiết.
 */

interface Props {
  duLieu: DuLieuChamCong;
  capNhat: (moi: DuLieuChamCong) => void;
  caiDat: CaiDatVai;
  dieuKhien: DieuKhienDoiChieu;
  onDong?: () => void;
}

const TEN_BUOI: Record<BuoiLam, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

export function ManHinhDoiChieu({ duLieu, capNhat, caiDat, dieuKhien, onDong }: Props) {
  const { trangThai, soBenKia, dongBo, noiGoogle } = dieuKhien;

  /** Máy thợ chỉ có một người nên mở thẳng vào chi tiết, không qua danh sách. */
  const [dangXem, datDangXem] = useState<string | null>(
    caiDat.vai === 'tho' ? caiDat.thoId : null,
  );
  const [loiLay, datLoiLay] = useState<string | null>(null);

  const homNay = Ngay.homNay();
  const benKia = caiDat.vai === 'chu' ? 'thợ' : 'chủ';

  /** Kết quả đối chiếu của từng thợ. Thợ chưa gửi sổ thì không có trong bảng này. */
  const ketTheoTho = useMemo(() => {
    const bang = new Map<string, KetQuaDoiChieu>();
    for (const [thoId, daNhan] of soBenKia) {
      bang.set(thoId, doiChieu(soCuaMay(duLieu, caiDat, thoId, homNay), daNhan.so));
    }
    return bang;
  }, [duLieu, caiDat, soBenKia, homNay]);

  function layMotDong(thoId: string, lech: DongLech) {
    try {
      capNhat(layTheoBenKia(duLieu, thoId, lech));
      datLoiLay(null);
    } catch {
      datLoiLay('Buổi này đã nằm trong kỳ đã quyết toán, không sửa được nữa.');
    }
  }

  const chuTrangThai = !trangThai.hoTro
    ? 'Cần bản app cài thẳng vào máy'
    : !trangThai.daNoi
      ? 'Chưa nối Google'
      : trangThai.loi !== null
        ? trangThai.loi
        : trangThai.dangChay
          ? 'Đang đồng bộ…'
          : trangThai.lucCuoi !== null
            ? `Đồng bộ lúc ${Ngay.gioPhut(trangThai.lucCuoi)}`
            : 'Chưa đồng bộ lần nào';

  return (
    <View style={kieu.khung}>
      <DauTrang
        tieuDe="Đối chiếu"
        phu={chuTrangThai}
        onLui={onDong}
        phai={
          trangThai.hoTro && trangThai.daNoi ? (
            <Pressable
              style={kieu.nutDongBo}
              onPress={dongBo}
              disabled={trangThai.dangChay}
              accessibilityLabel="Đồng bộ lại"
            >
              {trangThai.dangChay ? (
                <ActivityIndicator color={Mau.chinh} />
              ) : (
                <Feather name="refresh-cw" size={18} color={Mau.chinh} />
              )}
            </Pressable>
          ) : undefined
        }
      />

      <ScrollView contentContainerStyle={kieu.than}>
        {!trangThai.hoTro && (
          <Text style={kieu.chuNhac}>
            Bản chạy thử trong Expo Go không nối được Google. Cài bản app thật vào máy rồi
            mới đối chiếu được.
          </Text>
        )}

        {trangThai.hoTro && !trangThai.daNoi && (
          <View style={kieu.theNhac}>
            <Text style={kieu.chuNhac}>
              Hai máy phải nối cùng một tài khoản Google thì mới thấy sổ của nhau.
            </Text>
            <NutChip nhan="Nối Google" icon="cloud" onPress={noiGoogle} />
          </View>
        )}

        {loiLay !== null && <Text style={[kieu.chuNhac, kieu.chuLoi]}>{loiLay}</Text>}

        {dangXem === null ? (
          <DanhSachTho
            duLieu={duLieu}
            ketTheoTho={ketTheoTho}
            onChon={datDangXem}
          />
        ) : (
          <ChiTiet
            tenTho={timTho(duLieu, dangXem)?.ten ?? soBenKia.get(dangXem)?.so.tenTho ?? 'Thợ'}
            benKia={benKia}
            ket={ketTheoTho.get(dangXem) ?? null}
            nhanLuc={soBenKia.get(dangXem)?.suaLuc ?? null}
            maMoiCuaTho={caiDat.vai === 'chu' ? maMoi(dangXem) : null}
            onLay={(lech) => layMotDong(dangXem, lech)}
            // Máy thợ không có danh sách để quay về, nên không hiện nút Chọn thợ khác.
            onDoiTho={caiDat.vai === 'chu' ? () => datDangXem(null) : undefined}
          />
        )}
      </ScrollView>
    </View>
  );
}

/** Máy chủ: mỗi thợ một dòng, nói ngay là khớp, lệch mấy buổi, hay chưa gửi sổ. */
function DanhSachTho({
  duLieu,
  ketTheoTho,
  onChon,
}: {
  duLieu: DuLieuChamCong;
  ketTheoTho: Map<string, KetQuaDoiChieu>;
  onChon: (thoId: string) => void;
}) {
  const thos = thoDangLam(duLieu);

  if (thos.length === 0) {
    return (
      <View style={kieu.trong}>
        <Feather name="users" size={34} color={Mau.xam} />
        <Text style={kieu.chuTrongTo}>Chưa có thợ nào</Text>
      </View>
    );
  }

  return (
    <>
      {thos.map((tho) => {
        const ket = ketTheoTho.get(tho.id);
        const soLech = ket?.lechs.length ?? 0;

        const chu = !ket
          ? 'Chưa gửi sổ lên'
          : ket.khongTrungKhoang
            ? 'Sổ hai bên chưa có ngày nào chung'
            : soLech === 0
              ? `Khớp cả ${ket.soKhop} buổi`
              : `Lệch ${soLech} buổi · khớp ${ket.soKhop}`;

        const mau = !ket || ket.khongTrungKhoang ? Mau.xam : soLech === 0 ? Mau.xanhLa : Mau.do;
        const icon = !ket ? 'clock' : soLech === 0 && !ket.khongTrungKhoang ? 'check-circle' : 'alert-circle';

        return (
          <Pressable key={tho.id} style={kieu.theTho} onPress={() => onChon(tho.id)}>
            <Feather name={icon} size={18} color={mau} />
            <View style={kieu.giuaTheTho}>
              <Text style={kieu.chuTen} numberOfLines={1}>
                {tho.ten}
              </Text>
              <Text style={[kieu.chuPhu, { color: mau }]}>{chu}</Text>
            </View>
            <Feather name="chevron-right" size={18} color={Mau.xam} />
          </Pressable>
        );
      })}
    </>
  );
}

function ChiTiet({
  tenTho,
  benKia,
  ket,
  nhanLuc,
  maMoiCuaTho,
  onLay,
  onDoiTho,
}: {
  tenTho: string;
  benKia: string;
  ket: KetQuaDoiChieu | null;
  nhanLuc: string | null;
  maMoiCuaTho: string | null;
  onLay: (lech: DongLech) => void;
  onDoiTho?: () => void;
}) {
  return (
    <>
      <View style={kieu.theDau}>
        <View style={kieu.dongDau}>
          <Text style={kieu.chuTen} numberOfLines={1}>
            {tenTho}
          </Text>
          {onDoiTho !== undefined && (
            <NutChip nhan="Thợ khác" icon="users" onPress={onDoiTho} />
          )}
        </View>

        {ket !== null && !ket.khongTrungKhoang && (
          <>
            <Text style={kieu.chuPhu}>
              So từ {Ngay.ngayGon(ket.tuNgay)} đến {Ngay.ngayGon(ket.denNgay)}
              {nhanLuc !== null ? ` · sổ ${benKia} nhận lúc ${Ngay.gioPhut(nhanLuc)}` : ''}
            </Text>
            <View style={kieu.dongTong}>
              <Text style={kieu.chuTong}>
                Sổ tôi <Text style={kieu.chuTongSo}>{Ngay.soCong(ket.tongCongMinh)}</Text> công
              </Text>
              <Text style={kieu.chuTong}>
                Sổ {benKia} <Text style={kieu.chuTongSo}>{Ngay.soCong(ket.tongCongBenKia)}</Text> công
              </Text>
            </View>
          </>
        )}
      </View>

      {ket === null ? (
        <View style={kieu.trong}>
          <Feather name="inbox" size={34} color={Mau.xam} />
          <Text style={kieu.chuTrongTo}>Chưa có sổ của {benKia}</Text>
          <Text style={kieu.chuTrong}>
            {maMoiCuaTho !== null
              ? `Máy của thợ chưa gửi sổ lên. Nếu thợ chưa cài app, đọc cho họ mã mời: ${maMoiCuaTho}`
              : 'Chủ chưa gửi sổ xuống. Bấm mũi tên đồng bộ ở trên, hoặc nhắc chủ mở app.'}
          </Text>
        </View>
      ) : ket.khongTrungKhoang ? (
        <View style={kieu.trong}>
          <Feather name="calendar" size={34} color={Mau.xam} />
          <Text style={kieu.chuTrongTo}>Chưa so được</Text>
          <Text style={kieu.chuTrong}>
            Hai sổ không có ngày nào chung. Đợi thêm vài ngày chấm công rồi đối chiếu lại.
          </Text>
        </View>
      ) : ket.lechs.length === 0 ? (
        <View style={kieu.trong}>
          <Feather name="check-circle" size={34} color={Mau.xanhLa} />
          <Text style={kieu.chuTrongTo}>Hai sổ khớp nhau</Text>
          <Text style={kieu.chuTrong}>Cả {ket.soKhop} buổi hai bên ghi giống nhau.</Text>
        </View>
      ) : (
        ket.lechs.map((lech) => (
          <DongLechO
            key={`${lech.ngay}|${lech.buoi}`}
            lech={lech}
            benKia={benKia}
            onLay={() => onLay(lech)}
          />
        ))
      )}
    </>
  );
}

/** Một buổi hai bên nói khác nhau. Hai con số đứng cạnh nhau, không phải đọc chữ để suy. */
function DongLechO({
  lech,
  benKia,
  onLay,
}: {
  lech: DongLech;
  benKia: string;
  onLay: () => void;
}) {
  const chuSo = (so: number | null) => (so === null ? 'Chưa chấm' : `${Ngay.soCong(so)} công`);

  return (
    <View style={kieu.theLech}>
      <View style={kieu.dongDau}>
        <Text style={kieu.chuNgay}>
          {Ngay.thuVaNgay(lech.ngay)} · {TEN_BUOI[lech.buoi]}
        </Text>
        {lech.daChot ? (
          <View style={kieu.nhanChot}>
            <Text style={kieu.chuNhanChot}>Đã trả tiền</Text>
          </View>
        ) : (
          <NutChip nhan={`Lấy theo sổ ${benKia}`} icon="download" onPress={onLay} />
        )}
      </View>

      <View style={kieu.dongTong}>
        <View style={[kieu.oSo, kieu.oSoToi]}>
          <Text style={kieu.chuNhanO}>Sổ tôi</Text>
          <Text style={kieu.chuSoO}>{chuSo(lech.soCongMinh)}</Text>
        </View>
        <View style={[kieu.oSo, kieu.oSoBenKia]}>
          <Text style={kieu.chuNhanO}>Sổ {benKia}</Text>
          <Text style={kieu.chuSoO}>{chuSo(lech.soCongBenKia)}</Text>
        </View>
      </View>
    </View>
  );
}

const kieu = StyleSheet.create({
  khung: { flex: 1, backgroundColor: Mau.nen },
  than: { padding: 16, paddingTop: 4, gap: 12 },

  nutDongBo: {
    width: 44,
    height: 44,
    borderRadius: Co.bo,
    backgroundColor: Mau.trang,
    alignItems: 'center',
    justifyContent: 'center',
    ...Bong.the,
  },

  theNhac: { ...theTrang, gap: 12, alignItems: 'flex-start' },
  chuNhac: { fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuLoi: { color: Mau.do },

  theTho: { ...theTrang, flexDirection: 'row', alignItems: 'center', gap: 12 },
  giuaTheTho: { flex: 1, gap: 3 },
  chuTen: { flex: 1, fontSize: Co.chuTen, fontFamily: PhongChu.dam, color: Mau.chu },
  chuPhu: { fontSize: Co.chuPhu, fontFamily: PhongChu.thuong, color: Mau.xam },

  theDau: { ...theTrang, gap: 8 },
  dongDau: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  dongTong: { flexDirection: 'row', gap: 10 },
  chuTong: { flex: 1, fontSize: Co.chuThuong, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuTongSo: { fontFamily: PhongChu.dam, color: Mau.chu },

  theLech: { ...theTrang, gap: 10 },
  chuNgay: { flex: 1, fontSize: Co.chuSo, fontFamily: PhongChu.vua, color: Mau.chu },

  // Hai ô số nền nhạt đứng cạnh nhau: nhìn một cái là thấy bên nào nhiều hơn.
  oSo: { flex: 1, gap: 4, padding: 10, borderRadius: Co.boNho, borderWidth: 1 },
  oSoToi: { backgroundColor: Mau.chinhNhat, borderColor: Tuoi.chinh },
  oSoBenKia: { backgroundColor: Mau.ngocNhat, borderColor: Tuoi.ngoc },
  chuNhanO: { fontSize: Co.chuNho, fontFamily: PhongChu.thuong, color: Mau.xam },
  chuSoO: { fontSize: Co.chuSo, fontFamily: PhongChu.dam, color: Mau.chu },

  nhanChot: {
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: Co.boNho,
    backgroundColor: Mau.nen,
  },
  chuNhanChot: { fontSize: Co.chuNho, fontFamily: PhongChu.vua, color: Mau.xam },

  trong: { padding: 24, paddingTop: 40, gap: 10, alignItems: 'center' },
  chuTrongTo: { fontSize: Co.chuTieuDe, fontFamily: PhongChu.dam, color: Mau.chu },
  chuTrong: {
    fontSize: Co.chuThuong,
    fontFamily: PhongChu.thuong,
    color: Mau.xam,
    textAlign: 'center',
  },
});
