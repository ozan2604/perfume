using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserPerfumeInteractions_UserId_InteractionDate",
                table: "UserPerfumeInteractions",
                columns: new[] { "UserId", "InteractionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPerfumeInteractions_UserId_InteractionDate",
                table: "UserPerfumeInteractions");
        }
    }
}
