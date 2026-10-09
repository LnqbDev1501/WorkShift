-- Apply before using Payroll correction APIs. This script does not change historical amounts.
BEGIN;
-- Remove single-source UNIQUE constraints/indexes as well as the named indexes below.
-- Otherwise an old scaffolded constraint can still prevent copying a source into V2.
DO $$
DECLARE item RECORD;
BEGIN
    FOR item IN
        SELECT c.conname
        FROM pg_constraint c
        JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1]
        WHERE c.conrelid = 'public.payroll_details'::regclass
          AND c.contype = 'u' AND array_length(c.conkey, 1) = 1
          AND a.attname IN ('reward_penalty_id', 'attendance_penalty_id')
    LOOP
        EXECUTE format('ALTER TABLE public.payroll_details DROP CONSTRAINT %I', item.conname);
    END LOOP;
    FOR item IN
        SELECT n.nspname, i.relname
        FROM pg_index x
        JOIN pg_class i ON i.oid = x.indexrelid
        JOIN pg_namespace n ON n.oid = i.relnamespace
        JOIN pg_attribute a ON a.attrelid = x.indrelid AND a.attnum = x.indkey[0]
        WHERE x.indrelid = 'public.payroll_details'::regclass
          AND x.indisunique AND x.indnkeyatts = 1
          AND a.attname IN ('reward_penalty_id', 'attendance_penalty_id')
    LOOP
        EXECUTE format('DROP INDEX %I.%I', item.nspname, item.relname);
    END LOOP;
END $$;
DROP INDEX IF EXISTS public.uq_payroll_detail_adjustment;
DROP INDEX IF EXISTS public.uq_payroll_detail_attendance_penalty;
CREATE UNIQUE INDEX uq_payroll_detail_adjustment
    ON public.payroll_details (payroll_id, reward_penalty_id)
    WHERE reward_penalty_id IS NOT NULL;
CREATE UNIQUE INDEX uq_payroll_detail_attendance_penalty
    ON public.payroll_details (payroll_id, attendance_penalty_id)
    WHERE attendance_penalty_id IS NOT NULL;
COMMIT;
