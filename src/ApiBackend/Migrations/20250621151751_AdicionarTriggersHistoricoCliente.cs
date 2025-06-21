using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarTriggersHistoricoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Trigger para registrar operações de DELETE na tabela Clientes
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Delete')
                BEGIN
                    DROP TRIGGER trg_HistoricoCliente_Delete;
                END");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_HistoricoCliente_Delete
                ON Clientes
                AFTER DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO HistoricoClientes (ClienteId, Nome, DataCadastroUtc, DataAlteracaoUtc, Operacao)
                    SELECT 
                        d.ClienteId, 
                        d.Nome, 
                        d.DataCadastroUtc, 
                        GETUTCDATE(), 
                        'REMOCAO'
                    FROM deleted d;
                END;");

            // Trigger para registrar operações de UPDATE na tabela Clientes
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Update')
                BEGIN
                    DROP TRIGGER trg_HistoricoCliente_Update;
                END");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_HistoricoCliente_Update
                ON Clientes
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO HistoricoClientes (ClienteId, Nome, DataCadastroUtc, DataAlteracaoUtc, Operacao)
                    SELECT 
                        i.ClienteId, 
                        i.Nome, 
                        i.DataCadastroUtc, 
                        GETUTCDATE(), 
                        'ALTERACAO'
                    FROM inserted i;
                END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove os triggers em caso de rollback
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Delete')
                BEGIN
                    DROP TRIGGER trg_HistoricoCliente_Delete;
                END");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Update')
                BEGIN
                    DROP TRIGGER trg_HistoricoCliente_Update;
                END");
        }
    }
}
