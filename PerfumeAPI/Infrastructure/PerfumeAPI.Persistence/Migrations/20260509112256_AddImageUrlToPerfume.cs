using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToPerfume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Perfumes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Perfumes");
        }
    }
}
