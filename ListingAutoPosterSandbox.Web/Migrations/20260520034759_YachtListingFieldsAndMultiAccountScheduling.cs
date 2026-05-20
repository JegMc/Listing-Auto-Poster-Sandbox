using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAutoPosterSandbox.Web.Migrations
{
    /// <inheritdoc />
    public partial class YachtListingFieldsAndMultiAccountScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrokerageCompany",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Builder",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Cabins",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Guests",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthFeet",
                table: "Listings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSpeedKnots",
                table: "Listings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearBuilt",
                table: "Listings",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BrokerageCompany", "Builder", "Cabins", "Guests", "LengthFeet", "Location", "MaxSpeedKnots", "YearBuilt" },
                values: new object[] { "", "", null, null, null, "", null, null });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BrokerageCompany", "Builder", "Cabins", "Guests", "LengthFeet", "Location", "MaxSpeedKnots", "YearBuilt" },
                values: new object[] { "", "", null, null, null, "", null, null });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BrokerageCompany", "Builder", "Cabins", "Guests", "LengthFeet", "Location", "MaxSpeedKnots", "YearBuilt" },
                values: new object[] { "", "", null, null, null, "", null, null });

            migrationBuilder.UpdateData(
                table: "SocialAccounts",
                keyColumn: "Id",
                keyValue: 1,
                column: "PlatformAccountId",
                value: "1103146319551782");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrokerageCompany",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Builder",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Cabins",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Guests",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "LengthFeet",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "MaxSpeedKnots",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "YearBuilt",
                table: "Listings");

            migrationBuilder.UpdateData(
                table: "SocialAccounts",
                keyColumn: "Id",
                keyValue: 1,
                column: "PlatformAccountId",
                value: null);
        }
    }
}
