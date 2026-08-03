# Quản lý đơn hàng – Cửa hàng điện nước

Phần mềm desktop (Windows Forms, .NET 8) quản lý hoá đơn mua hàng của khách tại cửa hàng
điện nước. Mỗi khách có một (hoặc nhiều) hoá đơn kéo dài nhiều ngày; mỗi lần khách lấy hàng
thì thêm một dòng vào hoá đơn đó. Mỗi khách có bảng giá riêng cho từng loại vật tư.

## Tính năng

- **Màn hình chính**: danh sách khách hàng lọc theo năm (mặc định là năm hiện tại), kèm
  tổng mua / đã trả / còn nợ của khách trong năm đó. Tìm khách không dấu (gõ `nguyen` ra
  `Nguyễn`). Bấm đúp hoặc Enter để mở đơn hàng của khách.
- **Đơn hàng của khách**: cột trái là các hoá đơn trong năm, cột phải là chi tiết hàng đã lấy
  theo từng ngày (ngày, tên hàng, đơn vị, đơn giá, số lượng, thành tiền, ghi chú).
- **Thêm nhanh**: thanh nhập ngay trên lưới; chọn tên hàng là tự điền đơn vị và **giá của
  đúng khách đó**; gõ số lượng rồi Enter là xong một dòng. Tên hàng chưa có trong danh mục
  thì gõ mới, phần mềm tự thêm vào danh mục.
- **Sửa trực tiếp trên lưới như Excel**: bấm đúp (hoặc F2) vào ô để sửa, mọi thay đổi tự lưu.
- **Hoàn tác / Làm lại**: `Ctrl+Z` / `Ctrl+Y` cho mọi thao tác (thêm, sửa, xoá dòng, xoá hoá
  đơn, xoá khách…). Lịch sử chỉ giữ trong phiên đang mở, tối đa 50 bước — đóng phần mềm là
  hết, dữ liệu đã lưu vẫn còn nguyên.
- **Bảng giá riêng của khách**: khi nhập giá khác với giá đang lưu, phần mềm hỏi có dùng giá
  mới cho những lần sau không. Xem và sửa toàn bộ bảng giá của khách ở nút *Bảng giá của khách*.
- **Thanh toán**: ghi nhiều lần trả tiền cho một hoá đơn, tự tính còn nợ.
- **Chốt hoá đơn**: hoá đơn đã chốt thì khoá không cho sửa, cần thì mở lại.
- **Danh mục vật tư**: giá chung của cửa hàng, dùng khi khách chưa có giá riêng.
- **In hoá đơn**: xem trước đúng như tờ giấy sẽ in rồi in thẳng ra máy in. Không cần máy
  có Excel hay WPS. Nhiều hàng thì tự chia trang, trang giữa ghi *Cộng trang này*, trang
  cuối ghi *Tổng cộng* kèm số tiền bằng chữ.
- **Xuất Excel**: điền dữ liệu vào đúng file mẫu `.xls` của cửa hàng (trang 1 có tiêu đề,
  các trang sau chỉ có bảng), giữ nguyên khung kẻ và độ rộng cột.
- **Nhập từ Excel**: đọc ngược file hoá đơn Excel — kể cả các file cũ làm bằng WPS — vào
  phần mềm. Cho chọn lấy bảng nào trong file, đặt ngày lấy hàng, xem trước rồi mới nhập.
  File cũ thiếu đơn giá thì tự tính lại từ thành tiền và báo lại để kiểm.

## Phím tắt

| Phím | Tác dụng |
|---|---|
| `Ctrl+Z` / `Ctrl+Y` | Hoàn tác / Làm lại |
| `Enter` | Thêm dòng hàng (khi đang ở thanh nhập nhanh) |
| `F2` hoặc bấm đúp | Sửa ô đang chọn trên lưới |
| `F3` | Nhảy về ô Tên hàng / ô tìm kiếm |
| `Delete` | Xoá dòng hàng đang chọn |
| `Ctrl+N` | Thêm khách hàng (ở màn hình chính) |
| `Esc` | Đóng cửa sổ |

## Mẫu hoá đơn giấy

Hai file mẫu nằm trong thư mục `MauHoaDon` cạnh file chạy, dựng từ chính hoá đơn thật của
cửa hàng:

| File | Dùng cho | Sức chứa |
|---|---|---|
| `trang-1.xls` | Trang đầu: tiêu đề cửa hàng, tên khách, địa chỉ | 32 dòng hàng |
| `trang-sau.xls` | Trang thứ hai trở đi: chỉ có bảng | 35 dòng hàng |

Sửa được bằng Excel/WPS: đổi tên cửa hàng, địa chỉ, số điện thoại ở mấy dòng đầu là cả
bản in trong phần mềm lẫn file xuất ra đều đổi theo. Nếu thêm/bớt số dòng của bảng thì
phải sửa lại toạ độ trong [MauHoaDon.cs](src/QuanLyDienNuoc.Core/Excel/MauHoaDon.cs).

