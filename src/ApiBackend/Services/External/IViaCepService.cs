using ApiBackend.Features.Clientes.Dtos;

namespace ApiBackend.Services.External;

/// <summary>
/// Interface para o serviço de consulta de CEP via ViaCEP.
/// Permite criar mocks para testes de integração.
/// </summary>
public interface IViaCepService
{
    /// <summary>
    /// Obtém informações de endereço pelo CEP.
    /// </summary>
    /// <param name="cep">CEP para consulta (formato: 12345-678 ou 12345678)</param>
    /// <returns>Dados do endereço ou null se não encontrado</returns>
    Task<EnderecoCepDto?> ObterPorCep(string cep);
}
