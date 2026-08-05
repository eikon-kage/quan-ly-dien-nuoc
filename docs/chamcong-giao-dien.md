# App chấm công — nguyên tắc giao diện

Người dùng app này là **chủ cửa hàng, không rành công nghệ**, bấm ngoài công trình hoặc
ngoài sân. Mọi quyết định giao diện dưới đây đều xuất phát từ đó.

> **Đã điều chỉnh một lần.** Bản đầu làm chữ rất to (tên thợ 26pt) và nút rất cao (ô chấm
> 72pt) vì người dùng có tuổi. Nhìn trên máy thật thì nặng nề và thô, nên chủ dự án yêu cầu
> hạ xuống cho hài hoà. Cỡ hiện tại vừa phải, nhưng **vẫn lớn hơn app thông thường** — đừng
> hạ tiếp mà không hỏi.

## Chín điều bắt buộc

1. **Mở app là chấm được ngay.** Không đăng nhập, không màn hình chào, không hướng dẫn.
   Màn hình đầu tiên luôn là chấm công của *hôm nay*.
2. **Chữ vừa mắt, không nhỏ.** Tên thợ 19pt, chữ trên nút 15pt, chữ phụ 13pt. Vẫn to hơn
   app thông thường vì người dùng có tuổi.
3. **Nét chữ nhiều nhất là 600 (SemiBold).** Không dùng 700 — ở cỡ lớn nhìn nặng và thô.
4. **Nút cao 48pt, ô chấm 56pt.** Apple khuyên tối thiểu 44pt; đây là thứ bấm hằng ngày nên
   rộng hơn một chút.
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
   kèm thứ, không viết `3/8`. Rung nhẹ mỗi khi chạm trúng.

## Bốn màn hình, không hơn

Thanh dưới có bốn mục: **Chấm công · Bảng lương · Kỳ đã chốt · Thợ**. Mỗi mục thêm vào là
một chỗ để người dùng lạc — đừng thêm mục thứ năm.

> **Trước đây chỉ có ba mục.** Mục *Kỳ đã chốt* thêm vào cùng lúc với quyết toán. Đã cân
> nhắc nhét sổ cũ vào ngay trong Bảng lương, nhưng bỏ: ba mục đầu là chỗ **làm việc hằng
> ngày**, mục thứ tư là chỗ **tra sổ cũ**. Hai việc khác nhau, gộp lại thì màn hình dùng
> mỗi ngày bị sổ của mấy tháng trước chen chỗ.

Thanh tab này **tự vẽ** ([App.tsx](../mobile/App.tsx)) chứ không dùng thanh mặc định của iOS.
Thanh mặc định chữ khoảng 10pt và chủ yếu là hình — người có tuổi không đọc ra. Bản tự vẽ
cao 68pt, chữ 19pt, mục đang chọn có cả viền xanh trên đầu lẫn nền xanh nhạt.

Hai hộp thoại — chọn nửa công / công rưỡi ([HopChon.tsx](../mobile/src/giaodien/HopChon.tsx))
và nhập tiền ứng ([HopNhapSo.tsx](../mobile/src/giaodien/HopNhapSo.tsx)) — cũng tự vẽ. Ban
đầu dùng `ActionSheetIOS` và `Alert.prompt` của hệ điều hành, nhưng bỏ vì hai lẽ: chúng
**chỉ có trên iOS**, và chữ trong đó không ép to được. Tự vẽ thì nút cao 60pt, chữ 20pt,
giống hệt phần còn lại của app.

Nhờ vậy app chạy được cả trên Android, tiện lúc muốn thử nhanh trên máy Android có sẵn.

### 1. Chấm công (màn hình chính)

