using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedKasa.Siparis.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderItemUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `OrderItems`
                SET `UnitPrice` = ROUND(`UnitPrice` * `Quantity`, 2);
                """);

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "LineTotal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LineTotal",
                table: "OrderItems",
                newName: "UnitPrice");

            migrationBuilder.Sql("""
                UPDATE `OrderItems`
                SET `UnitPrice` = CASE
                    WHEN `Quantity` = 0 THEN `UnitPrice`
                    ELSE ROUND(`UnitPrice` / `Quantity`, 2)
                END;
                """);
        }
    }
}
