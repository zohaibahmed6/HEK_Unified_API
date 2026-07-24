using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyGlobalConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyGlobalConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DbServerHost = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DbName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CredentialSecretKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyGlobalConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyGlobalConnections_Key",
                table: "LegacyGlobalConnections",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyGlobalConnections");
        }
    }
}
