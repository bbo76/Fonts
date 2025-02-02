// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        [Fact]
        public void FakeFontGetGlyph()
        {
            Font font = CreateFont("hello world");
            Glyph glyph = font.GetGlyphs(new CodePoint('h'), ColorFontSupport.None).First();
            Assert.NotEqual(default, glyph);
        }

        [Theory]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Left,
            0,
            10)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            0,
            -320)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            0,
            -155)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Left,
            -60,
            10)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            -60,
            -320)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            -60,
            -155)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Left,
            -30,
            10)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            -30,
            -320)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            -30,
            -155)]
        public void VerticalAlignmentTests(
            VerticalAlignment vertical,
            HorizontalAlignment horizontal,
            float top,
            float left)
        {
            string text = "hello world\nhello";
            Font font = CreateFont(text);

            var span = new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical
            };

            IReadOnlyList<GlyphLayout> glyphsToRender = TextLayout.GenerateLayout(text.AsSpan(), span);
            FontRectangle bound = TextMeasurer.GetBounds(glyphsToRender, span.Dpi);

            Assert.Equal(310, bound.Width, 3);
            Assert.Equal(40, bound.Height, 3);
            Assert.Equal(left, bound.Left, 3);
            Assert.Equal(top, bound.Top, 3);
        }

        [Fact]
        public unsafe void MeasureTextWithSpan()
        {
            Font font = CreateFont("hello");

            Span<char> text = stackalloc char[]
            {
                'h',
                'e',
                'l',
                'l',
                'o'
            };

            // 72 * emSize means 1pt = 1px
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(10, size.Height, 4);
            Assert.Equal(130, size.Width, 4);
        }

        [Theory]
        [InlineData("h", 10, 10)]
        [InlineData("he", 10, 40)]
        [InlineData("hel", 10, 70)]
        [InlineData("hello", 10, 130)]
        [InlineData("hello world", 10, 310)]
        [InlineData("hello world\nhello world", 40, 310)]
        [InlineData("hello\nworld", 40, 130)]
        public void MeasureText(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Fact]
        public void TryMeasureCharacterBounds()
        {
            string text = "a b\nc";
            GlyphBounds[] expectedGlyphMetrics =
            {
                new(new CodePoint('a'), new FontRectangle(10, 0, 10, 10)),
                new(new CodePoint(' '), new FontRectangle(40, 0, 30, 10)),
                new(new CodePoint('b'), new FontRectangle(70, 0, 10, 10)),
                new(new CodePoint('c'), new FontRectangle(10, 30, 10, 10)),
            };
            Font font = CreateFont(text);

            Assert.True(TextMeasurer.TryMeasureCharacterBounds(
                text.AsSpan(),
                new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor },
                out GlyphBounds[] glyphMetrics));

            // Newline should not be returned.
            Assert.Equal(text.Length - 1, glyphMetrics.Length);
            for (int i = 0; i < glyphMetrics.Length; i++)
            {
                GlyphBounds expected = expectedGlyphMetrics[i];
                GlyphBounds actual = glyphMetrics[i];
                Assert.Equal(expected.Codepoint, actual.Codepoint);

                // 4 dp as there is minor offset difference in the float values
                Assert.Equal(expected.Bounds.X, actual.Bounds.X, 4);
                Assert.Equal(expected.Bounds.Y, actual.Bounds.Y, 4);
                Assert.Equal(expected.Bounds.Height, actual.Bounds.Height, 4);
                Assert.Equal(expected.Bounds.Width, actual.Bounds.Width, 4);
            }
        }

        [Theory]
        [InlineData("hello world", 10, 310)]
        [InlineData(
            "hello world hello world hello world",
            70, // 30 actual line height * 2 + 10 actual height
            310)]
        [InlineData(// issue https://github.com/SixLabors/ImageSharp.Drawing/issues/115
            "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。",
            160, // 30 actual line height * 2 + 10 actual height
            310)]
        public void MeasureTextWordWrappingHorizontalTopBottom(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.HorizontalTopBottom
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 10, 310)]
        [InlineData(
            "hello world hello world hello world",
            70, // 30 actual line height * 2 + 10 actual height
            310)]
        [InlineData(// issue https://github.com/SixLabors/ImageSharp.Drawing/issues/115
            "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。",
            160, // 30 actual line height * 2 + 10 actual height
            310)]
        public void MeasureTextWordWrappingHorizontalBottomTop(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.HorizontalBottomTop
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 310, 30)]
        [InlineData("hello world hello world hello world", 310, 90)]
        [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 310, 160)]
        public void MeasureTextWordWrappingVerticalLeftRight(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalLeftRight
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 310, 30)]
        [InlineData("hello world hello world hello world", 310, 90)]
        [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 310, 160)]
        public void MeasureTextWordWrappingVerticalRightLeft(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalRightLeft
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

#if OS_WINDOWS
        [Theory]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.Normal, 134, 871)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakAll, 160, 400)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.KeepAll, 80, 700)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.Normal, 134, 871)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakAll, 160, 400)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.KeepAll, 80, 700)]
        public void MeasureTextWordBreak(string text, LayoutMode layoutMode, WordBreaking wordBreaking, float height, float width)
        {
            // Testing using Windows only to ensure that actual glyphs are rendered
            // against known physically tested values.
            FontFamily arial = SystemFonts.Get("Arial");
            FontFamily jhengHei = SystemFonts.Get("Microsoft JhengHei");

            Font font = arial.CreateFont(20);
            FontRectangle size = TextMeasurer.Measure(
                text,
                new TextOptions(font)
                {
                    WrappingLength = 400,
                    LayoutMode = layoutMode,
                    WordBreaking = wordBreaking,
                    FallbackFontFamilies = new[] { jhengHei }
                });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }
