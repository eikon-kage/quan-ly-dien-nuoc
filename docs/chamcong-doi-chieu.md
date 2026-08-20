# Chấm công: hai máy đối chiếu sổ với nhau

Một máy là **máy chủ** — chủ chấm công cho cả nhóm, tính lương, chốt kỳ. Các máy khác là
**máy thợ** — mỗi thợ tự chấm cho mình. Hai bên giữ **hai sổ riêng**, đọc được sổ của nhau,
và có màn hình đối chiếu chỉ ra chỗ hai bên ghi khác nhau.

## Điều quan trọng nhất: không có gì tự trộn

Sổ bên kia là **bản chụp để đọc**, lưu ở một khoá riêng trong máy
([soBenKia.ts](../mobile/src/nghiepvu/soBenKia.ts)), không bao giờ nhập vào
`DuLieuChamCong`. Bảng lương và quyết toán chỉ tính từ sổ của chính máy đó.

Nếu để dữ liệu bên kia tự chảy vào sổ mình thì thợ tự thêm công cho mình được, và bảng lương
của chủ đổi số mà chủ không hề biết. Muốn sửa thì bấm **từng dòng lệch**, không có nút lấy
tất cả — chỗ này là chỗ tiền ra tiền vào.

Nhờ vậy cũng không cần tới những thứ mà đồng bộ tự động bắt buộc phải có: không cần đánh dấu
bản ghi đã xoá, không sợ buổi đã bỏ chấm sống lại, không sợ hai máy cùng chốt kỳ.

## Sổ công: chỉ có công, không có một đồng nào

Mẩu dữ liệu hai bên trao nhau là [`SoCong`](../mobile/src/nghiepvu/soCong.ts) — ngày, buổi,
số công của **đúng một thợ**. Không mốc lương, không ứng tiền, không kỳ đã chốt.

Cắt tiền ra ngay từ lúc đóng gói, không phải ở giao diện: gói đã gửi đi là nằm trong tay
người ta, mở file ra đọc được hết. Bài kiểm thử soát cả chuỗi JSON xem có số tiền nào lọt ra.

Cùng một kiểu dùng cho **cả hai chiều** — chủ gửi xuống và thợ gửi lên — nên hàm đối chiếu
chỉ có một bản, chạy đúng ở cả hai máy.

## Hai mốc ngày, và vì sao thiếu nó là vô dụng

Mỗi sổ khai `tuNgay`/`denNgay`: khoảng mà nó nói là **đầy đủ**. Đối chiếu chỉ so trong phần
giao của hai khoảng.

Không có hai mốc ấy thì máy thợ mới cài hôm qua, đối chiếu với sổ chủ có ba tháng trước đó,
sẽ ra một trăm dòng "thợ thiếu công" toàn là ngày thợ chưa có app. Người dùng nhìn một màn
hình đỏ rực không sửa được gì rồi thôi, không mở lại nữa.

- Máy thợ khai từ ngày nhận mã mời (`batDauTu`).
- Máy chủ khai 90 ngày gần nhất (`CUA_SO_NGAY`) — đối chiếu là việc của kỳ đang làm.

## Hôm nay còn dở thì chưa kết luận

Cùng một lẽ với hai mốc ngày trên, nhưng ở đầu bên kia của khoảng: **buổi của hôm nay mà chỉ
một bên chấm thì tạm gác**, không phải một dòng lệch.

Ngày còn đang chạy. Chủ chấm cả nhóm lúc nghỉ trưa, thợ mở app lúc về nhà — cùng một buổi ấy
hai người ghi cách nhau mấy tiếng, mà đó là chuyện thường ngày, không phải chuyện hai bên nói
khác nhau. Đếm luôn thì máy thợ vừa nhận mã mời xong, chưa chấm ô nào, mở đối chiếu ra đã thấy
hai dòng đỏ của đúng hôm nay: *người dùng không nhập gì mà app báo lệch*. Đó là màn hình đầu
tiên họ thấy, và là chỗ mất lòng tin đầu tiên.

