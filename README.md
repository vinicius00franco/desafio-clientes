# Desafio Clientes - Sistema de Gerenciamento

## 🏗️ Arquitetura do Projeto

Este projeto implementa um **sistema de gerenciamento de clientes** seguindo as **melhores práticas do mercado** para projetos .NET com Entity Framework Core:

### 📁 Estrutura de Diretórios

```
desafio-clientes/
├── DesafioClientes.sln          # Solution principal
├── docker-compose.yml           # SQL Server containerizado
├── README.md                    # Documentação do projeto
└── src/
    └── ApiBackend/              # API REST .NET 8
        ├── ApiBackend.csproj    # Configurações do projeto
        ├── Program.cs           # Entry point da aplicação
        ├── Data/                # 📊 Camada de Dados
        │   ├── ContextoApp.cs   # DbContext principal (EF Core)
        │   ├── Services/        # Serviços de dados
        │   │   └── DatabaseInitializationService.cs
        │   └── Scripts/         # 📜 Scripts SQL organizados
        │       ├── Views/       # Views do banco de dados
        │       ├── PostDeployment/ # Scripts pós-deployment
        │       ├── Seeds/       # Dados iniciais/sementes
        │       └── README.md    # Documentação dos scripts
        ├── Features/            # 🎯 Organização por Feature (Clean Architecture)
        │   └── Clientes/        # Feature de Gerenciamento de Clientes
        │       ├── Controllers/ # 🎮 API Controllers
        │       │   └── ClienteController.cs
        │       ├── Models/      # 📋 Entidades do Domínio
        │       │   ├── Cliente.cs
        │       │   ├── Endereco.cs
        │       │   ├── Contato.cs
        │       │   └── HistoricoCliente.cs
        │       ├── Services/    # 🔧 Lógica de Negócio
        │       ├── Repositories/ # 💾 Acesso a Dados (Repository Pattern)
        │       ├── Dtos/        # 📦 Data Transfer Objects
        │       └── Validators/  # ✅ Validações de entrada
        ├── Migrations/          # 🗃️ Entity Framework Migrations
        │   ├── 20250618165952_CriacaoInicial.cs
        │   ├── 20250620011627_AdicionarHistoricoCliente.cs
        │   └── ContextoAppModelSnapshot.cs
        └── Properties/          # ⚙️ Configurações da aplicação
            └── launchSettings.json
```

## 💽 Modelo de Dados

### Entidades Principais

- **Cliente**: Entidade principal com informações básicas
- **Endereco**: Endereços associados ao cliente (relacionamento 1:N)
- **Contato**: Contatos (telefone, email, etc.) do cliente (relacionamento 1:N)
- **HistoricoCliente**: Auditoria de mudanças no cliente

### Características Técnicas

- **Entity Framework Core 9.0**: ORM principal
- **SQL Server**: Banco de dados
- **Migrations**: Controle de versão do schema
- **Sequences**: Geração automática de IDs
- **Relacionamentos**: Configurados via Fluent API
- **Auditoria**: Histórico automático de alterações

## 🛠️ Tecnologias Utilizadas

### Backend (.NET 8)
- **ASP.NET Core 8.0**: Framework web
- **Entity Framework Core 9.0**: ORM
- **SQL Server**: Banco de dados
- **Swagger/OpenAPI**: Documentação da API
- **AutoMapper**: Mapeamento de DTOs para entidades
- **Clean Architecture**: Organização por features

#### 📦 APIs e Serviços Externos
- **ViaCEP**: Consulta automática de endereços por CEP
- **HttpClient**: Cliente HTTP configurado para APIs externas

#### 🎯 DTOs e AutoMapper
- Instalado `AutoMapper.Extensions.Microsoft.DependencyInjection`
- Criado `NovoClienteDto`, `NovoContatoDto` e `EnderecoCepDto`
- Perfil `ClienteMapper` configurado
- Registrado no `Program.cs`

#### 🌐 Serviço ViaCEP
- Criado `ViaCepService` usando `HttpClient`
- DTO de resposta em `Features/Clientes/Dtos`
- `AddHttpClient<ViaCepService>()` em `Program.cs`

#### 🚀 API - Endpoints Clientes
- **POST /api/clientes**: Cria cliente e busca endereço automaticamente via CEP
- **GET /api/clientes/{id}**: Busca cliente por ID com endereços e contatos
- **GET /api/clientes**: Lista todos os clientes

