using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkShiftManagement.DAL.Data;

#nullable disable

namespace WorkShiftManagement.DAL.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261007130000_AddEmploymentContractCancellationFields")]
public partial class AddEmploymentContractCancellationFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "cancellation_reason",
            table: "employment_contracts",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "cancelled_at",
            table: "employment_contracts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "cancelled_by",
            table: "employment_contracts",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_employment_contracts_cancelled_by",
            table: "employment_contracts",
            column: "cancelled_by");

        migrationBuilder.AddForeignKey(
            name: "employment_contracts_cancelled_by_fkey",
            table: "employment_contracts",
            column: "cancelled_by",
            principalTable: "users",
            principalColumn: "user_id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "employment_contracts_cancelled_by_fkey",
            table: "employment_contracts");

        migrationBuilder.DropIndex(
            name: "ix_employment_contracts_cancelled_by",
            table: "employment_contracts");

        migrationBuilder.DropColumn(
            name: "cancellation_reason",
            table: "employment_contracts");

        migrationBuilder.DropColumn(
            name: "cancelled_at",
            table: "employment_contracts");

        migrationBuilder.DropColumn(
            name: "cancelled_by",
            table: "employment_contracts");
    }
}
