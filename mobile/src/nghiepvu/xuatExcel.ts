/**
 * Dựng nội dung file Excel xuất ra từ toàn bộ dữ liệu chấm công.
 *
 * Sáu trang, xếp theo thứ tự cần dùng: các kỳ đã quyết toán trước (sổ tiền đã trả), rồi
 * kỳ đang mở (số tiền sắp phải trả), rồi mới tới số liệu thô để tự lọc tự cộng. Đây vừa
 * là cách lấy số liệu ra khỏi điện thoại, vừa là bản sao lưu đọc được bằng Excel/WPS mà
 * không cần cài gì.
 *
 * **Hai trang đầu phải khớp từng đồng với hai màn hình Kỳ đã chốt và Bảng lương.** Trước
 * đây trang này cắt theo tháng trong khi app cắt theo kỳ — cùng một khoản tiền mà file và
 * máy ra hai bức tranh khác nhau, đối chiếu là loạn. Cắt theo tháng thì để trang Buổi công
 * lo, ở đó có sẵn cột ngày để tự lọc.
 */

import { DuLieuChamCong } from './kieu';
import { kyHienTai } from './ky';
import { tach, thu } from './ngayViet';
import { ghiChuNgay, luongTaiNgay, tatCaTho } from './thaoTac';
import { Cot, TrangTinh, taoFileExcel } from './xlsx';

const CHU_BUOI: Record<string, string> = { Sang: 'Sáng', Chieu: 'Chiều' };

/** Tên file gửi đi, ví dụ "Cham-cong-05-08-2026.xlsx". Không dấu cho khỏi lỗi khi gửi qua mạng. */
export function tenFileExcel(homNay: string): string {
  const { nam, thang, ngay } = tach(homNay);
  const hai = (so: number) => String(so).padStart(2, '0');
  return `Cham-cong-${hai(ngay)}-${hai(thang)}-${nam}.xlsx`;
}

function cot(nhan: string, rong: number, kieu: Cot['kieu'] = 'chu'): Cot {
  return { nhan, rong, kieu };
}

/**
 * Trang 1 — các kỳ đã quyết toán, mỗi thợ một dòng cho mỗi kỳ.
 *
 * Đứng đầu vì đây là sổ tiền đã trả: con số đã chốt, đã đưa tận tay, không tính lại nữa.
 * Mấy trang sau là số liệu để tự lọc tự cộng. Lấy thẳng từ bản chụp lúc chốt kỳ chứ
 * không tính lại từ buổi công — sau này có tăng lương thợ thì tờ cũ vẫn y nguyên.
 */
function trangQuyetToan(duLieu: DuLieuChamCong): TrangTinh {
  const dongs = duLieu.kyLuongs.flatMap((ky) =>
    ky.dongs.map((dong) => [
      ky.tuNgay,
      ky.denNgay,
      dong.tenTho,
      dong.congSang,
      dong.congChieu,
      dong.tongCong,
      dong.tienCong,
      dong.daUng,
      dong.noKyTruoc,
      dong.phaiTra,
      dong.daTra,
      dong.chuyenKySau,
    ]),
  );

  const cong = (cot: number) => dongs.reduce((tong, dong) => tong + (dong[cot] as number), 0);

  return {
    ten: 'Quyết toán',
    cots: [
      cot('Từ ngày', 13, 'ngay'),
      cot('Đến ngày', 13, 'ngay'),
      cot('Thợ', 26),
      cot('Công sáng', 11, 'so'),
      cot('Công chiều', 11, 'so'),
      cot('Tổng công', 11, 'so'),
      cot('Tiền công', 15, 'tien'),
      cot('Đã ứng', 15, 'tien'),
      cot('Nợ kỳ trước', 15, 'tien'),
      cot('Phải trả', 15, 'tien'),
      cot('Đã trả', 15, 'tien'),
      cot('Chuyển kỳ sau', 15, 'tien'),
    ],
    dongs,
    dongTong:
      dongs.length === 0
        ? undefined
        : [
            'Tổng cộng',
            null,
            null,
            cong(3),
            cong(4),
            cong(5),
            cong(6),
            cong(7),
            cong(8),
            cong(9),
            cong(10),
            cong(11),
          ],
  };
}

