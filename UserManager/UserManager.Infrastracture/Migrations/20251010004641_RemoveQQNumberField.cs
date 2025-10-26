using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQQNumberField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserBasic_QQNumber",
                table: "T_Users");

            migrationBuilder.DropColumn(
                name: "UserBasic_QQNumber",
                table: "T_UserLoginHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserBasic_QQNumber",
                table: "T_Users",
                type: "character varying(11)",
                unicode: false,
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserBasic_QQNumber",
                table: "T_UserLoginHistories",
                type: "character varying(11)",
                unicode: false,
                maxLength: 11,
                nullable: true);
        }
    }
}
