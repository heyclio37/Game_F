using Steamworks;
using UnityEngine;

public static class PlayerNameProvider
{
    private const string PREFS_KEY = "LocalPlayerName";

    public static string GetLocalName()
    {
        if (IsSteamModeActive())
            return SteamFriends.GetPersonaName();
        return PlayerPrefs.GetString(PREFS_KEY, "");
    }

    public static ulong GetLocalSteamId()
    {
        if (IsSteamModeActive())
            return SteamUser.GetSteamID().m_SteamID;
        return 0;
    }

    public static void SaveLocalName(string name)
    {
        PlayerPrefs.SetString(PREFS_KEY, name);
        PlayerPrefs.Save();
    }

    private static bool IsSteamModeActive()
    {
        if (GameConnectionManager.Instance == null) return false;
        if (GameConnectionManager.Instance.Mode != ConnectionMode.Steam) return false;
        return SteamManager.Initialized;
    }
}