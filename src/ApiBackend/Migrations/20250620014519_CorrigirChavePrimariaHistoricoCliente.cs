using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiBackend.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirChavePrimariaHistoricoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricoClientes",
                columns: table => new
                {
                    HistoricoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataCadastroUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracaoUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Operacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoClientes", x => x.HistoricoId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoClientes");
        }
    }
}
