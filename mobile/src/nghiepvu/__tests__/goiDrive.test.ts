import { danhSach, ghiDe, LoiDrive, taiVe, taoFile, xoa } from '../goiDrive';

const TOKEN = 'token-gia';

/** Thay `fetch` bằng hàng giả để bài kiểm thử không đụng vào mạng thật. */
const goi = jest.fn();
global.fetch = goi as unknown as typeof fetch;

/** Dựng một câu trả lời giả giống thứ `fetch` trả về. */
function traLoi(than: unknown, ma = 200) {
  return {
    ok: ma >= 200 && ma < 300,
    status: ma,
    json: () => Promise.resolve(than),
    text: () => Promise.resolve(typeof than === 'string' ? than : JSON.stringify(than)),
  };
}

/** Lấy ra (địa chỉ, tuỳ chọn) của lần gọi fetch thứ `thu`. */
function lanGoi(thu = 0): [string, RequestInit] {
  return goi.mock.calls[thu] as [string, RequestInit];
}

beforeEach(() => {
  goi.mockReset();
});

describe('gửi token', () => {
  test('mọi lệnh đều kèm Authorization', async () => {
    goi.mockResolvedValue(traLoi({ files: [] }));

    await danhSach(TOKEN);

    expect(lanGoi()[1].headers).toMatchObject({ Authorization: `Bearer ${TOKEN}` });
  });
});

describe('liệt kê', () => {
  test('bỏ file trong thùng rác, xin đủ id, tên và giờ sửa', async () => {
    goi.mockResolvedValue(traLoi({ files: [] }));

    await danhSach(TOKEN);

    const [diaChi] = lanGoi();
    expect(diaChi).toContain('trashed+%3D+false');
    expect(diaChi).toContain('fields=files%28id%2Cname%2CmodifiedTime%29');
  });

  test('đổi tên trường của Google sang tên tiếng Việt của app', async () => {
    goi.mockResolvedValue(
      traLoi({
        files: [{ id: 'f1', name: 'Cham-cong-2026-08-05.json', modifiedTime: '2026-08-05T09:00:00Z' }],
      }),
    );

    expect(await danhSach(TOKEN)).toEqual([
      { id: 'f1', ten: 'Cham-cong-2026-08-05.json', suaLuc: '2026-08-05T09:00:00Z' },
    ]);
  });

  test('Drive không trả trường files thì coi như chưa có bản nào', async () => {
    goi.mockResolvedValue(traLoi({}));

    expect(await danhSach(TOKEN)).toEqual([]);
  });
});

describe('tạo file', () => {
  test('gửi thông tin file và nội dung trong đúng một lần gọi', async () => {
    goi.mockResolvedValue(traLoi({ id: 'f1', name: 'Cham-cong-2026-08-05.json' }));

    await taoFile(TOKEN, 'Cham-cong-2026-08-05.json', '{"a":1}');

    expect(goi).toHaveBeenCalledTimes(1);

    const [diaChi, tuyChon] = lanGoi();
    expect(diaChi).toContain('/upload/drive/v3/files?uploadType=multipart');
    expect(tuyChon.method).toBe('POST');
    expect(tuyChon.headers).toMatchObject({
      'Content-Type': 'multipart/related; boundary=ranh-gioi-cham-cong',
    });

    const than = tuyChon.body as string;
    expect(than).toContain('{"name":"Cham-cong-2026-08-05.json","mimeType":"application/json"}');
    expect(than).toContain('{"a":1}');
    expect(than.endsWith('--ranh-gioi-cham-cong--')).toBe(true);
  });
});

describe('ghi đè', () => {
  test('chỉ gửi nội dung, không đụng tới tên file', async () => {
    goi.mockResolvedValue(traLoi({ id: 'f1', name: 'Cham-cong-2026-08-05.json' }));

    await ghiDe(TOKEN, 'f1', '{"a":2}');

    const [diaChi, tuyChon] = lanGoi();
    expect(diaChi).toContain('/upload/drive/v3/files/f1?uploadType=media');
    expect(tuyChon.method).toBe('PATCH');
    expect(tuyChon.body).toBe('{"a":2}');
  });
});

describe('tải về và xoá', () => {
  test('tải về lấy nội dung thô chứ không phải thông tin file', async () => {
    goi.mockResolvedValue(traLoi('{"a":1}'));

    expect(await taiVe(TOKEN, 'f1')).toBe('{"a":1}');
    expect(lanGoi()[0]).toContain('/drive/v3/files/f1?alt=media');
  });

  test('xoá', async () => {
    goi.mockResolvedValue(traLoi({}));

    await xoa(TOKEN, 'f1');

    expect(lanGoi()[1].method).toBe('DELETE');
  });
});

describe('lỗi', () => {
  test('giữ nguyên mã HTTP để bên ngoài phân biệt hết hạn với hỏng thật', async () => {
    goi.mockResolvedValue(traLoi({ error: { message: 'Invalid Credentials' } }, 401));

    await expect(danhSach(TOKEN)).rejects.toMatchObject({ ma: 401 });
  });

  test('lấy câu giải thích của Google làm lời báo lỗi', async () => {
    goi.mockResolvedValue(traLoi({ error: { message: 'Rate limit exceeded' } }, 403));

    await expect(danhSach(TOKEN)).rejects.toThrow('Rate limit exceeded');
  });

  test('Google trả lỗi không phải JSON thì vẫn báo được mã số', async () => {
    goi.mockResolvedValue({
      ok: false,
      status: 500,
      json: () => Promise.reject(new Error('không phải json')),
    });

    await expect(danhSach(TOKEN)).rejects.toThrow(LoiDrive);
    await expect(danhSach(TOKEN)).rejects.toThrow('Drive trả lỗi 500.');
  });
});
