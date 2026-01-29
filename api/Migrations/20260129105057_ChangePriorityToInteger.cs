using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Todo.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangePriorityToInteger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary integer column
            migrationBuilder.AddColumn<int>(
                name: "Priority_New",
                table: "Todos",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            // Step 2: Convert existing string data to integers
            // "Low" -> 1, "Medium" -> 2, "High" -> 3
            migrationBuilder.Sql(@"
                UPDATE ""Todos""
                SET ""Priority_New"" = CASE
                    WHEN ""Priority"" = 'Low' THEN 1
                    WHEN ""Priority"" = 'Medium' THEN 2
                    WHEN ""Priority"" = 'High' THEN 3
                    ELSE 2
                END
            ");

            // Step 3: Drop the old string column
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Todos");

            // Step 4: Rename the new column to Priority
            migrationBuilder.RenameColumn(
                name: "Priority_New",
                table: "Todos",
                newName: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary string column
            migrationBuilder.AddColumn<string>(
                name: "Priority_Old",
                table: "Todos",
                type: "text",
                nullable: false,
                defaultValue: "Medium");

            // Step 2: Convert integer data back to strings
            // 1 -> "Low", 2 -> "Medium", 3 -> "High"
            migrationBuilder.Sql(@"
                UPDATE ""Todos""
                SET ""Priority_Old"" = CASE
                    WHEN ""Priority"" = 1 THEN 'Low'
                    WHEN ""Priority"" = 2 THEN 'Medium'
                    WHEN ""Priority"" = 3 THEN 'High'
                    ELSE 'Medium'
                END
            ");

            // Step 3: Drop the integer column
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Todos");

            // Step 4: Rename the old column back to Priority
            migrationBuilder.RenameColumn(
                name: "Priority_Old",
                table: "Todos",
                newName: "Priority");
        }
    }
}
