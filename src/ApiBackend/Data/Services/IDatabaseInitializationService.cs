using System.Threading.Tasks;

namespace ApiBackend.Data.Services
{
    /// <summary>
    /// Interface para o serviço de inicialização do banco de dados
    /// </summary>
    public interface IDatabaseInitializationService
    {
        /// <summary>
        /// Inicializa o banco de dados executando migrations e scripts necessários
        /// </summary>
        Task Initialize();
    }
}
