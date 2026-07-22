using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCentralHisoSessionRegistryMirroringPractices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HisoSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PracticeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PracticeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PracticeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DbServerHost = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DbName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RowLevelSecurityEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HisoSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HisoSessions_SessionGuid",
                table: "HisoSessions",
                column: "SessionGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HisoSessions");
        }
    }
}
