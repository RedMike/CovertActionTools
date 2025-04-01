using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a Crime, returns multiple assets to save:
    ///   * CRIMEx.DTA file (legacy)
    ///   * CRIMEx.json file (modern + metadata)
    /// </summary>
    public interface ICrimeExporter
    {
        IDictionary<string, byte[]> Export(CrimeModel crime);
    }
    
    internal class CrimeExporter : ICrimeExporter
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<CrimeExporter> _logger;

        public CrimeExporter(ILogger<CrimeExporter> logger)
        {
            _logger = logger;
        }

        public IDictionary<string, byte[]> Export(CrimeModel crime)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"CRIME{crime.Id}.DTA"] = GetLegacyCrimeData(crime),
                [$"CRIME{crime.Id}_crime.json"] = GetModernCrimeData(crime),
            };

            return dict;
        }

        private byte[] GetModernCrimeData(CrimeModel crime)
        {
            var json = JsonSerializer.Serialize(crime, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] GetLegacyCrimeData(CrimeModel crime)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            writer.Write((ushort)crime.Participants.Count);
            writer.Write((ushort)crime.Events.Count);

            foreach (var participant in crime.Participants)
            {
                writer.Write((ushort)0xFFFF);
                writer.Write((ushort)participant.Exposure);
                foreach (var c in participant.Role.Trim().Trim('\0').PadRight(32, (char)0))
                {
                    writer.Write(c);
                }

                writer.Write((ushort)participant.Unknown1);
                writer.Write((byte)participant.Unknown2);
                writer.Write((byte)participant.ParticipantType);
                writer.Write((byte)participant.Unknown3);
                writer.Write((ushort)participant.ClueType);
                writer.Write((ushort)participant.Rank);
                writer.Write((ushort)participant.Unknown4);
                writer.Write((byte)participant.Unknown5);
            }

            foreach (var ev in crime.Events)
            {
                writer.Write((ushort)ev.SourceParticipantId);
                writer.Write((ushort)0x0000);
                writer.Write((ushort)ev.MessageId);
                foreach (var c in ev.Description.Trim().Trim('\0').PadRight(32, (char)0))
                {
                    writer.Write(c);
                }

                writer.Write((byte)(ev.TargetParticipantId ?? 0));
                writer.Write((byte)ev.EventType);
                
                var receivedObjectBitmask = 0;
                for (var i = 0; i < 8; i++)
                {
                    if (ev.ReceivedObjectIds.Contains(i))
                    {
                        receivedObjectBitmask |= 1 << i;
                    }
                }
                writer.Write((byte)receivedObjectBitmask);
                
                var destroyedObjectBitmask = 0;
                for (var i = 0; i < 8; i++)
                {
                    if (ev.DestroyedObjectIds.Contains(i))
                    {
                        destroyedObjectBitmask |= 1 << i;
                    }
                }
                writer.Write((byte)destroyedObjectBitmask);

                writer.Write((ushort)ev.Score);
            }

            foreach (var obj in crime.Objects)
            {
                foreach (var c in obj.Name.Trim().Trim('\0').PadRight(16, (char)0))
                {
                    writer.Write(c);
                }

                writer.Write((byte)obj.PictureId);
                writer.Write((byte)0xFF);
            }

            //always exactly 4 objects, but some are blank
            for (var j = 0; j < 4 - crime.Objects.Count; j++)
            {
                //backfill a minimum number of items
                for (var i = 0; i < 16; i++)
                {
                    writer.Write((byte)0);
                }

                writer.Write((byte)0xFF); //object ID
                writer.Write((byte)0xFF); //marker
            }

            return memStream.ToArray();
        }
    }
}