Ba điều đi kèm, để chỗ gác lại này không tự sinh ra một câu nói dối khác:

- **Cả hai bên đều đã chấm mà số công khác nhau thì vẫn báo**, kể cả hôm nay. Chỗ ấy hai người
  thật sự nói khác nhau; gác lại là che mất.
- Buổi tạm gác **không cộng vào hai tổng** ở đầu trang. Tổng phải nói đúng những dòng đang hiện
  bên dưới, chứ không thì đầu trang bảo lệch 2 công mà không có dòng nào giải thích. Số buổi
  gác lại được nói thẳng thành một câu ("Hôm nay còn dở: 2 buổi mới một bên chấm").
- Không lệch mà cũng **chưa khớp buổi nào** thì đừng nói "hai sổ khớp nhau" — câu ấy là một lời
  bảo đảm, mà ở đây chưa so được gì cả. Màn hình nói *Chưa có gì để so*, và danh sách thợ trên
  máy chủ ghi *Chưa có buổi nào so được* thay vì tô xanh.

## Buổi đã quyết toán thì khoá

Sổ chủ mang thêm cờ `daChot` cho những buổi đã nằm trong kỳ đã trả tiền. Dòng lệch ấy vẫn
hiện lên cho hai bên biết, nhưng không có nút sửa: tiền đã trả rồi, sửa số công bây giờ là
bảng lương cũ nói khác tờ quyết toán đã đưa cho thợ.

## Màn hình chính của máy thợ

Máy thợ chỉ có **một màn hình**, không thanh tab: cả máy chỉ làm một việc. Xếp từ trên xuống
theo đúng thứ tự thợ cần:

1. **Thẻ *Hôm nay*** với hai ô chấm cao gấp rưỡi — chín phần mười lần mở app là để chấm cho
   hôm nay, nên nó là thứ to nhất màn hình. Chạm là một công, chạm lại là bỏ chấm, **bấm giữ**
   ra mấy mức có sẵn (1 / 0,5 / 1,5) kèm đường *Gõ số công khác* — cùng hộp nhập, cùng cách
   đọc số ("0,25", "0.25" đều hiểu) và cùng mức chặn 5 công một buổi như máy chủ. Hai bên gõ
   ra hai kiểu số thì đối chiếu báo lệch mà chẳng ai sai.
2. **Dải *Chưa nối nhóm*** — chỉ hiện khi máy chưa vào nhóm, và bấm vào là ra thẳng ô dán mã
   mời. Chưa vào nhóm thì thợ chấm mà sổ nằm im trong máy, chủ không thấy gì: đó là việc gấp
   nhất sau chấm công, không thể là một dòng chữ xám cuối trang. Trước đây đường vào duy nhất
   là dòng *Máy của thợ · đổi lại* ở đáy màn hình, còn câu chỉ đường trên đầu trang thì viết
   cho máy chủ — "mở mục Thợ → Nhóm chấm công" — mà máy thợ không có mục Thợ. Nút ở góc phải
   đầu trang cũng vậy: chưa vào nhóm thì nó mở ô dán mã, không còn bấm không ăn.
   Nối rồi thì dòng cuối trang ghi **Đã nối nhóm · thoát** — đó là đường đăng xuất của thợ
   (`ngat`: đăng xuất Supabase, sổ trong máy còn nguyên). Nút vốn đã có trong hộp *Nhóm chấm
   công*, nhưng gọi là "ngắt" và không có chữ nào ngoài màn hình cho thấy có đường ra, nên
   nhìn như app không cho thợ thoát. Hộp cũng nhắc rõ: **vào lại phải xin mã mời mới**, vì mã
   dùng một lần là hết.
3. **Hai ô tóm tắt**: công tuần này, công tháng này. Trước đây màn hình không có con số tổng
   nào, thợ muốn biết "tháng này tôi được bao nhiêu công" thì phải tự đếm ô.
