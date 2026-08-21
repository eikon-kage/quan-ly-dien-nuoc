#!/usr/bin/env node
/**
 * Dựng bản web cài được lên màn hình chính (PWA).
 *
 * Hai việc, phải làm đúng thứ tự này:
 *
 * 1. `expo export -p web` — sinh ra `dist/`: trang gốc, file mã, font, và mấy file trong
 *    `public/` chép nguyên sang (manifest, icon, `sw.js`).
 *
 * 2. Điền vào `dist/sw.js` ba chỗ mà lúc viết chưa biết: **phiên bản** (băm từ nội dung),
 *    **địa chỉ trang gốc**, và **danh sách file nạp sẵn** — tên file mã có mã băm ở trong
 *    nên mỗi lần dựng lại là một tên khác.
 *
 * Vì sao phải có script chứ không gọi thẳng `expo export`: `sw.js` bắt buộc phải biết đúng
 * tên file mã của **bản vừa dựng**. Viết tay là sớm muộn quên, mà quên thì service worker
 * nạp sẵn tên file của bản cũ, người dùng mất mạng là mở ra trắng bảng — kiểu lỗi chỉ hiện
 * ra đúng lúc không còn cách nào chữa.
 *
 * Chạy:
 *     npm run build:web                                  # phát từ gốc tên miền
 *     GOC_WEB=/quan-ly-dien-nuoc npm run build:web        # phát từ địa chỉ con (GitHub Pages)
 *
 * **Nạp sẵn cái gì.** Trang gốc, toàn bộ `_expo/` (file mã), manifest và icon. Font thì
 * *không* nạp sẵn: chúng nằm trong `assets/` gần 5 MB mà máy chỉ tải đúng vài cái nó dùng,
 * nên để service worker giữ lại lúc dùng thật (luật "kho trước, mạng sau" trong
 * [sw.js](../public/sw.js)). Nghĩa là **lần mở đầu tiên phải có mạng**; từ lần sau thì mất
 * mạng vẫn mở được. Với app này thì đúng: cài xong ai cũng mở thử ngay ở nhà.
 */

import { execFileSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const THU_MUC = 'dist';
const GOC = process.env.GOC_WEB ?? '';

/** Mọi file trong một thư mục, đường dẫn tương đối và luôn dùng dấu `/` như trên web. */
function moiFile(thuMuc) {
  return readdirSync(thuMuc, { withFileTypes: true }).flatMap((muc) => {
    const duong = join(thuMuc, muc.name);
    return muc.isDirectory() ? moiFile(duong) : [relative(THU_MUC, duong).split(sep).join('/')];
  });
}

/** Địa chỉ web của một file trong `dist`, có kèm gốc. */
function diaChi(duong) {
  return `${GOC}/${duong}`;
}

function dungLen() {
  console.log(`Dựng bản web, gốc "${GOC === '' ? '/' : GOC}"…`);
  // `--clear` không phải cho chắc ăn suông: Metro giữ bản đã dựng trong bộ nhớ đệm và
  // **không** dựng lại khi chỉ có biến môi trường đổi. Đã thử: đổi khoá Supabase rồi dựng
  // lại mà ra đúng file cũ tới từng mã băm. Mà hai thứ đi vào bản này qua biến môi trường
  // thì đều là thứ sai một cái là hỏng cả bản: gốc địa chỉ (`GOC_WEB`) và khoá Supabase.
  // Chậm thêm gần một phút, đổi lấy việc không bao giờ đẩy lên một bản dựng từ khoá cũ.
  execFileSync('npx', ['expo', 'export', '-p', 'web', '--clear', '--output-dir', THU_MUC], {
    stdio: 'inherit',
  });
}

function vietServiceWorker() {
  const cacFile = moiFile(THU_MUC);

  const canSan = [
    // Trang gốc để trước: mất mạng thì đây là thứ duy nhất mở ra được.
    `${GOC}/`,
    ...cacFile.filter(
      (f) =>
        f.startsWith('_expo/') ||
        f === 'manifest.webmanifest' ||
        f === 'favicon.ico' ||
        /^(icon-\d+|apple-touch-icon)\.png$/.test(f),
    ).map(diaChi),
  ];

  /**
   * Phiên bản băm từ **nội dung** những file nạp sẵn, không phải từ giờ dựng: dựng lại mà
   * mã không đổi thì phiên bản giữ nguyên, máy người dùng không phải tải lại cả bản.
   */
  const bam = createHash('sha256');
  for (const duong of cacFile.filter((f) => f.startsWith('_expo/') || f === 'index.html')) {
    bam.update(readFileSync(join(THU_MUC, duong)));
  }
  const phienBan = bam.digest('hex').slice(0, 12);

  const duongSw = join(THU_MUC, 'sw.js');
  const ban = readFileSync(duongSw, 'utf8');
  if (!ban.includes('__PHIEN_BAN__')) {
    throw new Error('dist/sw.js không có chỗ __PHIEN_BAN__ — public/sw.js đã bị sửa mất khuôn?');
  }

  writeFileSync(
    duongSw,
    ban
      .replace('__PHIEN_BAN__', phienBan)
      .replace('__TRANG_GOC__', `${GOC}/`)
      .replace('__CAN_SAN__', JSON.stringify(canSan, null, 2)),
  );

  // GitHub Pages chạy Jekyll, mà Jekyll thì bỏ qua mọi thư mục mở đầu bằng dấu gạch dưới —
  // đúng cái tên `_expo/` chứa toàn bộ file mã. Có file này là nó không xen vào nữa.
  writeFileSync(join(THU_MUC, '.nojekyll'), '');

  const nang = canSan
    .slice(1)
    .reduce((tong, d) => tong + statSync(join(THU_MUC, d.slice(GOC.length + 1))).size, 0);

  console.log(`\nService worker: phiên bản ${phienBan}`);
  console.log(`Nạp sẵn ${canSan.length} file, ${(nang / 1024 / 1024).toFixed(1)} MB`);
  console.log(`Cả thư mục dist: ${(tongDungLuong() / 1024 / 1024).toFixed(1)} MB (font chưa`
    + ` dùng tới thì máy không tải, xem lời tựa script này)`);
}

function tongDungLuong() {
  return moiFile(THU_MUC).reduce((tong, f) => tong + statSync(join(THU_MUC, f)).size, 0);
}

dungLen();
vietServiceWorker();
