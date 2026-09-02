import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { Alert } from 'react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import { quyetToan } from '../../nghiepvu/ky';
import * as Ngay from '../../nghiepvu/ngayViet';
import { cham, themTho, themUng } from '../../nghiepvu/thaoTac';
import { ManHinhBangLuong } from '../ManHinhBangLuong';

const HOM_NAY = Ngay.homNay();

function khoCoTho() {
  const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, HOM_NAY);
  return { duLieu: cham(duLieu, tho.id, HOM_NAY, 'Sang'), thoId: tho.id };
}

function dung(duLieu: DuLieuChamCong) {
  let hienTai = duLieu;
  render(
    <ManHinhBangLuong
      duLieu={duLieu}
      capNhat={(moi) => {
        hienTai = moi;
      }}
    />,
  );
  return { moiNhat: () => hienTai };
}

describe('ứng tiền ở bảng lương', () => {
  test('ghi kèm ghi chú để sau còn nhớ ứng vào việc gì', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Ứng tiền'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 500000'), '500000');
    fireEvent.changeText(screen.getByPlaceholderText('Ví dụ: ứng đổ xăng'), '  ứng mua thuốc  ');
    fireEvent.press(screen.getByText('Ghi'));

    const ung = moiNhat().ungTiens[0];
    expect(ung.thoId).toBe(thoId);
    expect(ung.soTien).toBe(500_000);
    // Khoảng trắng thừa hai đầu bị cắt, kẻo dòng ghi chú trong báo cáo bị thụt vào.
    expect(ung.ghiChu).toBe('ứng mua thuốc');
  });

  test('không điền ghi chú vẫn ứng được', () => {
    const { duLieu } = khoCoTho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Ứng tiền'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 500000'), '500000');
    fireEvent.press(screen.getByText('Ghi'));

    expect(moiNhat().ungTiens).toHaveLength(1);
    expect(moiNhat().ungTiens[0].ghiChu).toBe('');
  });
});

/**
 * Sửa lịch sử ứng: mở chi tiết một thợ rồi chạm vào dòng ứng. Ở đây soi cả đường đi từ
 * bảng lương xuống tận sổ — hai đầu nối đúng vào nhau thì con số mới thật sự đổi.
 */
describe('sửa lịch sử ứng tiền', () => {
  const hoi = jest.spyOn(Alert, 'alert').mockImplementation(() => {});

  beforeEach(() => hoi.mockClear());

  function moHopSua(soTien = 5_000_000) {
    const { duLieu, thoId } = khoCoTho();
    const daUng = themUng(duLieu, thoId, HOM_NAY, soTien, 'ứng đổ xăng');
    const { moiNhat } = dung(daUng);

    fireEvent.press(screen.getByText('Xem chi tiết từng ngày'));
    fireEvent.press(screen.getByLabelText(/chạm để sửa/));

    return { moiNhat, ungId: daUng.ungTiens[0].id };
  }

  test('sửa số tiền gõ thừa một số 0', () => {
    const { moiNhat, ungId } = moHopSua();

    fireEvent.changeText(screen.getByLabelText('Số tiền ứng'), '500000');
    fireEvent.press(screen.getByText('Ghi'));

    // Vẫn đúng lần ứng cũ chứ không đẻ thêm dòng mới.
    expect(moiNhat().ungTiens).toHaveLength(1);
    expect(moiNhat().ungTiens[0].id).toBe(ungId);
    expect(moiNhat().ungTiens[0].soTien).toBe(500_000);
  });

  test('xoá hẳn lần ứng ghi nhầm', () => {
    const { moiNhat } = moHopSua();

    fireEvent.press(screen.getByText('Xoá lần ứng này'));
    // Bọc `act`: nút của hộp thoại nằm ngoài cây React mà bấm vào thì màn hình đổi.
    const nut = (hoi.mock.calls[0][2] ?? []).find((n) => n.text === 'Xoá');
    act(() => nut?.onPress?.());

    expect(moiNhat().ungTiens).toEqual([]);
  });
});

/**
 * Xem lại tháng cũ. Kỳ lương cắt theo lúc trả tiền, nhưng người ta vẫn nhớ việc theo
 * tháng — "tháng Tám vừa rồi hết bao nhiêu tiền công" là câu hỏi có thật.
 */
