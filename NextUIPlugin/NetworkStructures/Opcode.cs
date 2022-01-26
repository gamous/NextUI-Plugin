namespace NextUIPlugin.NetworkStructures {
	/**
    * Server IPC Zone Type Codes.
    */
	enum ServerZoneIpcType : ushort {
		PlayerSetup = 0x0261, // updated 6.08
		UpdateHpMpTp = 0x02C9, // updated 6.08
		PlayerStats = 0x02C7, // updated 6.08
		ActorControl = 0x022F, // updated 6.08
		ActorControlSelf = 0x006B, // updated 6.08
		ActorControlTarget = 0x0191, // updated 6.08
		Playtime = 0x00CE, // updated 6.08
		UpdateSearchInfo = 0x03D1, // updated 6.08
		ExamineSearchInfo = 0x0297, // updated 6.08
		Examine = 0x03E2, // updated 6.08
		MarketBoardSearchResult = 0x00B2, // updated 6.08
		MarketBoardItemListingCount = 0x026A, // updated 6.08
		MarketBoardItemListingHistory = 0x013A, // updated 6.08
		MarketBoardItemListing = 0x01E2, // updated 6.08
		MarketBoardPurchase = 0x00A3, // updated 6.08
		ActorMove = 0x0370, // updated 6.08
		ResultDialog = 0x027C, // updated 6.08
		RetainerInformation = 0x023B, // updated 6.08
		NpcSpawn = 0x032C, // updated 6.08
		NpcSpawn2 = 0x008F, // updated 6.08
		ItemMarketBoardInfo = 0x0114, // updated 6.08
		PlayerSpawn = 0x0226, // updated 6.08
		ContainerInfo = 0x037A, // updated 6.08
		ItemInfo = 0x02A9, // updated 6.08
		UpdateClassInfo = 0x00FE, // updated 6.08
		ActorCast = 0x0104, // updated 6.08
		CurrencyCrystalInfo = 0x02BE, // updated 6.08
		InitZone = 0x01EB, // updated 6.08
		EffectResult = 0x00DE, // updated 6.08
		EventStart = 0x00AE, // updated 6.08
		EventFinish = 0x0210, // updated 6.08
		SomeDirectorUnk4 = 0x00EF, // updated 6.08
		UpdateInventorySlot = 0x0375, // updated 6.08
		DesynthResult = 0x0143, // updated 6.08
		InventoryActionAck = 0x008A, // updated 6.08
		InventoryTransaction = 0x0382, // updated 6.08
		InventoryTransactionFinish = 0x0299, // updated 6.08
		CFPreferredRole = 0x02DA, // updated 6.08
		CFNotify = 0x01C5, // updated 6.08
		PrepareZoning = 0x039A, // updated 6.08
		ActorSetPos = 0x0395, // updated 6.08
		PlaceFieldMarker = 0x0067, // updated 6.08
		PlaceFieldMarkerPreset = 0x01FE, // updated 6.08
		ObjectSpawn = 0x03A3, // updated 6.08
		StatusEffectList = 0x00BC, // updated 6.08
		StatusEffectList2 = 0x01FF, // updated 6.08
		StatusEffectList3 = 0x02AF, // updated 6.08
		ActorGauge = 0x03B5, // updated 6.08
		FreeCompanyInfo = 0x01A2, // updated 6.08
		FreeCompanyDialog = 0x0288, // updated 6.08
		AirshipTimers = 0x0225, // updated 6.08
		SubmarineTimers = 0x034A, // updated 6.08
		AirshipStatusList = 0x01F5, // updated 6.08
		AirshipStatus = 0x023E, // updated 6.08
		AirshipExplorationResult = 0x0212, // updated 6.08
		SubmarineProgressionStatus = 0x0092, // updated 6.08
		SubmarineStatusList = 0x019D, // updated 6.08
		SubmarineExplorationResult = 0x00C9, // updated 6.08

		// EffectResultBasic = 0x034E, // updated 6.08
		EffectResultBasic = 0x02D9, // updated 6.08
		
		ActionEffect1 = 0x03C7, // updated 6.08
		ActionEffect8 = 0x0149, // updated 6.08
		ActionEffect16 = 0x00C1, // updated 6.08
		ActionEffect24 = 0x0213, // updated 6.08
		ActionEffect32 = 0x038B, // updated 6.08

		EventPlay = 0x113, // Updated for 6.08
		EventPlay4 = 0x302, // Updated for 6.08
		EventPlay8 = 0x78, // Updated for 6.08
		EventPlay16 = 0x223, // Updated for 6.08
		EventPlay32 = 0x2F2, // Updated for 6.08
		EventPlay64 = 0x3BC, // Updated for 6.08
		EventPlay128 = 0x33E, // Updated for 6.08
		EventPlay255 = 0x79, // Updated for 6.08

		WeatherChange = 0x017D, // updated 6.08

		Logout = 0x03B2, // updated 6.08

		ObjectDespawn = 0x0082, // updated 6.08
	};

	/**
    * Client IPC Zone Type Codes.
    */
	enum ClientZoneIpcType : ushort {
		UpdatePosition = 0x0147, // updated 6.08
		ClientTrigger = 0x02F1, // updated 6.08
		ChatHandler = 0x01C8, // updated 6.08
		SetSearchInfoHandler = 0x02BB, // updated 6.08
		MarketBoardPurchaseHandler = 0x0282, // updated 6.08
		InventoryModifyHandler = 0x0154, // updated 6.08 (Base offset: 0x015B)
		UpdatePositionInstance = 0x0209, // updated 6.08

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