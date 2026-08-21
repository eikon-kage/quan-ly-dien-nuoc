-- Kiểm tra phân quyền (RLS) của hộp thư chấm công, chạy trên một Postgres **cục bộ**.
--
-- Vì sao phải có file này: app gọi thẳng vào database bằng một khoá công khai nằm sẵn trong
-- app. Không có máy chủ trung gian nào chặn hộ, nên RLS là ổ khoá duy nhất — mà RLS thì sai
-- một dòng cũng không có triệu chứng gì cả: app vẫn chạy êm, chỉ là dữ liệu công khai với
-- cả internet. Loại lỗi ấy phải bắt bằng bài kiểm tra chứ không bắt bằng đọc lại code.
--
-- Cách chạy (cần postgresql, không cần Docker, không cần project Supabase):
--
--   PG=/opt/homebrew/opt/postgresql@14/bin
--   $PG/psql -h 127.0.0.1 -p 55432 -U postgres -d thu -v ON_ERROR_STOP=1 \
--     -f mobile/supabase/kiem-tra-rls.sql
--
-- Chạy xong không báo gì ngoài mấy dòng "OK" nghĩa là đạt; sai một điều kiện nào là dừng
-- ngay tại đó với câu báo lỗi.

\set ON_ERROR_STOP on

-- ---------------------------------------------------------------------------
-- Giả lập phần Supabase dựng sẵn: schema auth, bảng người dùng, hàm auth.uid(),
-- và vai `authenticated` mà mọi người đăng nhập đều mang.
-- ---------------------------------------------------------------------------

drop schema if exists auth cascade;
create schema auth;

create table auth.users (id uuid primary key);

-- Supabase lấy id người đăng nhập từ JWT. Ở đây lấy từ một biến phiên để bài kiểm tra đổi
-- người dùng được bằng một câu `set`.
create or replace function auth.uid() returns uuid language sql stable as $$
  select nullif(current_setting('request.jwt.claim.sub', true), '')::uuid
$$;

-- `auth.jwt()` trả cả bộ claim. Policy của bảng sao_luu đọc `is_anonymous` từ đây để chặn máy
-- thợ đẩy sổ lên. Không giả hàm này thì policy ấy quăng lỗi "function does not exist" — mà lúc
-- ấy triệu chứng lại là *chủ không ghi được*, chứ không phải *thợ ghi được*.
create or replace function auth.jwt() returns jsonb language sql stable as $$
  select coalesce(nullif(current_setting('request.jwt.claims', true), '')::jsonb, '{}'::jsonb)
$$;

do $$ begin
  if not exists (select 1 from pg_roles where rolname = 'authenticated') then
    create role authenticated nologin;
  end if;
end $$;

grant usage on schema auth, public to authenticated;

-- Dọn sạch bảng cũ để chạy lại được nhiều lần.
drop table if exists so_cong, thanh_vien, ma_moi, sao_luu cascade;

\ir thiet-lap.sql

grant select, insert, update, delete on all tables in schema public to authenticated;
grant execute on all functions in schema public to authenticated;

-- ---------------------------------------------------------------------------
-- Ba người: chủ và thợ Tuấn cùng nhóm, thợ Bình ở nhóm khác.
-- ---------------------------------------------------------------------------

insert into auth.users (id) values
  ('11111111-1111-1111-1111-111111111111'),  -- chủ
  ('22222222-2222-2222-2222-222222222222'),  -- thợ Tuấn
  ('33333333-3333-3333-3333-333333333333');  -- thợ Bình, nhóm khác

-- Chủ lập nhóm, phát mã mời cho thợ Tuấn.
set role authenticated;
set request.jwt.claim.sub = '11111111-1111-1111-1111-111111111111';

select tao_nhom();
\gset

do $$ begin
  if vai_cua_toi() is distinct from 'chu' then
    raise exception 'FAIL: người lập nhóm phải là chủ';
  end if;
  raise notice 'OK  chủ lập được nhóm';
end $$;

-- Gọi lại lần nữa không được sinh thêm nhóm: người dùng bấm hai lần là chuyện thường.
do $$
declare cu uuid := nhom_cua_toi();
begin
  perform tao_nhom();
  if nhom_cua_toi() is distinct from cu then
    raise exception 'FAIL: bấm lập nhóm hai lần lại ra hai nhóm';
  end if;
  if (select count(*) from thanh_vien) <> 1 then
    raise exception 'FAIL: bấm hai lần sinh thêm dòng thành viên';
  end if;
  raise notice 'OK  bấm lập nhóm hai lần vẫn một nhóm';
