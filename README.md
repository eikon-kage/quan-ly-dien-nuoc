# Quản lý đơn hàng – Cửa hàng điện nước

Phần mềm desktop (Windows Forms, .NET 8) quản lý hoá đơn mua hàng của khách tại cửa hàng
điện nước. Mỗi khách có một (hoặc nhiều) hoá đơn kéo dài nhiều ngày; mỗi lần khách lấy hàng
thì thêm một dòng vào hoá đơn đó. Mỗi khách có bảng giá riêng cho từng loại vật tư.

## Tính năng

- **Màn hình chính**: danh sách khách hàng lọc theo năm (mặc định là năm hiện tại), kèm
  tổng mua / đã trả / còn nợ của khách trong năm đó. Tìm khách không dấu (gõ `nguyen` ra
  `Nguyễn`). Bấm đúp hoặc Enter để mở đơn hàng của khách.
- **Đơn hàng của khách**: chọn hoá đơn trong năm ở ô trên cùng, cả màn hình còn lại là chi tiết
  hàng đã lấy theo từng ngày (ngày, tên hàng, đơn vị, đơn giá, số lượng, thành tiền, ghi chú).
- **Sổ công nợ**: một màn hình cho cả cửa hàng — ai đang nợ, nợ bao nhiêu và **nợ đã bao
  nhiêu ngày** (tính từ lần lấy hàng hoặc trả tiền gần nhất), xếp sẵn theo nợ lâu nhất.
  Mở phần mềm lên là có ngay dải nhắc *"3 khách nợ quá 60 ngày"* trên đầu màn hình.
- **Tin nhắc nợ soạn sẵn**: bấm một nút ra đoạn tin kèm bảng kê từng hoá đơn còn nợ và số
  tiền bằng chữ, sửa vài chữ rồi chép sang Zalo là gửi được.
- **Gõ thẳng vào bảng như Excel**: cuối bảng chi tiết luôn có sẵn một dòng trống tô vàng — gõ
  tên hàng (tự điền đơn vị và **giá của đúng khách đó**), gõ số lượng rồi `Enter` là dòng đó
  vào sổ và có ngay dòng trống mới để gõ tiếp. Ngày lấy theo dòng ngay trên, sửa lại được.
  Số lượng âm là khách trả lại. Bỏ dòng đang gõ dở thì bấm `Delete`.
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
- **Cảnh báo nhập sai**: giá lệch quá 20% so với lần gần nhất bán cho chính khách đó, dòng
  trùng y hệt (cùng ngày, cùng hàng, cùng số lượng), hoặc thêm khách trùng tên — đều hỏi lại
  trước khi ghi.
- **Sửa trực tiếp trên lưới như Excel**: bấm đúp (hoặc F2) vào ô để sửa, mọi thay đổi tự lưu.
- **Chèn dòng vào giữa, không phải chỉ thêm vào cuối**: quên một món ở giữa thì chọn dòng
  muốn chèn cạnh, gõ hàng vào thanh nhập nhanh rồi `Ctrl+Enter` (chèn lên trên) hoặc
  `Ctrl+Shift+Enter` (chèn xuống dưới) — dòng mới lấy luôn ngày của dòng đang chọn nên nằm
  yên đúng chỗ. Xếp sai thì `Alt+↑` / `Alt+↓` đổi chỗ hai dòng (trong cùng một ngày). Bấm
  chuột phải lên lưới ra đủ các lệnh này. Thứ tự trên lưới cũng chính là thứ tự in ra giấy
  và xuất Excel — trong cùng một ngày phần mềm không tự xếp lại theo vần nữa.
- **Hoàn tác / Làm lại**: `Ctrl+Z` / `Ctrl+Y` cho mọi thao tác (thêm, sửa, xoá dòng, xoá hoá
  đơn, xoá khách…). Lịch sử chỉ giữ trong phiên đang mở, tối đa 50 bước — đóng phần mềm là
  hết, dữ liệu đã lưu vẫn còn nguyên.
- **Bảng giá riêng của khách**: khi nhập giá khác với giá đang lưu, phần mềm hỏi có dùng giá
  mới cho những lần sau không. Xem và sửa toàn bộ bảng giá của khách ở nút *Bảng giá của khách*.
