/**
 * Thêm đúng một thứ vào [app.json](./app.json): **địa chỉ gốc của bản web**.
 *
 * Expo đọc app.json trước rồi đưa vào đây (`config`), nên app.json vẫn là chỗ khai mọi thứ
 * khác — file này không sửa gì của Android/iOS cả.
 *
 * Vì sao phải có: bản web đẩy lên GitHub Pages nằm ở địa chỉ con
 * `…github.io/quan-ly-dien-nuoc/`, mà Expo thì chèn đường dẫn file mã bắt đầu bằng `/` vào
 * trang. Không khai gốc thì trang ấy đi tìm `/_expo/…` ở ngoài gốc tên miền và trắng bảng.
 *
 * Để trong biến môi trường chứ không viết cứng, vì cùng một mã nguồn dựng ra hai chỗ khác
 * nhau: chạy thử ở nhà (`npm run web`) thì gốc là `/`, còn đẩy lên Pages thì
 * `GOC_WEB=/quan-ly-dien-nuoc`. Đổi sang Cloudflare Pages hay tên miền riêng — chỗ phát từ
 * gốc — thì bỏ biến ấy đi là xong.
 */

function chuanHoaGoc(goc) {
  if (goc === '') {
    return null;
  }
  if (!goc.startsWith('/') || goc.endsWith('/')) {
    // Sai một dấu gạch là cả trang trắng mà không có câu báo nào. Nói ngay ở đây.
    throw new Error(`GOC_WEB phải mở đầu bằng "/" và không có "/" ở cuối. Đang nhận: "${goc}"`);
  }
  return goc;
}

module.exports = ({ config }) => {
  const goc = chuanHoaGoc(process.env.GOC_WEB ?? '');
  if (goc === null) {
    return config;
  }

  return { ...config, experiments: { ...config.experiments, baseUrl: goc } };
};
