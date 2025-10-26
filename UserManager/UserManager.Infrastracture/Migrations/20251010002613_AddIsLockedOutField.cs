using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLockedOutField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLockedOut",
                table: "T_UserAccessFails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLockedOut",
                table: "T_UserAccessFails");
        }
    }
}
