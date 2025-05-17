using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using OtterGui;
using OtterGui.Table;
using SupermarketSweep.IPC;
using SupermarketSweep.Models;

namespace SupermarketSweep.UI;

public class ResultsTable : Table<MarketDataListing>
{
    private static readonly ServerColumn _serverColumn = new() { Label = "Server" };
    private static readonly QuantityColumn _quantityColumn = new() { Label = "Quantity" };
    private static readonly HqColumn _hqColumn = new() { Label = "HQ" };
    private static readonly PriceColumn _priceColumn = new() { Label = "Total Price" };
    private static readonly PricePerItemColumn _pricePerItemColumn = new() { Label = "Price Per Item" };

    private static SupermarketSweep _manager;

    public ResultsTable(SupermarketSweep manager, List<MarketDataListing> data) : base("##Results", data, _serverColumn, _hqColumn, _quantityColumn, _priceColumn, _pricePerItemColumn)
    {
        _manager = manager;
        Flags |= ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Resizable;
    }

    public class PricePerItemColumn : ColumnString<MarketDataListing>
    {
        public override float Width => ImGui.CalcTextSize(Label).X + 10;

        public override string ToName(MarketDataListing item)
        {
            var price = item.Total / item.Quantity;
            return price.ToString();
        }

        public override void DrawColumn(MarketDataListing item, int _)
        {
            var price = item.Total / item.Quantity;
            ImGui.Text(price.ToString());
        }
    }
    public class PriceColumn : ColumnString<MarketDataListing>
    {
        public override float Width => ImGui.CalcTextSize(Label).X + 10;

        public override string ToName(MarketDataListing item)
        {
            return item.Total.ToString();
        }
        public override void DrawColumn(MarketDataListing item, int _)
        {
            ImGui.Text(item.Total.ToString());
        }
    }

    public class HqColumn : YesNoColumn<MarketDataListing>
    {
        public override float Width => ImGui.CalcTextSize(Label).X + 10;
        protected override bool GetValue(MarketDataListing item)
        {
            return item.Hq;
        }
    }

    public class QuantityColumn : ColumnString<MarketDataListing>
    {
        public override float Width => ImGui.CalcTextSize(Label).X + 10;
        public override string ToName(MarketDataListing item)
        {
            return item.Quantity.ToString();
        }

        public override void DrawColumn(MarketDataListing item, int _)
        {
            ImGui.Text(item.Quantity.ToString());
        }
    }

    public class ServerColumn : ColumnString<MarketDataListing>
    {
        public override float Width => ImGui.CalcTextSize(Label).X + 250;
        public override string ToName(MarketDataListing item)
        {
            return item.WorldName;
        }

        public override void DrawColumn(MarketDataListing item, int _)
        {
            var selected = ImGui.Selectable(item.WorldName);
            var hovered  = ImGui.IsItemHovered();

            if (selected)
            {
                if (!Lifestream_IPCSubscriber.IsEnabled)
                {
                    Svc.Chat.PrintError($"[Reborn Toolbox] LifeStream is required to move between servers");
                    return;
                }

                _manager.TaskManager.Enqueue(() => Lifestream_IPCSubscriber.ExecuteCommand(item.WorldName),
                    _manager.LifeStreamTaskConfig);
                _manager.TaskManager.Enqueue(() => !Lifestream_IPCSubscriber.IsBusy(), _manager.LifeStreamTaskConfig);
                _manager.TaskManager.Enqueue(GenericHelpers.IsScreenReady);
                _manager.TaskManager.Enqueue(_manager.QueueMoveToMarketboardTasks);
            }
            if (hovered)
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Move to {item.WorldName} using Lifestream");
                ImGui.EndTooltip();
            }
        }
    }

}