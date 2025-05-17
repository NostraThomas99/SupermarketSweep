using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Lumina.Excel.Sheets;
using NostraLib;
using OtterGui;
using OtterGui.Classes;
using SupermarketSweep.Models;
using SupermarketSweep.IPC;
using SupermarketSweep.UI;

namespace SupermarketSweep.UI;

public class MBShoppingList_UI : NostraWindow
{
    private SupermarketSweep _manager;
    private FileDialogManager _fileDialogManager;
    private MbShoppingListUiSelector _selector;
    private ResultsTable _resultsTable;
    private List<MarketDataListing> _marketData = new();

    public MBShoppingList_UI(SupermarketSweep manager) : base("Supermarket Sweep", ImGuiWindowFlags.None, false)
    {
        _manager = manager;
        _fileDialogManager = new FileDialogManager();
        _selector = new MbShoppingListUiSelector(_manager);
        _resultsTable = new ResultsTable(_manager, _marketData);
        TitleBarButtons.Add(new TitleBarButton()
        {
            AvailableClickthrough = true,
            Click = _ => manager.OpenConfigUi(),
            Icon = FontAwesomeIcon.Cog,
            Priority = 3,
            ShowTooltip = () =>
            {
                ImGui.BeginTooltip();
                ImGui.Text("Open Config Menu");
                ImGui.EndTooltip();
            }
        });
    }

    public override void Draw()
    {
        DrawItemAdd();
        if (ImGui.Button("Import MakePlace List"))
        {
            SelectFile();
        }

        ImGui.SameLine();
        if (ImGui.Button("Import from Clipboard"))
        {
            ExtractClipboardText();
        }

        ImGuiEx.Tooltip("Import items from clipboard using the following format:\n'10x Ipe Log'\n'999x Iron Ore");

        if (ImGuiUtil.GenericEnumCombo("Region/Datacenter", 300, SupermarketSweep.Config.ShoppingRegion,
                out RegionType newRegion, r => r.ToFriendlyString()))
        {
            SupermarketSweep.Config.ShoppingRegion = newRegion;
            EzConfig.Save();
        }

        ImGui.SetNextItemWidth(100);


        if (SupermarketSweep.Config.ExpertMode)
        {
            if (ImGuiUtil.DrawDisabledButton("Refresh All Market Data", new Vector2(0, 0),
                    "Refresh all Market Data for all items in the list\n(Can only be run every 30 seconds)\nWARNING: This will cause the Universalis servers to suffer!",
                    DateTime.Now < _lastMassRefresh + TimeSpan.FromSeconds(30)))
            {
                _lastMassRefresh = DateTime.Now;
                var insult = GetRandomInsult();
                Svc.Chat.Print($"[Reborn Toolbox] {insult}");
                foreach (var item in _manager.WantedItems)
                {
                    item.ClearDataResponse();
                    Task.Run(item.GetMarketDataResponseAsync);
                }
            }
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();

        _selector.Draw(200);
        ImGui.SameLine();
        DrawWantedItem(_selector.Current);

        _fileDialogManager.Draw();
    }

    private void ExtractClipboardText()
    {
        var clipboardText = ImGuiUtil.GetClipboardText();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            try
            {
                Dictionary<string, int> items = new Dictionary<string, int>();

                // Regex pattern
                var pattern = @"\b(\d+)x\s(.+)\b";
                var matches = Regex.Matches(clipboardText, pattern);

                // Loop through matches and add them to dictionary
                foreach (Match match in matches)
                {
                    var quantity = int.Parse(match.Groups[1].Value);
                    var itemName = match.Groups[2].Value;
                    items[itemName] = quantity;
                }

                bool saveNeeded = false;
                foreach (var item in items)
                {
                    var itemObj = SupermarketSweep.AllItems.FirstOrNull(i =>
                        string.Equals(i.Name.ToString(), item.Key, StringComparison.OrdinalIgnoreCase));
                    if (itemObj is null)
                    {
                        Svc.Log.Error($"Item {item.Key} not found");
                        continue;
                    }

                    var shoppingListItem = new ShoppingListItem(itemObj.Value, item.Value);
                    _manager.WantedItems.Add(shoppingListItem);
                    saveNeeded = true;
                }

                if (saveNeeded)
                    _manager.SaveList();
            }
            catch (Exception e)
            {
                Svc.Chat.PrintError("[Reborn Toolbox] Error importing clipboard text. See /xllog for details.");
                Svc.Log.Error($"Error importing from clipboard: {e}");
            }
        }
        else
        {
            Svc.Chat.PrintError($"Clipboard text is empty or invalid");
        }
    }

