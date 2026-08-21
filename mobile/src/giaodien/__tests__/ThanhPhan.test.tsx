/**
 * Ô nhập: **dấu "đang gõ" là viền ô đổi màu, do app tự vẽ.**
 *
 * Kiểm ở đây vì thứ này dễ mất mà không ai thấy: trên bản web, Chrome trước đây vẽ hộ một
 * vòng xanh quanh `<input>` nên ô nhập trông vẫn có dấu focus dù app chẳng vẽ gì. Vòng ấy
 * đã bỏ ([index.html](../../../public/index.html)) — giờ ô nào đang gõ mà không đổi viền
 * thì trên cả ba bản người dùng không còn biết mình đang gõ vào đâu.
 */

import { fireEvent, render, screen } from '@testing-library/react-native';

import { ONhap } from '../ThanhPhan';
import { Mau } from '../thietKe';

/** Khung ngoài của ô nhập — chỗ mang nét viền. `ONhap` dựng nó làm phần tử gốc. */
function khungONhap() {
  return screen.root;
}

test('ô đang gõ thì viền đổi sang xanh, gõ xong thì về xám', () => {
  render(<ONhap nhan="Tên thợ" placeholder="Ví dụ: Anh Tuấn" />);

  expect(khungONhap()).toHaveStyle({ borderColor: Mau.vien });

  fireEvent(screen.getByPlaceholderText('Ví dụ: Anh Tuấn'), 'focus');
  expect(khungONhap()).toHaveStyle({ borderColor: Mau.chinh });

  fireEvent(screen.getByPlaceholderText('Ví dụ: Anh Tuấn'), 'blur');
  expect(khungONhap()).toHaveStyle({ borderColor: Mau.vien });
});

test('vẫn gọi `onFocus` và `onBlur` của người gọi', () => {
  const onFocus = jest.fn();
  const onBlur = jest.fn();
  render(<ONhap nhan="Tên thợ" placeholder="Ví dụ: Anh Tuấn" onFocus={onFocus} onBlur={onBlur} />);

  fireEvent(screen.getByPlaceholderText('Ví dụ: Anh Tuấn'), 'focus');
  fireEvent(screen.getByPlaceholderText('Ví dụ: Anh Tuấn'), 'blur');

  expect(onFocus).toHaveBeenCalled();
  expect(onBlur).toHaveBeenCalled();
});
