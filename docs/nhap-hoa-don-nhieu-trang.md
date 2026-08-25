# Nhập hoá đơn từ Excel: một tờ nằm ở nhiều file

Mẫu giấy của cửa hàng để trang đầu và các trang sau ở **hai file riêng**:

| File mẫu | Sheet | Bố cục |
| --- | --- | --- |
| [`trang-1.xls`](../src/QuanLyDienNuoc/MauHoaDon/trang-1.xls) | `mau hoa don cũ` | r0–r2 tên/ĐC/ĐT cửa hàng · **r3 `Tên khách hàng:`** · r4 `Địa chỉ:` · r5 trống (tên tờ hoàn) · **r6 tiêu đề bảng** · r7–r31 hàng · r32 TỔNG CỘNG |
| [`trang-sau.xls`](../src/QuanLyDienNuoc/MauHoaDon/trang-sau.xls) | `Trang sau` | **r0 tiêu đề bảng** · r1–r35 hàng · r36 tổng · r40 `Ngày … tháng … năm` |

Một tờ hoá đơn dài vì thế nằm ở nhiều file, nên màn hình **Nhập hoá đơn từ Excel** gom chúng
thành một **lô**: thêm trang 1 trước, rồi thêm tiếp từng trang sau. Thứ tự thêm vào là thứ tự
trang.

## Người dùng làm gì

