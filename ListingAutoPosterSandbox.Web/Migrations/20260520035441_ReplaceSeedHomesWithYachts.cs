using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListingAutoPosterSandbox.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSeedHomesWithYachts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "BrokerageCompany", "Builder", "Cabins", "Description", "Guests", "ImageUrl", "LengthFeet", "Location", "MaxSpeedKnots", "Price", "Title", "YearBuilt" },
                values: new object[] { "Miami, FL", "YATCO Demo Brokerage", "Sunseeker", 4, "A sleek motor yacht with modern entertaining spaces, refined interior finishes, expansive deck areas, and strong performance for coastal cruising.", 8, "https://placehold.co/600x400?text=Azure+Horizon", 88m, "Miami, FL", 28m, 5495000m, "M/Y Azure Horizon", 2020 });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Address", "BrokerageCompany", "Builder", "Cabins", "Description", "Guests", "ImageUrl", "LengthFeet", "Location", "MaxSpeedKnots", "Price", "Title", "YearBuilt" },
                values: new object[] { "Palm Beach, FL", "YATCO Demo Brokerage", "Azimut", 4, "A well-appointed flybridge yacht designed for relaxed cruising, featuring generous outdoor lounging areas, a bright salon, and comfortable guest accommodations.", 8, "https://placehold.co/600x400?text=Silver+Current", 72m, "Palm Beach, FL", 31m, 3250000m, "M/Y Silver Current", 2018 });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Address", "BrokerageCompany", "Builder", "Cabins", "Description", "Guests", "ImageUrl", "LengthFeet", "Location", "MaxSpeedKnots", "Price", "Title", "YearBuilt" },
                values: new object[] { "Fort Lauderdale, FL", "YATCO Demo Brokerage", "Beneteau", 3, "A capable sailing yacht with clean lines, efficient handling, comfortable accommodations, and a practical layout suited for extended coastal passages.", 6, "https://placehold.co/600x400?text=Wind+Meridian", 58m, "Fort Lauderdale, FL", 12m, 875000m, "S/Y Wind Meridian", 2019 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "BrokerageCompany", "Builder", "Cabins", "Description", "Guests", "ImageUrl", "LengthFeet", "Location", "MaxSpeedKnots", "Price", "Title", "YearBuilt" },
                values: new object[] { "123 Main Street, Nashville, TN", "", "", null, "A bright two-bedroom condo near restaurants, shops, and public transit.", null, "https://placehold.co/600x400", null, "", null, 425000m, "Modern Downtown Condo", null });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Address", "BrokerageCompany", "Builder", "Cabins", "Description", "Guests", "ImageUrl", "LengthFeet", "Location", "MaxSpeedKnots", "Price", "Title", "YearBuilt" },
                values: new object[] { "456 Oak Ridge Drive, Franklin, TN", "", "", null, "A spacious four-bedroom home with an open kitchen and fenced backyard.", null, "https://placehold.co/600x400", null, "", null, 675000m, "Family Home with Large Backyard", null });

            migrationBuilder.UpdateData(
                table: "Listings",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Address", "BrokerageCompany", "Builder", "Cabins", "Description", "Guests", "ImageUrl", "LengthFeet", "Location", "MaxSpeedKnots", "Price", "Title", "YearBuilt" },
                values: new object[] { "789 Cedar Lane, Murfreesboro, TN", "", "", null, "A low-maintenance townhome close to walking trails and local parks.", null, "https://placehold.co/600x400", null, "", null, 350000m, "Quiet Townhome Near Parks", null });
        }
    }
}
