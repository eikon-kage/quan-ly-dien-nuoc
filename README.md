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
    Ui/So.cs, Ui/ChuViet.cs     đọc số kiểu "1.500.000", tìm kiếm không dấu
  QuanLyDienNuoc/               ứng dụng WinForms (net8.0-windows)
    Program.cs                  điểm khởi động, đặt ngôn ngữ vi-VN
    Ui/Theme.cs                 màu, phông chữ, lưới, nút dùng chung
    Forms/MainForm.cs           màn hình chính (khách hàng theo năm)
    Forms/DonHangForm.cs        hoá đơn và chi tiết hàng của một khách
    Forms/KhachHangForm.cs      thêm/sửa khách
    Forms/HoaDonForm.cs         thêm/sửa thông tin hoá đơn
    Forms/ThanhToanForm.cs      các lần trả tiền
    Forms/BangGiaForm.cs        bảng giá riêng theo khách
    Forms/VatTuForm.cs          danh mục vật tư
tests/
  QuanLyDienNuoc.Tests/         kiểm thử phần nghiệp vụ (xUnit): `dotnet test`
```

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
