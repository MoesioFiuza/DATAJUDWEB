using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataJudJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalFileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "pendente")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalProcessos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ProcessosProcessados = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultFilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultFileSize = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataJudJobs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataJudJobProcessos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobId = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroCNJ = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "pendente")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoTribunal = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeTribunal = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TotalMovimentacoes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataJudJobProcessos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataJudJobProcessos_DataJudJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataJudJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobProcessos_JobId",
                table: "DataJudJobProcessos",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobProcessos_NumeroCNJ",
                table: "DataJudJobProcessos",
                column: "NumeroCNJ");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobProcessos_Status",
                table: "DataJudJobProcessos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobs_CreatedAt",
                table: "DataJudJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobs_Status",
                table: "DataJudJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobs_UserId",
                table: "DataJudJobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobs_UserId_CreatedAt",
                table: "DataJudJobs",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataJudJobProcessos");

            migrationBuilder.DropTable(
                name: "DataJudJobs");
        }
    }
}
