import { Feather } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { DongLech, KetQuaDoiChieu, doiChieu, layTheoBenKia } from '../nghiepvu/doiChieu';
import { BuoiLam, DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import { soCuaMay } from '../nghiepvu/soCong';
import { thoDangLam, timTho } from '../nghiepvu/thaoTac';
import { CaiDatVai } from '../nghiepvu/vaiMay';
import { DieuKhienDoiChieu } from './dungDoiChieu';
import { DieuKhienNhom } from './dungSupabase';
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
  /** Để máy chủ phát mã mời ngay tại chỗ nhìn ra "thợ này chưa gửi sổ". */
  nhom: DieuKhienNhom;
  onDong?: () => void;
}

/** Việc phát mã mời, gói lại một chỗ để khỏi rải bốn tham số xuống `ChiTiet`. */
interface ViecPhatMa {
  chay: () => void;
  /** Mã vừa phát cho đúng thợ đang xem, null là chưa phát. */
  ma: string | null;
  dangChay: boolean;
  loi: string | null;
}

const TEN_BUOI: Record<BuoiLam, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

export function ManHinhDoiChieu({ duLieu, capNhat, caiDat, dieuKhien, nhom, onDong }: Props) {
  const { trangThai, soBenKia, dongBo } = dieuKhien;
  const { ketNoi } = trangThai;

  /** Máy thợ chỉ có một người nên mở thẳng vào chi tiết, không qua danh sách. */
  const [dangXem, datDangXem] = useState<string | null>(
    caiDat.vai === 'tho' ? caiDat.thoId : null,
  );
  const [loiLay, datLoiLay] = useState<string | null>(null);
  /** Mã vừa phát, giữ kèm thợ nào — đổi sang thợ khác thì mã cũ không còn nghĩa gì. */
  const [maVuaPhat, datMaVuaPhat] = useState<{ thoId: string; ma: string } | null>(null);

  const homNay = Ngay.homNay();
  const benKia = caiDat.vai === 'chu' ? 'thợ' : 'chủ';

  /**
   * Phát mã mời cho một thợ. Đặt ở màn hình này chứ không ở danh sách thợ: đây là chỗ chủ
   * nhìn ra "thợ này chưa gửi sổ", tức là đúng lúc cần cái mã.
   */
  async function phatMaCho(thoId: string) {
    const ma = await nhom.phatMa(thoId);
    if (ma !== null) {
      datMaVuaPhat({ thoId, ma });
    }
  }

  /** Kết quả đối chiếu của từng thợ. Thợ chưa gửi sổ thì không có trong bảng này. */
  const ketTheoTho = useMemo(() => {
    const bang = new Map<string, KetQuaDoiChieu>();
    for (const [thoId, daNhan] of soBenKia) {
      bang.set(thoId, doiChieu(soCuaMay(duLieu, caiDat, thoId, homNay), daNhan.so, homNay));
    }
    return bang;
  }, [duLieu, caiDat, soBenKia, homNay]);

  /**
   * Trên máy chủ, tên trong sổ mình là tên chuẩn — chủ mới là bên đặt tên. Trên máy thợ thì
   * ngược lại: tên nội bộ chỉ là chữ "Tôi" đặt tạm lúc nhận mã mời, còn tên thật nằm trong
   * sổ chủ gửi xuống. Lấy sai thứ tự thì màn hình chính gọi "Anh Tuấn" mà mở đối chiếu ra
   * lại thành "Tôi", cùng một người mà hai tên.
   */
  function tenCuaTho(thoId: string): string {
    const trongSoMinh = timTho(duLieu, thoId)?.ten;
    const trongSoBenKia = soBenKia.get(thoId)?.so.tenTho;
    const uuTien = caiDat.vai === 'chu' ? [trongSoMinh, trongSoBenKia] : [trongSoBenKia, trongSoMinh];
    return uuTien.find((ten) => ten !== undefined && ten !== '') ?? 'Thợ';
  }

  function layMotDong(thoId: string, lech: DongLech) {
    try {
      capNhat(layTheoBenKia(duLieu, thoId, lech));
      datLoiLay(null);
    } catch {
      datLoiLay('Buổi này đã nằm trong kỳ đã quyết toán, không sửa được nữa.');
    }
  }

  const chuTrangThai = !ketNoi.sanSang
    ? (ketNoi.chuaSanSang ?? 'Chưa nối hộp thư')
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
          ketNoi.sanSang ? (
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
        {/*
          Chưa nối được hộp thư thì nói **cách sửa**, không chỉ nói là chưa nối. Câu chỉ đường
          do bên chọn hộp thư đưa vào, vì chỉ bên ấy biết đang chạy trên đường nào.
        */}
        {!ketNoi.sanSang && (
          <View style={kieu.theNhac}>
            <Text style={kieu.chuNhac}>{ketNoi.chuaSanSang ?? 'Chưa nối hộp thư nào.'}</Text>
            {ketNoi.noi !== undefined && (
              <NutChip nhan="Nối ngay" icon="link" onPress={ketNoi.noi} />
            )}
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
            tenTho={tenCuaTho(dangXem)}
            benKia={benKia}
            ket={ketTheoTho.get(dangXem) ?? null}
            nhanLuc={soBenKia.get(dangXem)?.suaLuc ?? null}
            phatMa={
              caiDat.vai === 'chu'
                ? {
                    chay: () => void phatMaCho(dangXem),
                    ma: maVuaPhat?.thoId === dangXem ? maVuaPhat.ma : null,
                    dangChay: nhom.trangThai.dangChay,
                    loi: nhom.trangThai.loi,
                  }
                : undefined
            }
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

        // Chưa lệch mà cũng chưa khớp buổi nào thì chưa so được gì, đừng tô xanh — xem ghi
        // chú cùng chuyện ấy ở `ChiTiet`.
        const chuaSoDuoc = !ket || ket.khongTrungKhoang || (soLech === 0 && ket.soKhop === 0);

        const chu = !ket
          ? 'Chưa gửi sổ lên'
          : ket.khongTrungKhoang
            ? 'Sổ hai bên chưa có ngày nào chung'
            : soLech === 0 && ket.soKhop === 0
              ? 'Chưa có buổi nào so được'
              : soLech === 0
                ? `Khớp cả ${ket.soKhop} buổi`
                : `Lệch ${soLech} buổi · khớp ${ket.soKhop}`;

        const mau = chuaSoDuoc ? Mau.xam : soLech === 0 ? Mau.xanhLa : Mau.do;
        const icon = chuaSoDuoc ? 'clock' : soLech === 0 ? 'check-circle' : 'alert-circle';

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
  phatMa,
  onLay,
  onDoiTho,
}: {
  tenTho: string;
  benKia: string;
  ket: KetQuaDoiChieu | null;
  nhanLuc: string | null;
  /** Chỉ máy chủ có; máy thợ không phát mã cho ai. */
  phatMa?: ViecPhatMa;
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
            {/*
              Nói ra chỗ đang tạm gác, đừng gác lặng lẽ: hai tổng ở trên không có mấy buổi ấy,
              mà màn hình chấm công thì có — không giải thích thì thành hai chỗ nói hai số.
            */}
            {ket.soTamGac > 0 && (
              <Text style={kieu.chuPhu}>
                Hôm nay còn dở: {ket.soTamGac} buổi mới một bên chấm, chưa tính là lệch.
              </Text>
            )}
          </>
        )}
      </View>

      {ket === null ? (
        <View style={kieu.trong}>
          <Feather name="inbox" size={34} color={Mau.xam} />
          <Text style={kieu.chuTrongTo}>Chưa có sổ của {benKia}</Text>

          {phatMa === undefined ? (
            <Text style={kieu.chuTrong}>
              Chủ chưa gửi sổ xuống. Bấm mũi tên đồng bộ ở trên, hoặc nhắc chủ mở app.
            </Text>
          ) : phatMa.ma !== null ? (
            <>
              {/*
                Mã hiện to, giãn chữ: chủ phải đọc nó qua điện thoại cho một người đang ở
                công trường. Mã database sinh ra đã bỏ O, I, L, số 0 và 1 vì đọc lên nghe
                giống nhau — phần còn lại là việc của cỡ chữ.
              */}
              <Text style={kieu.chuMa}>{phatMa.ma}</Text>
              <Text style={kieu.chuTrong}>
                Đọc mã này cho thợ, hoặc gửi qua Zalo. Thợ mở app → mục Thợ → Máy của thợ →
                dán mã. Mã dùng một lần và sống ba ngày.
              </Text>
            </>
          ) : (
            <>
              <Text style={kieu.chuTrong}>
                Máy của thợ chưa gửi sổ lên. Thợ chưa cài app thì phát mã mời cho họ.
              </Text>
              <Pressable
                style={[kieu.nutPhatMa, phatMa.dangChay && kieu.nutMo]}
                onPress={phatMa.chay}
                disabled={phatMa.dangChay}
                accessibilityRole="button"
              >
                {phatMa.dangChay ? (
                  <ActivityIndicator color={Mau.trang} />
                ) : (
                  <Feather name="user-plus" size={16} color={Mau.trang} />
                )}
                <Text style={kieu.chuNutPhatMa}>
                  {phatMa.dangChay ? 'Đang phát mã…' : 'Phát mã mời'}
                </Text>
              </Pressable>
              {phatMa.loi !== null && (
                <Text style={[kieu.chuTrong, kieu.chuLoi]}>{phatMa.loi}</Text>
              )}
            </>
          )}
        </View>
      ) : ket.khongTrungKhoang ? (
        <View style={kieu.trong}>
          <Feather name="calendar" size={34} color={Mau.xam} />
          <Text style={kieu.chuTrongTo}>Chưa so được</Text>
          <Text style={kieu.chuTrong}>
            Hai sổ không có ngày nào chung. Đợi thêm vài ngày chấm công rồi đối chiếu lại.
          </Text>
        </View>
      ) : ket.lechs.length === 0 && ket.soKhop === 0 ? (
        /*
          Không lệch mà cũng chưa khớp buổi nào thì **đừng nói "hai sổ khớp nhau"**: câu ấy là
          một lời bảo đảm, mà ở đây chưa so được gì cả. Hay gặp nhất là máy thợ vừa cài xong,
          khoảng chung chỉ có đúng hôm nay.
        */
        <View style={kieu.trong}>
          <Feather name="clock" size={34} color={Mau.xam} />
          <Text style={kieu.chuTrongTo}>Chưa có gì để so</Text>
          <Text style={kieu.chuTrong}>
            {ket.soTamGac > 0
              ? `Hôm nay còn đang trong ngày: sổ ${benKia} đã chấm, sổ tôi chưa (hoặc ngược lại). Chấm xong rồi mở lại đây.`
              : 'Trong khoảng hai sổ cùng khai, chưa buổi nào được chấm.'}
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

  /* Mã mời: to, giãn chữ, đậm — chủ phải đọc nó qua điện thoại cho thợ. */
  chuMa: {
    fontSize: 34,
    fontFamily: PhongChu.dam,
    color: Mau.chinh,
    letterSpacing: 4,
    paddingVertical: 2,
  },
  nutPhatMa: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    minHeight: Co.caoNut,
    paddingVertical: 10,
    paddingHorizontal: 18,
    borderRadius: Co.bo,
    backgroundColor: Mau.chinh,
  },
  nutMo: { opacity: 0.6 },
  chuNutPhatMa: { fontSize: Co.chuNut, fontFamily: PhongChu.vua, color: Mau.trang },

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
