using System.Text;

namespace Nuve.DataStore.Redis;

/// <summary>
/// Computes the cluster slot of a key the same way Redis does, so multi-key commands can be
/// grouped per slot on deployments that reject a command spanning several slots.
/// </summary>
internal static class RedisHashSlot
{
    private const int SlotCount = 16384;
    private const ushort Polynomial = 0x1021;

    /// <summary>
    /// The slot the key belongs to. When the key carries a hash tag only the tag decides the slot,
    /// which is what lets related keys be placed together on purpose.
    /// </summary>
    internal static int Of(string key)
    {
        return Crc16(HashTagOf(key)) % SlotCount;
    }

    /// <summary>
    /// The content between the first '{' and the '}' that follows it. An empty tag does not count,
    /// in which case the whole key decides the slot.
    /// </summary>
    private static string HashTagOf(string key)
    {
        var start = key.IndexOf('{');
        if (start < 0)
            return key;

        var end = key.IndexOf('}', start + 1);
        return end > start + 1
            ? key.Substring(start + 1, end - start - 1)
            : key;
    }

    /// <summary>
    /// CRC16/XMODEM over the UTF-8 bytes of the key, matching the Redis cluster specification.
    /// </summary>
    private static ushort Crc16(string value)
    {
        ushort crc = 0;

        foreach (var current in Encoding.UTF8.GetBytes(value))
        {
            crc ^= (ushort)(current << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ Polynomial : crc << 1);
        }

        return crc;
    }
}