Các file hoá đơn gốc của cửa hàng để ở `docs/hoa-don-mau/` trên máy, **không đưa lên git**
(có số điện thoại và số tài khoản ngân hàng). Bản đã ẩn danh dùng cho kiểm thử nằm ở
`tests/QuanLyDienNuoc.Tests/HoaDonMau/` — giữ nguyên cấu trúc file (số tab, tab biểu đồ,
các ô lệch chuẩn) nhưng đã thay tên khách, số điện thoại và xoá khối tài khoản ngân hàng.

## Dữ liệu

Toàn bộ dữ liệu nằm trong một file JSON:

```
%APPDATA%\QuanLyDienNuoc\dulieu.json
```

Mỗi lần ghi đều giữ lại bản trước đó ở `dulieu.json.bak`. Sao lưu = copy file này đi nơi khác;
khôi phục = chép đè lại rồi mở phần mềm.

## Yêu cầu

- Windows 10 trở lên (WinForms chỉ chạy trên Windows)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (không bắt buộc)

## Cấu trúc mã nguồn

```
QuanLyDienNuoc.sln
src/
  QuanLyDienNuoc.Core/          thư viện nghiệp vụ, không phụ thuộc giao diện (net8.0)
    Models/                     KhachHang, VatTu, HoaDon, ChiTietHoaDon, ThanhToan
    Data/KhoDuLieu.cs           đọc/ghi JSON + lịch sử hoàn tác
    Excel/MauHoaDon.cs          toạ độ các ô trên mẫu hoá đơn
    Excel/XuatHoaDon.cs         điền hoá đơn vào mẫu Excel, chia trang
    Excel/DocHoaDon.cs          đọc ngược file Excel thành dòng hàng
    Excel/ThongTinCuaHang.cs    đọc phần đầu hoá đơn từ file mẫu
    Ui/So.cs, Ui/ChuViet.cs     đọc số kiểu "1.500.000", tìm kiếm không dấu
    Ui/DocSo.cs                 đọc số tiền thành chữ
  QuanLyDienNuoc/               ứng dụng WinForms (net8.0-windows)
    Program.cs                  điểm khởi động, đặt ngôn ngữ vi-VN
    MauHoaDon/                  hai file mẫu hoá đơn giấy
    Ui/Theme.cs                 màu, phông chữ, lưới, nút dùng chung
    Ui/InHoaDon.cs              vẽ hoá đơn ra giấy để xem trước và in
    Forms/MainForm.cs           màn hình chính (khách hàng theo năm)
    Forms/DonHangForm.cs        hoá đơn và chi tiết hàng của một khách
    Forms/KhachHangForm.cs      thêm/sửa khách
    Forms/HoaDonForm.cs         thêm/sửa thông tin hoá đơn
    Forms/ThanhToanForm.cs      các lần trả tiền
    Forms/BangGiaForm.cs        bảng giá riêng theo khách
    Forms/VatTuForm.cs          danh mục vật tư
    Forms/XemTruocForm.cs       xem trước bản in
    Forms/NhapExcelForm.cs      nhập hoá đơn từ file Excel
tests/
  QuanLyDienNuoc.Tests/         kiểm thử phần nghiệp vụ (xUnit): `dotnet test`
.github/workflows/
  anh-giao-dien.yml             dựng trên máy Windows của GitHub, chụp ảnh từng màn hình
```

## Xem giao diện mà không có máy Windows

Đẩy mã nguồn lên GitHub là workflow `anh-giao-dien.yml` tự chạy: dựng phần mềm trên máy
Windows, chạy toàn bộ kiểm thử, mở lần lượt từng màn hình và chụp lại thành ảnh PNG, kèm
cả ảnh bản in khổ A4. Vào tab **Actions** → chọn lần chạy → tải mục **anh-giao-dien** ở
cuối trang. Cùng chỗ đó có sẵn bản `.exe` đã đóng gói.

Chạy tay ở máy Windows cũng được:

```bash
dotnet run --project src/QuanLyDienNuoc -- --chup-anh anh-giao-dien
```

Chế độ này dùng dữ liệu mẫu trong thư mục ảnh, không đụng vào dữ liệu thật.

Giao diện viết bằng code (không dùng designer) để chữ và dòng đều to, dễ nhìn.

## Build và chạy

```bash
dotnet build
dotnet run --project src/QuanLyDienNuoc
```

Hoặc mở `QuanLyDienNuoc.sln` bằng Visual Studio rồi bấm F5.

## Đóng gói thành 1 file exe

```bash
dotnet publish src/QuanLyDienNuoc -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

File chạy nằm ở `src/QuanLyDienNuoc/bin/Release/net8.0-windows/win-x64/publish/QuanLyDienNuoc.exe`,
copy sang máy khác dùng được ngay, không cần cài .NET.