4. **Dòng đối chiếu** với sổ chủ, kèm số buổi lệch — việc *phải nhìn*.
5. **Hai nút nửa bề ngang**: *Sổ công của tôi* và *Xuất ra Excel*. Trước đây mỗi việc một dòng
   thẻ trắng riêng; ba dòng xếp dọc đẩy danh sách chấm bù xuống quá nửa màn hình, mà hai việc
   này thì cả tuần mới dùng một lần.
6. **Chấm bù mấy ngày trước**: 14 ngày, gom theo tuần — mỗi tuần một thẻ, có tổng công của
   tuần. Bản cũ là 13 thẻ trắng rời nhau, mỗi ngày một thẻ hai dòng: cuộn mãi không hết mà
   vẫn không thấy tuần nào đi nhiều tuần nào đi ít. Giờ thứ và ngày nằm chung một cột hẹp bên
   trái, hai ô chấm cùng dòng, Chủ Nhật ghi đỏ như lịch treo tường.

Tổng của thẻ *Tuần này* tính **cả hôm nay**, dù hôm nay không nằm trong danh sách (nó đã có
thẻ riêng ở trên). Đếm theo mấy dòng đang hiện thì con số ấy lại khác ô tóm tắt, hai chỗ trên
cùng một màn hình nói hai kiểu.

Cả màn hình **không có một con số tiền nào** — xem `ketNap` trong
[vaiMay.ts](../mobile/src/nghiepvu/vaiMay.ts).

## Máy thợ xem sổ của mình theo tháng

Màn hình chính của máy thợ chỉ có **14 ngày gần đây** — đủ để chấm và chấm bù, nhưng thợ
thắc mắc thì hay thắc mắc chuyện *tháng trước*: "hôm mùng mười tôi có đi không". Trước đây
thợ không có đường nào xem, phải nhờ chủ mở máy ra tra hộ. Giờ có
[ManHinhSoCuaToi](../mobile/src/giaodien/ManHinhSoCuaToi.tsx), mở từ dòng *Sổ công của tôi*.

Dựng **giống màn hình chi tiết một thợ bên máy chủ**: cùng [tờ lịch](../mobile/src/giaodien/LichCong.tsx),
cùng cách chia nửa tháng, cùng lưới bốn ô tóm tắt. Cố ý giống — ngồi soát với nhau thì hai
bên chỉ tay vào cùng một ô, thay vì mỗi người đọc một kiểu bảng.

Khác bên chủ đúng một điều, và là điều bắt buộc: **không có đồng tiền nào**. Chỗ chủ để tiền
công / đã ứng / còn phải trả thì đây là số ngày đi làm, số ngày nghỉ và số buổi ghi khác sổ
chủ. Bảo đảm bằng **dữ liệu vào** chứ không bằng giao diện: màn hình này dựng trên `SoCong`,
không dựng trên `DuLieuChamCong` như bên chủ, nên không có tiền trong tay mà lỡ hiện ra —
kể cả khi sổ trong máy còn sót mốc lương từ lúc máy này từng là máy chủ.

Hai chỗ khác cũng đáng ghi:

- **Chỉ xem, không sửa.** Chấm và chấm bù vẫn ở màn hình chính — hai chỗ chấm được cùng một
  buổi là hai chỗ để bấm nhầm. Ngày nào lệch sổ chủ thì có dấu đỏ ngay trên dòng, còn sửa thì
  sang màn hình đối chiếu.
- **Mũi tên đổi tháng tắt ở hai đầu** theo đúng khoảng sổ khai là đầy đủ. Cho lùi mãi thì thợ
  xem được mười tờ lịch trắng rồi tưởng máy mất dữ liệu — mà ngoài khoảng ấy là *không biết*,
  chứ không phải không đi làm.

## Nối ngay lúc mở app

Mở app ra là nối luôn, không ai phải mò vào mục nào bấm nút nào. Hai nửa:

