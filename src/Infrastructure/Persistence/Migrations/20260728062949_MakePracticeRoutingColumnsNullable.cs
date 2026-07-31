using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HekCoreApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakePracticeRoutingColumnsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment_SourceSystem",
                table: "Practices");

            migrationBuilder.AlterColumn<string>(
                name: "PracticeId",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "PracticeCode",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Environment",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment_SourceSystem",
                table: "Practices",
                columns: new[] { "PracticeId", "PracticeCode", "Environment", "SourceSystem" },
                unique: true,
                filter: "[PracticeId] IS NOT NULL AND [PracticeCode] IS NOT NULL AND [Environment] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment_SourceSystem",
                table: "Practices");

            migrationBuilder.AlterColumn<string>(
                name: "PracticeId",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PracticeCode",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Environment",
                table: "Practices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practices_PracticeId_PracticeCode_Environment_SourceSystem",
                table: "Practices",
                columns: new[] { "PracticeId", "PracticeCode", "Environment", "SourceSystem" },
                unique: true);
        }
    }
}
