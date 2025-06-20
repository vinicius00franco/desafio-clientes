using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ApiBackend.Data.Services;

public class DatabaseInitializationService
{
    private readonly ContextoApp _context;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseInitializationService(
        ContextoApp context, 
        ILogger<DatabaseInitializationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Initialize()
    {
        try
        {
            // Aplica migrations pendentes
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Migrations aplicadas com sucesso");

            // Executa scripts pós-deployment
            await ExecutePostDeploymentScripts();

            _logger.LogInformation("Inicialização do banco concluída");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante inicialização do banco");
            throw;
        }
    }

    private async Task ExecutePostDeploymentScripts()
    {
        try
        {
            // Caminho base para os scripts
            var scriptBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Scripts");
            
            // Buscar scripts de PostDeployment em ordem alfabética
            var scriptFiles = Directory.GetFiles(
                Path.Combine(scriptBasePath, "PostDeployment"), 
                "*.sql",
                SearchOption.TopDirectoryOnly)
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            _logger.LogInformation($"Encontrados {scriptFiles.Count} scripts para execução");

            foreach (var scriptFile in scriptFiles)
            {
                var scriptName = Path.GetFileName(scriptFile);
                _logger.LogInformation($"Executando script: {scriptName}");

                var sql = File.ReadAllText(scriptFile);
                await _context.Database.ExecuteSqlRawAsync(sql);
                
                _logger.LogInformation($"Script {scriptName} executado com sucesso");
            }

            _logger.LogInformation("Todos os scripts pós-deployment foram executados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar scripts pós-deployment");
            throw;
        }
    }
}