**Nửa tự động** nằm trong [dungSupabase.ts](../mobile/src/giaodien/dungSupabase.ts): lúc mở
app nó đọc phiên đăng nhập đã lưu, tra nhóm, và **nếu là máy chủ đã đăng nhập mà chưa có
nhóm thì lập nhóm luôn**. `tao_nhom` gọi mấy lần cũng ra đúng một nhóm nên gọi thẳng là an
toàn. Trước đây đoạn này chỉ *đọc*, nên một lượt lập nhóm hụt (mất mạng, bảng chưa dựng) là
mỗi lần mở app lại phải vào **Thợ → Nhóm chấm công** bấm *Lập nhóm, thử lại*.

Đoạn ấy **đợi đọc xong vai máy mới chạy**, và đó là điều bắt buộc: lập nhóm giúp một máy thợ
là đặt nó vào một nhóm một người — sổ nó gửi lên không ai nhận, mà mã mời của chủ sau đó cũng
không đổi được nữa vì máy đã có nhóm.

**Nửa hỏi người dùng** là [ManHinhMoDau.tsx](../mobile/src/giaodien/ManHinhMoDau.tsx), hiện
lên trước cả thanh tab khi biết chắc máy chưa ở trong nhóm nào. Nó hỏi **hai bước**:

1. *Máy này là của ai* — chủ hay thợ. Chỗ duy nhất trong app hỏi câu này.
2. *Vào bằng cách nào* — và mỗi vai một cách: chủ đăng nhập email, thợ dán mã mời. Hai cách ấy
   mở đúng hai cái hộp đang dùng trong mục Thợ, không viết lại form lần thứ hai.

Hỏi hai bước chứ không gộp một, dù gộp thì ít hơn một cú bấm: gộp lại là hai đường *đăng nhập*
và *dán mã* nằm cạnh nhau, người dùng phải tự dịch từ "tôi là ai" sang "tôi bấm cái nào" — câu
hỏi khó hơn hẳn câu app đang cần họ trả lời.

Bước hai luôn có **đường đi tiếp mà không cần email cũng không cần mã mời**, và đó là một cái
nút bấm được chứ không phải một câu chữ an ủi:

- **Chủ: *Dùng một mình, không cần email.*** Nhà ba thợ, chấm bằng một cái điện thoại, chẳng
  cần ai đối chiếu. Đây là một *quyết định*, khác *Để sau* là hoãn — nên nó ghi xuống máy
  (`dungMotMinh` trong [vaiMay.ts](../mobile/src/nghiepvu/vaiMay.ts)) và lần mở app sau không
  hỏi lại.
- **Thợ: *Chưa có mã, tự chấm trước.*** Thợ tải app giữa tuần, chủ đang ngoài công trình. Máy
  thành máy thợ ngay với `thoId` **do máy tự đặt**, đánh dấu `thoIdTuTao`. Tới lúc dán được mã,
  `ketNap` gọi [`doiThoId`](../mobile/src/nghiepvu/thaoTac.ts) kéo mọi buổi đã chấm sang id thật
  của sổ chủ, và **giữ nguyên mốc bắt đầu chấm** — đặt lại mốc thành hôm nay thì đúng mấy buổi
  ấy rơi ra ngoài khoảng sổ khai là đầy đủ, đối chiếu bỏ qua sạch.

  Cờ `thoIdTuTao` là thứ bắt buộc phải nhớ, không đoán lại được: không có nó thì không phân
  biệt được id tự đặt với id thật của một nhóm cũ, mà chuyển bừa là **gộp sổ hai người**.

Ba điều kiện quyết định có hiện màn hình ấy hay không, mỗi điều kiện chặn một cách hỏi sai:

- **Chưa tra xong thì chưa hỏi** (`dangDoc`). Máy đã nối rồi mà thấy màn hình đăng nhập nhoáng
  lên một nhịp thì người dùng tưởng mất tài khoản.
- **Không tra được thì cũng không hỏi** (`traHut`). Mất mạng là *không biết* đã ở nhóm nào
  chưa, khác hẳn *biết là chưa vào nhóm*. Máy chủ ngoài vùng phủ sóng phải mở app ra chấm công
  được, không phải nhìn màn hình đăng nhập.
