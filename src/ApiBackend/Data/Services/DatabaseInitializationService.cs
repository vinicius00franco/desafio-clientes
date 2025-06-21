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

            if (scriptName.Contains("Trigger", StringComparison.OrdinalIgnoreCase))
                await ExecuteTriggerScript(sql, scriptName);
            else
                await ExecuteRegularScript(sql, scriptName);
        }

        private async Task ExecuteTriggerScript(string sql, string scriptName)
        {
            var triggerBlocks = ExtractTriggerBlocks(sql);

            foreach (var block in triggerBlocks)
            {
                if (string.IsNullOrWhiteSpace(block))
                    continue;

                try
                {
                    await _context.Database.ExecuteSqlRawAsync(block);
                    _logger.LogInformation($"Trigger executado com sucesso ({scriptName})");
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException is SqlException sqlEx
                        && (sqlEx.Number == 2714  // objeto já existe
                            || sqlEx.Number == 3701  // objeto não existe ao dropar
                            || sqlEx.Number == 111)) // CREATE TRIGGER deve ser primeira instrução
                {
                    _logger.LogWarning(
                        $"Trigger ignorado ({scriptName}) – {GetSqlErrorMessage(sqlEx.Number)}: {sqlEx.Message}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao executar trigger de {scriptName}: {ex.Message}");
                }
            }
        }

        private string GetSqlErrorMessage(int errorNumber) => errorNumber switch
        {
            2714 => "Objeto já existe",
            3701 => "Objeto não existe ao dropar",
            111  => "CREATE TRIGGER deve ser primeira instrução",
            _    => "Erro SQL"
        };

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

        private List<string> ExtractTriggerBlocks(string sql)
        {
            var blocks = new List<string>();

            // Padrão completo: IF EXISTS ... DROP + CREATE TRIGGER
            var fullPattern = @"(?s)(IF\s+EXISTS\s*\([^)]+\)\s*BEGIN\s*DROP\s+TRIGGER[^;]+?;\s*END)\s*(CREATE\s+TRIGGER[^;]*?END;?)";
            var fullMatches = Regex.Matches(sql, fullPattern, RegexOptions.IgnoreCase);

            foreach (Match match in fullMatches)
            {
                blocks.Add(match.Groups[1].Value.Trim());
                blocks.Add(match.Groups[2].Value.Trim());
            }

            // Se não achou, extrai DROP e CREATE separadamente
            if (blocks.Count == 0)
            {
                var dropPattern = @"IF\s+EXISTS\s*\([^)]+\)\s*BEGIN\s*DROP\s+TRIGGER[^;]+?;\s*END";
                foreach (Match m in Regex.Matches(sql, dropPattern, RegexOptions.IgnoreCase))
                    blocks.Add(m.Value.Trim());

                var createPattern = @"CREATE\s+TRIGGER\s+[^;]*?END;?";
                foreach (Match m in Regex.Matches(sql, createPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    blocks.Add(m.Value.Trim());
            }

            // Fallback final: split por CREATE TRIGGER
            if (blocks.Count == 0)
            {
                foreach (var part in Regex.Split(sql, @"(?=CREATE\s+TRIGGER)", RegexOptions.IgnoreCase))
                {
                    var t = part.Trim();
                    if (t.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
                        blocks.Add(t);
                }
            }

            return blocks;
        }
    }
}
