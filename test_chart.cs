using System;
using System.Text.Json;

var sentimentChartConfig = new {
    type = "outlabeledPie",
    data = new {
        labels = new[] { "Tích cực", "Tiêu cực", "Trung lập" },
        datasets = new[] {
            new {
                data = new[] { 9, 2, 5 },
                backgroundColor = new[] { "#10B981", "#EF4444", "#64748B" }
            }
        }
    },
    options = new { 
        legend = new { display = false },
        plugins = new { outlabels = new { text = "%l %p", color = "white", stretch = 35, font = new { resizable = true, minSize = 12, maxSize = 18 } } } 
    }
};

var json = JsonSerializer.Serialize(sentimentChartConfig, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
Console.WriteLine($"https://quickchart.io/chart?c={Uri.EscapeDataString(json)}");
