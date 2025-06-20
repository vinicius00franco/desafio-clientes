-- Arquivo para aplicação de views pós-deployment
-- Este script deve ser executado após as migrations

-- View de resumo de clientes
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_cliente_resumo')
BEGIN
    EXEC('
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
    GROUP BY c.ClienteId, c.Nome, c.DataCadastroUtc
    ')
END
ELSE
BEGIN
    -- Atualiza a view se já existe
    EXEC('
    ALTER VIEW vw_cliente_resumo AS
    SELECT 
        c.ClienteId,
        c.Nome,
        c.DataCadastroUtc,
        COUNT(DISTINCT e.EnderecoId) AS TotalEnderecos,
        COUNT(DISTINCT ct.ContatoId) AS TotalContatos
    FROM Clientes c
    LEFT JOIN Enderecos e ON e.ClienteId = c.ClienteId
    LEFT JOIN Contatos ct ON ct.ClienteId = c.ClienteId
    GROUP BY c.ClienteId, c.Nome, c.DataCadastroUtc
    ')
END
