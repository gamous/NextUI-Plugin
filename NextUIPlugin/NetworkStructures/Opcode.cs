namespace NextUIPlugin.NetworkStructures {
	/**
    * Server IPC Zone Type Codes.
    */
	// Updated 6.11 hotfix
	public enum ServerLobbyIpcType : ushort {

	};

	public enum ClientLobbyIpcType : ushort {

	};

	public enum ServerZoneIpcType : ushort {
		ActorCast = 0x00FF,
		ActorControl = 0x012D,
		ActorControlSelf = 0x02B4,
		ActorControlTarget = 0x02F8,
		ActorGauge = 0x030D,
		ActorMove = 0x0376,
		ActorSetPos = 0x02F5,
		AirshipExplorationResult = 0x02A8,
		AirshipStatus = 0x037E,
		AirshipStatusList = 0x011C,
		AoeEffect16 = 0x01B6,
		AoeEffect24 = 0x024E,
		AoeEffect32 = 0x00C7,
		AoeEffect8 = 0x01AB,
		BossStatusEffectList = 0x035B,
		CEDirector = 0x0356,
		CFNotify = 0x03D1,
		CFPreferredRole = 0x036F,
		ContainerInfo = 0x03C9,
		CurrencyCrystalInfo = 0x0294,
		DesynthResult = 0x0199,
		Effect = 0x006D,
		EffectResult = 0x03CD,
		EnvironmentControl = 0x009A,
		EventFinish = 0x03D7,
		EventPlay = 0x0321,
		EventPlay4 = 0x03AA,
		EventStart = 0x012C,
		Examine = 0x01C7,
		ExamineSearchInfo = 0x038D,
		FateInfo = 0x01B5,
		FreeCompanyDialog = 0x02A3,
		FreeCompanyInfo = 0x0127,
		HousingWardInfo = 0x0237,
		InitZone = 0x019B,
		InventoryActionAck = 0x0139,
		InventoryTransaction = 0x0281,
		InventoryTransactionFinish = 0x00DE,
		ItemInfo = 0x03BC,
		ItemMarketBoardInfo = 0x0336,
		Logout = 0x028C,
		MarketBoardItemListing = 0x0190,
		MarketBoardItemListingCount = 0x00EF,
		MarketBoardItemListingHistory = 0x0351,
		MarketBoardPurchase = 0x036B,
		MarketBoardSearchResult = 0x0081,
		MiniCactpotInit = 0x011A,
		NpcSpawn = 0x03CB,
		NpcSpawn2 = 0x0289,
		ObjectSpawn = 0x01E8,
		PlaceFieldMarker = 0x0262,
		PlaceFieldMarkerPreset = 0x0225,
		PlayerSetup = 0x00A7,
		PlayerSpawn = 0x02C6,
		PlayerStats = 0x0089,
		Playtime = 0x033D,
		PrepareZoning = 0x03CA,
		ResultDialog = 0x009D,
		RetainerInformation = 0x028E,
		StatusEffectList = 0x00E1,
		StatusEffectList2 = 0x00B3,
		StatusEffectList3 = 0x0231,
		SubmarineExplorationResult = 0x021C,
		SubmarineProgressionStatus = 0x0394,
		SubmarineStatusList = 0x03A6,
		SystemLogMessage = 0x0296,
		UpdateClassInfo = 0x03E0,
		UpdateHpMpTp = 0x00D8,
		UpdateInventorySlot = 0x00B1,
		UpdateSearchInfo = 0x03CE,
	};

	public enum ClientZoneIpcType : ushort {
		ChatHandler = 0x0264,
		ClientTrigger = 0x022F,
		InventoryModifyHandler = 0x0313,
		MarketBoardPurchaseHandler = 0x0275,
		SetSearchInfoHandler = 0x007D,
		UpdatePositionHandler = 0x0077,
		UpdatePositionInstance = 0x02BD,
	};

	public enum ServerChatIpcType : ushort {

	};

	public enum ClientChatIpcType : ushort {

	};
}