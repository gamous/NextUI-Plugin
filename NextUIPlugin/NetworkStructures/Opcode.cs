namespace NextUIPlugin.NetworkStructures {
	/**
    * Server IPC Zone Type Codes.
    */
	enum ServerZoneIpcType : ushort {
		PlayerSetup = 0x03DD, // updated 6.05
		UpdateHpMpTp = 0x00F4, // updated 6.05
		PlayerStats = 0x018C, // updated 6.05
		ActorControl = 0x02CF, // updated 6.05
		ActorControlSelf = 0x0096, // updated 6.05
		ActorControlTarget = 0x0272, // updated 6.05
		Playtime = 0x039F, // updated 6.05
		UpdateSearchInfo = 0x01E3, // updated 6.05
		ExamineSearchInfo = 0x0222, // updated 6.05
		Examine = 0x02AB, // updated 6.05
		MarketBoardSearchResult = 0x0213, // updated 6.05
		MarketBoardItemListingCount = 0x02A3, // updated 6.05
		MarketBoardItemListingHistory = 0x02DB, // updated 6.05
		MarketBoardItemListing = 0x01F2, // updated 6.05
		MarketBoardPurchase = 0x03DB, // updated 6.05
		ActorMove = 0x00DB, // updated 6.05
		ResultDialog = 0x03D0, // updated 6.05
		RetainerInformation = 0x0318, // updated 6.05
		NpcSpawn = 0x01D2, // updated 6.05
		ItemMarketBoardInfo = 0x029A, // updated 6.05
		PlayerSpawn = 0x0338, // updated 6.05
		ContainerInfo = 0x0130, // updated 6.05
		ItemInfo = 0x0280, // updated 6.05
		UpdateClassInfo = 0x0202, // updated 6.05
		ActorCast = 0x0307, // updated 6.05
		CurrencyCrystalInfo = 0x0126, // updated 6.05
		InitZone = 0x0137, // updated 6.05
		// EffectResult = 0x01DF, // updated 6.05
		EffectResult = 0x0203, // updated 6.05
		EventStart = 0x01D6, // updated 6.05
		EventFinish = 0x0206, // updated 6.05
		SomeDirectorUnk4 = 0x027A, // updated 6.05
		UpdateInventorySlot = 0x027E, // updated 6.05
		DesynthResult = 0x0273, // updated 6.05
		InventoryActionAck = 0x02EA, // updated 6.05
		InventoryTransaction = 0x02BC, // updated 6.05
		InventoryTransactionFinish = 0x0269, // updated 6.05
		CFNotify = 0x0183, // updated 6.05
		PrepareZoning = 0x01DD, // updated 6.05
		ActorSetPos = 0x0081, // updated 6.05
		PlaceFieldMarker = 0x00FD, // updated 6.05
		PlaceFieldMarkerPreset = 0x0067, // updated 6.05
		ObjectSpawn = 0x01FD, // updated 6.05
		StatusEffectList = 0x0188, // updated 6.05
		StatusEffectList2 = 0x0293, // updated 6.05
		StatusEffectList3 = 0x0353, // updated 6.05
		ActorGauge = 0x022D, // updated 6.05
		FreeCompanyInfo = 0x013D, // updated 6.05
		FreeCompanyDialog = 0x0261, // updated 6.05
		AirshipTimers = 0x01C7, // updated 6.05
		SubmarineTimers = 0x0211, // updated 6.05
		AirshipStatusList = 0x0182, // updated 6.05
		AirshipStatus = 0x01AD, // updated 6.05
		AirshipExplorationResult = 0x02BB, // updated 6.05
		SubmarineProgressionStatus = 0x0121, // updated 6.05
		SubmarineStatusList = 0x03A9, // updated 6.05
		SubmarineExplorationResult = 0x0128, // updated 6.05
		EffectResultBasic = 0x0330, // updated 6.05

		// CHANGED sapphire
		// CreateObject = 0x019D,
		// DeleteObject = 0x019E,
		ObjectDespawn = 0x0227, // updated 6.05

		// CHANGED based on machina
		ActionEffect1 = 0x033E, // updated 6.05
		ActionEffect8 = 0x01F4, // updated 6.05
		ActionEffect16 = 0x01FA, // updated 6.05
		ActionEffect24 = 0x0300, // updated 6.05
		ActionEffect32 = 0x03CD, // updated 6.05

		EventPlay = 0x13F, // updated 6.05
		EventPlay4 = 0x212, // updated 6.05
		EventPlay8 = 0x10B, // updated 6.05
		EventPlay16 = 0xD0, // updated 6.05
		EventPlay32 = 0xC5, // updated 6.05
		EventPlay64 = 0xC6, // updated 6.05
		EventPlay128 = 0x32C, // updated 6.05
		EventPlay255 = 0x295, // updated 6.05

		WeatherChange = 0x028A, // updated 6.05

		Logout = 0x008A, // updated 6.05
	};

	/**
    * Client IPC Zone Type Codes.
    */
	enum ClientZoneIpcType : ushort {
		UpdatePosition = 0x021B, // Updated 6.05
		ClientTrigger = 0x00E7, // Updated 6.05
		ChatHandler = 0x0276, // Updated 6.05
		SetSearchInfoHandler = 0x031A, // Updated 6.05
		MarketBoardPurchaseHandler = 0x0387, // Updated 6.05
		InventoryModifyHandler = 0x01B9, // Updated 6.05 (Base offset: 0x01C0)
		UpdatePositionInstance = 0x00A3, // Updated 6.05

		//PingHandler = 0x02CD, // updated 5.58 hotfix
		//InitHandler = 0x01AA, // updated 5.58 hotfix

		//FinishLoadingHandler = 0x02DA, // updated 5.58 hotfix

		//CFCommenceHandler = 0x0092, // updated 5.58 hotfix

		//CFRegisterDuty = 0x03C7, // updated 5.58 hotfix
		//CFRegisterRoulette = 0x00C2, // updated 5.58 hotfix
		//PlayTimeHandler = 0x00B0, // updated 5.58 hotfix
		//LogoutHandler = 0x0178, // updated 5.58 hotfix
		//CancelLogout = 0x01F9, // updated 5.58 hotfix

		//CFDutyInfoHandler = 0x0092, // updated 5.58 hotfix

		//SocialReqSendHandler = 0x023A, // updated 5.58 hotfix
		//CreateCrossWorldLS = 0x0336, // updated 5.58 hotfix

		//SocialListHandler = 0x0187, // updated 5.58 hotfix
		//ReqSearchInfoHandler = 0x022C, // updated 5.58 hotfix
		//ReqExamineSearchCommentHandler = 0x0315, // updated 5.58 hotfix

		//ReqRemovePlayerFromBlacklist = 0x0145, // updated 5.58 hotfix
		//BlackListHandler = 0x0161, // updated 5.58 hotfix
		//PlayerSearchHandler = 0x02FF, // updated 5.58 hotfix

		//LinkshellListHandler = 0x023B, // updated 5.58 hotfix

		//MarketBoardRequestItemListingInfo = 0x0189, // updated 5.58 hotfix
		//MarketBoardRequestItemListings = 0x0092, // updated 5.58 hotfix
		//MarketBoardSearch = 0x02F9, // updated 5.58 hotfix

		//ReqExamineFcInfo = 0x0136, // updated 5.58 hotfix

		//FcInfoReqHandler = 0x0234, // updated 5.58 hotfix

		//FreeCompanyUpdateShortMessageHandler = 0x0123, // added 5.0

		//ReqMarketWishList = 0x0306, // updated 5.58 hotfix

		//ReqJoinNoviceNetwork = 0x01D5, // updated 5.58 hotfix

		//ReqCountdownInitiate = 0x00C2, // updated 5.58 hotfix
		//ReqCountdownCancel = 0x00E6, // updated 5.58 hotfix

		//ZoneLineHandler = 0x03CC, // updated 5.58 hotfix
		//DiscoveryHandler = 0x023A, // updated 5.58 hotfix


		//PlaceFieldMarker = 0x02AF, // updated 5.58 hotfix
		//PlaceFieldMarkerPreset = 0x018E, // updated 5.58 hotfix
		//SkillHandler = 0x0244, // updated 5.58 hotfix
		//GMCommand1 = 0x018A, // updated 5.58 hotfix
		//GMCommand2 = 0x02FD, // updated 5.58 hotfix
		//AoESkillHandler = 0x01F1, // updated 5.58 hotfix

		//InventoryEquipRecommendedItems = 0x0109, // updated 5.58 hotfix

		//ReqPlaceHousingItem = 0x0352, // updated 5.58 hotfix
		//BuildPresetHandler = 0x024E, // updated 5.58 hotfix

		//TalkEventHandler = 0x0305, // updated 5.58 hotfix
		//EmoteEventHandler = 0x03A7, // updated 5.58 hotfix
		//WithinRangeEventHandler = 0x02EE, // updated 5.58 hotfix
		//OutOfRangeEventHandler = 0x00EE, // updated 5.58 hotfix
		//EnterTeriEventHandler = 0x0389, // updated 5.58 hotfix

		//ReturnEventHandler = 0x03B4, // updated 5.58 hotfix
		//TradeReturnEventHandler = 0x0216, // updated 5.58 hotfix

		//LinkshellEventHandler = 0x0239, // updated 5.58 hotfix
		//LinkshellEventHandler1 = 0x0239, // updated 5.58 hotfix

		//ReqEquipDisplayFlagsChange = 0x01F6, // updated 5.58 hotfix

		//LandRenameHandler = 0x018C, // updated 5.58 hotfix
		//HousingUpdateHouseGreeting = 0x02F4, // updated 5.58 hotfix
		//HousingUpdateObjectPosition = 0x02CB, // updated 5.58 hotfix

		//SetSharedEstateSettings = 0x0179, // updated 5.58 hotfix

		//PerformNoteHandler = 0x016E, // updated 5.58 hotfix
	};

	/**
	* Server IPC Chat Type Codes.
	*/
	enum ServerChatIpcType : ushort {
		//Tell = 0x0064, // updated for sb
		//TellErrNotFound = 0x0066,

		//FreeCompanyEvent = 0x012C, // added 5.0
	};

	/**
    * Client IPC Chat Type Codes.
    */
	enum ClientChatIpcType : ushort {
		//TellReq = 0x0064,
	};
}