/**
 * Trang 2 — kỳ đang mở, mỗi thợ một dòng. Giống hệt màn hình Bảng lương, kể cả cột nợ
 * kỳ trước: xuất file lúc chưa chốt kỳ thì đây là số tiền sắp phải móc ví.
 */
function trangKyNay(duLieu: DuLieuChamCong, homNay: string): TrangTinh {
  const ky = kyHienTai(duLieu, homNay);

  const dongs = ky.dongs.map((dong) => [
    ky.tuNgay,
    ky.denNgay,
    dong.tho.ten,
    dong.congSang,
    dong.congChieu,
    dong.tongCong,
    dong.tienCong,
    dong.daUng,
    dong.noKyTruoc,
    dong.conLai,
  ]);

  const cong = (cot: number) => dongs.reduce((tong, dong) => tong + (dong[cot] as number), 0);

  return {
    ten: 'Kỳ này',
    cots: [
      cot('Từ ngày', 13, 'ngay'),
      cot('Đến ngày', 13, 'ngay'),
      cot('Thợ', 26),
      cot('Công sáng', 11, 'so'),
      cot('Công chiều', 11, 'so'),
      cot('Tổng công', 11, 'so'),
      cot('Tiền công', 15, 'tien'),
      cot('Đã ứng', 15, 'tien'),
      cot('Nợ kỳ trước', 15, 'tien'),
      cot('Còn phải trả', 15, 'tien'),
    ],
    dongs,
    dongTong:
      dongs.length === 0
        ? undefined
        : [
            'Tổng cộng',
            null,
            null,
            cong(3),
            cong(4),
            cong(5),
            cong(6),
            cong(7),
            cong(8),
            cong(9),
          ],
  };
}

/** Trang 3 — từng buổi đã chấm. Đây là chỗ tra khi thợ thắc mắc "hôm ấy tôi có đi mà". */
function trangBuoiCong(duLieu: DuLieuChamCong): TrangTinh {
  const tenTho = new Map(duLieu.thos.map((tho) => [tho.id, tho]));

  // Xếp như người ta đọc: theo ngày, trong ngày theo tên thợ, mỗi thợ sáng trước chiều sau.
  const thuTuBuoi = (buoi: string) => (buoi === 'Sang' ? 0 : 1);
  const dongs = [...duLieu.buoiCongs]
    .sort((a, b) => {
      if (a.ngay !== b.ngay) {
        return a.ngay < b.ngay ? -1 : 1;
      }

      const tenA = tenTho.get(a.thoId)?.ten ?? '';
      const tenB = tenTho.get(b.thoId)?.ten ?? '';
      const theoTen = tenA.localeCompare(tenB, 'vi', { sensitivity: 'base' });
      return theoTen !== 0 ? theoTen : thuTuBuoi(a.buoi) - thuTuBuoi(b.buoi);
    })
    .map((buoi) => {
      const tho = tenTho.get(buoi.thoId);
      const motCong = buoi.tienMotCong ?? (tho ? luongTaiNgay(tho, buoi.ngay) : 0);
      return [
        buoi.ngay,
        thu(buoi.ngay),
        tho?.ten ?? '(thợ đã bị xoá)',
        CHU_BUOI[buoi.buoi] ?? buoi.buoi,
        buoi.soCong,
        motCong,
        Math.round(buoi.soCong * motCong),
        // Ghi chú của ngày trước, ghi chú riêng của buổi sau: một trang một cột, mà cột
        // này là để người đọc hiểu *vì sao hôm ấy chấm thế* — ghi chú ngày trả lời câu
        // ấy, còn ghi chú buổi là ngoại lệ hiếm, chỉ hiện khi ngày không có chữ nào.
        ghiChuNgay(duLieu, buoi.thoId, buoi.ngay) || buoi.ghiChu,
      ];
    });

  const cong = (cot: number) => dongs.reduce((tong, dong) => tong + (dong[cot] as number), 0);

  return {
    ten: 'Buổi công',
    cots: [
      cot('Ngày', 12, 'ngay'),
      cot('Thứ', 10),
      cot('Thợ', 26),
      cot('Buổi', 9),
      cot('Số công', 9, 'so'),
      cot('Tiền một công', 15, 'tien'),
      cot('Thành tiền', 15, 'tien'),
      cot('Ghi chú', 26),
    ],
    dongs,
    dongTong:
      dongs.length === 0
        ? undefined
        : ['Tổng cộng', null, null, null, cong(4), null, cong(6), null],
  };
}

