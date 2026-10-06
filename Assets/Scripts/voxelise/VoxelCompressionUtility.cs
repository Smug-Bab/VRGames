using System.Collections.Generic;

public static class VoxelCompressionUtility
{
    [System.Serializable]
    public struct RLEToken
    {
        public ushort blockID;
        public ushort count;

        public RLEToken(ushort id, ushort cnt)
        {
            blockID = id;
            count = cnt;
        }
    }

    // Compresses a flat 4096 voxel array into a compact list of runs
    public static List<RLEToken> Compress(ushort[] rawData)
    {
        List<RLEToken> compressedData = new List<RLEToken>();
        if (rawData == null || rawData.Length == 0) return compressedData;

        ushort currentID = rawData[0];
        ushort currentCount = 1;

        for (int i = 1; i < rawData.Length; i++)
        {
            if (rawData[i] == currentID && currentCount < ushort.MaxValue)
            {
                currentCount++;
            }
            else
            {
                compressedData.Add(new RLEToken(currentID, currentCount));
                currentID = rawData[i];
                currentCount = 1;
            }
        }

        compressedData.Add(new RLEToken(currentID, currentCount));
        return compressedData;
    }

    // Decompresses RLE tokens back into the fast 4096 lookup array for runtime
    public static void Decompress(List<RLEToken> compressedData, ushort[] targetOutArray)
    {
        int index = 0;
        foreach (var token in compressedData)
        {
            for (int i = 0; i < token.count; i++)
            {
                if (index < targetOutArray.Length)
                {
                    targetOutArray[index++] = token.blockID;
                }
            }
        }
    }
}