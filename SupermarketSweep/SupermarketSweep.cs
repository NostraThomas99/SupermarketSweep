using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.Commands;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using SupermarketSweep.Models;
using SupermarketSweep.IPC;
using SupermarketSweep.UI;

namespace SupermarketSweep;

public class SupermarketSweep : IDalamudPlugin
{
    public static List<Item> AllItems;

    public static List<Item> MarketableItems;
    public static Config Config;

    public List<ShoppingListItem> WantedItems = [];

    public TaskManager TaskManager;

#if DEBUG
    static bool showDebug = true;
#else
    static bool showDebug = false;
#endif
    
    public WindowSystem WindowSystem;
    public MBShoppingList_UI MBShoppingListUI;
    public ConfigUi MBShoppingListConfigUI;

    public TaskManagerConfiguration LifeStreamTaskConfig;

    public TaskManagerConfiguration DefaultTaskConfig;

    public SupermarketSweep(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, Module.DalamudReflector);
        Config = EzConfig.Init<Config>();
        AllItems = Svc.Data.GameData.GetExcelSheet<Item>()!.ToList();
        MarketableItems = AllItems.Where(i => i.ItemSearchCategory.RowId != 0).ToList();

        LifeStreamTaskConfig = new TaskManagerConfiguration(
            timeLimitMS: Config.LifeStreamTimeout * 1000, showDebug: showDebug);
        DefaultTaskConfig =
            new TaskManagerConfiguration(timeLimitMS: 40000, showDebug: showDebug);

        TaskManager = new TaskManager(DefaultTaskConfig);
        LoadList();
        _OnItemAdded =
            Svc.PluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>(
                "AllaganTools.ItemAdded");
        _OnItemRemoved =
            Svc.PluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>(
                "AllaganTools.ItemRemoved");

        _OnItemAdded.Subscribe(OnItemAdded);

