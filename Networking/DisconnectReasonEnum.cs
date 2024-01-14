namespace FourInARowBattle;

/// <summary>
/// Enum for disconnect reason
/// </summary>
public enum DisconnectReasonEnum : byte
{
    //unknown
    UNKNOWN,
    //generic
    GENERIC,
    //client just wants to disconnect
    DESIRE,
    //client has a desync
    DESYNC,
    //client lost connection
    CONNECTION,
}
