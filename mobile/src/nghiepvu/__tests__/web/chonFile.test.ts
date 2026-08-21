/**
 * Chọn file trên bản web.
 *
 * Bài đáng giá nhất là bài **bấm huỷ**: tài liệu Expo nói bản web không báo được việc người
 * dùng huỷ, nhưng đọc mã nguồn thư viện thì nó có nghe sự kiện `cancel` và trả về
 * `canceled: true`. Bài này chốt lại cách app xử khi nhận được `canceled` — huỷ là `null`
 * chứ không phải lỗi, vì màn hình nhập Excel dựa vào đúng chỗ ấy để thôi quay vòng xoay.
 */

import * as DocumentPicker from 'expo-document-picker';

import { chonFile } from '../../chonFile.web';

jest.mock('expo-document-picker', () => ({ getDocumentAsync: jest.fn() }));

const moBang = DocumentPicker.getDocumentAsync as jest.MockedFunction<
  typeof DocumentPicker.getDocumentAsync
>;

/**
 * jsdom 20 chưa có `Blob.text()` và `Blob.arrayBuffer()` (trình duyệt thật có từ Safari 14).
 * Vá bằng `FileReader` — thứ jsdom có — để bài kiểm thử thử đúng mã thật chứ không phải thử
 * một bản chép tay khác.
 */
beforeAll(() => {
  function doc(khoi: Blob, cach: 'readAsText' | 'readAsArrayBuffer') {
    return new Promise((xong, hong) => {
      const may = new FileReader();
      may.onload = () => xong(may.result);
      may.onerror = () => hong(may.error);
      may[cach](khoi);
    });
  }

  Blob.prototype.text ??= function (this: Blob) {
    return doc(this, 'readAsText') as Promise<string>;
  };
  Blob.prototype.arrayBuffer ??= function (this: Blob) {
    return doc(this, 'readAsArrayBuffer') as Promise<ArrayBuffer>;
  };
});

beforeEach(() => {
  moBang.mockReset();
  // jsdom không có `URL.createObjectURL`/`revokeObjectURL`; trình duyệt thật thì luôn có.
  URL.revokeObjectURL = jest.fn();
});

/** Bản trả về của thư viện khi người dùng chọn xong một file. */
function daChon(ten: string, noiDung: string) {
  const file = new File([noiDung], ten, { type: 'application/octet-stream' });
  return {
    canceled: false as const,
    assets: [{ uri: 'blob:http://may/1', name: ten, size: noiDung.length, mimeType: '', file }],
    output: null,
  };
}

test('bấm huỷ thì trả về null, không ném lỗi', async () => {
  moBang.mockResolvedValue({ canceled: true, assets: null });

  expect(await chonFile()).toBeNull();
});

test('đọc được nội dung file ra chữ', async () => {
  moBang.mockResolvedValue(daChon('sao-luu.json', '{"a":1}') as never);

  const chon = await chonFile();

  expect(chon?.ten).toBe('sao-luu.json');
  expect(await chon?.text()).toBe('{"a":1}');
});

test('đọc được nội dung file ra byte', async () => {
  moBang.mockResolvedValue(daChon('cong.xlsx', 'PK') as never);

  const byte = await (await chonFile())?.bytes();

  expect(byte).toBeInstanceOf(Uint8Array);
  expect(Array.from(byte ?? [])).toEqual([0x50, 0x4b]);
});

test('không lọc kiểu file, để bên gọi tự soát đuôi tên', async () => {
  moBang.mockResolvedValue(daChon('cong.xlsx', 'PK') as never);

  await chonFile();

  // `type` để mặc định là mọi thứ. Lọc chặt thì file gửi qua Zalo bị làm mờ không bấm được.
  expect(moBang.mock.calls[0][0]).toEqual({ multiple: false, base64: false });
});

test('thu hồi địa chỉ blob, kẻo nhập nhiều file một buổi là đầy RAM', async () => {
  moBang.mockResolvedValue(daChon('cong.xlsx', 'PK') as never);

  await chonFile();

  expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:http://may/1');
});
