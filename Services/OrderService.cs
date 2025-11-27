namespace CSE325_visioncoders.Services;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using CSE325_visioncoders.Models;

public interface IOrderService
{
    Task<List<Order>> GetAsync();
}

public class OrderService : IOrderService
{
    private readonly IWebHostEnvironment _env;
    public OrderService(IWebHostEnvironment env) => _env = env;

    public async Task<List<Order>> GetAsync()
    {
        // Ajusta la ruta al archivo según dónde lo colocaste.
        // Recomendado: wwwroot/data/orderData.json
        var path = "wwwroot/data/orderData.json";
        using var fs = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<List<Order>>(fs,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data ?? new List<Order>();
    }
}