```
┌────────────────────────────────────┐
│  ‹         Thứ Tư 05/08         ›  │   ngày đang xem, 17pt đậm
│ Tuần       [ Hôm nay ]       Tuần  │   nút Hôm nay chỉ hiện khi xem ngày khác
│ ┌───┐┌───┐┌───┐┌───┐┌───┐┌───┐┌───┐│
│ │ T2││ T3││Nay││ T5││ T6││ T7││ CN││   dải bảy ngày, chạm là sang ngày đó
│ │ 03││ 04││ 05││ 06││ 07││ 08││ 09││
│ │  4││  2││  ·││  ·││  ·││  ·││  ·││   số công cả tổ đã chấm ngày đó
│ └───┘└───┘└───┘└───┘└───┘└───┘└───┘│
├────────────────────────────────────┤
│     [   Cả tổ đi đủ cả ngày   ]    │   nút xanh, cao 48pt
├────────────────────────────────────┤
│  Anh Tuấn                   [Sửa]  │   19pt
│  ┌──────────────┐ ┌──────────────┐ │
│  │   SÁNG    ✓  │ │  CHIỀU    ✓  │ │   cao 56pt
│  └──────────────┘ └──────────────┘ │
│                                    │
│  Anh Bình                   [Sửa]  │
│  ┌──────────────┐ ┌──────────────┐ │
│  │   SÁNG    ✓  │ │  CHIỀU       │ │
│  └──────────────┘ └──────────────┘ │
├────────────────────────────────────┤
│  Hôm nay: 3 công                   │   thanh dưới cố định
└────────────────────────────────────┘
```

Mỗi thợ là một thẻ riêng chứ không phải một dòng trong bảng. Nhồi tên và hai ô vào cùng một
dòng thì với cỡ chữ 19pt là chật, chữ bị cắt.

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
thế"*. Trên cùng vẫn là mấy con số tóm tắt, dưới là **tờ lịch**
([LichCong.tsx](../mobile/src/giaodien/LichCong.tsx)), cuối cùng là các lần ứng tiền.

Kỳ chốt lúc nào cũng được nên nó hay **vắt qua hai tháng**. Lúc ấy mỗi tháng vẽ một tờ lịch
riêng xếp dọc, có tên tháng ở trên. Gộp hai tháng vào một tờ thì không còn là tờ lịch treo
tường nữa, mà chính hình dáng tờ lịch mới là thứ làm người xem nhìn ra ngay chỗ nghỉ nằm đâu.

##### Lọc theo khoảng ngày

Mở ra là trọn kỳ, nhưng chọn hẹp lại được — nhiều nhà trả một phần giữa chừng chứ không đợi
chốt kỳ, lúc ấy con số cần nhìn là của mấy ngày đó.

```
┌──────────────────────────────────────────┐
│  Từ [ 01/08 ]  →  Đến [ 15/08 ]          │   chạm là mở tờ lịch chọn ngày
│  [Cả kỳ] [Cả tháng] [Nửa đầu] [Nửa cuối] │   bốn khoảng hay dùng, một chạm
└──────────────────────────────────────────┘
```

**Nút *Cả kỳ* luôn đứng đầu**: lỡ lọc hẹp rồi thì đó là đường về. Kỳ trùng đúng một tháng thì
*Cả kỳ* và *Cả tháng* cùng sáng — đúng vậy, hai nút đang trỏ về một khoảng.

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
│  [  Trả đủ  ]  [ Không trả ]       │   hai nút tắt
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

