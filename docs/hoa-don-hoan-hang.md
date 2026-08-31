# Hoá đơn hoàn hàng

Khách mang hàng trả về thì cửa hàng có **ba đường**, chọn theo chỗ tờ hoá đơn đang ở đâu:

| Tình huống | Làm gì | Kết quả trong sổ |
|---|---|---|
| Hoá đơn còn đang mở, chưa in cho khách | Nút **− TRẢ LẠI** ở thanh nhập (hoặc gõ số lượng âm `-2`) | Thêm một dòng số lượng âm vào chính hoá đơn đó |
| Hoá đơn đã in cho khách, hoặc đã chốt | Nút ⋯ → **Hoàn hàng cho hoá đơn này** | Một tờ hoá đơn riêng, mã `HH2026-01`, hoàn cho hoá đơn đó |
| Đã có sẵn file Excel tờ hoàn (máy khác xuất ra, hoặc gõ trên Excel) | Nút ⋯ → **Nhập hoá đơn / tờ hoàn từ file Excel** | Cũng một tờ `HH2026-01` riêng, các dòng lấy từ file |

Ranh giới là **tờ giấy khách đang giữ**. Sửa vào hoá đơn khách đã cầm về thì hai bên giữ hai
con số khác nhau, đối chiếu là cãi nhau; còn tờ hoàn là chứng từ riêng, đưa khách một bản là
hai bên khớp sổ. Vì vậy hoá đơn **đã chốt vẫn hoàn được** — chốt là chặn sửa vào tờ cũ, mà
hoàn hàng thì không sửa vào tờ cũ một chữ nào.

## Lập tờ hoàn

Mở đơn hàng của khách, chọn hoá đơn bán ở ô trên, rồi nút ⋯ → *Hoàn hàng cho hoá đơn này*.
Tờ hoàn là **một hoá đơn mới, nhập riêng**: bảng bày ra trống, gõ tay từng món khách mang
trả về chứ không phải chọn trong hàng của hoá đơn gốc.

- Mỗi dòng gõ **tên hàng, đơn vị, đơn giá, số hoàn** (và ghi chú nếu cần). Gõ xong tên hàng
  thì đơn vị và giá **tự điền**: lấy đúng giá đã bán trên hoá đơn gốc trước, món không có
  trên tờ gốc thì tra sang bảng giá riêng của khách. Ô nào đã gõ tay thì để nguyên.
- Ô tên hàng có **gợi ý**: món trên hoá đơn gốc và cả danh mục vật tư.
- Gõ xong một dòng là bảng tự mở thêm dòng trống ở dưới. Còn nút **+ THÊM DÒNG**, nút
  **Xoá dòng đang chọn** và phím `Delete` cho dòng gõ nhầm.
- Số hoàn và đơn giá gõ **số dương**; vào sổ phần mềm tự ghi thành số âm. Gõ số âm thì phần
  mềm lấy lại số dương và nhắc một câu ở thanh dưới.
- **NGÀY HOÀN** và **LÝ DO HOÀN** (hàng lỗi, khách lấy thừa, sai chủng loại…) đều in lên tờ
  giấy. Cả tờ ghi theo đúng ngày hoàn, khỏi gõ ngày ở từng dòng.
- Bấm **TẠO HOÁ ĐƠN HOÀN HÀNG**. Nhầm thì `Ctrl+Z`.

Nhập riêng như vậy vì hàng khách mang trả về không phải lúc nào cũng đúng một dòng trên tờ
gốc: khách đổi món lấy từ lần khác, hai bên thoả lại giá lúc hoàn, hay gộp mấy dòng thành
một. Đổi lại, phần mềm **không tự chặn hoàn quá số đã bán** nữa — người lập tự soát với tờ
hoá đơn gốc (mã và ngày của nó ghi ngay trên đầu cửa sổ).

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

## Nhập tờ hoàn từ file Excel

Tờ hoàn **cũng là một đơn hàng**: có file Excel riêng và nhập vào y như hoá đơn bán, chỉ khác
là tiền của nó trừ đi. Vào nút ⋯ → *Nhập hoá đơn / tờ hoàn từ file Excel*:

- Cột **LOẠI** ở bảng các tờ tìm thấy ghi rõ từng tờ là *Bán hàng* hay *Hoàn hàng* — nhận ra
  ở tên tờ in phía trên bảng hàng.
- Tích tờ hoàn thì ô **NHẬP VÀO** đổi sang danh sách tờ hoàn: *Tạo hoá đơn hoàn hàng mới*, hoặc
  một tờ `HH…` đang mở để nhập thêm vào. Tờ bán và tờ hoàn **không nhập lẫn vào nhau** được (một
  tờ cộng vào nợ, tờ kia trừ ra), tích lẫn cả hai loại một lượt thì phần mềm chặn và nhắc.
