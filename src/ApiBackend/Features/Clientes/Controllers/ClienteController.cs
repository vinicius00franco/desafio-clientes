using Microsoft.AspNetCore.Mvc;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Services;
using ApiBackend.Features.Clientes.Dtos;

namespace ApiBackend.Features.Clientes.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClienteController : ControllerBase
{
    private readonly ClienteService _clienteService;

    public ClienteController(ClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarCliente([FromBody] NovoClienteDto dto)
    {
        try
        {
            var clienteId = await _clienteService.CriarClienteAsync(dto);
            return CreatedAtAction(nameof(ObterPorId), new { id = clienteId }, new { id = clienteId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { erro = "Erro interno do servidor" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var cliente = await _clienteService.ObterPorIdAsync(id);
        
        if (cliente == null)
            return NotFound(new { erro = "Cliente não encontrado" });

        return Ok(cliente);
    }

    [HttpGet]
    public async Task<IActionResult> ListarTodos()
    {
        var clientes = await _clienteService.ListarTodosAsync();
        return Ok(clientes);
    }
}