1. Ở màn *Đơn hàng của khách*, bấm **Nhập từ Excel**, chọn **file trang 1**.
2. Màn hình mở ra, bảng bên trái là lô: mỗi dòng một trang, cột **TRANG** đánh số 1, 2, 3…
3. Bấm **+ Thêm trang...** cho từng file trang sau. Trang mới nối vào cuối lô.
4. Chọn **NĂM CỦA TỜ** (xem [Vì sao phải chọn năm](#vì-sao-phải-chọn-năm)).
5. Soát bảng xem trước bên phải — có cột **NGÀY** của từng dòng — rồi bấm **NHẬP VÀO HOÁ ĐƠN**.

Chọn nhầm file thì chọn dòng đó trong lô và bấm **Bỏ trang này**, không phải mở lại màn hình.
Thêm hai lần cùng một file thì phần mềm hỏi lại: hàng vào sổ hai lần thì trên sổ không còn dấu
vết nào để nhận ra.

## Trang 1 và trang sau khác nhau ở đâu

Cài trong [`Excel/DocHoaDon.cs`](../src/QuanLyDienNuoc.Core/Excel/DocHoaDon.cs), xét theo **dòng
tiêu đề bảng nằm ở đâu**:

- tiêu đề ở dòng 0 → **trang sau**: không có phần đầu, nên không có tên khách;
- tiêu đề ở dòng nào khác → **trang 1**: phía trên nó là phần đầu, đọc được `Tên khách hàng:`.

Tên khách của cả tờ lấy ở **trang 1**, không phải "trang nào có thì lấy" — các trang sau không
có phần đầu nên chẳng có gì để lấy.

## Thứ tự trang phải đúng

[`Excel/ThuTuTrangGiay.cs`](../src/QuanLyDienNuoc.Core/Excel/ThuTuTrangGiay.cs) xét nhóm trang
**đang tích** (bỏ tích một trang là nó không tính nữa):

| Tình trạng lô | Xử lý |
| --- | --- |
| Trang 1 đứng đầu, dưới là các trang sau | nhập được |
| Trang 1 nằm sau một trang nối tiếp | **chặn** — hàng vào sổ lệch trang mà trên sổ không còn dấu vết trang nào để dò lại |
| Lô có hai trang 1 | **chặn** — đó là hai tờ hoá đơn khác nhau, dồn vào một hoá đơn là sai |
| Lô chỉ có trang sau | nhập được, kèm dải nhắc màu cam là không đọc được tên khách |

File `to1.xls` của cửa hàng có **hai** sheet đều là trang 1 (hai tờ khác nhau trong cùng file),
nên thêm file đó vào lô là gặp câu chặn "lô đang có 2 trang 1" — bỏ tích một tờ là nhập được.

Tên khách đọc ở trang 1 mà khác khách đang mở thì hiện dải cam nói rõ hai tên: màn hình này nhập
vào sổ của **khách đang mở**, lấy nhầm tờ là nợ sang tên người khác.

## Chọn lọc dòng, không lấy cả bảng

Trước đây cứ có tên hàng là lấy, quét đến khi gặp chữ "TỔNG CỘNG". Ba chỗ hỏng vì vậy:

**1. Dòng tổng không có nhãn.** `to2.xls` sheet `mau cũ` dòng 37 chỉ có số tiền ở cột THÀNH
TIỀN, ô đầu dòng gộp lại và để trống — không có chữ "TỔNG CỘNG" nào để dừng. Đây cũng đúng chỗ
mẫu mới ghi **tiền cộng sang từ tờ trước**: `to2.xls` sheet `mẫu mới` dòng 7 mang 2.507.900 —
đúng bằng tổng của sheet `mau cũ` cùng file. Lấy dòng đó vào là sinh một mặt hàng không tên và
cộng tiền của tờ trước thêm một lần nữa. Nay **thấy dòng chỉ có tiền ở cột thành tiền là hết
bảng**.

**2. Mẫu in sẵn số thứ tự.** Mẫu trang 1 in trước số 1..26 và công thức thành tiền ra 0 cho cả
trang, nên "có chữ ở cột TT" không có nghĩa là có hàng. Nay **ba dòng trống liền nhau là hết
bảng** — file mẫu chưa điền gì thì đọc ra 0 dòng, không phải 25 mặt hàng rỗng.

**3. Dòng có số lượng mà thiếu tên hàng.** `to2.xls` sheet `mau cũ` các dòng 7, 17, 19 có số
lượng và đơn vị mà bỏ trống tên hàng. Trước đây bỏ im, tức là mất hàng mà không ai biết. Nay
**vẫn lấy** và ghi vào dải cảnh báo `Dòng 7: có số lượng mà thiếu tên hàng — điền tên trước khi
nhập`, để điền thẳng trên bảng xem trước.

Không cắt cứng theo sức chứa mẫu (25 dòng / 35 dòng): `to1.xls` có **32 dòng hàng liền nhau**
trên một trang, cắt ở 25 là mất 7 dòng thật.

## Ngày của từng dòng: mốc ngày ở cột số thứ tự

Chủ cửa hàng hay viết ngày vào **cột số thứ tự** thay cho con số — `1/12`, `12\4`, `5-11` — rồi
các dòng từ đó xuống là hàng lấy hôm ấy, đến khi gặp mốc khác. Trên tờ giấy không có chỗ nào
khác ghi ngày cho từng dòng.

- Nhận `d/m`, `d\m`, `d-m`. **Không** nhận dấu chấm, để `1.5` không thành ngày 1 tháng 5.
- Số thứ tự viết lạ (`13 .`, `3 2`) hay ngày vô lý (`32/13`) thì để yên, vẫn là số thứ tự.
- Excel có thể đã tự đổi ô đó thành ô ngày thật, nên đọc cả ô ngày lẫn ô chữ.
- Mốc đứng riêng một dòng (dòng đó không có hàng) vẫn có hiệu lực cho các dòng dưới.
- Dòng nào không nằm dưới mốc nào thì lấy **NGÀY LẤY HÀNG** đặt chung cho cả lô.

**Xuất Excel ghi mốc ngày y như vậy** ([`Excel/XuatHoaDon.cs`](../src/QuanLyDienNuoc.Core/Excel/XuatHoaDon.cs)),
nhưng **mốc đứng riêng một dòng**, không ghi đè lên số thứ tự của dòng hàng: đổi ngày thì chèn một
dòng chỉ có `1/12` ở cột TT, hàng lấy hôm ấy nằm bên dưới và vẫn giữ số thứ tự của nó. Cách cũ ghi
mốc thẳng vào ô số thứ tự của dòng hàng, nên tờ của khách mối — mỗi ngày lấy một ít — có gần như cả
cột TT là ngày, chẳng còn số thứ tự nào để soát.

Dòng mốc **ăn một dòng của trang** y như một dòng hàng, nên trang 1 gom hàng của một ngày chỉ còn
24 dòng hàng (không phải 25), trang sau còn 34. Chỗ này tính trong `XuatHoaDon.LenTrang` và cả bản
in dùng chung, để tờ in ra giấy và file Excel xuất ra ngắt trang giống nhau.

**Dòng đầu mỗi trang luôn có mốc**, kể cả khi cùng ngày với dòng cuối trang trước — mỗi trang là
một file riêng và nhập vào từng lần, trang nào không tự mang ngày của nó thì nhập riêng trang đó ra
là mất ngày.

## Xuất ra: mỗi trang một file riêng

Mẫu giấy của cửa hàng vốn là **hai file rời** (`trang-1.xls`, `trang-sau.xls`) và màn nhập cũng gom
từng file trang thành một lô, nên xuất Excel cũng ra **rời từng trang**: tờ ba trang là ba file, chứ
không gộp ba tab vào một file — tab thì máy in bỏ qua và người dùng cũng không thấy.

- Tờ **một trang** giữ đúng tên người dùng đặt trong hộp thoại lưu.
- Tờ **nhiều trang** thì mỗi file thêm ` - trang N`: `Hoa don anh Dung - trang 1.xls`,
  `... - trang 2.xls`. Số trang nằm trong tên nên xếp trong thư mục đúng thứ tự, và nhập lại cũng
  theo thứ tự ấy.
- Màn *Đơn hàng của khách → Xuất Excel* nói rõ đã ghi mấy file, tên từng file và thư mục chứa,
  rồi hỏi có mở trang 1 lên xem không.

## Vì sao phải chọn năm

Mẫu giấy in sẵn `Ngày ......... tháng ......... năm 20.........`, nên tờ điền tay thường chỉ có
ngày và tháng. Mốc ngày ở cột số thứ tự (`1/12`) cũng không có năm. Vì vậy màn hình nhập có ô
**NĂM CỦA TỜ**, mặc định là năm của sổ đang mở, bày sẵn từ năm sau đến 8 năm trước — hoá đơn cũ
của cửa hàng là giấy của mấy năm trước.

- Ngày/tháng vẫn đọc trong file, chỉ ghép năm đã chọn vào.
- Đổi ô năm là ngày của cả lô đổi theo, không phải nhập lại file.
- Tờ nào ghi rõ đủ bốn chữ số năm mà khác năm đang chọn thì hiện dải cam
  `Giấy ghi năm 2020 mà ô NĂM CỦA TỜ đang chọn 2026` — **năm chọn vẫn thắng**, nhưng phải nói ra.
- Ngày mở của hoá đơn mới lấy ngày sớm nhất trong lô.

## Chữ tiếng Việt dạng tổ hợp

Có tờ hoá đơn thật của cửa hàng lưu chữ Việt ở **dạng tổ hợp**: `Ngày` là `N`, `g`, `a` rồi ký tự
dấu huyền rời ra. Trông y hệt chữ thường nên không ai nghĩ là khác, mà so từng ký tự thì trượt
hết — dòng `Ngày … tháng … năm …` ở chân tờ `to1.xls` vì vậy chưa bao giờ đọc được. Nay mọi ô chữ
đọc lên đều dồn về một dạng ngay tại
[`DocHoaDon.LayChu`](../src/QuanLyDienNuoc.Core/Excel/DocHoaDon.cs).

> Phần nhập danh sách khách hàng ([`Excel/NhapKhachHang.cs`](../src/QuanLyDienNuoc.Core/Excel/NhapKhachHang.cs))
> chưa dồn dạng chữ như vậy. Chỗ dò tiêu đề ở đó so chữ đã bỏ dấu nên không hỏng, nhưng tên khách
> đọc từ file dạng tổ hợp sẽ vào sổ ở dạng đó.

## Kiểm thử

[`tests/QuanLyDienNuoc.Tests/NhapNhieuTrangTests.cs`](../tests/QuanLyDienNuoc.Tests/NhapNhieuTrangTests.cs)
— nhận trang 1 / trang sau (cả trên file thật `to1.xls`, `to2.xls`), file mẫu chưa điền ra 0
dòng, dòng tổng không nhãn, dòng thiếu tên hàng, ba dòng trống liền, giữ đúng 32 dòng của
`to1.xls`, ghép năm đã chọn, giữ lại năm ghi trên giấy, chữ dạng tổ hợp, mốc ngày ở cột số thứ
tự (kể cả mốc đứng riêng và số thứ tự viết lạ), xuất rồi đọc lại giữ nguyên ngày từng dòng kể cả
qua chỗ sang trang, và bốn cách xếp thứ tự trang trong lô.
