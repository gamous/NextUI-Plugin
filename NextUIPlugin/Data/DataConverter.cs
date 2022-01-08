using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;
using Action = Lumina.Excel.GeneratedSheets.Action;
using LuminaStatus = Lumina.Excel.GeneratedSheets.Status;

namespace NextUIPlugin.Data {
	public static class DataConverter {
		public static object ActionToObject(Action action) {
			return new {
				id = action.RowId,
				name = action.Name.ToDalamudString(),
				range = action.Range,
				castType = action.CastType,
				maxCharges = action.MaxCharges,
				effectRange = action.EffectRange
			};
		}

		public static object ActorToObject(BattleChara actor, ulong? contentId = null) {
			return new {
				id = actor.ObjectId,
				contentId = contentId?.ToString("X16"),
				name = actor.Name.TextValue,
				nameId = actor.NameId,
				position = new { x = actor.Position.X, y = actor.Position.Y, z = actor.Position.Z },
				hp = actor.CurrentHp,
				hpMax = actor.MaxHp,
				mana = actor.CurrentMp,
				manaMax = actor.MaxMp,
				gp = actor.CurrentGp,
				gpMax = actor.MaxGp,
				cp = actor.CurrentCp,
				cpMax = actor.MaxCp,
				jobId = actor.ClassJob.Id,
				level = actor.Level,
				rotation = actor.Rotation,
				companyTag = actor.CompanyTag.TextValue,
			};
		}

		public static object PartyMemberToObject(PartyMember actor) {
			return new {
				id = actor.ObjectId,
				contentId = actor.ContentId,
				name = actor.Name.TextValue,
				position = new { x = actor.Position.X, y = actor.Position.Y, z = actor.Position.Z },
				hp = actor.CurrentHP,
				hpMax = actor.MaxHP,
				mana = actor.CurrentMP,
				manaMax = actor.MaxMP,
				jobId = actor.ClassJob.Id,
				level = actor.Level,
				worldId = actor.World.Id,
				worldName = actor.World.GameData.Name.ToDalamudString().TextValue,
				territoryId = actor.Territory.Id
			};
		}

		public static object StatusToObject(Status status) {
			return new {
				id = status.StatusId,
				name = status.GameData.Name.ToString(),
				remains = status.RemainingTime,
				sourceId = status.SourceID,
				stack = status.StackCount
			};
		}

		public static object LuminaStatusToObject(LuminaStatus status) {
			return new {
				id = status.RowId,
				name = status.Name.ToDalamudString(),
				description = status.Description.ToDalamudString(),
				category = status.Category,
				isPermanent = status.IsPermanent,
				maxStacks = status.MaxStacks,
			};
		}
	}
}