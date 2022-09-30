using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using NextUIPlugin.NetworkStructures;
using NextUIPlugin.NetworkStructures.Client;
using NextUIPlugin.NetworkStructures.Server;

namespace NextUIPlugin.Data {
	public static class NetworkBinding {
		public static readonly Dictionary<ushort, string> binding = new() {
			{ (ushort)ServerZoneIpcType.ActorCast, "actorCast" },
			{ (ushort)ServerZoneIpcType.ActorMove, "actorMove" },
			{ (ushort)ServerZoneIpcType.ActorGauge, "actorGauge" },
			{ (ushort)ServerZoneIpcType.ActorControl, "actorControl" },
			{ (ushort)ServerZoneIpcType.ActorControlSelf, "actorControlSelf" },
			{ (ushort)ServerZoneIpcType.ActorControlTarget, "actorControlTarget" },
			{ (ushort)ServerZoneIpcType.ActorSetPos, "actorSetPos" },
			//{ (ushort)ServerZoneIpcType.ActionEffect1, "actionEffect1" },
			//{ (ushort)ServerZoneIpcType.ActionEffect8, "actionEffect8" },
			//{ (ushort)ServerZoneIpcType.ActionEffect16, "actionEffect16" },
			//{ (ushort)ServerZoneIpcType.ActionEffect24, "actionEffect24" },
			//{ (ushort)ServerZoneIpcType.ActionEffect32, "actionEffect32" },
			{ (ushort)ServerZoneIpcType.EffectResult, "effectResult" },
			//{ (ushort)ServerZoneIpcType.EffectResultBasic, "effectResultBasic" },
			{ (ushort)ServerZoneIpcType.StatusEffectList, "statusEffectList" },
			{ (ushort)ServerZoneIpcType.StatusEffectList2, "statusEffectList2" },
			{ (ushort)ServerZoneIpcType.StatusEffectList3, "statusEffectList3" },
			{ (ushort)ServerZoneIpcType.UpdateHpMpTp, "updateHpMpTp" },
			{ (ushort)ServerZoneIpcType.NpcSpawn, "npcSpawn" },
			{ (ushort)ServerZoneIpcType.PlayerSpawn, "playerSpawn" },
			//{ (ushort)ServerZoneIpcType.ObjectDespawn, "objectDespawn" },
			// Client zone events
			{ (ushort)ClientZoneIpcType.UpdatePositionHandler, "updatePosition" },
			{ (ushort)ClientZoneIpcType.UpdatePositionInstance, "updatePositionInstance" },
		};

		public static object? Convert(ushort opcode, IntPtr dataPtr) {
			object strObj;
			switch (opcode) {
				case (ushort)ServerZoneIpcType.ActorCast:
					strObj = Marshal.PtrToStructure<XivIpcActorCast>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActorMove:
					strObj = Marshal.PtrToStructure<XivIpcActorMove>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActorGauge:
					strObj = Marshal.PtrToStructure<XivIpcActorGauge>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActorControl:
					strObj = Marshal.PtrToStructure<XivIpcActorControl>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActorControlSelf:
					strObj = Marshal.PtrToStructure<XivIpcActorControlSelf>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActorControlTarget:
					strObj = Marshal.PtrToStructure<XivIpcActorControlTarget>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActorSetPos:
					strObj = Marshal.PtrToStructure<XivIpcActorSetPos>(dataPtr);
					break;
				//case (ushort)ServerZoneIpcType.ActionEffect1:
				//	strObj = Marshal.PtrToStructure<XivIpcActionEffect1>(dataPtr);
				//	break;
				//case (ushort)ServerZoneIpcType.ActionEffect8:
				//	strObj = Marshal.PtrToStructure<XivIpcActionEffect8>(dataPtr);
				//	break;
				//case (ushort)ServerZoneIpcType.ActionEffect16:
				//	strObj = Marshal.PtrToStructure<XivIpcActionEffect16>(dataPtr);
				//	break;
				//case (ushort)ServerZoneIpcType.ActionEffect24:
				//	strObj = Marshal.PtrToStructure<XivIpcActionEffect24>(dataPtr);
				//	break;
				//case (ushort)ServerZoneIpcType.ActionEffect32:
				//	strObj = Marshal.PtrToStructure<XivIpcActionEffect32>(dataPtr);
				//	break;
				case (ushort)ServerZoneIpcType.EffectResult:
					strObj = Marshal.PtrToStructure<XivIpcEffectResult>(dataPtr);
					break;
				//case (ushort)ServerZoneIpcType.EffectResultBasic:
				//	strObj = Marshal.PtrToStructure<XivIpcEffectResultBasic>(dataPtr);
				//	break;
				case (ushort)ServerZoneIpcType.StatusEffectList:
					strObj = Marshal.PtrToStructure<XivIpcStatusEffectList>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.StatusEffectList2:
					strObj = Marshal.PtrToStructure<XivIpcStatusEffectList2>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.StatusEffectList3:
					strObj = Marshal.PtrToStructure<XivIpcStatusEffectList3>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.UpdateHpMpTp:
					strObj = Marshal.PtrToStructure<XivIpcUpdateHpMpTp>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.NpcSpawn:
					strObj = Marshal.PtrToStructure<XivIpcNpcSpawn>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.PlayerSpawn:
					strObj = Marshal.PtrToStructure<XivIpcPlayerSpawn>(dataPtr);
					break;
				//case (ushort)ServerZoneIpcType.ObjectDespawn:
				//	strObj = Marshal.PtrToStructure<XivIpcObjectDespawn>(dataPtr);
				//	break;
				// Client side
				case (ushort)ClientZoneIpcType.UpdatePositionHandler:
					strObj = Marshal.PtrToStructure<XivIpcUpdatePosition>(dataPtr);
					break;
				case (ushort)ClientZoneIpcType.UpdatePositionInstance:
					strObj = Marshal.PtrToStructure<XivIpcUpdatePositionInstance>(dataPtr);
					break;
				default:
					return null;
			}

			return strObj;
		}
	}
}