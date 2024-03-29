using System.Text.Json;

namespace PcSyncLib;

public class PcSyncData
{
    public DateTime LastSyncTime { get; set; } = DateTime.MinValue;

    public string ToJson() => JsonSerializer.Serialize(this);
    public static PcSyncData? FromJson(string json) => JsonSerializer.Deserialize<PcSyncData>(json);
}