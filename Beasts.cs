using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beasts.Api;
using Beasts.Data;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace Beasts;

public partial class Beasts : BaseSettingsPlugin<BeastsSettings>
{
    private readonly Dictionary<long, Entity> _trackedBeasts = new();

    public override void OnLoad()
    {
        Settings.FetchBeastPrices.OnPressed += async () => await FetchPrices();
        Task.Run(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        try
        {
            DebugWindow.LogMsg("Fetching league list from PoeNinja...");
            var leagues = await PoeNinja.GetLeagues();
            Settings.AvailableLeagues = leagues;

            if (string.IsNullOrEmpty(Settings.SelectedLeague) && leagues.Count > 0)
            {
                Settings.SelectedLeague = leagues[0];
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"Failed to fetch league list: {ex.Message}");
        }

        await FetchPrices();
    }

    private async Task FetchPrices()
    {
        var league = Settings.SelectedLeague;
        if (string.IsNullOrEmpty(league))
        {
            DebugWindow.LogMsg("No league selected, skipping price fetch.");
            return;
        }

        DebugWindow.LogMsg($"Fetching Beast Prices from PoeNinja for league: {league}...");
        var prices = await PoeNinja.GetBeastsPrices(league);
        foreach (var beast in BeastsDatabase.AllBeasts)
        {
            Settings.BeastPrices[beast.DisplayName] = prices.TryGetValue(beast.DisplayName, out var price) ? price : -1;
        }

        Settings.LastUpdate = DateTime.Now;
    }

    public override void AreaChange(AreaInstance area)
    {
        _trackedBeasts.Clear();
    }

    public override void EntityAdded(Entity entity)
    {
        if (entity.Rarity != MonsterRarity.Rare) return;
        foreach (var _ in BeastsDatabase.AllBeasts.Where(beast => entity.Metadata == beast.Path))
        {
            _trackedBeasts.Add(entity.Id, entity);
        }
    }

    public override void EntityRemoved(Entity entity)
    {
        if (_trackedBeasts.ContainsKey(entity.Id))
        {
            _trackedBeasts.Remove(entity.Id);
        }
    }
}