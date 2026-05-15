using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ListingAutoPosterSandbox.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Listings",
                columns: new[] { "Id", "Address", "Description", "ImageUrl", "Price", "Title" },
                values: new object[,]
                {
                    { 1, "123 Main Street, Nashville, TN", "A bright two-bedroom condo near restaurants, shops, and public transit.", "https://placehold.co/600x400", 425000m, "Modern Downtown Condo" },
                    { 2, "456 Oak Ridge Drive, Franklin, TN", "A spacious four-bedroom home with an open kitchen and fenced backyard.", "https://placehold.co/600x400", 675000m, "Family Home with Large Backyard" },
                    { 3, "789 Cedar Lane, Murfreesboro, TN", "A low-maintenance townhome close to walking trails and local parks.", "https://placehold.co/600x400", 350000m, "Quiet Townhome Near Parks" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Listings");
        }
    }
}
