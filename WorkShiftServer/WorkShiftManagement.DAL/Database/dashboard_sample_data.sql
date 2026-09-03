-- ============================================================
-- SAMPLE DATA FOR GET /api/admin/dashboard
-- Run this file in Supabase SQL Editor after Initial_schema.sql.
-- No existing user is required. For test purposes this script directly
-- inserts a Supabase Auth record when auth.users is empty.
-- All sample records use the DASH_TEST_ prefix where possible.
-- ============================================================

-- Bridge a Supabase Auth account into the application's public.users table.
-- The project schema currently has no auth.users -> public.users trigger.
DO $$
DECLARE
    v_auth_user_id uuid;
    v_auth_email text;
    v_admin_role_id integer;
BEGIN
    IF EXISTS (SELECT 1 FROM public.users) THEN
        RETURN;
    END IF;

    SELECT id, email
    INTO v_auth_user_id, v_auth_email
    FROM auth.users
    ORDER BY created_at
    LIMIT 1;

    IF v_auth_user_id IS NULL THEN
        v_auth_user_id := '10000000-0000-0000-0000-000000000001'::uuid;
        v_auth_email := 'dashboard.admin@workshift.test';

        INSERT INTO auth.users
            (instance_id, id, aud, role, email, encrypted_password,
             email_confirmed_at, raw_app_meta_data, raw_user_meta_data,
             created_at, updated_at, confirmation_token,
             email_change, email_change_token_new, recovery_token)
        VALUES
            ('00000000-0000-0000-0000-000000000000'::uuid,
             v_auth_user_id, 'authenticated', 'authenticated', v_auth_email,
             extensions.crypt('Test@123456', extensions.gen_salt('bf')),
             now(),
             '{"provider":"email","providers":["email"]}'::jsonb,
             '{"full_name":"Dashboard Test Admin"}'::jsonb,
             now(), now(), '', '', '', '')
        ON CONFLICT (id) DO NOTHING;

        RAISE NOTICE
            'Created test auth user: dashboard.admin@workshift.test / Test@123456';
    END IF;

    PERFORM setval(pg_get_serial_sequence('public.roles', 'role_id'),
        coalesce((SELECT max(role_id) FROM public.roles), 0) + 1, false);

    INSERT INTO public.roles (role_name, description, status)
    VALUES ('ADMIN', 'System administrator', 'ACTIVE')
    ON CONFLICT (role_name) DO NOTHING;

    SELECT role_id
    INTO v_admin_role_id
    FROM public.roles
    WHERE role_name = 'ADMIN';

    INSERT INTO public.users
        (user_id, role_id, full_name, gender, date_of_birth,
         address, identity_no, email, phone, status)
    VALUES
        (v_auth_user_id, v_admin_role_id, 'Dashboard Test Admin', 'OTHER',
         date '1995-01-01', 'Dashboard test address',
         'DASH' || substr(replace(v_auth_user_id::text, '-', ''), 1, 20),
         coalesce(v_auth_email, 'dashboard.' || substr(v_auth_user_id::text, 1, 8) || '@example.com'),
         '+849' || substr(replace(v_auth_user_id::text, '-', ''), 1, 9),
         'ACTIVE');

    RAISE NOTICE 'Created public.users profile for auth user %', v_auth_user_id;
END $$;

