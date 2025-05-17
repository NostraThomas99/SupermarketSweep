using ECommons.Configuration;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using SupermarketSweep.Models;

namespace SupermarketSweep;

public class Config : IEzConfig
{
    public bool AllCharactersInventory { get; set; } = false;
    public RegionType ShoppingRegion { get; set; } = RegionType.NorthAmerica;
    public bool ExpertMode { get; set; } = false;
    public int LifeStreamTimeout { get; set; } = 300;
    public bool RemoveQuantityAutomatically { get; set; }
    public bool UseVnavPathing { get; set; } = true;
}