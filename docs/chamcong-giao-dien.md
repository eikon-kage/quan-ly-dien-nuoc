# App chấm công — nguyên tắc giao diện

Người dùng app này là **chủ cửa hàng, không rành công nghệ**, bấm ngoài công trình hoặc
ngoài sân. Mọi quyết định giao diện dưới đây đều xuất phát từ đó.

> **Đã điều chỉnh một lần.** Bản đầu làm chữ rất to (tên thợ 26pt) và nút rất cao (ô chấm
> 72pt) vì người dùng có tuổi. Nhìn trên máy thật thì nặng nề và thô, nên chủ dự án yêu cầu
> hạ xuống cho hài hoà. Cỡ hiện tại vừa phải, nhưng **vẫn lớn hơn app thông thường** — đừng
> hạ tiếp mà không hỏi.

## Bộ giao diện lấy từ đâu

Hình khối và bảng màu lấy theo bộ **HR Attendance App UI Kit** trên Figma
([file](https://www.figma.com/design/zQRM6yWIqGW17ctO7jtB3E/HR-Attendance-App-UI-Kit---Community--Community-?node-id=113-7674),
trang *🎨 Design*, chế độ sáng). Ba mảnh nhận ra ngay:

- **Thẻ trắng bo 16, tách nhau bằng một vệt bóng rất loãng**, không có nét viền nào. Cả app
  trước đây là lưới thẻ kẻ viền xám — bỏ viền đi thì nhẹ hẳn.
- **Ô tóm tắt nền màu 5% viền màu tươi**, nhãn nhỏ ở trên, con số to ở dưới. Xếp 2×2 thành
  lưới, dùng ở màn hình chi tiết thợ.
- **Thanh phân đoạn có viên trượt xanh**, thay cho mấy nút viền rời nhau.
- **Đầu trang không còn dải trắng kẻ viền dưới**: tiêu đề nằm thẳng trên nền trang, căn trái,
  nút bấm dồn sang phải.

Màu, font, cỡ và bóng khai ở [thietKe.ts](../mobile/src/giaodien/thietKe.ts); mấy mảnh lặp lại
ở nhiều màn hình (đầu trang, ô tóm tắt, thanh phân đoạn, chip, thẻ trắng) ở
[ThanhPhan.tsx](../mobile/src/giaodien/ThanhPhan.tsx). Sửa hai file ấy là cả bốn màn hình đổi
theo — đừng gõ mã màu hay số bo góc thẳng vào từng màn hình.

Chỉ lấy phần **hình dáng**. Hai chỗ cố ý làm khác bản thiết kế, đều vì người dùng app này:

1. **Thanh tab dưới vẫn có chữ.** Bản thiết kế để thanh tab chỉ có icon, kèm một nút tròn nổi
   ở giữa. Không lấy — xem điều 8 dưới đây.
2. **Màu dùng để viết chữ đều đậm hơn bản thiết kế một nấc.** Xem mục *Màu*.

## Chín điều bắt buộc

1. **Mở app là chấm được ngay.** Không đăng nhập, không màn hình chào, không hướng dẫn.
   Màn hình đầu tiên luôn là chấm công của *hôm nay*.
2. **Chữ vừa mắt, không nhỏ.** Tên thợ 19pt, chữ trên nút 15pt, chữ phụ 13pt. Vẫn to hơn
   app thông thường vì người dùng có tuổi.
3. **Nét chữ nhiều nhất là 600 (SemiBold).** Không dùng 700 — ở cỡ lớn nhìn nặng và thô.
4. **Nút cao 48pt, ô chấm 56pt.** Apple khuyên tối thiểu 44pt; đây là thứ bấm hằng ngày nên
   rộng hơn một chút. Đó là mức **tối thiểu** chứ không phải cố định — xem *Cỡ chữ hệ thống*
   bên dưới.
5. **Không có cử chỉ ẩn.** Không vuốt để xoá, không nhấn giữ để hiện menu, không lắc để
   hoàn tác. Muốn làm gì cũng phải có một cái nút nhìn thấy được.
6. **Không dùng riêng màu để báo trạng thái.** Ô đã chấm đổi cả ba thứ cùng lúc: nền sang
   xanh nhạt, vòng tròn thành dấu tích, chữ từ xám sang đậm.
7. **Bấm nhầm sửa được bằng đúng thao tác vừa rồi.** Chạm ô đang xanh là bỏ chấm. Không cần
   tìm nút xoá, không cần hộp thoại hỏi lại.
8. **Icon luôn đi kèm chữ.** Không có nút nào chỉ có hình — trừ mũi tên đổi tháng ở Bảng
   lương và nút xoá một mốc lương, hai chỗ đã quá quen mặt. Riêng mũi tên ở màn hình chấm
   công có chữ *Tuần* đi kèm vì nó nhảy bảy ngày một lần, không đoán được nếu chỉ có hình.
   Dùng bộ **Feather** (`@expo/vector-icons`): nét mảnh, hợp với tổng thể nhẹ nhàng.
9. **Số tiền viết đủ chữ số**: `1.500.000 đ`, không viết tắt `1,5tr`. Ngày viết `03/08`
   kèm thứ, không viết `3/8`.

> **Không rung.** Bản đầu rung nhẹ mỗi lần chạm trúng, lấy lý do "ngoài nắng nhìn không rõ
> thì có phản hồi ở tay cho yên tâm". Chủ dự án bấm thử trên máy thật rồi yêu cầu bỏ: bấm
> liên tục mười mấy ô một lượt thì rung thành ra rối tay chứ không giúp gì. Gói
> `expo-haptics` đã bỏ khỏi app. Đừng thêm lại mà không hỏi.

## Bốn màn hình, không hơn

Thanh dưới có bốn mục: **Chấm công · Bảng lương · Kỳ đã chốt · Thợ**. Mỗi mục thêm vào là
một chỗ để người dùng lạc — đừng thêm mục thứ năm.

> **Trước đây chỉ có ba mục.** Mục *Kỳ đã chốt* thêm vào cùng lúc với quyết toán. Đã cân
> nhắc nhét sổ cũ vào ngay trong Bảng lương, nhưng bỏ: ba mục đầu là chỗ **làm việc hằng
> ngày**, mục thứ tư là chỗ **tra sổ cũ**. Hai việc khác nhau, gộp lại thì màn hình dùng
> mỗi ngày bị sổ của mấy tháng trước chen chỗ.

Thanh tab này **tự vẽ** ([App.tsx](../mobile/App.tsx)) chứ không dùng thanh mặc định của iOS.
Thanh mặc định chữ khoảng 10pt và chủ yếu là hình — người có tuổi không đọc ra. Bản tự vẽ
cao ít nhất 58pt, icon 20pt **kèm chữ 13pt**, mục đang chọn có một vạch xanh ngắn ngay trên
đầu icon rồi cả icon lẫn chữ chuyển xanh.

Nền trắng bo hai góc trên, nổi lên bằng bóng chứ không bằng đường kẻ ngang — dáng thanh tab
của bản thiết kế. Vạch xanh **giữ chỗ sẵn cả khi không chọn** (màu trong suốt), kẻo bấm sang
mục khác thì cả hàng nhích lên một nhịp.

Bản thiết kế bỏ hẳn chữ và nhét thêm một nút tròn nổi ở giữa thanh. Không lấy: người dùng app
này có tuổi, nhìn bốn cái hình trơ trọi không đoán ra mục nào là mục nào (điều 8), còn nút tròn
giữa thanh thì không biết nó là mục thứ năm hay là một việc riêng.

Hai hộp thoại — chọn nửa công / công rưỡi ([HopChon.tsx](../mobile/src/giaodien/HopChon.tsx))
và nhập tiền ứng ([HopNhapSo.tsx](../mobile/src/giaodien/HopNhapSo.tsx)) — cũng tự vẽ. Ban
đầu dùng `ActionSheetIOS` và `Alert.prompt` của hệ điều hành, nhưng bỏ vì hai lẽ: chúng
**chỉ có trên iOS**, và chữ trong đó không ép to được. Tự vẽ thì nút cao 60pt, chữ 20pt,
giống hệt phần còn lại của app.

Nhờ vậy app chạy được cả trên Android, tiện lúc muốn thử nhanh trên máy Android có sẵn.

### 1. Chấm công (màn hình chính)

```
┌────────────────────────────────────┐
│  Thứ Tư 05/08          ┌────┐┌────┐│   ngày đang xem, 19pt đậm, căn trái
│  [ Hôm nay ]           │ ‹  ││ ›  ││   hai nút đổi tuần dồn sang phải
│                        │Tuần││Tuần││   nút Hôm nay chỉ hiện khi xem ngày khác
│                        └────┘└────┘│
│ ┌───┐┌───┐┌───┐┌───┐┌───┐┌───┐┌───┐│
│ │ T2││ T3││Nay││ T5││ T6││ T7││ CN││   dải bảy ngày, chạm là sang ngày đó
│ │ 03││ 04││ 05││ 06││ 07││ 08││ 09││
│ │  4││  2││  ·││  ·││  ·││  ·││  ·││   số công cả tổ đã chấm ngày đó
│ └───┘└───┘└───┘└───┘└───┘└───┘└───┘│
│                                    │
│  [    Cả tổ đi đủ cả ngày     ]    │   nút xanh, cao 48pt
│                                    │
│ ╭────────────────────────────────╮ │   mỗi thợ một thẻ trắng bo 16,
│ │ Anh Tuấn                [Sửa]  │ │   nổi lên bằng bóng, không viền
│ │ ┌──────────────┐┌────────────┐ │ │
│ │ │  SÁNG     ✓  ││ CHIỀU   ✓  │ │ │   cao 56pt
│ │ └──────────────┘└────────────┘ │ │
│ ╰────────────────────────────────╯ │
│ ╭────────────────────────────────╮ │
│ │ Anh Bình                [Sửa]  │ │
│ │ ┌──────────────┐┌────────────┐ │ │
│ │ │  SÁNG     ✓  ││ CHIỀU      │ │ │
│ │ └──────────────┘└────────────┘ │ │
│ ╰────────────────────────────────╯ │
│           Hôm nay: 3 công          │   dòng tổng, nằm trên nền trang
└────────────────────────────────────┘
```

Mỗi thợ là một thẻ riêng chứ không phải một dòng trong bảng. Nhồi tên và hai ô vào cùng một
dòng thì với cỡ chữ 19pt là chật, chữ bị cắt.

Cả màn hình giờ **chỉ còn một nền**: đầu trang, dải ngày và dòng tổng nằm thẳng trên nền trang,
không còn hai dải trắng kẻ viền kẹp trên kẹp dưới. Thứ duy nhất còn là mảng trắng là thanh tab
ở đáy — thêm một dải trắng nữa ngay trên nó thì thành hai tầng bóng chồng nhau.

**Dải bảy ngày** dưới đầu trang là chỗ đổi ngày. Bản đầu chỉ có mỗi ngày đang xem với hai
mũi tên lùi / tới *một ngày* — nhìn không ra được ngày nào đã chấm, mà muốn quay lại đầu
tuần thì bấm bốn năm lần. Dải này chữa cả hai:

- **Cả tuần hiện cùng lúc**, chạm một cái là sang đúng ngày cần, không phải bấm nhiều lần.
- **Ngày nào chấm rồi thì thấy ngay** vì dưới mỗi ngày có số công cả tổ; ngày chưa chấm để
  dấu `·` mờ. Chưa cần mở ngày ra vẫn biết hôm kia quên chấm.
- **Hôm nay ghi hẳn chữ "Nay"** thay cho thứ, ô có viền xanh — khỏi phải nhớ hôm nay thứ mấy.
  Ngày đang xem tô nền xanh đặc, chữ trắng.
- **Ngày chưa tới thì mờ đi** nhưng vẫn chấm được, phòng khi cần chấm trước.

Tuần bắt đầu từ Thứ Hai, giống lịch treo tường và giống tờ lịch trong màn hình báo cáo thợ.

Hai mũi tên `‹ ›` giờ nhảy **cả tuần** chứ không phải một ngày — một ngày đã có dải lo rồi.
Vì đổi việc nên chúng phải có thêm chữ *Tuần* bên dưới: mũi tên trơ trọi thì người dùng
không đoán được nó nhảy một ngày hay bảy ngày.

Chúng cũng **không còn kẹp hai bên ngày** mà dồn cả sang phải, thành hai thẻ trắng nổi bóng —
đúng chỗ đặt nút của đầu trang trong bản thiết kế. Ngày được cả bề ngang bên trái nên tên thứ
dài (`Thứ Tư 05/08`) không còn bị bó giữa hai mũi tên.

**Nút "Cả tổ đi đủ cả ngày"** là chỗ nhanh nhất: bình thường cả tổ đi đủ, bấm một cái xong,
rồi bỏ chấm vài người nghỉ. Nhanh hơn nhiều so với bấm 16 ô. Khi cả tổ đã đủ công thì nút
đổi thành **"Xoá hết chấm hôm nay"** viền đỏ.

**Nút `[Sửa]`** mở ô chọn nửa công / công rưỡi cho từng buổi. Để riêng ra vì chín trên mười
lần là một công tròn — không được bắt người dùng đi qua bước này mỗi ngày. Buổi nào khác 1
công thì ô đó hiện thêm `½` hoặc `1½` để nhìn là biết.

### 2. Bảng lương

Màn hình này luôn hiện đúng **kỳ đang mở**: từ sau lần quyết toán trước tới hôm nay. Mỗi thợ
một thẻ: tổng công, tiền công, đã ứng, **còn phải trả** in to nhất và đậm nhất — đó là con số
anh cần khi móc ví. Ứng quá thì số âm và in đỏ.

> **Trước đây màn hình này xem theo tháng**, đổi tháng bằng hai mũi tên `‹ ›`. Bỏ đi vì tiền
> công ngoài công trình không chạy theo tháng: xong việc là trả, có khi mười ngày, có khi sáu
> tuần. Bây giờ chỉ có đúng một kỳ đang mở nên không còn gì để đổi qua đổi lại — hai mũi tên
> mất luôn. Muốn xem lại kỳ đã trả thì sang mục *Kỳ đã chốt*.

Kỳ trước trả thiếu thì phần thiếu hiện thành **một dòng riêng** *Nợ kỳ trước*, không cộng
thầm vào tiền công. Thợ hỏi *"sao kỳ này nhiều thế"* thì chỉ đúng vào dòng đó mà trả lời.

Nút **Ứng tiền** trên mỗi thẻ mở hộp nhập số tiền, kèm một ô **ghi chú không bắt buộc**
(*"ứng đổ xăng"*, *"ứng mua thuốc"*). Ghi chú hiện lại ở danh sách ứng tiền trong màn hình
chi tiết — vài tuần sau nhìn lại còn biết tiền ấy ứng vào việc gì. Không bắt điền: lúc vội
mà ép gõ thì người dùng gõ bừa một chữ cho xong, ghi chú thành vô nghĩa.

#### Xem chi tiết một thợ

Bấm cả thẻ là mở màn hình chi tiết cả kỳ — chỗ để tra khi thợ thắc mắc *"sao kỳ này ít tiền
thế"*. Trên cùng là **lưới 2×2 bốn con số tóm tắt**, dưới là **tờ lịch**
([LichCong.tsx](../mobile/src/giaodien/LichCong.tsx)), cuối cùng là các lần ứng tiền.

```
┌──────────────────────┐┌──────────────────────┐
│ Số công              ││ Tiền công            │   viền xanh dương / xanh ngọc
│ 4 công               ││ 1.200.000 đ          │
└──────────────────────┘└──────────────────────┘
┌──────────────────────┐┌──────────────────────┐
│ Đã ứng               ││ Còn phải trả         │   viền đỏ / xanh lá
│ −200.000 đ           ││ 1.000.000 đ          │
└──────────────────────┘└──────────────────────┘
```

Trước đây bốn số này là bốn dòng nhãn–số trong một thẻ trắng. Đọc thì ra, nhưng phải rà mắt
từng dòng. Lưới thì con số nằm to giữa ô, mỗi ô một màu — nhìn một cái là hết.

Ba điều đã cân nhắc ở lưới này:

1. **Ô *Đã ứng* hiện cả khi bằng `0 đ`**, khác bản cũ (bản cũ ẩn dòng ấy đi). Lưới 2×2 khuyết
   một góc nhìn như thiếu chỗ chứ không như *"không có gì"*.
2. **Ứng quá tiền công thì cả ô *Còn phải trả* chuyển sang đỏ**, không chỉ riêng con số — đúng
   điều 6, đổi cả nền lẫn viền lẫn màu chữ.
3. **Dòng *Nợ kỳ trước* không vào lưới**, nó nằm thành một thẻ riêng ngay dưới. Nó chỉ hiện khi
   có nợ, mà lưới thì phải luôn đủ bốn ô.

Kỳ chốt lúc nào cũng được nên nó hay **vắt qua hai tháng**. Lúc ấy mỗi tháng vẽ một tờ lịch
riêng xếp dọc, có tên tháng ở trên. Gộp hai tháng vào một tờ thì không còn là tờ lịch treo
tường nữa, mà chính hình dáng tờ lịch mới là thứ làm người xem nhìn ra ngay chỗ nghỉ nằm đâu.

##### Lọc theo khoảng ngày

Mở ra là trọn kỳ, nhưng chọn hẹp lại được — nhiều nhà trả một phần giữa chừng chứ không đợi
chốt kỳ, lúc ấy con số cần nhìn là của mấy ngày đó.

```
┌──────────────────────────────────────────┐
│  Từ [ 01/08 ]  →  Đến [ 15/08 ]          │   chạm là mở tờ lịch chọn ngày
│ ╔═══════╗                                │   thanh phân đoạn: viên xanh là
│ ║Cả kỳ  ║ Cả tháng  Nửa đầu   Nửa cuối   │   khoảng đang xem
│ ╚═══════╝                                │
└──────────────────────────────────────────┘
```

**Mục *Cả kỳ* luôn đứng đầu**: lỡ lọc hẹp rồi thì đó là đường về.

Trước đây bốn khoảng này là bốn nút viền rời nhau, khoảng đang dùng thì nút đổi màu. Giờ là
**một thanh phân đoạn có viên trượt** như bản thiết kế: nhìn ra ngay đúng một mục đang chọn,
thay vì phải soi xem nút nào đang khác màu. Chọn tay hai đầu ngày thành một khoảng không có
trong bốn mục thì **không viên nào sáng** — đúng vậy, lúc ấy đang xem một khoảng riêng.

Thanh phân đoạn chỉ sáng **một** viên, nên kỳ trùng đúng một tháng thì viên sáng là *Cả kỳ*,
viên đầu tiên khớp. Bản cũ cho cả *Cả kỳ* và *Cả tháng* cùng sáng; bỏ đi cũng không mất gì,
hai mục vẫn trỏ về cùng một khoảng.

Năm điều đã cân nhắc ở đây:

1. **Cả bốn con số tóm tắt, tờ lịch lẫn danh sách ứng tiền đều tính lại theo khoảng.** Lọc
   mà chỉ lọc một mục thì người xem cộng nhầm lúc nào không hay.
2. **Tờ lịch vẫn vẽ trọn tháng**, ngày ngoài khoảng thành ô trắng "chưa tính". Cắt tờ lịch
   cho vừa khoảng thì mất chỗ dựa của mắt — nhìn vào không biết đang ở đoạn nào của tháng.
3. **Hộp chọn ngày tự vẽ** ([HopChonNgay.tsx](../mobile/src/giaodien/HopChonNgay.tsx)) như tờ
   lịch, chạm một cái là xong, không có nút *Đồng ý*. Hộp của hệ điều hành là ba bánh xe quay
   chữ nhỏ, người có tuổi quay trượt tay, mà bản Android lại khác hẳn bản iOS.
4. **Chọn ngày đầu muộn hơn ngày cuối thì kéo luôn ngày cuối theo**, chứ không khoá ngày lại
   cho bấm không ăn. Người dùng chỉ gặp khoảng hợp lệ, không bao giờ vào ngõ cụt.
5. **Lọc hẹp thì bỏ dòng *Nợ kỳ trước* ra.** Món nợ ấy thuộc về cả kỳ, không thuộc riêng mấy
   ngày đang xem; cộng vào thì con số dưới đáy chẳng còn nghĩa gì.

Đầu trang ghi `Cả kỳ · 01/08 → 31/08` khi đang xem trọn kỳ, ghi `01/08 → 15/08` khi đã lọc —
nhìn một chỗ là biết con số bên dưới đang tính cho đoạn nào.

Tháng 8/2026, thợ đi đều cả tháng, riêng ngày 13 chỉ đi nửa buổi, xem vào ngày 27:

```
┌──────────────────────────────────────────────┐
│  T2     T3     T4     T5     T6     T7   CN  │
│                                     1     2  │   1, 2: nghỉ — ô xám
│  3 ✓   4 ✓    5 ✓    6 ✓    7 ✓     8     9  │
│ 10 ✓  11 ✓   12 ✓   13 ✓0,5 14 ✓   15    16  │   13: đi thiếu công
│ 17 ✓  18 ✓   19 ✓   20 ✓   21 ✓    22    23  │
│ 24 ✓  25 ✓   26 ✓   27      28     29    30  │   28-31: chưa tới
│ 31                                           │   nên để trắng
├──────────────────────────────────────────────┤
│ [✓] Đi làm 18 ngày      [ ] Nghỉ 9 ngày      │
└──────────────────────────────────────────────┘
```

Trước đây chỗ này là **hai danh sách xếp dọc** — ngày đi làm, rồi ngày nghỉ. Đọc thì ra,
nhưng phải rà mắt hết cả cột mới biết tháng này nghỉ dày hay thưa, mà đó mới là điều người
xem muốn biết. Nhìn tờ lịch là thấy ngay khoảng trống nằm ở đâu, đúng như nhìn tờ lịch treo
tường có khoanh bút chì.

Bốn điều đã cân nhắc, đừng đổi mà không đọc lại:

1. **Tuần bắt đầu từ Thứ Hai**, giống lịch bán ngoài hàng, không bắt đầu từ Chủ Nhật kiểu Mỹ.
2. **Ba trạng thái ô, mỗi trạng thái khác cả nền lẫn viền lẫn dấu bên trong** — đúng điều 6:
   *đi làm* nền xanh nhạt, viền xanh, có dấu tích; *nghỉ* nền xám nhạt, viền xám, để trống;
   *chưa tính* không nền không viền, số mờ đi. Ô "chưa tính" là ngày chưa tới hoặc ngày thợ
   chưa vào làm — để trắng chứ không tô xám, kẻo mở lịch đầu tháng thấy báo nghỉ gần trọn
   tháng thì hoảng.
3. **Đi đủ cả ngày (2 công) chỉ có dấu tích, không ghi số.** Đi đủ là chuyện thường ngày;
   ghi số vào thì cả tháng chi chít, mắt không bắt được ngày nào khác thường. Chỉ ngày lệch
   khỏi 2 công mới ghi thêm `0,5`, `1`, `2,5` bên cạnh dấu tích.
4. **Chú thích nằm ngay dưới lịch và kiêm luôn chỗ đếm ngày** — "Đi làm 21 ngày · Nghỉ 6
   ngày". Vừa khỏi phải đoán ô xanh nghĩa là gì, vừa khỏi ngồi đếm ô.

Đổi lại, **tiền của từng ngày không còn hiện** như hồi làm danh sách — ô lịch không đủ chỗ.
Số đó suy ra được từ số công nhân đơn giá, mà tổng tiền công thì vẫn nằm ngay trên đầu.

Mỗi ô có nhãn cho trình đọc màn hình dạng `03/08 Thứ Hai, đi làm 2 công`, vì bản thân ô chỉ
là một con số với một dấu tích, đọc trơn lên thì không rõ nghĩa.

#### Quyết toán kỳ

Nút **Quyết toán kỳ này** nằm ở chân màn hình Bảng lương, ngay dưới dòng tổng. Bấm không chốt
luôn mà **mở ra màn hình đếm tiền** — chốt kỳ là việc nặng nhất trong app, phải nhìn thấy
từng người bao nhiêu trước khi gật đầu.

```
┌────────────────────────────────────┐
│  ‹      Quyết toán kỳ              │
│         03/08 → 05/08              │
├────────────────────────────────────┤
│  Anh Tuấn                  2 công  │
│  Tiền công            600.000 đ    │
│  Đã ứng              −200.000 đ    │
│  ────────────────────────────────  │
│  Phải trả             400.000 đ    │
│ ┌────────────────────────────────┐ │
│ │ Thực trả        400.000 đ   ✎  │ │   điền sẵn, chạm để sửa
│ └────────────────────────────────┘ │
│ ╔══════════╗                       │   thanh phân đoạn ba mục;
│ ║ Trả đủ   ║ Khoản khác  Không trả │   Khoản khác mở hộp nhập số
│ ╚══════════╝                       │
├────────────────────────────────────┤
│  Tổng phải trả        650.000 đ    │
│  Đưa cho thợ hôm nay  650.000 đ    │
│  [   Chốt kỳ, đã trả tiền    ]     │
│  Chốt xong dữ liệu cũ vẫn còn      │
│  nguyên. Bấm nhầm thì vào mục Kỳ   │
│  đã chốt bỏ ra.                    │
└────────────────────────────────────┘
```

Sáu điều đã cân nhắc, đừng đổi mà không đọc lại:

1. **Điền sẵn là trả đủ.** Chín trên mười lần là trả đủ — mở ra bấm một nút là xong. Ba lối
   trả nằm trên một thanh phân đoạn: *Trả đủ*, *Khoản khác*, *Không trả*. Mục **Khoản khác**
   mở đúng hộp nhập mà chạm vào con số *Thực trả* cũng mở — thêm vào vì trước đây muốn trả
   một khoản nhất định thì phải tự đoán ra là chạm được vào con số ấy. Số đang trả không
   phải trả đủ cũng không phải 0 thì viên *Khoản khác* là viên sáng.
2. **Không có ô tích chọn từng người.** Chốt là chốt cả tổ. Muốn khất hẳn một người thì bấm
   *Không trả*: người đó **vẫn nằm trong tờ quyết toán** với số nợ chuyển sang, chứ không
   biến mất khỏi sổ.
3. **Sổ khớp với tiền thật trong ví, không khớp với tiền đáng lẽ phải trả.** Trả thiếu thì
   phần thiếu thành *nợ đầu kỳ* của kỳ sau; trả dư thì thành số âm, kỳ sau trừ lại.
4. **Tách rõ tiền công, tiền đã ứng và nợ cũ** trên từng thẻ. Gộp thành một số "phải trả"
   thì thợ hỏi vì sao ra con số ấy là chịu, không giải thích được.
5. **Thợ đang cầm dư tiền thì mặc định trả 0**, không phải đi đòi lại. Đòi hay không là
   chuyện của người, không phải của máy.
6. **Nói trước là gỡ lại được**, ngay dưới cái nút đáng sợ nhất app. Người dùng ngần ngại ở
   đây thì gọi điện hỏi, mà hỏi thì mất cả buổi.

### 3. Kỳ đã chốt

Các kỳ đã quyết toán, **kỳ mới nhất lên đầu**. Mỗi kỳ một thẻ: khoảng ngày, số thợ, số công,
tiền đã trả. Bấm mở ra **tờ quyết toán** của kỳ đó — từng thợ làm bao nhiêu công, cầm về bao
nhiêu tiền.

Tờ quyết toán là **bản chụp lúc chốt, không tính lại bao giờ nữa**. Sau này tăng lương thợ
hay sửa tên thợ thì tờ cũ vẫn y nguyên như hôm trả tiền — kể cả tên: mỗi dòng giữ lại tên
của lúc ấy chứ không tra ngược theo id.

#### Bỏ chốt

Kỳ mới nhất có nhãn *Mới nhất* và có nút **Bỏ chốt kỳ này**, để tận đáy tờ quyết toán, sau
khi đã cuộn qua hết — không phải thứ bấm trúng lúc đang xem. Viền đỏ, và **hỏi lại ngay trên
chính cái nút**: bấm lần đầu nút đổi thành *"Chắc chưa? Bấm lần nữa để bỏ chốt"*, kèm nút
*Thôi, giữ nguyên* hiện ra bên dưới.

Chỉ kỳ mới nhất mới bỏ chốt được. Gỡ một kỳ ở giữa thì nợ đầu kỳ của các kỳ sau nó thành sai.

Bỏ chốt **không mất buổi công nào** — công và tiền ứng quay về mục Bảng lương y như cũ. Thứ
duy nhất mất là con số *đã trả* đã ghi, phải nhập lại lúc chốt sau. Dòng chữ ngay trên nút
nói đúng như vậy.

### 4. Thợ

Danh sách tên kèm tiền một công. Thêm/sửa thợ trên một biểu mẫu chữ to. Thợ nghỉ việc thì
tắt *Đang làm* chứ không xoá — xoá là mất luôn bảng lương các tháng trước.

```
┌────────────────────────────────────┐
│  Thợ                [ + Thêm thợ ] │   tiêu đề căn trái, nút cao 44pt
│  3 đang làm · 1 đã nghỉ            │
│ ╭────────────────────────────────╮ │
│ │ Anh Tuấn                [Sửa]  │ │   thẻ trắng bo 16, nổi bằng bóng
│ │ 300.000 đ một công             │ │
│ ╰────────────────────────────────╯ │
│  ...                               │
│ ╭────────────────────────────────╮ │
│ │ ▣ Sao lưu                   ›  │ │   dán đáy màn hình
│ │   Đã sao lưu lúc 16:12         │ │
│ ╰────────────────────────────────╯ │
│  [ Nhập Excel ] [ Xuất ra Excel ]  │
└────────────────────────────────────┘
```

**Nút Thêm thợ nằm trong đầu trang**, không phải thanh xanh chiếm hết bề ngang như bản đầu.
Thêm thợ là việc làm vài lần rồi thôi — để nó to bằng cả màn hình thì lấn chỗ danh sách,
thứ người dùng vào đây để xem. Vào đầu trang thì màn hình này dùng đúng khối đầu trang
([`DauTrang`](../mobile/src/giaodien/ThanhPhan.tsx)) như Chấm công, Bảng lương và Kỳ đã chốt —
bốn màn hình nhìn ra một bộ.

Nút cao 44pt chứ không phải 48pt như nút thường — bằng mũi tên đổi tháng bên Bảng lương,
vẫn đúng mức tối thiểu Apple khuyên. Đừng hạ thêm.

Dòng đếm dưới tiêu đề (*3 đang làm · 1 đã nghỉ*) để khỏi ngồi đếm danh sách; chưa có ai thì
ghi thẳng *Chưa có ai* chứ không bỏ trống.

Dưới đáy màn hình có nút **Xuất toàn bộ ra Excel** — xem mục dưới đây.

## Ô nhập và bàn phím

**Ô nhập** dựng theo mẫu `Input` của bản thiết kế: **nhãn nằm bên trong ô**, ở trên, cỡ nhỏ;
chữ người dùng gõ nằm ngay dưới, cỡ lớn hơn. Cả hai **căn trái**. Một chỗ duy nhất:
[`ONhap`](../mobile/src/giaodien/ThanhPhan.tsx).

```
┌────────────────────────────────┐
│ Thợ ứng bao nhiêu?             │   nhãn 12pt xám, nằm trong ô
│ 500.000                        │   chữ gõ vào, 22pt, căn trái
└────────────────────────────────┘
```

Bản đầu để nhãn nằm *ngoài* ô, còn ô nhập số thì **căn giữa**. Bỏ căn giữa vì hai lẽ:

1. **Con nháy đứng giữa ô lúc còn trống** — người gõ không biết chữ sẽ chạy ra đâu. Căn trái
   là chỗ con nháy lúc nào cũng đứng, giống mọi ô nhập khác trên máy.
2. **Gỡ được một mớ code.** Ô căn giữa mà còn trống và có `placeholder` thì Android đẩy con
   nháy ra sát mép phải, nên bản cũ phải tự vẽ chữ gợi ý bằng một lớp phủ riêng. Ô căn trái
   không gặp lỗi ấy, dùng `placeholder` thật là xong.

**Bàn phím đẩy hộp lên** — lấy đúng cách của `CommonModal` bên `trustybot-mobile`, gom vào
[`HopDay`](../mobile/src/giaodien/HopDay.tsx) làm vỏ chung cho cả ba hộp trượt từ đáy:

1. `behavior="padding"` cho **cả iOS lẫn Android**, không phân biệt hệ. Bản cũ để Android là
   `undefined` cho hệ điều hành tự lo. Không phân biệt vẫn đúng: khi cửa sổ tự co lại
   (Android, `adjustResize`) thì `KeyboardAvoidingView` tính ra khoảng đệm bằng 0 nên không
   đẩy thêm lần nữa; còn khi cửa sổ *không* co — đúng trường hợp mấy hộp này, vì
   `statusBarTranslucent` — thì nó là thứ duy nhất đẩy hộp lên.
2. `KeyboardAvoidingView` **phủ kín màn hình** và dồn nội dung xuống đáy, chứ không phải chính
   nó là nền mờ. Nền mờ là một lớp riêng nằm dưới, nhờ vậy bàn phím đẩy hộp lên mà nền mờ vẫn
   phủ nguyên cả màn hình.
3. `pointerEvents="box-none"` để chạm vào khoảng trống hai bên hộp vẫn xuyên xuống nền mờ mà
   đóng được.
4. Đệm đáy lấy từ `useSafeAreaInsets()` chứ không viết cứng — máy có vạch home và máy có nút
   bấm chừa ra hai khoảng khác nhau.

Biểu mẫu thêm/sửa thợ cũng `behavior="padding"` cho cả hai hệ, bọc một `ScrollView` có
`keyboardShouldPersistTaps="handled"` để bấm được nút *Lưu* ngay khi bàn phím còn mở.

## Bộ icon app

Logo lấy từ chính file thiết kế (`Logo`, khối lục giác sáu mặt xanh `#3085FE` với hai mặt bên
đậm `#0D3671`). Dựng từ vector nên nét ở mọi cỡ; nền đặt `#F6F8FB` cho khớp nền app.

| File | Cỡ | Nội dung |
|---|---|---|
| `icon.png` | 1024 | logo 62% trên nền `#F6F8FB` (iOS không nhận nền trong suốt) |
| `splash-icon.png` | 1024 | logo 66%, nền trong suốt |
| `android-icon-foreground.png` | 512 | logo 55%, nền trong suốt — nhỏ vậy để Android bo tròn kiểu nào cũng không cắt vào logo |
| `android-icon-background.png` | 512 | một màu `#F6F8FB` |
| `android-icon-monochrome.png` | 432 | bóng logo tô đen đặc, Android tự nhuộm màu |
| `favicon.png` | 48 | như `icon.png` |

> **Đổi icon phải dựng lại app**, không phải nạp lại JS là thấy: icon nằm trong phần tài nguyên
> gốc của Android/iOS. Bộ icon cũ (đồng hồ kèm dấu tích) còn trong git nếu muốn quay lại.

## Xuất ra Excel

Nút nằm ở chân màn hình **Thợ**, không nằm ở Bảng lương: đây là việc thỉnh thoảng mới làm,
để cạnh bảng lương thì lấn chỗ con số cần nhìn hằng ngày. Chưa có thợ và chưa có công thì
nút chưa hiện — xuất ra một file rỗng chỉ làm người dùng hoang mang.

Bấm một cái là dựng file rồi mở thẳng bảng chia sẻ của máy: gửi Zalo, gửi mail, hay lưu vào
Files/Drive. Không có màn hình chọn kỳ, chọn cột, chọn nơi lưu — mỗi bước chọn là một chỗ
để phân vân. Xuất là xuất hết, mọi kỳ.

File có sáu trang, xếp theo thứ tự cần dùng:

| Trang | Nội dung |
|---|---|
| Quyết toán | Các kỳ đã chốt, mỗi thợ một dòng cho mỗi kỳ: công, tiền công, đã ứng, nợ kỳ trước, đã trả, chuyển kỳ sau |
| Kỳ này | Kỳ đang mở, mỗi thợ một dòng — giống hệt màn hình Bảng lương |
| Buổi công | Từng buổi đã chấm: ngày, thứ, thợ, buổi, số công, thành tiền |
| Ứng tiền | Từng lần ứng |
| Thợ | Danh sách thợ và tiền một công hiện tại |
| Mốc lương | Lịch sử tăng lương của từng thợ |

**Hai trang đầu phải khớp từng đồng với hai màn hình Kỳ đã chốt và Bảng lương.** Trước đây
trang đầu cắt theo tháng trong khi app cắt theo kỳ — cùng một khoản tiền mà file và máy ra
hai bức tranh khác nhau, đối chiếu là loạn. Muốn xem theo tháng thì lọc cột *Ngày* ở trang
Buổi công, Excel làm việc đó giỏi hơn app.

Vừa là cách lấy số liệu ra khỏi điện thoại, vừa là **bản sao lưu đọc được** — máy hỏng mà
còn file này thì vẫn còn sổ sách, dù đọc bằng Excel chứ không nạp ngược vào app được.

Bốn điều đã cân nhắc khi làm, đừng đổi mà không đọc lại:

1. **File cắt theo kỳ, giống hệt app.** Xem bảng trên: hai trang đầu là hai màn hình.
2. **File .xlsx thật, không phải CSV.** CSV mở bằng Excel thì tiếng Việt hay vỡ chữ, ngày
   bị hiểu thành tháng, và không có nổi một dòng tiêu đề in đậm.
3. **Tự dựng file** ([xlsx.ts](../mobile/src/nghiepvu/xlsx.ts)) chứ không lấy thư viện Excel
   có sẵn. Các thư viện đó nặng vài trăm KB, và bản miễn phí của chúng không kẻ được nét
   đậm hay định dạng số. Ở đây chỉ cần nén zip
   ([fflate](https://www.npmjs.com/package/fflate)) và mấy đoạn XML. Phần *đọc* file cũng
   tự viết nốt ([docXlsx.ts](../mobile/src/nghiepvu/docXlsx.ts)) khi làm tính năng nhập
   từ Excel — cùng lý do, và nó chỉ lấy giá trị từng ô chứ không đọc màu mè gì.
4. **Ngày và tiền ghi thành số, không ghi thành chữ.** Có vậy trong Excel mới lọc theo
   khoảng ngày và cộng cột tiền được. Riêng chữ *Tổng cộng* của dòng cuối nằm ngay dưới cột
   Ngày — chỗ này phải ghi thành chữ, ép thành ngày là Excel kêu file hỏng.

Máy Mac không chạy được app để nhìn tận mắt, nên file xuất ra được kiểm hai đường: bộ kiểm
thử `xuatExcel.test.ts` giải nén file rồi soi từng ô, và mở thử bằng NPOI — đúng thư viện
Excel mà app quản lý điện nước trên máy tính đang dùng.

## Nhập từ Excel

Chiều ngược lại của mục trên: đọc một file Excel vào app để **nhập nhanh công của một
thợ**. Có nó vì chấm công trong app là bấm từng buổi từng ngày — nhanh khi chấm hằng ngày,
nhưng chậm phát khóc khi nhập bù cả tháng cũ, hoặc khi chủ đã có sẵn bảng công gõ trên máy
tính. Nút nằm cạnh nút *Xuất ra Excel* ở chân màn hình **Thợ**, hai chiều đứng cạnh nhau.

**Mỗi file một thợ, và thợ chọn trong app chứ không ghi trong file.** Đã cân nhắc để file
tự khai tên thợ cho khỏi phải chọn, nhưng bỏ: gõ "A.Tuấn" hay "Tuấn (thợ nề)" là dò trượt,
mà dò trượt thì cả tháng công rơi vào tay người khác. Chọn trong app thì không bao giờ nhầm
người.

Màn hình xếp dọc **ba bước trên cùng một trang**, không phải ba trang nối tiếp: lúc nào cũng
nhìn thấy mình đang nhập cho ai, và quay lại sửa bước trước không phải bấm lui từng trang.

1. **Nhập cho thợ nào.** Chỉ có một thợ thì chọn sẵn luôn. Thợ đã nghỉ vẫn chọn được — nhập
   bù sổ cũ là việc hay làm nhất.
2. **Lấy file.** Nút *Lấy file mẫu tháng này* dựng sẵn một file `.xlsx` đã **điền sẵn cột
   Ngày và cột Thứ của cả tháng**, gửi qua bảng chia sẻ như lúc xuất. Điền sẵn ngày chứ
   không đưa bảng trống: gõ tay ba mươi cái ngày là ba mươi cơ hội gõ sai định dạng. Nút
   *Chọn file Excel đã điền* mở bảng chọn file của máy.
3. **Xem lại rồi ghi vào sổ.** Bốn ô tóm tắt — số ngày, tổng công, khoảng ngày, tiền ứng —
   rồi mới tới nút *Ghi vào sổ*.

**Luôn xem trước rồi mới ghi.** Đây là chỗ duy nhất trong app đổi hàng chục buổi công chỉ
bằng một cú bấm; nhìn con số tổng là biết ngay có phải file mình cần không. Dòng nào đọc
không ra thì kể rõ *dòng số mấy, sai chỗ nào* chứ không nuốt im, và phần còn lại vẫn ghi
được bình thường.

Bảng gồm sáu cột, mỗi **ngày một dòng** — hai cột Sáng, Chiều đúng như màn hình chấm công,
điền nhanh hơn hẳn kiểu mỗi buổi một dòng:

| Ngày | Thứ | Sáng | Chiều | Ứng tiền | Ghi chú |
|---|---|---|---|---|---|
| 03/08/2026 | Thứ Hai | 1 | 1 | | |
| 04/08/2026 | Thứ Ba | 1 | 0,5 | 500000 | về sớm |

Ba mức của một ô công, phân biệt rõ vì đây là chỗ dễ mất dữ liệu nhất:

- **Để trống** — không đụng tới buổi ấy. Nhập file chỉ có tuần này thì tuần trước trong máy
  vẫn nguyên.
- **`0`, `n`, `nghỉ`, `-`** — nói rõ là nghỉ, buổi đã chấm trong máy sẽ bị bỏ chấm. Màn hình
  đếm trước số buổi sắp bị bỏ chấm và nói ra, không lặng lẽ xoá.
- **Số công, hoặc `x`** — chấm buổi ấy. `x` cho người quen đánh dấu bằng chữ thập.

Bốn điều làm cho nhập nhầm không thành tai hoạ:

1. **Nhập lại đúng một file lần nữa không đổi gì thêm.** Buổi công vốn một-buổi-một-bản-ghi
   theo (thợ, ngày, buổi) nên chỉ đè lên chính nó; ứng tiền không có khoá như vậy nên chỗ
   này tự bỏ qua lần ứng trùng khít cả ngày lẫn số tiền. Bấm nhầm hai lần không thành ứng
   gấp đôi.
2. **Buổi đã nằm trong kỳ đã chốt thì để nguyên**, chỉ báo lại là đã bỏ qua bao nhiêu buổi.
   Tiền kỳ ấy trả xong rồi, sửa số công cũ chỉ làm sổ lệch với tờ đã in đưa thợ.
3. **Dò cột theo tên ở dòng tiêu đề, không đếm theo thứ tự.** Người dùng hay chèn thêm cột
   của riêng họ (số điện thoại, tên công trình); đếm vị trí thì thêm một cột là lệch hết.
   Chấp cả tên không dấu và mấy tên quen thuộc khác (`Buổi sáng`, `Tạm ứng`…).
4. **Ngày nhận cả ba kiểu**: số ngày của Excel, `03/08/2026`, và `2026-08-03`. Ngày không có
   thật như 31/02 thì báo lỗi dòng đó chứ không lặng lẽ đẩy sang 03/03.

File `.xls` đời cũ thì báo thẳng "mở bằng Excel rồi lưu lại thành .xlsx" — nó là định dạng
khác hẳn, không phải cùng một thứ đổi đuôi.

Xem thử file mẫu: [docs/mau-cham-cong.xlsx](mau-cham-cong.xlsx) — đúng file app dựng ra khi
bấm *Lấy file mẫu tháng này*.

Máy Mac không chạy được app để nhìn tận mắt, nên phần đọc file kiểm hai đường: bộ kiểm thử
`nhapExcel.test.ts` dựng file thật rồi đọc lại, và một file điền bằng **openpyxl** — thư
viện Excel của Python, ghi theo đúng lối Excel thật với bảng chữ dùng chung — cũng đọc ra
đủ và đúng.


## Font chữ

**Lexend**, tải qua `@expo-google-fonts/lexend`. Đây là font của bản thiết kế Figma, và cũng
là font *vẽ riêng để dễ đọc*: chữ cái nở ngang, khoảng cách thưa, làm cho người đọc chậm bớt
phải dò. Hợp với người dùng có tuổi của app này hơn hẳn một font trung tính.

Không dùng font mặc định của máy (Roboto trên Android, San Francisco trên iOS): cả hai đều
khô cứng, và mỗi máy một font thì app nhìn khác nhau trên hai hệ.

Đã **soi bảng mã trong file `.ttf`** trước khi đổi: Lexend có đủ chữ tiếng Việt hai dấu
(`ế`, `ộ`, `ữ`, `ằ`, `ỹ`). Đây là điều phải kiểm mỗi lần đổi font — chữ hai dấu là chỗ font
nước ngoài hay thiếu, mà thiếu thì màn hình hiện ô vuông.

Dùng bốn nét: `300Light` (chỉ cho nhãn nhỏ trên ô tóm tắt, đúng như bản thiết kế),
`400Regular`, `500Medium`, `600SemiBold` — khai báo ở
[thietKe.ts](../mobile/src/giaodien/thietKe.ts).

> **Bẫy:** khi đã dùng font riêng thì thuộc tính `fontWeight` **hết tác dụng** — hệ điều hành
> không tự làm đậm font ngoài được. Muốn chữ đậm phải đổi hẳn `fontFamily` sang
> `PhongChu.dam`. Trong app không còn chỗ nào dùng `fontWeight` nữa; thêm mới cũng đừng dùng.

Riêng hai mũi tên `‹ ›` để nguyên font hệ thống — chúng là ký hiệu, không phải chữ.

### Cỡ chữ hệ thống

Người dùng có tuổi hay chỉnh cỡ chữ trong Cài đặt máy lên to. Chữ trong app phóng theo, nên
**mọi khung có chữ đều đặt `minHeight` kèm `paddingVertical`, không đặt `height`**. Đặt
`height` cứng thì khung không nở ra, chữ phóng to bị cắt cụt mất nửa dưới — đúng lỗi đã gặp.

Số ghi trong [thietKe.ts](../mobile/src/giaodien/thietKe.ts) (`caoNut` 48, `caoOCham` 56,
`caoNutNho` 36) vì vậy là **mức sàn**: cỡ chữ thường thì nút cao đúng bằng đó, cỡ chữ to thì
nút tự cao thêm. Nút có icon đi kèm chữ thì cho phần chữ `flexShrink: 1` để nó xuống dòng
thay vì tràn ngang.

Ba chỗ **không nở ngang được** vì chia cột đều nhau — dải bảy ngày ở màn hình Chấm công, tờ
lịch, hộp chọn ngày — cộng thanh tab bốn mục: chữ ở đó chặn ở 1,3 lần
(`HeSoChuToiDaLuoi`, dùng qua thuộc tính `maxFontSizeMultiplier` của `Text`). Phóng hơn nữa
thì hai chữ số ngày không còn chỗ. Chỉ chặn ở lưới, đừng chặn ở nút và chữ thường.

App chờ font tải xong mới vẽ màn hình đầu tiên. Hiện trước rồi font nhảy vào sau thì chữ
giật một cái, nhìn như phần mềm lỗi.

## Màu

Ba màu mang nghĩa rõ ràng, không dùng lẫn lộn. Nhìn màu là đoán được việc, khỏi phải đọc chữ:

| Màu | Nghĩa | Mã dùng cho chữ | Mã Figma, dùng cho viền |
|---|---|---|---|
| Xanh lá | **Đã có công** — ô đã chấm, nút *Cả tổ đi đủ*, tiền còn phải trả | `#4A7D0F` | `#A3D139` |
| Xanh dương | **Thao tác và điều hướng** — nút, nút đổi tuần, ngày đang xem trên dải, tab đang chọn | `#2569E9` | `#3085FE` |
| Đỏ | **Xoá, hoặc số tiền âm** — nút xoá hết, thợ ứng quá tiền, ô *Đã ứng* | `#CE3F30` | `#FF7F74` |
| Xanh ngọc | **Không mang nghĩa gì** — chỉ để phân biệt ô trong lưới tóm tắt | `#0E7A74` | `#30BEB6` |

Phụ trợ: chữ `#101317` (đúng bản thiết kế), chữ mờ `#696F79`, nền `#F6F8FB`, viền `#EBEDF1`.

Bảng màu định nghĩa một chỗ duy nhất ở [thietKe.ts](../mobile/src/giaodien/thietKe.ts): `Mau`
là màu viết chữ được, `Tuoi` là đúng mã Figma và **chỉ dùng cho viền với mảng tô**.

**Vì sao hai cột màu.** Bản Figma làm cho màn hình máy tính trong nhà: chữ trắng trên nền
`#3085FE` chỉ tương phản 3,6:1, chữ lá mạ `#A3D139` trên nền trắng chưa tới 2:1. Người dùng
app này bấm ngoài công trình, giữa nắng. Nên mỗi màu **dùng để viết chữ** đều hạ xuống tới khi
đạt WCAG AA 4,5:1, còn **viền và mảng tô** thì giữ nguyên mã Figma vì chỗ đó không có chữ.
Nhờ vậy ô tóm tắt (nền màu 5% + viền tươi) nhìn giống hệt bản thiết kế, chỉ con số bên trong
là đậm hơn một chút.

Ba điều bắt buộc khi đổi màu:

1. **Mọi cặp chữ/nền phải đạt tương phản WCAG AA 4,5:1**, và phải đạt trên **cả hai** nền —
   nền trắng của thẻ *và* nền hơi xám `#F6F8FB` của trang. Đã kiểm cột *chữ*, số nhỏ hơn là
   số trên nền xám: xanh lá 5,0 / 4,7:1; xanh dương 4,9 / 4,6:1; đỏ 4,8 / 4,5:1; xanh ngọc
   5,2 / 4,9:1; chữ mờ 5,1 / 4,8:1. Vì tương phản tính đối xứng nên chữ trắng trên nền tô đặc
   cũng ra đúng con số ấy. Chọn màu sáng hơn nữa là tụt xuống dưới ngưỡng, ra nắng không đọc
   được — mã Figma nguyên bản chính là như vậy.
2. **Không đổi nghĩa của màu.** Ô đã chấm phải là xanh lá; đừng cho nút thao tác cũng xanh lá.
3. **Xanh ngọc không được mang nghĩa.** Nó là màu thứ tư để bốn ô trong lưới tóm tắt khác
   nhau, không phải một trạng thái. Đừng dùng nó để báo *đã xong* hay *đang chờ*.

App quản lý điện nước trên máy tính dùng tông xanh dương đậm hơn
([Theme.cs](../src/QuanLyDienNuoc/Ui/Theme.cs), `#1565C0`) — cùng họ màu nhưng không giống
hệt, vì trên điện thoại cần sáng và tươi hơn.
