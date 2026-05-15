using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ListingAutoPosterSandbox.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SocialAccountId",
                table: "ScheduledPosts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SocialAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Platform = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialAccounts", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SocialAccounts",
                columns: new[] { "Id", "CreatedUtc", "DisplayName", "IsConnected", "Platform", "SecretName", "UpdatedUtc" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo Facebook Page", true, "Facebook", "dev/social/facebook/demo-page", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo Instagram Business Account", true, "Instagram", "dev/social/instagram/demo-business", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo LinkedIn Company Page", true, "LinkedIn", "dev/social/linkedin/demo-company", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPosts_SocialAccountId",
                table: "ScheduledPosts",
                column: "SocialAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledPosts_SocialAccounts_SocialAccountId",
                table: "ScheduledPosts",
                column: "SocialAccountId",
                principalTable: "SocialAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledPosts_SocialAccounts_SocialAccountId",
                table: "ScheduledPosts");

            migrationBuilder.DropTable(
                name: "SocialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledPosts_SocialAccountId",
                table: "ScheduledPosts");

            migrationBuilder.DropColumn(
                name: "SocialAccountId",
                table: "ScheduledPosts");
        }
    }
}
