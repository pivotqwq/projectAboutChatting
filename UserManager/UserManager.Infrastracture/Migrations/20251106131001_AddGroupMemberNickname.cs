using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMemberNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "T_GroupMembers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "T_GroupMembers");
        }
    }
}
