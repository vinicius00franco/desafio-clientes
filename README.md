# Desafio Clientes

##  Visão Geral
API REST desenvolvida em .NET 8 para gerenciamento de clientes, endereços e contatos, com persistência em SQL Server e integração automática com a API ViaCEP para preenchimento de endereços.

## 🛠️ Tecnologias
- **.NET 8**: Framework principal
- **Entity Framework Core**: ORM para acesso aos dados
- **SQL Server 2022**: Banco de dados relacional
- **AutoMapper**: Mapeamento de DTOs
- **Swagger/OpenAPI**: Documentação da API
- **Docker**: Containerização do banco de dados

## ⚙️ Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (para SQL Server)

## 🚀 Configuração e Execução

### 1. Configurar Banco de Dados
```bash
docker-compose up -d
```

### 2. Aplicar Migrations
```bash
cd src/ApiBackend
dotnet ef database update
```

### 3. Executar API
```bash
dotnet run
```

A API estará disponível em:
- **HTTPS**: `https://localhost:5009`
- **HTTP**: `http://localhost:5009`
- **Swagger UI**: `https://localhost:5009/swagger`

## 📊 Banco de Dados

### SQL Server (Produção/Desenvolvimento)
Configurado via Docker Compose para desenvolvimento:

**Configuração**:
- **Imagem**: `mcr.microsoft.com/mssql/server:2022-latest`
- **Porta**: `1433`
- **Credenciais**: `sa` / `SeuPass#Seguro123`
- **Database**: `ClientesDb`
- **Volume**: Dados persistem no volume `sql_dados`

### Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost,1433;Database=ClientesDb;User Id=sa;Password=SeuPass#Seguro123;TrustServerCertificate=true;"
  }
}
```

### Banco de Dados para Testes
Os testes utilizam **Entity Framework InMemory Provider**:

```csharp
// ApiBackend.Tests - Configuração automática
services.AddDbContext<ContextoApp>(options =>
    options.UseInMemoryDatabase("TestDb"));
```

**Características**:
- **Isolamento**: Cada teste usa uma instância limpa
- **Performance**: Execução rápida sem I/O de disco
- **Simplicidade**: Não requer Docker para testes
- **Limitações**: Não valida constraints SQL específicas

### Inicialização Automática
O banco é configurado automaticamente na inicialização:

```csharp
// Program.cs - DatabaseInitializationService
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider
        .GetRequiredService<IDatabaseInitializationService>();
    await dbInitializer.Initialize();
}
```

## 🔗 Endpoints da API

### Criar Cliente
```http
POST /api/clientes
Content-Type: application/json

{
  "nome": "João Silva",
  "cep": "01310-100",
  "numero": "123",
  "complemento": "Apt 45",
  "contatos": [
    { "tipo": "Email", "valor": "joao@example.com" },
    { "tipo": "Telefone", "valor": "11999999999" }
  ]
}
```
**Resposta**: `201 Created` com o ID do cliente criado.

### Buscar Cliente
```http
GET /api/clientes/{id}
```
**Resposta**: JSON com dados completos do cliente, endereços e contatos.

### Listar Clientes
```http
GET /api/clientes
```
**Resposta**: Array JSON com resumo de todos os clientes.

## 🧪 Testes

### Executar Todos os Testes
```bash
cd src/ApiBackend.Tests
dotnet test --verbosity normal
```

### Testes Específicos
```bash
# Teste de integração específico
dotnet test --filter "FullyQualifiedName~ClienteControllerIntegracaoBasicosTests"

# Testes unitários (banco em memória)
dotnet test --filter "Category=Unit"

# Testes de integração (SQL Server real)
dotnet test --filter "Category=Integration"
```

## 📝 Recursos Adicionais

- **Integração ViaCEP**: Preenchimento automático de endereços por CEP
- **Validações**: Dados obrigatórios e formatos validados
- **Auditoria**: Histórico automático de alterações
- **Documentação**: Swagger UI disponível em desenvolvimento