describe('lùi về tháng cũ', () => {
  const THANG_TRUOC = Ngay.congNgay(
    Ngay.ghep(Ngay.tach(HOM_NAY).nam, Ngay.tach(HOM_NAY).thang, 1),
    -10,
  );

  function khoHaiThang() {
    const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, THANG_TRUOC);
    return {
      duLieu: cham(cham(duLieu, tho.id, THANG_TRUOC, 'Sang'), tho.id, HOM_NAY, 'Sang'),
      thang: Ngay.tach(THANG_TRUOC),
    };
  }

  test('mở ra là kỳ đang mở, bấm mũi tên trái là lùi về tháng', () => {
    const { duLieu, thang } = khoHaiThang();
    dung(duLieu);

    expect(screen.getByText('Kỳ này')).toBeTruthy();

    fireEvent.press(screen.getByLabelText('Tháng trước'));
    expect(
      screen.getByText(`Tháng ${Ngay.tach(HOM_NAY).thang}/${Ngay.tach(HOM_NAY).nam}`),
    ).toBeTruthy();

    fireEvent.press(screen.getByLabelText('Tháng trước'));
    expect(screen.getByText(`Tháng ${thang.thang}/${thang.nam}`)).toBeTruthy();
  });

  test('mũi tên phải đưa về lại kỳ đang mở', () => {
    const { duLieu } = khoHaiThang();
    dung(duLieu);

    fireEvent.press(screen.getByLabelText('Tháng trước'));
    fireEvent.press(screen.getByLabelText('Tháng sau'));

    expect(screen.getByText('Kỳ này')).toBeTruthy();
  });

  /*
    Hai chỗ này là lý do phải tách thẻ của tháng ra khỏi thẻ của kỳ. Ứng tiền bao giờ cũng
    ghi vào hôm nay nên để nút ấy trên tháng cũ là mời người dùng ghi nhầm ngày; còn "còn
    phải trả" thì chốt theo kỳ chứ không theo tháng, ghi cho một tháng lẻ là ghi ra một con
    số không ai đòi ai cả.
  */
  test('xem tháng cũ thì không có nút ứng tiền, không có dòng còn phải trả', () => {
    const { duLieu } = khoHaiThang();
    dung(duLieu);
    fireEvent.press(screen.getByLabelText('Tháng trước'));

    expect(screen.queryByText('Ứng tiền')).toBeNull();
    expect(screen.queryByText('Còn phải trả')).toBeNull();
    expect(screen.getByText('Tiền công cả tháng')).toBeTruthy();
  });

  test('xem tháng cũ thì không quyết toán được — chốt kỳ không phải chốt một khúc lịch', () => {
    const { duLieu } = khoHaiThang();
    dung(duLieu);

    expect(screen.getByText('Quyết toán kỳ này')).toBeTruthy();
    fireEvent.press(screen.getByLabelText('Tháng trước'));
    expect(screen.queryByText('Quyết toán kỳ này')).toBeNull();
  });

  test('hết đường thì mũi tên mờ đi chứ không biến mất, kẻo hai nút nhảy chỗ', () => {
    const { duLieu } = khoHaiThang();
    dung(duLieu);

    // Đang ở kỳ đang mở: không lùi tới nữa được.
    expect(screen.getByLabelText('Tháng sau')).toBeDisabled();
    expect(screen.getByLabelText('Tháng trước')).toBeEnabled();
  });

  test('sổ trắng thì không có mũi tên nào', () => {
    dung(duLieuRong());

    expect(screen.queryByLabelText('Tháng trước')).toBeNull();
    expect(screen.queryByLabelText('Tháng sau')).toBeNull();
  });

  /**
   * Kỳ cắt theo bản ghi nào đã quyết toán, tháng cắt theo ngày. Chốt kỳ xong bảng lương về
   * 0, nhưng tháng vừa rồi thì vẫn phải ra đủ số công đã làm — đó chính là chỗ mà xem theo
   * kỳ không trả lời được.
   */
  test('chốt kỳ xong vẫn tra lại được công của tháng ấy', () => {
    const { duLieu, thang } = khoHaiThang();
    const daChot = quyetToan(duLieu, { denNgay: HOM_NAY });
    dung(daChot);

    expect(screen.getByText('Kỳ này chưa có công nào')).toBeTruthy();

    fireEvent.press(screen.getByLabelText('Tháng trước'));
    fireEvent.press(screen.getByLabelText('Tháng trước'));
    expect(screen.getByText(`Tháng ${thang.thang}/${thang.nam}`)).toBeTruthy();
    expect(screen.getByText('Tiền công cả tháng')).toBeTruthy();
    // Một buổi sáng là nửa công, 300.000 một công nên ra 150.000 — hiện hai chỗ, trên thẻ
    // của thợ và ở dòng cộng cả tổ dưới chân trang.
    expect(screen.getAllByText('150.000 đ')).toHaveLength(2);
  });
});

