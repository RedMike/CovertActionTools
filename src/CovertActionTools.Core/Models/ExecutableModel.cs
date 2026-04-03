using System;
using System.Linq;
using CovertActionTools.Core.Models.Executables;

namespace CovertActionTools.Core.Models
{
    // Pointer recomputation: all pointer arrays are now computed in ToBytes().
    //
    // TODO: Shared data sections (character names, clue relationship phrases) are duplicated
    // across multiple EXEs (TAC, FINAL, GAME, BUG all have the same 192 character names;
    // GAME and BUG share the same 40 clue phrases). These should be split into a shared
    // data segment model so that editing names in one EXE automatically updates all others.

    /// <summary>
    /// Model for an EXEPACK-compressed DOS executable.
    /// Data segment field boundaries and interpretations are based on reverse engineering
    /// and may not be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
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
        /// x86-16 machine code segment (everything between the dead zone and the data segment).
        /// </summary>
        public byte[] CodeSegment { get; set; } = Array.Empty<byte>();

        /// <summary>TAC.EXE data segment. Only populated when this model represents TAC.</summary>
        public TacDataSegment TacData { get; set; }

        /// <summary>FINAL.EXE data segment. Only populated when this model represents FINAL.</summary>
        public FinalDataSegment FinalData { get; set; }

        /// <summary>GAME.EXE data segment. Only populated when this model represents GAME.</summary>
        public GameDataSegment GameData { get; set; }

        /// <summary>BUG.EXE data segment. Only populated when this model represents BUG.</summary>
        public BugDataSegment BugData { get; set; }

        /// <summary>CHASE.EXE data segment. Only populated when this model represents CHASE.</summary>
        public ChaseDataSegment ChaseData { get; set; }

        /// <summary>CODE.EXE data segment. Only populated when this model represents CODE.</summary>
        public CodeExeDataSegment CodeData { get; set; }
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

        /// <summary>
        /// Reconstructs the data segment bytes from whichever per-EXE data model is populated.
        /// </summary>
        public byte[] GetDataSegmentBytes()
        {
            if (TacData != null) return TacData.ToBytes();
            if (FinalData != null) return FinalData.ToBytes();
            if (GameData != null) return GameData.ToBytes();
            if (BugData != null) return BugData.ToBytes();
            if (ChaseData != null) return ChaseData.ToBytes();
            if (CodeData != null) return CodeData.ToBytes();
            return Array.Empty<byte>();
        }

        public ExecutableModel Clone()
        {
            return new ExecutableModel()
            {
                DeadZone = DeadZone.ToArray(),
                CodeSegment = CodeSegment.ToArray(),
                TacData = TacData?.Clone(),
                FinalData = FinalData?.Clone(),
                GameData = GameData?.Clone(),
                BugData = BugData?.Clone(),
                ChaseData = ChaseData?.Clone(),
                CodeData = CodeData?.Clone(),
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
