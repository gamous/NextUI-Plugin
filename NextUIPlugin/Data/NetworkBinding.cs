using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using NextUIPlugin.NetworkStructures;
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
			{ (ushort)ServerZoneIpcType.ActionEffect1, "actionEffect1" },
			{ (ushort)ServerZoneIpcType.ActionEffect8, "actionEffect8" },
			{ (ushort)ServerZoneIpcType.ActionEffect16, "actionEffect16" },
			{ (ushort)ServerZoneIpcType.ActionEffect24, "actionEffect24" },
			{ (ushort)ServerZoneIpcType.ActionEffect32, "actionEffect32" },
			{ (ushort)ServerZoneIpcType.EffectResult, "effectResult" },
			{ (ushort)ServerZoneIpcType.UpdateHpMpTp, "updateHpMpTp" },
			{ (ushort)ServerZoneIpcType.NpcSpawn, "npcSpawn" },
			{ (ushort)ServerZoneIpcType.PlayerSpawn, "playerSpawn" },
			{ (ushort)ServerZoneIpcType.ObjectDespawn, "objectDespawn" },
		};

		public static dynamic ToDynamic(object obj) {
			IDictionary<string, object?> expando = new ExpandoObject();
			var type = obj.GetType();

			foreach (var fieldInfo in type.GetFields()) {
				expando.Add(fieldInfo.Name, fieldInfo.GetValue(obj));
			}

			foreach (var propertyInfo in type.GetProperties()) {
				expando.Add(propertyInfo.Name, propertyInfo.GetValue(obj));
			}

			return (ExpandoObject)expando;
		}

		public static dynamic? Convert(ushort opcode, IntPtr dataPtr, uint targetActorId) {
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
				case (ushort)ServerZoneIpcType.ActionEffect1:
					strObj = Marshal.PtrToStructure<XivIpcActionEffect1>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActionEffect8:
					strObj = Marshal.PtrToStructure<XivIpcActionEffect8>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActionEffect16:
					strObj = Marshal.PtrToStructure<XivIpcActionEffect16>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActionEffect24:
					strObj = Marshal.PtrToStructure<XivIpcActionEffect24>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.ActionEffect32:
					strObj = Marshal.PtrToStructure<XivIpcActionEffect32>(dataPtr);
					break;
				case (ushort)ServerZoneIpcType.EffectResult:
					strObj = Marshal.PtrToStructure<XivIpcEffectResult>(dataPtr);
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
				case (ushort)ServerZoneIpcType.ObjectDespawn:
					strObj = Marshal.PtrToStructure<XivIpcObjectDespawn>(dataPtr);
					break;
				default:
					return null;
			}

			var dyn = ToDynamic(strObj);

			dyn.targetActorId = targetActorId;
			dyn.targetActorName = NextUIPlugin.objectTable.SearchById(targetActorId)?.Name.TextValue ?? "";

			if (opcode == (ushort)ServerZoneIpcType.NpcSpawn) {
				var bNpcName = NextUIPlugin.dataManager.GetExcelSheet<BNpcName>()?.GetRow(
					((XivIpcNpcSpawn)strObj).bNPCName
				);

				if (bNpcName != null) {
					dyn.name = bNpcName.Singular.ToDalamudString().TextValue;
				}
			}

			return dyn;
		}
	}
}