-- Safe to rerun. Run before deploying the normalized-name uniqueness check.
BEGIN;

-- Block concurrent writes between cleanup and index creation; reads remain available.
LOCK TABLE public.employee_types IN SHARE ROW EXCLUSIVE MODE;

DO $$
DECLARE
    duplicate_row record;
    candidate text;
    suffix text;
    attempt integer;
BEGIN
    -- Preserve IDs, references and statuses. Keep the lowest ID in each normalized-name group.
    FOR duplicate_row IN
        SELECT employee_type_id, type_name FROM (
            SELECT employee_type_id, type_name,
                   row_number() OVER (PARTITION BY lower(trim(type_name)) ORDER BY employee_type_id) AS position
            FROM public.employee_types
        ) AS ranked
        WHERE position > 1
        ORDER BY employee_type_id
    LOOP
        attempt := 0;
        LOOP
            suffix := CASE WHEN attempt = 0
                THEN format(' (duplicate #%s)', duplicate_row.employee_type_id)
                ELSE format(' (duplicate #%s-%s)', duplicate_row.employee_type_id, attempt) END;
            candidate := left(trim(duplicate_row.type_name), 100 - char_length(suffix)) || suffix;
            EXIT WHEN NOT EXISTS (
                SELECT 1 FROM public.employee_types
                WHERE lower(trim(type_name)) = lower(trim(candidate))
                  AND employee_type_id <> duplicate_row.employee_type_id
            );
            attempt := attempt + 1;
        END LOOP;

        UPDATE public.employee_types
        SET type_name = candidate, updated_at = now()
        WHERE employee_type_id = duplicate_row.employee_type_id;

        INSERT INTO public.audit_logs (action, table_name, record_id, old_value, new_value)
        VALUES ('DEDUPLICATE_EMPLOYEE_TYPE_NAME', 'employee_types', duplicate_row.employee_type_id::text,
                jsonb_build_object('type_name', duplicate_row.type_name),
                jsonb_build_object('type_name', candidate));
    END LOOP;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_employee_types_normalized_type_name
    ON public.employee_types (lower(trim(type_name)));

COMMIT;
