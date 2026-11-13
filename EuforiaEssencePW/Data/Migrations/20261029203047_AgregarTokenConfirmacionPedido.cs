using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuforiaEssencePW.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTokenConfirmacionPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TokenConfirmacion",
                table: "Pedidos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_TokenConfirmacion",
                table: "Pedidos",
                column: "TokenConfirmacion",
                unique: true,
                filter: "[TokenConfirmacion] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pedidos_TokenConfirmacion",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TokenConfirmacion",
                table: "Pedidos");
        }
    }
}
