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
- **Sổ công nợ**: một màn hình cho cả cửa hàng — ai đang nợ, nợ bao nhiêu và **nợ đã bao
  nhiêu ngày** (tính từ lần lấy hàng hoặc trả tiền gần nhất), xếp sẵn theo nợ lâu nhất.
  Mở phần mềm lên là có ngay dải nhắc *"3 khách nợ quá 60 ngày"* trên đầu màn hình.
- **Tin nhắc nợ soạn sẵn**: bấm một nút ra đoạn tin kèm bảng kê từng hoá đơn còn nợ và số
  tiền bằng chữ, sửa vài chữ rồi chép sang Zalo là gửi được.
- **Thêm nhanh**: thanh nhập ngay trên lưới; chọn tên hàng là tự điền đơn vị và **giá của
  đúng khách đó**; gõ số lượng rồi Enter là xong một dòng. Tên hàng chưa có trong danh mục
  thì gõ mới, phần mềm tự thêm vào danh mục.
- **Gõ tắt tên hàng**: gõ `o27` (mã tắt tự đặt trong danh mục vật tư), `ong 27` không dấu,
  hay `27 ong` ngược thứ tự đều ra `Ống nhựa PVC D27`.
- **Tính ngay trong ô**: gõ `3+2*4` vào ô số lượng hoặc đơn giá là ra `11`, khỏi bấm máy
  tính riêng. Dùng được cả trên lưới lẫn thanh nhập nhanh.
- **Nhập nhiều dòng một lượt**: gõ `ống 27 x10, co 90 x5, keo x1` rồi xem trước giá trước
  khi ghi vào hoá đơn.
- **Bộ hàng thường dùng**: gom các món hay đi cùng nhau thành một bộ ("Bộ lắp bồn nước"),
  chọn một lần là ra đủ dòng, giá vẫn lấy theo bảng giá của khách.
- **Chép lại một ngày** và **nhân đôi dòng** (`Ctrl+D`): khách quen lấy lại đúng bộ hàng cũ
  thì khỏi gõ lại từng món.
- **Cảnh báo nhập sai**: giá lệch quá 20% so với lần gần nhất bán cho chính khách đó, dòng
  trùng y hệt (cùng ngày, cùng hàng, cùng số lượng), hoặc thêm khách trùng tên — đều hỏi lại
  trước khi ghi.
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
- **Sao lưu**: mỗi ngày mở phần mềm là tự sao lưu một bản, giữ 30 bản gần nhất. Mỗi bản gồm
  một file `.json` (nạp ngược lại vào phần mềm) và một file `.xlsx` nhiều trang (mở xem bằng
  Excel/WPS mà không cần phần mềm này). Đặt thư mục sao lưu ở USB hoặc OneDrive là mất máy
  vẫn còn dữ liệu. Khôi phục ngay trong phần mềm, không phải mò vào `%APPDATA%`.
- **Xuất toàn bộ ra Excel**: một file `.xlsx` có 8 trang — khách hàng, hoá đơn, chi tiết hàng,
  thanh toán, công nợ, vật tư, bảng giá riêng, bộ hàng — kèm dòng tổng và bộ lọc sẵn.
- **Nhật ký thay đổi**: mọi lần thêm/sửa/xoá đều ghi lại kèm giờ, ghi ra file riêng nên
  `Ctrl+Z` không xoá mất. Khách thắc mắc *"sao hôm trước giá khác"* là có chỗ tra.

## Phím tắt

| Phím | Tác dụng |
|---|---|
| `Ctrl+Z` / `Ctrl+Y` | Hoàn tác / Làm lại |
| `Enter` | Thêm dòng hàng (khi đang ở thanh nhập nhanh) |
| `F2` hoặc bấm đúp | Sửa ô đang chọn trên lưới |
| `F3` | Nhảy về ô Tên hàng / ô tìm kiếm |
| `Delete` | Xoá dòng hàng đang chọn |
| `Ctrl+D` | Nhân đôi dòng hàng đang chọn |
| `Ctrl+N` | Thêm khách hàng (ở màn hình chính) |
| `F6` | Mở sổ công nợ (ở màn hình chính) |
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

