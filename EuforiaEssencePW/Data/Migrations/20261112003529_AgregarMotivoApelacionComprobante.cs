using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuforiaEssencePW.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMotivoApelacionComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoApelacion",
                table: "ComprobantesPago",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoApelacion",
                table: "ComprobantesPago");
        }
    }
}
