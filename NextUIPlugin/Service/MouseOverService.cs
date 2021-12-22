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
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace NextUIPlugin.Service {
#if DEBUG
	public delegate void OnSetMouseoverActorId(long arg1, long arg2);
#endif

	public unsafe delegate ulong OnRequestAction(
		long arg1, uint arg2, ulong arg3, long arg4, uint arg5, uint arg6, int arg7, void* arg8
	);

	public class MouseOverService : IDisposable {
#if DEBUG
		protected IntPtr setMouseOverActorId;
		protected Hook<OnSetMouseoverActorId> mouseOverActorIdHook;
#endif
		protected IntPtr requestAction;
		protected Hook<OnRequestAction> requestActionHook;

		protected ExcelSheet<Action>? sheet;
		public GameObject? target = null;

		public unsafe MouseOverService() {
#if DEBUG
			setMouseOverActorId = NextUIPlugin.sigScanner.ScanText(
				"48 89 91 ?? ?? ?? ?? C3 CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 55 56 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8D B1 ?? ?? ?? ?? 44 89 44 24 ?? 48 8B EA 48 8B D9 48 8B CE 48 8D 15 ?? ?? ?? ?? 41 B9 ?? ?? ?? ??"
			);
			mouseOverActorIdHook = new Hook<OnSetMouseoverActorId>(setMouseOverActorId, HandleMouseOverActorId);
			mouseOverActorIdHook.Enable();
#endif
			requestAction = NextUIPlugin.sigScanner.ScanText(
				"E8 ?? ?? ?? ?? 89 9F ?? ?? ?? ?? EB 0A C7 87 ?? ?? ?? ?? ?? ?? ?? ??"
			);
			requestActionHook = new Hook<OnRequestAction>(requestAction, HandleRequestAction);
			requestActionHook.Enable();

			sheet = NextUIPlugin.dataManager.GetExcelSheet<Action>();
		}

#if DEBUG
		protected void HandleMouseOverActorId(long arg1, long arg2) {
			PluginLog.Log("MO: {0} - {1}", arg1, arg2);
			mouseOverActorIdHook.Original(arg1, arg2);
		}
#endif

		protected unsafe ulong HandleRequestAction(
			long arg1, uint arg2, ulong arg3, long arg4, uint arg5, uint arg6, int arg7, void* arg8
		) {
#if DEBUG
			PluginLog.Log(
				"ACTION: {0} - {1} - {2} - {3} - {4} - {5} - {6}}",
				arg1, arg2, arg3, arg4, arg5, arg6, arg7
			);
#endif

			if (IsActionValid(arg3, target)) {
				return requestActionHook.Original(arg1, arg2, arg3, target!.ObjectId, arg5, arg6, arg7, arg8);
			}

			return requestActionHook.Original(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
		}

		protected bool IsActionValid(ulong actionId, GameObject? target) {
			if (target == null || actionId == 0 || sheet == null) {
				return false;
			}

			var action = sheet.GetRow((uint)actionId);
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

		public void Dispose() {
			requestActionHook?.Dispose();
#if DEBUG
			mouseOverActorIdHook?.Dispose();
#endif
		}
	}
}