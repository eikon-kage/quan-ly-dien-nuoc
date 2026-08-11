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

/** Số ngày của một tháng. */
export function soNgayTrongThang(nam: number, thang: number): number {
  return new Date(Date.UTC(nam, thang, 0)).getUTCDate();
}

/**
 * Các tháng mà một khoảng ngày đi qua, từ cũ tới mới. Kỳ lương chốt lúc nào cũng được
 * nên nó có thể vắt qua hai ba tháng, mà tờ lịch thì vẽ từng tháng một.
 */
export function cacThangTrongKhoang(
  tuNgay: string,
  denNgay: string,
): { nam: number; thang: number }[] {
  const dau = tach(tuNgay);
  const cuoi = tach(denNgay);

  const cacThang: { nam: number; thang: number }[] = [];
  let nam = dau.nam;
  let thang = dau.thang;
  while (nam < cuoi.nam || (nam === cuoi.nam && thang <= cuoi.thang)) {
    cacThang.push({ nam, thang });
    if (thang === 12) {
      nam += 1;
      thang = 1;
    } else {
      thang += 1;
    }
  }

  return cacThang;
}

/** Khoảng ngày viết gọn cho đầu trang: "20/07 → 05/08". */
export function khoangGon(tuNgay: string, denNgay: string): string {
  return `${ngayGon(tuNgay).slice(0, 5)} → ${ngayGon(denNgay).slice(0, 5)}`;
}

/** Tên bảy cột của tờ lịch, cũng kể từ Thứ Hai. */
export const COT_LICH = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

/**
 * Các ô của tờ lịch một tháng, xếp thành từng hàng bảy ô. Ô `null` là chỗ trống ở đầu
 * và cuối tháng, chỗ mà tháng chưa bắt đầu hoặc đã hết.
 */
export function oLichThang(nam: number, thang: number): (number | null)[][] {
  // Dời cột đi một để Thứ Hai đứng đầu: Chủ Nhật (0) rơi xuống cuối hàng.
  const cotDau = (soThu(ghep(nam, thang, 1)) + 6) % 7;

  const o: (number | null)[] = [
    ...Array.from({ length: cotDau }, () => null),
    ...Array.from({ length: soNgayTrongThang(nam, thang) }, (_, i) => i + 1),
  ];
  while (o.length % 7 !== 0) {
    o.push(null);
  }

  const hangs: (number | null)[][] = [];
  for (let i = 0; i < o.length; i += 7) {
    hangs.push(o.slice(i, i + 7));
  }

  return hangs;
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

/**
 * Một mốc thời gian ISO viết gọn theo giờ máy: "05/08, 16:12".
 *
 * Dùng cho những thứ *xảy ra lúc mấy giờ* như lần sao lưu gần nhất — khác với các hàm
 * trên, chỗ này nhận chuỗi ISO đầy đủ chứ không phải "yyyy-MM-dd". Chuỗi hỏng thì trả về
 * rỗng để chỗ gọi bỏ qua, đừng hiện "Invalid Date" lên màn hình.
 */
export function gioPhut(iso: string): string {
  const luc = new Date(iso);
  if (Number.isNaN(luc.getTime())) {
    return '';
  }
  const hai = (so: number) => String(so).padStart(2, '0');
  return `${hai(luc.getDate())}/${hai(luc.getMonth() + 1)}, ${hai(luc.getHours())}:${hai(luc.getMinutes())}`;
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
