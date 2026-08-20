/**
 * Điều phối hộp thư — phần *khi nào sổ được đẩy lên*.
 *
 * Đây là chỗ từng làm mất công thật, và mất một cách không ai thấy: sổ chỉ lên nhóm khi
 * người dùng bấm nút trong màn hình Đối chiếu, mà chủ chấm công cả ngày thì không mở màn
 * hình ấy ra. Nhìn code không ra lỗi, nhìn màn hình cũng không — chỉ có bài kiểm thử này
 * nói được là chấm xong thì sổ có lên hay không.
 */

import { act, renderHook } from '@testing-library/react-native';

import { HopThu, SoDaNhan } from '../../nghiepvu/hopThu';
import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { SoCong } from '../../nghiepvu/soCong';
import { cham, themTho } from '../../nghiepvu/thaoTac';
import { CaiDatVai } from '../../nghiepvu/vaiMay';
import { KetNoiHopThu, dungDoiChieu } from '../dungDoiChieu';

const DA_NOI: KetNoiHopThu = { sanSang: true, chuaSanSang: null };
const CHUA_NOI: KetNoiHopThu = { sanSang: false, chuaSanSang: 'Chưa nối nhóm.' };

const MAY_CHU: CaiDatVai = {
  vai: 'chu',
  thoId: null,
  batDauTu: null,
  thoIdTuTao: false,
  dungMotMinh: false,
};

/** Hộp thư giả: ghi lại từng sổ được gửi để đếm. */
function hopThuGia(): HopThu & { daGui: SoCong[] } {
  const daGui: SoCong[] = [];
  return {
    daGui,
    async gui(so) {
      daGui.push(so);
    },
    async doc(): Promise<SoDaNhan | null> {
      return null;
    },
    async docSoCacTho(): Promise<SoDaNhan[]> {
      return [];
    },
  };
}

function soCuaChu(): { duLieu: DuLieuChamCong; thoId: string } {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  return { duLieu, thoId: tho.id };
}

/**
 * Chờ hết `CHO_YEN` rồi để mấy lời hứa trong `dongBo` chạy xong.
 *
 * Phải chạy đồng hồ *trong* `act` và chờ thêm một nhịp: `dongBo` là hàm async, hẹn giờ chỉ
 * bắt đầu nó chứ không chờ nó xong.
 */
async function choDayLen() {
  await act(async () => {
    jest.advanceTimersByTime(60_000);
  });
}

beforeEach(() => {
  jest.useFakeTimers();
});

afterEach(() => {
  jest.useRealTimers();
});

describe('tự đẩy sổ sau khi chủ nhập', () => {
  test('chủ chấm xong, ngồi im một lát là sổ lên nhóm — không cần bấm nút nào', async () => {
    const hopThu = hopThuGia();
    const { duLieu, thoId } = soCuaChu();

    const { rerender } = renderHook(
      ({ d }: { d: DuLieuChamCong }) => dungDoiChieu(d, MAY_CHU, hopThu, DA_NOI),
      { initialProps: { d: duLieu } },
    );

    // Lượt đồng bộ lúc mở app.
    await choDayLen();
    const sauKhiMo = hopThu.daGui.length;

    // Chủ chấm một buổi.
    const daCham = cham(duLieu, thoId, Ngay.homNay(), 'Sang', 1);
    await act(async () => {
      rerender({ d: daCham });
    });

    // Chưa hết giờ chờ thì chưa gửi: cả một lượt chấm phải gói vào một lượt gửi.
    await act(async () => {
      jest.advanceTimersByTime(5_000);
    });
    expect(hopThu.daGui.length).toBe(sauKhiMo);

    await choDayLen();

    const sauKhiCham = hopThu.daGui[hopThu.daGui.length - 1];
    expect(hopThu.daGui.length).toBeGreaterThan(sauKhiMo);
    expect(sauKhiCham.dongs).toEqual([{ ngay: Ngay.homNay(), buoi: 'Sang', soCong: 1 }]);
  });

  test('sổ không đổi thì không gửi lại — chấm cho một thợ không đẩy cả nhóm lên', async () => {
    const hopThu = hopThuGia();
    let d = duLieuRong();
    const thos: string[] = [];
    for (const ten of ['Anh Tuấn', 'Anh Bình', 'Anh Cường']) {
      const them = themTho(d, ten, 300_000, '2026-08-01');
      d = them.duLieu;
      thos.push(them.tho.id);
    }

    const { rerender } = renderHook(
      ({ du }: { du: DuLieuChamCong }) => dungDoiChieu(du, MAY_CHU, hopThu, DA_NOI),
      { initialProps: { du: d } },
    );

    await choDayLen();
    // Lượt mở app gửi sổ của cả ba thợ, vì chưa gửi sổ nào lần nào.
    expect(hopThu.daGui.length).toBe(3);

    const daCham = cham(d, thos[1], Ngay.homNay(), 'Sang', 1);
    await act(async () => {
      rerender({ du: daCham });
    });
    await choDayLen();

    // Chỉ đúng một sổ đi lên, và là sổ của người vừa được chấm.
    expect(hopThu.daGui.length).toBe(4);
    expect(hopThu.daGui[3].thoId).toBe(thos[1]);
  });

  test('chưa nối nhóm thì không hẹn giờ gửi gì cả', async () => {
    const hopThu = hopThuGia();
    const { duLieu, thoId } = soCuaChu();

    const { rerender } = renderHook(
      ({ d }: { d: DuLieuChamCong }) => dungDoiChieu(d, MAY_CHU, hopThu, CHUA_NOI),
      { initialProps: { d: duLieu } },
    );

    await act(async () => {
      rerender({ d: cham(duLieu, thoId, Ngay.homNay(), 'Sang', 1) });
    });
    await choDayLen();

    expect(hopThu.daGui).toEqual([]);
  });
});
