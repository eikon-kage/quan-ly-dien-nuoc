-- Thiết lập database cho hộp thư đối chiếu chấm công.
--
-- Dán cả file này vào Supabase → SQL Editor → Run. Chạy lại lần nữa cũng không sao:
-- mọi lệnh đều có `if not exists` hoặc `create or replace`.
--
-- ĐIỀU QUAN TRỌNG NHẤT: app gọi thẳng vào database, không qua máy chủ trung gian nào. Khoá
-- công khai nằm sẵn trong app, ai gỡ app ra cũng đọc được. Vì vậy **RLS là ổ khoá duy nhất**.
-- Quên bật RLS trên một bảng là bảng đó công khai với cả internet.
--
-- Mô hình: mỗi máy vẫn giữ sổ thật trong máy. Bảng dưới đây chỉ là **hộp thư** — chỗ hai bên
-- đặt sổ cho nhau đọc rồi tự đối chiếu. Không có gì tự trộn, xem docs/chamcong-doi-chieu.md.

-- ---------------------------------------------------------------------------
-- 1. Ai thuộc nhóm nào, vai gì
-- ---------------------------------------------------------------------------

create table if not exists thanh_vien (
  user_id  uuid primary key references auth.users on delete cascade,
  nhom_id  uuid not null,
  vai      text not null check (vai in ('chu', 'tho')),
  -- Máy thợ: id của thợ trong sổ chủ, nhận qua mã mời. Máy chủ để trống.
  tho_id   text,
  tao_luc  timestamptz not null default now(),
  -- Thợ thì buộc phải biết mình là ai; chủ thì không mang tho_id nào.
  constraint tho_phai_co_id check ((vai = 'tho') = (tho_id is not null))
);

create index if not exists thanh_vien_nhom on thanh_vien (nhom_id);

-- ---------------------------------------------------------------------------
-- 2. Hộp thư: mỗi (nhóm, thợ, bên gửi) đúng một hàng, ghi đè mãi lên nó
-- ---------------------------------------------------------------------------
--
-- Không giữ lịch sử ở đây: sổ là bản chụp toàn khoảng, bản mới nói đủ những gì bản cũ nói.
-- Lịch sử đã có bên sao lưu theo ngày lo.

create table if not exists so_cong (
  nhom_id  uuid not null,
  tho_id   text not null,
  nguon    text not null check (nguon in ('chu', 'tho')),
  -- Tên thợ theo sổ bên gửi. Máy thợ lấy tên của mình từ đây chứ không bắt thợ tự gõ —
  -- chủ mới là bên đặt tên.
  ten_tho  text not null default '',
  tu_ngay  date not null,
  den_ngay date not null,
  -- Đúng mảng `dongs` của app: [{"ngay","buoi","soCong","daChot"}]. Không có tiền ở đây,
  -- và đó là chủ ý: máy thợ chỉ được thấy số công.
  dongs    jsonb not null default '[]'::jsonb,
  tao_luc  timestamptz not null default now(),
  primary key (nhom_id, tho_id, nguon),
  -- Khoảng ngày ngược là sổ vô nghĩa, và hàm đối chiếu sẽ ra kết quả sai lặng lẽ.
  constraint khoang_ngay_hop_le check (tu_ngay <= den_ngay),
  constraint dongs_phai_la_mang check (jsonb_typeof(dongs) = 'array')
);

-- ---------------------------------------------------------------------------
-- 3. Mã mời: chủ phát ra, thợ đổi lấy chỗ trong nhóm
-- ---------------------------------------------------------------------------

create table if not exists ma_moi (
  ma       text primary key,
  nhom_id  uuid not null,
  tho_id   text not null,
  het_han  timestamptz not null,
  da_dung  boolean not null default false
);

-- ---------------------------------------------------------------------------
-- 4. Bật RLS. Đây là phần chặn thật — đừng bỏ dòng nào.
-- ---------------------------------------------------------------------------

alter table thanh_vien enable row level security;
alter table so_cong    enable row level security;
alter table ma_moi     enable row level security;

-- `stable` + `security definer` để policy gọi được mà không đệ quy vào chính RLS của
-- thanh_vien. Không có nó thì policy đọc thanh_vien lại kích hoạt policy của thanh_vien.
create or replace function nhom_cua_toi()
returns uuid language sql stable security definer set search_path = public as $$
  select nhom_id from thanh_vien where user_id = auth.uid()
$$;

create or replace function vai_cua_toi()
returns text language sql stable security definer set search_path = public as $$
  select vai from thanh_vien where user_id = auth.uid()
$$;

create or replace function tho_cua_toi()
returns text language sql stable security definer set search_path = public as $$
  select tho_id from thanh_vien where user_id = auth.uid()
$$;

-- Ai cũng chỉ đọc được dòng thành viên của chính mình.
drop policy if exists thanh_vien_doc_cua_minh on thanh_vien;
create policy thanh_vien_doc_cua_minh on thanh_vien
  for select using (user_id = auth.uid());

