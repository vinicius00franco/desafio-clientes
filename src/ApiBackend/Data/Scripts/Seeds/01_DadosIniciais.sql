-- Script para dados iniciais (seeds)
-- Este script insere dados de teste/exemplo se necessário

-- Exemplo de dados de teste (descomente se necessário)
/*
IF NOT EXISTS (SELECT 1 FROM Clientes WHERE Nome = 'Cliente Exemplo')
BEGIN
    INSERT INTO Clientes (Nome, DataCadastroUtc) 
    VALUES ('Cliente Exemplo', GETUTCDATE())
END
*/
