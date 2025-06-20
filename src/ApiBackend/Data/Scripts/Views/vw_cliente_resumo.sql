-- View de resumo de clientes
CREATE OR ALTER VIEW vw_cliente_resumo AS
SELECT 
    c.ClienteId,
    c.Nome,
    c.DataCadastroUtc,
    COUNT(DISTINCT e.EnderecoId) AS TotalEnderecos,
    COUNT(DISTINCT ct.ContatoId) AS TotalContatos
FROM Clientes c
LEFT JOIN Enderecos e ON e.ClienteId = c.ClienteId
LEFT JOIN Contatos ct ON ct.ClienteId = c.ClienteId
GROUP BY c.ClienteId, c.Nome, c.DataCadastroUtc;
