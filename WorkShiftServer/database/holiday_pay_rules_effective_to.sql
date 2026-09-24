-- Run against the same database used by the API and EF scaffolding.
-- Adds the missing end date without changing existing start dates or payroll snapshots.
BEGIN;
ALTER TABLE public.holiday_pay_rules
    ADD COLUMN IF NOT EXISTS effective_to date;
COMMIT;

SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'holiday_pay_rules'
  AND column_name IN ('effective_from', 'effective_to', 'status');
