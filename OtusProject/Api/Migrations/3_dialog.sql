DO
$$
    BEGIN
        IF
            NOT EXISTS (SELECT 1 FROM pg_tables where schemaname = 'public' and tablename = 'dialog')
        THEN
            CREATE table public.dialog
            (
                id              varchar(100) not null,
                from_user       varchar(100) not null,
                to_user         varchar(100) not null,
                text            text         not null,
                last_updated    timestamptz  not null
            );
        END IF;
    END
$$;