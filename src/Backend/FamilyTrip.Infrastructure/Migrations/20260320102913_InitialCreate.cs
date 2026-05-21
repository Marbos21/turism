using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FamilyTrip.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChildFriendlyScore = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgCostPerDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Climate = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredClimate = table.Column<int>(type: "INTEGER", nullable: false),
                    TravelStyle = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Families_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FamilyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Interests = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SpecialNeeds = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FamilyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DestinationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Reasoning = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recommendations_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FamilyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DestinationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstimatedBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripPlans_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripPlans_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItineraryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TripPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeSlot = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItineraryItems_TripPlans_TripPlanId",
                        column: x => x.TripPlanId,
                        principalTable: "TripPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Destinations",
                columns: new[] { "Id", "AvgCostPerDay", "Category", "ChildFriendlyScore", "Climate", "Country", "CreatedAt", "Description", "ImageUrl", "Latitude", "Longitude", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), 250m, "Cultural", 8, 3, "France", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The City of Light offers world-class museums, iconic landmarks, and exquisite cuisine. Family-friendly attractions include the Eiffel Tower, Louvre, and Disneyland Paris nearby.", "https://images.unsplash.com/photo-1502602898657-3e91760cbb34", 48.8566, 2.3521999999999998, "Paris", null },
                    { new Guid("00000001-0000-0000-0000-000000000002"), 200m, "Cultural", 8, 2, "Italy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "The Eternal City captivates with ancient history, art, and incredible food. Kids love the Colosseum, Vatican Museums, and tossing coins in the Trevi Fountain.", "https://images.unsplash.com/photo-1515542622106-78bda8ba0e5b", 41.902799999999999, 12.4964, "Rome", null },
                    { new Guid("00000001-0000-0000-0000-000000000003"), 180m, "Mixed", 9, 2, "Spain", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A vibrant city blending Gaudí's architectural wonders, beautiful beaches, and lively markets. Perfect for families seeking culture and sun.", "https://images.unsplash.com/photo-1583422409516-2895a77efded", 41.385100000000001, 2.1734, "Barcelona", null },
                    { new Guid("00000001-0000-0000-0000-000000000004"), 300m, "Relaxation", 7, 2, "Greece", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Stunning volcanic island with iconic white-washed buildings, breathtaking sunsets, and crystal-clear Aegean waters. A romantic yet family-friendly destination.", "https://images.unsplash.com/photo-1570077188670-e3a8d69ac5ff", 36.3932, 25.461500000000001, "Santorini", null },
                    { new Guid("00000001-0000-0000-0000-000000000005"), 120m, "Adventure", 8, 1, "Indonesia", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A tropical paradise with lush rice terraces, ancient temples, and warm hospitality. Families enjoy water parks, cultural experiences, and beautiful beaches.", "https://images.unsplash.com/photo-1537996194471-e657df975ab4", -8.3405000000000005, 115.092, "Bali", null },
                    { new Guid("00000001-0000-0000-0000-000000000006"), 600m, "Relaxation", 7, 1, "Maldives", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A luxury island paradise with overwater bungalows, pristine coral reefs, and turquoise lagoons. Perfect for families seeking ultimate relaxation and water activities.", "https://images.unsplash.com/photo-1514282401047-d79a71a590e8", 3.2027999999999999, 73.220699999999994, "Maldives", null },
                    { new Guid("00000001-0000-0000-0000-000000000007"), 280m, "Cultural", 9, 3, "Japan", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A futuristic metropolis that blends ancient traditions with cutting-edge technology. Kids are amazed by Disneyland Tokyo, Robot Restaurant, and unique pop culture.", "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf", 35.676200000000001, 139.65029999999999, "Tokyo", null },
                    { new Guid("00000001-0000-0000-0000-000000000008"), 150m, "Adventure", 9, 1, "Costa Rica", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A biodiversity hotspot with rainforests, volcanoes, and gorgeous Pacific and Caribbean beaches. Families love zip-lining, wildlife tours, and surfing lessons.", "https://images.unsplash.com/photo-1518259102261-b40117eabbc9", 9.7489000000000008, -83.753399999999999, "Costa Rica", null },
                    { new Guid("00000001-0000-0000-0000-000000000009"), 220m, "Cultural", 8, 3, "Netherlands", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A charming city of canals, museums, and bicycle culture. Families enjoy the Anne Frank House, Rijksmuseum, Vondelpark, and unique canal boat tours.", "https://images.unsplash.com/photo-1512470876302-972faa2aa9a4", 52.367600000000003, 4.9040999999999997, "Amsterdam", null },
                    { new Guid("00000001-0000-0000-0000-000000000010"), 350m, "Adventure", 8, 4, "Iceland", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Land of fire and ice with dramatic landscapes, geysers, waterfalls, and the Northern Lights. An unforgettable adventure destination for nature-loving families.", "https://images.unsplash.com/photo-1529963183134-61a90db47eaf", 64.963099999999997, -19.020800000000001, "Iceland", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Families_UserId",
                table: "Families",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_FamilyId",
                table: "FamilyMembers",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryItems_TripPlanId",
                table: "ItineraryItems",
                column: "TripPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_DestinationId",
                table: "Recommendations",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_FamilyId",
                table: "Recommendations",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlans_DestinationId",
                table: "TripPlans",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlans_FamilyId",
                table: "TripPlans",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "ItineraryItems");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "TripPlans");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
