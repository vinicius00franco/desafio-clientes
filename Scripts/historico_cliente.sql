-- Criação de histórico de alterações de clientes
CREATE TABLE HistoricoCliente (
    HistoricoId INT IDENTITY PRIMARY KEY,
    ClienteId INT NOT NULL,
    Nome NVARCHAR(255) NOT NULL,
    DataCadastroUtc DATETIME2 NOT NULL,
    DataAlteracaoUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Operacao NVARCHAR(20) NOT NULL
);
-- Exemplo de trigger para histórico (ajuste conforme necessário)
-- CREATE TRIGGER trg_Clientes_Historico ON Clientes AFTER INSERT, UPDATE, DELETE AS ...
