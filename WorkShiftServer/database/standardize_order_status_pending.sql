-- Standardize the order lifecycle to PENDING / COMPLETED / CANCELLED.
-- Replaces the legacy 'DRAFT' status that predates the payment-first flow
-- (staff now creates the order already paid and in PENDING).
-- Run against the same database used by the API.

BEGIN;

-- 1. Migrate any legacy rows that still carry the old 'DRAFT' status.
UPDATE public.orders SET status = 'PENDING' WHERE status = 'DRAFT';

-- 2. Change the column default.
ALTER TABLE public.orders ALTER COLUMN status SET DEFAULT 'PENDING';

-- 3. Drop any CHECK constraint on orders that still allows 'DRAFT'
--    (constraint name varies, so locate it by definition).
DO $$
DECLARE
    cname text;
BEGIN
    SELECT con.conname INTO cname
    FROM pg_constraint con
    JOIN pg_class rel ON rel.oid = con.conrelid
    JOIN pg_namespace ns ON ns.oid = rel.relnamespace
    WHERE ns.nspname = 'public'
      AND rel.relname = 'orders'
      AND con.contype = 'c'
      AND pg_get_constraintdef(con.oid) ILIKE '%''DRAFT''%'
    LIMIT 1;

    IF cname IS NOT NULL THEN
        EXECUTE format('ALTER TABLE public.orders DROP CONSTRAINT %I', cname);
    END IF;
END $$;

ALTER TABLE public.orders
    ADD CONSTRAINT orders_status_check
    CHECK (status IN ('PENDING', 'COMPLETED', 'CANCELLED'));

COMMIT;

SELECT DISTINCT status FROM public.orders ORDER BY status;
