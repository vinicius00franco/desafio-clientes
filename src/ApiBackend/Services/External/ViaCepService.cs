using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApiBackend.Features.Clientes.Dtos;

namespace ApiBackend.Services.External;

public class ViaCepService
{
    private readonly HttpClient _httpClient;

    public ViaCepService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EnderecoCepDto?> ObterPorCepAsync(string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return null;

        var cepLimpo = cep.Replace("-", "").Replace(".", "");

        try
        {
            var resultado = await _httpClient.GetFromJsonAsync<EnderecoCepDto>(
                $"https://viacep.com.br/ws/{cepLimpo}/json/");

            return resultado;
        }
        catch
        {
            return null;
        }
    }
}
