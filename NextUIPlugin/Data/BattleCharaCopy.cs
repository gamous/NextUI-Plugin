using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;

namespace NextUIPlugin.Data {
	public class BattleCharaCopy {
		public string Name;
		public uint ObjectId;
		public Vector3 Position;
		public float Rotation;
		public uint CurrentHp;
		public uint MaxHp;
		public uint CurrentMp;
		public uint MaxMp;
		public uint CurrentGp;
		public uint MaxGp;
		public uint CurrentCp;
		public uint MaxCp;
		public byte Level;
		public string CompanyTag;
		public uint TargetObjectId;

		public static BattleCharaCopy FromBattleChara(BattleChara chara) {
			var output = new BattleCharaCopy();
			output.UpdateFromBattleChara(chara);
			return output;
		}

		public void UpdateFromBattleChara(BattleChara chara) {
			Name = chara.Name.TextValue;
			Position = new Vector3(chara.Position.X, chara.Position.Y, chara.Position.Z);
			Level = chara.Level;
			Rotation = chara.Rotation;
			CompanyTag = chara.CompanyTag.TextValue;
			CurrentCp = chara.CurrentCp;
			CurrentGp = chara.CurrentGp;
			CurrentHp = chara.CurrentHp;
			CurrentMp = chara.CurrentMp;
			MaxCp = chara.MaxCp;
			MaxGp = chara.MaxGp;
			MaxHp = chara.MaxHp;
			MaxMp = chara.MaxMp;
			ObjectId = chara.ObjectId;
			TargetObjectId = chara.TargetObjectId;
		}

		public bool HasChanged(BattleChara chara, bool comparePosition, bool compareTarget) {
			var isSame =
					Name == chara.Name.TextValue &&
					Level == chara.Level &&
					CompanyTag == chara.CompanyTag.TextValue &&
					CurrentCp == chara.CurrentCp &&
					CurrentGp == chara.CurrentGp &&
					CurrentHp == chara.CurrentHp &&
					CurrentMp == chara.CurrentMp &&
					MaxCp == chara.MaxCp &&
					MaxGp == chara.MaxGp &&
					MaxHp == chara.MaxHp &&
					MaxMp == chara.MaxMp
				;

			if (comparePosition) {
				isSame =
					isSame &&
					Position == new Vector3(chara.Position.X, chara.Position.Y, chara.Position.Z) &&
					// ReSharper disable once CompareOfFloatsByEqualityOperator
					Rotation == chara.Rotation;
			}

			if (compareTarget) {
				isSame =
					isSame &&
					TargetObjectId == chara.TargetObjectId;
			}

			return !isSame;
		}
	}
}