Toàn bộ dữ liệu nằm trong thư mục `%APPDATA%\QuanLyDienNuoc\`:

| File / thư mục | Là gì |
|---|---|
| `dulieu.json` | Toàn bộ khách hàng, hoá đơn, vật tư, bộ hàng |
| `dulieu.json.bak` | Bản của lần ghi ngay trước đó |
| `caidat.json` | Cài đặt: số ngày nhắc nợ, thư mục sao lưu, ngưỡng cảnh báo giá |
| `nhatky.jsonl` | Nhật ký thay đổi, mỗi dòng một mục |
| `SaoLuu\` | Các bản sao lưu theo ngày (`.json` + `.xlsx`), mặc định giữ 30 bản |

Cài đặt và nhật ký để riêng khỏi `dulieu.json` để `Ctrl+Z` không cuốn theo.

Nên vào **Tiện ích → Sao lưu và khôi phục** đổi thư mục sao lưu sang USB hoặc thư mục đồng bộ
lên mạng (OneDrive, Google Drive) — hỏng ổ cứng thì vẫn còn dữ liệu.

## Yêu cầu

- Windows 10 trở lên (WinForms chỉ chạy trên Windows)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (không bắt buộc)

## Cấu trúc mã nguồn

```
QuanLyDienNuoc.sln
src/
  QuanLyDienNuoc.Core/          thư viện nghiệp vụ, không phụ thuộc giao diện (net8.0)
    Models/                     KhachHang, VatTu, HoaDon, ChiTietHoaDon, ThanhToan, BoHang
    Data/KhoDuLieu.cs           đọc/ghi JSON + lịch sử hoàn tác
    Data/CaiDat.cs              cài đặt, lưu riêng khỏi dữ liệu
    Data/NhatKy.cs              nhật ký thay đổi, ghi nối tiếp ra file
    Data/SaoLuu.cs              tạo / liệt kê / khôi phục bản sao lưu
    BaoCao/CongNo.cs            tính công nợ và số ngày nợ từng khách
    BaoCao/TinNhacNo.cs         soạn tin nhắc nợ
    BaoCao/KiemTra.cs           cảnh báo giá lệch, dòng trùng, khách trùng tên
    Excel/MauHoaDon.cs          toạ độ các ô trên mẫu hoá đơn
    Excel/XuatHoaDon.cs         điền hoá đơn vào mẫu Excel, chia trang
    Excel/XuatToanBo.cs         xuất toàn bộ dữ liệu ra .xlsx nhiều trang
    Excel/DocHoaDon.cs          đọc ngược file Excel thành dòng hàng
    Excel/ThongTinCuaHang.cs    đọc phần đầu hoá đơn từ file mẫu
    Ui/So.cs, Ui/ChuViet.cs     đọc số kiểu "1.500.000" và phép tính, tìm kiếm không dấu
    Ui/TimHang.cs               khớp tên hàng theo kiểu gõ tắt
    Ui/DongNhapNhanh.cs         tách "ống 27 x10, co 90 x5" thành từng món
    Ui/DocSo.cs                 đọc số tiền thành chữ
  QuanLyDienNuoc/               ứng dụng WinForms (net8.0-windows)
    Program.cs                  điểm khởi động, đặt ngôn ngữ vi-VN, tự sao lưu
    MauHoaDon/                  hai file mẫu hoá đơn giấy
    Ui/Theme.cs                 màu, phông chữ, lưới, nút dùng chung
    Ui/InHoaDon.cs              vẽ hoá đơn ra giấy để xem trước và in
    Forms/MainForm.cs           màn hình chính (khách hàng theo năm, dải nhắc nợ)
    Forms/DonHangForm.cs        hoá đơn và chi tiết hàng của một khách
    Forms/CongNoForm.cs         sổ công nợ của cả cửa hàng
    Forms/KhachHangForm.cs      thêm/sửa khách
    Forms/HoaDonForm.cs         thêm/sửa thông tin hoá đơn
    Forms/ThanhToanForm.cs      các lần trả tiền
    Forms/BangGiaForm.cs        bảng giá riêng theo khách
    Forms/VatTuForm.cs          danh mục vật tư (kèm mã tắt)
    Forms/BoHangForm.cs         bộ hàng thường dùng
    Forms/NhapNhieuDongForm.cs  gõ một dòng ra nhiều món, có xem trước
    Forms/ChepNgayForm.cs       chép lại hàng của một ngày sang ngày khác
    Forms/SaoLuuForm.cs         sao lưu, khôi phục, xuất toàn bộ ra Excel
    Forms/NhatKyForm.cs         xem nhật ký thay đổi
    Forms/VanBanForm.cs         hiện đoạn văn bản soạn sẵn để chép đi (tin nhắc nợ)
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
