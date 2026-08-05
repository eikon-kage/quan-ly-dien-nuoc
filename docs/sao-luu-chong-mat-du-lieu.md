# Sao lưu chống mất dữ liệu khi máy hỏng

Tài liệu thiết kế — đọc và chốt trước khi sửa code.

Câu hỏi cần trả lời: **ổ cứng của máy tính cửa hàng chết sáng mai, mất bao nhiêu dữ liệu?**
Với cơ chế hiện tại, câu trả lời là **mất sạch**. Tài liệu này giải thích vì sao và đề xuất
cách sửa.

## 1. Hiện trạng

Ba lớp bảo vệ đang có:

| Lớp | Chỗ trong code | Chống được |
|---|---|---|
| Ghi ra `.tmp` rồi `File.Move` | `KhoDuLieu.GhiRaFile()` | Cúp điện giữa lúc ghi |
| Chép `dulieu.json` → `dulieu.json.bak` mỗi lần ghi | `KhoDuLieu.GhiRaFile()` | File hỏng, lùi được 1 bước |
| Thư mục `SaoLuu`, mỗi ngày một bản, giữ 30 bản | `SaoLuu.TuDongNeuCan()` | Xoá nhầm, sai dữ liệu, lùi 30 ngày |

Cả ba đều tốt và nên giữ. Nhưng cả ba đều nằm **trên cùng một ổ đĩa với file gốc**:

```
%APPDATA%\QuanLyDienNuoc\
├── dulieu.json          ← bản gốc
├── dulieu.json.bak      ← bản lùi 1 bước
├── caidat.json
├── nhatky.log
└── SaoLuu\              ← 30 bản sao lưu
    ├── sao-luu-2026-08-05-0815.json
    ├── sao-luu-2026-08-05-0815.xlsx
    └── ...
```

Ổ cứng hỏng, máy mất cắp, cháy, hay ransomware mã hoá ổ C — **tất cả đi cùng nhau**.

`CaiDat.ThuMucSaoLuu` cho phép đổi sang USB hay OneDrive, nhưng mặc định là để trống, và
để trống nghĩa là `SaoLuu` cạnh file dữ liệu. Một tính năng chỉ bảo vệ được người nhớ bật nó
thì gần như không bảo vệ được ai.

## 2. Năm điểm yếu cụ thể

**2.1 — Bản sao lưu nằm cùng ổ với bản gốc.** Lỗ hổng chính, mô tả ở trên.

**2.2 — Chỉ có một đích sao lưu.** `ThuMucSaoLuu` là một chuỗi. Nếu trỏ vào USB `E:\` thì
hôm nào quên cắm, `Directory.CreateDirectory` ném lỗi, `SaoLuu.Tao()` ném lên
`Program.TuDongSaoLuu()`, hiện hộp thoại cảnh báo — và hôm đó **không có bản sao lưu nào**,
kể cả bản trên máy. Đích ngoài hỏng làm mất luôn đích trong.

**2.3 — Một bản mỗi ngày, tạo lúc mở phần mềm.** `TuDongNeuCan` bỏ qua nếu
`LanSaoLuuCuoi.Date >= hôm nay`. Bản sao lưu của hôm nay được tạo lúc 8 giờ sáng, khi dữ liệu
còn là của **hôm qua**. Máy chết lúc 5 giờ chiều là mất trọn một ngày bán hàng.

**2.4 — Giữ 30 bản gần nhất nghĩa là chỉ lùi được 30 ngày.** `DonBanCu` xoá thẳng mọi bản
ngoài 30 bản mới nhất. Nhập sai một hoá đơn hồi tháng 4, tháng 8 mới phát hiện thì không còn
gì để tra ngược.

**2.5 — Không ai kiểm bản sao lưu có đọc được không.** `SaoLuu.Tao` chép file rồi ghi
`LanSaoLuuCuoi` là xong. Nếu file bị chép dở (rút USB giữa chừng, OneDrive đồng bộ lỗi) thì
không ai biết cho tới ngày cần khôi phục. Sao lưu chưa từng thử khôi phục thì chưa tính là
sao lưu.

## 3. Thiết kế đề xuất

Nguyên tắc: **ít nhất một bản dữ liệu phải nằm ngoài cái máy đó**, và phải tự động — không
phụ thuộc vào việc người dùng nhớ làm gì.

### 3.1 Nhiều đích sao lưu song song

Đổi cấu hình trong `CaiDat`:

```csharp
// Trước
public string ThuMucSaoLuu { get; set; } = string.Empty;

