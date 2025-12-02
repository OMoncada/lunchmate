namespace CSE325_visioncoders.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
[BsonIgnoreExtraElements]

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonElement("orderDate")]
    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; set; }

    [BsonElement("email")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("quantity")]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [BsonElement("specials")]
    [JsonPropertyName("specials")]
    public List<Dictionary<string, string>> Specials { get; set; } = new();
}