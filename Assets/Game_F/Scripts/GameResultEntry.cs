using FishNet.Serializing;

public struct GameResultEntry
{
    public string PlayerName;
    public int ClientId;
    public bool Escaped;
    public bool Captured;

    public static void WriteGameResultEntry(Writer writer, GameResultEntry value)
    {
        writer.WriteString(value.PlayerName);
        writer.WriteInt32(value.ClientId);
        writer.WriteBoolean(value.Escaped);
        writer.WriteBoolean(value.Captured);
    }

    public static GameResultEntry ReadGameResultEntry(Reader reader)
    {
        return new GameResultEntry
        {
            PlayerName = reader.ReadString(),
            ClientId = reader.ReadInt32(),
            Escaped = reader.ReadBoolean(),
            Captured = reader.ReadBoolean()
        };
    }
}