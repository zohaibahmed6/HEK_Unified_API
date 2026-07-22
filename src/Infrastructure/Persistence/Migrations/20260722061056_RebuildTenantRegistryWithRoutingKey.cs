using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RebuildTenantRegistryWithRoutingKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Practices",
                table: "Practices");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Practices",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Practices",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Practices",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PracticeCode",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Practices",
                table: "Practices",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment",
                table: "Practices",
                columns: new[] { "PracticeId", "PracticeCode", "Environment" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Practices",
                table: "Practices");

            migrationBuilder.DropIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "Practices");

            migrationBuilder.DropColumn(
                name: "PracticeCode",
                table: "Practices");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Practices",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Practices",
                newName: "CreatedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Practices",
                table: "Practices",
                column: "PracticeId");
        }
    }
}
