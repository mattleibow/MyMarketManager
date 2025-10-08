using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchTypeToStagingBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchType",
                table: "StagingBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchType",
                table: "StagingBatches");
        }
    }
}
