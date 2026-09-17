-- DASHBOARD DEMO: PostgreSQL / Supabase, development database only.
-- Run this file in the SQL editor of the database used by the API.
-- Prerequisites: current application schema; at least one public STAFF user
-- with staff_salary_history, and one ADMIN user. No auth.users changes.
-- Existing user/role counts are preserved. Creates 1 region, 3 branches,
-- 3 payroll periods/payrolls, and 7 attendance statuses for today's VN date.
-- Safe to rerun on the same day: the whole seed is skipped after success.
-- On another day it creates a new daily sample. No existing rows are updated.
BEGIN;
SET LOCAL TIME ZONE 'Asia/Ho_Chi_Minh';
SELECT pg_advisory_xact_lock(91726001);
DO $$
DECLARE
    d date := current_date;
    prefix text := 'DEMO-DASH-' || to_char(current_date, 'YYYYMMDD');
    actor uuid;
    staff public.users%ROWTYPE;
    salary public.staff_salary_history%ROWTYPE;
    rid integer; bid integer; pid integer; tid integer;
    template_id integer; week_id integer; shift_id_new integer;
    assignment_id_new integer; rule_id integer;
    branch_name text; period_status text; payroll_status text;
    attendance_status text;
    shift_start timestamptz; shift_end timestamptz;
    i integer;
    seq_target record;
    seq_name text;
    max_id bigint;
    seq_value bigint;