- **Đã chọn dùng một mình thì không hỏi nữa** (`dungMotMinh`). Hỏi lại mỗi lần mở app là
  phiền đúng người đã trả lời xong.
- **Nút *Để sau* luôn có,** và chỉ nhớ trong lượt mở app ấy chứ không ghi xuống máy. Đây là
  điều kiện để đưa màn hình này lên trước: app chấm công vẫn chạy trọn vẹn khi không có mạng
  và không có tài khoản nào, nên chặn đường người chỉ muốn chấm công thì mất nhiều hơn được.
  Không ghi xuống máy vì sổ chưa nối thì vẫn chưa ai nhận được — lần mở sau hỏi lại là đúng.

Mục **Thợ → Nhóm chấm công** vẫn còn nguyên: đó là chỗ xem đang nối bằng tài khoản nào và
ngắt khỏi nhóm, chứ không còn là đường vào duy nhất.

## Mã mời: đúng một mã, làm cả ba việc

Chủ mở **Thợ → Đối chiếu với sổ thợ**, chọn thợ chưa gửi sổ, bấm **Phát mã mời**. Database
sinh ra một mã sáu ký tự (`phat_ma_moi`), dùng một lần, sống ba ngày. Mã bỏ hẳn chữ O, I, L,
số 0 và 1 — đọc lên nghe giống nhau.

Thợ mở **Thợ → Máy của thợ** rồi dán mã. Một lần dán mã ấy làm xong cả ba việc:

1. Xin một tài khoản ẩn danh (không email, không mật khẩu).
2. Vào nhóm — `doi_ma_moi` kiểm mã còn hạn, chưa dùng, rồi ghi hàng `thanh_vien` trong **một**
   bước.
3. Đặt vai máy kèm `thoId` **lấy từ câu trả lời của database**, không phải từ chữ người dùng
   gõ. Đó là id của thợ trong sổ chủ; hai máy đặt id khác nhau thì lúc đối chiếu không ghép
   được ai với ai.

Bản trước bắt thợ nhập **hai** mã khác nhau: một mã `CC-<thoId>` để đặt vai máy (chỉ chạy
trong máy, không cần mạng), rồi một mã sáu ký tự để vào nhóm. Hai mã cho một lần cài, mà
người phải làm là thợ đang đứng ngoài công trường. Cái giá của việc gộp lại: **lúc cài máy
thợ phải có mạng.** Chấm công thì vẫn không cần.

Thứ tự trong code cũng là thứ tự bắt buộc: **đổi mã trước, đặt vai sau**. Làm ngược thì mã
sai cũng đã biến máy thành máy thợ — không còn thấy tiền, không còn danh sách thợ — mà nhóm
thì vẫn chưa vào được.

Mã không nhồi tên thợ vào: tên có dấu, gõ lại qua Zalo là sai. Máy thợ lấy tên từ chính sổ
chủ gửi xuống.

Máy cũ của chủ chuyền tay cho thợ thì lúc nhận mã có thêm nút **xoá sổ của người khác** —
bỏ hết bản ghi của người khác và xoá sạch tiền, kể cả mốc lương của chính thợ ấy. Cái gì
không có trên máy thì không ai xem lén được; ẩn bằng giao diện thì vẫn còn nằm đó.

## Hộp thư: Supabase, có phân quyền thật

[hopThu.ts](../mobile/src/nghiepvu/hopThu.ts) là **một giao diện có thể thay ruột**. Nó chỉ
nói bằng lời của việc chấm công — gửi sổ, đọc sổ — không hé một chữ nào về bảng, file hay
token. Ruột là [hopThuSupabase.ts](../mobile/src/nghiepvu/hopThuSupabase.ts), và
[App.tsx](../mobile/App.tsx) là chỗ **duy nhất** dựng nó ra.

Mỗi `(nhóm, thợ, bên gửi)` đúng một hàng trong bảng `so_cong`, ghi đè mãi lên nó. Không giữ
lịch sử ở đó: sổ là bản chụp toàn khoảng, bản mới nói đủ những gì bản cũ nói — lịch sử đã có
bên [sao lưu theo ngày](chamcong-sao-luu.md) lo.

