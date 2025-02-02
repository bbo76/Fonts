// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General.CMap
{
    public class Format0SubTableTests
    {
        [Fact]
        public void LoadFormat0()
        {
            var writer = new BigEndianBinaryWriter();

            // int subtableCount = 1;
            writer.WriteCMapSubTable(
                new Format0SubTable(
                    0,
                    PlatformIDs.Windows,
                    2,
                    new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

            BigEndianBinaryReader reader = writer.GetReader();
            ushort format = reader.ReadUInt16(); // read format before we pass along as that's what the cmap table does
            Assert.Equal(0, format);

            Format0SubTable table = Format0SubTable.Load(
                new[] { new EncodingRecord(PlatformIDs.Windows, 2, 0) },
                reader).Single();

            Assert.Equal(0, table.Language);
            Assert.Equal(PlatformIDs.Windows, table.Platform);
            Assert.Equal(2, table.Encoding);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, table.GlyphIds);
        }

        [Fact]
        public void GetCharacter()
        {
            var format = new Format0SubTable(0, PlatformIDs.Windows, 2, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            bool found = format.TryGetGlyphId(new CodePoint(4), out ushort id);

            Assert.True(found);
            Assert.Equal(5, id);
        }

        [Fact]
        public void GetCharacter_missing()
        {
            var format = new Format0SubTable(0, PlatformIDs.Windows, 2, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            bool found = format.TryGetGlyphId(new CodePoint(99), out ushort id);

            Assert.False(found);
            Assert.Equal(0, id);
        }
    }
}
