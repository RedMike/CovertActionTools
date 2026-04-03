using System;
using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class ExecutableModel
    {
        #region Dead Zone
        /// <summary>
        /// Overlay manager stub code that occupies the start of the packed data region.
        /// These bytes survive EXEPACK in-place decompression and must be preserved verbatim.
        /// </summary>
        public byte[] DeadZone { get; set; } = Array.Empty<byte>();
        #endregion

        #region Payload
        /// <summary>
        /// Decompressed program payload after the dead zone.
        /// This is the modifiable program data that will later be split into structured fields.
        /// </summary>
        public byte[] RawPayloadData { get; set; } = Array.Empty<byte>();
        #endregion

        #region EXEPACK Metadata
        /// <summary>
        /// Original packed EXE's full MZ header (typically 512 bytes).
        /// Preserved so the publisher can reproduce a byte-identical packed EXE.
        /// </summary>
        public byte[] OriginalMzHeader { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// EXEPACK decompression stub code (bytes between the 16-byte EXEPACK header
        /// and the "Packed file is corrupt" error string).
        /// </summary>
        public byte[] ExepackStub { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Relocation table entries as a flat array: [seg0, off0, seg1, off1, ...].
        /// Each pair is a (segment, offset) relocation entry.
        /// </summary>
        public ushort[] Relocations { get; set; } = Array.Empty<ushort>();

        /// <summary>
        /// Real entry point code segment (from EXEPACK header, not the packed MZ header).
        /// </summary>
        public ushort EntryCS { get; set; }

        /// <summary>
        /// Real entry point instruction pointer (from EXEPACK header).
        /// </summary>
        public ushort EntryIP { get; set; }

        /// <summary>
        /// Real stack segment (from EXEPACK header).
        /// </summary>
        public ushort StackSS { get; set; }

        /// <summary>
        /// Real stack pointer (from EXEPACK header).
        /// </summary>
        public ushort StackSP { get; set; }
        #endregion

        public ExecutableModel Clone()
        {
            return new ExecutableModel()
            {
                DeadZone = DeadZone.ToArray(),
                RawPayloadData = RawPayloadData.ToArray(),
                OriginalMzHeader = OriginalMzHeader.ToArray(),
                ExepackStub = ExepackStub.ToArray(),
                Relocations = Relocations.ToArray(),
                EntryCS = EntryCS,
                EntryIP = EntryIP,
                StackSS = StackSS,
                StackSP = StackSP
            };
        }
    }
}