    private DateTime _lastMassRefresh = DateTime.MinValue;

    private void DrawWantedItem(ShoppingListItem? item)
    {
        ImGui.BeginChild("Wanted Item");
        if (item is null)
        {
            ImGui.Text("No item selected");
            ImGui.EndChild();
            return;
        }

        if (ImGui.Selectable(item.Name))
        {
            var seString = new SeStringBuilder().AddText($"[Reborn Toolbox]").AddItemLink(item.ItemId).BuiltString;
            Svc.Chat.Print(seString);
        }

        ImGuiEx.Tooltip("Click to print item link in chat");


        int quantity = (int)item.Quantity;
        ImGui.PushItemWidth(100);
        if (ImGui.InputInt("Needed Quantity", ref quantity))
        {
            item.Quantity = quantity;
            _manager.SaveList();
        }

        ImGui.PopItemWidth();

        ImGui.Text($"Already Owned: {item.InventoryCount}");
        ImGuiEx.Tooltip(
            "Amount of this item you have across all characters (including retainers and alts)\nSourced from Allagan Tools\nSee Allagan Tools for detailed information");

        string buttonLabel;
        string buttonDescription;
        bool buttonDisabled;

        if (item.IsMarketable)
        {
            DrawItemSearch(item);
            if (item.IsFetchingData)
            {
                buttonLabel = $"Fetching data...";
                buttonDescription = $"Currently fetching data from Universalis. Retries: {item.Retries}";
                buttonDisabled = true;
            }
            else if (item.MarketDataResponse == null)
            {
                buttonLabel = "Pull Market Data";
                buttonDescription = $"Pull Market Data from Universalis.";
                buttonDisabled = false;
            }
            else
            {
                buttonLabel = "Refresh Market Data";
                buttonDescription = $"Refresh Market Data from Universalis.";
                buttonDisabled = false;
            }

            if (OtterGui.ImGuiUtil.DrawDisabledButton(buttonLabel, new Vector2(0, 0), buttonDescription,
                    buttonDisabled))
            {
                if (item.MarketDataResponse != null)
                    item.ClearDataResponse();

                Task.Run(item.GetMarketDataResponseAsync);
            }
        }
        else
        {
            ImGui.Text("This item cannot be purchased on the Market Board");
        }

        var individualItem = SupermarketSweep.Config.ShowIndividualListings;
        if (ImGui.Checkbox("Show individual listings", ref individualItem))
        {
            SupermarketSweep.Config.ShowIndividualListings = individualItem;
            EzConfig.Save();
        }

        // If data is present, display it
        if (item.MarketDataResponse != null && !item.IsFetchingData)
        {
            var resultsTable = new ResultsTable(_manager, item.MarketDataResponse.Listings);
            resultsTable.Draw(10);
        }

        ImGui.EndChild();
    }

    private unsafe void DrawItemSearch(ShoppingListItem item)
    {
        AddonItemSearch* addonItemSearch = (AddonItemSearch*)Svc.GameGui.GetAddonByName("ItemSearch");
        var disabled = addonItemSearch == null;
        var description = disabled
            ? "Automatically search for this item on the Marketboard (MarketBoard window must be open)"
            : "Automatically search for this item on the Marketboard";
        if (ImGuiUtil.DrawDisabledButton($"Search Marketboard for Item##{item.ItemId}", new Vector2(), description,
                disabled))
        {
            addonItemSearch->SearchTextInput->SetText(item.Name);
            addonItemSearch->RunSearch();
        }
    }

