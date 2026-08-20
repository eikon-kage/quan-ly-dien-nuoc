# Xem chấm công trên máy tính

Sổ chấm công **thật** nằm trong app điện thoại. Phần mềm máy tính chỉ **đọc** bản sao lưu mà
app ấy đẩy lên tài khoản Supabase của chủ — vào thanh bên bên trái, mục **Chấm công thợ**.

Chỉ đọc, và đó là chủ ý. Máy tính ghi vào đấy nữa thì hai bên đè sổ lên nhau mà không ai biết:
app điện thoại mới là chỗ có đủ luồng hỏi lại kèm số liệu trước khi ghi đè
(xem [chamcong-sao-luu.md](chamcong-sao-luu.md)). Sửa chấm công thì sửa trên điện thoại.

## Cần điền gì một lần

| Ô | Lấy ở đâu |
| --- | --- |
| ĐỊA CHỈ SUPABASE | Supabase → Project Settings → Data API → *Project URL* |
| KHOÁ CÔNG KHAI | cùng chỗ đó, *anon key* (bản mới gọi là *publishable key*) |
| EMAIL CHỦ | đúng tài khoản đang dùng trên điện thoại |
| MẬT KHẨU | gõ mỗi lần mở, **không được lưu lại** |

Địa chỉ, khoá và email nhớ vào `caidat.json` cạnh file dữ liệu. **Mật khẩu thì không**: file ấy
là văn bản thường, ai mở máy ra cũng đọc được.

Khoá công khai không phải bí mật — nó nằm trong mọi bản app điện thoại đã cài, ai gỡ app ra cũng
đọc được, và Supabase phát nó ra để làm đúng việc ấy. Thứ chặn người này đọc sổ người kia là
**RLS trong database**. Tuyệt đối đừng điền `service_role` key: khoá ấy **bỏ qua RLS**, ai moi
được là đọc và xoá được cả database.

Phải là **tài khoản chủ đăng nhập bằng email** — đúng tài khoản đã đẩy sổ lên, vì bảng `sao_luu`
khoá theo `user_id`. Máy thợ đăng nhập ẩn danh nên không có bản nào ở đây.

## Bốn cách xem cùng một sổ

| Bảng | Là gì |
| --- | --- |
| **Kỳ đang mở** | phần chưa ai trả tiền: mỗi thợ bao nhiêu công, thành bao nhiêu tiền, đã ứng bao nhiêu, nợ kỳ trước mang sang, còn phải trả bao nhiêu |
| **Buổi công** | từng buổi đã chấm, kèm tiền một công *của đúng ngày đó*, và dấu "đã trả" nếu buổi ấy đã nằm trong kỳ đã chốt |
| **Ứng tiền** | từng lần thợ ứng, kèm dấu "đã trừ" |
| **Kỳ đã chốt** | các kỳ đã quyết toán, kỳ mới nhất lên đầu |

Con số ở đây **phải trùng với app điện thoại** — chủ cửa hàng sẽ đặt hai màn hình cạnh nhau mà
so. Nên phần tính là bản dịch từng dòng của `mobile/src/nghiepvu/bangLuong.ts` và `ky.ts`, nằm ở
[`src/ChamCong.Core/SoDiDong/BangLuongSo.cs`](../src/ChamCong.Core/SoDiDong/BangLuongSo.cs), có
bài kiểm thử riêng trong `tests/ChamCong.Tests/SoDiDongTests.cs`.

Hai chỗ dễ tính sai, cả hai đều đã có test canh:

1. **Kỳ lương không cắt theo khoảng ngày** mà cắt theo *bản ghi nào đã được quyết toán*. Chấm bù
   một ngày thuộc kỳ đã chốt thì buổi ấy chưa ai trả tiền, nó phải rơi vào kỳ đang mở. Cắt theo
   ngày là buổi ấy lọt ra ngoài cả hai kỳ và thợ mất công.
2. **Tiền một công lấy theo mốc lương tại đúng ngày của buổi đó.** Tăng lương giữa tháng thì nửa
   đầu tháng vẫn tính giá cũ. Lấy mức hiện tại nhân cho tất cả là tính lại sai cả tháng trước.

## Mỗi ngày một bản

App điện thoại giữ **30 ngày gần nhất**, mỗi ngày một hàng. Ô *BẢN NGÀY* trên màn hình chọn được
từng bản: hôm nay lỡ tay xoá mấy chục buổi công thì mở bản hôm trước ra mà đối chiếu. Mở phần mềm
lên là tự chọn bản mới nhất.

## Dáng dữ liệu

Cột `goi` trong bảng `sao_luu` mang đúng cái gói mà file sao lưu của app điện thoại mang:
`{"app":"cham-cong","phienBan":1,"taoLuc":...,"duLieu":{thos,buoiCongs,ungTiens,kyLuongs}}`.

Bộ đọc ở máy tính ([`Goi.cs`](../src/ChamCong.Core/SoDiDong/Goi.cs)) là bản dịch của `docGoi` bên
điện thoại, và giữ nguyên tinh thần của nó: **dữ liệu từ database cũng là dữ liệu từ ngoài vào**.
Hàng ấy sửa tay được trong SQL Editor, mà cùng một tài khoản có thể vừa chạy bản app cũ vừa chạy
bản mới. Gói của bản app **mới hơn** thì từ chối hẳn chứ không đọc bừa rồi hiện ra số sai.

`ChamCong.SoDiDong` cố ý tách khỏi `ChamCong.Models`: bộ `Models` là mô hình của bản máy tính
viết trước, thợ chỉ có một mức tiền công và chưa có kỳ quyết toán. Sổ trên điện thoại đã đi xa
hơn (mốc lương theo thời gian, kỳ đã chốt) — nhét sổ ấy vào mô hình cũ là mất mốc lương, mất kỳ.
