/**
 * Sao lưu lên tài khoản — phần *khi nào được đẩy lên*.
 *
 * Bài này canh đúng một chỗ, và là chỗ nguy hiểm nhất của cả tính năng: **máy mới đăng nhập
 * không được đẩy sổ trống lên đè bản thật.** Chủ đổi điện thoại, đăng nhập tài khoản cũ, sổ
 * trong máy trống — mà lượt đẩy thì chạy ngầm sau hai phút. Không có luật chặn thì đúng cái
 * bản sổ họ đang đi tìm bị xoá bởi một lượt tự động, không ai bấm gì cả.
 *
 * Nhìn code không thấy được điều đó, nhìn màn hình cũng không: nó chỉ hiện ra mấy hôm sau,
 * lúc người dùng vào tìm bản cũ.
 */

import { act, renderHook } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { BanTaiKhoan, KhoTaiKhoan } from '../../nghiepvu/saoLuuTaiKhoan';
import { cham, themTho } from '../../nghiepvu/thaoTac';
import { dungSaoLuuTaiKhoan } from '../dungSaoLuuTaiKhoan';

const HOM_NAY = Ngay.homNay();
const HOM_QUA = Ngay.congNgay(HOM_NAY, -1);

/** Kho giả: ghi lại từng lượt đẩy để đếm, và trả về danh sách bản đặt sẵn. */
function khoGia(
  cacBan: BanTaiKhoan[] | 'hut' = [],
): KhoTaiKhoan & { daDay: { ngay: string; duLieu: DuLieuChamCong }[] } {
  const daDay: { ngay: string; duLieu: DuLieuChamCong }[] = [];
  return {
    daDay,
    hoTro: () => true,
    async day(duLieu, ngay) {
      daDay.push({ ngay, duLieu });
      return { ngay, suaLuc: `${ngay}T09:00:00.000Z` };
    },
    async danhSach() {
      if (cacBan === 'hut') {
        throw new Error('Không nối được mạng. Kiểm tra 3G hay wifi rồi thử lại.');
      }
      return cacBan;
    },
    async docBan() {
      return duLieuRong();
    },
  };
}

function soCoCong(): DuLieuChamCong {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, '2026-08-01');
  return cham(duLieu, tho.id, HOM_NAY, 'Sang');
}

/**
 * Dựng hook như App dựng: `duLieu` là `null` cho tới khi đọc xong bộ nhớ máy.
 *
 * Phải đi qua đúng hai nhịp ấy, không đưa dữ liệu vào ngay từ đầu: cờ "máy này lúc mở app đã
 * có sổ chưa" chốt ở nhịp đầu tiên có dữ liệu, mà đó chính là thứ bài này kiểm.
 */
function dung(kho: KhoTaiKhoan, duocDung = true) {
  const ket = renderHook(
    ({ duLieu }: { duLieu: DuLieuChamCong | null }) => dungSaoLuuTaiKhoan(duLieu, kho, duocDung),
    { initialProps: { duLieu: null as DuLieuChamCong | null } },
  );
  return ket;
}

/** Chờ hết giờ chờ yên rồi để mấy lời hứa trong lượt đẩy chạy xong. */
async function choDay() {
  await act(async () => {
    jest.advanceTimersByTime(180_000);
  });
}

/** Để lượt đọc danh sách lúc mở app chạy xong. */
async function choDocXong() {
  await act(async () => {});
}

beforeEach(() => {
  jest.useFakeTimers();
});

afterEach(() => {
  jest.useRealTimers();
});

