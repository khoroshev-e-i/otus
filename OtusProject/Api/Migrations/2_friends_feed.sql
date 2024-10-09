DO
$$
BEGIN
        IF
NOT EXISTS (SELECT 1 FROM pg_tables where schemaname = 'public' and tablename = 'user_friend') THEN
CREATE table public.user_friend
(
    id              varchar(100) not null,
    user_id         varchar(100) not null,
    friend_id       varchar(100) not null
);
END IF;
        IF
NOT EXISTS (SELECT 1 FROM pg_tables where schemaname = 'public' and tablename = 'user_post') THEN
CREATE table public.user_post
(
    id              varchar(100) not null,
    user_id         varchar(100) not null,
    post_body       text not null,
    last_update     timestamptz not null 
);
                
END IF;
END
$$;