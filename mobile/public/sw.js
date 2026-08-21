/**
 * Service worker của bản web: giữ cho sổ chấm công **mở được khi mất mạng**.
 *
 * Việc này cần thật, không phải làm cho đủ bộ: người dùng app này chấm công ngoài công
 * trình, chỗ sóng có lúc không có. App vốn đã giữ toàn bộ sổ ngay trong máy nên chỉ còn
 * thiếu đúng một thứ khi mất mạng — mấy file mã của chính trang này.
 *
 * **Ba luật, chia theo cái gì được phép cũ.**
 *
 * 1. Việc gửi lên Supabase (`POST`, `PATCH`, …) và mọi địa chỉ ngoài: **không chen vào**.
 *    Sổ của cả nhóm phải luôn là bản thật trên máy chủ, không bao giờ là bản giữ lại.
 *
 * 2. Mở trang: **mạng trước, kho sau**. Trang gốc trỏ tới tên file mã có mã băm ở trong;
 *    lấy trang cũ trong kho ra trước là mở mãi bản cũ dù đã đẩy bản mới lên. Mất mạng mới
 *    lấy bản đã giữ.
 *
 * 3. Còn lại (file mã, font, icon): **kho trước, mạng sau**. Tên mấy file ấy đã có mã băm
 *    nội dung nên nội dung không bao giờ đổi dưới cùng một tên — giữ lại là an toàn tuyệt
 *    đối, và cũng là chỗ tiết kiệm được nhiều nhất.
 *
 * Mỗi lần dựng lại là một tên kho mới (`PHIEN_BAN` băm từ nội dung), và lúc kho mới nhận
 * việc thì xoá sạch kho cũ — không để hai bản nằm chồng chiếm chỗ trong máy người dùng.
 *
 * **File này là bản mẫu.** Ba chỗ `__…__` do [dung-web.mjs](../scripts/dung-web.mjs) điền
 * lúc dựng, vì lúc viết thì chưa biết tên file mã lẫn địa chỉ gốc.
 */

const PHIEN_BAN = '__PHIEN_BAN__';
const TRANG_GOC = '__TRANG_GOC__';
const CAN_SAN = __CAN_SAN__;

const TEN_KHO = `cham-cong-${PHIEN_BAN}`;

self.addEventListener('install', (su) => {
  su.waitUntil(
    (async () => {
      const kho = await caches.open(TEN_KHO);
      // Nạp từng file một chứ không `addAll`: `addAll` mà trượt một file là hỏng cả kho,
      // lúc ấy người dùng mất luôn phần chạy offline chỉ vì một cái font lẻ.
      await Promise.all(
        CAN_SAN.map((diaChi) =>
          kho.add(new Request(diaChi, { cache: 'reload' })).catch(() => {}),
        ),
      );
      // Nhận việc ngay, không đợi người dùng đóng hết tab: bản mới càng sớm thay bản cũ
      // càng ít cảnh hai tab chạy hai bản khác nhau.
      await self.skipWaiting();
    })(),
  );
});

self.addEventListener('activate', (su) => {
  su.waitUntil(
    (async () => {
      const ten = await caches.keys();
      await Promise.all(
        ten
          .filter((t) => t.startsWith('cham-cong-') && t !== TEN_KHO)
          .map((t) => caches.delete(t)),
      );
      await self.clients.claim();
    })(),
  );
});

self.addEventListener('fetch', (su) => {
  const yeuCau = su.request;

  // Luật 1: chỉ lo việc đọc, và chỉ lo file của chính trang này.
  if (yeuCau.method !== 'GET') {
    return;
  }
  if (new URL(yeuCau.url).origin !== self.location.origin) {
    return;
  }

  // Luật 2 và 3.
  su.respondWith(yeuCau.mode === 'navigate' ? moTrang(su) : layFile(su));
});

/** Mạng trước, kho sau. Mỗi lần mở được từ mạng thì giữ lại bản mới nhất cho lần mất mạng. */
async function moTrang(su) {
  const kho = await caches.open(TEN_KHO);

  try {
    const traVe = await fetch(su.request);
    su.waitUntil(kho.put(TRANG_GOC, traVe.clone()));
    return traVe;
  } catch {
    const daCo = await kho.match(TRANG_GOC);
    return daCo ?? Response.error();
  }
}

/** Kho trước, mạng sau. File lấy được từ mạng thì giữ luôn, để lần sau mất mạng vẫn có. */
async function layFile(su) {
  const kho = await caches.open(TEN_KHO);

  const daCo = await kho.match(su.request);
  if (daCo !== undefined) {
    return daCo;
  }

  try {
    const traVe = await fetch(su.request);
    // Chỉ giữ bản trả về lành lặn. Giữ cả bản lỗi 404 hay 500 thì lần sau mất mạng lấy ra
    // đúng cái lỗi ấy, mà lúc ấy thì không còn cách nào chữa.
    if (traVe.ok) {
      su.waitUntil(kho.put(su.request, traVe.clone()));
    }
    return traVe;
  } catch {
    return Response.error();
  }
}
