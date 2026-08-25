import { fireEvent, render, screen } from '@testing-library/react-native';

import { DuLieuChamCong, duLieuRong } from '../../nghiepvu/kieu';
import * as Ngay from '../../nghiepvu/ngayViet';
import { dangCham, datGhiChuNgay, ghiChuNgay, themTho } from '../../nghiepvu/thaoTac';
import { ManHinhChamCong } from '../ManHinhChamCong';

const HOM_NAY = Ngay.homNay();

/** Một ngày khác hôm nay nhưng vẫn nằm trong dải bảy ngày đang hiện. */
const NGAY_KHAC = Ngay.tuan(HOM_NAY).find((n) => n !== HOM_NAY) as string;

/** Chạm vào một ô trên dải ngày. */
function chonNgay(ngay: string) {
  fireEvent.press(screen.getByLabelText(`Chọn ${Ngay.thuVaNgay(ngay)}`));
}

function khoCoTho(...tens: string[]) {
  let duLieu = duLieuRong();
  const ids: string[] = [];

  for (const ten of tens) {
    const ketQua = themTho(duLieu, ten, 300_000, HOM_NAY);
    duLieu = ketQua.duLieu;
    ids.push(ketQua.tho.id);
  }

  return { duLieu, ids };
}

/** Dựng màn hình và giữ lại dữ liệu mới nhất mà màn hình gửi ra. */
function dung(duLieu: DuLieuChamCong) {
  let hienTai = duLieu;
  const capNhat = jest.fn((moi: DuLieuChamCong) => {
    hienTai = moi;
  });

  const ketQua = render(<ManHinhChamCong duLieu={duLieu} capNhat={capNhat} />);
  return { ...ketQua, capNhat, moiNhat: () => hienTai };
}

