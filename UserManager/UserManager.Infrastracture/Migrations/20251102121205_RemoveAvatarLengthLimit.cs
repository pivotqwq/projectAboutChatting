using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAvatarLengthLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Avatar",
                table: "T_UserProfiles",
                type: "TEXT",
                unicode: false,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldUnicode: false,
                oldMaxLength: 500,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Avatar",
                table: "T_UserProfiles",
                type: "character varying(500)",
                unicode: false,
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldUnicode: false,
                oldNullable: true);
        }
    }
}
