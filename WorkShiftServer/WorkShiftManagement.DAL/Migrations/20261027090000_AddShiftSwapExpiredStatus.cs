using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftManagement.DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261027090000_AddShiftSwapExpiredStatus")]
public sealed class AddShiftSwapExpiredStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE public.shift_swap_requests DROP CONSTRAINT IF EXISTS shift_swap_requests_status_check;
        ALTER TABLE public.shift_swap_requests ADD CONSTRAINT shift_swap_requests_status_check
            CHECK (status IN ('PENDING', 'ACCEPTED', 'REJECTED', 'CANCELLED', 'EXPIRED'));
        UPDATE public.shift_swap_requests AS r SET status = 'EXPIRED', updated_at = now()
        FROM public.shift_assignments AS a, public.shift_assignments AS b,
             public.work_shifts AS s, public.work_shifts AS t
        WHERE r.status = 'PENDING'
          AND a.assignment_id = r.source_assignment_id AND b.assignment_id = r.target_assignment_id
          AND s.shift_id = a.shift_id AND t.shift_id = b.shift_id
          AND LEAST(s.scheduled_start_at, t.scheduled_start_at) <= now();
        """);

    // Downgrade must not relabel expired requests as pending or as a staff response.
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DO $$ BEGIN
            IF EXISTS (SELECT 1 FROM public.shift_swap_requests WHERE status = 'EXPIRED') THEN
                RAISE EXCEPTION 'Cannot remove EXPIRED status while expired shift swap history exists.';
            END IF;
        END $$;
        ALTER TABLE public.shift_swap_requests DROP CONSTRAINT shift_swap_requests_status_check;
        ALTER TABLE public.shift_swap_requests ADD CONSTRAINT shift_swap_requests_status_check
            CHECK (status IN ('PENDING', 'ACCEPTED', 'REJECTED', 'CANCELLED'));
        """);
}
