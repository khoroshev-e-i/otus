DO
$$
BEGIN
        IF
NOT EXISTS (SELECT 1 FROM pg_tables where schemaname = 'public' and tablename = 'users') THEN
CREATE table public.users
(
    id          varchar(100) not null,
    username    varchar(100) not null,
    first_name  varchar(50) not null,
    second_name varchar(100) not null,
    birthdate   date not null,
    biography   varchar(500) null,
    city        varchar(50) null,
    password        varchar(100) null
);
                
END IF;
END
$$;