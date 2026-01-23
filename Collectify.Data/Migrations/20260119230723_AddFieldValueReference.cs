using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldValueReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FieldValueReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FieldValueId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldValueReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldValueReferences_Items_RelatedItemId",
                        column: x => x.RelatedItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldValueReferences_Values_FieldValueId",
                        column: x => x.FieldValueId,
                        principalTable: "Values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldValueReferences_FieldValueId_RelatedItemId",
                table: "FieldValueReferences",
                columns: new[] { "FieldValueId", "RelatedItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldValueReferences_RelatedItemId",
                table: "FieldValueReferences",
                column: "RelatedItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldValueReferences");
        }
    }
}
