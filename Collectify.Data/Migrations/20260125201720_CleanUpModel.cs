using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectify.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsList",
                table: "FieldDefinitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsList",
                table: "FieldDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
