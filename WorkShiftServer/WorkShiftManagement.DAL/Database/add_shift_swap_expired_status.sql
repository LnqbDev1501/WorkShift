-- Run once before deploying shift swap expiry support (safe to rerun).
BEGIN;
ALTER TABLE public.shift_swap_requests
    DROP CONSTRAINT IF EXISTS shift_swap_requests_status_check;
ALTER TABLE public.shift_swap_requests
    ADD CONSTRAINT shift_swap_requests_status_check
    CHECK (status IN ('PENDING', 'ACCEPTED', 'REJECTED', 'CANCELLED', 'EXPIRED'));

-- Reconcile existing pending requests using the earlier of the two shift starts.
UPDATE public.shift_swap_requests AS r
SET status = 'EXPIRED', updated_at = now()
FROM public.shift_assignments AS a, public.shift_assignments AS b,
     public.work_shifts AS s, public.work_shifts AS t
WHERE r.status = 'PENDING'
  AND a.assignment_id = r.source_assignment_id AND b.assignment_id = r.target_assignment_id
  AND s.shift_id = a.shift_id AND t.shift_id = b.shift_id
  AND LEAST(s.scheduled_start_at, t.scheduled_start_at) <= now();
COMMIT;
