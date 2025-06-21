using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarViewClienteResumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remover view se já existe e recriar
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_cliente_resumo')
                BEGIN
                    DROP VIEW vw_cliente_resumo;
                END");

            // Criar view de resumo de clientes
            migrationBuilder.Sql(@"
                CREATE VIEW vw_cliente_resumo AS
                SELECT 
                    c.ClienteId,
                    c.Nome,
                    c.DataCadastroUtc,
                    COUNT(DISTINCT e.EnderecoId) AS TotalEnderecos,
                    COUNT(DISTINCT ct.ContatoId) AS TotalContatos
                FROM Clientes c
                LEFT JOIN Enderecos e ON e.ClienteId = c.ClienteId
                LEFT JOIN Contatos ct ON ct.ClienteId = c.ClienteId
                GROUP BY c.ClienteId, c.Nome, c.DataCadastroUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove a view em caso de rollback
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_cliente_resumo')
                BEGIN
                    DROP VIEW vw_cliente_resumo;
                END");
        }
    }
}
