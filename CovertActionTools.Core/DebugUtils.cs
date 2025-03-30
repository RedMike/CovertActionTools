using System.Linq;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core
{
    public static class DebugUtils
    {
        public static void LogDebugFirstBytes(this byte[] bytes, ILogger logger, int batchSize, int batchCount, int offset = 0)
        {
            for (var i = 0; i < batchCount; i++)
            {
                logger.LogError($"{i}: " + string.Join(" ", bytes.Skip(offset).Skip(i * batchSize).Take(batchSize).Select(x => $"{x:X2}")));
            }
        }
    }
}