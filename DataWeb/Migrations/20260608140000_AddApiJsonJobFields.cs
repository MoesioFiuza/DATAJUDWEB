using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataWeb.Migrations
{
    public partial class AddApiJsonJobFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputCnjsJson",
                table: "DataJudJobs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "JobKind",
                table: "DataJudJobs",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "xlsx")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResultJson",
                table: "DataJudJobs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DataJudJobs_JobKind_Status",
                table: "DataJudJobs",
                columns: new[] { "JobKind", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataJudJobs_JobKind_Status",
                table: "DataJudJobs");

            migrationBuilder.DropColumn(
                name: "InputCnjsJson",
                table: "DataJudJobs");

            migrationBuilder.DropColumn(
                name: "JobKind",
                table: "DataJudJobs");

            migrationBuilder.DropColumn(
                name: "ResultJson",
                table: "DataJudJobs");
        }
    }
}
