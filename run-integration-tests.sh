#!/bin/bash

# Script para executar testes de integração com Testcontainers
# Requer Docker em execução

echo "🧪 EXECUTANDO TESTES DE INTEGRAÇÃO COM TESTCONTAINERS"
echo "=================================================="
echo ""

# Verificar se Docker está rodando
if ! docker info >/dev/null 2>&1; then
    echo "❌ ERRO: Docker não está rodando!"
    echo "   Por favor, inicie o Docker antes de executar os testes."
    exit 1
fi

echo "✅ Docker está rodando"
echo ""

# Navegar para o diretório do projeto
cd "$(dirname "$0")"

# Verificar se estamos no diretório correto
if [ ! -f "DesafioClientes.sln" ]; then
    echo "❌ ERRO: Não foi possível encontrar DesafioClientes.sln"
    echo "   Execute este script a partir do diretório raiz do projeto."
    exit 1
fi

echo "📁 Diretório: $(pwd)"
echo ""

# Limpar builds anteriores
echo "🧹 Limpando builds anteriores..."
dotnet clean --verbosity quiet
echo ""

# Restaurar dependências
echo "📦 Restaurando dependências..."
dotnet restore --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ ERRO: Falha ao restaurar dependências"
    exit 1
fi
echo ""

# Compilar projeto
echo "🔨 Compilando projeto..."
dotnet build --no-restore --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ ERRO: Falha na compilação"
    exit 1
fi
echo ""

echo "🐳 INICIANDO TESTES COM TESTCONTAINERS"
echo "   Isso pode demorar na primeira execução (download da imagem SQL Server)"
echo ""

# Executar todos os testes de integração
echo "🚀 Executando todos os testes de integração..."
dotnet test src/ApiBackend.Tests/ApiBackend.Tests.csproj \
    --no-build \
    --verbosity normal \
    --logger "console;verbosity=detailed" \
    --filter "FullyQualifiedName~Integration"

TEST_RESULT=$?

echo ""
if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ TODOS OS TESTES PASSARAM!"
    echo ""
    echo "🎉 SUCESSO: Testes de integração com Testcontainers executados com êxito!"
    echo ""
    echo "📊 O que foi testado:"
    echo "   ✅ ClienteService com banco SQL Server real"
    echo "   ✅ ClienteRepository com todas as operações CRUD"
    echo "   ✅ ClienteController com testes end-to-end"
    echo "   ✅ Integração completa: HTTP → Controller → Service → Repository → Database"
    echo "   ✅ Validações de negócio e tratamento de erros"
    echo "   ✅ Relacionamentos entre entidades"
    echo "   ✅ Migrations aplicadas automaticamente"
    echo ""
else
    echo "❌ ALGUNS TESTES FALHARAM!"
    echo ""
    echo "🔍 Para debugar:"
    echo "   1. Verifique os logs acima para detalhes específicos"
    echo "   2. Certifique-se que o Docker tem recursos suficientes"
    echo "   3. Execute testes individuais para isolar problemas:"
    echo "      dotnet test --filter 'ClienteServiceIntegrationTests'"
    echo "      dotnet test --filter 'ClienteRepositoryIntegrationTests'"
    echo "      dotnet test --filter 'ClienteControllerIntegrationTests'"
    echo ""
fi

exit $TEST_RESULT