**Chặn nằm ở database, không ở app.** Máy thợ gọi `select` cả bảng thì Postgres tự cắt còn
đúng hai dòng của nó, theo RLS: sổ chủ gửi cho chính nó, và sổ chính nó gửi lên. Ghi thì chỉ
ghi được hàng của mình mang nguồn `'tho'` — "thợ tự thêm công cho mình" bị chặn ở tầng
database, không phải ở giao diện. Toàn bộ chính sách nằm trong
[thiet-lap.sql](../mobile/supabase/thiet-lap.sql), và khoá công khai trong app không mở thêm
được gì: quên bật RLS trên một bảng mới là bảng ấy công khai với cả internet, nên đừng thêm
bảng mà quên dòng `enable row level security`.

### Hai vai, hai kiểu đăng nhập

**Máy chủ bắt buộc dùng email và mật khẩu.** Tài khoản ấy nắm nhóm của cả cửa hàng, mà hàng
`thanh_vien` thì gắn với `user_id` — nên nếu chủ đăng nhập ẩn danh, tài khoản chỉ sống trong
một cái điện thoại: mất máy là mất nhóm, mọi thợ phải nhận mã mời lại từ một nhóm mới, còn sổ
họ đã gửi lên thì nằm ở nhóm cũ không ai vào được nữa. Bản trước có nút *Nối nhanh, không cần
email* cho chủ; đã bỏ, và **đừng thêm lại** — có một bài kiểm thử canh chỗ đó.

**Máy thợ thì ngược lại, ẩn danh là đúng:** không email, không mật khẩu, chỉ một mã mời. Sổ
thật của thợ nằm trong máy họ, nên mất máy chỉ việc dán mã mời mới. Bắt thợ nhớ thêm một cặp
email — mật khẩu là thêm một lý do để họ thôi không dùng app nữa.

### Vì sao bỏ hộp thư Drive

Bản đầu đặt sổ thành file JSON trên Google Drive, cả nhóm đăng nhập **cùng một tài khoản** —
bắt buộc phải thế, vì quyền `drive.file` chỉ cho app thấy file do chính nó tạo trên tài khoản
đó. Cách ấy chạy được nhưng *không chặn được gì về mặt quyền*: máy nào cũng đọc và xoá được
sổ của máy khác, ai mở drive.google.com bằng tài khoản ấy là thấy hết, và một máy thợ xoá
nhầm là cả nhóm mất hộp thư.

Đã bỏ hẳn, cùng lúc bỏ sao lưu Drive — nghĩa là app **không còn phụ thuộc Google** ở đâu nữa.

## Khi nào đồng bộ

Một lần lúc mở app (nếu đã vào nhóm), và mỗi lần bấm mũi tên đồng bộ. Không chạy ngầm sau
từng ô chấm như sao lưu: đối chiếu là việc cuối ngày hay cuối kỳ, đẩy đi liên tục chỉ tốn 3G
của cả nhóm cho những con số chưa ai xem.

Sổ nhận về ghi xuống máy ngay, nên mất mạng vẫn xem đối chiếu được — chỉ là số liệu tính đến
lần đồng bộ gần nhất, và màn hình ghi rõ giờ của lần ấy.

## Chưa làm

- **Ứng tiền chưa đối chiếu.** Ứng tiền là tiền, mà máy thợ được quy định chỉ thấy số công.
  Muốn thợ soát được "tôi ứng 500, sổ ghi 300" thì phải mở cho máy thợ thấy tiền ứng của
  chính mình — một quyết định về nghiệp vụ, không phải về code.
- **Chưa có QR.** Mã mời đọc bằng miệng hoặc dán qua Zalo. Một tháng chấm công của một thợ
  chỉ vài chục byte nếu mã hoá gọn, lọt thừa vào một QR — sau này thêm được đường trao sổ
  không cần mạng.
