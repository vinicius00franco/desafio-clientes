using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApiBackend.Data.Services
{
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
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Migrations aplicadas com sucesso");

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
            var scriptBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Scripts");
            var postDeploymentPath = Path.Combine(scriptBasePath, "PostDeployment");

            if (!Directory.Exists(postDeploymentPath))
                Directory.CreateDirectory(postDeploymentPath);

            var scriptFiles = Directory
                .GetFiles(postDeploymentPath, "*.sql", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName)
                .ToList();

            _logger.LogInformation($"Encontrados {scriptFiles.Count} scripts para execução");

            foreach (var scriptFile in scriptFiles)
            {
                var scriptName = Path.GetFileName(scriptFile);
                _logger.LogInformation($"Executando script: {scriptName}");

                await ExecuteScript(scriptFile, scriptName);
            }

            _logger.LogInformation("Todos os scripts pós-deployment foram processados");
        }

        private async Task ExecuteScript(string scriptFile, string scriptName)
        {
            var sql = File.ReadAllText(scriptFile);
            sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);

            // Ignorar arquivos de trigger - eles agora são gerenciados via migrations
            if (scriptName.Contains("Trigger", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Script de trigger ignorado ({scriptName}) - triggers são gerenciados via migrations");
                return;
            }

            await ExecuteRegularScript(sql, scriptName);
        }

        private async Task ExecuteRegularScript(string sql, string scriptName)
        {
            var batches = Regex.Split(
                sql,
                @"^\s*GO\s*$(?:\r\n?|\n)?",
                RegexOptions.Multiline | RegexOptions.IgnoreCase
            );

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                    continue;

                try
                {
                    await _context.Database.ExecuteSqlRawAsync(batch);
                    _logger.LogInformation($"Batch executado com sucesso ({scriptName})");
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException is SqlException sqlEx
                        && (sqlEx.Number == 2714 || sqlEx.Number == 3701))
                {
                    _logger.LogWarning(
                        $"Batch ignorado ({scriptName}) – objeto já existe ou não existe: {sqlEx.Message}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao executar batch de {scriptName}: {ex.Message}");
                }
            }
        }
    }
}
