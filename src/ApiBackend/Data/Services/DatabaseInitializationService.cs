using Microsoft.EntityFrameworkCore;

namespace ApiBackend.Data.Services;

public class DatabaseInitializationService
{
    private readonly ContextoApp _context;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(ContextoApp context, ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Aplica migrations pendentes
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Migrations aplicadas com sucesso");

            // Executa scripts pós-deployment
            await ExecutePostDeploymentScriptsAsync();
            
            _logger.LogInformation("Inicialização do banco concluída");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante inicialização do banco");
            throw;
        }
    }

    private Task ExecutePostDeploymentScriptsAsync()
    {
        // Aqui você pode executar scripts SQL específicos
        // Por exemplo, criar views, procedures, etc.
        
        _logger.LogInformation("Scripts pós-deployment executados");
        
        return Task.CompletedTask;
    }
}