end $$;

-- Chủ đặt sổ của mình cho thợ Tuấn.
insert into so_cong (nhom_id, tho_id, nguon, tu_ngay, den_ngay, dongs)
values (nhom_cua_toi(), 'tho-tuan', 'chu', '2026-05-21', '2026-08-19',
        '[{"ngay":"2026-08-18","buoi":"Sang","soCong":1}]'::jsonb);

do $$ begin
  if (select count(*) from so_cong) <> 1 then
    raise exception 'FAIL: chủ không ghi được sổ của mình';
  end if;
  raise notice 'OK  chủ ghi được sổ trong nhóm mình';
end $$;

-- Khoảng ngày ngược phải bị chặn ngay ở database, không chờ app kiểm hộ.
do $$ begin
  begin
    insert into so_cong (nhom_id, tho_id, nguon, tu_ngay, den_ngay)
    values (nhom_cua_toi(), 'tho-tuan-2', 'chu', '2026-08-19', '2026-05-21');
    raise exception 'FAIL: database nhận sổ có khoảng ngày ngược';
  exception when check_violation then
    raise notice 'OK  chặn sổ có khoảng ngày ngược';
  end;
end $$;

-- Mã mời cho thợ Tuấn. Giữ lại để dùng ở phần dưới.
create temp table ma_cua_tuan as select phat_ma_moi('tho-tuan') as ma;

do $$ begin
  if (select length(ma) from ma_cua_tuan) <> 6 then
    raise exception 'FAIL: mã mời phải 6 ký tự';
  end if;
  raise notice 'OK  chủ phát được mã mời';
end $$;

-- Chủ cũng **không đọc được** bảng mã mời: cho đọc là máy nào cũng dò được mã của người khác.
do $$ begin
  if (select count(*) from ma_moi) <> 0 then
    raise exception 'FAIL: bảng mã mời đọc được — mã của người khác bị lộ';
  end if;
  raise notice 'OK  không ai select được bảng mã mời';
end $$;

-- ---------------------------------------------------------------------------
-- Thợ Tuấn đổi mã, rồi thử vượt quyền
-- ---------------------------------------------------------------------------

set request.jwt.claim.sub = '22222222-2222-2222-2222-222222222222';

do $$
declare ma text := (select ma from ma_cua_tuan);
begin
  perform doi_ma_moi(ma);
  if vai_cua_toi() is distinct from 'tho' or tho_cua_toi() is distinct from 'tho-tuan' then
    raise exception 'FAIL: đổi mã mời không vào đúng vai và đúng thợ';
  end if;
  raise notice 'OK  thợ đổi mã mời vào đúng nhóm, đúng người';
end $$;

-- Mã dùng một lần: người thứ hai cầm cùng mã ấy phải bị chặn.
do $$
declare ma text := (select ma from ma_cua_tuan);
begin
  begin
    perform doi_ma_moi(ma);
    raise exception 'FAIL: mã mời dùng lại được lần hai';
  exception when raise_exception then
    if sqlerrm like 'FAIL:%' then raise; end if;
    raise notice 'OK  mã mời chỉ dùng được một lần';
  end;
end $$;

-- Thợ đọc được sổ chủ gửi cho mình.
do $$ begin
  if (select count(*) from so_cong where nguon = 'chu') <> 1 then
    raise exception 'FAIL: thợ không đọc được sổ chủ gửi cho mình';
  end if;
  raise notice 'OK  thợ đọc được sổ chủ gửi cho mình';
end $$;

-- Thợ ghi được sổ của chính mình.
insert into so_cong (nhom_id, tho_id, nguon, tu_ngay, den_ngay, dongs)
values (nhom_cua_toi(), 'tho-tuan', 'tho', '2026-08-09', '2026-08-19',
        '[{"ngay":"2026-08-18","buoi":"Sang","soCong":1}]'::jsonb);

do $$ begin
  raise notice 'OK  thợ ghi được sổ của mình';
end $$;

