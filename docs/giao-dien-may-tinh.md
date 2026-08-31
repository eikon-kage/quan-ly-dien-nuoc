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
| `OChonNgay` | OChonNgay.cs | ô chọn ngày: ô nhập bo góc có lề, bấm nút lịch thì bung tờ lịch tiếng Việt |
| `BangLich` | BangLich.cs | tờ lịch tháng tự vẽ, chữ tiếng Việt (xem mục dưới) |

Mười lăm cửa sổ con không phải sửa gì: chúng dựng bằng đúng các hàm trên, đổi ở `Theme.cs` là
đổi hết một lượt.

## Màn hình chính

```
┌──────────────┬──────────────────────────────────────────────────────┐
│ Sổ điện nước │  [ô tìm khách]              Năm [2026] [+ Thêm khách]│
│              ├──────────────────────────────────────────────────────┤
│ Trang chủ    │  ┌ ⚠ 2 khách nợ quá 60 ngày …    [Mở sổ công nợ] ──┐│
│ Danh mục vật │  └─────────────────────────────────────────────────┘│
│              │  ┌ Khách hàng        ☐ Chỉ hiện khách có đơn ───────┐│
│              │  │ bảng khách hàng                                  ││
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
| Trang chủ, chân bảng khách | Mở đơn hàng · Thu tiền · Lịch sử thu tiền · Sửa khách hàng · Xoá khách hàng | *(không còn nút ⋯)* |
| Đơn hàng, dải tiêu đề | năm · hoá đơn · + Hoá đơn mới · In / xem trước | thu tiền · xem lịch sử thu tiền · hoàn hàng · chốt (mở lại) · sửa mã / ngày · xoá hoá đơn · bảng giá riêng · hoàn tác · Excel vào/ra |
| Đơn hàng, hàng nhập hàng | + Thêm dòng · − Trả lại | *(không còn)* |
| Đơn hàng, thanh tổng tiền | Nhập nhiều dòng | chèn dòng · chọn tất cả dòng · chuyển lên/xuống · xoá dòng đã chọn |
| Sổ công nợ | Mở đơn hàng · Thu tiền | xem lịch sử thu tiền · soạn tin nhắc nợ · xuất Excel |
| Sao lưu | Sao lưu ngay | xuất Excel · mở thư mục · khôi phục |
| Xem trước hoá đơn | In hoá đơn · trang trước/sau | phóng to · thu nhỏ · vừa màn hình |

**Chân bảng khách ở trang chủ là ngoại lệ: bày hết nút ra, bỏ hẳn nút ⋯.** Chỗ ấy chỉ có ba việc
giấu bên trong (lịch sử thu tiền, sửa khách, xoá khách) mà lại là màn hình mở suốt ngày — giấu đi
thì chủ cửa hàng phải bấm thử mới biết trong đó có gì. Đổi lại hàng nút dài ra, nên dải ấy
**tự xuống hàng và tự cao theo chữ** (`FlowLayoutPanel.WrapContents`, dải nền `AutoSize`): cửa sổ
kéo hẹp hay máy đặt cỡ hiển thị 125% thì nút cuối tụt xuống hàng dưới, chứ không lấn sang đè lên
dòng tổng kết và thanh phân trang bên phải. Mấy màn còn lại vẫn giữ nút ⋯ vì menu dài hơn hẳn —
riêng màn đơn hàng là mười việc, bày hết ra thì không màn hình nào chứa nổi.

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

Các bảng dài — **khách hàng** (trang chủ), **sổ công nợ**, các bảng của màn **chấm công**, và
**bảng hàng trong đơn hàng của khách** — chỉ đổ 30 dòng vào lưới một lúc. Phép chia trang nằm ở
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

### Bảng hàng của hoá đơn: chia trang mà vẫn gõ được

Bảng này lâu nay để nguyên không chia trang, vì **dòng vàng đang gõ dở** nằm lẫn trong bảng — chia
trang thì đứng ở trang 1 không thấy nó đâu. Hoá đơn công trình dài vài trăm dòng nên vẫn phải
chia; chỗ dòng vàng xử lý như sau:

- Mở một hoá đơn ra là vào thẳng **trang cuối**, không phải trang 1: hàng mới nhất và dòng vàng
  đều ở đấy, đó là chỗ cần tới ngay. Nạp lại cùng hoá đơn ấy (sửa, xoá, hoàn tác) thì giữ nguyên
  trang đang xem.
- Dòng vàng vẫn giữ đúng chỗ của nó trong **cả bảng** (cuối bảng, hoặc cạnh dòng mốc khi bấm
  Ctrl+Enter chèn giữa), rồi mới cắt trang. Nó nằm trang nào là do chỗ chèn quyết định.
- Mọi đường dẫn con trỏ về dòng vàng (Ctrl+Enter, Enter ghi xong một dòng, bấm "Không" ở câu hỏi
  kiểm tra) đều **mở đúng trang có nó** trước khi đặt con trỏ — nếu không thì đặt con trỏ vào một
  hàng không có trên lưới, trượt không trúng gì cả.
- Lật trang thì thanh dưới nói luôn dòng vàng đang ở trang nào. Còn thanh nhập nhanh phía **trên**
  bảng thì trang nào cũng ghi được, ghi xong bảng tự nhảy tới trang chứa dòng vừa ghi.
- Ctrl+A chỉ chọn được các dòng **của trang đang xem** — lưới chỉ giữ đến đấy. Nhiều trang thì câu
  nhắc ghi rõ "ở trang này", để không ai bấm Delete xong mới ngã ngửa là còn sót mấy trang kia.

Ba con số tiền dưới bảng (tổng cộng, đã trả, còn lại) vẫn tính trên **cả hoá đơn**, in và xuất
Excel cũng cả hoá đơn — đúng luật số 2 ở trên.

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

## Bảng hàng không tự xếp lại dòng

Thứ tự các dòng trong hoá đơn là **đúng thứ tự chủ cửa hàng đã gõ** — không xếp theo ngày, không
xếp theo vần ([`ThuTuDong.cs`](../src/QuanLyDienNuoc.Core/Ui/ThuTuDong.cs)). Bảng trên màn hình,
tờ in ra giấy và file Excel đều đi theo thứ tự ấy.

Trước đây bảng tự xếp theo ngày lấy hàng. Bỏ đi vì phép xếp ấy giành quyền của người dùng: gõ bù
một dòng của hôm trước là dòng ấy tự nhảy lên giữa bảng, sửa ô NGÀY một dòng là nó biến mất khỏi
chỗ đang nhìn. Tờ hoá đơn viết tay vốn hàng nào ghi trước thì nằm trước.

Bỏ phép xếp thì mấy chỗ dựng quanh nó cũng bỏ theo:

- `Alt+↑` / `Alt+↓` **đi được khắp bảng**, không còn bị chặn ở mép ngày. Câu nhắc khi hết đường
  cũng đổi thành "đã ở đầu / cuối bảng" chứ không phải "đầu / cuối ngày".
- Dòng chèn bằng `Ctrl+Enter` **giữ nguyên ngày người dùng gõ**. Trước đây phần mềm ép ngày của nó
  theo dòng mốc, chỉ để phép xếp khỏi kéo nó đi chỗ khác; dòng trống mới vẫn điền sẵn ngày của
  dòng mốc, nhưng chỉ để đỡ phải gõ lại.
- Mốc ngày trên tờ in hiện ra mỗi khi dòng dưới đổi ngày so với dòng trên, nên một ngày có thể
  hiện mốc mấy lần nếu chủ cửa hàng gõ xen kẽ — vẫn đọc lại đúng ngày lúc nhập tờ ấy vào.

## Chọn nhiều dòng rồi làm một lượt

Bảng chi tiết cho chọn nhiều dòng: `Ctrl`+bấm để chọn thêm từng dòng, `Shift`+bấm để chọn cả
dải, `Ctrl+A` chọn hết **trang đang xem** (trừ dòng vàng đang gõ dở). Xoá (`Delete`) và chuyển lên / xuống
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

### Nay cả app đi theo quy tắc ấy — bằng năm món dùng chung của `Theme`

Sửa từng màn hình một thì lần sau vẫn có người chép lại kiểu cũ, nên phần xếp hình đã gom vào
`Theme` để mọi màn hình cùng đi qua một chỗ:

| Món | Thay cho | Được gì |
| --- | --- | --- |
| `Theme.HangO(mauNen, ...ô)` | `Panel` + `FlowLayoutPanel` chép tay ở mỗi màn | Dải ô nhập tự cao theo chữ, **ô tự xuống hàng dưới** khi cửa sổ hẹp hay cỡ chữ to |
| `Theme.ThanhDuoi(ghiChu, ...nút)` | `Panel` cao cứng 84px + nhãn `Dock = Right` rộng cứng | Hàng nút cuối cửa sổ tự cao, câu ghi chú neo phải tự xuống dòng |
| `Theme.ThanhTrangThai(nhãn)` | tám bản chép y hệt nhau trong tám form | Dải trạng thái đáy cửa sổ tự cao, câu dài tự xuống dòng |
| `Theme.NhanDaiDong(chữ)` | `Label` một dòng trong ô cao cứng | Nhãn chứa **câu** thì tự xuống dòng theo bề ngang thật, tự cao theo số dòng |
| `Theme.TruongNhieuDong(nhãn, ô, rộng, sốDòng)` | nhãn + ô ghi chú đặt toạ độ tay | Ô nhiều dòng cao đúng số dòng chữ *của máy đó* |

Kèm theo là hai chỗ chặn ở tầng dưới, khỏi phải nhớ:

- `Theme.Truong` lấy bề cao nhãn theo `Theme.FontNhan.Height` (24px chỉ còn là mức thấp nhất), và
  mở ra hai số dùng chung `Theme.CaoNhanTrongTruong` / `Theme.DinhOTrongTruong` — nút đứng cùng
  hàng với ô nhập lùi xuống đúng bằng chỗ cái nhãn, khỏi đoán 22 hay 26 px.
- `Theme.ApDungLuoi` chặn **bề ngang thấp nhất của mỗi cột theo từ dài nhất trong tên cột**. Cột
  chia theo tỷ lệ nên bảng chật là mọi cột co lại, co quá thì "SỐ LƯỢNG" chỉ còn thấy "SỐ" mà
  "TRANG" còn thấy "G". Chặn theo *từ* chứ không cả tên: tên hai chữ vẫn được xuống hai dòng như
  cũ, chỉ cấm cắt ngang một chữ. Cột có nội dung dài sẵn (ngày `25/02/2026`, tiền triệu) thì thêm
  `Theme.Cot(..., toiThieu: 104)`.

Nút thì hầu hết đã bật `noTheoChu` — kể cả nút phụ (`Theme.NutPhu(..., noTheoChu: true)`).

Hai chỗ nữa cùng bệnh, đã chữa cùng đợt: bảng chỉ đủ chỗ cho vài cột thì **bớt cột** chứ đừng
nhồi (bảng các trang trong lô ở hai màn nhập từ file bỏ cột BẢNG và TÊN KHÁCH, đưa vào lời mách
của dòng), và nhãn dài hơn ô của nó thì **rút gọn nhãn**, câu đầy đủ để vào lời mách
("NGÀY LẤY HÀNG CHO CÁC DÒNG" → "NGÀY LẤY HÀNG").

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

## Ô chọn ngày: vì sao phải tự vẽ lấy tờ lịch

`DateTimePicker` của WinForms hỏng **hai chỗ**, và cả hai đều không vá được từ bên ngoài:

1. **Bảng lịch bung ra** do Windows tự vẽ, lấy tên tháng và tên thứ theo *cài đặt Region của
   máy*, **không** theo ngôn ngữ phần mềm đặt trong `Program.cs`. Máy cài Windows tiếng Anh thì
   chủ cửa hàng bấm mũi tên là thấy "August 2026 — S M T W T F S". Đặt `CultureInfo` hay gọi
   `SetThreadLocale` đều không chắc đổi được bảng ấy.
2. **Ô gõ viết chữ dính sát viền trái**, không có lề — mà lề thì không đặt được, `DateTimePicker`
   không có `Padding` cũng không bỏ được viền của nó. Ở cỡ chữ to của phần mềm, chữ số đầu của
   ngày trông như bị cắt cụt. Nới ô rộng ra cũng vô ích: chữ căn trái nên chỗ thừa rơi hết về
   bên phải.

Nên phần mềm **thay hẳn cả ô lẫn lịch**:

- [`OChonNgay`](../src/QuanLyDienNuoc/Ui/OChonNgay.cs) — ô chọn ngày dùng ở 6 màn hình. Ruột là
  `TextBox` thường đặt trong khung bo góc do ô tự vẽ, **lề trái 10px giống mọi ô nhập khác**;
  bên phải, nằm trong khung, là nút hình tờ lịch. Bề ngang tối thiểu `RongToiThieu` được **đo**
  theo cỡ chữ đang dùng (`TextRenderer.MeasureText`) rồi khoá vào `MinimumSize`, nên máy đặt cỡ
  hiển thị 125% thì ô nở theo — `Theme.Truong` nới khung có nhãn theo `MinimumSize` của ô.
- [`NgayViet`](../src/QuanLyDienNuoc.Core/Ui/NgayViet.cs) — đọc chữ người dùng gõ. Gõ kiểu gì
  cũng nhận: `3/8`, `03/08`, `3-8-26`, `3.8.2026`, `3\8`, `3108`, `31082026`; thiếu năm thì lấy
  năm của ngày đang chọn. Gõ sai (`31/2`, `29/2/2026`) thì **trả ô về ngày cũ chứ không đoán
  bừa**. Phím: `↑↓` chỉnh từng ngày, `PageUp/PageDown` chỉnh từng tháng, `F4` hoặc `Alt+↓` bung
  lịch, `Esc` bỏ chữ vừa gõ. Test: [`NgayVietTests`](../tests/QuanLyDienNuoc.Tests/NgayVietTests.cs).
- [`BangLich`](../src/QuanLyDienNuoc/Ui/BangLich.cs) — tờ lịch: "Tháng 8, 2026", cột `T2 T3 T4 T5
  T6 T7 CN` bắt đầu từ thứ hai như lịch treo tường, cột chủ nhật màu đỏ, ngày đang chọn tô đặc,
  hôm nay viền xanh, chân bảng có dòng "Hôm nay: Thứ hai, 31/08/2026" bấm được. Lật tháng bằng
  `‹ ›`, lật năm bằng `‹‹ ››`; bàn phím thì mũi tên đi từng ngày, `PageUp/PageDown` đổi tháng,
  `Enter` chọn, `Esc` bỏ.
- [`LichViet`](../src/QuanLyDienNuoc.Core/Ui/LichViet.cs) — phần tính toán (xếp 42 ô của tháng,
  tên thứ, tên tháng) để ở Core, **không dính WinForms**, nên chạy được `dotnet test` trên máy
  Mac: xem [`LichVietTests`](../tests/QuanLyDienNuoc.Tests/LichVietTests.cs). Cách đọc ngày gõ
  tay cũng vậy — cả hai phần khó của ô ngày đều test được mà không cần máy Windows.

Mọi số đo của tờ lịch tính theo `Font.Height` nên máy đặt cỡ hiển thị 125% hay 150% thì lịch nở
theo, không vỡ chữ. Ảnh `21-lich-chon-ngay.png` trong [`docs/anh-giao-dien/`](anh-giao-dien/) là
tờ lịch chụp trên máy Windows thật.

## Xem ảnh giao diện

Không cần máy Windows: đẩy mã nguồn lên GitHub, workflow `anh-giao-dien.yml` chụp lại từng màn
hình rồi commit vào [`docs/anh-giao-dien/`](anh-giao-dien/). Xem thêm mục *Xem giao diện mà
không có máy Windows* ở [README](../README.md).
