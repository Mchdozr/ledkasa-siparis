using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedKasa.Siparis.Data.Migrations
{
    /// <inheritdoc />
    public partial class WipeOrdersAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM `OrderItems`;
                DELETE FROM `Orders`;
                DELETE FROM `AuditLogs`;
                ALTER TABLE `OrderItems` AUTO_INCREMENT = 1;
                ALTER TABLE `Orders` AUTO_INCREMENT = 1;
                ALTER TABLE `AuditLogs` AUTO_INCREMENT = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
