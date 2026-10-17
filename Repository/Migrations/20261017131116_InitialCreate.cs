using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Stay = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Stars = table.Column<int>(type: "int", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HighlightsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PopularJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmenityGroupsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GalleryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NearbyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PoliciesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PropertyId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RoomId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: false),
                    Refundable = table.Column<bool>(type: "bit", nullable: false),
                    Nightly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Rooms = table.Column<int>(type: "int", nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Cancelled = table.Column<bool>(type: "bit", nullable: false),
                    Refund = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RoomId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Author = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Traveller = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nights = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Liked = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disliked = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    PropertyId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Beds = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Sleeps = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Refundable = table.Column<bool>(type: "bit", nullable: false),
                    Breakfast = table.Column<bool>(type: "bit", nullable: false),
                    Inventory = table.Column<int>(type: "int", nullable: false),
                    FeaturesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => new { x.PropertyId, x.Id });
                    table.ForeignKey(
                        name: "FK_Rooms_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedStays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedStays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedStays_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedStays_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PropertyId_RoomId_CheckIn_CheckOut",
                table: "Reservations",
                columns: new[] { "PropertyId", "RoomId", "CheckIn", "CheckOut" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Reference",
                table: "Reservations",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_PropertyId",
                table: "Reviews",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedStays_AccountId_PropertyId",
                table: "SavedStays",
                columns: new[] { "AccountId", "PropertyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedStays_PropertyId",
                table: "SavedStays",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "SavedStays");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
