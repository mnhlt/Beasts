using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Beasts.Api;

public static class PoeNinja
{
    private static readonly string IndexStateUrl = "https://poe.ninja/poe1/api/data/index-state";
    private static readonly string PoeNinjaUrlTemplate = "https://poe.ninja/api/data/itemoverview?league={0}&type=Beast";

    private class EconomyLeague
    {
        [JsonProperty("name")] public string Name;
    }

    private class IndexStateResponse
    {
        [JsonProperty("economyLeagues")] public List<EconomyLeague> EconomyLeagues;
    }

    private class PoeNinjaLine
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("chaosValue")] public float ChaosValue;
    }

    private class PoeNinjaResponse
    {
        [JsonProperty("lines")] public List<PoeNinjaLine> Lines;
    }

    public static async Task<List<string>> GetLeagues()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(IndexStateUrl);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Failed to get poe.ninja index-state");

        var json = await response.Content.ReadAsStringAsync();
        var indexState = JsonConvert.DeserializeObject<IndexStateResponse>(json);
        return indexState.EconomyLeagues.Select(l => l.Name).ToList();
    }

    public static async Task<Dictionary<string, float>> GetBeastsPrices(string league)
    {
        using var httpClient = new HttpClient();
        var url = string.Format(PoeNinjaUrlTemplate, league);
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Failed to get poe.ninja response");

        var json = await response.Content.ReadAsStringAsync();
        var poeNinjaResponse = JsonConvert.DeserializeObject<PoeNinjaResponse>(json);

        return poeNinjaResponse.Lines.ToDictionary(line => line.Name, line => line.ChaosValue);
    }
}
