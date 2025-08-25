create schema if not exists discount;

create table if not exists discount.discount_code (
  code        varchar(8) primary key,
  created_at  timestamptz not null default now(),
  used        boolean not null default false,
  used_at     timestamptz null,

  constraint chk_code_len check (char_length(code) between 7 and 8)
);

create index if not exists ix_discount_code_unused
  on discount.discount_code (code) where used = false;
