# Scripts de Banco de Dados

Esta pasta contém scripts SQL organizados por categoria:

## Estrutura

- **Views/**: Views do banco de dados aplicadas pós-deployment
- **PostDeployment/**: Scripts executados após as migrations
- **Seeds/**: Scripts para inserção de dados iniciais

## Uso

1. **Migrations**: Use `dotnet ef migrations add` para mudanças de schema
2. **Views**: Coloque views em `Views/` e referencie em `PostDeployment/`
3. **Seeds**: Dados iniciais vão em `Seeds/`

## Ordem de Execução

1. Entity Framework Migrations
2. Scripts em PostDeployment/
3. Scripts em Seeds/ (se necessário)

## Convenções

- Prefixe arquivos com números para ordem: `01_`, `02_`, etc.
- Use comentários para documentar o propósito
- Teste sempre em ambiente de desenvolvimento primeiro
