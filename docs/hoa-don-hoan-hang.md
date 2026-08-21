# Hoá đơn hoàn hàng

Khách mang hàng trả về thì cửa hàng có **hai đường**, chọn theo chỗ tờ hoá đơn đang ở đâu:

| Tình huống | Làm gì | Kết quả trong sổ |
|---|---|---|
| Hoá đơn còn đang mở, chưa in cho khách | Nút **− TRẢ LẠI** ở thanh nhập (hoặc gõ số lượng âm `-2`) | Thêm một dòng số lượng âm vào chính hoá đơn đó |
| Hoá đơn đã in cho khách, hoặc đã chốt | Nút ⋯ → **Hoàn hàng cho hoá đơn này** | Một tờ hoá đơn riêng, mã `HH2026-01`, hoàn cho hoá đơn đó |

Ranh giới là **tờ giấy khách đang giữ**. Sửa vào hoá đơn khách đã cầm về thì hai bên giữ hai
con số khác nhau, đối chiếu là cãi nhau; còn tờ hoàn là chứng từ riêng, đưa khách một bản là
hai bên khớp sổ. Vì vậy hoá đơn **đã chốt vẫn hoàn được** — chốt là chặn sửa vào tờ cũ, mà
hoàn hàng thì không sửa vào tờ cũ một chữ nào.

## Lập tờ hoàn

Mở đơn hàng của khách, chọn hoá đơn bán ở ô trên, rồi nút ⋯ → *Hoàn hàng cho hoá đơn này*:

- Bảng bày ra **từng dòng hàng của hoá đơn gốc**, kèm đã lấy bao nhiêu, đã hoàn bao nhiêu ở
  những lần trước và **còn hoàn được** bao nhiêu.
- Gõ số vào cột **SỐ HOÀN** (ô tô vàng) ở những dòng khách mang về. Khách trả cả lô thì bấm
  **ĐIỀN HOÀN HẾT** rồi sửa lại vài dòng.
- Gõ quá số còn hoàn được thì phần mềm sửa lại đúng số đó và nhắc một câu ở thanh dưới, chứ
  không bật hộp thoại chặn giữa lúc đang gõ.
- **NGÀY HOÀN** và **LÝ DO HOÀN** (hàng lỗi, khách lấy thừa, sai chủng loại…) đều in lên tờ giấy.
- Bấm **TẠO HOÁ ĐƠN HOÀN HÀNG**. Nhầm thì `Ctrl+Z`.

Giá hoàn lấy đúng **giá đã bán** trên dòng gốc, không lấy giá hiện tại của danh mục: giá lên
xuống theo tháng, hoàn theo giá mới là một trong hai bên bị hụt.

## Số tiền chạy đi đâu

Trong sổ, các dòng của tờ hoàn ghi **số lượng âm** nên tổng tiền của tờ là số âm. Nhờ vậy
mọi chỗ đang cộng hoá đơn lại không phải biết gì thêm về loại hoá đơn này:

- Trang chủ: cột *TỔNG MUA* và *CÒN NỢ* của khách đã trừ phần hoàn.
- Sổ công nợ: hoàn hết thì khách hết nợ, không còn bị nhắc.
- Tin nhắc nợ: bảng kê có riêng một dòng `HH2026-01 (hoàn hàng ngày …): trừ 90.000đ`, để
  khách đối chiếu ra đúng con số.

Tiền hoàn **trừ vào nợ của khách**, phần mềm không có chỗ ghi "đã trả lại khách bằng tiền
mặt". Khách đã trả hết tiền rồi mới hoàn hàng thì sổ để *còn nợ* âm — tức là cửa hàng đang
giữ thừa tiền của khách, lần lấy hàng sau tự trừ vào.

## In và xuất Excel

Tờ hoàn dùng chung mẫu giấy với hoá đơn bán (`MauHoaDon/trang-1.xls`), chỉ khác:

- Tên tờ ở dòng trống ngay trên bảng hàng: **HÓA ĐƠN HOÀN HÀNG (Hoàn cho hoá đơn HD2026-02
  ngày 02/06/2026 — lý do)**. Mẫu giấy hiện tại dành cả góc trên phải cho số tài khoản ngân
  hàng nên không còn ô tên tờ riêng; mẫu cũ có ô đó thì phần mềm vẫn ghi vào đúng chỗ, phần
  "hoàn cho hoá đơn nào" xuống dòng phụ đề bên dưới.
- Dòng tổng ghi **TỔNG TIỀN HOÀN LẠI**, chỗ ký là *KHÁCH TRẢ HÀNG* / *NGƯỜI NHẬN HÀNG*.
- Số lượng và số tiền in ra **số dương**: cả tờ giấy đã nói là hoàn rồi, in kèm dấu trừ nữa
  thì khách đọc thành hoàn của hoàn. Trong sổ chúng vẫn là số âm.

Nhập ngược lại bằng *Nhập hàng từ file Excel* vẫn đúng dấu: đọc thấy tên tờ là hoàn hàng thì
phần mềm đổi số lượng thành số âm.

## Mã hoá đơn

Hai loại đánh số riêng theo từng khách, từng năm: hoá đơn bán là `HD2026-01`, `HD2026-02`…,
tờ hoàn là `HH2026-01`, `HH2026-02`… Lập tờ hoàn không làm nhảy số hoá đơn bán tiếp theo.
Tờ hoàn thuộc **đúng năm của hoá đơn gốc**, kể cả khi khách trả hàng sang năm sau — hai tờ
phải nằm cùng một năm mới đối chiếu được với nhau.

## Không sửa từng dòng trên tờ hoàn

Chọn một tờ `HH…` ở ô hoá đơn thì bảng chi tiết chỉ để xem: số hoàn phải khớp với hoá đơn
gốc, sửa tay trên lưới là hoàn quá số khách đã lấy mà không ai chặn. Hoàn thiếu thì lập thêm
một tờ hoàn nữa; hoàn sai thì xoá cả tờ (nút ⋯ → *Xoá hoá đơn này*) rồi lập lại.

## Chỗ để trong mã nguồn

| Việc | File |
|---|---|
| Loại hoá đơn, tờ gốc, dấu khi in | [Models/LoaiHoaDon.cs](../src/QuanLyDienNuoc.Core/Models/LoaiHoaDon.cs), [Models/HoaDon.cs](../src/QuanLyDienNuoc.Core/Models/HoaDon.cs) |
| Còn hoàn được bao nhiêu, lập tờ hoàn | [BaoCao/HoanHang.cs](../src/QuanLyDienNuoc.Core/BaoCao/HoanHang.cs) |
| Màn hình hoàn hàng | [Forms/HoanHangForm.cs](../src/QuanLyDienNuoc/Forms/HoanHangForm.cs) |
| Bản in | [Ui/InHoaDon.cs](../src/QuanLyDienNuoc/Ui/InHoaDon.cs) |
| Xuất / đọc Excel | [Excel/XuatHoaDon.cs](../src/QuanLyDienNuoc.Core/Excel/XuatHoaDon.cs), [Excel/DocHoaDon.cs](../src/QuanLyDienNuoc.Core/Excel/DocHoaDon.cs) |
| Kiểm thử | [HoanHangTests.cs](../tests/QuanLyDienNuoc.Tests/HoanHangTests.cs) |
