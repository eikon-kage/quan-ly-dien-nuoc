# Giao diện phần mềm máy tính (WinForms)

Giao diện lấy theo bộ thiết kế **Inventory Management Dashboard** trên Figma
([file gốc](https://www.figma.com/design/0jRkAltW0tNMQQhfPdkKQS/Inventory-Management-Dashboard--Community-?node-id=0-1)),
dựng lại bằng WinForms chứ không dùng thư viện giao diện ngoài nào.

Bản thiết kế đó chỉ cho **ngôn ngữ hình khối và bảng màu**. Ba chỗ cố tình làm khác, lý do ghi
ở mục [Chỗ làm khác bản thiết kế](#chỗ-làm-khác-bản-thiết-kế).

## Bốn quy tắc

1. **Nội dung nằm trong thẻ trắng bo góc, trên nền xám nhạt.** Không còn khối màu trải hết bề
   ngang màn hình. Thẻ dựng bằng `Theme.The`, bo góc 12, viền 1px, bóng rất nhẹ ở dưới.
2. **Mỗi khu chỉ một nút tô màu đặc** — nút việc chính (`Theme.Nut`). Các nút còn lại nền
   trắng viền mảnh (`Theme.NutPhu`), muốn phân biệt thì đổi màu chữ: xanh cho việc thu tiền,
   đỏ cho việc xoá.
3. **Bảng kẻ dòng mảnh, đầu bảng nền trắng chữ xám.** Dải xanh đậm ở đầu bảng bỏ đi — màu xanh
   giờ chỉ dành cho nút việc chính và mục đang mở ở thanh bên.
4. **Không dùng phông icon.** Hình trong thanh bên vẽ bằng nét (`ThanhBen.VeIcon`). Máy khách
   thiếu bộ phông icon của Windows là hiện ra ô vuông rỗng, mà chuyện đó không kiểm được từ xa.

## Bảng màu

Lấy đúng biến màu của bộ thiết kế, khai báo ở [`Ui/Theme.cs`](../src/QuanLyDienNuoc/Ui/Theme.cs):

| Tên trong mã | Mã màu | Dùng vào |
| --- | --- | --- |
| `Nen` | `#F0F1F3` | nền cửa sổ (grey-50) |
| `Trang` | `#FFFFFF` | thẻ, thanh bên, thanh trên |
| `Chinh` | `#1366D9` | nút việc chính, mục đang mở (primary-600) |
| `ChinhNhat` | `#E8F1FD` | nền dòng đang chọn, nền mục đang mở |
| `Xanh` | `#10A760` | việc thu tiền, số tiền đã thu |
| `Cam` | `#E19133` | số tiền đã mua, dải nhắc nợ quá hạn |
| `Do` | `#DA3E33` | số còn nợ, việc xoá |
| `Xam` / `XamNhat` | `#667085` / `#858D9D` | chữ phụ, chữ chú thích |
| `Vien` | `#E0E2E7` | viền thẻ, kẻ dòng bảng |
| `Chu` / `ChuDam` | `#383E49` / `#1D1F2C` | chữ thường / chữ tiêu đề |

## Các mảnh dùng chung

| Mảnh | Ở đâu | Là gì |
| --- | --- | --- |
| `Theme.The` | Theme.cs | thẻ trắng bo góc, có bóng nhẹ |
| `Theme.Nut` / `Theme.NutPhu` | Theme.cs | nút bo góc tự vẽ, có trạng thái trỏ chuột / đang bấm / đang được bàn phím chọn; chữ không vừa nút thì tự hạ cỡ chứ không để bị cắt, hoặc `noTheoChu: true` cho nút nở theo chữ |
| `Theme.NutBaCham` | Theme.cs | nút ⋯ gom các việc ít dùng vào một menu |
| `ThanhPhanTrang` | ThanhPhanTrang.cs | thanh phân trang: hai nút lùi/tiến và câu "Trang 2/7" |
| `Theme.HopO` / `Theme.HopTim` | Theme.cs | ô nhập bo góc; ô tìm kiếm có kính lúp và chữ gợi ý mờ |
| `Theme.ApDungLuoi` | Theme.cs | bảng kiểu mới: đầu bảng trắng, kẻ dòng mảnh |
| `Theme.ThanhTieuDe` | Theme.cs | dải tiêu đề đầu mỗi cửa sổ con: nền trắng, kẻ một vạch dưới |
| `ThanhBen` | ThanhBen.cs | thanh bên trái của màn hình chính, kèm hình vẽ nét |

Mười lăm cửa sổ con không phải sửa gì: chúng dựng bằng đúng các hàm trên, đổi ở `Theme.cs` là
đổi hết một lượt.

## Màn hình chính

```
┌──────────────┬──────────────────────────────────────────────────────┐
│ Sổ điện nước │  [ô tìm khách]              Năm [2026] [+ Thêm khách]│
│              ├──────────────────────────────────────────────────────┤
│ Trang chủ    │  ┌ ⚠ 2 khách nợ quá 60 ngày …    [Mở sổ công nợ] ──┐│
│ Sổ công nợ   │  └─────────────────────────────────────────────────┘│
│ Danh mục vật │  ┌ Khách hàng        ☐ Chỉ hiện khách có đơn ───────┐│
│ Bộ hàng      │  │ bảng khách hàng                                  ││
│              │  │                                                  ││
│ Sao lưu      │  │                                                  ││
│ Nhật ký      │  │                                                  ││
│              │  │                                                  ││
│              │  │ [Mở đơn hàng] [Thu tiền] [⋯]           5 khách   ││
│              │  └─────────────────────────────────────────────────┘│
└──────────────┴──────────────────────────────────────────────────────┘
```

## Mỗi khu chỉ để ngoài một hai việc, còn lại vào nút ⋯

Trước đây dưới mỗi bảng là một hàng nút dài, mỗi nút một câu chữ — "Sửa khách", "Xoá khách",
"Soạn tin nhắc nợ", "Khôi phục bản đã chọn"… Nhìn vào phải đọc hết mới biết bấm cái nào, mà
thực tế chỉ hai nút đầu là dùng hằng ngày. Nay mỗi khu giữ ngoài **một nút việc chính (tô màu
đặc) và tối đa một nút phụ**, còn lại nằm sau nút `⋯`:

| Ở đâu | Để ngoài | Trong nút ⋯ |
| --- | --- | --- |
| Trang chủ, chân bảng khách | Mở đơn hàng · Thu tiền | sửa khách · xoá khách |
| Đơn hàng, dải tiêu đề | năm · hoá đơn · + Hoá đơn mới · In / xem trước | thu tiền · trả cho hoá đơn · chốt (mở lại) · sửa mã · xoá hoá đơn · bảng giá riêng · nhắc nợ · hoàn tác · làm lại · Excel vào/ra |
| Đơn hàng, hàng nhập hàng | + Thêm dòng · − Trả lại | *(không còn)* |
| Đơn hàng, thanh tổng tiền | Nhập nhiều dòng | chèn dòng · chọn tất cả dòng · chuyển lên/xuống · xoá dòng đã chọn |
| Sổ công nợ | Mở đơn hàng · Thu tiền | soạn tin nhắc nợ · xuất Excel |
| Sao lưu | Sao lưu ngay | xuất Excel · mở thư mục · khôi phục |
| Xem trước hoá đơn | In hoá đơn · trang trước/sau | phóng to · thu nhỏ · vừa màn hình |

Ba chấm **vẽ bằng ba hình tròn**, không dùng ký tự "⋯": phông Segoe UI thiếu nhiều ký tự ký
hiệu, thiếu là Windows in ra ô vuông rỗng. Trỏ chuột vào nút thì hiện chú thích ("Việc khác
với hoá đơn đang xem") để không phải bấm thử mới biết trong đó có gì. Chữ và trạng thái mờ /
không mờ của từng dòng menu tính lại đúng lúc mở menu.

## Màn đơn hàng: chỗ dành cho bảng chi tiết

Bảng chi tiết hàng đã lấy là thứ ngồi nhìn cả ngày. Mọi thứ phía trên nó dồn hết vào **một dải
tiêu đề duy nhất**:

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Ông Long (thợ xây)      [2026] [HD2026-02 · 02/06 · đã chốt] [+ Hoá đơn mới] │
│ ĐT: 0912 345 678 · Xóm 5, Hải Minh        [IN / XEM TRƯỚC] [⋯] [Đóng]      │
├──────────────────────────────────────────────────────────────────────────────┤
│ NGÀY LẤY      TÊN HÀNG        ĐƠN VỊ  ĐƠN GIÁ  SỐ LƯỢNG  [+ THÊM DÒNG] [−]   │
├──────────────────────────────────────────────────────────────────────────────┤
│ bảng chi tiết hàng đã lấy                                                    │
```

So với bản trước, ba thứ bị bỏ hẳn:

- **thanh công cụ 76px** (bảng giá, nhắc nợ, hoàn tác, làm lại) — vào nút `⋯`;
- **thanh chọn hoá đơn 62px** — hai ô chọn dời lên dải tiêu đề, nằm ngang hàng tên khách;
- **dòng "CHI TIẾT HOÁ ĐƠN HD… · mở ngày… · 2 dòng"** — mã, ngày và trạng thái chốt đã hiện
  ngay trong ô chọn hoá đơn nên dòng đó chỉ nhắc lại; chưa có hoá đơn nào thì nhắc ở thanh dưới.

Cộng cả hàng nút phụ trong khối nhập hàng, bảng chi tiết cao thêm khoảng **240px** so với bản
theo Figma đầu tiên.

Thanh dưới cùng không còn dòng nhắc phím tắt dài; nó chỉ nói việc vừa làm ("Đã xoá 3 dòng. Bấm
Ctrl+Z để lấy lại.") và nhắc ô còn thiếu.

## Bảng dài thì chia trang, 30 dòng một trang

Ba bảng dài nhất — **khách hàng** (trang chủ), **sổ công nợ**, và các bảng của màn **chấm công**
— chỉ đổ 30 dòng vào lưới một lúc. Phép chia trang nằm ở
[`PhanTrang.cs`](../src/QuanLyDienNuoc.Core/Ui/PhanTrang.cs) (hàm thuần, có test), thanh nút ở
[`ThanhPhanTrang.cs`](../src/QuanLyDienNuoc/Ui/ThanhPhanTrang.cs).

Hai chỗ dễ sai lặng lẽ, cả hai đều có test canh:

1. **Trang đang xem vượt quá cuối sổ.** Xoá dòng cuối của trang cuối, hay lọc hẹp lại, thì con
   trỏ trang vẫn trỏ vào một trang không còn tồn tại — bảng hiện ra trống trơn trong khi sổ vẫn
   đầy dòng. Nên trang luôn bị kẹp về khoảng còn hợp lệ.
2. **Câu tổng ở chân bảng phải cộng trên cả sổ, không cộng trên trang đang xem.** "5 khách hàng
   trong năm 2026" mà chỉ đếm 30 dòng đang hiện thì sai hẳn nghĩa. Xuất Excel cũng vậy: xuất cả
   sổ chứ không xuất một trang.

Nạp lại sau khi sửa thì **mở đúng trang có dòng đang chọn**, chứ không quăng về trang 1 — đang dò
dở giữa sổ mà bị đẩy về đầu là phải bấm lại cả chục lần. Vừa một trang thì hai nút ẩn hẳn.

Bảng chi tiết hoá đơn **không** chia trang: dòng vàng đang gõ dở phải luôn nằm ở cuối bảng, chia
trang là gõ ở trang 1 không thấy nó đâu.

## Nhập hàng: Enter đi một đường

**Không gợi ý gì trong lúc gõ.** Trước đây gõ tới đâu bung danh sách tới đó, rồi lúc ghi vào sổ
lại hỏi *"Danh mục chưa có «a». Ý anh là «abc» phải không?"* — đang nhập liền tay bị cắt nhịp hai
lần. Nay: gõ gì ra nấy. Danh sách vẫn nằm sẵn trong ô, muốn chọn thì bấm mũi tên mở ra; rời ô mà
tên **khớp hẳn** một mặt hàng thì phần mềm điền hộ đơn vị và đơn giá của khách, không đoán, không
hỏi. Gõ tắt ("o27", "27 ong") để riêng cho màn **Nhập nhiều dòng** — đó là chỗ nó có ích thật.

Ô ĐƠN GIÁ và SỐ LƯỢNG nhận cả phép tính (`3+2*4`). Gõ chữ vào đó rồi Enter thì **xoá trắng ô ấy**
và nhắc một câu ở thanh dưới: để nguyên chữ vô nghĩa thì người ta gõ tiếp vào giữa nó, ra một
chuỗi sai nữa.

### Giá gõ tắt: `8k`, `2tr5`

Trong màn **Nhập nhiều dòng**, giá viết sau dấu `@` và nhận luôn lối nói miệng: `@8k` là 8.000,
`@2tr` là 2.000.000, `@2tr5` là 2.500.000 ("hai triệu năm"). Nhận cả `nghìn`, `ngàn`, `ng`,
`triệu`, `trieu`, `củ`. Có nó vì đó là cách cả nước nói giá: bắt gõ đủ `2500000` là bảy chữ số
không có dấu ngăn, thừa hay thiếu một số 0 thì lệch mười lần — mà lệch giá thì tới lúc thu tiền
mới lộ.

**Một** chữ số sau đuôi là phần mười của đuôi ấy (`2tr5` = 2,5 triệu, `10k5` = 10.500). Hai chữ số
trở lên thì **không đoán**: `1tr50` có người hiểu 1.050.000, người hiểu 1.500.000. Không đọc được
thì bỏ trống giá và bảng xem trước ghi *"Chưa có giá — nhớ sửa lại"*, chứ tên hàng và số lượng vẫn
vào đúng — trượt cả dòng thì cả câu thành một cái tên hàng lạ, tệ hơn nhiều.

Chỉ giá mới đọc kiểu này, số lượng thì không: `x2k` hai nghìn cái ống là chuyện không có, mà nhận
bừa thì một cú gõ nhầm thành hai nghìn cái. Bộ đọc nằm ở `So.TryDocTien`.

Gõ tên hàng → `Enter` sang thẳng **ô số lượng** (đơn vị và đơn giá phần mềm tự điền theo danh
mục, gõ tay chỉ khi cần sửa) → `Enter` là ghi dòng. Thiếu ô nào thì **không có hộp thoại chặn
giữa**: thanh dưới nhắc một câu, con trỏ nhảy về đúng ô còn thiếu. Nhập cả chục dòng liền tay
mới không bị mất nhịp.

Ô NGÀY LẤY to hẳn ra (190 × 40px, chữ 14pt, lịch bung ra cũng cỡ đó) — trong hàng nhập thì đây
là ô phải bấm chuột nhiều nhất. Cả hàng cộng lại đúng **1286px**, vừa màn laptop 1366 mở toàn
màn hình: rộng hơn nữa là hàng nút bị đẩy ra ngoài rồi cắt mất.

Nhãn trên ô nhập chỉ để một hai chữ. Cách gõ tắt tên hàng, gõ phép tính ở ô đơn giá, gõ số âm
ở ô số lượng — chuyển hết vào chú thích hiện ra khi trỏ chuột vào đúng ô đó.

## Chọn nhiều dòng rồi làm một lượt

Bảng chi tiết cho chọn nhiều dòng: `Ctrl`+bấm để chọn thêm từng dòng, `Shift`+bấm để chọn cả
dải, `Ctrl+A` chọn hết bảng (trừ dòng vàng đang gõ dở). Xoá (`Delete`) và chuyển lên / xuống
(`Alt+↑` / `Alt+↓`) áp cho **cả nhóm đang chọn**, ghi thành một bước hoàn tác duy nhất.
Chuyển xuống thì chạy từ dòng cuối nhóm lên, chuyển lên thì từ dòng đầu xuống — làm ngược lại
là cả nhóm dồn cục vào nhau (`ThuTuDong.ChuyenNhom`). Chuyển xong nhóm vẫn được chọn, bấm
`Alt+↓` liên tiếp là cả nhóm đi tiếp.

Ba chỗ cho biết đang làm với mấy dòng, vì "chọn nhầm cả dải rồi bấm Delete" là lỗi khó lấy lại
nhất ở màn này:

- **Thanh dưới** hiện "Đang chọn 5 dòng · 1.250.000 đ — Delete xoá cả nhóm, Alt+↑ / Alt+↓
  chuyển cả nhóm" ngay khi chọn từ hai dòng trở lên; bỏ chọn thì về lời nhắc thường.
- **Menu chuột phải và nút ⋯** đổi chữ theo số dòng: "Xoá 5 dòng đã chọn", "Chuyển 5 dòng lên".
  Hai chỗ này ăn cùng một danh sách việc trong `DonHangForm.ViecVoiDongDangChon` nên không lệch
  nhau được.
- **Hộp hỏi lại trước khi xoá** nói rõ số dòng.

Bấm chuột phải **vào giữa nhóm đang chọn thì giữ nguyên cả nhóm**; chỉ khi bấm ra ngoài nhóm
mới chuyển con trỏ sang dòng đó. Đặt lại con trỏ là Windows bỏ hết dấu chọn của các dòng khác,
nên trước đây chọn 5 dòng rồi bấm chuột phải là lệnh trong menu chỉ còn xoá đúng một dòng.

## Chỗ làm khác bản thiết kế

1. **Cỡ chữ giữ to như cũ** (12–13pt cho chữ thường, không hạ về 14px của bản thiết kế). Chủ
   cửa hàng có tuổi, đây là thứ ngồi nhìn cả ngày.
2. **Phông chữ dùng Segoe UI**, không dùng Inter như bản thiết kế: máy khách không có Inter,
   mà kèm file phông vào bản cài chỉ để lệch đi một chút thì không đáng.
3. **Không có thẻ tổng quan, ô đại diện, chuông thông báo, biểu đồ.** Phần mềm chạy trên một máy ở cửa hàng,
   không có tài khoản người dùng và không có gì để thông báo. Chỗ đó dành cho việc thật: chọn
   năm và nút thêm khách hàng.

## Chữ bị cắt — bốn cái bẫy đã gặp thật

Máy khách đặt cỡ hiển thị Windows 125% là mọi chỗ đặt cứng đều lộ ra. Gốc của cả bốn là một điều:
**cỡ control đặt bằng con số điểm ảnh, còn phông đặt bằng điểm (`12pt`)** — phông tính theo điểm
thì to lên theo cỡ hiển thị, con số điểm ảnh thì không. Máy 125% là chữ dài thêm một phần tư trong
cái ô vẫn y nguyên. (Các form đặt `AutoScaleMode.Dpi` nhưng **không đặt `AutoScaleDimensions`**, mà
thiếu nó thì WinForms tính hệ số phóng ra 1 và không phóng cỡ control nào cả.)

Bốn chỗ đã cắt chữ thật và cách chữa:

1. **Nhãn trên ô nhập** (`NGÀY LẤY`, `ĐƠN VỊ`) — ô nhãn cao 20px, mà chữ hoa tiếng Việt có dấu cả
   trên (`Ầ`) lẫn dưới (`Ị`) nên cao hơn chữ hoa tiếng Anh: cắt mất dấu là đọc ra chữ khác. Nay ô
   nhãn cao 24px, và nhãn **tự vẽ** (`Theme.NhanO`) để đo cỡ chữ **lúc vẽ, trên bề ngang thật**.
   Đo sẵn lúc dựng là đem chiều dài chữ tính bằng điểm ảnh thật đi so với bề ngang chưa phóng —
   máy 125% là so lệch hẳn.
2. **Phụ đề của thanh tiêu đề cửa sổ** — tiêu đề 19pt đặt cứng ở `y = 14`, phụ đề ở `y = 52`. Cỡ
   chữ phóng lên thì hộp của tiêu đề tràn xuống quá 52, mà tiêu đề nằm trên nên nó **che mất nửa
   trên của phụ đề**. Nay hai nhãn neo `Top` xếp nối nhau, không còn toạ độ cứng.
3. **Tên cột trong đầu bảng** — tên dài trong cột hẹp thì Windows cho xuống hai dòng, mà đầu bảng
   cao cố định 46px là mất hẳn dòng dưới (`SỐ HĐ NỢ` chỉ còn thấy `SỐ HĐ`). Nay đầu bảng
   `AutoSize` theo chữ, lề trên dưới 9px để một dòng vẫn thoáng như cũ.
4. **Nút `NHẬP NHIỀU DÒNG`** — nút rộng cứng 210px, mà ở cỡ 100% chữ đã chiếm 150px trên 190px
   lòng nút: chỉ còn 40px dư, nên 125% là chữ tràn ra. `NutBo` vốn đỡ bằng cách **hạ cỡ chữ** cho
   vừa nút, nhưng hạ chữ trên máy người ta *cố tình* đặt chữ to là làm ngược điều họ muốn — mà
   dưới 9,5pt thì nó hết chỗ hạ và cắt thật. Nay `Theme.Nut(..., noTheoChu: true)` cho nút **nở
   ra theo chữ**: hai con số 210x44 thành mức thấp nhất, bề ngang thật đo lúc bố trí (
   `GetPreferredSize`) nên đo trên phông thật. Nút này nằm trong `FlowLayoutPanel` có `AutoSize`
   nên nở được mà không đè ai.

Quy tắc rút ra: **đừng đặt cứng cỡ cho thứ có chữ trong đó** — cứ coi con số ấy là mức thấp nhất.
Nút chữ dài thì `noTheoChu`, nhãn thì tự vẽ, đầu bảng thì `AutoSize`.

Màn **Nhập nhiều dòng** là chỗ đầu tiên làm theo quy tắc ấy cho cả cửa sổ: năm dòng của
`TableLayoutPanel` trước đây đặt cứng 92 / 150 / — / 56 / 80 px, nay **bốn dòng có chữ đều
`AutoSize`**, chỉ bảng xem trước ăn phần còn lại. Ô gõ thì cao đúng ba dòng chữ *của máy đó*
(`Theme.FontNhap.Height * 3`) thay vì 150px cứng, `Theme.ThanhTieuDe(..., tuCao: true)` cho thanh
tiêu đề tự cao, và ba dòng chỉ cách gõ thay cho một dòng dài 110 ký tự — dòng ấy vừa bị cắt mất
đuôi, mà đọc được đủ cũng không ai đọc hết ba luật nhồi một dòng.

Chữa gốc thì phải đặt `AutoScaleDimensions = new SizeF(96F, 96F)` cho từng form để WinForms phóng
mọi cỡ đặt tay theo cỡ hiển thị. Chưa làm: nó phóng lại **toàn bộ** bố cục của mọi cửa sổ một
lượt, mà máy làm việc là macOS nên không chạy thử được để nhìn — đổi kiểu ấy thì phải có máy
Windows ngồi soát từng màn hình.

## Thứ tự neo trong WinForms — chỗ dễ sai nhất

Trong một khung, control neo `Fill` phải **thêm vào trước**, control neo cạnh (`Top`, `Left`,
`Bottom`, `Right`) thêm vào sau. WinForms xếp các control neo theo thứ tự từ cái thêm sau về
cái thêm trước: cái thêm sau chiếm cạnh của nó trước, cái `Fill` thêm trước được xếp sau cùng
nên ăn đúng phần còn lại. Thêm ngược lại thì cái `Fill` chiếm hết chỗ và control neo cạnh
không còn dải nào để nằm.

## Ô gợi ý tên hàng — chỗ dễ sai thứ hai

Ô "TÊN HÀNG" là `ComboBox` kiểu `DropDown`, tự lọc theo kiểu gõ tắt rồi bung danh sách gợi ý.
Cái bẫy: **mỗi lần bung danh sách, Windows tự tìm dòng khớp đầu chữ, chọn nó rồi viết luôn tên
dòng đó vào ô nhập.** Gõ "o" là ô đã thành "Ống 27..."; gõ tiếp thì chữ mới lẫn vào giữa tên
cũ, ra một chuỗi vô nghĩa. Nên sau khi đặt `DroppedDown = true` phải:

1. chỉ bung khi danh sách đang đóng (`if (nenBung != _cboHang.DroppedDown)`), đừng bung lại mỗi
   lần gõ một chữ;
2. `SelectedIndex = -1` để bỏ dòng Windows tự chọn;
3. viết lại đúng chữ người dùng đang gõ rồi đặt con trỏ về cuối.

Danh sách vẫn hiện bình thường; muốn lấy một dòng thì bấm chuột, hoặc `↓` rồi `Enter`.

## Xem ảnh giao diện

Không cần máy Windows: đẩy mã nguồn lên GitHub, workflow `anh-giao-dien.yml` chụp lại từng màn
hình rồi commit vào [`docs/anh-giao-dien/`](anh-giao-dien/). Xem thêm mục *Xem giao diện mà
không có máy Windows* ở [README](../README.md).
