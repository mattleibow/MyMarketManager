using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMarketManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImageVectorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiDescription",
                table: "ProductPhotos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiTags",
                table: "ProductPhotos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VectorEmbedding",
                table: "ProductPhotos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VectorizedAt",
                table: "ProductPhotos",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiDescription",
                table: "ProductPhotos");

            migrationBuilder.DropColumn(
                name: "AiTags",
                table: "ProductPhotos");

            migrationBuilder.DropColumn(
                name: "VectorEmbedding",
                table: "ProductPhotos");

            migrationBuilder.DropColumn(
                name: "VectorizedAt",
                table: "ProductPhotos");
        }
    }
}
