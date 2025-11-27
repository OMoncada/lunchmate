namespace CSE325_visioncoders.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Order
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    // specials es una lista de objetos con una única clave cada uno
    [JsonPropertyName("specials")]
    public List<Dictionary<string, string>> Specials { get; set; } = new();
}