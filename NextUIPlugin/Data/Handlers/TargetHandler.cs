using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dalamud.Game.ClientState.Objects.Types;
using Fleck;
using NextUIPlugin.Socket;

namespace NextUIPlugin.Data.Handlers {
	public static unsafe class TargetHandler {
		internal static readonly ReadOnlyCollection<string> targetTypes = new(
			new[] { "target", "targetOfTarget", "hover", "focus" }
		);

		internal static readonly Dictionary<string, uint?> targets = new() {
			{ "target", null },
			{ "targetOfTarget", null },
			{ "hover", null },
			{ "focus", null },
		};

		#region Commands

		public static void RegisterCommands() {
			NextUISocket.RegisterCommand("setTarget", (socket, ev) => {
				SetTargetInternal(socket, ev, "target");
			});
			NextUISocket.RegisterCommand("setFocus", (socket, ev) => {
				SetTargetInternal(socket, ev, "focus");
			});
			NextUISocket.RegisterCommand("setMouseOver", (socket, ev) => {
				SetTargetInternal(socket, ev, "mouseOver");
			});
		}

		public static void SetTargetInternal(IWebSocketConnection socket, SocketEvent ev, string type) {
			try {
				var objectId = ev.request?.id ?? 0;

				GameObject? target = null;
				if (objectId != 0) {
					target = NextUIPlugin.objectTable.SearchById(objectId);
					if (target == null) {
						NextUISocket.Respond(socket, ev, new { success = false, message = "Invalid object ID" });
						return;
					}
				}

				switch (type) {
					case "target":
						NextUIPlugin.targetManager.SetTarget(target);
						break;
					case "focus":
						NextUIPlugin.targetManager.SetFocusTarget(target);
						break;
					case "mouseOver":
						NextUIPlugin.targetManager.SetMouseOverTarget(target);
						break;
				}

				NextUISocket.Respond(socket, ev, new { success = true });
			}
			catch (Exception err) {
				NextUISocket.Respond(socket, ev, new { success = false, message = err.ToString() });
			}
		}

		#endregion

		#region Watchers

		internal static (GameObject?, uint?) GetTargetObjectId(string targetType) {
			var obj = targetType switch {
				"target" => NextUIPlugin.targetManager.Target,
				"targetOfTarget" => NextUIPlugin.targetManager.Target?.TargetObject,
				"hover" => NextUIPlugin.targetManager.MouseOverTarget,
				"focus" => NextUIPlugin.targetManager.FocusTarget,
				_ => throw new Exception("Unexpected target type")
			};

			if (obj == null) {
				return (null, null);
			}

			var realObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj.Address;
			return (obj, realObject->GetObjectID().ObjectID);
		}

		public static void Watch() {
			foreach (var unitId in targetTypes) {
				var eventName = unitId + "Changed";
				var sockets = NextUIPlugin.socketServer.GetEventSubscriptions(eventName);
				if (sockets == null || sockets.Count == 0) {
					continue;
				}

				var (gameObject, currentTarget) = GetTargetObjectId(unitId);
				var oldTarget = targets[unitId];

				if (currentTarget == oldTarget) {
					continue;
				}

				BroadcastTargetChanged(sockets, unitId, gameObject);
				targets[unitId] = currentTarget;
			}
		}

		internal static void BroadcastTargetChanged(
			List<IWebSocketConnection> sockets,
			string unitId,
			GameObject? actor = null
		) {
			NextUISocket.BroadcastTo(new {
				@event = unitId + "Changed",
				data = new {
					actorId = actor?.ObjectId,
					actorName = actor?.Name.TextValue,
					unit = unitId,
					actor = actor == null
						? null
						: (actor is BattleChara chara ? DataConverter.ActorToObject(chara) : null),
				}
			}, sockets);
		}

		#endregion

		// TODO: Think about migrating to FFXIVStructs instead
		// private static GameObject* GetTargetOfTarget(GameObject* obj) {
		// 	if (obj == null) return null;
		// 	if (!obj->IsCharacter()) {
		// 		return null;
		// 	}
		// 		
		// 	var targetId = ((Character*)obj)->GetTargetId();
		// 	if (targetId is 0 or 0xE000_0000) {
		// 		return null;
		// 	}
		// 		
		// 	var mgr = GameObjectManager.Instance();
		// 	for (var i = 0; i < mgr->ObjectListFilteredCount; i++) {
		// 		var objPtr = mgr->ObjectListFiltered[i];
		// 		if (objPtr == 0) continue;
		// 		if (((GameObject*)objPtr)->GetObjectID().ObjectID == targetId) {
		// 			return (GameObject*)objPtr;
		// 		}
		// 			
		// 	}
		// 	return null;
		// }
	}
}