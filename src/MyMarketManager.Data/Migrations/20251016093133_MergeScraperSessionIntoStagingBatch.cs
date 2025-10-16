using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeScraperSessionIntoStagingBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StagingBatches_ScraperSessions_ScraperSessionId",
                table: "StagingBatches");

            migrationBuilder.DropTable(
                name: "ScraperSessions");

            migrationBuilder.DropIndex(
                name: "IX_StagingBatches_ScraperSessionId",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "ScraperSessionId",
                table: "StagingBatches");

            migrationBuilder.RenameColumn(
                name: "UploadDate",
                table: "StagingBatches",
                newName: "StartedAt");

            migrationBuilder.AddColumn<int>(
                name: "BatchType",
                table: "StagingBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "StagingBatches",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileContents",
                table: "StagingBatches",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchType",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "FileContents",
                table: "StagingBatches");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "StagingBatches",
                newName: "UploadDate");

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperSessionId",
                table: "StagingBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScraperSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CookieFileJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StagingBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperSessions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StagingBatches_ScraperSessionId",
                table: "StagingBatches",
                column: "ScraperSessionId",
                unique: true,
                filter: "[ScraperSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSessions_SupplierId",
                table: "ScraperSessions",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_StagingBatches_ScraperSessions_ScraperSessionId",
                table: "StagingBatches",
                column: "ScraperSessionId",
                principalTable: "ScraperSessions",
                principalColumn: "Id");
        }
    }
}
