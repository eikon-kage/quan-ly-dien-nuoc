/**
 * Sổ công của thợ, xem chi tiết từng ngày.
 *
 * Hai điều phải giữ: xem được **tháng trước** (14 ngày ở màn hình chính không tới), và
 * **không một con số tiền nào** — kể cả khi sổ trong máy còn sót mốc lương từ lúc máy này
 * từng là máy chủ. Đó là lý do màn hình dựng trên `SoCong` chứ không trên `DuLieuChamCong`.
 */

import { fireEvent, render, screen } from '@testing-library/react-native';

import { BuoiLam } from '../../nghiepvu/kieu';
import { SoCong } from '../../nghiepvu/soCong';
import { CachSuaNgay } from '../HopSuaNgay';
import { ManHinhSoCuaToi } from '../ManHinhSoCuaToi';

const HOM_NAY = '2026-08-12';
const BAT_DAU = '2026-07-20';

function so(dongs: SoCong['dongs'], sua: Partial<SoCong> = {}): SoCong {
  return {
    thoId: 't1',
    tenTho: 'Anh Tuấn',
    nguon: 'tho',
    tuNgay: BAT_DAU,
    denNgay: HOM_NAY,
    dongs,
    taoLuc: '',
    ...sua,
  };
}

function dung(cuaToi: SoCong, cuaChu: SoCong | null = null, suaNgay?: CachSuaNgay) {
  return render(
    <ManHinhSoCuaToi
      so={cuaToi}
      soChu={cuaChu}
      homNay={HOM_NAY}
      suaNgay={suaNgay}
      onDong={() => {}}
    />,
  );
}

test('mở ra là tháng này, tóm tắt công và ngày nghỉ', () => {
  dung(
    so([
      { ngay: '2026-08-03', buoi: 'Sang', soCong: 1 },
      { ngay: '2026-08-03', buoi: 'Chieu', soCong: 1 },
      { ngay: '2026-08-04', buoi: 'Sang', soCong: 0.5 },
    ]),
  );

  expect(screen.getByText('Tháng 8/2026')).toBeTruthy();
  expect(screen.getByText('2,5 công')).toBeTruthy();
  expect(screen.getByText('2 ngày')).toBeTruthy();
  // Từ 01/08 tới 12/08 là 12 ngày, trừ hai ngày có công.
  expect(screen.getByText('10 ngày')).toBeTruthy();
});

test('buổi chấm bù trước hôm nhận vai máy vẫn có mặt trong sổ', () => {
  /*
    Sổ khai đầy đủ từ 20/07, nhưng thợ đã chấm bù ra 18/07 — buổi ấy nằm ngoài khoảng khai
    (cố ý: mấy ngày quanh nó máy này *không biết*, khai xuống là đối chiếu báo lệch cả tuần).
    Nó vẫn phải hiện ra ở đây, không thì màn hình chính hiện ô đã chấm mà sổ của chính mình
    lại bảo không có ngày ấy.
  */
  dung(
    so([{ ngay: '2026-07-18', buoi: 'Sang', soCong: 1 }], {
      tuNgay: BAT_DAU,
      denNgay: HOM_NAY,
    }),
  );

  fireEvent.press(screen.getByLabelText('Tháng trước'));

  expect(screen.getByText('Tháng 7/2026')).toBeTruthy();
  expect(screen.getByText('18/07')).toBeTruthy();
  expect(screen.getByText('Sáng 1 · Chiều —')).toBeTruthy();
});

test('mỗi ngày một dòng, ghi rõ đi buổi nào mấy công', () => {
  dung(
    so([
      { ngay: '2026-08-03', buoi: 'Sang', soCong: 1 },
      { ngay: '2026-08-03', buoi: 'Chieu', soCong: 0.5 },
      { ngay: '2026-08-04', buoi: 'Chieu', soCong: 1 },
    ]),
  );

  expect(screen.getByText('Chi tiết từng ngày')).toBeTruthy();
  expect(screen.getByText('Sáng 1 · Chiều 0,5')).toBeTruthy();
  expect(screen.getByText('1,5 công')).toBeTruthy();
  // Ngày chỉ đi một buổi vẫn hiện tên buổi kia kèm gạch, không để trống.
  expect(screen.getByText('Sáng — · Chiều 1')).toBeTruthy();
});

