# Nhập danh sách khách hàng từ file

Cửa hàng nào cũng có sẵn danh sách khách trên Excel (hoặc sổ tay đã ai đó gõ lại). Gõ tay
lại từng khách vào phần mềm mất cả buổi, nên màn hình chính có nút **Nhập từ file**, ngay
cạnh nút *+ Thêm khách hàng*.

## Người dùng làm gì

1. Bấm **Nhập từ file** ở góc trên phải màn hình chính.
2. Chưa có file thì bấm **Tải file mẫu...** ngay trong màn hình vừa mở, chọn chỗ lưu. Phần
   mềm ghi ra một file `.xlsx` gồm:
   - sheet **Khách hàng** — chỉ có dòng tiêu đề đánh số:
     `1. TÊN KHÁCH HÀNG (bắt buộc)` · `2. ĐIỆN THOẠI` · `3. ĐỊA CHỈ` · `4. GHI CHÚ`;
   - sheet **Hướng dẫn** — 5 bước điền, kèm hai dòng ví dụ.
3. Điền mỗi khách một dòng vào sheet *Khách hàng*, lưu lại.
4. Bấm **Chọn file...**, trỏ vào file vừa điền. Bảng xem trước hiện lên ngay.
5. Soát bảng, sửa thẳng trên bảng nếu cần, rồi bấm **NHẬP … KHÁCH VÀO SỔ**.

Bản mẫu để xem trước ở [mau-danh-sach-khach-hang.xlsx](mau-danh-sach-khach-hang.xlsx) —
chính file phần mềm sinh ra, không phải bản chép tay.

## Làm sao người dùng biết cột nào là cột mấy

Ba chỗ nói cùng một chuyện, không cần đọc tài liệu:

- **Phụ đề màn hình**: *"File theo mẫu, cột xếp đúng thứ tự: 1 tên khách hàng · 2 điện thoại
  · 3 địa chỉ · 4 ghi chú"*.
- **Dòng tiêu đề trong file mẫu** có sẵn số thứ tự, người dùng điền ngay dưới nó.
- **Tên cột trên bảng xem trước** cũng mang số: `1 · TÊN KHÁCH HÀNG`, `2 · ĐIỆN THOẠI`,
  `3 · ĐỊA CHỈ`, `4 · GHI CHÚ`. Mở bảng ra là thấy cột 1 của file đã vào đúng ô tên khách
  hay chưa — đây mới là chỗ người dùng thật sự *biết* file đọc đúng, chứ không phải lời hứa
  ở tài liệu.

Cột **DÒNG** trên bảng là số dòng đúng như Excel hiện ở lề trái, để dò lại tận chỗ trong file.

## Quy tắc đọc file

Cài trong [`Excel/NhapKhachHang.cs`](../src/QuanLyDienNuoc.Core/Excel/NhapKhachHang.cs):

- **Nhận cột theo chữ ở dòng tiêu đề trước**, so không dấu và bỏ qua số: `Tên khách hàng`,
  `Họ tên`, `SĐT`, `Số điện thoại`, `Địa chỉ`, `Ghi chú`, cả `Name`/`Phone`/`Address`/`Note`.
  Nhờ vậy người dùng đổi chỗ cột, hay chèn thêm cột lạ (`STT`, `Nợ cũ`) vẫn đọc đúng.
- **File không có tiêu đề** thì mới đọc theo đúng thứ tự cột của file mẫu (1-2-3-4), và màn
  hình hiện dải cảnh báo màu cam nói rõ là đang đọc theo thứ tự cột — đoán thì phải nói ra.
- Dò tiêu đề trong 20 dòng đầu, phải có cột tên khách và ít nhất hai cột nhận ra được mới
  coi là tiêu đề. File nhiều sheet thì lấy sheet đầu tiên có tiêu đề đọc được (nên file mẫu
  có thêm sheet *Hướng dẫn* nằm cạnh cũng không lẫn).
- Đọc được cả `.xlsx`, `.xls` và `.csv` (CSV xuất từ Excel bản Việt hay dùng dấu `;` — tự dò
  dấu tách).
- **Số điện thoại điền vào ô kiểu số** bị Excel cắt số 0 đầu: thấy đúng 9 chữ số mà không bắt
  đầu bằng 0 thì trả lại số 0. Cột điện thoại trong file mẫu đã đặt kiểu chữ để không mất.
- Dòng trống hẳn bị bỏ im, không tính vào câu tổng kết.

## Chấm từng dòng trước khi ghi vào sổ

Mỗi dòng có một tình trạng, hiện ở cột **TÌNH TRẠNG**:

| Tình trạng | Nghĩa | Tích sẵn |
| --- | --- | --- |
| Thêm mới (xanh) | khách chưa có trong phần mềm | có |
| `Đã có khách "..." — bỏ qua` (cam) | trùng tên (so không dấu) với khách đã có | không |
| Trùng dòng phía trên — bỏ qua (cam) | hai dòng trong file cùng một tên | không |
| Thiếu tên khách (đỏ) | cột 1 để trống, không ghi được | không |

- Bốn cột dữ liệu **sửa được ngay trên bảng**: sửa tên xong là tình trạng tự chấm lại.
- Người dùng tự tay tích/bỏ tích dòng nào thì lần chấm lại sau **không đè lên** ý đó — cố
  tình thêm một khách trùng tên (hai người thật cùng tên) thì tích tay là được, sửa dòng
  khác cũng không mất tích vừa đặt.
- Trong lô sắp nhập còn dòng trùng tên thì bấm Nhập sẽ hỏi lại một lần: thêm nữa là một
  người thành hai khách, công nợ bị chia đôi.
- Cả lô ghi vào sổ bằng **một** việc trong nhật ký (`Nhập N khách hàng từ file`), nên
  `Ctrl+Z` một lần là bỏ hết, không phải xoá lại từng người.

## Kiểm thử

[`tests/QuanLyDienNuoc.Tests/NhapKhachTests.cs`](../tests/QuanLyDienNuoc.Tests/NhapKhachTests.cs)
— xuất file mẫu rồi đọc lại (không ra khách ảo từ mấy dòng ví dụ), đổi chỗ cột, thêm cột lạ,
file không tiêu đề, trùng tên với khách cũ và trùng trong cùng file, thiếu tên, dòng trống,
số điện thoại kiểu số, file CSV dấu `;`.
