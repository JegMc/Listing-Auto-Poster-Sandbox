using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAutoPosterSandbox.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAccountIdToSocialAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlatformAccountId",
                table: "SocialAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SocialAccounts",
                keyColumn: "Id",
                keyValue: 1,
                column: "PlatformAccountId",
                value: null);

            migrationBuilder.UpdateData(
                table: "SocialAccounts",
                keyColumn: "Id",
                keyValue: 2,
                column: "PlatformAccountId",
                value: null);

            migrationBuilder.UpdateData(
                table: "SocialAccounts",
                keyColumn: "Id",
                keyValue: 3,
                column: "PlatformAccountId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlatformAccountId",
                table: "SocialAccounts");
        }
    }
}
