ALTER TABLE public.notifications
    ADD COLUMN target_type varchar(20) NOT NULL DEFAULT 'All',
    ADD COLUMN target_region_id integer NULL REFERENCES public.regions(region_id) ON DELETE RESTRICT,
    ADD COLUMN target_branch_id integer NULL REFERENCES public.branches(branch_id) ON DELETE RESTRICT,
    ADD COLUMN channels varchar(20)[] NOT NULL DEFAULT ARRAY['InApp'];

ALTER TABLE public.notifications
    ADD CONSTRAINT ck_notifications_target_type
    CHECK (target_type IN ('All', 'Staff', 'Manager', 'Accountant', 'Region', 'Branch'));

ALTER TABLE public.notifications
    ADD CONSTRAINT ck_notifications_target_scope
    CHECK (
        (target_type = 'Region' AND target_region_id IS NOT NULL AND target_branch_id IS NULL)
        OR (target_type = 'Branch' AND target_branch_id IS NOT NULL AND target_region_id IS NULL)
        OR (target_type NOT IN ('Region', 'Branch') AND target_region_id IS NULL AND target_branch_id IS NULL)
    );
