using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Management.Server.Migrations
{
    /// <inheritdoc />
    public partial class addedm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagoMembresia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembresiaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MesDePago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CantidadPagada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaDePago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EsPagado = table.Column<bool>(type: "boolean", nullable: false),
                    MetodoPago = table.Column<string>(type: "text", nullable: true),
                    TransactionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoMembresia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoMembresia_Membresias_MembresiaId",
                        column: x => x.MembresiaId,
                        principalTable: "Membresias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoMembresia_MembresiaId",
                table: "PagoMembresia",
                column: "MembresiaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagoMembresia");
        }
    }
}