-- Sổ công — chủ: toàn quyền trong nhóm mình.
drop policy if exists so_cong_chu on so_cong;
create policy so_cong_chu on so_cong
  for all using (vai_cua_toi() = 'chu' and nhom_id = nhom_cua_toi())
  with check (vai_cua_toi() = 'chu' and nhom_id = nhom_cua_toi());

-- Sổ công — thợ: **đọc** đúng hai dòng của chính mình (sổ chủ gửi xuống và sổ mình gửi lên).
-- Thợ khác cùng nhóm cũng không thấy, nên không ai biết công của ai.
drop policy if exists so_cong_tho_doc on so_cong;
create policy so_cong_tho_doc on so_cong
  for select using (
    vai_cua_toi() = 'tho' and nhom_id = nhom_cua_toi() and tho_id = tho_cua_toi()
  );

-- Sổ công — thợ: **ghi** đúng một dòng, của mình và mang nguồn 'tho'. Không sửa được sổ chủ,
-- không sửa được sổ thợ khác. Đây là chỗ chặn "thợ tự thêm công cho mình" ở tầng database.
drop policy if exists so_cong_tho_ghi on so_cong;
create policy so_cong_tho_ghi on so_cong
  for insert with check (
    vai_cua_toi() = 'tho'
    and nhom_id = nhom_cua_toi()
    and tho_id = tho_cua_toi()
    and nguon = 'tho'
  );

drop policy if exists so_cong_tho_sua on so_cong;
create policy so_cong_tho_sua on so_cong
  for update using (
    vai_cua_toi() = 'tho' and nhom_id = nhom_cua_toi() and tho_id = tho_cua_toi() and nguon = 'tho'
  )
  with check (
    vai_cua_toi() = 'tho' and nhom_id = nhom_cua_toi() and tho_id = tho_cua_toi() and nguon = 'tho'
  );

-- Bảng mã mời **không có policy đọc nào cả**: không ai select được nó, kể cả chủ. Đổi mã đi
-- qua hàm bên dưới. Nếu cho đọc thì máy thợ dò được mã của người khác.
drop policy if exists ma_moi_chu_phat on ma_moi;
create policy ma_moi_chu_phat on ma_moi
  for insert with check (vai_cua_toi() = 'chu' and nhom_id = nhom_cua_toi());

-- ---------------------------------------------------------------------------
-- 5. Ba hàm app gọi
-- ---------------------------------------------------------------------------

-- Máy chủ lần đầu: tạo nhóm mới, hoặc trả về nhóm đang có. Gọi lại nhiều lần không sinh
-- thêm nhóm — người dùng bấm hai lần là chuyện thường.
create or replace function tao_nhom()
returns thanh_vien language plpgsql security definer set search_path = public as $$
declare ket thanh_vien;
begin
  if auth.uid() is null then
    raise exception 'Chưa đăng nhập.';
  end if;

  select * into ket from thanh_vien where user_id = auth.uid();
  if found then
    return ket;
  end if;

  insert into thanh_vien (user_id, nhom_id, vai)
  values (auth.uid(), gen_random_uuid(), 'chu')
  returning * into ket;
  return ket;
end $$;

-- Máy chủ phát mã mời cho một thợ. Mã ngắn, viết hoa, dễ đọc qua điện thoại — bỏ hẳn chữ
-- O, I, L, số 0 và 1 vì đọc lên nghe giống nhau.
create or replace function phat_ma_moi(p_tho_id text, p_so_gio int default 72)
returns text language plpgsql security definer set search_path = public as $$
declare ma text;
begin
  if vai_cua_toi() is distinct from 'chu' then
    raise exception 'Chỉ máy chủ phát được mã mời.';
  end if;

  select string_agg(substr('ABCDEFGHJKMNPQRSTUVWXYZ23456789', (random() * 30)::int + 1, 1), '')
    into ma from generate_series(1, 6);

  insert into ma_moi (ma, nhom_id, tho_id, het_han)
  values (ma, nhom_cua_toi(), p_tho_id, now() + make_interval(hours => p_so_gio));

  return ma;
end $$;

-- Máy thợ đổi mã lấy chỗ trong nhóm. Mã dùng một lần và có hạn.
create or replace function doi_ma_moi(p_ma text)
returns thanh_vien language plpgsql security definer set search_path = public as $$
declare m ma_moi; ket thanh_vien;
begin
  if auth.uid() is null then
    raise exception 'Chưa đăng nhập.';
  end if;

  select * into m from ma_moi
   where ma = upper(trim(p_ma)) and not da_dung and het_han > now()
   for update;

  if not found then
    -- Cùng một câu cho cả ba trường hợp sai / hết hạn / đã dùng: nói rõ hơn là chỉ điểm cho
    -- người dò mã biết họ đang dò gần đúng.
    raise exception 'Mã mời không dùng được. Xin chủ phát mã mới.';
  end if;

  insert into thanh_vien (user_id, nhom_id, vai, tho_id)
  values (auth.uid(), m.nhom_id, 'tho', m.tho_id)
  on conflict (user_id) do update set nhom_id = m.nhom_id, vai = 'tho', tho_id = m.tho_id
  returning * into ket;

  update ma_moi set da_dung = true where ma = m.ma;
  return ket;
end $$;
