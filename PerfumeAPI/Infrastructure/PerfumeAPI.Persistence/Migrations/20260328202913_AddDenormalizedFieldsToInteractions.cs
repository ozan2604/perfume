using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDenormalizedFieldsToInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PerfumeName",
                table: "UserPerfumeInteractions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "UserPerfumeInteractions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserPerfumeInteractions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserSurname",
                table: "UserPerfumeInteractions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerfumeName",
                table: "UserPerfumeInteractions");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "UserPerfumeInteractions");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserPerfumeInteractions");

            migrationBuilder.DropColumn(
                name: "UserSurname",
                table: "UserPerfumeInteractions");
        }
    }
}