describe('màn hình chấm công', () => {
  test('hiện tên từng thợ kèm hai ô Sáng và Chiều', () => {
    const { duLieu } = khoCoTho('Anh Tuấn', 'Anh Bình');
    dung(duLieu);

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.getByText('Anh Bình')).toBeTruthy();
    expect(screen.getAllByText('Sáng')).toHaveLength(2);
    expect(screen.getAllByText('Chiều')).toHaveLength(2);
  });

  test('mở lên là ngày hôm nay', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    dung(duLieu);

    expect(screen.getByText(Ngay.thuVaNgay(HOM_NAY))).toBeTruthy();
    // Đang ở hôm nay thì không hiện nút "Hôm nay" cho rối.
    expect(screen.queryByText('Hôm nay')).toBeNull();
  });

  test('chạm ô Sáng là chấm một công cho đúng thợ đó', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const { capNhat, moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Sáng'));

    expect(capNhat).toHaveBeenCalledTimes(1);
    expect(dangCham(moiNhat(), ids[0], HOM_NAY, 'Sang')?.soCong).toBe(1);
    expect(dangCham(moiNhat(), ids[0], HOM_NAY, 'Chieu')).toBeUndefined();
  });

  test('chạm lại ô đang xanh là bỏ chấm', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const { moiNhat, rerender } = dung(duLieu);

    fireEvent.press(screen.getByText('Sáng'));
    const sauLanMot = moiNhat();
    rerender(<ManHinhChamCong duLieu={sauLanMot} capNhat={() => {}} />);

    // Dựng lại với dữ liệu đã chấm rồi bấm lần nữa.
    let cuoiCung = sauLanMot;
    render(
      <ManHinhChamCong
        duLieu={sauLanMot}
        capNhat={(moi) => {
          cuoiCung = moi;
        }}
      />,
    );
    fireEvent.press(screen.getAllByText('Sáng')[0]);

    expect(dangCham(cuoiCung, ids[0], HOM_NAY, 'Sang')).toBeUndefined();
  });

  test('nút cả tổ chấm đủ hai buổi cho mọi thợ', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn', 'Anh Bình');
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Cả tổ đi đủ cả ngày'));

    for (const id of ids) {
      expect(dangCham(moiNhat(), id, HOM_NAY, 'Sang')?.soCong).toBe(1);
      expect(dangCham(moiNhat(), id, HOM_NAY, 'Chieu')?.soCong).toBe(1);
    }
  });

  test('cả tổ đã đủ công thì nút đổi thành xoá hết', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Cả tổ đi đủ cả ngày'));
    render(<ManHinhChamCong duLieu={moiNhat()} capNhat={() => {}} />);

    expect(screen.getByText('Xoá hết chấm ngày này')).toBeTruthy();
  });

  test('tổng công dưới chân màn hình cộng đúng', () => {
    const { duLieu } = khoCoTho('Anh Tuấn', 'Anh Bình');
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Cả tổ đi đủ cả ngày'));
    render(<ManHinhChamCong duLieu={moiNhat()} capNhat={() => {}} />);

    expect(screen.getByText('4 công')).toBeTruthy();
  });

  test('dải ngày hiện đủ bảy ngày trong tuần, ô hôm nay ghi rõ chữ Nay', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    dung(duLieu);

    for (const n of Ngay.tuan(HOM_NAY)) {
      expect(screen.getByLabelText(`Chọn ${Ngay.thuVaNgay(n)}`)).toBeTruthy();
    }

    expect(screen.getByText('Nay')).toBeTruthy();
  });

  test('chạm một ngày trên dải là sang thẳng ngày đó', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    dung(duLieu);

    chonNgay(NGAY_KHAC);

    expect(screen.getByText(Ngay.thuVaNgay(NGAY_KHAC))).toBeTruthy();
    expect(screen.getByText('Hôm nay')).toBeTruthy();
  });

  test('công chấm ngày khác không hiện ở hôm nay', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const { moiNhat } = dung(duLieu);

    chonNgay(NGAY_KHAC);
    fireEvent.press(screen.getByText('Sáng'));

    expect(dangCham(moiNhat(), ids[0], NGAY_KHAC, 'Sang')).toBeDefined();
    expect(dangCham(moiNhat(), ids[0], HOM_NAY, 'Sang')).toBeUndefined();
  });

  test('dải ngày cho thấy ngày nào đã chấm mấy công', () => {
    const { duLieu } = khoCoTho('Anh Tuấn', 'Anh Bình');
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Cả tổ đi đủ cả ngày'));
    render(<ManHinhChamCong duLieu={moiNhat()} capNhat={() => {}} />);

    // Ô hôm nay trên dải hiện "4", tách biệt với dòng "4 công" dưới chân màn hình.
    expect(screen.getByText('4')).toBeTruthy();
    expect(screen.getByLabelText(`Chọn ${Ngay.thuVaNgay(HOM_NAY)}`).props.accessibilityHint).toBe(
      '4 công',
    );
  });

  test('nút Tuần lùi hẳn bảy ngày chứ không phải một ngày', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    dung(duLieu);

    fireEvent.press(screen.getByLabelText('Tuần trước'));

    expect(screen.getByText(Ngay.thuVaNgay(Ngay.congNgay(HOM_NAY, -7)))).toBeTruthy();
  });

  test('chưa có thợ thì chỉ đường sang mục Thợ, không hiện nút cả tổ', () => {
    dung(duLieuRong());

    expect(screen.getByText('Chưa có thợ nào')).toBeTruthy();
    expect(screen.queryByText('Cả tổ đi đủ cả ngày')).toBeNull();
  });

  test('thợ đã nghỉ không hiện ra để chấm', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn', 'Anh Bình');
    duLieu.thos = duLieu.thos.map((t) => (t.id === ids[1] ? { ...t, dangLam: false } : t));
    dung(duLieu);

    expect(screen.getByText('Anh Tuấn')).toBeTruthy();
    expect(screen.queryByText('Anh Bình')).toBeNull();
  });

  test('sửa công gõ được số bất kỳ, không bị bó vào ba mức có sẵn', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Sửa'));
    fireEvent.press(screen.getByText('Buổi sáng'));
    fireEvent.press(screen.getByText('Gõ số công khác'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ 0,75'), '0,75');
    fireEvent.press(screen.getByText('Ghi'));

    expect(dangCham(moiNhat(), ids[0], HOM_NAY, 'Sang')?.soCong).toBe(0.75);
  });

  test('gõ số công lớn quá thì chặn lại và nói rõ vì sao', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const { capNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Sửa'));
    fireEvent.press(screen.getByText('Buổi sáng'));
    fireEvent.press(screen.getByText('Gõ số công khác'));
    // Gõ "10" thay vì "1,0" là lỗi hay gặp, lọt qua thì tiền công sai gấp mười.
    fireEvent.changeText(screen.getByLabelText('Ví dụ 0,75'), '10');

    expect(screen.getByText('Nhiều nhất 5 công một buổi.')).toBeTruthy();

    fireEvent.press(screen.getByText('Ghi'));
    expect(capNhat).not.toHaveBeenCalled();
  });

  test('ghi chú của ngày hiện ngay trên thẻ thợ, chạm vào là mở ra sửa', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const coGhiChu = datGhiChuNgay(duLieu, ids[0], HOM_NAY, 'về sớm đi đám cưới');
    const { moiNhat } = dung(coGhiChu);

    expect(screen.getByText('về sớm đi đám cưới')).toBeTruthy();

    fireEvent.press(screen.getByLabelText('Ghi chú của Anh Tuấn: về sớm đi đám cưới. Chạm để sửa.'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ: về sớm đi đám cưới'), 'nghỉ nửa ngày');
    fireEvent.press(screen.getByText('Ghi'));

    expect(ghiChuNgay(moiNhat(), ids[0], HOM_NAY)).toBe('nghỉ nửa ngày');
  });

  test('chưa có ghi chú thì thẻ thợ không mọc thêm dòng nào', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    dung(duLieu);

    expect(screen.queryByText('icon:message-square')).toBeNull();
  });

  test('ghi chú vào được từ nút Sửa, gắn đúng thợ và đúng ngày đang xem', () => {
    // Danh sách xếp theo tên, nên Anh Bình đứng trước Anh Tuấn.
    const { duLieu, ids } = khoCoTho('Anh Bình', 'Anh Tuấn');
    const { moiNhat } = dung(duLieu);

    chonNgay(NGAY_KHAC);
    // Nút Sửa của thợ thứ hai — ghi chú phải gắn vào đúng người đó.
    fireEvent.press(screen.getAllByText('Sửa')[1]);
    fireEvent.press(screen.getByText('Ghi chú cho ngày này'));
    fireEvent.changeText(screen.getByLabelText('Ví dụ: về sớm đi đám cưới'), 'mưa, nghỉ cả ngày');
    fireEvent.press(screen.getByText('Ghi'));

    expect(ghiChuNgay(moiNhat(), ids[1], NGAY_KHAC)).toBe('mưa, nghỉ cả ngày');
    expect(ghiChuNgay(moiNhat(), ids[1], HOM_NAY)).toBe('');
    expect(ghiChuNgay(moiNhat(), ids[0], NGAY_KHAC)).toBe('');
  });

  test('ghi chú của ngày khác không hiện ở ngày đang xem', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const coGhiChu = datGhiChuNgay(duLieu, ids[0], NGAY_KHAC, 'mưa, nghỉ cả ngày');
    dung(coGhiChu);

    expect(screen.queryByText('mưa, nghỉ cả ngày')).toBeNull();

    chonNgay(NGAY_KHAC);
    expect(screen.getByText('mưa, nghỉ cả ngày')).toBeTruthy();
  });

  test('nút Xoá ghi chú bỏ hẳn ghi chú của ngày đó', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    const coGhiChu = datGhiChuNgay(duLieu, ids[0], HOM_NAY, 'về sớm đi đám cưới');
    const { moiNhat } = dung(coGhiChu);

    fireEvent.press(screen.getByText('về sớm đi đám cưới'));
    fireEvent.press(screen.getByText('Xoá ghi chú'));

    expect(ghiChuNgay(moiNhat(), ids[0], HOM_NAY)).toBe('');
    expect(moiNhat().ghiChuNgays).toEqual([]);
  });

  test('ghi chú còn nguyên sau khi bỏ chấm cả hai buổi', () => {
    const { duLieu, ids } = khoCoTho('Anh Tuấn');
    let hienTai = datGhiChuNgay(duLieu, ids[0], HOM_NAY, 'nghỉ đau chân');
    hienTai = { ...hienTai };

    const { moiNhat } = dung(hienTai);
    fireEvent.press(screen.getByText('Cả tổ đi đủ cả ngày'));

    const daCham = moiNhat();
    const { moiNhat: sauKhiXoa } = dung(daCham);
    fireEvent.press(screen.getByText('Xoá hết chấm ngày này'));

    expect(sauKhiXoa().buoiCongs).toEqual([]);
    expect(ghiChuNgay(sauKhiXoa(), ids[0], HOM_NAY)).toBe('nghỉ đau chân');
  });

  test('ô đã chấm nói rõ trạng thái cho trình đọc màn hình', () => {
    const { duLieu } = khoCoTho('Anh Tuấn');
    const { moiNhat } = dung(duLieu);

    fireEvent.press(screen.getByText('Sáng'));
    render(<ManHinhChamCong duLieu={moiNhat()} capNhat={() => {}} />);

    expect(screen.getByLabelText('Sáng có đi làm')).toBeTruthy();
    expect(screen.getByLabelText('Chiều chưa chấm')).toBeTruthy();
  });
});
