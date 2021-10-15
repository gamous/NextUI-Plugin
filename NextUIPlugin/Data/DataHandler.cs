using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using NextUIPlugin.NetworkStructures;
using NextUIPlugin.Service;
using SpellAction = Lumina.Excel.GeneratedSheets.Action;
using BattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;

namespace NextUIPlugin.Data {
	public class DataHandler : IDisposable {
		public Action<string>? onPlayerNameChanged;
		public Action<string, uint?, string?>? onTargetChanged;
		public Action<uint, string>? onHoverChanged;
		public Action<List<uint>>? onPartyChanged;
		public Action<string, uint, string, float, float, uint>? CastStart;

		protected readonly Dictionary<string, uint?> targets = new();
		protected readonly Dictionary<string, bool> casts = new();
		protected List<uint> party = new List<uint>();

		public DataHandler() {
			NextUIPlugin.framework.Update += FrameworkOnUpdate;
			// NextUIPlugin.gameNetwork.NetworkMessage += GameNetworkOnNetworkMessage;
		}

		protected unsafe void GameNetworkOnNetworkMessage(
			IntPtr dataPtr,
			ushort opcode,
			uint sourceActorId,
			uint targetActorId,
			NetworkMessageDirection direction
		) {
			// if (direction == NetworkMessageDirection.ZoneUp) {
			// 	return;
			// }

			string dir = direction == NetworkMessageDirection.ZoneDown ? "Down" : "Up";
			PluginLog.Log("NETWORK 0x" + opcode.ToString("X4") + " " + dir);
			if (true) {
				using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
				using var reader = new BinaryReader(stream);
				var raw = reader.ReadBytes(1540);
				reader.Close();
				stream.Close();
				PluginLog.Log(Convert.ToHexString(raw));
			}
			
			// using UnmanagedMemoryStream stream = new((byte*)dataPtr.ToPointer(), 1544);
			// using BinaryReader reader = new(stream);
			if (opcode == 0x03B0) { // (ushort)ServerZoneIpcType.Chat
				XivIpcChat chat = Marshal.PtrToStructure<XivIpcChat>(dataPtr);
				// var sn = Marshal.PtrToStringUTF8(new IntPtr(chat.name));
				// var sv = Marshal.PtrToStringUTF8(new IntPtr(chat.msg));
				var sn = SeString.Parse(chat.msg, 32);
				var sv = SeString.Parse(chat.msg, 1012);
				PluginLog.Log("MSG FROM: " + sn.TextValue);
				PluginLog.Log("MSG: " + sv.TextValue);
			}
			
		}

		protected unsafe void FrameworkOnUpdate(Framework framework) {
			WatchCasts();

			Dictionary<string, GameObject?> currentTargets = new() {
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "hover", NextUIPlugin.targetManager.MouseOverTarget },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, GameObject? value) in currentTargets) {
				if (!targets.ContainsKey(key)) {
					targets[key] = null;
				}

				if (value != null) {
					if (targets[key] != value.ObjectId) {
						targets[key] = value.ObjectId;
						onTargetChanged?.Invoke(key, value.ObjectId, value.Name.TextValue);
					}
				}
				else {
					if (targets[key] != null) {
						targets[key] = null;
						onTargetChanged?.Invoke(key, null, null);
					}
				}
			}

			// List<int> currentParty = NextUIPlugin.clientState.
			// 	.Select(partyMember => partyMember.Actor.ActorId).ToList();
			//
			// if (party.Count != currentParty.Count) {
			// 	onPartyChanged?.Invoke(currentParty);
			// }
			// else {
			// 	List<int> firstNotSecond = party.Except(currentParty).ToList();
			// 	List<int> secondNotFirst = currentParty.Except(party).ToList();
			//
			// 	bool eq = !firstNotSecond.Any() && !secondNotFirst.Any();
			// 	if (!eq) {
			// 		onPartyChanged?.Invoke(currentParty);
			// 		party = currentParty;
			// 	}
			// }
		}

		protected unsafe void WatchCasts() {
			Dictionary<string, GameObject?> actorsCasts = new() {
				{ "player", NextUIPlugin.clientState.LocalPlayer },
				{ "target", NextUIPlugin.targetManager.Target },
				{ "targetOfTarget", NextUIPlugin.targetManager.Target?.TargetObject },
				{ "focus", NextUIPlugin.targetManager.FocusTarget },
			};

			foreach ((string key, GameObject? actor) in actorsCasts) {
				if (actor == null) {
					continue;
				}

				if (!casts.ContainsKey(key)) {
					casts[key] = false;
				}

				BattleChara* battleChara = (BattleChara*)actor.Address;
				BattleChara.CastInfo castInfo = battleChara->SpellCastInfo;

				bool isCasting = castInfo.IsCasting > 0;
				bool targetIsCasting = casts[key];

				if (isCasting != targetIsCasting && isCasting) {
					string castName = ActionService.GetActionNameFromCastInfo(actor.TargetObject, castInfo);
					CastStart?.Invoke(
						key,
						castInfo.ActionID,
						castName,
						castInfo.CurrentCastTime,
						castInfo.TotalCastTime,
						castInfo.CastTargetID
					);
				}

				casts[key] = isCasting;
			}
		}

		public void Dispose() {
			NextUIPlugin.framework.Update -= FrameworkOnUpdate;
			// NextUIPlugin.gameNetwork.NetworkMessage -= GameNetworkOnNetworkMessage;
		}
	}
}