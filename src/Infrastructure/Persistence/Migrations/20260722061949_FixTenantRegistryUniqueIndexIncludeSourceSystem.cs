using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantRegistryUniqueIndexIncludeSourceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment",
                table: "Practices");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment_SourceSystem",
                table: "Practices",
                columns: new[] { "PracticeId", "PracticeCode", "Environment", "SourceSystem" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment_SourceSystem",
                table: "Practices");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment",
                table: "Practices",
                columns: new[] { "PracticeId", "PracticeCode", "Environment" },
                unique: true);
        }
    }
}
