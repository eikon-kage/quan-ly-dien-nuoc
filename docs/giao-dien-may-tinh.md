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
| `Theme.Nut` / `Theme.NutPhu` | Theme.cs | nút bo góc tự vẽ, có trạng thái trỏ chuột / đang bấm / đang được bàn phím chọn; chữ không vừa nút thì tự hạ cỡ chứ không để bị cắt |
| `Theme.NutBaCham` | Theme.cs | nút ⋯ gom các việc ít dùng vào một menu |
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
| Đơn hàng, dải tiêu đề | năm · hoá đơn · + Hoá đơn mới · In / xem trước | thu tiền · trả cho hoá đơn · chốt (mở lại) · sửa mã · xoá hoá đơn · nhập nhiều dòng · bảng giá riêng · nhắc nợ · hoàn tác · làm lại · Excel vào/ra |
| Đơn hàng, hàng nhập hàng | + Thêm dòng · − Trả lại | *(không còn)* |
| Đơn hàng, thanh tổng tiền | *(chỉ còn nút ⋯)* | chèn dòng · chuyển lên/xuống · xoá dòng |
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

## Nhập hàng: Enter đi một đường

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
dải. Xoá (`Delete`) và chuyển lên / xuống (`Alt+↑` / `Alt+↓`) áp cho **cả nhóm đang chọn**,
ghi thành một bước hoàn tác duy nhất. Chuyển xuống thì chạy từ dòng cuối nhóm lên, chuyển lên
thì từ dòng đầu xuống — làm ngược lại là cả nhóm dồn cục vào nhau. Chuyển xong nhóm vẫn được
chọn, bấm `Alt+↓` liên tiếp là cả nhóm đi tiếp.

## Chỗ làm khác bản thiết kế

1. **Cỡ chữ giữ to như cũ** (12–13pt cho chữ thường, không hạ về 14px của bản thiết kế). Chủ
   cửa hàng có tuổi, đây là thứ ngồi nhìn cả ngày.
2. **Phông chữ dùng Segoe UI**, không dùng Inter như bản thiết kế: máy khách không có Inter,
   mà kèm file phông vào bản cài chỉ để lệch đi một chút thì không đáng.
3. **Không có thẻ tổng quan, ô đại diện, chuông thông báo, biểu đồ.** Phần mềm chạy trên một máy ở cửa hàng,
   không có tài khoản người dùng và không có gì để thông báo. Chỗ đó dành cho việc thật: chọn
   năm và nút thêm khách hàng.

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