-- **Chỗ quan trọng nhất cả file**: thợ không được ghi sổ mang danh chủ. Nếu lọt thì thợ tự
-- thêm công cho mình vào sổ chủ, và bảng lương của chủ đổi số mà chủ không biết.
do $$ begin
  begin
    insert into so_cong (nhom_id, tho_id, nguon, tu_ngay, den_ngay)
    values (nhom_cua_toi(), 'tho-tuan', 'chu', '2026-08-01', '2026-08-19');
    raise exception 'FAIL: thợ ghi được sổ mang danh chủ';
  exception when insufficient_privilege then
    raise notice 'OK  thợ không ghi được sổ mang danh chủ';
  end;
end $$;

-- Thợ cũng không sửa được sổ chủ đã có.
do $$
declare so_dong int;
begin
  update so_cong set dongs = '[]'::jsonb where nguon = 'chu';
  get diagnostics so_dong = row_count;
  if so_dong <> 0 then
    raise exception 'FAIL: thợ sửa được sổ của chủ';
  end if;
  raise notice 'OK  thợ không sửa được sổ của chủ';
end $$;

-- Và không được ghi sổ hộ thợ khác.
do $$ begin
  begin
    insert into so_cong (nhom_id, tho_id, nguon, tu_ngay, den_ngay)
    values (nhom_cua_toi(), 'tho-khac', 'tho', '2026-08-01', '2026-08-19');
    raise exception 'FAIL: thợ ghi được sổ hộ thợ khác';
  exception when insufficient_privilege then
    raise notice 'OK  thợ không ghi được sổ hộ thợ khác';
  end;
end $$;

-- ---------------------------------------------------------------------------
-- Người ngoài nhóm không thấy gì
-- ---------------------------------------------------------------------------

set request.jwt.claim.sub = '33333333-3333-3333-3333-333333333333';

do $$ begin
  if (select count(*) from so_cong) <> 0 then
    raise exception 'FAIL: người chưa vào nhóm nào vẫn đọc được sổ';
  end if;
  if (select count(*) from thanh_vien) <> 0 then
    raise exception 'FAIL: đọc được danh sách thành viên của nhóm khác';
  end if;
  raise notice 'OK  người ngoài nhóm không thấy gì';
end $$;

-- Chưa đăng nhập (khoá công khai trong tay nhưng không có phiên) cũng không thấy gì. Đây là
-- trường hợp có thật: ai gỡ app ra cũng có khoá ấy.
set request.jwt.claim.sub = '';

do $$ begin
  if (select count(*) from so_cong) <> 0 then
    raise exception 'FAIL: chưa đăng nhập vẫn đọc được sổ';
  end if;
  raise notice 'OK  chưa đăng nhập thì không thấy gì';
end $$;

-- ---------------------------------------------------------------------------
-- Chủ vẫn thấy đủ hai sổ của thợ trong nhóm mình
-- ---------------------------------------------------------------------------

set request.jwt.claim.sub = '11111111-1111-1111-1111-111111111111';

do $$ begin
  if (select count(*) from so_cong) <> 2 then
    raise exception 'FAIL: chủ phải thấy cả sổ mình gửi và sổ thợ gửi lên';
  end if;
  raise notice 'OK  chủ thấy cả hai sổ — đối chiếu được';
end $$;

-- ---------------------------------------------------------------------------
-- Bản sao lưu: sổ đầy đủ, **có tiền**, chỉ chính tài khoản ấy được thấy
-- ---------------------------------------------------------------------------
--
-- Đây là bảng duy nhất trong file có tiền trong đó, nên phần dưới là phần đáng soát nhất:
-- lọt một dòng ở đây là tiền công của cả cửa hàng mở cho mọi máy thợ đọc.

set request.jwt.claim.sub = '11111111-1111-1111-1111-111111111111';
set request.jwt.claims = '{"is_anonymous": false}';

insert into sao_luu (user_id, ngay, goi)
values (auth.uid(), '2026-08-19',
        '{"app":"cham-cong","phienBan":1,"taoLuc":"2026-08-19T09:00:00Z",
          "duLieu":{"thos":[{"id":"tho-tuan","mocLuong":[{"tuNgay":"2026-01-01","tienMotCong":300000}]}]}}'::jsonb);

do $$ begin
  if (select count(*) from sao_luu) <> 1 then
    raise exception 'FAIL: chủ không ghi hoặc không đọc được bản sao lưu của mình';
  end if;
  raise notice 'OK  chủ ghi và đọc được bản sao lưu của mình';
end $$;

