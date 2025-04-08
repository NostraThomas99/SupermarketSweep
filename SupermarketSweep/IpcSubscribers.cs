using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SupermarketSweep.IPC
{
    internal class IPCSubscriber_Common
    {
        internal static bool IsReady(string pluginName)
            => DalamudReflector.TryGetDalamudPlugin(pluginName, out _, false, true);

        internal static void DisposeAll(EzIPCDisposalToken[] _disposalTokens)
        {
            foreach (var token in _disposalTokens)
            {
                try
                {
                    token.Dispose();
                }
                catch (Exception ex)
                {
                    Svc.Log.Error($"Error while unregistering IPC: {ex}");
                }
            }
        }
    }

    internal static class VNavmesh_IPCSubscriber
    {
        private static EzIPCDisposalToken[] _disposalTokens = EzIPC.Init(typeof(VNavmesh_IPCSubscriber), "vnavmesh");

        internal static bool IsEnabled
            => IPCSubscriber_Common.IsReady("vnavmesh");

        [EzIPC("vnavmesh.Nav.IsReady", applyPrefix: false)]
        internal static readonly Func<bool> Nav_IsReady;

        [EzIPC("vnavmesh.Nav.BuildProgress", applyPrefix: false)]
        internal static readonly Func<float> Nav_BuildProgress;

        [EzIPC("vnavmesh.Nav.Reload", applyPrefix: false)]
        internal static readonly Func<bool> Nav_Reload;

        [EzIPC("vnavmesh.Nav.Rebuild", applyPrefix: false)]
        internal static readonly Func<bool> Nav_Rebuild;

        [EzIPC("vnavmesh.Nav.Pathfind", applyPrefix: false)]
        internal static readonly Func<Vector3, Vector3, bool, Task<List<Vector3>>> Nav_Pathfind;

        [EzIPC("vnavmesh.Nav.PathfindCancelable", applyPrefix: false)]
        internal static readonly Func<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>> Nav_PathfindCancelable;

        [EzIPC("vnavmesh.Nav.PathfindCancelAll", applyPrefix: false)]
        internal static readonly Action Nav_PathfindCancelAll;

        [EzIPC("vnavmesh.Nav.PathfindInProgress", applyPrefix: false)]
        internal static readonly Func<bool> Nav_PathfindInProgress;

        [EzIPC("vnavmesh.Nav.PathfindNumQueued", applyPrefix: false)]
        internal static readonly Func<int> Nav_PathfindNumQueued;

        [EzIPC("vnavmesh.Nav.IsAutoLoad", applyPrefix: false)]
        internal static readonly Func<bool> Nav_IsAutoLoad;

        [EzIPC("vnavmesh.Nav.SetAutoLoad", applyPrefix: false)]
        internal static readonly Action<bool> Nav_SetAutoLoad;

        [EzIPC("vnavmesh.Query.Mesh.NearestPoint", applyPrefix: false)]
        internal static readonly Func<Vector3, float, float, Vector3> Query_Mesh_NearestPoint;

        [EzIPC("vnavmesh.Query.Mesh.PointOnFloor", applyPrefix: false)]
        internal static readonly Func<Vector3, bool, float, Vector3> Query_Mesh_PointOnFloor;

        [EzIPC("vnavmesh.Path.MoveTo", applyPrefix: false)]
        internal static readonly Action<List<Vector3>, bool> Path_MoveTo;

        [EzIPC("vnavmesh.Path.Stop", applyPrefix: false)]
        internal static readonly Action Path_Stop;

        [EzIPC("vnavmesh.Path.IsRunning", applyPrefix: false)]
        internal static readonly Func<bool> Path_IsRunning;

        [EzIPC("vnavmesh.Path.NumWaypoints", applyPrefix: false)]
        internal static readonly Func<int> Path_NumWaypoints;

        [EzIPC("vnavmesh.Path.GetMovementAllowed", applyPrefix: false)]
        internal static readonly Func<bool> Path_GetMovementAllowed;

        [EzIPC("vnavmesh.Path.SetMovementAllowed", applyPrefix: false)]
        internal static readonly Action<bool> Path_SetMovementAllowed;

        [EzIPC("vnavmesh.Path.GetAlignCamera", applyPrefix: false)]
        internal static readonly Func<bool> Path_GetAlignCamera;

        [EzIPC("vnavmesh.Path.SetAlignCamera", applyPrefix: false)]
        internal static readonly Action<bool> Path_SetAlignCamera;

        [EzIPC("vnavmesh.Path.GetTolerance", applyPrefix: false)]
        internal static readonly Func<float> Path_GetTolerance;

        [EzIPC("vnavmesh.Path.SetTolerance", applyPrefix: false)]
        internal static readonly Action<float> Path_SetTolerance;

        [EzIPC("vnavmesh.SimpleMove.PathfindAndMoveTo", applyPrefix: false)]
        internal static readonly Func<Vector3, bool, bool> SimpleMove_PathfindAndMoveTo;

        [EzIPC("vnavmesh.SimpleMove.PathfindInProgress", applyPrefix: false)]
        internal static readonly Func<bool> SimpleMove_PathfindInProgress;

        [EzIPC("vnavmesh.Window.IsOpen", applyPrefix: false)]
        internal static readonly Func<bool> Window_IsOpen;

        [EzIPC("vnavmesh.Window.SetOpen", applyPrefix: false)]
        internal static readonly Action<bool> Window_SetOpen;

        [EzIPC("vnavmesh.DTR.IsShown", applyPrefix: false)]
        internal static readonly Func<bool> DTR_IsShown;

        [EzIPC("vnavmesh.DTR.SetShown", applyPrefix: false)]
        internal static readonly Action<bool> DTR_SetShown;

        internal static void Dispose()
            => IPCSubscriber_Common.DisposeAll(_disposalTokens);
    }

    internal static class Lifestream_IPCSubscriber
    {
        private static EzIPCDisposalToken[] _disposalTokens = EzIPC.Init(typeof(Lifestream_IPCSubscriber), "Lifestream");

        internal static bool IsEnabled
            => IPCSubscriber_Common.IsReady("Lifestream");

        [EzIPC("Lifestream.ExecuteCommand", applyPrefix: false)]
        internal static readonly Action<string> ExecuteCommand;
        
        [EzIPC("Lifestream.IsBusy", applyPrefix: false)]
        internal static readonly Func<bool> IsBusy;

        [EzIPC("Lifestream.Abort", applyPrefix: false)]
        internal static readonly Action Abort;

        [EzIPC("Lifestream.AethernetTeleport", applyPrefix: false)]
        internal static readonly Func<string, bool> AethernetTeleport;
        internal static void Dispose()
            => IPCSubscriber_Common.DisposeAll(_disposalTokens);
    }

    internal static class AllaganTools_IPCSubscriber
{
    private static EzIPCDisposalToken[] _disposalTokens = EzIPC.Init(typeof(AllaganTools_IPCSubscriber), "AllaganTools");

    internal static bool IsEnabled
        => IPCSubscriber_Common.IsReady("AllaganTools");

    [EzIPC("AllaganTools.InventoryCountByType", applyPrefix: false)]
    internal static readonly Func<uint, ulong?, uint> InventoryCountByType;

    [EzIPC("AllaganTools.InventoryCountByTypes", applyPrefix: false)]
    internal static readonly Func<uint[], ulong?, uint> InventoryCountByTypes;

    [EzIPC("AllaganTools.ItemCount", applyPrefix: false)]
    internal static readonly Func<uint, ulong, int, uint> ItemCount;

    [EzIPC("AllaganTools.ItemCountHQ", applyPrefix: false)]
    internal static readonly Func<uint, ulong, int, uint> ItemCountHQ;

    [EzIPC("AllaganTools.ItemCountOwned", applyPrefix: false)]
    internal static readonly Func<uint, bool, uint[], uint> ItemCountOwned;

    [EzIPC("AllaganTools.EnableUiFilter", applyPrefix: false)]
    internal static readonly Func<string, bool> EnableUiFilter;

    [EzIPC("AllaganTools.DisableUiFilter", applyPrefix: false)]
    internal static readonly Func<bool> DisableUiFilter;

    [EzIPC("AllaganTools.ToggleUiFilter", applyPrefix: false)]
    internal static readonly Func<string, bool> ToggleUiFilter;

    [EzIPC("AllaganTools.EnableBackgroundFilter", applyPrefix: false)]
    internal static readonly Func<string, bool> EnableBackgroundFilter;

    [EzIPC("AllaganTools.DisableBackgroundFilter", applyPrefix: false)]
    internal static readonly Func<bool> DisableBackgroundFilter;

    [EzIPC("AllaganTools.ToggleBackgroundFilter", applyPrefix: false)]
    internal static readonly Func<string, bool> ToggleBackgroundFilter;

    [EzIPC("AllaganTools.EnableCraftList", applyPrefix: false)]
    internal static readonly Func<string, bool> EnableCraftList;

    [EzIPC("AllaganTools.DisableCraftList", applyPrefix: false)]
    internal static readonly Func<bool> DisableCraftList;

    [EzIPC("AllaganTools.ToggleCraftList", applyPrefix: false)]
    internal static readonly Func<string, bool> ToggleCraftList;

    [EzIPC("AllaganTools.AddItemToCraftList", applyPrefix: false)]
    internal static readonly Func<string, uint, uint, bool> AddItemToCraftList;

    [EzIPC("AllaganTools.RemoveItemFromCraftList", applyPrefix: false)]
    internal static readonly Func<string, uint, uint, bool> RemoveItemFromCraftList;

    [EzIPC("AllaganTools.GetFilterItems", applyPrefix: false)]
    internal static readonly Func<string, Dictionary<uint, uint>> GetFilterItems;

    [EzIPC("AllaganTools.GetCraftItems", applyPrefix: false)]
    internal static readonly Func<string, Dictionary<uint, uint>> GetCraftItems;

    [EzIPC("AllaganTools.GetRetrievalItems", applyPrefix: false)]
    internal static readonly Func<Dictionary<uint, uint>> GetRetrievalItems;

    [EzIPC("AllaganTools.GetCharacterItems", applyPrefix: false)]
    internal static readonly Func<ulong, HashSet<ulong[]>> GetCharacterItems;

    [EzIPC("AllaganTools.GetCharactersOwnedByActive", applyPrefix: false)]
    internal static readonly Func<bool, HashSet<ulong>> GetCharactersOwnedByActive;

    [EzIPC("AllaganTools.GetCharacterItemsByType", applyPrefix: false)]
    internal static readonly Func<ulong, uint, HashSet<ulong[]>> GetCharacterItemsByType;

    [EzIPC("AllaganTools.GetCraftLists", applyPrefix: false)]
    internal static readonly Func<Dictionary<string, string>> GetCraftLists;

    [EzIPC("AllaganTools.GetSearchFilters", applyPrefix: false)]
    internal static readonly Func<Dictionary<string, string>> GetSearchFilters;

    [EzIPC("AllaganTools.AddNewCraftList", applyPrefix: false)]
    internal static readonly Func<string, Dictionary<uint, uint>, string> AddNewCraftList;

    [EzIPC("AllaganTools.CurrentCharacter", applyPrefix: false)]
    internal static readonly Func<ulong> CurrentCharacter;

    [EzIPC("AllaganTools.RetainerChanged", applyPrefix: false)]
    internal static readonly Func<ulong?, bool> RetainerChanged;

    [EzIPC("AllaganTools.IsInitialized", applyPrefix: false)]
    internal static readonly Func<bool> IsInitialized;

    [EzIPC("AllaganTools.Initialized", applyPrefix: false)]
    internal static readonly Func<bool, bool> Initialized;

    internal static void Dispose()
        => IPCSubscriber_Common.DisposeAll(_disposalTokens);
}

}