// Sau — vẫn đọc được file caidat.json cũ (xem mục 3.7)
public List<DichSaoLuu> DichSaoLuu { get; set; } = new();

public sealed class DichSaoLuu
{
    public string ThuMuc { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;        // "OneDrive", "USB cửa hàng"
    public bool BatBuoc { get; set; }                       // false = lỗi thì bỏ qua lặng lẽ
    public DateTime? LanGhiCuoi { get; set; }
    public string LoiLanCuoi { get; set; } = string.Empty;
}
```

`SaoLuu.Tao` chép ra **tất cả** đích ghi được. Đích nào lỗi thì bắt exception, ghi vào
`LoiLanCuoi`, **đi tiếp đích khác** — không ném lên trên, không hiện hộp thoại. Trả về kết quả
gồm danh sách đích thành công và đích thất bại để màn hình hiển thị.

Quan trọng: **đích cục bộ luôn có và luôn chạy trước**. Mạng chết hay USB rút ra không bao giờ
được phép làm mất bản sao lưu trên máy.

### 3.2 Đích ngoài mặc định là OneDrive

Lần đầu chạy, nếu người dùng chưa cấu hình gì, phần mềm tự dò:

```csharp
// Windows đặt sẵn biến môi trường khi cài OneDrive
Environment.GetEnvironmentVariable("OneDrive")            // OneDrive cá nhân
Environment.GetEnvironmentVariable("OneDriveCommercial")  // tài khoản công ty
// Google Drive: %USERPROFILE%\Google Drive hoặc ổ G:\My Drive
```

Tìm thấy thì tạo `<OneDrive>\QuanLyDienNuoc-SaoLuu` và thêm làm đích mặc định, kèm một hộp
thoại một lần: *"Phần mềm sẽ tự cất một bản dữ liệu vào OneDrive để mất máy vẫn còn dữ liệu.
Đồng ý?"*. Không tìm thấy thì hiện lời nhắc dẫn sang màn hình sao lưu.

Vì sao chọn OneDrive/Google Drive làm đích ngoài chính:

- Có sẵn trên hầu hết máy Windows, miễn phí ở dung lượng cần dùng (file JSON vài trăm KB)
- Tự đồng bộ lên mây, không cần nhớ cắm gì
- Bản thân dịch vụ giữ lịch sử phiên bản 30 ngày — thêm một lớp nữa nếu file bị đè hỏng
- Cháy nhà, mất cắp, ransomware ổ C đều không với tới

Điểm cần biết: OneDrive **đồng bộ**, không phải sao lưu. Xoá nhầm ở máy là xoá luôn trên mây.
Chính vì thế thư mục sao lưu phải giữ **nhiều bản theo ngày** chứ không phải một file duy nhất
— và đó đúng là cách `SaoLuu` đang làm.

### 3.3 Sao lưu dày hơn

Bỏ điều kiện "mỗi ngày một lần", thay bằng ba mốc:

| Mốc | Vì sao |
|---|---|
| Lúc mở phần mềm (như hiện nay) | Chốt lại trạng thái đầu ngày |
| Mỗi 2 giờ, chỉ khi có thay đổi kể từ bản trước | Mất tối đa 2 giờ làm việc |
| **Lúc đóng phần mềm** | Quan trọng nhất — chốt trọn ngày bán hàng |

Bản đóng app là bản đáng giá nhất mà hiện nay hoàn toàn không có. Chỗ móc sạch nhất là ngay
**sau `Application.Run(new MainForm())`** trong `Program.Main` — lúc đó cửa sổ đã đóng nhưng
`KhoDuLieu.Instance` vẫn còn nguyên trong bộ nhớ, và không phải override `OnFormClosing` của
`MainForm` (hiện chưa có). Nhớ bọc `try/catch` như `TuDongSaoLuu` đang làm: lỗi lúc thoát thì
càng không được phép hiện lỗi chắn đường người dùng.

Chi phí gần như bằng không: JSON của một cửa hàng cỡ này chỉ vài trăm KB. Riêng file `.xlsx`
thì nặng và chậm hơn — đề xuất chỉ xuất Excel cho **bản đầu tiên trong ngày**, các bản giữa
ngày chỉ ghi JSON.

### 3.4 Giữ bản theo ngày và theo tháng

Thay `DonBanCu` bằng quy tắc ông – bố – con:

- Giữ **30 bản gần nhất** (như hiện nay)
- **Cộng thêm**: bản đầu tiên của mỗi tháng, giữ **12 tháng**, không bao giờ bị dọn

Tốn thêm vài MB, đổi lại lùi được cả năm. Đặt tên có tiền tố riêng (`sao-luu-thang-2026-04.json`)
để `DonBanCu` nhận ra mà chừa lại.

### 3.5 Tự kiểm sau khi chép

Chép xong mỗi đích thì đọc lại **chính file vừa ghi** ở đích đó:

```csharp
var json = File.ReadAllText(fileDich);
var kiemTra = JsonSerializer.Deserialize<DuLieuApp>(json, TuyChonJson);
bool dat = kiemTra is not null
    && kiemTra.KhachHangs.Count == kho.DuLieu.KhachHangs.Count
    && kiemTra.HoaDons.Count == kho.DuLieu.HoaDons.Count;
```

Không đạt thì coi đích đó là thất bại, ghi lý do, và **không cập nhật `LanGhiCuoi`**. File hỏng
bị phát hiện ngay hôm đó, chứ không phải vào ngày anh cần nó nhất.

Màn hình sao lưu hiện luôn con số đã kiểm: *"Bản 05/08/2026 17:40 — 42 khách, 128 hoá đơn ✔ đọc lại được"*.

### 3.6 Dải nhắc khi sao lưu ngoài quá hạn

Sao lưu hỏng luôn hỏng âm thầm — người dùng không bao giờ tự nhận ra. Màn hình chính đã có sẵn
dải nhắc nợ, thêm một dải đỏ cùng chỗ:

> ⚠ **7 ngày nay chưa cất được bản nào ra ngoài máy này.** OneDrive báo: không đăng nhập được.
> [Mở màn hình sao lưu]

Điều kiện hiện: mọi đích không phải đích cục bộ đều có `LanGhiCuoi` cũ hơn N ngày (mặc định 3).
Đây là thứ thực sự cứu được dữ liệu, vì nó biến một lỗi thầm lặng thành một việc phải làm.

### 3.7 Đọc được cấu hình cũ

`CaiDat.Doc` bắt `JsonException` và trả về bản mặc định, nên đổi kiểu `ThuMucSaoLuu` từ chuỗi
sang danh sách sẽ **âm thầm xoá mất mọi cài đặt cũ** của người dùng. Cách làm đúng: giữ lại
thuộc tính `ThuMucSaoLuu` cũ, đánh dấu `[Obsolete]`, và trong `Doc()` nếu thấy nó khác rỗng
mà `DichSaoLuu` rỗng thì chuyển nó thành một mục trong danh sách rồi xoá đi.

## 4. Việc thủ công: mỗi quý thử khôi phục một lần

Không có phần mềm nào thay được việc này. Mỗi quý một lần:

1. Vào thư mục sao lưu trên OneDrive **bằng trình duyệt** (không phải thư mục đồng bộ trên máy)
2. Tải một file `sao-luu-*.json` về một máy khác
3. Cài phần mềm, dùng chức năng Khôi phục, mở lên xem đủ khách và đủ hoá đơn không

Ba lần đầu thấy đủ thì mới yên tâm là cơ chế chạy thật. Nên ghi vào README như một mục việc
của chủ cửa hàng.

## 5. Thứ tự làm

| Đợt | Nội dung | Đổi lại được gì |
|---|---|---|
| **1** | Nhiều đích (3.1) + tự dò OneDrive (3.2) + đọc cấu hình cũ (3.7) | Lấp lỗ hổng chính — máy hỏng vẫn còn dữ liệu |
| **2** | Sao lưu lúc đóng app và mỗi 2 giờ (3.3) | Mất tối đa 2 giờ thay vì cả ngày |
| **3** | Tự kiểm (3.5) + dải nhắc quá hạn (3.6) | Biết được lúc sao lưu hỏng |
| **4** | Giữ bản theo tháng (3.4) + mục README cho mục 4 | Lùi được cả năm |

Đợt 1 là phần đáng làm nhất. Ba đợt sau làm tăng dần chất lượng nhưng không đợt nào cấp thiết
bằng.

## 6. Ảnh hưởng tới kiểm thử

`SaoLuuTests.cs` hiện dựng `CaiDat` với một `ThuMucSaoLuu` trỏ vào thư mục tạm. Khi chuyển sang
danh sách đích, cần bổ sung các trường hợp:

- Một đích ghi được, một đích trỏ vào ổ không tồn tại → bản cục bộ vẫn phải có, không ném lỗi
- File ở đích bị sửa hỏng sau khi chép → tự kiểm phải báo thất bại
- `caidat.json` kiểu cũ (chuỗi `ThuMucSaoLuu`) → phải chuyển thành một mục trong danh sách
- `DonBanCu` với 40 bản trải 5 tháng → còn 30 bản ngày cộng các bản mốc tháng
