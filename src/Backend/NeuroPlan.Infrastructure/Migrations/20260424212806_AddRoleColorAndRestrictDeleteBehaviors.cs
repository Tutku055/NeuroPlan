using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroPlan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleColorAndRestrictDeleteBehaviors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Worklogs_Users_UserId",
                table: "Worklogs");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Roles",
                type: "TEXT",
                maxLength: 7,
                nullable: false,
                defaultValue: "#64748B");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Worklogs_Users_UserId",
                table: "Worklogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Worklogs_Users_UserId",
                table: "Worklogs");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Roles");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Worklogs_Users_UserId",
                table: "Worklogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
