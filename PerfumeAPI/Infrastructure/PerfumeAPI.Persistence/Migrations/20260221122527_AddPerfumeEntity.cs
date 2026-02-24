using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PerfumeAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfumeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Perfumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RatingValue = table.Column<double>(type: "float", nullable: true),
                    RatingCount = table.Column<int>(type: "int", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    TopNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MiddleNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaseNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainAccord1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainAccord2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainAccord3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainAccord4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainAccord5 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Perfumer1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Perfumer2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Perfumer3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Perfumer4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmbeddingVector = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfumes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perfumes");
        }
    }
}