describe('máy mới: sổ trống mà tài khoản đã có bản', () => {
  test('mời lấy bản mới nhất về, và chưa đẩy gì lên', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: `${HOM_QUA}T16:12:00.000Z` }]);
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();
    await choDay();

    expect(kho.daDay).toEqual([]);
    expect(result.current.trangThai.banChoLay).toEqual({
      ngay: HOM_QUA,
      suaLuc: `${HOM_QUA}T16:12:00.000Z`,
    });
  });

  test('người dùng chấm luôn mấy ô mà chưa trả lời: vẫn không đẩy, và thôi chắn ngang', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: `${HOM_QUA}T16:12:00.000Z` }]);
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();

    rerender({ duLieu: soCoCong() });
    await choDay();

    // Đây là chỗ chính: sổ trên tài khoản không bị sổ của máy mới ghi đè.
    expect(kho.daDay).toEqual([]);
    // Và đã gõ rồi thì đừng chắn ngang nữa — muốn lấy thì vào Thợ → Sao lưu.
    expect(result.current.trangThai.banChoLay).toBeNull();
  });

  /**
   * Lỗi bắt được lúc chạy trên máy thật, không bài nào ở trên thấy: sổ trống + tài khoản chưa
   * có bản thì máy được phép đẩy (không có gì để mất). Nó đẩy xong là `cacBan` có một bản —
   * và app quay lại mời chính nó lấy về cái nó vừa ghi.
   */
  test('máy vừa tự đẩy sổ lên thì đừng mời nó lấy về cái nó vừa ghi', async () => {
    const kho = khoGia([]);
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();

    // Người dùng thêm thợ vào sổ trống; lượt đẩy chạy vì trên tài khoản chưa có gì.
    rerender({ duLieu: soCoCong() });
    await choDay();

    expect(kho.daDay).toHaveLength(1);
    expect(result.current.trangThai.cacBan).toHaveLength(1);
    expect(result.current.trangThai.banChoLay).toBeNull();
  });

  test('trả lời rồi thì mới được đẩy — và đẩy đúng sổ đang có', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: `${HOM_QUA}T16:12:00.000Z` }]);
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();

    act(() => result.current.daTraLoi());

    const so = soCoCong();
    rerender({ duLieu: so });
    await choDay();

    expect(kho.daDay).toHaveLength(1);
    expect(kho.daDay[0]).toEqual({ ngay: HOM_NAY, duLieu: so });
    // Trả lời rồi thì không mời lấy sổ nữa.
    expect(result.current.trangThai.banChoLay).toBeNull();
  });

  test('sổ vẫn trống thì không đẩy, dù đã trả lời — không có sổ trống nào đáng lưu', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: '' }]);
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();
    act(() => result.current.daTraLoi());
    await choDay();

    expect(kho.daDay).toEqual([]);
  });

  test('bấm thẳng nút Sao lưu ngay lúc sổ trống thì bị từ chối, kèm câu nói rõ vì sao', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: '' }]);
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();

    await act(async () => {
      await result.current.dayNgay();
    });

    expect(kho.daDay).toEqual([]);
    expect(result.current.trangThai.loi).toContain('đang trống');
  });
});

describe('máy đang dùng: sổ đã có từ lúc mở app', () => {
  test('đổi dữ liệu rồi ngồi im một lát là sổ tự lên tài khoản', async () => {
    const kho = khoGia([{ ngay: HOM_NAY, suaLuc: `${HOM_NAY}T08:00:00.000Z` }]);
    const { rerender } = dung(kho);

    const dau = soCoCong();
    rerender({ duLieu: dau });
    await choDocXong();

    // Bản của hôm nay đã có trên tài khoản nên lượt lúc mở app không chạy.
    expect(kho.daDay).toEqual([]);

    rerender({ duLieu: cham(dau, dau.thos[0].id, HOM_NAY, 'Chieu') });
    await choDay();

    expect(kho.daDay).toHaveLength(1);
  });

  test('hôm nay chưa có bản nào trên tài khoản thì đẩy luôn lúc mở app', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: '' }]);
    const { rerender } = dung(kho);

    rerender({ duLieu: soCoCong() });
    await choDocXong();
    await act(async () => {});

    expect(kho.daDay.map((lan) => lan.ngay)).toEqual([HOM_NAY]);
  });

  test('sổ không đổi thì không đẩy lại — mỗi lượt đẩy là cả sổ', async () => {
    const kho = khoGia([{ ngay: HOM_NAY, suaLuc: '' }]);
    const { rerender } = dung(kho);

    const so = soCoCong();
    rerender({ duLieu: so });
    await choDocXong();

    // Cùng một sổ, chỉ là một object khác — giao diện dựng lại là chuyện thường.
    rerender({ duLieu: { ...so } });
    await choDay();
    rerender({ duLieu: { ...so, thos: [...so.thos] } });
    await choDay();

    expect(kho.daDay).toHaveLength(1);
  });
});

describe('những lúc chưa biết chắc thì không làm gì', () => {
  test('không đọc được danh sách bản: không đẩy, và cũng không mời lấy sổ về', async () => {
    const kho = khoGia('hut');
    const { result, rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();
    rerender({ duLieu: soCoCong() });
    await choDay();

    expect(kho.daDay).toEqual([]);
    expect(result.current.trangThai.cacBan).toBeNull();
    expect(result.current.trangThai.banChoLay).toBeNull();
    expect(result.current.trangThai.loi).toContain('Không nối được mạng');
  });

  test('tài khoản chưa có bản nào thì đẩy được ngay — không có gì để mất', async () => {
    const kho = khoGia([]);
    const { rerender } = dung(kho);

    rerender({ duLieu: duLieuRong() });
    await choDocXong();
    rerender({ duLieu: soCoCong() });
    await choDay();

    expect(kho.daDay).toHaveLength(1);
  });

  test('máy thợ hay máy chưa đăng nhập thì không đọc, không đẩy, không mời gì', async () => {
    const kho = khoGia([{ ngay: HOM_QUA, suaLuc: '' }]);
    const { result, rerender } = dung(kho, false);

    rerender({ duLieu: soCoCong() });
    await choDay();

    expect(kho.daDay).toEqual([]);
    expect(result.current.trangThai.hoTro).toBe(false);
    expect(result.current.trangThai.cacBan).toBeNull();
    expect(result.current.trangThai.banChoLay).toBeNull();
  });
});