DO $$
DECLARE
    v_user_id uuid;
    v_region_north_id integer;
    v_region_south_id integer;
    v_branch_hcm_id integer;
    v_branch_hn_id integer;
    v_branch_inactive_id integer;
    v_template_id integer;
    v_schedule_id integer;
    v_rule_id integer;
    v_shift_id integer;
    v_assignment_id integer;
    v_contract_id integer;
    v_salary_id integer;
    v_period_id integer;
    v_timesheet_id integer;
    v_today date := (now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date;
    v_week_start date := date_trunc(
        'week',
        (now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date
    )::date;
    v_status text;
    v_index integer;
BEGIN
    -- Existing reference rows may have been inserted with explicit IDs.
    -- Realign identity sequences before generating any new IDs.
    PERFORM setval(pg_get_serial_sequence('public.roles', 'role_id'),
        coalesce((SELECT max(role_id) FROM public.roles), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.regions', 'region_id'),
        coalesce((SELECT max(region_id) FROM public.regions), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.branches', 'branch_id'),
        coalesce((SELECT max(branch_id) FROM public.branches), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.employee_requests', 'request_id'),
        coalesce((SELECT max(request_id) FROM public.employee_requests), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.shift_templates', 'shift_template_id'),
        coalesce((SELECT max(shift_template_id) FROM public.shift_templates), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.work_schedule_weeks', 'schedule_week_id'),
        coalesce((SELECT max(schedule_week_id) FROM public.work_schedule_weeks), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.attendance_rules', 'attendance_rule_id'),
        coalesce((SELECT max(attendance_rule_id) FROM public.attendance_rules), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.work_shifts', 'shift_id'),
        coalesce((SELECT max(shift_id) FROM public.work_shifts), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.shift_assignments', 'assignment_id'),
        coalesce((SELECT max(assignment_id) FROM public.shift_assignments), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.attendances', 'attendance_id'),
        coalesce((SELECT max(attendance_id) FROM public.attendances), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.employment_contracts', 'contract_id'),
        coalesce((SELECT max(contract_id) FROM public.employment_contracts), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.staff_salary_history', 'salary_history_id'),
        coalesce((SELECT max(salary_history_id) FROM public.staff_salary_history), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.payroll_periods', 'payroll_period_id'),
        coalesce((SELECT max(payroll_period_id) FROM public.payroll_periods), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.timesheets', 'timesheet_id'),
        coalesce((SELECT max(timesheet_id) FROM public.timesheets), 0) + 1, false);
    PERFORM setval(pg_get_serial_sequence('public.payrolls', 'payroll_id'),
        coalesce((SELECT max(payroll_id) FROM public.payrolls), 0) + 1, false);

    SELECT user_id
    INTO v_user_id
    FROM public.users
    WHERE deleted_at IS NULL
    ORDER BY created_at
    LIMIT 1;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'Unable to create or locate a public.users record.';
    END IF;

    -- Reference data used by dashboard counters.
    INSERT INTO public.roles (role_name, description, status)
    VALUES
        ('ADMIN', 'System administrator', 'ACTIVE'),
        ('MANAGER', 'Branch manager', 'ACTIVE'),
        ('ACCOUNTANT', 'Payroll accountant', 'ACTIVE'),
        ('STAFF', 'Branch staff', 'ACTIVE')
    ON CONFLICT (role_name) DO NOTHING;

    INSERT INTO public.regions
        (region_code, region_name, description, is_active)
    VALUES
        ('DASH_TEST_NORTH', 'Dashboard Test - North', 'Dashboard sample data', true)
    ON CONFLICT (region_code) DO UPDATE
    SET region_name = EXCLUDED.region_name,
        description = EXCLUDED.description,
        is_active = EXCLUDED.is_active
    RETURNING region_id INTO v_region_north_id;

    INSERT INTO public.regions
        (region_code, region_name, description, is_active)
    VALUES
        ('DASH_TEST_SOUTH', 'Dashboard Test - South', 'Dashboard sample data', true)
    ON CONFLICT (region_code) DO UPDATE
    SET region_name = EXCLUDED.region_name,
        description = EXCLUDED.description,
        is_active = EXCLUDED.is_active
    RETURNING region_id INTO v_region_south_id;

    INSERT INTO public.branches
        (branch_code, branch_name, region_id, address, latitude, longitude, status)
    VALUES
        ('DASH_TEST_HCM', 'Dashboard Test - Ho Chi Minh', v_region_south_id,
         '1 Nguyen Hue, Ho Chi Minh City', 10.776900, 106.700900, 'ACTIVE')
    ON CONFLICT (branch_code) DO UPDATE
    SET branch_name = EXCLUDED.branch_name,
        region_id = EXCLUDED.region_id,
        address = EXCLUDED.address,
        status = EXCLUDED.status
    RETURNING branch_id INTO v_branch_hcm_id;

    INSERT INTO public.branches
        (branch_code, branch_name, region_id, address, latitude, longitude, status)
    VALUES
        ('DASH_TEST_HN', 'Dashboard Test - Ha Noi', v_region_north_id,
         '1 Trang Tien, Ha Noi', 21.024500, 105.841200, 'ACTIVE')
    ON CONFLICT (branch_code) DO UPDATE
    SET branch_name = EXCLUDED.branch_name,
        region_id = EXCLUDED.region_id,
        address = EXCLUDED.address,
        status = EXCLUDED.status
    RETURNING branch_id INTO v_branch_hn_id;

    INSERT INTO public.branches
        (branch_code, branch_name, region_id, address, status)
    VALUES
        ('DASH_TEST_CLOSED', 'Dashboard Test - Inactive Branch', v_region_south_id,
         'Dashboard test address', 'INACTIVE')
    ON CONFLICT (branch_code) DO UPDATE
    SET branch_name = EXCLUDED.branch_name,
        region_id = EXCLUDED.region_id,
        address = EXCLUDED.address,
        status = EXCLUDED.status
    RETURNING branch_id INTO v_branch_inactive_id;

    -- Five recent requests: three PENDING and two already processed.
    INSERT INTO public.employee_requests
        (user_id, branch_id, request_type, reason, status, created_at, updated_at)
    VALUES
        (v_user_id, v_branch_hcm_id, 'LEAVE',
         'DASH_TEST request 1', 'PENDING', now() - interval '5 minutes', now() - interval '5 minutes'),
        (v_user_id, v_branch_hcm_id, 'LATE_ARRIVAL',
         'DASH_TEST request 2', 'APPROVED', now() - interval '10 minutes', now() - interval '10 minutes'),
        (v_user_id, v_branch_hn_id, 'EARLY_LEAVE',
         'DASH_TEST request 3', 'PENDING', now() - interval '15 minutes', now() - interval '15 minutes'),
        (v_user_id, v_branch_hn_id, 'SALARY_INCREASE',
         'DASH_TEST request 4', 'REJECTED', now() - interval '20 minutes', now() - interval '20 minutes'),
        (v_user_id, v_branch_hcm_id, 'LEAVE',
         'DASH_TEST request 5', 'PENDING', now() - interval '25 minutes', now() - interval '25 minutes');

    -- Scheduling prerequisites for today's attendance records.
    INSERT INTO public.shift_templates
        (shift_name, start_time, end_time, break_minutes, description, status)
    VALUES
        ('DASH_TEST_DAY_SHIFT', time '08:00', time '17:00', 60,
         'Dashboard sample shift', 'ACTIVE')
    ON CONFLICT (shift_name) DO UPDATE
    SET start_time = EXCLUDED.start_time,
        end_time = EXCLUDED.end_time,
        break_minutes = EXCLUDED.break_minutes,
        status = EXCLUDED.status
    RETURNING shift_template_id INTO v_template_id;

    INSERT INTO public.work_schedule_weeks
        (branch_id, week_start_date, week_end_date, registration_deadline,
         status, created_by)
    VALUES
        (v_branch_hcm_id, v_week_start, v_week_start + 6,
         (v_week_start::timestamp AT TIME ZONE 'Asia/Ho_Chi_Minh') - interval '1 day',
         'COMPLETED', v_user_id)
    ON CONFLICT (branch_id, week_start_date) DO UPDATE
    SET status = EXCLUDED.status
    RETURNING schedule_week_id INTO v_schedule_id;

    INSERT INTO public.attendance_rules
        (branch_id, rule_name, description, require_gps,
         require_face_verification, status, created_by)
    VALUES
        (v_branch_hcm_id, 'DASH_TEST_ATTENDANCE_RULE',
         'Dashboard sample attendance rule', false, false, 'DRAFT', v_user_id)
    RETURNING attendance_rule_id INTO v_rule_id;

    -- Produce one attendance for each possible dashboard status.
    FOR v_index IN 1..6 LOOP
        v_status := (ARRAY[
            'PRESENT', 'LATE', 'EARLY_LEAVE',
            'LATE_AND_EARLY_LEAVE', 'ABSENT', 'IN_PROGRESS'
        ])[v_index];

        INSERT INTO public.work_shifts
            (schedule_week_id, shift_template_id, shift_name_snapshot,
             scheduled_start_at, scheduled_end_at, break_minutes_snapshot,
             required_employees, note, created_by, status)
        VALUES
            (v_schedule_id, v_template_id,
             'DASH_TEST shift ' || v_index,
             ((v_today::text || ' ' || lpad((7 + v_index)::text, 2, '0') || ':00:00')::timestamp
                 AT TIME ZONE 'Asia/Ho_Chi_Minh'),
             ((v_today::text || ' ' || lpad((8 + v_index)::text, 2, '0') || ':00:00')::timestamp
                 AT TIME ZONE 'Asia/Ho_Chi_Minh'),
             0, 1, 'DASH_TEST attendance', v_user_id,
             CASE WHEN v_status = 'IN_PROGRESS' THEN 'SCHEDULED' ELSE 'COMPLETED' END)
        RETURNING shift_id INTO v_shift_id;

        INSERT INTO public.shift_assignments
            (shift_id, user_id, assignment_source, assigned_by, status)
        VALUES
            (v_shift_id, v_user_id, 'MANAGER_ASSIGNMENT', v_user_id,
             CASE WHEN v_status = 'IN_PROGRESS' THEN 'ASSIGNED' ELSE 'COMPLETED' END)
        RETURNING assignment_id INTO v_assignment_id;

        INSERT INTO public.attendances
            (assignment_id, attendance_rule_id, rule_snapshot_json,
             employee_full_name_snapshot, shift_start_at_snapshot,
             shift_end_at_snapshot, branch_name_snapshot,
             branch_address_snapshot, status, actual_working_minutes,
             working_hours, total_late_minutes, excused_late_minutes,
             chargeable_late_minutes, total_early_leave_minutes,
             excused_early_leave_minutes, chargeable_early_leave_minutes)
        SELECT
            v_assignment_id,
            v_rule_id,
            '{"source":"DASH_TEST"}'::jsonb,
            u.full_name,
            w.scheduled_start_at,
            w.scheduled_end_at,
            b.branch_name,
            b.address,
            v_status,
            CASE WHEN v_status IN ('ABSENT', 'IN_PROGRESS') THEN 0 ELSE 60 END,
            CASE WHEN v_status IN ('ABSENT', 'IN_PROGRESS') THEN 0 ELSE 1 END,
            CASE WHEN v_status IN ('LATE', 'LATE_AND_EARLY_LEAVE') THEN 15 ELSE 0 END,
            0,
            CASE WHEN v_status IN ('LATE', 'LATE_AND_EARLY_LEAVE') THEN 15 ELSE 0 END,
            CASE WHEN v_status IN ('EARLY_LEAVE', 'LATE_AND_EARLY_LEAVE') THEN 10 ELSE 0 END,
            0,
            CASE WHEN v_status IN ('EARLY_LEAVE', 'LATE_AND_EARLY_LEAVE') THEN 10 ELSE 0 END
        FROM public.users u
        CROSS JOIN public.work_shifts w
        CROSS JOIN public.branches b
        WHERE u.user_id = v_user_id
          AND w.shift_id = v_shift_id
          AND b.branch_id = v_branch_hcm_id;
    END LOOP;

    -- Payroll prerequisites. EXPIRED/INACTIVE avoid changing business-active records.
    INSERT INTO public.employment_contracts
        (user_id, contract_number, start_date, end_date, hourly_rate,
         employee_position_snapshot, branch_id, region_id,
         employee_full_name_snapshot, employee_phone_snapshot,
         employee_address_snapshot, employee_identity_no_snapshot,
         branch_name_snapshot, branch_address_snapshot,
         region_name_snapshot, status, created_by)
    SELECT
        u.user_id,
        'DASH_CONTRACT_' || replace(u.user_id::text, '-', ''),
        v_today - 365, v_today + 365, 50000,
        'Test Staff', b.branch_id, r.region_id,
        u.full_name, u.phone, coalesce(u.address, 'Dashboard test address'),
        u.identity_no, b.branch_name, b.address, r.region_name,
        'EXPIRED', v_user_id
    FROM public.users u
    JOIN public.branches b ON b.branch_id = v_branch_hcm_id
    JOIN public.regions r ON r.region_id = b.region_id
    WHERE u.user_id = v_user_id
    ON CONFLICT (contract_number) DO UPDATE
    SET hourly_rate = EXCLUDED.hourly_rate
    RETURNING contract_id INTO v_contract_id;

    INSERT INTO public.staff_salary_history
        (user_id, contract_id, hourly_rate, change_type,
         effective_from, effective_to, status, reason, created_by)
    VALUES
        (v_user_id, v_contract_id, 50000, 'INITIAL',
         v_today - 365, v_today + 365, 'INACTIVE',
         'DASH_TEST salary', v_user_id)
    RETURNING salary_history_id INTO v_salary_id;

    -- Three latest payrolls; two SUBMITTED contribute to pendingPayrolls.
    FOR v_index IN 1..3 LOOP
        INSERT INTO public.payroll_periods
            (branch_id, branch_name_snapshot, period_code, period_name,
             start_date, end_date, status, created_by,
             calculated_by, calculated_at, submitted_by, submitted_at,
             created_at, updated_at)
        SELECT
            b.branch_id, b.branch_name,
            'DASH_TEST_PERIOD_' || to_char(v_today, 'YYYYMMDD') || '_' || v_index,
            'Dashboard Test Payroll ' || v_index,
            (v_today - (v_index * 40)),
            (v_today - (v_index * 40) + 29),
            CASE WHEN v_index < 3 THEN 'SUBMITTED' ELSE 'APPROVED' END,
            v_user_id, v_user_id, now() - (v_index || ' hours')::interval,
            CASE WHEN v_index < 3 THEN v_user_id ELSE NULL END,
            CASE WHEN v_index < 3 THEN now() - (v_index || ' hours')::interval ELSE NULL END,
            now() - (v_index || ' days')::interval,
            now() - (v_index || ' hours')::interval
        FROM public.branches b
        WHERE b.branch_id = v_branch_hcm_id
        RETURNING payroll_period_id INTO v_period_id;

        INSERT INTO public.timesheets
            (user_id, payroll_period_id, total_assigned_shifts,
             completed_shift_count, absent_shift_count,
             total_scheduled_hours, total_work_hours, status)
        VALUES
            (v_user_id, v_period_id, 20, 19, 1, 160, 152, 'LOCKED')
        RETURNING timesheet_id INTO v_timesheet_id;

        INSERT INTO public.payrolls
            (user_id, payroll_period_id, timesheet_id, branch_id,
             salary_history_id, employee_full_name_snapshot,
             employee_position_snapshot, branch_name_snapshot,
             hourly_rate_snapshot, total_work_hours, work_pay,
             total_reward, total_penalty, attendance_deduction,
             gross_salary, total_deduction, net_salary, status,
             calculated_at, created_at, updated_at)
        SELECT
            u.user_id, v_period_id, v_timesheet_id, b.branch_id,
            v_salary_id, u.full_name, 'Test Staff', b.branch_name,
            50000, 152, 7600000,
            v_index * 100000, 50000, 25000,
            7600000 + (v_index * 100000),
            75000,
            7525000 + (v_index * 100000),
            CASE WHEN v_index < 3 THEN 'SUBMITTED' ELSE 'APPROVED' END,
            now() - (v_index || ' hours')::interval,
            now() - (v_index || ' days')::interval,
            now() - (v_index || ' hours')::interval
        FROM public.users u
        CROSS JOIN public.branches b
        WHERE u.user_id = v_user_id
          AND b.branch_id = v_branch_hcm_id;
    END LOOP;

    RAISE NOTICE 'Dashboard sample data created successfully with user_id=%', v_user_id;
END $$;

-- Quick verification: values should also appear in GET /api/admin/dashboard.
SELECT
    (SELECT count(*) FROM public.users) AS total_users,
    (SELECT count(*) FROM public.regions) AS total_regions,
    (SELECT count(*) FROM public.branches) AS total_branches,
    (SELECT count(*) FROM public.branches WHERE status = 'ACTIVE') AS active_branches,
    (SELECT count(*) FROM public.payrolls WHERE status = 'SUBMITTED') AS pending_payrolls,
    (SELECT count(*) FROM public.employee_requests WHERE status = 'PENDING') AS pending_requests;

SELECT status, count(*) AS attendance_count
FROM public.attendances
WHERE shift_start_at_snapshot >= (
        (now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date::timestamp
        AT TIME ZONE 'Asia/Ho_Chi_Minh'
    )
  AND shift_start_at_snapshot < (
        ((now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date + 1)::timestamp
        AT TIME ZONE 'Asia/Ho_Chi_Minh'
    )
GROUP BY status
ORDER BY status;
