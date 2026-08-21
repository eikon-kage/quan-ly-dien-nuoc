/**
 * Gửi file đi trên bản web.
 *
 * Hai đường và ba tình huống phải phân biệt cho đúng:
 *   · máy có bảng chia sẻ  → dùng bảng ấy, **không** tải về
 *   · người dùng bấm huỷ   → coi như xong, không tải về đè lên (huỷ là ý muốn của họ)
 *   · máy từ chối chia sẻ  → tải về, chứ không báo hỏng
 *
 * Chỗ dễ sai nhất là tình huống giữa: bắt hết mọi lỗi rồi tải về thì người dùng bấm huỷ mà
 * file vẫn rơi vào thư mục Tải về — đúng cái họ vừa nói là không muốn.
 */

import { KIEU_EXCEL, guiFile } from '../../chiaSeFile.web';

let neoDaBam: HTMLAnchorElement | null = null;

beforeEach(() => {
  neoDaBam = null;
  URL.createObjectURL = jest.fn(() => 'blob:http://may/1');
  URL.revokeObjectURL = jest.fn();

  // Bấm thật vào thẻ `a` thì jsdom đi tải file. Chỉ giữ lại thẻ để soi.
  jest
    .spyOn(HTMLAnchorElement.prototype, 'click')
    .mockImplementation(function (this: HTMLAnchorElement) {
      neoDaBam = this;
    });
});

afterEach(() => {
  jest.restoreAllMocks();
  Reflect.deleteProperty(navigator, 'share');
  Reflect.deleteProperty(navigator, 'canShare');
});

function coBangChiaSe(chiaSe: (du: ShareData) => Promise<void>) {
  Object.defineProperty(navigator, 'canShare', { configurable: true, value: () => true });
  Object.defineProperty(navigator, 'share', { configurable: true, value: chiaSe });
}

test('máy có bảng chia sẻ thì gửi qua bảng ấy, không tải về', async () => {
  const chiaSe = jest.fn((_du: ShareData) => Promise.resolve());
  coBangChiaSe(chiaSe);

  await guiFile(new Uint8Array([1, 2]), 'cong.xlsx', KIEU_EXCEL, 'Gửi file chấm công');

  expect(chiaSe).toHaveBeenCalledTimes(1);
  const file = (chiaSe.mock.calls[0][0].files ?? [])[0];
  expect(file.name).toBe('cong.xlsx');
  expect(file.type).toBe(KIEU_EXCEL.mime);
  expect(neoDaBam).toBeNull();
});

test('người dùng bấm huỷ trên bảng chia sẻ thì không tải về đè lên', async () => {
  coBangChiaSe(() => Promise.reject(new DOMException('huỷ', 'AbortError')));

  await guiFile('{}', 'sao-luu.json', KIEU_EXCEL, 'Gửi bản sao lưu');

  expect(neoDaBam).toBeNull();
});

test('máy từ chối chia sẻ thì lùi về đường tải file, không báo hỏng', async () => {
  // `NotAllowedError`: máy coi cú bấm của người dùng đã nguội vì dựng file mất một nhịp.
  coBangChiaSe(() => Promise.reject(new DOMException('nguội', 'NotAllowedError')));

  await guiFile('{}', 'sao-luu.json', KIEU_EXCEL, 'Gửi bản sao lưu');

  expect(neoDaBam?.download).toBe('sao-luu.json');
});

test('máy không có bảng chia sẻ thì tải file về, đúng tên file', async () => {
  await guiFile(new Uint8Array([1]), 'so-cong-Tuan.xlsx', KIEU_EXCEL, 'Gửi sổ công');

  expect(neoDaBam?.download).toBe('so-cong-Tuan.xlsx');
  expect(neoDaBam?.href).toBe('blob:http://may/1');
});

test('dọn thẻ a khỏi trang sau khi bấm, không để rác lại trong DOM', async () => {
  await guiFile(new Uint8Array([1]), 'cong.xlsx', KIEU_EXCEL, 'Gửi file');

  expect(document.querySelectorAll('a').length).toBe(0);
});
