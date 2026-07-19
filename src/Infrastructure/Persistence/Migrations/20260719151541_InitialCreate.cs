using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Practices",
                columns: table => new
                {
                    PracticeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PracticeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DbServerHost = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DbName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RowLevelSecurityEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practices", x => x.PracticeId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Practices");
        }
    }
}