test('ngày không chấm buổi nào thì dòng ấy ghi Nghỉ', () => {
  dung(so([{ ngay: '2026-08-03', buoi: 'Sang', soCong: 1 }]));

  expect(screen.getAllByText('Nghỉ').length).toBeGreaterThan(0);
});

test('ngày mai chưa tới thì không có dòng, cũng không tính là nghỉ', () => {
  dung(so([]));

  expect(screen.getByText('12/08')).toBeTruthy();
  expect(screen.queryByText('13/08')).toBeNull();
});

test('lùi được về tháng trước, tiến lại được tháng này', () => {
  dung(so([{ ngay: '2026-07-22', buoi: 'Sang', soCong: 1 }]));

  fireEvent.press(screen.getByLabelText('Tháng trước'));
  expect(screen.getByText('Tháng 7/2026')).toBeTruthy();
  expect(screen.getByText('22/07')).toBeTruthy();
  // Sổ chỉ có từ 20/07: ngày 19 trở về trước không phải nghỉ nên không có dòng.
  expect(screen.queryByText('19/07')).toBeNull();

  fireEvent.press(screen.getByLabelText('Tháng sau'));
  expect(screen.getByText('Tháng 8/2026')).toBeTruthy();
});

test('hết sổ thì mũi tên tắt, không bấm ra tháng trắng', () => {
  dung(so([]));

  // Tháng sau chưa tới, mà lùi hai tháng là ra ngoài khoảng sổ khai là đầy đủ.
  expect(screen.getByLabelText('Tháng sau')).toBeDisabled();
  fireEvent.press(screen.getByLabelText('Tháng trước'));
  expect(screen.getByLabelText('Tháng trước')).toBeDisabled();
});

test('lọc nửa tháng nhưng vẫn vẽ trọn tờ lịch', () => {
  dung(
    so([
      { ngay: '2026-08-03', buoi: 'Sang', soCong: 1 },
      { ngay: '2026-08-11', buoi: 'Sang', soCong: 1 },
    ]),
  );

  fireEvent.press(screen.getByText('Nửa cuối'));

  expect(screen.getByText('16/08 → 31/08')).toBeTruthy();
  expect(screen.getByText('0 công')).toBeTruthy();
  // Tờ lịch vẫn đủ 31 ô, ngày ngoài khoảng thành ô chưa tính.
  expect(screen.getByLabelText('03/08 Thứ Hai, chưa tính')).toBeTruthy();
});

describe('so với sổ chủ', () => {
  test('chưa nhận sổ chủ thì nói rõ chứ không báo khớp', () => {
    dung(so([{ ngay: '2026-08-03', buoi: 'Sang', soCong: 1 }]));

    expect(screen.getByText('Chưa có')).toBeTruthy();
  });

  test('hai sổ giống nhau thì báo khớp cả', () => {
    const dongs: SoCong['dongs'] = [{ ngay: '2026-08-03', buoi: 'Sang', soCong: 1 }];
    dung(so(dongs), so(dongs, { nguon: 'chu' }));

    expect(screen.getByText('Khớp cả')).toBeTruthy();
    expect(screen.queryByText('Sổ chủ ghi khác')).toBeNull();
  });

  /**
   * Sổ chủ có công ở ngày **sổ này chưa biết** (trước hôm nhận vai máy). Không đánh dấu được
   * vào dòng nào — ngày ấy nằm ngoài sổ này — nên ô tóm tắt phải nói ra, chứ nói "Khớp cả" là
   * thợ đóng màn hình mà không biết mình còn 1 buổi chưa chấm bù.
   */
  test('chủ có công ở ngày sổ này chưa biết thì không báo khớp cả', () => {
    dung(
      // Máy thợ mới có sổ từ 05/08.
      so([{ ngay: '2026-08-06', buoi: 'Sang', soCong: 1 }], { tuNgay: '2026-08-05' }),
      so(
        [
          { ngay: '2026-08-06', buoi: 'Sang', soCong: 1 },
          // Chủ chấm hôm 02/08, trước hôm máy thợ có sổ.
          { ngay: '2026-08-02', buoi: 'Sang', soCong: 1 },
        ],
        { nguon: 'chu' },
      ),
    );

    expect(screen.getByText('Còn 1 buổi')).toBeTruthy();
    expect(screen.queryByText('Khớp cả')).toBeNull();
  });

  test('ngày lệch được đánh dấu ngay trên dòng của nó', () => {
    dung(
      so([
        { ngay: '2026-08-03', buoi: 'Sang', soCong: 1 },
        { ngay: '2026-08-04', buoi: 'Sang', soCong: 1 },
      ]),
      so([{ ngay: '2026-08-03', buoi: 'Sang', soCong: 1 }], { nguon: 'chu' }),
    );

    expect(screen.getByText('1 buổi')).toBeTruthy();
    expect(screen.getByText('Sổ chủ ghi khác')).toBeTruthy();
  });
});

