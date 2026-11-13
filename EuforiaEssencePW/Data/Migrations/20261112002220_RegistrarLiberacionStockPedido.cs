using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuforiaEssencePW.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarLiberacionStockPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StockLiberado",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockLiberado",
                table: "Pedidos");
        }
    }
}
