using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class mig2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPerfumeInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerfumeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLiked = table.Column<bool>(type: "bit", nullable: false),
                    InteractionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPerfumeInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPerfumeInteractions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPerfumeInteractions_Perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "Perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPerfumeInteractions_PerfumeId",
                table: "UserPerfumeInteractions",
                column: "PerfumeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPerfumeInteractions_UserId_PerfumeId",
                table: "UserPerfumeInteractions",
                columns: new[] { "UserId", "PerfumeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPerfumeInteractions");
        }
    }
}
