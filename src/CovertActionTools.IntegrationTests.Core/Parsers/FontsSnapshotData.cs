namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Base64-encoded FONTS.CV binary data and expected parsed output captured from the current
/// (known-correct) implementation. Used by snapshot tests to detect regressions.
///
/// To regenerate: run FontsSnapshotGenerator and copy the output.
/// </summary>
public static class FontsSnapshotData
{
    /// <summary>
    /// Single font, chars A-C, 8px wide, 4 rows tall.
    /// Patterns: A=0xFF, B=0xAA, C=0x55.
    /// </summary>
    public const string SingleFont_Default_Binary = "AQAPAAgICEFDAQADAQIA/6pV/6pV/6pV/6pV";

    /// <summary>
    /// Expected RawVgaImageData for char 'A' from SingleFont_Default (all 0xFF = all color 15).
    /// </summary>
    public const string SingleFont_Default_CharA_Pixels = "Dw8PDw8PDw8PDw8PDw8PDw8PDw8PDw8PDw8PDw8PDw8=";

    /// <summary>
    /// Expected RawVgaImageData for char 'B' from SingleFont_Default (0xAA = alternating).
    /// </summary>
    public const string SingleFont_Default_CharB_Pixels = "DwAPAA8ADwAPAA8ADwAPAA8ADwAPAA8ADwAPAA8ADwA=";

    /// <summary>
    /// Expected RawVgaImageData for char 'C' from SingleFont_Default (0x55 = alternating).
    /// </summary>
    public const string SingleFont_Default_CharC_Pixels = "AA8ADwAPAA8ADwAPAA8ADwAPAA8ADwAPAA8ADwAPAA8=";

    /// <summary>
    /// Single font, chars A-C, widths 3/5/7, 4 rows tall, all 0xFF face data.
    /// </summary>
    public const string SingleFont_CustomWidths_Binary = "AQAPAAMFB0FDAQADAQIA////////////////";

    /// <summary>
    /// Expected RawVgaImageData for char 'A' from SingleFont_CustomWidths (width 3).
    /// </summary>
    public const string SingleFont_CustomWidths_CharA_Pixels = "Dw8PDw8PDw8PDw8P";
}
