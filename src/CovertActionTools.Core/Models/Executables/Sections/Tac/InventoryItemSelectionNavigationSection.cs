using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Inventory item selection cursor navigation table (DS:0x2100).
    /// Each row is 8 bytes = 4 ushorts (Up/Down/Left/Right); the cell holds the
    /// target inventory slot index reached when that arrow key is pressed.
    /// The in-game table is always 12 rows; the section pads to at least 12
    /// rows on write so the downstream ragdoll coords stay aligned.
    /// </summary>
    public class InventoryItemSelectionNavigationSection : IExecutableSection
    {
        public const int MinimumRowCount = 12;
        public const int FieldsPerRow = 4;
        public const int RowSizeBytes = FieldsPerRow * 2;

        private static readonly MenuNavigationDirection[] FieldOrder =
        {
            MenuNavigationDirection.Up,
            MenuNavigationDirection.Down,
            MenuNavigationDirection.Left,
            MenuNavigationDirection.Right,
        };

        public Dictionary<int, Dictionary<MenuNavigationDirection, int>> Entries { get; set; }
            = new Dictionary<int, Dictionary<MenuNavigationDirection, int>>();

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Entries = new Dictionary<int, Dictionary<MenuNavigationDirection, int>>();
            for (var row = 0; row < MinimumRowCount; row++)
            {
                var rowOffset = startingOffset + row * RowSizeBytes;
                var rowEntries = new Dictionary<MenuNavigationDirection, int>();
                for (var field = 0; field < FieldsPerRow; field++)
                {
                    var value = BitConverter.ToUInt16(fullPayload, rowOffset + field * 2);
                    rowEntries[FieldOrder[field]] = value;
                }
                Entries[row] = rowEntries;
            }
            return MinimumRowCount * RowSizeBytes;
        }

        public byte[] WriteBytes()
        {
            var maxKey = Entries.Count == 0 ? -1 : Entries.Keys.Max();
            var rowCount = Math.Max(MinimumRowCount, maxKey + 1);
            var result = new byte[rowCount * RowSizeBytes];
            for (var row = 0; row < rowCount; row++)
            {
                Entries.TryGetValue(row, out var rowEntries);
                for (var field = 0; field < FieldsPerRow; field++)
                {
                    ushort value = 0;
                    if (rowEntries != null && rowEntries.TryGetValue(FieldOrder[field], out var v))
                    {
                        value = (ushort)v;
                    }
                    var pos = row * RowSizeBytes + field * 2;
                    result[pos] = (byte)(value & 0xFF);
                    result[pos + 1] = (byte)((value >> 8) & 0xFF);
                }
            }
            return result;
        }

        public InventoryItemSelectionNavigationSection Clone()
        {
            var clone = new InventoryItemSelectionNavigationSection
            {
                Entries = Entries.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ToDictionary(iv => iv.Key, iv => iv.Value))
            };
            return clone;
        }
    }
}
