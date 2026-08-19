/**
 * Điều phối hộp thư: gửi sổ của máy này đi, nhận sổ bên kia về, giữ bản chụp trong máy.
 *
 * Khác hẳn sao lưu Drive ở một chỗ: sao lưu chạy ngầm sau mỗi lần đổi dữ liệu, còn đồng bộ
 * hộp thư thì **chỉ chạy khi người dùng mở màn hình đối chiếu hoặc bấm nút**. Đối chiếu là
 * việc làm cuối ngày hay cuối kỳ, không phải việc từng phút; chạy ngầm liên tục chỉ tốn 3G
 * của cả nhóm để đẩy đi những con số chưa ai xem.
 *
 * Sổ bên kia nhận về được lưu xuống máy ngay, nên mở app ra là xem đối chiếu được kể cả
 * lúc mất mạng — chỉ là số liệu tính đến lần đồng bộ gần nhất, và màn hình nói rõ điều đó.
 */

import AsyncStorage from '@react-native-async-storage/async-storage';
import { useCallback, useEffect, useRef, useState } from 'react';

import { ChuaDangNhap, HetPhien } from '../nghiepvu/dangNhapGoogle';
import * as Google from '../nghiepvu/dangNhapGoogle';
import { HopThu, SoDaNhan, hopThuDrive } from '../nghiepvu/hopThu';
import { DuLieuChamCong } from '../nghiepvu/kieu';
import * as Ngay from '../nghiepvu/ngayViet';
import * as SoBenKia from '../nghiepvu/soBenKia';
import { catSo, cuaSoCuaChu, soCuaMay } from '../nghiepvu/soCong';
import { thoDangLam } from '../nghiepvu/thaoTac';
import { CaiDatVai } from '../nghiepvu/vaiMay';

const KHOA_LAN_CUOI = 'chamcong.hopthu.lancuoi.v1';

export interface TrangThaiDoiChieu {
  /** Máy này nối được Google không (Expo Go và web thì không). */
  hoTro: boolean;
  daNoi: boolean;
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
  noiGoogle: () => Promise<void>;
}

/**
 * `hopThu` nhận từ ngoài để sau này thay ruột Drive bằng máy chủ có phân quyền mà không
 * phải sửa hook, và để bài kiểm thử giao diện đưa hộp thư giả vào.
 */
export function dungDoiChieu(
  duLieu: DuLieuChamCong | null,
  caiDat: CaiDatVai,
  hopThu: HopThu = hopThuDrive(),
): DieuKhienDoiChieu {
  const [daNoi, datDaNoi] = useState(false);
  const [dangChay, datDangChay] = useState(false);
  const [lucCuoi, datLucCuoi] = useState<string | null>(null);
  const [loi, datLoi] = useState<string | null>(null);
  const [soBenKia, datSoBenKia] = useState<Map<string, SoDaNhan>>(new Map());

  const hoTro = Google.hoTro();

  /** Dữ liệu mới nhất giữ trong ref: hàm `dongBo` không phải dựng lại mỗi lần chấm một ô. */
  const moiNhat = useRef(duLieu);
  moiNhat.current = duLieu;

  const caiDatMoiNhat = useRef(caiDat);
  caiDatMoiNhat.current = caiDat;

  useEffect(() => {
    SoBenKia.doc().then(datSoBenKia);
    AsyncStorage.getItem(KHOA_LAN_CUOI).then(datLucCuoi);
    if (hoTro) {
      Google.taiKhoanDaLuu().then((taiKhoan) => datDaNoi(taiKhoan !== null));
    }
  }, [hoTro]);

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
        const { tuNgay, denNgay } = cuaSoCuaChu(homNay);
        for (const tho of thoDangLam(hienTai)) {
          await hopThu.gui(catSo(hienTai, tho.id, 'chu', tuNgay, denNgay, taoLuc));
        }
        datSoBenKia(await SoBenKia.luu(await hopThu.docSoCacTho()));
      } else if (vai.thoId !== null) {
        // Máy thợ khai đúng từ ngày nó bắt đầu chấm — trước đó nó không biết gì, xem ghi
        // chú ở `CaiDatVai.batDauTu`.
        await hopThu.gui(soCuaMay(hienTai, vai, vai.thoId, homNay, taoLuc));

        const cuaChu = await hopThu.doc(vai.thoId, 'chu');
        datSoBenKia(await SoBenKia.luu(cuaChu ? [cuaChu] : []));
      }

      const xong = new Date().toISOString();
      await AsyncStorage.setItem(KHOA_LAN_CUOI, xong);
      datLucCuoi(xong);
      datLoi(null);
    } catch (loiChay) {
      if (loiChay instanceof ChuaDangNhap) {
        datDaNoi(false);
        datLoi(null);
      } else if (loiChay instanceof HetPhien) {
        datDaNoi(false);
        datLoi('Kết nối Google đã hết hạn. Bấm Nối lại.');
      } else {
        datLoi('Chưa đồng bộ được. Kiểm tra mạng rồi bấm lại.');
      }
    } finally {
      datDangChay(false);
    }
  }, [hopThu]);

  /**
   * Đồng bộ một lần lúc mở app, nếu đã nối Google.
   *
   * Đúng một lần, không phải sau mỗi lần chấm như sao lưu: sổ đối chiếu là để *ngồi soát
   * với nhau*, mà lúc soát thì người ta bấm mũi tên đồng bộ. Đẩy đi sau từng ô chấm chỉ
   * tốn 3G của cả nhóm cho những con số chưa ai xem.
   */
  const daDongBoLanDau = useRef(false);

  useEffect(() => {
    if (!daNoi || duLieu === null || daDongBoLanDau.current) {
      return;
    }
    daDongBoLanDau.current = true;
    dongBo();
  }, [daNoi, duLieu, dongBo]);

  const noiGoogle = useCallback(async () => {
    datLoi(null);
    try {
      const moi = await Google.dangNhap();
      if (moi) {
        datDaNoi(true);
        await dongBo();
      }
    } catch {
      datLoi('Chưa nối được với Google. Kiểm tra mạng rồi thử lại.');
    }
  }, [dongBo]);

  return {
    trangThai: { hoTro, daNoi, dangChay, lucCuoi, loi },
    soBenKia,
    dongBo,
    noiGoogle,
  };
}