- Số lượng trên giấy là số dương, vào sổ thành **số âm** nên tự trừ vào nợ như tờ lập bằng tay.
- File có ghi *"Hoàn cho hoá đơn HD2026-02"* thì tờ hoàn mới **nối lại đúng hoá đơn đó** (và
  thuộc đúng năm của nó, năm ghi luôn trong dòng *Tạo hoá đơn hoàn hàng mới* để không tưởng tờ
  mới nằm ở năm đang xem), lý do hoàn in trên giấy cũng lấy lại làm lý do của tờ. Không có dòng
  đó thì tờ hoàn đứng riêng một mình — vẫn trừ vào nợ như thường.
- Mã hoá đơn gốc và lý do đọc theo **đúng những bảng đang tích**, không phải bảng đầu tiên của
  file. Các bảng đang tích ghi hoàn cho những hoá đơn khác nhau thì phần mềm chặn và nhắc tích
  riêng từng hoá đơn gốc một lượt — một tờ hoàn chỉ nối vào một hoá đơn bán.
- Nhập thêm vào một tờ `HH…` sẵn có mà giấy ghi mã của hoá đơn khác thì phần mềm **chặn**: nhập
  vào đó là trừ số đã hoàn vào hoá đơn không phải nó, còn hoá đơn trên giấy vẫn để 0 nên hoàn
  được lần thứ hai.
- Giấy ghi mã mà trong sổ của khách này không có mã đó (hoá đơn của khách khác, hay giấy ghi
  sai) thì hỏi lại trước khi nhập: nhập tiếp vẫn được, nợ vẫn trừ đúng, chỉ là tờ hoàn đứng
  riêng nên hoá đơn kia không biết đã hoàn.
- Nối được vào hoá đơn gốc thì từng dòng còn ghép vào **đúng dòng hàng** của hoá đơn đó (khớp cả
  tên hàng và đơn giá), nên **lần nhập file sau** biết món ấy đã hoàn bao nhiêu rồi mà nhắc khi
  hoàn quá số khách đã lấy. Hoá đơn gốc bán món đó ở hai ngày thì một dòng trên giấy tách ra
  hai dòng trong sổ, đúng số của từng ngày. (Tờ hoàn gõ tay không nối vào dòng gốc: nó ghi
  đúng những gì đã gõ, nên cũng không góp vào con số này.)
- Món không có trên hoá đơn gốc, giá lệch, hay hoàn quá số khách đã lấy: **vẫn ghi vào tờ hoàn**
  (sổ phải khớp tờ giấy khách đang giữ) nhưng có câu nhắc "cần xem lại" ở hộp thoại sau khi nhập.

## Mã hoá đơn

Hai loại đánh số riêng theo từng khách, từng năm: hoá đơn bán là `HD2026-01`, `HD2026-02`…,
tờ hoàn là `HH2026-01`, `HH2026-02`… Lập tờ hoàn không làm nhảy số hoá đơn bán tiếp theo.
Tờ hoàn thuộc **đúng năm của hoá đơn gốc**, kể cả khi khách trả hàng sang năm sau — hai tờ
phải nằm cùng một năm mới đối chiếu được với nhau.

## Không sửa từng dòng trên tờ hoàn

Chọn một tờ `HH…` ở ô hoá đơn thì bảng chi tiết chỉ để xem. Tờ hoàn là chứng từ đã đưa
khách, lập trọn một lượt ở màn hình Hoàn hàng — sửa lắt nhắt trên lưới là tờ trong sổ lệch
tờ giấy khách đang giữ. Hoàn thiếu thì lập thêm một tờ hoàn nữa; hoàn sai thì xoá cả tờ
(nút ⋯ → *Xoá hoá đơn này*) rồi lập lại.

Thêm dòng vào một tờ `HH…` sẵn có thì chỉ có một đường: **nhập từ file Excel** — file là chứng
từ nói rõ hoàn những gì, khác hẳn gõ tay từng ô trên lưới mà không có gì đối chiếu.

## Chỗ để trong mã nguồn

| Việc | File |
|---|---|
| Loại hoá đơn, tờ gốc, dấu khi in | [Models/LoaiHoaDon.cs](../src/QuanLyDienNuoc.Core/Models/LoaiHoaDon.cs), [Models/HoaDon.cs](../src/QuanLyDienNuoc.Core/Models/HoaDon.cs) |
| Lập tờ hoàn, ghép tờ hoàn từ file vào hoá đơn gốc | [BaoCao/HoanHang.cs](../src/QuanLyDienNuoc.Core/BaoCao/HoanHang.cs) |
| Màn hình hoàn hàng | [Forms/HoanHangForm.cs](../src/QuanLyDienNuoc/Forms/HoanHangForm.cs) |
| Bản in | [Ui/InHoaDon.cs](../src/QuanLyDienNuoc/Ui/InHoaDon.cs) |
| Xuất / đọc Excel | [Excel/XuatHoaDon.cs](../src/QuanLyDienNuoc.Core/Excel/XuatHoaDon.cs), [Excel/DocHoaDon.cs](../src/QuanLyDienNuoc.Core/Excel/DocHoaDon.cs) |
| Màn hình nhập từ Excel | [Forms/NhapExcelForm.cs](../src/QuanLyDienNuoc/Forms/NhapExcelForm.cs) |
| Kiểm thử | [HoanHangTests.cs](../tests/QuanLyDienNuoc.Tests/HoanHangTests.cs) |