        WindowSystem = new WindowSystem();
        MBShoppingListUI = new MBShoppingList_UI(this);
        MBShoppingListConfigUI = new ConfigUi();
        WindowSystem.AddWindow(MBShoppingListUI);
        WindowSystem.AddWindow(MBShoppingListConfigUI);
        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
    }

    [Cmd("/shop", "Opens the shopping list UI.")]
    public void OnCommand(string command, string args)
    {
        if (!string.IsNullOrEmpty(args) && args.ToLower() == "expert")
        {
            Config.ExpertMode = true;
            EzConfig.Save();
            Svc.Chat.Print("[Supermarket Sweep] Expert mode enabled.");
            return;
        }

        OpenMainUi();
    }

    public void OpenConfigUi()
    {
        MBShoppingListConfigUI.Toggle();
    }

    public void OpenMainUi()
    {
        MBShoppingListUI.Toggle();
    }

    private static ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>? _OnItemAdded;
    private static ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>? _OnItemRemoved;

    private void OnItemAdded((uint, InventoryItem.ItemFlags, ulong, uint) itemDetails)
    {
        if (!SupermarketSweep.Config.RemoveQuantityAutomatically)
            return;
        var wantedItem = WantedItems.FirstOrDefault(i => i.ItemId == itemDetails.Item1);
        if (wantedItem is null)
            return;

        wantedItem.Quantity -= itemDetails.Item4;
        if (wantedItem.Quantity < 0)
            wantedItem.Quantity = 0;
    }


    public void SaveList()
    {
        var path = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "shoppinglist.json");
        var json = JsonConvert.SerializeObject(WantedItems, Formatting.Indented);
        File.WriteAllText(path, json);
        Svc.Log.Verbose("Shopping list saved.");
    }

    public void LoadList()
    {
        var path = Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, "shoppinglist.json");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var shoppingList = JsonConvert.DeserializeObject<List<ShoppingListItem>>(json);
            if (shoppingList != null)
            {
                WantedItems = shoppingList;
                Svc.Log.Verbose("Shopping list loaded.");
            }
        }
    }

    public void QueueMoveToMarketboardTasks()
    {
        if (!SupermarketSweep.Config.UseVnavPathing)
            return;
        if (!VNavmesh_IPCSubscriber.IsEnabled)
        {
            Svc.Chat.PrintError($"[Reborn Toolbox] VNavmesh is required for automatic movement");
            return;
        }

        switch (Svc.ClientState.TerritoryType)
        {
            case 129:
                MoveToNearestMarketboard();
                return;
            case 130:
                QueueUldahMoveToMarketboardTasks();
                return;
            case 132:
                QueueGridMoveToMarketboardTasks();
                return;
            default:
                Svc.Log.Error($"TerritoryType {Svc.ClientState.TerritoryType} is not supported.");
                return;
        }
    }

    private void QueueUldahMoveToMarketboardTasks()
    {
        TaskManager.Enqueue(() => Lifestream_IPCSubscriber.AethernetTeleport("sapphire"));
        TaskManager.Enqueue(() => Svc.ClientState.TerritoryType == 131);
        TaskManager.Enqueue(GenericHelpers.IsScreenReady);
        TaskManager.Enqueue(() => VNavmesh_IPCSubscriber.Nav_IsReady());
        TaskManager.Enqueue(MoveToNearestMarketboard);
    }

    private uint marketboardDataId = 2000402;
    private uint otherMoreStupidMarketboardDataId = 2000442;

    private Vector3 oldGridBowerHousePosition = new Vector3(141.558f, 13.571f, -97.028f);

    private void QueueGridMoveToMarketboardTasks()
    {
        TaskManager.Enqueue(() => Lifestream_IPCSubscriber.AethernetTeleport("bower"));
        TaskManager.Enqueue(() => Svc.ClientState.TerritoryType == 133);
        TaskManager.Enqueue(GenericHelpers.IsScreenReady);
        TaskManager.Enqueue(() => VNavmesh_IPCSubscriber.Nav_IsReady());
        TaskManager.Enqueue(() =>
            VNavmesh_IPCSubscriber.SimpleMove_PathfindAndMoveTo(oldGridBowerHousePosition, false));
        TaskManager.Enqueue(() =>
            !VNavmesh_IPCSubscriber.Path_IsRunning() && !VNavmesh_IPCSubscriber.Nav_PathfindInProgress());
        TaskManager.Enqueue(() => VNavmesh_IPCSubscriber.Path_Stop());
        TaskManager.Enqueue(MoveToNearestMarketboard);
    }

    private void MoveToNearestMarketboard()
    {
        var marketBoard = Svc.Objects
            .Where(o => o.DataId == marketboardDataId || o.DataId == otherMoreStupidMarketboardDataId)
            .OrderBy(o => Vector3.Distance(o.Position, Player.Position)).FirstOrDefault();
        if (marketBoard == null)
        {
            Svc.Log.Error($"Unable to find marketboard for {Svc.ClientState.TerritoryType}.");
            return;
        }

        TaskManager.Enqueue(() => VNavmesh_IPCSubscriber.Nav_IsReady());
        TaskManager.Enqueue(() => VNavmesh_IPCSubscriber.SimpleMove_PathfindAndMoveTo(marketBoard.Position, false));
        TaskManager.Enqueue(() => Vector3.Distance(marketBoard.Position, Player.Position) < 2.9);
        TaskManager.Enqueue(() => VNavmesh_IPCSubscriber.Path_Stop());
        TaskManager.Enqueue(() => InteractWithObject(marketBoard));
    }

    private unsafe void InteractWithObject(IGameObject obj)
    {
        var targetSystem = TargetSystem.Instance();
        if (targetSystem == null)
        {
            Svc.Log.Error($"TargetSystem was null.");
        }

        targetSystem->OpenObjectInteraction(
            (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Address);
    }

    public void Dispose()
    {
        TaskManager.Dispose();
        ECommonsMain.Dispose();
    }
}