using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Management.Server.Migrations
{
    /// <inheritdoc />
    public partial class addedme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PagoMembresia_Membresias_MembresiaId",
                table: "PagoMembresia");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PagoMembresia",
                table: "PagoMembresia");

            migrationBuilder.RenameTable(
                name: "PagoMembresia",
                newName: "PagosMembresia");

            migrationBuilder.RenameIndex(
                name: "IX_PagoMembresia_MembresiaId",
                table: "PagosMembresia",
                newName: "IX_PagosMembresia_MembresiaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PagosMembresia",
                table: "PagosMembresia",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PagosMembresia_Membresias_MembresiaId",
                table: "PagosMembresia",
                column: "MembresiaId",
                principalTable: "Membresias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PagosMembresia_Membresias_MembresiaId",
                table: "PagosMembresia");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PagosMembresia",
                table: "PagosMembresia");

            migrationBuilder.RenameTable(
                name: "PagosMembresia",
                newName: "PagoMembresia");

            migrationBuilder.RenameIndex(
                name: "IX_PagosMembresia_MembresiaId",
                table: "PagoMembresia",
                newName: "IX_PagoMembresia_MembresiaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PagoMembresia",
                table: "PagoMembresia",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PagoMembresia_Membresias_MembresiaId",
                table: "PagoMembresia",
                column: "MembresiaId",
                principalTable: "Membresias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
