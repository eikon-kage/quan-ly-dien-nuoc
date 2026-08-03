# Chạy thử app trên máy Mac (Apple Silicon) bằng máy ảo UTM

WinForms chỉ chạy trên Windows. Tài liệu này hướng dẫn dựng một máy ảo Windows 11 ARM
miễn phí bằng UTM để chạy thử phần mềm.

## 0. Kiểm tra dung lượng trống — làm trước tiên

Cần **tối thiểu 35 GB trống**, nên có 45 GB:

- file cài đặt Windows (ISO): ~6 GB
- Windows sau khi cài: ~20-25 GB
- app và chỗ thở: ~5 GB

Kiểm tra bằng lệnh:

```bash
df -h /System/Volumes/Data
```

Thiếu chỗ thì dọn bớt (Cài đặt hệ thống → Cài đặt chung → Bộ nhớ), hoặc để ổ cứng ngoài
rồi lưu máy ảo vào đó.

## 1. Phần mềm cần cài trên Mac

Đã cài sẵn:

- **UTM** (`/Applications/UTM.app`) — phần mềm máy ảo
- **CrystalFetch** (`/Applications/CrystalFetch.app`) — tải file cài Windows chính chủ

## 2. Tải file cài Windows 11 ARM

1. Mở **CrystalFetch**
2. Chọn: Windows **11**, kiến trúc **ARM64**, ngôn ngữ tuỳ ý (English hoặc Tiếng Việt)
3. Bấm **Download** → được một file `.iso` khoảng 6 GB (mất 15-40 phút tuỳ mạng)

## 3. Tạo máy ảo trong UTM

1. Mở **UTM** → **Create a New Virtual Machine**
2. Chọn **Virtualize** (không chọn Emulate — chậm hơn nhiều)
3. Chọn **Windows**
4. Tick **Import VHDX Image** thì bỏ qua; bấm **Browse** rồi chọn file `.iso` vừa tải
5. Giữ nguyên hai ô đã tick sẵn:
   - *Install drivers and SPICE tools* — để có chia sẻ thư mục và copy/paste giữa Mac và Windows
6. Cấu hình:
   - **Memory**: 6144 MB (máy 16 GB RAM trở lên thì để 8192)
   - **CPU Cores**: 4
   - **Storage**: 64 GB
   - **Shared Directory**: chọn thư mục `/Users/quangvinh/winforms-app`
7. Bấm **Save** rồi bấm nút ▶ để khởi động

## 4. Cài Windows

Bấm phím bất kỳ khi thấy dòng "Press any key to boot from CD" rồi làm theo hướng dẫn.

**Nếu Windows bắt đăng nhập tài khoản Microsoft mà không cho bỏ qua**: ở màn hình đó bấm
`Shift + F10` để mở cửa sổ lệnh, gõ một trong hai lệnh:

```
start ms-cxh:localonly
```

(bản Windows 11 24H2 trở lên), hoặc bản cũ hơn:

```
oobe\bypassnro
```

Máy sẽ khởi động lại và cho chọn *"I don't have internet"* → tạo tài khoản cục bộ.

## 5. Cài công cụ khách của UTM (để dùng thư mục chia sẻ)

Sau khi vào được màn hình Desktop của Windows:

1. Mở **File Explorer** → vào ổ **D:** (đĩa UTM guest tools)
2. Chạy file cài đặt trong đó → cài xong khởi động lại
3. Thư mục chia sẻ sẽ xuất hiện dưới dạng một ổ đĩa mạng (thường là ổ `Z:`)

## 6. Chạy app — cách nhanh nhất, không cần cài gì thêm

File `.exe` đã đóng gói sẵn, chứa luôn .NET bên trong, chỉ cần copy vào máy ảo là chạy.

Trên Mac, file nằm ở:

```
src/QuanLyDienNuoc/bin/Release/net8.0-windows/win-arm64/publish/QuanLyDienNuoc.exe
```

Trong máy ảo Windows: mở ổ chia sẻ `Z:` → vào đúng đường dẫn trên → **copy file
`QuanLyDienNuoc.exe` ra Desktop của Windows** (chạy thẳng từ ổ mạng đôi khi bị chậm) →
bấm đúp để chạy.

Windows SmartScreen báo "Windows protected your PC" → bấm **More info** → **Run anyway**
(do file chưa mua chữ ký số).

Dữ liệu của app trong máy ảo nằm ở `%APPDATA%\QuanLyDienNuoc\dulieu.json`.

## 7. (Tuỳ chọn) Cài .NET SDK trong máy ảo để sửa code và build

Chỉ cần khi muốn sửa code rồi chạy lại ngay trong máy ảo:

1. Tải .NET 8 SDK bản **Arm64**: https://dotnet.microsoft.com/download/dotnet/8.0
2. Mở PowerShell, vào ổ chia sẻ:

```powershell
Z:
cd \winforms-app
dotnet run --project src/QuanLyDienNuoc
```

## Đóng gói lại file exe sau khi sửa code trên Mac

```bash
dotnet publish src/QuanLyDienNuoc -c Release -r win-arm64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableWindowsTargeting=true
```

Máy ảo dùng chip ARM nên dùng `win-arm64`. Nếu đưa sang máy tính Windows thường (Intel/AMD)
thì đổi thành `win-x64`.