#endif

        [Theory]
        [InlineData("ab", 477, 1081, false)] // no kerning rules defined for lowercase ab so widths should stay the same
        [InlineData("ab", 477, 1081, true)]
        [InlineData("AB", 465, 1033, false)] // width changes between kerning enabled or not
        [InlineData("AB", 465, 654, true)]
        public void MeasureTextWithKerning(string text, float height, float width, bool applyKerning)
        {
            var c = new FontCollection();
            Font font = c.Add(TestFonts.SimpleFontFileData()).CreateFont(12);
            FontRectangle size = TextMeasurer.MeasureBounds(
                text,
                new TextOptions(new Font(font, 1))
                {
                    Dpi = font.FontMetrics.ScaleFactor,
                    KerningMode = applyKerning ? KerningMode.Normal : KerningMode.None,
                });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("a", 100, 100, 125, 452)]
        public void LayoutWithLocation(string text, float x, float y, float expectedX, float expectedY)
        {
            var c = new FontCollection();
            Font font = c.Add(TestFonts.SimpleFontFileData()).CreateFont(12);

            var glyphRenderer = new GlyphRenderer();
            var renderer = new TextRenderer(glyphRenderer);
            renderer.RenderText(
                text,
                new TextOptions(new Font(font, 1))
                {
                    Dpi = font.FontMetrics.ScaleFactor,
                    Origin = new Vector2(x, y)
                });

            Assert.Equal(expectedX, glyphRenderer.GlyphRects[0].Location.X, 2);
            Assert.Equal(expectedY, glyphRenderer.GlyphRects[0].Location.Y, 2);
        }

        // https://github.com/SixLabors/Fonts/issues/244
        [Fact]
        public void MeasureTextLeadingFraction()
        {
            FontCollection c = new();
            Font font = c.Add(TestFonts.SimpleFontFileData()).CreateFont(12);
            TextOptions textOptions = new(font);
            FontRectangle measurement = TextMeasurer.Measure("/ This will fail", textOptions);

            Assert.NotEqual(FontRectangle.Empty, measurement);
        }

        [Theory]
        [InlineData("hello world", 1)]
        [InlineData("hello world\nhello world", 2)]
        [InlineData("hello world\nhello world\nhello world", 3)]
        public void CountLines(string text, int usedLines)
        {
            Font font = CreateFont(text);
            int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(usedLines, count);
        }

        [Fact]
        public void CountLinesWithSpan()
        {
            Font font = CreateFont("hello\n!");

            Span<char> text = stackalloc char[]
            {
                'h',
                'e',
                'l',
                'l',
                'o',
                '\n',
                '!'
            };
            int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 25, 7)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 50, 7)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 100, 6)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 200, 6)]
        public void CountLinesWrappingLength(string text, int wrappingLength, int usedLines)
        {
            Font font = CreateFont(text);
            int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor, WrappingLength = wrappingLength });

            Assert.Equal(usedLines, count);
        }

        [Fact]
        public void BuildTextRuns_EmptyReturnsDefaultRun()
        {
            const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
            Font font = CreateFont(text);
            TextOptions options = new(font);

            IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

            Assert.True(runs.Count == 1);
            Assert.Equal(font, runs[0].Font);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(CodePoint.GetCodePointCount(text.AsSpan()), runs[0].End);
        }

        [Fact]
        public void BuildTextRuns_ReturnsCreatesInterimRuns()
        {
            const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
            Font font = CreateFont(text);
            Font font2 = CreateFont(text, 16);
            TextOptions options = new(font)
            {
                TextRuns = new List<TextRun>()
                {
                    new TextRun() { Start = 9, End = 23, Font = font2 },
                    new TextRun() { Start = 35, End = 54, Font = font2 },
                    new TextRun() { Start = 68, End = 70, Font = font2 },
                }
            };

            IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

            Assert.True(runs.Count == 7);

            Assert.Equal(0, runs[0].Start);
            Assert.Equal(9, runs[0].End);
            Assert.Equal(font, runs[0].Font);
            Assert.Equal(9, runs[0].Slice(text.AsSpan()).Length);

            Assert.Equal(9, runs[1].Start);
            Assert.Equal(23, runs[1].End);
            Assert.Equal(font2, runs[1].Font);
            Assert.Equal(14, runs[1].Slice(text.AsSpan()).Length);

            Assert.Equal(23, runs[2].Start);
            Assert.Equal(35, runs[2].End);
            Assert.Equal(font, runs[2].Font);
            Assert.Equal(12, runs[2].Slice(text.AsSpan()).Length);

            Assert.Equal(35, runs[3].Start);
            Assert.Equal(54, runs[3].End);
            Assert.Equal(font2, runs[3].Font);
            Assert.Equal(19, runs[3].Slice(text.AsSpan()).Length);

            Assert.Equal(54, runs[4].Start);
            Assert.Equal(68, runs[4].End);
            Assert.Equal(font, runs[4].Font);
            Assert.Equal(14, runs[4].Slice(text.AsSpan()).Length);

            Assert.Equal(68, runs[5].Start);
            Assert.Equal(70, runs[5].End);
            Assert.Equal(font2, runs[5].Font);
            Assert.Equal(2, runs[5].Slice(text.AsSpan()).Length);

            Assert.Equal(70, runs[6].Start);
            Assert.Equal(76, runs[6].End);
            Assert.Equal(font, runs[6].Font);
            Assert.Equal(6, runs[6].Slice(text.AsSpan()).Length);
        }

        [Fact]
        public void BuildTextRuns_PreventsOverlappingRun()
        {
            const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
            Font font = CreateFont(text);
            TextOptions options = new(font)
            {
                TextRuns = new List<TextRun>()
                {
                    new TextRun() { Start = 0, End = 23 },
                    new TextRun() { Start = 1, End = 76 },
                }
            };

            IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

            Assert.Equal(2, runs.Count);
            Assert.Equal(font, runs[0].Font);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(1, runs[0].End);

            Assert.Equal(font, runs[1].Font);
            Assert.Equal(1, runs[1].Start);
            Assert.Equal(76, runs[1].End);
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterCharacter_Horizontal(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterCharacter
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterDimensions(justifiedLine, options.Dpi, out GlyphBounds[] justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.Sum(x => x.Bounds.Width), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterDimensions(line, options.Dpi, out GlyphBounds[] characterBounds);

            // All but the last justified character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (i == characterBounds.Length - 1)
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Width, characterBounds[i].Bounds.Width);
                }
                else
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Width > characterBounds[i].Bounds.Width);
                }
            }
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterWord_Horizontal(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterWord
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterDimensions(justifiedLine, options.Dpi, out GlyphBounds[] justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.Sum(x => x.Bounds.Width), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterDimensions(line, options.Dpi, out GlyphBounds[] characterBounds);

            // All but the last justified whitespace character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (CodePoint.IsWhiteSpace(characterBounds[i].Codepoint) && i != characterBounds.Length - 1)
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Width > characterBounds[i].Bounds.Width);
                }
                else
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Width, characterBounds[i].Bounds.Width);
                }
            }
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterCharacter_Vertical(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                LayoutMode = LayoutMode.VerticalLeftRight,
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterCharacter
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterDimensions(justifiedLine, options.Dpi, out GlyphBounds[] justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.Sum(x => x.Bounds.Height), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterDimensions(line, options.Dpi, out GlyphBounds[] characterBounds);

            // All but the last justified character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (i == characterBounds.Length - 1)
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Height, characterBounds[i].Bounds.Height);
                }
                else
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Height > characterBounds[i].Bounds.Height);
                }
            }
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterWord_Vertical(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                LayoutMode = LayoutMode.VerticalLeftRight,
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterWord
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterDimensions(justifiedLine, options.Dpi, out GlyphBounds[] justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.Sum(x => x.Bounds.Height), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterDimensions(line, options.Dpi, out GlyphBounds[] characterBounds);

            // All but the last justified whitespace character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (CodePoint.IsWhiteSpace(characterBounds[i].Codepoint) && i != characterBounds.Length - 1)
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Height > characterBounds[i].Bounds.Height);
                }
                else
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Height, characterBounds[i].Bounds.Height);
                }
            }
        }

        private static IReadOnlyList<GlyphLayout> CollectFirstLine(IReadOnlyList<GlyphLayout> glyphs)
        {
            List<GlyphLayout> line = new()
            {
                glyphs[0]
            };

            for (int i = 1; i < glyphs.Count; i++)
            {
                if (glyphs[i].IsStartOfLine)
                {
                    break;
                }

                line.Add(glyphs[i]);
            }

            return line;
        }

#if OS_WINDOWS
        [Fact]
        public FontRectangle BenchmarkTest()
        {
            Font font = Arial;
            return TextMeasurer.Measure("The quick brown fox jumped over the lazy dog", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
        }

        private static readonly Font Arial = SystemFonts.CreateFont("Arial", 12);
#endif

        public static Font CreateFont(string text)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }

        public static Font CreateFont(string text, float pointSize)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, pointSize);
        }
    }
}