-- Ghi lần thứ hai trong ngày là **ghi đè**, không sinh hàng thứ hai.
insert into sao_luu (user_id, ngay, goi)
values (auth.uid(), '2026-08-19', '{"app":"cham-cong","phienBan":1,"duLieu":{}}'::jsonb)
on conflict (user_id, ngay) do update set goi = excluded.goi, sua_luc = now();

do $$ begin
  if (select count(*) from sao_luu) <> 1 then
    raise exception 'FAIL: sao lưu hai lần trong ngày lại thành hai hàng';
  end if;
  raise notice 'OK  mỗi ngày đúng một hàng, ghi đè lên nhau';
end $$;

-- Gói phải là một object. Ràng buộc này chặn ngay ở database chứ không chờ app kiểm hộ.
do $$ begin
  begin
    insert into sao_luu (user_id, ngay, goi) values (auth.uid(), '2026-08-18', '[1,2,3]'::jsonb);
    raise exception 'FAIL: database nhận gói sao lưu không phải object';
  exception when check_violation then
    raise notice 'OK  chặn gói sao lưu không đúng dạng';
  end;
end $$;

-- **Chỗ quan trọng nhất của bảng này**: thợ cùng nhóm không thấy một chữ nào trong đó. Sổ này
-- có mốc lương, ứng tiền, kỳ đã chốt — tức là có tiền của tất cả mọi người.
set request.jwt.claim.sub = '22222222-2222-2222-2222-222222222222';

do $$ begin
  if (select count(*) from sao_luu) <> 0 then
    raise exception 'FAIL: thợ cùng nhóm đọc được bản sao lưu của chủ — tiền công bị lộ';
  end if;
  raise notice 'OK  thợ cùng nhóm không thấy bản sao lưu của chủ';
end $$;

-- Không thấy thì cũng không xoá được, và không ghi đè được.
do $$
declare so_dong int;
begin
  delete from sao_luu;
  get diagnostics so_dong = row_count;
  if so_dong <> 0 then
    raise exception 'FAIL: thợ xoá được bản sao lưu của chủ';
  end if;

  update sao_luu set goi = '{}'::jsonb;
  get diagnostics so_dong = row_count;
  if so_dong <> 0 then
    raise exception 'FAIL: thợ ghi đè được bản sao lưu của chủ';
  end if;
  raise notice 'OK  thợ không xoá, không ghi đè được bản sao lưu của chủ';
end $$;

-- Tài khoản ẩn danh không được ghi bản nào, kể cả hàng của chính nó: tài khoản ấy chỉ sống
-- trong một cái điện thoại, nên sao lưu vào đó là nhân thêm một chỗ có tiền mà không cứu được
-- ai. App cũng chặn, nhưng app thì sửa được mà database thì không.
set request.jwt.claims = '{"is_anonymous": true}';

do $$ begin
  begin
    insert into sao_luu (user_id, ngay, goi)
    values (auth.uid(), '2026-08-17', '{"app":"cham-cong","phienBan":1,"duLieu":{}}'::jsonb);
    raise exception 'FAIL: tài khoản ẩn danh ghi được bản sao lưu';
  exception when insufficient_privilege then
    raise notice 'OK  tài khoản ẩn danh không ghi được bản sao lưu';
  end;
end $$;

-- Chưa đăng nhập: khoá công khai trong tay cũng không thấy gì.
set request.jwt.claim.sub = '';
set request.jwt.claims = '{}';

do $$ begin
  if (select count(*) from sao_luu) <> 0 then
    raise exception 'FAIL: chưa đăng nhập vẫn đọc được bản sao lưu';
  end if;
  raise notice 'OK  chưa đăng nhập thì không thấy bản sao lưu nào';
end $$;

-- Còn chủ thì vẫn thấy đúng bản của mình — đó là cả mục đích của bảng này.
set request.jwt.claim.sub = '11111111-1111-1111-1111-111111111111';
set request.jwt.claims = '{"is_anonymous": false}';

do $$ begin
  if (select count(*) from sao_luu where ngay = '2026-08-19') <> 1 then
    raise exception 'FAIL: chủ không lấy lại được bản sao lưu của mình';
  end if;
  raise notice 'OK  chủ lấy lại được bản của mình — đổi máy là lấy sổ về được';
end $$;

reset role;
\echo 'Tất cả điều kiện phân quyền đều đạt.'
