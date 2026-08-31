# Nhập một khách hàng từ tờ hoá đơn của khách đó

Một tờ hoá đơn của cửa hàng là của **đúng một** khách: tên khách ghi ở đầu trang 1, các dòng
hàng nằm ở cả tờ. Nên nút **Nhập từ file** ở màn hình chính (cạnh *+ Thêm khách hàng*) không
phải là chỗ nhập cả danh sách khách — nó nhận một tờ hoá đơn và ghi vào sổ **một khách kèm hoá
đơn đầu tiên của khách ấy**.

Cách nhập danh sách nhiều khách theo file mẫu 4 cột đã bỏ hẳn: thứ cửa hàng thật sự có trên máy
là mấy tờ hoá đơn Excel, không phải một danh sách khách gõ sẵn.

Khách **đã có trong sổ** thì không đi đường này: mở *Đơn hàng của khách → Nhập từ Excel* để nhập
tờ vào sổ của người ấy — xem [nhap-hoa-don-nhieu-trang.md](nhap-hoa-don-nhieu-trang.md).

## Người dùng làm gì

1. Bấm **Nhập từ file** ở góc trên phải màn hình chính.
2. Chưa có tờ nào trên máy thì bấm **Tải file mẫu...**, chọn chỗ lưu. Phần mềm ghi ra **hai
   file** — `Mau-hoa-don-trang-1.xls` và `Mau-hoa-don-trang-sau.xls`. Đây chính là mẫu giấy cửa
   hàng đang dùng (bảng hàng đánh số thứ tự sẵn), không phải một mẫu khác nghĩ ra: điền vào rồi
   nhập lại là khớp đúng chỗ.
3. Bấm **+ Thêm trang...** và chọn **file trang 1 trước** — trang 1 là trang có *"Tên khách
   hàng"* ở đầu. Tờ dài nằm ở nhiều file thì bấm tiếp *+ Thêm trang...* cho từng trang sau.
4. Chọn **NĂM CỦA TỜ** — mặc định là năm sổ đang mở, bày sẵn từ năm sau đến 8 năm trước. Mẫu
   giấy in `năm 20.........` nên tờ điền tay thường không có năm.
5. Soát ô **TÊN KHÁCH HÀNG** / **ĐỊA CHỈ** đã điền sẵn từ giấy, thêm điện thoại và ghi chú nếu
   muốn, rồi bấm **NHẬP KHÁCH VÀ HOÁ ĐƠN**.

Lô trang hiện ở bảng *CÁC TRANG TRONG LÔ (theo thứ tự thêm vào · tích để lấy)*: bỏ tích một
trang là lô tính lại ngay — bỏ tích trang 1 thì mất tên khách, tích thêm trang 1 của tờ khác thì
bị chặn vì hai tờ khác nhau dồn vào một hoá đơn.

## Bảng lô: ô tích đứng cạnh tên file, bảng bên phải bày một file

Cột **LẤY** là ô tích đứng ngay trước cột **FILE**: thêm file vào rồi vẫn bỏ được, không phải
file nào đã thêm cũng bắt buộc phải nhập. Ô tích có bề ngang thấp nhất cố định — cột chia theo
tỷ lệ nên bảng chật là nó co lại còn một vạch, người dùng không biết là có chỗ tích.

Bấm vào dòng nào thì **bảng xem trước bên phải bày đúng file của dòng ấy**, kể cả file chưa
tích — đang cân nhắc có lấy file này không thì phải nhìn thấy nó có gì đã. Thêm file xong là màn
hình tự chuyển sang đứng ở file vừa thêm. Câu tóm tắt phía trên bảng nói cả hai con số —
*"ĐANG XEM to2.xls · 35 DÒNG · … — CẢ LÔ: SẼ NHẬP 1 KHÁCH · 65 DÒNG HÀNG · …"* — và nút **Xem cả
lô** đổi bảng sang bày toàn bộ những gì sắp vào sổ.

Cái vào sổ luôn là **cả lô đang tích**, không phải bảng đang bày: bấm Nhập lúc đang xem một file
vẫn nhập đủ các file khác.

## File có hai tab "mẫu cũ" và "mẫu mới" thì chỉ lấy tab "mẫu cũ"

File hoá đơn của cửa hàng hay để hai tab cạnh nhau: tab `mau cũ` là tờ đã điền cho khách, tab
`mẫu mới` là mẫu trắng còn sót mấy dòng ví dụ chép sẵn (bóng điện, bệt, công làm — file nào cũng
y hệt file nào). Lấy cả hai là sổ có thêm gần 4 triệu tiền hàng chẳng ai mua, nên
[`Excel/DocHoaDon.cs`](../src/QuanLyDienNuoc.Core/Excel/DocHoaDon.cs) bỏ tab `mẫu mới` khi trong
file có tab `mau cũ` đọc ra được dòng hàng.

Hai chỗ cố ý làm chặt tay:

- tab `mau cũ` **không ra dòng nào** thì giữ nguyên cả hai — có người điền vào tab kia, bỏ nốt là
  cả file trắng trơn mà màn hình không nói được vì sao;
- chỉ xét **đúng hai cái tên ấy** (bỏ dấu, gộp khoảng trắng). Có file cửa hàng đặt tên tab là
  `mau hoa don cũ` / `mẫu hoá đơn mối` mà tờ đã điền lại nằm ở tab "mối" — so lỏng tay là vứt
  đúng tờ của khách.

## Tên và địa chỉ: đọc trên giấy nhưng sửa được

Tên và địa chỉ lấy ở phần đầu **trang 1** (các trang sau không có phần đầu nên chẳng có gì để
lấy). Điền sẵn vào ô để soát, và tự tay sửa thì lần tính lại sau không đè lên chữ vừa gõ.

