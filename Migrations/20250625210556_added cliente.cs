using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Management.Server.Migrations
{
    /// <inheritdoc />
    public partial class addedcliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Membresias",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_ClienteId",
                table: "Membresias",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Membresias_AspNetUsers_ClienteId",
                table: "Membresias",
                column: "ClienteId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Membresias_AspNetUsers_ClienteId",
                table: "Membresias");

            migrationBuilder.DropIndex(
                name: "IX_Membresias_ClienteId",
                table: "Membresias");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Membresias");
        }
    }
}
