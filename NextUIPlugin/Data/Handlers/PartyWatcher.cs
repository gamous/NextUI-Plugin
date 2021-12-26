using System.Collections.Generic;
using System.Linq;
using Fleck;

namespace NextUIPlugin.Data.Handlers {
	public static class PartyWatcher {
		internal static List<uint> party = new();
		internal static uint partyLeader;

		public static void Watch() {
			var sockets = NextUIPlugin.socketServer.GetEventSubscriptions("partyChanged");
			if (sockets == null || sockets.Count == 0) {
				return;
			}

			var currentParty = NextUIPlugin.partyList
				.Select(partyMember => partyMember.ObjectId).ToList();

			if (party.Count != currentParty.Count || partyLeader != NextUIPlugin.partyList.PartyLeaderIndex) {
				partyLeader = NextUIPlugin.partyList.PartyLeaderIndex;
				party = currentParty;
				BroadcastPartyChanged(sockets, partyLeader);
				return;
			}

			var eq = DataHandler.CompareList(party, currentParty);
			if (eq) {
				return;
			}

			BroadcastPartyChanged(sockets, NextUIPlugin.partyList.PartyLeaderIndex);
			party = currentParty;
		}

		internal static void BroadcastPartyChanged(List<IWebSocketConnection> sockets, uint newPartyLeader) {
			var currentParty = new List<object>();
			foreach (var partyMember in NextUIPlugin.partyList) {
				currentParty.Add(DataConverter.PartyMemberToObject(partyMember));
			}

			NextUIPlugin.socketServer.BroadcastTo(new {
				@event = "partyChanged",
				data = new { currentParty, newPartyLeader },
			}, sockets);
		}
	}
}