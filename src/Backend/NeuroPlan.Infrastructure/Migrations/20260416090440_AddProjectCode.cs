using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroPlan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "Projects");
        }
    }
}
