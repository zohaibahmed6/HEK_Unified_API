using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerTierUniqueIndexesOnPractices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Practices_Tier1_PracticeIdOnly",
                table: "Practices",
                columns: new[] { "PracticeId", "SourceSystem" },
                unique: true,
                filter: "[PracticeCode] IS NULL AND [Environment] IS NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_Tier2_PracticeIdAndCode",
                table: "Practices",
                columns: new[] { "PracticeId", "PracticeCode", "SourceSystem" },
                unique: true,
                filter: "[PracticeCode] IS NOT NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_Tier3_EnvironmentOnly",
                table: "Practices",
                columns: new[] { "Environment", "SourceSystem" },
                unique: true,
                filter: "[Environment] IS NOT NULL AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Practices_Tier1_PracticeIdOnly",
                table: "Practices");

            migrationBuilder.DropIndex(
                name: "IX_Practices_Tier2_PracticeIdAndCode",
                table: "Practices");

            migrationBuilder.DropIndex(
                name: "IX_Practices_Tier3_EnvironmentOnly",
                table: "Practices");
        }
    }
}
