-- Migration: Fix ck_payroll_deduction to support carry_forward_deduction and deferred_deduction_amount
-- Problem: The legacy check constraint only checked total_deduction = total_penalty + attendance_deduction.
-- When deductions exceed gross salary (producing deferred_deduction_amount) or when previous period deductions
-- are carried forward (carry_forward_deduction), the legacy constraint caused a 23514 check constraint violation.
-- This script aligns the constraint with the actual business accounting formula.

BEGIN;

ALTER TABLE public.payrolls DROP CONSTRAINT IF EXISTS ck_payroll_deduction;

ALTER TABLE public.payrolls ADD CONSTRAINT ck_payroll_deduction
    CHECK (total_deduction = total_penalty + attendance_deduction + carry_forward_deduction - deferred_deduction_amount);

COMMIT;
