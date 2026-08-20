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
| `Theme.Nut` / `Theme.NutPhu` | Theme.cs | nút bo góc tự vẽ, có trạng thái trỏ chuột / đang bấm / đang được bàn phím chọn |
| `Theme.HopO` / `Theme.HopTim` | Theme.cs | ô nhập bo góc; ô tìm kiếm có kính lúp và chữ gợi ý mờ |
| `Theme.ApDungLuoi` | Theme.cs | bảng kiểu mới: đầu bảng trắng, kẻ dòng mảnh |
| `Theme.ThanhTieuDe` | Theme.cs | dải tiêu đề đầu mỗi cửa sổ con: nền trắng, kẻ một vạch dưới |
| `ThanhBen` | ThanhBen.cs | thanh bên trái của màn hình chính, kèm hình vẽ nét |
| `OThongKe` | OThongKe.cs | một ô số liệu trong thẻ tổng quan |

Mười lăm cửa sổ con không phải sửa gì: chúng dựng bằng đúng các hàm trên, đổi ở `Theme.cs` là
đổi hết một lượt.

## Màn hình chính

```
┌──────────────┬──────────────────────────────────────────────────────┐
│ Sổ điện nước │  [ô tìm khách]              Năm [2026] [+ Thêm khách]│
│              ├──────────────────────────────────────────────────────┤
│ Trang chủ    │  ┌ Tổng quan năm 2026 ──────────────────────────────┐│
│ Sổ công nợ   │  │ Khách hàng │ Tổng mua │ Đã thu │ Còn nợ          ││
│ Danh mục vật │  │     5      │ 9.985.000│ 7.500..│ 2.485.000       ││
│ Bộ hàng      │  └─────────────────────────────────────────────────┘│
│              │  ┌ ⚠ 2 khách nợ quá 60 ngày …    [Mở sổ công nợ] ──┐│
│ Sao lưu      │  └─────────────────────────────────────────────────┘│
│ Nhật ký      │  ┌ Khách hàng        ☐ Chỉ hiện khách có đơn ───────┐│
│              │  │ bảng khách hàng                                  ││
│              │  │ [Mở đơn hàng] [Thu tiền] [Sửa] [Xoá]   5 khách   ││
│              │  └─────────────────────────────────────────────────┘│
└──────────────┴──────────────────────────────────────────────────────┘
```

Bốn ô số liệu tính theo **đúng danh sách đang hiện**: lọc theo năm hay gõ từ khoá tìm thì số
trên thẻ cũng chạy theo, để số trên thẻ không bao giờ lệch với bảng ở dưới.

## Chỗ làm khác bản thiết kế

1. **Cỡ chữ giữ to như cũ** (12–13pt cho chữ thường, không hạ về 14px của bản thiết kế). Chủ
   cửa hàng có tuổi, đây là thứ ngồi nhìn cả ngày.
2. **Phông chữ dùng Segoe UI**, không dùng Inter như bản thiết kế: máy khách không có Inter,
   mà kèm file phông vào bản cài chỉ để lệch đi một chút thì không đáng.
3. **Không có ô đại diện, chuông thông báo, biểu đồ.** Phần mềm chạy trên một máy ở cửa hàng,
   không có tài khoản người dùng và không có gì để thông báo. Chỗ đó dành cho việc thật: chọn
   năm và nút thêm khách hàng.

## Thứ tự neo trong WinForms — chỗ dễ sai nhất

Trong một khung, control neo `Fill` phải **thêm vào trước**, control neo cạnh (`Top`, `Left`,
`Bottom`, `Right`) thêm vào sau. WinForms xếp các control neo theo thứ tự từ cái thêm sau về
cái thêm trước: cái thêm sau chiếm cạnh của nó trước, cái `Fill` thêm trước được xếp sau cùng
nên ăn đúng phần còn lại. Thêm ngược lại thì cái `Fill` chiếm hết chỗ và control neo cạnh
không còn dải nào để nằm.

## Xem ảnh giao diện

Không cần máy Windows: đẩy mã nguồn lên GitHub, workflow `anh-giao-dien.yml` chụp lại từng màn
hình rồi commit vào [`docs/anh-giao-dien/`](anh-giao-dien/). Xem thêm mục *Xem giao diện mà
không có máy Windows* ở [README](../README.md).
