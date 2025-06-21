-- Trigger para registrar operações de DELETE na tabela Clientes
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Delete')
BEGIN
    DROP TRIGGER trg_HistoricoCliente_Delete;
END

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
END;
