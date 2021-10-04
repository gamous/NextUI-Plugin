using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using BattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;

namespace NextUIPlugin.Service {
	public class ActionService {
		public static string GetActionNameFromCastInfo(BattleChara.CastInfo castInfo) {
			switch (castInfo.ActionType) {
				case ActionType.PetAction:
				case ActionType.Spell:
				case ActionType.SquadronAction:
				case ActionType.PvPAction:
				case ActionType.CraftAction:
				case ActionType.Ability:
					Action? action = NextUIPlugin.dataManager.GetExcelSheet<Action>()?.GetRow(castInfo.ActionID);
					return action?.Name.ToString() ?? "";
				case ActionType.Mount:
					Mount? mount = NextUIPlugin.dataManager.GetExcelSheet<Mount>()?.GetRow(castInfo.ActionID);
					return mount?.Singular.ToString() ?? "";
				case ActionType.KeyItem:
				case ActionType.Item:
					Item? item = NextUIPlugin.dataManager.GetExcelSheet<Item>()?.GetRow(castInfo.ActionID);
					return item?.Name.ToString() ?? "Using item...";

				case ActionType.Companion:
					Companion? companion = NextUIPlugin.dataManager.GetExcelSheet<Companion>()?.GetRow(castInfo.ActionID);
					return companion?.Singular.ToString() ?? "";
				case ActionType.None:
				case ActionType.General:
				case ActionType.Unk_7:
				case ActionType.Unk_8:
				case ActionType.MainCommand:
				case ActionType.Waymark:
				case ActionType.ChocoboRaceAbility:
				case ActionType.ChocoboRaceItem:
				case ActionType.Unk_12:
				case ActionType.Unk_18:
				case ActionType.Accessory:
				default:
					return "Casting...";
			}
		}
	}
}