- **Khách trả lại hàng**: bấm nút *Trả lại* (hoặc gõ số lượng âm, ví dụ `-2`) là ghi được
  dòng trả hàng — thành tiền âm, trừ thẳng vào hoá đơn, in ra có dấu trừ. Trả lại nhiều hơn
  số đang giữ thì phần mềm hỏi lại. Trong ô nhập nhiều dòng viết `ống 27 x-2`.
- **Thanh toán**: ghi nhiều lần trả tiền cho một hoá đơn, tự tính còn nợ.
- **Thu tiền của khách (một lần trả cho nhiều hoá đơn)**: khách đưa 5 triệu trả cho 3 hoá đơn
  thì gõ một số tiền, phần mềm chia sẵn từ hoá đơn cũ nhất và cho xem trước hoá đơn nào trừ
  bao nhiêu, còn lại bao nhiêu. Ghi một lần thành một phiếu thu, muốn bỏ thì xoá cả lần thu
  chứ không phải đi từng hoá đơn. Đưa thừa thì hỏi có ghi phần thừa thành trả trước không.
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
- **Hai máy dùng chung một file**: để `dulieu.json` trên thư mục mạng rồi mở ở hai máy thì
  máy thứ hai được báo *"file đang được máy X mở"* và chỉ mở ở chế độ **CHỈ XEM** (xem, in,
  xuất Excel bình thường; sửa gì cũng bị chặn). Ngoài ra trước mỗi lần ghi, phần mềm so lại
  file trên đĩa: nếu máy khác vừa sửa thì hỏi ghi đè hay bỏ thay đổi và nạp lại — bản của máy
  kia được cất thành `dulieu.json.maykhac-…json` chứ không bị đè mất. Đang mở mà file bị máy
  khác sửa thì thanh dưới báo đỏ, bấm `F5` để nạp lại bản mới nhất.

## Phím tắt

