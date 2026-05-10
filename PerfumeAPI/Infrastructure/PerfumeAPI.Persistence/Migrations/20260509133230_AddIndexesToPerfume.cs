using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesToPerfume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Perfumes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Perfumes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Perfumes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Brand",
                table: "Perfumes",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Country",
                table: "Perfumes",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Gender",
                table: "Perfumes",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Price",
                table: "Perfumes",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_RatingValue",
                table: "Perfumes",
                column: "RatingValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Perfumes_Brand",
                table: "Perfumes");

            migrationBuilder.DropIndex(
                name: "IX_Perfumes_Country",
                table: "Perfumes");

            migrationBuilder.DropIndex(
                name: "IX_Perfumes_Gender",
                table: "Perfumes");

            migrationBuilder.DropIndex(
                name: "IX_Perfumes_Price",
                table: "Perfumes");

            migrationBuilder.DropIndex(
                name: "IX_Perfumes_RatingValue",
                table: "Perfumes");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Perfumes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Perfumes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand",
                table: "Perfumes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
