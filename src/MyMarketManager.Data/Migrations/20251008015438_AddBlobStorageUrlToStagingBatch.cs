using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlobStorageUrlToStagingBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlobStorageUrl",
                table: "StagingBatches",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlobStorageUrl",
                table: "StagingBatches");
        }
    }
}
