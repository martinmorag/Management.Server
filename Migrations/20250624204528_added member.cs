using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Management.Server.Migrations
{
    /// <inheritdoc />
    public partial class addedmember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAjusteManual",
                table: "PagosMembresia",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsAjusteManual",
                table: "PagosMembresia");
        }
    }
}