test('không hiện một con số tiền nào', () => {
  dung(
    so([
      { ngay: '2026-08-03', buoi: 'Sang', soCong: 1 },
      { ngay: '2026-08-03', buoi: 'Chieu', soCong: 1 },
    ]),
  );

  expect(screen.queryByText(/đ$/)).toBeNull();
  expect(screen.queryByText(/tiền|lương/i)).toBeNull();
});

/**
 * Thợ chấm bù ngay tại chỗ nhìn ra ngày trống. Trước đây màn hình này cố ý chỉ cho xem,
 * nên chữa lại phải lui về màn hình chính rồi dò lại đúng ngày vừa nhìn thấy.
 */
describe('chấm bù ngay trên sổ của mình', () => {
  function cachSua(daCham: Record<string, number> = {}) {
    const datCong = jest.fn();
    return {
      cong: (ngay: string, buoi: BuoiLam) => daCham[`${ngay} ${buoi}`] ?? null,
      datCong,
    };
  }

  test('không truyền đường sửa thì cả tờ lịch lẫn danh sách chỉ để đọc', () => {
    dung(so([{ ngay: '2026-08-03', buoi: 'Sang', soCong: 0.5 }]));

    expect(screen.queryByLabelText(/Chạm để sửa/)).toBeNull();
    expect(screen.queryByText('Chạm vào một ngày để chấm hoặc sửa ngày ấy.')).toBeNull();
  });

  test('chạm một ô trên tờ lịch là mở hộp chấm cho ngày ấy', () => {
    const sua = cachSua();
    dung(so([]), null, sua);

    fireEvent.press(screen.getByLabelText('05/08 Thứ Tư, nghỉ. Chạm để sửa.'));
    fireEvent.press(screen.getByLabelText('Sáng chưa chấm, chạm để đổi'));

    expect(sua.datCong).toHaveBeenCalledWith('2026-08-05', 'Sang', 0.5);
  });

  test('hộp không có tên thợ: máy này chỉ có một người', () => {
    dung(so([]), null, cachSua());

    fireEvent.press(screen.getByLabelText('05/08 Thứ Tư, nghỉ. Chạm để sửa.'));

    expect(screen.getByText('Thứ Tư 05/08')).toBeTruthy();
    expect(screen.queryByText(/Anh Tuấn/)).toBeNull();
  });

  test('chạm một dòng trong danh sách cũng mở đúng hộp ấy', () => {
    const sua = cachSua({ '2026-08-04 Sang': 0.5 });
    dung(so([{ ngay: '2026-08-04', buoi: 'Sang', soCong: 0.5 }]), null, sua);

    fireEvent.press(screen.getByLabelText('Thứ Ba 04/08, 0,5 công. Chạm để sửa.'));
    fireEvent.press(screen.getByLabelText('Sáng có đi làm, chạm để đổi'));

    expect(sua.datCong).toHaveBeenCalledWith('2026-08-04', 'Sang', null);
  });

  test('không chấm trước cho ngày chưa tới', () => {
    dung(so([]), null, cachSua());

    // 13/08 là ngày mai: vẫn vẽ ra ô, nhưng không chạm được — buổi chấm cho ngày chưa tới
    // nằm ngoài khoảng sổ khai là đầy đủ nên gói gửi lên nhóm không mang nó theo.
    expect(screen.getByLabelText('13/08 Thứ Năm, chưa tính')).toBeTruthy();
  });

  test('hộp không có phần ghi chú: máy thợ không có chỗ nào ghi chú', () => {
    dung(so([]), null, cachSua());

    fireEvent.press(screen.getByLabelText('05/08 Thứ Tư, nghỉ. Chạm để sửa.'));
    fireEvent.press(screen.getByText('Sửa'));

    expect(screen.getByText('Buổi sáng')).toBeTruthy();
    expect(screen.queryByText('Ghi chú cho ngày này')).toBeNull();
  });
});