Những ô **không** được nhận là tên khách — ghi vào sổ là một khách rác không ai nhận ra:

- chỗ để trống in sẵn của tờ giấy: `Tên khách hàng: .....`, `.........`;
- ô có nhãn đầu dòng: `ĐC: ...`, `Kính gửi: ...` (dấu hai chấm nằm trong 25 ký tự đầu);
- nhãn bảng hàng và dòng chốt tờ: `TT`, `TÊN HÀNG`, `ĐVT`, `SỐ LƯỢNG`, `ĐƠN GIÁ`, `THÀNH TIỀN`,
  `Tổng cộng`, `Tiền bằng chữ`, `Người mua hàng`, `Người bán hàng` (so sau khi bỏ dấu);
- chuỗi ngắn hơn 2 ký tự.

Không đọc được tên thì phần mềm **nói rõ phải gõ tay**, chứ không đoán: *"Trang 1 để trống chỗ
Tên khách hàng nên không đọc được tên"*, hoặc *"Lô chưa có trang 1 nên không đọc được tên
khách"*. Chưa có tên thì nút nhập vẫn khoá.

## Chỗ chặn và chỗ nhắc

**Chặn** (dải đỏ, không nhập được):

| Chặn | Vì sao |
| --- | --- |
| **Trang 1 nằm sau** một trang nối tiếp | thứ tự trang đảo thì hàng vào sổ lệch trang, mà trên sổ không còn dấu vết trang nào để dò lại — xét trong [`Excel/ThuTuTrangGiay.cs`](../src/QuanLyDienNuoc.Core/Excel/ThuTuTrangGiay.cs), dùng chung với màn nhập hoá đơn |
| Lô có **hai trang 1** (hai tờ của hai lượt mua) | mỗi lượt nhập chỉ ra một khách, dồn cả hai là một hoá đơn sai |
| Lô có **tờ hoàn hàng** | tờ hoàn là hoàn cho một hoá đơn đã có, nhập cùng lúc với khách mới thì nợ thành số âm mà chẳng có hoá đơn nào để đối chiếu |

**Nhắc** (dải cam, vẫn nhập được — sai chỗ nào cũng là hàng vào sổ người khác hoặc vào năm khác,
mà trên sổ không còn dấu vết để dò lại):

- lô **chưa có trang 1** (chỉ có trang nối tiếp) nên không đọc được tên — vẫn nhập được, tên gõ
  tay;
- trong sổ đã có khách trùng tên (so không dấu) — nhập nữa là một người thành hai khách, công nợ
  chia đôi; bấm Nhập thì còn hỏi lại một lần nữa;
- tên đang gõ trông không giống tên khách;
- giấy ghi rõ năm mà khác **NĂM CỦA TỜ** đang chọn (năm chọn thắng, nhưng phải nói ra);
- số dòng cần xem lại do bộ đọc chấm (thiếu đơn giá đã tự tính từ thành tiền, thiếu tên hàng…).

## Ngày và năm của hoá đơn

Phần đọc file dùng chung [`Excel/DocHoaDon.cs`](../src/QuanLyDienNuoc.Core/Excel/DocHoaDon.cs)
với màn nhập hoá đơn, nên mốc ngày viết ở cột số thứ tự (`1/12`, `12\4`) có hiệu lực y như ở đó:
dòng nào nằm dưới mốc thì mang ngày của mốc, dòng không có mốc mới lấy ngày chung của lô.

Ngày chung lấy theo dòng *"Ngày … tháng …"* ở chân tờ, không có thì lấy mốc ngày đầu tiên đọc
được. Năm luôn lấy từ ô **NĂM CỦA TỜ**, và hoá đơn thuộc năm ấy chứ không phải năm sổ đang mở —
nhập tờ của năm trước thì màn hình chính tự chuyển sổ sang năm đó, không thì bấm vào khách mới
lại thấy trống trơn.

## Vào sổ một lượt

Khách và hoá đơn ghi trong **cùng một việc** của nhật ký (`Nhập khách <tên> và N dòng hàng từ
file`), nên `Ctrl+Z` một lần là bỏ cả hai — không để lại một khách rỗng không có hoá đơn nào.

## Kiểm thử

[`tests/QuanLyDienNuoc.Tests/NhapKhachTuToTests.cs`](../tests/QuanLyDienNuoc.Tests/NhapKhachTuToTests.cs)
— tờ đã điền lấy đúng tên và địa chỉ ở trang 1; tờ dài hai trang vẫn chỉ ra một khách; thứ tự
trang sai và lô có tờ hoàn thì chặn; lô thiếu trang 1 hoặc trang 1 bỏ trống chỗ tên thì nhắc gõ
tay; nhãn tờ giấy và chỗ để trống in sẵn không thành tên khách; trùng tên khách đã có thì chỉ ra
đúng khách cũ; xuất hai file mẫu rồi điền vào và nhập lại ra đúng một khách kèm dòng hàng, còn
file mẫu chưa điền gì thì không ra khách nào.

Kèm test hồi quy chạy trên **chính file hoá đơn thật** của cửa hàng (bản ẩn danh `to1.xls`): file
này có hai sheet đều là trang 1, tức hai tờ của hai lượt mua khác nhau, và phải bị chặn.

[`tests/QuanLyDienNuoc.Tests/HoaDonExcelTests.cs`](../tests/QuanLyDienNuoc.Tests/HoaDonExcelTests.cs)
giữ ba test cho chuyện hai tab mẫu: `to2.xls` chỉ ra tab `mau cũ`; tab `mau cũ` rỗng thì vẫn lấy
tab `mẫu mới`; tên tab kiểu `mẫu hoá đơn mối` thì giữ nguyên cả hai.
