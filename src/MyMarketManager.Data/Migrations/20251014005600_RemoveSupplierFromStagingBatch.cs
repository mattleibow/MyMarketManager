using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupplierFromStagingBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StagingBatches_Suppliers_SupplierId",
                table: "StagingBatches");

            migrationBuilder.DropIndex(
                name: "IX_StagingBatches_SupplierId",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "StagingBatches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "StagingBatches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StagingBatches_SupplierId",
                table: "StagingBatches",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_StagingBatches_Suppliers_SupplierId",
                table: "StagingBatches",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
