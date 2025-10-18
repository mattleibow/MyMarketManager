using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorScraperInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScraperSessions_StagingBatches_StagingBatchId",
                table: "ScraperSessions");

            migrationBuilder.DropIndex(
                name: "IX_ScraperSessions_StagingBatchId",
                table: "ScraperSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperSessionId",
                table: "StagingBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StagingBatches_ScraperSessionId",
                table: "StagingBatches",
                column: "ScraperSessionId",
                unique: true,
                filter: "[ScraperSessionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StagingBatches_ScraperSessions_ScraperSessionId",
                table: "StagingBatches",
                column: "ScraperSessionId",
                principalTable: "ScraperSessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StagingBatches_ScraperSessions_ScraperSessionId",
                table: "StagingBatches");

            migrationBuilder.DropIndex(
                name: "IX_StagingBatches_ScraperSessionId",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "ScraperSessionId",
                table: "StagingBatches");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSessions_StagingBatchId",
                table: "ScraperSessions",
                column: "StagingBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScraperSessions_StagingBatches_StagingBatchId",
                table: "ScraperSessions",
                column: "StagingBatchId",
                principalTable: "StagingBatches",
                principalColumn: "Id");
        }
    }
}