#### 🔧 Serviços de Negócio
- `ClienteService`: Centraliza lógica de criação e consulta
- Injeta `ContextoApp`, `ViaCepService` e `IMapper`
- Mapeia DTOs para entidades, busca dados do CEP e persiste

### Infraestrutura
- **Docker Compose**: SQL Server containerizado
- **Migrations**: Versionamento do banco
- **DatabaseInitializationService**: Inicialização automática

## 🚀 Scripts SQL Organizados

### ❌ Antes (Estrutura Incorreta)
```
Scripts/                    # ❌ Na raiz do projeto
├── historico_cliente.sql   # ❌ Scripts soltos sem organização
└── vw_cliente_resumo.sql   # ❌ Misturados com código
```

### ✅ Agora (Padrão de Mercado)
```
src/ApiBackend/Data/Scripts/
├── Views/                  # 👁️ Views do banco de dados
│   └── vw_cliente_resumo.sql
├── PostDeployment/        # 🚀 Scripts executados após deployment
│   └── 01_Views.sql
├── Seeds/                 # 🌱 Dados iniciais para desenvolvimento
│   └── 01_DadosIniciais.sql
└── README.md             # 📚 Documentação específica dos scripts
```

## ✨ Vantagens da Arquitetura Atual

### 🎯 Organização por Features
- **Modularidade**: Cada feature é independente
- **Escalabilidade**: Fácil adição de novas funcionalidades
- **Manutenibilidade**: Código organizado logicamente
- **Testabilidade**: Isolamento de responsabilidades

### 🗄️ Gerenciamento de Dados
1. **Versionamento**: Scripts versionados junto com o código
2. **Automação**: Execução automática na inicialização
3. **Organização**: Separação clara por tipo de script
4. **Padrão**: Segue convenções da comunidade .NET
5. **Integração**: Perfeita integração com CI/CD

### 🔧 Tecnologias Modernas
- **.NET 8**: Última versão LTS
- **EF Core 9**: ORM mais recente
- **Clean Architecture**: Padrão da indústria
- **Docker**: Containerização do banco

## 🎮 Como Desenvolver

### 📋 Pré-requisitos
- **.NET 8 SDK** instalado
- **Docker** para executar SQL Server
- **VS Code** ou **Visual Studio** (recomendado)

### 🚀 Execução Local

1. **Inicie o banco de dados**:
   ```bash
   docker-compose up -d
   ```

2. **Execute as migrations**:
   ```bash
   cd src/ApiBackend
   dotnet ef database update
   ```

3. **Inicie a API**:
   ```bash
   dotnet run
   ```

4. **Acesse a documentação**: `https://localhost:7008/swagger`

### 🛠️ Comandos Úteis

#### Desenvolvimento de Schema
```bash
# Criar nova migration
dotnet ef migrations add NomeDaMigration

# Aplicar migrations
dotnet ef database update

# Reverter migration
dotnet ef database update PreviousMigrationName
```

#### Gerenciamento de Scripts
- **Views/Functions**: Adicione em `Data/Scripts/Views/`
- **PostDeployment**: Configure em `Data/Scripts/PostDeployment/`
- **Seeds**: Adicione dados em `Data/Scripts/Seeds/`

### 📝 Padrões de Desenvolvimento

#### Adicionando Nova Feature
1. Crie pasta em `Features/NomeFeature/`
2. Organize por responsabilidade:
   - `Models/` - Entidades
   - `Controllers/` - API endpoints
   - `Services/` - Lógica de negócio
   - `Repositories/` - Acesso a dados
   - `Dtos/` - Contratos da API
   - `Validators/` - Validações

#### Configuração de Entidades
- Use `ContextoApp.cs` para configurar relacionamentos
- Implemente sequences para IDs automáticos
- Configure cascatas e indexes conforme necessário

## 📚 Recursos Adicionais

### Documentação
- **Swagger UI**: Disponível em desenvolvimento
- **README dos Scripts**: `src/ApiBackend/Data/Scripts/README.md`
- **Migrations**: Histórico completo no diretório `Migrations/`

### Configurações
- **Connection String**: `appsettings.Development.json`
- **Scripts Database**: `appsettings.DatabaseScripts.json`
- **Logs**: Configurados para desenvolvimento
