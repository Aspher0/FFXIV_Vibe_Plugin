using System;

namespace FFXIV_Vibe_Plugin;

internal class OpCodes
{
    public static string? GetName(ushort opCode)
    {
        string name = "?Unknow?";

        if (Enum.IsDefined(typeof(ServerLobbyIpcType), opCode))
            name = "ServerLobbyIpcType-" + Enum.GetName(typeof(ServerLobbyIpcType), opCode);

        if (Enum.IsDefined(typeof(ClientLobbyIpcType), opCode))
            name = "ClientLobbyIpcType-" + Enum.GetName(typeof(ClientLobbyIpcType), opCode);

        if (Enum.IsDefined(typeof(ServerZoneIpcType), opCode))
            name = "ServerZoneIpcType-" + Enum.GetName(typeof(ServerZoneIpcType), opCode);

        if (Enum.IsDefined(typeof(ClientZoneIpcType), opCode))
            name = "ClientZoneIpcType-" + Enum.GetName(typeof(ClientZoneIpcType), opCode);

        if (Enum.IsDefined(typeof(ServerChatIpcType), opCode))
            name = "ServerChatIpcType-" + Enum.GetName(typeof(ServerChatIpcType), opCode);

        if (Enum.IsDefined(typeof(ClientChatIpcType), opCode))
            name = "ClientChatIpcType-" + Enum.GetName(typeof(ClientChatIpcType), opCode);

        return name;
    }

    public enum ServerLobbyIpcType : ushort
    {
        LobbyError = 2,
        LobbyServiceAccountList = 12, // 0x000C
        LobbyCharList = 13, // 0x000D
        LobbyCharCreate = 14, // 0x000E
        LobbyEnterWorld = 15, // 0x000F
        LobbyServerList = 21, // 0x0015
        LobbyRetainerList = 23, // 0x0017
    }

    public enum ClientLobbyIpcType : ushort
    {
        ReqCharList = 3,
        ReqEnterWorld = 4,
        ClientVersionInfo = 5,
        ReqCharDelete = 10, // 0x000A
        ReqCharCreate = 11, // 0x000B
    }

    public enum ServerZoneIpcType : ushort
    {
        EventPlay32 = 101, // 0x0065
        ItemMarketBoardInfo = 138, // 0x008A
        PlayerSetup = 139, // 0x008B
        InventoryTransaction = 143, // 0x008F
        PrepareZoning = 144, // 0x0090
        MarketBoardPurchase = 157, // 0x009D
        EventPlay = 165, // 0x00A5
        ResultDialog = 175, // 0x00AF
        AirshipExplorationResult = 180, // 0x00B4
        AirshipTimers = 237, // 0x00ED
        ContainerInfo = 238, // 0x00EE
        SubmarineTimers = 245, // 0x00F5
        InventoryActionAck = 252, // 0x00FC
        ActorCast = 264, // 0x0108
        Examine = 283, // 0x011B
        RetainerInformation = 297, // 0x0129
        PlayerSpawn = 307, // 0x0133
        SomeDirectorUnk4 = 356, // 0x0164
        ActorControlTarget = 360, // 0x0168
        EventPlay128 = 366, // 0x016E
        ItemInfo = 371, // 0x0173
        ActorControl = 382, // 0x017E
        SubmarineExplorationResult = 387, // 0x0183
        EventPlay8 = 395, // 0x018B
        MarketBoardItemListingHistory = 402, // 0x0192
        EffectResult = 406, // 0x0196
        ActorSetPos = 409, // 0x0199
        EventFinish = 440, // 0x01B8
        PlaceFieldMarkerPreset = 463, // 0x01CF
        AirshipStatus = 483, // 0x01E3
        EventPlay16 = 500, // 0x01F4
        WeatherChange = 509, // 0x01FD
        MarketBoardSearchResult = 513, // 0x0201
        EventPlay4 = 558, // 0x022E
        ActorMove = 565, // 0x0235
        MarketBoardItemListingCount = 572, // 0x023C
        AirshipStatusList = 575, // 0x023F
        CurrencyCrystalInfo = 600, // 0x0258
        ActorGauge = 643, // 0x0283
        UpdateHpMpTp = 662, // 0x0296
        UpdateInventorySlot = 694, // 0x02B6
        InitZone = 708, // 0x02C4
        StatusEffectList = 709, // 0x02C5
        DesynthResult = 725, // 0x02D5
        ActorControlSelf = 742, // 0x02E6
        Logout = 748, // 0x02EC
        SubmarineStatusList = 756, // 0x02F4
        SubmarineProgressionStatus = 779, // 0x030B
        CFNotify = 791, // 0x0317
        ObjectSpawn = 793, // 0x0319
        FreeCompanyInfo = 796, // 0x031C
        MarketBoardItemListing = 803, // 0x0323
        NpcSpawn = 814, // 0x032E
        EventStart = 820, // 0x0334
        Effect = 858, // 0x035A
        EventPlay255 = 870, // 0x0366
        FreeCompanyDialog = 878, // 0x036E
        PlaceFieldMarker = 893, // 0x037D
        PlayerStats = 909, // 0x038D
        InventoryTransactionFinish = 923, // 0x039B
        UpdateClassInfo = 933, // 0x03A5
        EventPlay64 = 936, // 0x03A8
        Playtime = 963, // 0x03C3
    }

    public enum ClientZoneIpcType : ushort
    {
        InventoryModifyHandler = 163, // 0x00A3
        MarketBoardPurchaseHandler = 220, // 0x00DC
        UpdatePositionInstance = 355, // 0x0163
        ChatHandler = 460, // 0x01CC
        UpdatePositionHandler = 838, // 0x0346
        ClientTrigger = 940, // 0x03AC
        SetSearchInfoHandler = 945, // 0x03B1
    }

    public enum ServerChatIpcType : ushort
    {
    }

    public enum ClientChatIpcType : ushort
    {
    }
}
