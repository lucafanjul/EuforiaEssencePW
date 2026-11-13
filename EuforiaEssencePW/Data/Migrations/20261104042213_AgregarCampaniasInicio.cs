using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuforiaEssencePW.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCampaniasInicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campanias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Desde = table.Column<DateTime>(type: "date", nullable: false),
                    Hasta = table.Column<DateTime>(type: "date", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampaniasPerfumes",
                columns: table => new
                {
                    CampaniaId = table.Column<int>(type: "int", nullable: false),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaniasPerfumes", x => new { x.CampaniaId, x.PerfumeId });
                    table.ForeignKey(
                        name: "FK_CampaniasPerfumes_Campanias_CampaniaId",
                        column: x => x.CampaniaId,
                        principalTable: "Campanias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaniasPerfumes_Perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "Perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campanias_Activa_Desde_Hasta",
                table: "Campanias",
                columns: new[] { "Activa", "Desde", "Hasta" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaniasPerfumes_PerfumeId",
                table: "CampaniasPerfumes",
                column: "PerfumeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaniasPerfumes");

            migrationBuilder.DropTable(
                name: "Campanias");
        }
    }
}
