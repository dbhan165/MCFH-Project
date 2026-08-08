using System;
using System.Text.Json;

public class MentionQueryDto
{
    public string? Platform { get; set; }
    public string? Sentiment { get; set; }
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool ExcludeMuted { get; set; } = true;
    public bool? IsCrisisAlert { get; set; }
}

public class Program
{
    public static void Main()
    {
        var json = "{\"platform\":\"all\",\"sentiment\":\"all\",\"search\":\"vietnam\",\"dateFrom\":null,\"dateTo\":null,\"excludeMuted\":true,\"isCrisisAlert\":null}";
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        try 
        {
            var config = JsonSerializer.Deserialize<MentionQueryDto>(json, options);
            Console.WriteLine("Success!");
            Console.WriteLine($"Search: {config.Search}");
        } 
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
