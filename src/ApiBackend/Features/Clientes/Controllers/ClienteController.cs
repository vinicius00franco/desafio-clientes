using Microsoft.AspNetCore.Mvc;
using ApiBackend.Features.Clientes.Models;

namespace ApiBackend.Features.Clientes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    // Exemplo de endpoint GET
    [HttpGet]
    public IActionResult GetClientes()
    {
        // Aqui você pode injetar um service ou repository futuramente
        return Ok(new List<Cliente>()); // Retorna lista vazia por enquanto
    }
}
