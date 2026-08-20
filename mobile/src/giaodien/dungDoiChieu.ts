/**
 * Điều phối hộp thư: gửi sổ của máy này đi, nhận sổ bên kia về, giữ bản chụp trong máy.
 *
 * Chạy ngầm sau mỗi lần đổi dữ liệu, giống sao lưu — chỉ là chờ lâu hơn một chút.
 *
 * Trước đây **chỉ chạy khi người dùng bấm nút đồng bộ**, với lý do tiết kiệm 3G: đối chiếu
 * là việc cuối ngày, đẩy đi sau từng ô chấm là đẩy những con số chưa ai xem. Nghe đúng mà
 * thực tế thì sai, và sai về đúng phía tệ nhất: nút đồng bộ nằm trong màn hình Đối chiếu,
 * mà chủ chấm công cả ngày thì không có việc gì mở màn hình ấy ra. Chủ nhập xong, sổ nằm
 * im trong máy; thợ mở app lên vẫn thấy sổ của lần chủ tình cờ vào Đối chiếu gần nhất, rồi
 * đối chiếu báo lệch những buổi chẳng ai ghi sai. Người dùng không có cách nào biết là mình
 * còn thiếu một cú bấm — trên màn hình Chấm công không có dấu hiệu nào cả.
 *
 * Nên giờ đẩy ngầm, và giữ phần tiết kiệm bằng hai cách rẻ hơn nhiều so với bắt người dùng
 * nhớ bấm: chờ yên tĩnh (`CHO_YEN`) để cả một lượt chấm chỉ thành một lượt gửi, và **chỉ gửi
 * sổ nào đổi so với lần gửi trước** (`guiNeuDoi`) — chấm cho một thợ thì chỉ một sổ đi lên,
 * không phải cả nhóm.
 *
 * Sổ bên kia nhận về được lưu xuống máy ngay, nên mở app ra là xem đối chiếu được kể cả
 * lúc mất mạng — chỉ là số liệu tính đến lần đồng bộ gần nhất, và màn hình nói rõ điều đó.
 */

import AsyncStorage from '@react-native-async-storage/async-storage';
import { useCallback, useEffect, useRef, useState } from 'react';

import { HopThu, SoDaNhan } from '../nghiepvu/hopThu';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import * as SoBenKia from '../nghiepvu/soBenKia';
import { soCuaMay } from '../nghiepvu/soCong';
import { thoDangLam } from '../nghiepvu/thaoTac';
import { CaiDatVai } from '../nghiepvu/vaiMay';

const KHOA_LAN_CUOI = 'chamcong.hopthu.lancuoi.v1';

/**
 * Đổi xong chờ 45 giây yên tĩnh mới đẩy lên.
 *
 * Lâu hơn sao lưu (20 giây) vì đây là gọi mạng, không phải ghi xuống máy: chấm cho mười thợ
 * là mười lần bấm cách nhau vài giây, chờ dài hơn thì cả lượt ấy gói vào một lượt gửi. Đừng
 * nới thêm nữa — thợ đứng cạnh chủ hỏi "sổ tôi lên chưa" thì phút rưỡi đã là lâu.
 */
const CHO_YEN = 45_000;

/**
 * Hộp thư đang dùng đã nối được chưa, và nếu chưa thì nói gì với người dùng.
 *
 * Hook này **không tự đi hỏi bên nào cả**. Trước đây nó tự hỏi tài khoản Google, nên lúc hộp
 * thư chuyển sang Supabase thì màn hình Đối chiếu vẫn báo "cần nối Google" rồi ẩn luôn nút
 * đồng bộ — sổ có nơi để gửi mà không có nút để bấm. Ai chọn hộp thư thì người đó nói luôn
 * trạng thái của nó.
 */
export interface KetNoiHopThu {
  /** Hộp thư dùng được chưa. */
  sanSang: boolean;
  /** Câu chỉ đường khi chưa sẵn sàng, ví dụ "vào mục Thợ để nối nhóm". */
  chuaSanSang: string | null;
  /** Nối được ngay tại màn hình đối chiếu thì đưa hàm vào; không thì để trống. */
  noi?: () => Promise<void>;
}

