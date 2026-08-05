/**
 * Viết ngày, số công và số tiền theo kiểu người Việt đọc.
 *
 * Không dùng Intl hay toLocaleDateString vì kết quả đổi theo ngôn ngữ đang cài trên máy —
 * điện thoại để tiếng Anh sẽ ra "Monday" và "1,500,000".
 */

const TEN_THU = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];

/** Tách chuỗi "2026-08-03" thành số. */
export function tach(ngay: string): { nam: number; thang: number; ngay: number } {
  const [nam, thang, ngayTrongThang] = ngay.split('-').map(Number);
  return { nam, thang, ngay: ngayTrongThang };
}

/** Ghép số thành chuỗi "2026-08-03". */
export function ghep(nam: number, thang: number, ngay: number): string {
  const hai = (so: number) => String(so).padStart(2, '0');
  return `${nam}-${hai(thang)}-${hai(ngay)}`;
}

/** Hôm nay theo giờ của máy, dạng "yyyy-MM-dd". */
export function homNay(): string {
  const bayGio = new Date();
  return ghep(bayGio.getFullYear(), bayGio.getMonth() + 1, bayGio.getDate());
}

/** Cộng (hoặc trừ) số ngày. Dùng UTC để không bị lệch vì giờ mùa hè. */
export function congNgay(ngay: string, soNgay: number): string {
  const { nam, thang, ngay: n } = tach(ngay);
  const moc = new Date(Date.UTC(nam, thang - 1, n + soNgay));
  return ghep(moc.getUTCFullYear(), moc.getUTCMonth() + 1, moc.getUTCDate());
}

/** Thứ dưới dạng số: 0 là Chủ Nhật, 1 là Thứ Hai... Dùng để xếp ngày vào cột trong lịch. */
export function soThu(ngay: string): number {
  const { nam, thang, ngay: n } = tach(ngay);
  return new Date(Date.UTC(nam, thang - 1, n)).getUTCDay();
}

export function thu(ngay: string): string {
  return TEN_THU[soThu(ngay)];
}

/** Thứ viết tắt, đủ chỗ cho một ô hẹp trên dải ngày: "T2".."T7", "CN". */
export function thuGon(ngay: string): string {
  const so = soThu(ngay);
  return so === 0 ? 'CN' : `T${so + 1}`;
}

/**
 * Bảy ngày của tuần chứa ngày này, kể từ Thứ Hai. Tuần bắt đầu từ Thứ Hai giống lịch
 * treo tường bán ngoài hàng, chứ không bắt đầu từ Chủ Nhật kiểu Mỹ.
 */
export function tuan(ngay: string): string[] {
  const so = soThu(ngay);
  const dauTuan = congNgay(ngay, -(so === 0 ? 6 : so - 1));
  return Array.from({ length: 7 }, (_, i) => congNgay(dauTuan, i));
}

/** Kiểu hiện trên đầu màn hình chấm công: "Thứ Hai 03/08". */
export function thuVaNgay(ngay: string): string {
  const { thang, ngay: n } = tach(ngay);
  const hai = (so: number) => String(so).padStart(2, '0');
  return `${thu(ngay)} ${hai(n)}/${hai(thang)}`;
}

/** Ngày viết gọn kiểu Việt: "2026-08-03" thành "03/08/2026". */
export function ngayGon(ngay: string): string {
  const { nam, thang, ngay: n } = tach(ngay);
  const hai = (so: number) => String(so).padStart(2, '0');
  return `${hai(n)}/${hai(thang)}/${nam}`;
}

/** Số công viết gọn: 1 công ra "1", nửa công ra "0,5". */
export function soCong(so: number): string {
  return String(Math.round(so * 10) / 10).replace('.', ',');
}

/**
 * Dấu trừ đứng trước số tiền. Dùng dấu trừ toán học U+2212 chứ không phải dấu gạch nối
 * "-": gạch nối ngắn và thấp, đứng cạnh chữ số nhìn như vết bẩn. Cả app chỉ dùng hằng số
 * này, đừng gõ thẳng ký tự vào chỗ khác kẻo mỗi chỗ một kiểu.
 */
export const DAU_TRU = '−';

/** Tiền viết đủ chữ số kèm đơn vị: "1.500.000 đ". Số âm có dấu trừ ở đầu. */
export function tien(soTien: number): string {
  const am = soTien < 0;
  const nguyen = Math.round(Math.abs(soTien));
  const chu = String(nguyen).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return `${am ? DAU_TRU : ''}${chu} đ`;
}

/** Số tiền bị trừ đi: "−500.000 đ". Dùng cho tiền đã ứng. */
export function tienTru(soTien: number): string {
  return `${DAU_TRU}${tien(Math.abs(soTien))}`;
}