| Phím | Tác dụng |
|---|---|
| `Ctrl+Z` / `Ctrl+Y` | Hoàn tác / Làm lại |
| `Enter` | Thêm dòng hàng (ở thanh nhập nhanh, hoặc ở dòng trống cuối bảng) |
| `F2` hoặc bấm đúp | Sửa ô đang chọn trên lưới |
| `F3` | Nhảy về ô Tên hàng / ô tìm kiếm |
| `Delete` | Xoá dòng hàng đang chọn |
| `Ctrl+Enter` | Chèn dòng hàng lên trên dòng đang chọn |
| `Ctrl+Shift+Enter` | Chèn dòng hàng xuống dưới dòng đang chọn |
| `Alt+↑` / `Alt+↓` | Đổi chỗ dòng đang chọn với dòng liền kề |
| `Ctrl+N` | Thêm khách hàng (ở màn hình chính) |
| `F5` | Nạp lại dữ liệu từ file (khi máy khác vừa sửa) |
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
| `dulieu.json.khoa`, `dulieu.json.dangmo` | Đánh dấu đang có máy mở file; tự xoá khi đóng phần mềm |
| `dulieu.json.maykhac-….json` | Bản của máy khác, cất lại trước khi mình ghi đè |
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
    Data/KhoDuLieu.cs           đọc/ghi JSON + lịch sử hoàn tác + chống hai máy ghi đè nhau
    Data/KhoaFile.cs            khoá file dữ liệu khi đang mở, báo máy nào đang giữ
    Data/CaiDat.cs              cài đặt, lưu riêng khỏi dữ liệu
    Data/NhatKy.cs              nhật ký thay đổi, ghi nối tiếp ra file
    Data/SaoLuu.cs              tạo / liệt kê / khôi phục bản sao lưu
    BaoCao/CongNo.cs            tính công nợ và số ngày nợ từng khách
    BaoCao/TinNhacNo.cs         soạn tin nhắc nợ
    BaoCao/ThuTien.cs           chia một lần thu tiền cho nhiều hoá đơn, cũ nhất trả trước
    BaoCao/KiemTra.cs           cảnh báo giá lệch, dòng trùng, khách trùng tên, trả lại quá số đã mua
    Excel/MauHoaDon.cs          toạ độ các ô trên mẫu hoá đơn
    Excel/XuatHoaDon.cs         điền hoá đơn vào mẫu Excel, chia trang
    Excel/XuatToanBo.cs         xuất toàn bộ dữ liệu ra .xlsx nhiều trang
    Excel/DocHoaDon.cs          đọc ngược file Excel thành dòng hàng
    Excel/ThongTinCuaHang.cs    đọc phần đầu hoá đơn từ file mẫu
    Ui/So.cs, Ui/ChuViet.cs     đọc số kiểu "1.500.000" và phép tính, tìm kiếm không dấu
    Ui/TimHang.cs               khớp tên hàng theo kiểu gõ tắt
    Ui/DongNhapNhanh.cs         tách "ống 27 x10, co 90 x5" thành từng món
    Ui/ThuTuDong.cs             thứ tự dòng hàng: chèn vào giữa, đổi chỗ, giữ đúng thứ tự khi in
    Ui/DocSo.cs                 đọc số tiền thành chữ
  QuanLyDienNuoc/               ứng dụng WinForms (net8.0-windows)
    Program.cs                  điểm khởi động, đặt ngôn ngữ vi-VN, khoá file, tự sao lưu
    MauHoaDon/                  hai file mẫu hoá đơn giấy
    Ui/Theme.cs                 màu, phông chữ, thẻ, nút, ô nhập, lưới dùng chung
    Ui/ThanhBen.cs              thanh bên trái của màn hình chính, hình vẽ bằng nét
    Ui/OThongKe.cs              một ô số liệu trong thẻ tổng quan
    Ui/InHoaDon.cs              vẽ hoá đơn ra giấy để xem trước và in
    Forms/MainForm.cs           màn hình chính (khách hàng theo năm, dải nhắc nợ)
    Forms/DonHangForm.cs        hoá đơn và chi tiết hàng của một khách
    Forms/CongNoForm.cs         sổ công nợ của cả cửa hàng
    Forms/KhachHangForm.cs      thêm/sửa khách
    Forms/HoaDonForm.cs         thêm/sửa thông tin hoá đơn
    Forms/ThanhToanForm.cs      các lần trả tiền của một hoá đơn
    Forms/ThuTienForm.cs        thu một cục tiền, chia cho nhiều hoá đơn
    Forms/BangGiaForm.cs        bảng giá riêng theo khách
    Forms/VatTuForm.cs          danh mục vật tư (kèm mã tắt)
    Forms/BoHangForm.cs         bộ hàng thường dùng
    Forms/NhapNhieuDongForm.cs  gõ một dòng ra nhiều món, có xem trước
    Forms/SaoLuuForm.cs         sao lưu, khôi phục, xuất toàn bộ ra Excel
    Forms/NhatKyForm.cs         xem nhật ký thay đổi
    Forms/VanBanForm.cs         hiện đoạn văn bản soạn sẵn để chép đi (tin nhắc nợ)
    Forms/XemTruocForm.cs       xem trước bản in
    Forms/NhapExcelForm.cs      nhập hoá đơn từ file Excel
tests/
  QuanLyDienNuoc.Tests/         kiểm thử phần nghiệp vụ (xUnit): `dotnet test`
.github/workflows/
  anh-giao-dien.yml             dựng trên máy Windows của GitHub, chụp ảnh từng màn hình
docs/
  anh-giao-dien/                ảnh giao diện mới nhất, do workflow tự commit lên
  giao-dien-may-tinh.md         bảng màu, các mảnh dùng chung và quy tắc dựng giao diện
```

## Xem giao diện mà không có máy Windows

Đẩy mã nguồn lên GitHub là workflow `anh-giao-dien.yml` tự chạy: dựng phần mềm trên máy
Windows, chạy toàn bộ kiểm thử, mở lần lượt từng màn hình và chụp lại thành ảnh PNG, kèm
cả ảnh bản in khổ A4.

Ảnh được chính workflow commit thẳng vào thư mục [`docs/anh-giao-dien/`](docs/anh-giao-dien/),
nên ở nhà chỉ cần `git pull` là có ảnh mới nhất, không phải vào tab Actions tải về. Xem trên
web cũng được: mở thư mục đó trong GitHub. Kèm theo có `nhat-ky.txt` ghi màn hình nào chụp
được, màn hình nào lỗi.

Bản `.exe` đóng gói sẵn thì vẫn nằm ở mục artifact: vào tab **Actions** → **Run workflow**
để tự bấm chạy, xong tải ở cuối trang lần chạy đó.

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
