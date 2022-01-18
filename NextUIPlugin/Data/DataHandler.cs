using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using NextUIPlugin.Data.Handlers;
using NextUIPlugin.Socket;
using SpellAction = Lumina.Excel.GeneratedSheets.Action;

namespace NextUIPlugin.Data {
	public class DataHandler : IDisposable {

		public DataHandler() {
			NextUIPlugin.framework.Update += FrameworkOnUpdate;
			NextUIPlugin.chatGui.ChatMessage += ChatGuiOnChatMessage;
			NextUIPlugin.clientState.Login += ClientStateOnLogin;
			NextUIPlugin.clientState.Logout += ClientStateOnLogout;
			NextUIPlugin.clientState.TerritoryChanged += ClientStateOnTerritoryChanged;
		}

		protected void FrameworkOnUpdate(Framework framework) {
			if (NextUIPlugin.clientState.LocalPlayer == null || !NextUIPlugin.socketServer.IsRunning()) {
				return;
			}

			TargetHandler.Watch();
			ActorHandler.Watch();
			PartyHandler.Watch();
			XWorldPartyHandler.Watch();
			EnmityListHandler.Watch();
			UiVisibility.Watch();
			StatusFlagsHandler.Watch();
		}

		public static bool CompareList<T>(List<T> a, List<T> b) {
			if (a.Count != b.Count) {
				return false;
			}

			for (var i = 0; i < a.Count; i++) {
				if (!a[i]!.Equals(b[i])) {
					return false;
				}
			}

			return true;
			// var firstNotSecond = a.Except(b).ToList();
			// var secondNotFirst = b.Except(a).ToList();
			//
			// return !firstNotSecond.Any() && !secondNotFirst.Any();
		}

		#region Broadcasters

		protected void ClientStateOnTerritoryChanged(object? sender, ushort e) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "zoneChanged",
				data = e
			});
		}

		protected void ClientStateOnLogout(object? sender, EventArgs e) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = "playerLogout",
			});
		}

		protected void ClientStateOnLogin(object? sender, EventArgs e) {
			var player = NextUIPlugin.clientState.LocalPlayer;

			object? data = null;
			if (player != null && NextUIPlugin.clientState.LocalContentId != 0) {
				data = DataConverter.ActorToObject(player, NextUIPlugin.clientState.LocalContentId);
			}

			NextUIPlugin.socketServer.Broadcast(new {
				@event = "playerLogin",
				data = data
			});

			UiVisibility.Initialize();
			EnmityListHandler.Initialize();
		}

		protected void ChatGuiOnChatMessage(
			XivChatType type,
			uint senderId,
			ref SeString sender,
			ref SeString message,
			ref bool isHandled
		) {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("chatMessage");
			if (sockets != null && sockets.Count > 0) {
				NextUISocket.BroadcastTo(new {
					@event = "chatMessage",
					data = new {
						typeId = (ushort)type,
						senderId,
						sender = sender.TextValue,
						message = message.TextValue,
					}
				}, sockets);
			}
		}

		#endregion

		public void Dispose() {
			NextUIPlugin.framework.Update -= FrameworkOnUpdate;
			NextUIPlugin.chatGui.ChatMessage -= ChatGuiOnChatMessage;
			NextUIPlugin.clientState.Login -= ClientStateOnLogin;
			NextUIPlugin.clientState.Logout -= ClientStateOnLogout;
			NextUIPlugin.clientState.TerritoryChanged -= ClientStateOnTerritoryChanged;
		}
	}
}