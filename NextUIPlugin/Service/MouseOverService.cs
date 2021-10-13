/*
Copyright(c) 2021 attickdoor (https://github.com/attickdoor/MOActionPlugin)
Modifications Copyright(c) 2021 NextUI
28-09-2021 - Used original's code hooks and action validations while using 
NextUI's own logic to select a target. Borrowed from DelvUI.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Dalamud.Hooking;
using Lumina.Excel;
using System;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace NextUIPlugin.Service {
#if DEBUG
	public delegate void OnSetMouseoverActorId(long arg1, long arg2);
#endif

	public delegate ulong OnRequestAction(long arg1, uint arg2, ulong arg3, long arg4, uint arg5, uint arg6, int arg7);

	public class MouseOverService {
		protected SigScanner sigScanner;
		protected DataManager dataManager;
#if DEBUG
		protected IntPtr setMouseOverActorId;
		protected Hook<OnSetMouseoverActorId> mouseOverActorIdHook;
#endif
		protected IntPtr requestAction;
		protected Hook<OnRequestAction> requsetActionHook;

		protected ExcelSheet<Action>? sheet;
		public GameObject? Target = null;

		public MouseOverService(SigScanner sigScanner, DataManager dataManager) {
			this.sigScanner = sigScanner;
			this.dataManager = dataManager;

#if DEBUG
			setMouseOverActorId = sigScanner.ScanText(
				"48 89 91 ?? ?? ?? ?? C3 CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 55 56 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8D B1 ?? ?? ?? ?? 44 89 44 24 ?? 48 8B EA 48 8B D9 48 8B CE 48 8D 15 ?? ?? ?? ?? 41 B9 ?? ?? ?? ??"
			);
			mouseOverActorIdHook = new Hook<OnSetMouseoverActorId>(
				setMouseOverActorId,
				new OnSetMouseoverActorId(HandleMouseOverActorId)
			);
			mouseOverActorIdHook.Enable();
#endif
			requestAction = sigScanner.ScanText(
				"40 53 55 57 41 54 41 57 48 83 EC 60 83 BC 24 ?? ?? ?? ?? ?? 49 8B E9 45 8B E0 44 8B FA 48 8B F9 41 8B D8 74 14 80 79 68 00 74 0E 32 C0 48 83 C4 60 41 5F 41 5C 5F 5D 5B C3"
			);
			requsetActionHook = new Hook<OnRequestAction>(requestAction, new OnRequestAction(HandleRequestAction));
			requsetActionHook.Enable();

			sheet = dataManager.GetExcelSheet<Action>();
		}
#if DEBUG
		protected void HandleMouseOverActorId(long arg1, long arg2) {
			PluginLog.Log("MO: {0} - {1}", arg1, arg2);
			mouseOverActorIdHook.Original(arg1, arg2);
		}
#endif
		protected ulong HandleRequestAction(
			long arg1, uint arg2, ulong arg3, long arg4, uint arg5, uint arg6, int arg7
		) {
			// PluginLog.Log("ACTION: {0} - {1} - {2} - {3} - {4} - {5} - {6}}", arg1, arg2, arg3, arg4, arg5, arg6, arg7);

			if (IsActionValid(arg3, Target)) {
				return requsetActionHook.Original(arg1, arg2, arg3, Target!.ObjectId, arg5, arg6, arg7);
			}

			return requsetActionHook.Original(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
		}

		protected bool IsActionValid(ulong actionId, GameObject? target) {
			if (target == null || actionId == 0 || sheet == null) {
				return false;
			}

			Action? action = sheet.GetRow((uint)actionId);
			if (action == null) {
				return false;
			}

			// friendly player (TODO: pvp? lol)
			if (target is PlayerCharacter) {
				return action.CanTargetFriendly || action.CanTargetParty || action.CanTargetSelf;
			}

			// friendly npc
			if (target is BattleNpc npc) {
				if (npc.BattleNpcKind != BattleNpcSubKind.Enemy) {
					return action.CanTargetFriendly || action.CanTargetParty || action.CanTargetSelf;
				}
			}

			return action.CanTargetHostile;
		}
	}
}