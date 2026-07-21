using Microsoft.EntityFrameworkCore.Migrations;
using YG.Modules.Catalog.Domain;

#nullable disable

namespace YG.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributesShapeComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ProductAttributes>(
                name: "Attributes",
                schema: "catalog",
                table: "products",
                type: "jsonb",
                nullable: false,
                comment: "Free-form product attributes: {key: value}, varies per product. Searchable via the GIN index.",
                oldClrType: typeof(ProductAttributes),
                oldType: "jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ProductAttributes>(
                name: "Attributes",
                schema: "catalog",
                table: "products",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(ProductAttributes),
                oldType: "jsonb",
                oldComment: "Free-form product attributes: {key: value}, varies per product. Searchable via the GIN index.");
        }
    }
}
