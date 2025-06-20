-- Trigger para registrar operações de DELETE na tabela Clientes
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Delete')
    DROP TRIGGER trg_HistoricoCliente_Delete;
GO

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
GO

-- Trigger para registrar operações de UPDATE na tabela Clientes
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_HistoricoCliente_Update')
    DROP TRIGGER trg_HistoricoCliente_Update;
GO

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
END;
GO
