using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperSessionAndUpdateEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "StagingBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScraperSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StagingBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CookieFileJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperSessions_StagingBatches_StagingBatchId",
                        column: x => x.StagingBatchId,
                        principalTable: "StagingBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScraperSessions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSessions_StagingBatchId",
                table: "ScraperSessions",
                column: "StagingBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSessions_SupplierId",
                table: "ScraperSessions",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScraperSessions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "StagingBatches");
        }
    }
}
