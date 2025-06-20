# Desafio Clientes - Estrutura do Projeto

## Arquitetura

Este projeto segue as **melhores práticas do mercado** para projetos .NET com Entity Framework:

### Estrutura de Diretórios

```
src/
├── ApiBackend/
│   ├── Data/                    # Camada de dados
│   │   ├── ContextoApp.cs      # DbContext principal
│   │   └── Scripts/            # Scripts SQL organizados
│   │       ├── Views/          # Views do banco
│   │       ├── PostDeployment/ # Scripts pós-deployment
│   │       └── Seeds/          # Dados iniciais
│   ├── Features/               # Organização por feature
│   │   └── Clientes/          # Feature de clientes
│   │       ├── Controllers/    # API Controllers
│   │       ├── Models/        # Entidades do domínio
│   │       ├── Services/      # Lógica de negócio
│   │       ├── Repositories/  # Acesso a dados
│   │       ├── Dtos/         # Data Transfer Objects
│   │       └── Validators/    # Validações
│   └── Migrations/            # Entity Framework Migrations
```

## Organização de Scripts SQL

### ❌ Antes (Incorreto)
```
Scripts/                    # Na raiz do projeto
├── historico_cliente.sql   # Scripts soltos
└── vw_cliente_resumo.sql
```

### ✅ Agora (Correto - Padrão do Mercado)
```
src/ApiBackend/Data/Scripts/
├── Views/
│   └── vw_cliente_resumo.sql
├── PostDeployment/
│   └── 01_Views.sql
└── Seeds/
    └── 01_DadosIniciais.sql
```

## Vantagens da Nova Estrutura

1. **Versionamento**: Scripts ficam junto com o código
2. **Organização**: Separação clara por tipo de script
3. **Integração**: Scripts integrados com deployment
4. **Padrão**: Segue convenções da comunidade .NET
5. **Manutenção**: Facilita evolução e correções

## Gerenciamento de Banco

- **Schema**: Gerenciado via Entity Framework Migrations
- **Views/Functions**: Scripts em `PostDeployment/`
- **Dados Iniciais**: Scripts em `Seeds/`
- **Inicialização**: Automática via `DatabaseInitializationService`

## Como Usar

1. **Mudanças de Schema**: `dotnet ef migrations add NomeDaMigration`
2. **Views/Functions**: Adicione em `Scripts/Views/` e referencie em `PostDeployment/`
3. **Dados**: Use `Seeds/` para dados iniciais
4. **Deploy**: Execute `dotnet ef database update`