BEGIN
    IF EXISTS (SELECT 1 FROM public.regions WHERE region_code = prefix) THEN
        RAISE NOTICE 'Dashboard demo for % already exists; nothing inserted.', d;
        RETURN;
    END IF;
    SELECT u.user_id INTO actor FROM public.users u JOIN public.roles r USING(role_id)
        WHERE upper(r.role_name) = 'ADMIN' AND u.status = 'ACTIVE'
        ORDER BY u.user_id LIMIT 1;
    SELECT u.* INTO staff FROM public.users u JOIN public.roles r USING(role_id)
        WHERE upper(r.role_name) = 'STAFF' AND u.status = 'ACTIVE'
        AND EXISTS (SELECT 1 FROM public.staff_salary_history s WHERE s.user_id = u.user_id)
        ORDER BY u.user_id LIMIT 1;
    IF actor IS NULL OR staff.user_id IS NULL THEN
        RAISE EXCEPTION 'Need one active ADMIN and one active STAFF with staff_salary_history. Seed rolls back without changes.';
    END IF;
    SELECT * INTO salary FROM public.staff_salary_history
        WHERE user_id = staff.user_id ORDER BY effective_from DESC, salary_history_id DESC LIMIT 1;

    -- Imported/manual IDs can leave identity sequences behind table contents.
    -- Lock all affected tables before reading maxima. Never move a sequence back.
    -- Sequence advances are not rolled back by PostgreSQL; gaps are harmless.
    LOCK TABLE public.regions, public.branches, public.payroll_periods,
        public.timesheets, public.payrolls, public.shift_templates,
        public.work_schedule_weeks, public.attendance_rules,
        public.work_shifts, public.shift_assignments, public.attendances
        IN SHARE ROW EXCLUSIVE MODE;
    FOR seq_target IN
        SELECT * FROM (VALUES
            ('regions', 'region_id'), ('branches', 'branch_id'),
            ('payroll_periods', 'payroll_period_id'), ('timesheets', 'timesheet_id'),
            ('payrolls', 'payroll_id'), ('shift_templates', 'shift_template_id'),
            ('work_schedule_weeks', 'schedule_week_id'), ('attendance_rules', 'attendance_rule_id'),
            ('work_shifts', 'shift_id'), ('shift_assignments', 'assignment_id'),
            ('attendances', 'attendance_id')
        ) AS targets(table_name, column_name)
    LOOP
        seq_name := pg_get_serial_sequence('public.' || seq_target.table_name, seq_target.column_name);
        IF seq_name IS NULL THEN
            RAISE EXCEPTION 'Missing identity/serial sequence for public.%.%', seq_target.table_name, seq_target.column_name;
        END IF;
        EXECUTE format('SELECT max(%I) FROM public.%I', seq_target.column_name, seq_target.table_name) INTO max_id;
        EXECUTE format('SELECT last_value FROM %s', seq_name::regclass) INTO seq_value;
        IF max_id IS NOT NULL AND max_id >= seq_value THEN
            PERFORM setval(seq_name::regclass, max_id, true);
        END IF;
    END LOOP;
    INSERT INTO public.regions(region_code,region_name,description)
    VALUES(prefix,prefix || ' Test region','Dashboard demo; synthetic data') RETURNING region_id INTO rid;
    FOR i IN 1..3 LOOP
        branch_name := prefix || ' Branch ' || i;
        INSERT INTO public.branches(branch_code,branch_name,region_id,address,status)
        VALUES(prefix || '-' || i,branch_name,rid,'Demo address',CASE WHEN i=3 THEN 'INACTIVE' ELSE 'ACTIVE' END)
        RETURNING branch_id INTO bid;
        period_status := (ARRAY['SUBMITTED','APPROVED','REVISION_REQUIRED'])[i];
        payroll_status := (ARRAY['SUBMITTED','APPROVED','REJECTED'])[i];
        INSERT INTO public.payroll_periods(branch_id,branch_name_snapshot,period_code,period_name,
            start_date,end_date,status,created_by,calculated_by,calculated_at,submitted_by,submitted_at,approved_by,approved_at)
        VALUES(bid,branch_name,prefix || '-' || i,'Demo ' || period_status,
            date_trunc('month',d)::date,d,period_status,actor,actor,now(),actor,now(),
            CASE WHEN i=2 THEN actor END,CASE WHEN i=2 THEN now() END)
        RETURNING payroll_period_id INTO pid;
        INSERT INTO public.timesheets(user_id,payroll_period_id,total_assigned_shifts,completed_shift_count,
            total_scheduled_hours,total_work_hours,status,generated_at)
        VALUES(staff.user_id,pid,1,1,8,8,'GENERATED',now()) RETURNING timesheet_id INTO tid;
        INSERT INTO public.payrolls(user_id,payroll_period_id,timesheet_id,branch_id,salary_history_id,
            employee_full_name_snapshot,employee_position_snapshot,branch_name_snapshot,hourly_rate_snapshot,
            total_work_hours,work_pay,gross_salary,net_salary,status,calculated_at,rejected_by,rejected_at,reject_reason)
        VALUES(staff.user_id,pid,tid,bid,salary.salary_history_id,staff.full_name,'Demo Staff',branch_name,
            salary.hourly_rate,8,8*salary.hourly_rate,8*salary.hourly_rate,8*salary.hourly_rate,payroll_status,now(),
            CASE WHEN i=3 THEN actor END,CASE WHEN i=3 THEN now() END,CASE WHEN i=3 THEN 'Demo: verify hours' END);
    END LOOP;
    -- Attendance uses a demo branch; no real employee assignment is modified.
    SELECT branch_id, b.branch_name INTO bid,branch_name FROM public.branches b WHERE branch_code=prefix || '-1';
    INSERT INTO public.shift_templates(shift_name,start_time,end_time,break_minutes)
        VALUES(prefix || ' Shift','08:00','09:00',0) RETURNING shift_template_id INTO template_id;
    INSERT INTO public.work_schedule_weeks(branch_id,week_start_date,week_end_date,registration_deadline,status,created_by)
        VALUES(bid,date_trunc('week',d)::date,date_trunc('week',d)::date+6,d::timestamptz,'COMPLETED',actor)
        RETURNING schedule_week_id INTO week_id;
    INSERT INTO public.attendance_rules(branch_id,rule_name,require_gps,require_face_verification,effective_from,status,created_by)
        VALUES(bid,prefix || ' Rule',false,false,d,'ACTIVE',actor) RETURNING attendance_rule_id INTO rule_id;
    FOR i IN 1..7 LOOP
        attendance_status := (ARRAY['PRESENT','LATE','EARLY_LEAVE','LATE_AND_EARLY_LEAVE','ABSENT','INCOMPLETE','IN_PROGRESS'])[i];
        shift_start := d::timestamptz + (i+6)*interval '1 hour';
        shift_end := shift_start + interval '1 hour';
        INSERT INTO public.work_shifts(schedule_week_id,shift_template_id,shift_name_snapshot,
            scheduled_start_at,scheduled_end_at,required_employees,created_by,status)
        VALUES(week_id,template_id,prefix || ' Slot ' || i,shift_start,shift_end,1,actor,'SCHEDULED')
        RETURNING shift_id INTO shift_id_new;
        INSERT INTO public.shift_assignments(shift_id,user_id,assignment_source,assigned_by)
        VALUES(shift_id_new,staff.user_id,'MANAGER_ASSIGNMENT',actor) RETURNING assignment_id INTO assignment_id_new;
        INSERT INTO public.attendances(assignment_id,attendance_rule_id,rule_snapshot_json,employee_full_name_snapshot,
            shift_start_at_snapshot,shift_end_at_snapshot,branch_name_snapshot,status,check_in_time,check_out_time,
            actual_working_minutes,working_hours,total_late_minutes,chargeable_late_minutes,
            total_early_leave_minutes,chargeable_early_leave_minutes)
        VALUES(assignment_id_new,rule_id,'{"demo":true}'::jsonb,staff.full_name,shift_start,shift_end,branch_name,attendance_status,
            CASE WHEN i<>5 THEN shift_start + CASE WHEN i IN (2,4) THEN interval '10 minutes' ELSE interval '0' END END,
            CASE WHEN i<=4 THEN shift_end - CASE WHEN i IN (3,4) THEN interval '10 minutes' ELSE interval '0' END END,
            CASE WHEN i<=4 THEN 60-CASE WHEN i IN (2,4) THEN 10 ELSE 0 END-CASE WHEN i IN (3,4) THEN 10 ELSE 0 END ELSE 0 END,
            CASE WHEN i<=4 THEN (60-CASE WHEN i IN (2,4) THEN 10 ELSE 0 END-CASE WHEN i IN (3,4) THEN 10 ELSE 0 END)/60.0 ELSE 0 END,
            CASE WHEN i IN (2,4) THEN 10 ELSE 0 END,CASE WHEN i IN (2,4) THEN 10 ELSE 0 END,
            CASE WHEN i IN (3,4) THEN 10 ELSE 0 END,CASE WHEN i IN (3,4) THEN 10 ELSE 0 END);
    END LOOP;
    RAISE NOTICE 'Created demo %. Expected additions: 1 region, 3 branches (2 active), 1 pending payroll, 7 attendances today.',prefix;
END $$;
-- Verify today's attendance chart and demo payroll periods.
SELECT status,count(*) FROM public.attendances
WHERE shift_start_at_snapshot >= current_date::timestamptz
  AND shift_start_at_snapshot < (current_date+1)::timestamptz
  AND branch_name_snapshot LIKE 'DEMO-DASH-' || to_char(current_date,'YYYYMMDD') || '%'
GROUP BY status ORDER BY status;
SELECT period_code,status,start_date,end_date FROM public.payroll_periods
WHERE period_code LIKE 'DEMO-DASH-%' ORDER BY end_date DESC,payroll_period_id DESC;
COMMIT;
