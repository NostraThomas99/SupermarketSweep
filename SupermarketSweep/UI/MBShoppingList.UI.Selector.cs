using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui;
using SupermarketSweep.Models;

namespace SupermarketSweep.UI;

public class MbShoppingListUiSelector : ItemSelector<ShoppingListItem>
{
    private readonly SupermarketSweep _manager;
    public MbShoppingListUiSelector(SupermarketSweep manager) : base(manager.WantedItems, Flags.Delete)
    {
        _manager = manager;
    }

    protected override bool OnDraw(int idx)
    {
        using var id = ImRaii.PushId(idx);
        var item = Items[idx];

        var textColor = item.InventoryCount >= item.Quantity ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudWhite;

        ImGui.PushStyleColor(ImGuiCol.Text, textColor);

        bool selected = CurrentIdx == idx;
        bool result = ImGui.Selectable(item.Name, selected);

        // Pop the style colors after the Selectable
        ImGui.PopStyleColor(); // Pop in reverse order: HeaderActive, HeaderHovered, Header, Text

        return result;
    }

    protected override bool Filtered(int idx) => Filter.Length != 0 &&
                                                 !Items[idx].Name.Contains(Filter,
                                                     StringComparison.InvariantCultureIgnoreCase);

    protected override bool OnDelete(int idx)
    {
        Items.RemoveAt(idx);
        _manager.SaveList();
        return true;
    }

    protected override bool OnClipboardImport(string name, string data)
    {
        return base.OnClipboardImport(name, data);
    }
}