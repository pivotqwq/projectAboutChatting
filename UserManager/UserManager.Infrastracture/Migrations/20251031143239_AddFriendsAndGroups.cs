using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendsAndGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_Friendships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Friendships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "T_GroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_GroupMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "T_Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Groups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_Friendships_UserId_FriendUserId",
                table: "T_Friendships",
                columns: new[] { "UserId", "FriendUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_T_GroupMembers_GroupId_UserId",
                table: "T_GroupMembers",
                columns: new[] { "GroupId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_Friendships");

            migrationBuilder.DropTable(
                name: "T_GroupMembers");

            migrationBuilder.DropTable(
                name: "T_Groups");
        }
    }
}