1. **Điền sẵn là trả đủ.** Chín trên mười lần là trả đủ — mở ra bấm một nút là xong.
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
│  Thợ                [ + Thêm thợ ] │   đầu trang trắng, nút cao 44pt
│  3 đang làm · 1 đã nghỉ            │
├────────────────────────────────────┤
│  Anh Tuấn                   [Sửa]  │
│  300.000 đ một công                │
│  ...                               │
├────────────────────────────────────┤
│  [   Xuất toàn bộ ra Excel   ]     │   thanh dưới cố định
└────────────────────────────────────┘
```

**Nút Thêm thợ nằm trong đầu trang**, không phải thanh xanh chiếm hết bề ngang như bản đầu.
Thêm thợ là việc làm vài lần rồi thôi — để nó to bằng cả màn hình thì lấn chỗ danh sách,
thứ người dùng vào đây để xem. Vào đầu trang thì màn hình này cũng có đầu trang trắng
giống Chấm công và Bảng lương, các màn hình nhìn ra một bộ.

Nút cao 44pt chứ không phải 48pt như nút thường — bằng mũi tên đổi tháng bên Bảng lương,
vẫn đúng mức tối thiểu Apple khuyên. Đừng hạ thêm.

Dòng đếm dưới tiêu đề (*3 đang làm · 1 đã nghỉ*) để khỏi ngồi đếm danh sách; chưa có ai thì
ghi thẳng *Chưa có ai* chứ không bỏ trống.

Dưới đáy màn hình có nút **Xuất toàn bộ ra Excel** — xem mục dưới đây.

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
   có sẵn. Các thư viện đó nặng vài trăm KB vì mang theo cả phần *đọc* file — app này chỉ
   ghi — và bản miễn phí của chúng không kẻ được nét đậm hay định dạng số. Ở đây chỉ cần
   nén zip ([fflate](https://www.npmjs.com/package/fflate)) và mấy đoạn XML.
4. **Ngày và tiền ghi thành số, không ghi thành chữ.** Có vậy trong Excel mới lọc theo
   khoảng ngày và cộng cột tiền được. Riêng chữ *Tổng cộng* của dòng cuối nằm ngay dưới cột
   Ngày — chỗ này phải ghi thành chữ, ép thành ngày là Excel kêu file hỏng.

Máy Mac không chạy được app để nhìn tận mắt, nên file xuất ra được kiểm hai đường: bộ kiểm
thử `xuatExcel.test.ts` giải nén file rồi soi từng ô, và mở thử bằng NPOI — đúng thư viện
Excel mà app quản lý điện nước trên máy tính đang dùng.

## Font chữ

**Be Vietnam Pro**, tải qua `@expo-google-fonts/be-vietnam-pro`.

Không dùng font mặc định của máy (Roboto trên Android, San Francisco trên iOS): cả hai đều
khô cứng, và quan trọng hơn là chúng không được vẽ riêng cho tiếng Việt — chữ có hai dấu
chồng nhau như `ế`, `ộ`, `ữ` bị đặt lệch hoặc sát quá. Be Vietnam Pro thiết kế từ đầu cho
tiếng Việt nên dấu mũ và dấu thanh nằm gọn, đọc ở cỡ to càng rõ.

Dùng ba nét: `400Regular`, `500Medium`, `700Bold` — khai báo ở
[thietKe.ts](../mobile/src/giaodien/thietKe.ts).

> **Bẫy:** khi đã dùng font riêng thì thuộc tính `fontWeight` **hết tác dụng** — hệ điều hành
> không tự làm đậm font ngoài được. Muốn chữ đậm phải đổi hẳn `fontFamily` sang
> `PhongChu.dam`. Trong app không còn chỗ nào dùng `fontWeight` nữa; thêm mới cũng đừng dùng.

Riêng hai mũi tên `‹ ›` để nguyên font hệ thống — chúng là ký hiệu, không phải chữ.

App chờ font tải xong mới vẽ màn hình đầu tiên. Hiện trước rồi font nhảy vào sau thì chữ
giật một cái, nhìn như phần mềm lỗi.

## Màu

Ba màu mang nghĩa rõ ràng, không dùng lẫn lộn. Nhìn màu là đoán được việc, khỏi phải đọc chữ:

| Màu | Nghĩa | Mã |
|---|---|---|
| Xanh lá | **Đã có công** — ô đã chấm, nút *Cả tổ đi đủ*, tiền còn phải trả | `#15803D` |
| Xanh dương | **Thao tác và điều hướng** — nút, mũi tên đổi tuần, ngày đang xem trên dải, tab đang chọn | `#2563EB` |
| Đỏ | **Xoá, hoặc số tiền âm** — nút xoá hết, thợ ứng quá tiền | `#E11D48` |

Phụ trợ: chữ `#0F172A`, chữ mờ `#64748B`, nền `#F8FAFC`, viền `#E2E8F0`.

Bảng màu định nghĩa một chỗ duy nhất ở [thietKe.ts](../mobile/src/giaodien/thietKe.ts).

Hai điều bắt buộc khi đổi màu:

1. **Mọi cặp chữ/nền phải đạt tương phản WCAG AA.** Bảng trên đã kiểm: chữ trắng trên
   xanh lá 5,0:1; xanh dương trên trắng 5,1:1; đỏ trên trắng 4,7:1. Chọn màu sáng hơn nữa
   là tụt xuống dưới ngưỡng, ra nắng không đọc được.
2. **Không đổi nghĩa của màu.** Ô đã chấm phải là xanh lá; đừng cho nút thao tác cũng xanh lá.

App quản lý điện nước trên máy tính dùng tông xanh dương đậm hơn
([Theme.cs](../src/QuanLyDienNuoc/Ui/Theme.cs), `#1565C0`) — cùng họ màu nhưng không giống
hệt, vì trên điện thoại cần sáng và tươi hơn.
