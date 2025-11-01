using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManager.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendAndGroupRequestEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_FriendRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_FriendRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "T_GroupJoinRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_GroupJoinRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_FriendRequests_RequesterId_ReceiverId",
                table: "T_FriendRequests",
                columns: new[] { "RequesterId", "ReceiverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_T_GroupJoinRequests_GroupId_RequesterId",
                table: "T_GroupJoinRequests",
                columns: new[] { "GroupId", "RequesterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_FriendRequests");

            migrationBuilder.DropTable(
                name: "T_GroupJoinRequests");
        }
    }
}
