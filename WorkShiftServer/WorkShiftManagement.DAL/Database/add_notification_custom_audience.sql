ALTER TABLE public.notifications
    ADD COLUMN IF NOT EXISTS target_roles varchar(20)[] NOT NULL DEFAULT ARRAY[]::varchar(20)[];

ALTER TABLE public.notifications
    DROP CONSTRAINT IF EXISTS ck_notifications_target_type,
    DROP CONSTRAINT IF EXISTS ck_notifications_target_scope,
    DROP CONSTRAINT IF EXISTS notifications_target_type_check,
    DROP CONSTRAINT IF EXISTS notifications_target_scope_check;

ALTER TABLE public.notifications
    ADD CONSTRAINT ck_notifications_target_type
    CHECK (target_type IN ('All', 'Staff', 'Manager', 'Accountant', 'Region', 'Branch', 'Custom')),
    ADD CONSTRAINT ck_notifications_target_scope
    CHECK (
        (target_type = 'Region' AND target_region_id IS NOT NULL AND target_branch_id IS NULL AND cardinality(target_roles) = 0)
        OR (target_type = 'Branch' AND target_branch_id IS NOT NULL AND target_region_id IS NULL AND cardinality(target_roles) = 0)
        OR (target_type IN ('All', 'Staff', 'Manager', 'Accountant') AND target_region_id IS NULL AND target_branch_id IS NULL AND cardinality(target_roles) = 0)
        OR (target_type = 'Custom' AND cardinality(target_roles) > 0 AND target_roles <@ ARRAY['STAFF', 'MANAGER', 'ACCOUNTANT']::varchar(20)[])
    );