/**
 * Chấm bù ngay trên tờ lịch trong màn hình chi tiết một thợ — chỗ chủ đang nhìn khi thợ
 * thắc mắc, nên cũng phải là chỗ chữa được.
 */
describe('sửa một ngày ngay trên tờ lịch chi tiết thợ', () => {
  /** Nhãn của ô ngày trên tờ lịch: "05/08 Thứ Tư, ...". */
  const oNgay = (ngay: string) =>
    new RegExp(`^${Ngay.ngayGon(ngay).slice(0, 5)} ${Ngay.thu(ngay)},`);

  test('chạm ô ngày rồi chấm nốt buổi chiều', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Xem chi tiết từng ngày'));
    fireEvent.press(screen.getByLabelText(oNgay(HOM_NAY)));
    fireEvent.press(screen.getByLabelText('Chiều chưa chấm, chạm để đổi'));

    const buoi = moiNhat().buoiCongs.filter((b) => b.thoId === thoId && b.ngay === HOM_NAY);
    expect(buoi.map((b) => b.buoi).sort()).toEqual(['Chieu', 'Sang']);
  });

  test('ghi chú cho ngày ấy cũng ghi được từ đây', () => {
    const { duLieu, thoId } = khoCoTho();
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Xem chi tiết từng ngày'));
    fireEvent.press(screen.getByLabelText(oNgay(HOM_NAY)));
    fireEvent.press(screen.getByText('Sửa'));
    fireEvent.press(screen.getByText('Ghi chú cho ngày này'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ: về sớm đi đám cưới'), 'mưa, về sớm');
    fireEvent.press(screen.getByText('Ghi'));

    expect(moiNhat().ghiChuNgays).toEqual([
      expect.objectContaining({ thoId, ngay: HOM_NAY, noiDung: 'mưa, về sớm' }),
    ]);
  });

  test('xem tháng cũ thì tờ lịch chỉ để đọc — chỗ ấy chỉ để tra sổ', () => {
    const thangTruoc = Ngay.congNgay(
      Ngay.ghep(Ngay.tach(HOM_NAY).nam, Ngay.tach(HOM_NAY).thang, 1),
      -10,
    );
    const { duLieu, tho } = themTho(duLieuRong(), 'Anh Tuấn', 300_000, thangTruoc);
    dung(cham(cham(duLieu, tho.id, thangTruoc, 'Sang'), tho.id, HOM_NAY, 'Sang'));

    fireEvent.press(screen.getByLabelText('Tháng trước'));
    fireEvent.press(screen.getByText('Xem chi tiết từng ngày'));

    expect(screen.queryByText('Chạm vào một ngày để chấm hoặc sửa ngày ấy.')).toBeNull();
    expect(screen.queryByLabelText(/Chạm để sửa/)).toBeNull();
  });

  /**
   * Chấm bù một ngày của kỳ đã chốt thì kỳ đang mở lùi đầu về tận đó, kéo theo cả mấy
   * ngày đã trả tiền vào trong tờ lịch. Buổi đã trả tiền phải khoá lại: `KyLuong.dongs` là
   * bản chụp không tính lại bao giờ nữa, sửa nó chỉ làm sổ nói khác tờ quyết toán đã đưa.
   */
  test('buổi đã nằm trong kỳ đã chốt thì khoá lại', () => {
    const { duLieu, thoId } = khoCoTho();
    const daChot = quyetToan(duLieu, { denNgay: HOM_NAY });
    const homQua = Ngay.congNgay(HOM_NAY, -1);
    const { moiNhat } = dung(cham(daChot, thoId, homQua, 'Sang'));

    fireEvent.press(screen.getByText('Xem chi tiết từng ngày'));
    fireEvent.press(screen.getByLabelText(oNgay(HOM_NAY)));

    expect(
      screen.getByLabelText('Sáng có đi làm, đã chốt kỳ nên không sửa được'),
    ).toBeTruthy();

    // Buổi chiều của chính ngày ấy thì chưa ai trả tiền, vẫn chấm được.
    fireEvent.press(screen.getByLabelText('Chiều chưa chấm, chạm để đổi'));
    expect(
      moiNhat().buoiCongs.filter((b) => b.ngay === HOM_NAY && b.buoi === 'Chieu'),
    ).toHaveLength(1);
  });
});