    private void SelectFile()
    {
        void Callback(bool finished, string path)
        {
            if (finished && !string.IsNullOrEmpty(path))
            {
                string text = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ExtractItemsFromMakePlaceList(text, "Furniture", "Dyes");
                    ExtractItemsFromMakePlaceList(text, "Dyes", "Furniture (With Dye)");
                    _manager.SaveList();
                }
            }
        }

        _fileDialogManager.OpenFileDialog("Select MakePlace List File", "MakePlace List Files{.txt}", Callback);
    }

    private string _searchTerm = string.Empty;
    private Item? _selectedItem;

    private void DrawItemAdd()
    {
        ImGui.InputText("##searchBar", ref _searchTerm, 100);

        if (!string.IsNullOrEmpty(_searchTerm) && _selectedItem is null)
        {
            var matchingItems = SupermarketSweep.MarketableItems.Where(item =>
                item.Name.ToString().Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));

            if (matchingItems.Any())
            {
                ImGui.BeginChild($"ItemList", new Vector2(0, 150), true);
                foreach (var item in matchingItems)
                {
                    if (ImGui.Selectable(item.Name.ToString()))
                    {
                        _selectedItem = item;
                        _searchTerm = item.Name.ToString();
                    }
                }

                ImGui.EndChild();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Add"))
        {
            if (_selectedItem is null)
            {
                Svc.Log.Warning("No item to add to shopping list");
                return;
            }

            var shoppingListItem = new ShoppingListItem(_selectedItem.Value, 1);
            _manager.WantedItems.Add(shoppingListItem);
            _searchTerm = string.Empty;
            _selectedItem = null;
            _manager.SaveList();
            Svc.Log.Debug($"Added shopping list item: {shoppingListItem.Name}");
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            _selectedItem = null;
            _searchTerm = string.Empty;
        }
    }

    private unsafe void DrawMBButton(ShoppingListItem item)
    {
        var mbAddon = (AddonItemSearch*)Svc.GameGui.GetAddonByName("ItemSearch");
        if (mbAddon == null)
            return;

        ImGui.SameLine();

        if (ImGui.Button($"Search##{item.ItemId}"))
        {
            mbAddon->SearchTextInput->SetText(item.Name);
            mbAddon->RunSearch();
        }
    }

    void ExtractItemsFromMakePlaceList(string text, string startSection, string endSection)
    {
        // Find the start index of the section
        int startIndex = text.IndexOf(startSection);
        if (startIndex == -1)
        {
            Svc.Log.Error($"Section '{startSection}' not found.");
            return;
        }

        // Find the end index of the section
        int endIndex = text.IndexOf(endSection, startIndex);
        if (endIndex == -1)
        {
            endIndex = text.Length; // If end section not found, read till end
        }

        // Extract the section text
        string sectionText = text.Substring(startIndex, endIndex - startIndex);

        // Split the section into lines
        string[] lines = sectionText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Regular expression to match lines with format 'Item Name: Quantity'
        Regex regex = new Regex(@"^\s*(.+?):\s*(\d+)\s*$");

        // Iterate over each line and extract item name and quantity
        foreach (string line in lines)
        {
            Match match = regex.Match(line);
            if (match.Success)
            {
                string itemName = match.Groups[1].Value.Trim();
                if (string.Equals(startSection, "Dyes", StringComparison.OrdinalIgnoreCase))
                    itemName += " Dye";
                int quantity = int.Parse(match.Groups[2].Value.Trim());

                var item = SupermarketSweep.AllItems.FirstOrNull(i =>
                    string.Equals(i.Name.ToString(), itemName, StringComparison.OrdinalIgnoreCase));
                if (item == null && string.Equals(startSection, "Dyes", StringComparison.OrdinalIgnoreCase))
                {
                    item = SupermarketSweep.AllItems.FirstOrNull(i =>
                        string.Equals(i.Name.ToString(), $"General-purpose {itemName}",
                            StringComparison.OrdinalIgnoreCase));
                }

                if (item == null)
                {
                    Svc.Log.Warning($"Item '{itemName}' does not exist.");
                    continue;
                }

                var existingItem = _manager.WantedItems.FirstOrDefault(i => i.ItemId == item.Value.RowId);

                // Add or update the item in the dictionary
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    _manager.WantedItems.Add(new ShoppingListItem(item.Value, quantity));
                }
            }
        }
    }

    private static readonly Random RandomGenerator = new Random();

    private static string GetRandomInsult()
    {
        int index = RandomGenerator.Next(Insults.Count); // Random index from 0 to the length of the insult list
        return Insults[index];
    }

    private static readonly List<string> Insults = new List<string>
    {
        "Way to hammer the API like a clueless fuckstick. Hope you’re proud of yourself, dipshit.",
        "Congrats, you API-throttling asshole. Keep this shit up, and the server’s going to crash just for you.",
        "Nice job, you fucking data parasite. The server’s definitely enjoying your selfish bullshit.",
        "Look at you, a total bandwidth-sucking dickhead. I bet you feel real clever, huh?",
        "Bravo, douche-canoe. Because what the API really needed was another inconsiderate prick like you.",
        "Great going, API-slammer. Do us all a favor and learn some patience, you trigger-happy bastard.",
        "Wow, look at you hammering the server like a complete shit-for-brains. Slow the fuck down, maybe?",
        "Fucking excellent, now the API has another selfish prick to deal with. You must be so proud.",
        "Good job, you inconsiderate dicknugget. Maybe let the API breathe for a fucking second?",
        "Oh fantastic, a throttling fucknugget with zero impulse control. The API’s really going to love you.",
        "Way to go, you goddamn server-hammering toolbag. Keep clicking, maybe it'll just crash for you.",
        "Holy shit, do you even know what patience is, or are you just this much of an API-smashing douche?",
        "Good one, you dumbfuck. Slamming the API like that really shows how little you care about anyone else.",
        "Look at this fucking guy, treating the API like a punching bag. Get a grip, you reckless bastard.",
        "Nice going, throttle-happy fuckwit. It's like you're trying to kill the server on purpose.",
        "Well done, dickhead. Your API abuse is exactly what the server didn’t need right now.",
        "You really are an inconsiderate shithead, aren't you? The API’s going to fucking love you for this.",
        "Way to spam the API like a goddamn moron. Maybe give the server a break, shitheel?",
        "Fucking phenomenal, you're the reason rate limits exist, you API-hammering asshole.",
        "Jesus Christ, slow the fuck down, you refresh-spamming fuckstick. The API isn't your personal bitch.",
        "Impressive, you data-hungry douchebag. Do you ever stop to think, or do you just slam buttons like an idiot?",
        "Wow, really hammering that API, huh? What are you, some kind of bandwidth-sucking shit-for-brains?",
        "Goddamn it, give the API a fucking rest, you self-centered prick.",
        "Fucking perfect. Another clueless dipshit who doesn’t give a fuck about anyone else’s server performance.",
        "Congrats, you’re officially the API’s worst fucking nightmare. Well done, you inconsiderate fuck.",
        "You’ve got to be shitting me. Could you throttle the API any harder, you absolute bastard?",
        "Holy shit, maybe ease up on the server abuse, you API-slamming fucker.",
        "Good job, douche-nozzle. Your ability to hammer the API is only matched by your complete lack of awareness.",
        "Look at you, all trigger-happy and API-abusing. Are you really this much of a selfish bastard?",
        "Fantastic. Another brain-dead fuckwit pounding the API like it owes them something.",
        "You refresh-spamming assclown. Keep this up, and maybe the server will just explode for you."
    };
}