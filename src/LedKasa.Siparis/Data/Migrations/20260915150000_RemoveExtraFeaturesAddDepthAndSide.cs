using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LedKasa.Siparis.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtraFeaturesAddDepthAndSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemExtraFeatures");

            migrationBuilder.DropTable(
                name: "ExtraFeatures");

            migrationBuilder.Sql("UPDATE `OrderItems` SET `WidthCm` = ROUND(`WidthCm`), `HeightCm` = ROUND(`HeightCm`);");

            migrationBuilder.AlterColumn<int>(
                name: "WidthCm",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "HeightCm",
                table: "OrderItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "DepthCm",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "Side",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepthCm",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Side",
                table: "OrderItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "WidthCm",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "HeightCm",
                table: "OrderItems",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "ExtraFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraFeatures", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ExtraFeatures",
                columns: new[] { "Id", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, "Köşe kesim", 1 },
                    { 2, true, "Delik", 2 },
                    { 3, true, "Askı aparatı", 3 },
                    { 4, true, "Su oluğu", 4 },
                    { 5, true, "Özel renk", 5 },
                    { 6, true, "Modül yönü değişikliği", 6 }
                });

            migrationBuilder.CreateTable(
                name: "OrderItemExtraFeatures",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    ExtraFeatureId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemExtraFeatures", x => new { x.OrderItemId, x.ExtraFeatureId });
                    table.ForeignKey(
                        name: "FK_OrderItemExtraFeatures_ExtraFeatures_ExtraFeatureId",
                        column: x => x.ExtraFeatureId,
                        principalTable: "ExtraFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemExtraFeatures_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFeatures_Name",
                table: "ExtraFeatures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemExtraFeatures_ExtraFeatureId",
                table: "OrderItemExtraFeatures",
                column: "ExtraFeatureId");
        }
    }
}