export interface TrangThaiDoiChieu {
  ketNoi: KetNoiHopThu;
  dangChay: boolean;
  /** Lần đồng bộ xong gần nhất, dạng ISO. */
  lucCuoi: string | null;
  loi: string | null;
}

export interface DieuKhienDoiChieu {
  trangThai: TrangThaiDoiChieu;
  /** Sổ bên kia đang giữ trong máy, tra theo thoId. */
  soBenKia: Map<string, SoDaNhan>;
  /** Gửi sổ của máy này rồi nhận sổ bên kia. */
  dongBo: () => Promise<void>;
}

/**
 * `hopThu` nhận từ ngoài — không có bản mặc định — để đổi ruột hộp thư mà không phải sửa
 * hook, và để bài kiểm thử giao diện đưa hộp thư giả vào. Ai chọn hộp thư thì người đó đưa
 * vào; hook này không được tự dựng một cái nào.
 */
export function dungDoiChieu(
  duLieu: DuLieuChamCong | null,
  caiDat: CaiDatVai,
  hopThu: HopThu,
  ketNoi: KetNoiHopThu = { sanSang: false, chuaSanSang: 'Chưa nối hộp thư nào.' },
): DieuKhienDoiChieu {
  const [dangChay, datDangChay] = useState(false);
  const [lucCuoi, datLucCuoi] = useState<string | null>(null);
  const [loi, datLoi] = useState<string | null>(null);
  const [soBenKia, datSoBenKia] = useState<Map<string, SoDaNhan>>(new Map());

  /** Dữ liệu mới nhất giữ trong ref: hàm `dongBo` không phải dựng lại mỗi lần chấm một ô. */
  const moiNhat = useRef(duLieu);
  moiNhat.current = duLieu;

  const caiDatMoiNhat = useRef(caiDat);
  caiDatMoiNhat.current = caiDat;

  /**
   * Dấu của sổ đã gửi lần trước, tra theo (thợ, bên gửi). Chỉ giữ trong lượt mở app này —
   * mở app lại thì gửi lại một lượt, mà lượt ấy vốn đã có sẵn (đồng bộ lúc mở app).
   */
  const daGui = useRef(new Map<string, string>());

  /**
   * Gửi một sổ, nhưng bỏ qua nếu nội dung y hệt lần gửi trước.
   *
   * Đây là chỗ giữ cho việc đẩy ngầm không tốn 3G: chủ chấm cho một thợ thì chín sổ kia
   * không đổi một chữ, gửi lại chúng là chín lần gọi mạng để ghi đúng thứ hộp thư đang có.
   *
   * `taoLuc` **không tính vào dấu**: nó đổi mỗi lượt mà không phải nội dung sổ, tính vào là
   * dấu nào cũng khác và cả bộ so này thành vô dụng. Gửi hụt thì không ghi dấu, nên lượt sau
   * thử lại.
   */
  const guiNeuDoi = useCallback(
    async (so: Parameters<HopThu['gui']>[0]) => {
      const khoa = `${so.thoId}|${so.nguon}`;
      const dau = JSON.stringify({
        tenTho: so.tenTho,
        tuNgay: so.tuNgay,
        denNgay: so.denNgay,
        dongs: so.dongs,
      });
      if (daGui.current.get(khoa) === dau) {
        return;
      }

      await hopThu.gui(so);
      daGui.current.set(khoa, dau);
    },
    [hopThu],
  );

  useEffect(() => {
    SoBenKia.doc().then(datSoBenKia);
    AsyncStorage.getItem(KHOA_LAN_CUOI).then(datLucCuoi);
  }, []);

  const dongBo = useCallback(async () => {
    const hienTai = moiNhat.current;
    const vai = caiDatMoiNhat.current;
    if (!hienTai) {
      return;
    }

    datDangChay(true);
    try {
      const homNay = Ngay.homNay();
      const taoLuc = new Date().toISOString();

      if (vai.vai === 'chu') {
        // Chỉ gửi sổ cho thợ **đang làm**: thợ đã nghỉ thì máy họ không còn ai xem, mà mỗi
        // sổ là một lần gọi mạng.
        // Cắt qua `soCuaMay` y như màn hình đối chiếu, không tự dựng khoảng ngày ở đây: hai
        // chỗ cắt theo hai kiểu là sổ gửi lên nhóm khai khác sổ đang hiện trên máy.
        for (const tho of thoDangLam(hienTai)) {
          await guiNeuDoi(soCuaMay(hienTai, vai, tho.id, homNay, taoLuc));
        }
        datSoBenKia(await SoBenKia.luu(await hopThu.docSoCacTho()));
      } else if (vai.thoId !== null) {
        // Máy thợ khai đúng từ ngày nó bắt đầu chấm — trước đó nó không biết gì, xem ghi
        // chú ở `CaiDatVai.batDauTu`.
        await guiNeuDoi(soCuaMay(hienTai, vai, vai.thoId, homNay, taoLuc));

        const cuaChu = await hopThu.doc(vai.thoId, 'chu');
        datSoBenKia(await SoBenKia.luu(cuaChu ? [cuaChu] : []));
      }

      const xong = new Date().toISOString();
      await AsyncStorage.setItem(KHOA_LAN_CUOI, xong);
      datLucCuoi(xong);
      datLoi(null);
    } catch (loiChay) {
      if (loiChay instanceof Error && loiChay.message !== '') {
        // Lỗi từ hộp thư đã là câu viết cho người dùng (xem nhomSupabase, hopThuSupabase),
        // nên hiện thẳng. Che nó thành "thử lại sau" là bỏ mất chỗ chỉ đường duy nhất.
        datLoi(loiChay.message);
      } else {
        datLoi('Chưa đồng bộ được. Kiểm tra mạng rồi bấm lại.');
      }
    } finally {
      datDangChay(false);
    }
  }, [hopThu, guiNeuDoi]);

  /**
   * Đồng bộ một lần lúc mở app, nếu hộp thư đã sẵn sàng. Lượt này lấy sổ bên kia về để mở
   * ra là xem đối chiếu được ngay, và đẩy lên những gì lượt trước chưa đẩy kịp.
   */
  const daDongBoLanDau = useRef(false);

  useEffect(() => {
    if (!ketNoi.sanSang || duLieu === null || daDongBoLanDau.current) {
      return;
    }
    daDongBoLanDau.current = true;
    dongBo();
  }, [ketNoi.sanSang, duLieu, dongBo]);

  /**
   * Chấm xong ngồi im một lát là sổ tự lên nhóm — không phải nhớ vào Đối chiếu bấm nút.
   *
   * Gọi thẳng `dongBo` chứ không viết riêng một đường *chỉ gửi*: `lucCuoi` là thứ màn hình
   * đọc lên thành "Đã gửi sổ lúc 14:32", mà một đường gửi mà không nhận thì câu ấy nói quá —
   * sổ mình đã lên nhưng sổ bên kia đang cầm vẫn là bản cũ, và đối chiếu sẽ so với bản cũ ấy.
   * Phần nhận về chỉ thêm đúng một lần gọi mạng, còn phần gửi thì `guiNeuDoi` đã lọc.
   *
   * Bỏ qua lần chạy đầu như bên sao lưu: lúc ấy dữ liệu vừa đọc lên từ máy chứ chưa ai sửa
   * gì, mà lượt đồng bộ lúc mở app ở trên đã gửi rồi.
   */
  const daBoQuaLanDau = useRef(false);

  useEffect(() => {
    if (!ketNoi.sanSang || duLieu === null) {
      return;
    }
    if (!daBoQuaLanDau.current) {
      daBoQuaLanDau.current = true;
      return;
    }

    const hen = setTimeout(dongBo, CHO_YEN);
    return () => clearTimeout(hen);
  }, [duLieu, ketNoi.sanSang, dongBo]);

  return {
    trangThai: { ketNoi, dangChay, lucCuoi, loi },
    soBenKia,
    dongBo,
  };
}
