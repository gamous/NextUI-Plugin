using System;
using System.Collections.Generic;
using Dalamud.Game.Network;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using NextUIPlugin.Data;
using NextUIPlugin.Data.Handlers;

namespace NextUIPlugin.Service {
	// ReSharper disable once InconsistentNaming
	public static class IPCService {
		internal static ICallGateProvider<string, string, bool> registerCommand = null!;
		internal static ICallGateProvider<string, string, bool> unregisterCommand = null!;
		internal static ICallGateProvider<string, string, object, bool> broadcastCommand = null!;

		internal static ICallGateProvider<uint, List<object>, bool> partyChanged = null!;

		internal static ICallGateProvider<
			ushort, string, NetworkMessageDirection, (object, uint?), bool
		> networkEvent = null!;

		public static readonly List<string> registeredEvents = new();

		public static void InitIpc() {
			var pi = NextUIPlugin.pluginInterface;
			try {
				// Commands
				registerCommand = pi.GetIpcProvider<string, string, bool>("NextUI.Register");
				unregisterCommand = pi.GetIpcProvider<string, string, bool>("NextUI.Unregister");
				broadcastCommand = pi.GetIpcProvider<string, string, object, bool>("NextUI.Broadcast");

				// Actual events
				networkEvent = pi.GetIpcProvider<
					ushort, string, NetworkMessageDirection, (object, uint?), bool
				>("NextUI.NetworkEvent");
				partyChanged = pi.GetIpcProvider<uint, List<object>, bool>("NextUI.PartyChanged");

				registerCommand.RegisterFunc(Register);
				unregisterCommand.RegisterFunc(Unregister);
				broadcastCommand.RegisterFunc(Broadcast);
			}
			catch (Exception e) {
				PluginLog.Error($"Error registering IPC providers:\n{e}");
			}
		}

		internal static bool Broadcast(string pluginInternalName, string socketEvent, object data) {
			NextUIPlugin.socketServer.Broadcast(new {
				@event = socketEvent,
				data,
			});
			return true;
		}

		internal static bool Register(string pluginInternalName, string eventName) {
			PluginLog.Log($"IPC register {pluginInternalName} x {eventName} x");

			switch (eventName) {
				case "NetworkEvent" when !registeredEvents.Contains(eventName):
					NetworkHandler.NetworkListener += NetworkHandlerOnNetworkListener;
					registeredEvents.Add(eventName);
					break;
				case "PartyChanged" when !registeredEvents.Contains(eventName):
					PartyHandler.PartyChanged += PartyHandlerOnPartyChanged;
					registeredEvents.Add(eventName);
					break;
			}

			return true;
		}

		internal static bool Unregister(string pluginInternalName, string eventName) {
			PluginLog.Log($"IPC unregister {pluginInternalName} x {eventName} x");

			switch (eventName) {
				case "NetworkEvent" when registeredEvents.Contains(eventName):
					NetworkHandler.NetworkListener -= NetworkHandlerOnNetworkListener;
					registeredEvents.Remove(eventName);
					break;
				case "PartyChanged" when registeredEvents.Contains(eventName):
					PartyHandler.PartyChanged -= PartyHandlerOnPartyChanged;
					registeredEvents.Remove(eventName);
					break;
			}

			return true;
		}

		static private void PartyHandlerOnPartyChanged(uint partyLeader, List<object> party) {
			partyChanged.SendMessage(partyLeader, party);
		}

		internal static void NetworkHandlerOnNetworkListener(
			ushort opcode,
			string eventName,
			NetworkMessageDirection dir,
			object networkPacket,
			uint? actorTargetId
		) {
			networkEvent.SendMessage(opcode, eventName, dir, (networkPacket, actorTargetId));
		}

		public static void Deinit() {
			foreach (var registeredEvent in registeredEvents) {
				switch (registeredEvent) {
					case "NetworkEvent":
						NetworkHandler.NetworkListener -= NetworkHandlerOnNetworkListener;
						break;
					case "PartyChanged":
						PartyHandler.PartyChanged -= PartyHandlerOnPartyChanged;
						break;
				}
			}

			registerCommand.UnregisterFunc();
			unregisterCommand.UnregisterFunc();
		}
	}
}