/** Trang 4 — các lần ứng tiền. */
function trangUngTien(duLieu: DuLieuChamCong): TrangTinh {
  const tenTho = new Map(duLieu.thos.map((tho) => [tho.id, tho.ten]));

  const dongs = [...duLieu.ungTiens]
    .sort((a, b) => (a.ngay < b.ngay ? -1 : 1))
    .map((ung) => [
      ung.ngay,
      thu(ung.ngay),
      tenTho.get(ung.thoId) ?? '(thợ đã bị xoá)',
      ung.soTien,
      ung.ghiChu,
    ]);

  return {
    ten: 'Ứng tiền',
    cots: [
      cot('Ngày', 12, 'ngay'),
      cot('Thứ', 10),
      cot('Thợ', 26),
      cot('Số tiền', 15, 'tien'),
      cot('Ghi chú', 30),
    ],
    dongs,
    dongTong:
      dongs.length === 0
        ? undefined
        : [
            'Tổng cộng',
            null,
            null,
            dongs.reduce((tong, dong) => tong + (dong[3] as number), 0),
            null,
          ],
  };
}

/** Trang 5 — danh sách thợ. */
function trangTho(duLieu: DuLieuChamCong, homNay: string): TrangTinh {
  return {
    ten: 'Thợ',
    cots: [
      cot('Tên thợ', 26),
      cot('Điện thoại', 15),
      cot('Đang làm', 10),
      cot('Tiền một công', 15, 'tien'),
      cot('Ngày vào làm', 13, 'ngay'),
      cot('Ghi chú', 30),
    ],
    dongs: tatCaTho(duLieu).map((tho) => [
      tho.ten,
      tho.dienThoai,
      tho.dangLam ? 'Có' : 'Đã nghỉ',
      luongTaiNgay(tho, homNay),
      tho.ngayTao,
      tho.ghiChu,
    ]),
  };
}

/**
 * Trang 6 — lịch sử tăng lương. Tách riêng vì một thợ có nhiều mốc; nhét vào trang Thợ
 * thì một thợ chiếm nhiều dòng, nhìn rối.
 */
function trangMocLuong(duLieu: DuLieuChamCong): TrangTinh {
  return {
    ten: 'Mốc lương',
    cots: [cot('Thợ', 26), cot('Từ ngày', 13, 'ngay'), cot('Tiền một công', 15, 'tien')],
    dongs: tatCaTho(duLieu).flatMap((tho) =>
      [...tho.mocLuong]
        .sort((a, b) => (a.tuNgay < b.tuNgay ? -1 : 1))
        .map((moc) => [tho.ten, moc.tuNgay, moc.tienMotCong]),
    ),
  };
}

/** Toàn bộ dữ liệu, dựng thành các trang của file Excel. */
export function cacTrangExcel(duLieu: DuLieuChamCong, homNay: string): TrangTinh[] {
  return [
    trangQuyetToan(duLieu),
    trangKyNay(duLieu, homNay),
    trangBuoiCong(duLieu),
    trangUngTien(duLieu),
    trangTho(duLieu, homNay),
    trangMocLuong(duLieu),
  ];
}

/** Toàn bộ dữ liệu thành khối byte của một file .xlsx. */
export function xuatExcel(duLieu: DuLieuChamCong, homNay: string): Uint8Array {
  return taoFileExcel(cacTrangExcel(duLieu, homNay));
}
