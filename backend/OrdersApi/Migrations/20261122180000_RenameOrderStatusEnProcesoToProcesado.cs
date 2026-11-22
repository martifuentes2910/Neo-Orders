using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdersApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderStatusEnProcesoToProcesado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // KAN-35: renombrar estado persistido; no tocar stock ni borrar pedidos.
            migrationBuilder.Sql(
                "UPDATE Orders SET Estado = 'Procesado' WHERE Estado = 'En proceso';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Orders SET Estado = 'En proceso' WHERE Estado = 'Procesado';");
        }
    }
}
