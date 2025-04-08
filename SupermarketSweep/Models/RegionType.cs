namespace SupermarketSweep.Models;

public enum RegionType
{
    NorthAmerica = 0,
    Europe = 1,
    Japan = 2,
    Oceania = 3,
    Aether = 4,
    Crystal = 5,
    Dynamis = 6,
    Primal = 7,
    Chaos = 8,
    Light = 9,
    Elemental = 10,
    Gaia = 11,
    Mana = 12,
    Meteor = 13,
    Materia = 14
}

public static class RegionTypeExtensions
{
    public static string ToUniversalisString(this RegionType regionType)
    {
        switch (regionType)
        {
            case RegionType.NorthAmerica:
                return "north-america";
            case RegionType.Europe:
                return "europe";
            case RegionType.Japan:
                return "japan";
            case RegionType.Oceania:
                return "oceania";
            default:
                return regionType.ToString().ToLower();
        }
    }

    public static string ToFriendlyString(this RegionType regionType)
    {
        switch (regionType)
        {
            case RegionType.NorthAmerica:
                return "North America";
            default:
                return regionType.ToString();
        }
    }
}