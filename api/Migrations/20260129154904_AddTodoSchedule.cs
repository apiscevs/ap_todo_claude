using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Todo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndAtUtc",
                table: "Todos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAtUtc",
                table: "Todos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Todos_Schedule",
                table: "Todos",
                sql: "(\"StartAtUtc\" IS NULL AND \"EndAtUtc\" IS NULL) OR (\"StartAtUtc\" IS NOT NULL AND \"EndAtUtc\" IS NOT NULL AND \"EndAtUtc\" >= \"StartAtUtc\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Todos_Schedule",
                table: "Todos");

            migrationBuilder.DropColumn(
                name: "EndAtUtc",
                table: "Todos");

            migrationBuilder.DropColumn(
                name: "StartAtUtc",
                table: "Todos");
        }
    }
}
