using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Management.Server.Migrations
{
    /// <inheritdoc />
    public partial class addedmemememeasdasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PagosMembresia_MembresiaId",
                table: "PagosMembresia");

            migrationBuilder.CreateTable(
                name: "AjustesDeudaManual",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembresiaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MesAplicado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CantidadAjustada = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TipoAjusteInterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    FechaDeRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegistradoPor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesDeudaManual", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesDeudaManual_Membresias_MembresiaId",
                        column: x => x.MembresiaId,
                        principalTable: "Membresias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosMembresia_MembresiaId_MesDePago",
                table: "PagosMembresia",
                columns: new[] { "MembresiaId", "MesDePago" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AjustesDeudaManual_MembresiaId",
                table: "AjustesDeudaManual",
                column: "MembresiaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AjustesDeudaManual");

            migrationBuilder.DropIndex(
                name: "IX_PagosMembresia_MembresiaId_MesDePago",
                table: "PagosMembresia");

            migrationBuilder.CreateIndex(
                name: "IX_PagosMembresia_MembresiaId",
                table: "PagosMembresia",
                column: "MembresiaId");
        }
    }
}
