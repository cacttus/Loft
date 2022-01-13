using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using SharpFont;
namespace PirateCraft
{
    class DebugChar
    {
        public char Char { get; set; }
        public float AdvanceX { get; set; }
        public float BearingX { get; set; }
        public float Width { get; set; }
        public float Underrun { get; set; }
        public float Overrun { get; set; }
        public float Kern { get; set; }
        public float RightEdge { get; set; }
        internal DebugChar(char c, float advanceX, float bearingX, float width)
        {
            this.Char = c; this.AdvanceX = advanceX; this.BearingX = bearingX; this.Width = width;
        }

        public override string ToString()
        {
            return string.Format("'{0}' {1,5:F0} {2,5:F0} {3,5:F0} {4,5:F0} {5,5:F0} {6,5:F0} {7,5:F0}",
                this.Char, this.AdvanceX, this.BearingX, this.Width, this.Underrun, this.Overrun,
                this.Kern, this.RightEdge);
        }
        public static void PrintHeader()
        {
            Debug.Print("    {1,5} {2,5} {3,5} {4,5} {5,5} {6,5} {7,5}",
                "", "adv", "bearing", "wid", "undrn", "ovrrn", "kern", "redge");
        }
    }

    public class Font
    {
        SharpFont.Library lib;
        SharpFont.Face FontFace;
        internal float Size { get { return _size; } set { SetSize(value); } }
        private float _size;
        internal void SetSize(float size)
        {
            _size = size;
            if (FontFace != null)
            {
                FontFace.SetCharSize(0, (int)size, 0, 96);
            }
        }
        public Font(string loc)
        {
            lib = new SharpFont.Library();
            FontFace = new SharpFont.Face(lib, loc);
            SetSize(8.25f);
        }

        /// <summary>
        /// Render the string into a bitmap with <see cref="SystemColors.ControlText"/> text color and a transparent background.
        /// </summary>
        /// <param name="text">The string to render.</param>
        public  Bitmap RenderString(string text)
        {
            try
            {
                return RenderString(this.lib, this.FontFace, text, SystemColors.ControlText, Color.Transparent);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Render the string into a bitmap with a transparent background.
        /// </summary>
        /// <param name="text">The string to render.</param>
        /// <param name="foreColor">The color of the text.</param>
        /// <returns></returns>
        public Bitmap RenderString(string text, Color foreColor)
        {
            return RenderString(this.lib, this.FontFace, text, foreColor, Color.Transparent);
        }

        /// <summary>
        /// Render the string into a bitmap with an opaque background.
        /// </summary>
        /// <param name="text">The string to render.</param>
        /// <param name="foreColor">The color of the text.</param>
        /// <param name="backColor">The color of the background behind the text.</param>
        /// <returns></returns>
        public Bitmap RenderString(string text, Color foreColor, Color backColor)
        {
            return RenderString(this.lib, this.FontFace, text, foreColor, backColor);
        }

        public static Bitmap RenderString(Library library, Face face, string text, Color foreColor, Color backColor)
        {
            var measuredChars = new List<DebugChar>();
            var renderedChars = new List<DebugChar>();
            float penX = 0, penY = 0;
            float stringWidth = 0; // the measured width of the string
            float stringHeight = 0; // the measured height of the string
            float overrun = 0;
            float underrun = 0;
            float kern = 0;
            int spacingError = 0;
            bool trackingUnderrun = true;
            int rightEdge = 0; // tracking rendered right side for debugging

            // Bottom and top are both positive for simplicity.
            // Drawing in .Net has 0,0 at the top left corner, with positive X to the right
            // and positive Y downward.
            // Glyph metrics have an origin typically on the left side and at baseline
            // of the visual data, but can draw parts of the glyph in any quadrant, and
            // even move the origin (via kerning).
            float top = 0, bottom = 0;

            // Measure the size of the string before rendering it. We need to do this so
            // we can create the proper size of bitmap (canvas) to draw the characters on.
            for (int i = 0; i < text.Length; i++)
            {
                #region Load character
                char c = text[i];

                // Look up the glyph index for this character.
                uint glyphIndex = face.GetCharIndex(c);

                // Load the glyph into the font's glyph slot. There is usually only one slot in the font.
                face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);

                // Refer to the diagram entitled "Glyph Metrics" at http://www.freetype.org/freetype2/docs/tutorial/step2.html.
                // There is also a glyph diagram included in this example (glyph-dims.svg).
                // The metrics below are for the glyph loaded in the slot.
                float gAdvanceX = (float)face.Glyph.Advance.X; // same as the advance in metrics
                float gBearingX = (float)face.Glyph.Metrics.HorizontalBearingX;
                float gWidth = (float)face.Glyph.Metrics.Width;//.ToSingle(); //**Not sure..?
                var rc = new DebugChar(c, gAdvanceX, gBearingX, gWidth);
                #endregion
                #region Underrun
                // Negative bearing would cause clipping of the first character
                // at the left boundary, if not accounted for.
                // A positive bearing would cause empty space.
                underrun += -(gBearingX);
                if (stringWidth == 0)
                    stringWidth += underrun;
                if (trackingUnderrun)
                    rc.Underrun = underrun;
                if (trackingUnderrun && underrun <= 0)
                {
                    underrun = 0;
                    trackingUnderrun = false;
                }
                #endregion
                #region Overrun
                // Accumulate overrun, which coould cause clipping at the right side of characters near
                // the end of the string (typically affects fonts with slanted characters)
                if (gBearingX + gWidth > 0 || gAdvanceX > 0)
                {
                    overrun -= Math.Max(gBearingX + gWidth, gAdvanceX);
                    if (overrun <= 0) overrun = 0;
                }
                overrun += (float)(gBearingX == 0 && gWidth == 0 ? 0 : gBearingX + gWidth - gAdvanceX);
                // On the last character, apply whatever overrun we have to the overall width.
                // Positive overrun prevents clipping, negative overrun prevents extra space.
                if (i == text.Length - 1)
                    stringWidth += overrun;
                rc.Overrun = overrun; // accumulating (per above)
                #endregion

                #region Top/Bottom
                // If this character goes higher or lower than any previous character, adjust
                // the overall height of the bitmap.
                float glyphTop = (float)face.Glyph.Metrics.HorizontalBearingY;
                float glyphBottom = (float)(face.Glyph.Metrics.Height - face.Glyph.Metrics.HorizontalBearingY);
                if (glyphTop > top)
                    top = glyphTop;
                if (glyphBottom > bottom)
                    bottom = glyphBottom;
                #endregion

                // Accumulate the distance between the origin of each character (simple width).
                stringWidth += gAdvanceX;
                rc.RightEdge = stringWidth;
                measuredChars.Add(rc);

                #region Kerning (for NEXT character)
                // Calculate kern for the NEXT character (if any)
                // The kern value adjusts the origin of the next character (positive or negative).
                if (face.HasKerning && i < text.Length - 1)
                {
                    char cNext = text[i + 1];
                    kern = (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
                    // sanity check for some fonts that have kern way out of whack
                    if (kern > gAdvanceX * 5 || kern < -(gAdvanceX * 5))
                        kern = 0;
                    rc.Kern = kern;
                    stringWidth += kern;
                }

                #endregion
            }

            stringHeight = top + bottom;

            // If any dimension is 0, we can't create a bitmap
            if (stringWidth == 0 || stringHeight == 0)
                return null;

            // Create a new bitmap that fits the string.
            Bitmap bmp = new Bitmap((int)Math.Ceiling(stringWidth), (int)Math.Ceiling(stringHeight));
            trackingUnderrun = true;
            underrun = 0;
            overrun = 0;
            stringWidth = 0;
            using (var g = Graphics.FromImage(bmp))
            {
                #region Set up graphics
                // HighQuality and GammaCorrected both specify gamma correction be applied (2.2 in sRGB)
                // https://msdn.microsoft.com/en-us/library/windows/desktop/ms534094(v=vs.85).aspx
                g.CompositingQuality = CompositingQuality.HighQuality;
                // HighQuality and AntiAlias both specify antialiasing
                g.SmoothingMode = SmoothingMode.HighQuality;
                // If a background color is specified, blend over it.
                g.CompositingMode = CompositingMode.SourceOver;

                g.Clear(backColor);
                #endregion

                // Draw the string into the bitmap.
                // A lot of this is a repeat of the measuring steps, but this time we have
                // an actual bitmap to work with (both canvas and bitmaps in the glyph slot).
                for (int i = 0; i < text.Length; i++)
                {
                    #region Load character
                    char c = text[i];

                    // Same as when we were measuring, except RenderGlyph() causes the glyph data
                    // to be converted to a bitmap.
                    uint glyphIndex = face.GetCharIndex(c);
                    face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
                    face.Glyph.RenderGlyph(RenderMode.Normal);
                    FTBitmap ftbmp = face.Glyph.Bitmap;

                    float gAdvanceX = (float)face.Glyph.Advance.X;
                    float gBearingX = (float)face.Glyph.Metrics.HorizontalBearingX;
                    float gWidth = (float)face.Glyph.Metrics.Width;

                    var rc = new DebugChar(c, gAdvanceX, gBearingX, gWidth);
                    #endregion
                    #region Underrun
                    // Underrun
                    underrun += -(gBearingX);
                    if (penX == 0)
                        penX += underrun;
                    if (trackingUnderrun)
                        rc.Underrun = underrun;
                    if (trackingUnderrun && underrun <= 0)
                    {
                        underrun = 0;
                        trackingUnderrun = false;
                    }
                    #endregion
                    #region Draw glyph
                    // Whitespace characters sometimes have a bitmap of zero size, but a non-zero advance.
                    // We can't draw a 0-size bitmap, but the pen position will still get advanced (below).
                    if ((ftbmp.Width > 0 && ftbmp.Rows > 0))
                    {
                        // Get a bitmap that .Net can draw (GDI+ in this case).
                        //**The argument isn't available in this version of .net
                        Bitmap cBmp = ftbmp.ToGdipBitmap(); // (foreColor);

                        ///////////////
                        ///////////////
                        ///////////////
                        //**TEST
                        //Bitmap bmpTest = new System.Drawing.Bitmap(ftbmp.Width,ftbmp.Rows,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                         
                        //var mode = ftbmp.PixelMode;//gray or monocrhome. monocrome is 1 bit gray is 1 byte
                        ////sign of pitch determines if rows are ascending or descending

                        //bmpTest.Save("./Test3.bmp");

                        //cBmp.Save("./bmps/"+ DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()+".bmp");
                        ///////////////
                        ///////////////
                        ///////////////
                        ///////////////


                        rc.Width = cBmp.Width;
                        rc.BearingX = face.Glyph.BitmapLeft;
                        int x = (int)Math.Round(penX + face.Glyph.BitmapLeft);
                        int y = (int)Math.Round(penY + top - (float)face.Glyph.Metrics.HorizontalBearingY);
                        //Not using g.DrawImage because some characters come out blurry/clipped. (Is this still true?)
                        g.DrawImageUnscaled(cBmp, x, y);
                        rc.Overrun = face.Glyph.BitmapLeft + cBmp.Width - gAdvanceX;
                        // Check if we are aligned properly on the right edge (for debugging)
                        rightEdge = Math.Max(rightEdge, x + cBmp.Width);
                        spacingError = bmp.Width - rightEdge;
                    }
                    else
                    {
                        rightEdge = (int)(penX + gAdvanceX);
                        spacingError = bmp.Width - rightEdge;
                    }
                    #endregion

                    #region Overrun
                    if (gBearingX + gWidth > 0 || gAdvanceX > 0)
                    {
                        overrun -= Math.Max(gBearingX + gWidth, gAdvanceX);
                        if (overrun <= 0) overrun = 0;
                    }
                    overrun += (float)(gBearingX == 0 && gWidth == 0 ? 0 : gBearingX + gWidth - gAdvanceX);
                    if (i == text.Length - 1) penX += overrun;
                    rc.Overrun = overrun;
                    #endregion

                    // Advance pen positions for drawing the next character.
                    penX += (float)face.Glyph.Advance.X; // same as Metrics.HorizontalAdvance?
                    penY += (float)face.Glyph.Advance.Y;

                    rc.RightEdge = penX;
                    spacingError = bmp.Width - (int)Math.Round(rc.RightEdge);
                    renderedChars.Add(rc);

                    #region Kerning (for NEXT character)
                    // Adjust for kerning between this character and the next.
                    if (face.HasKerning && i < text.Length - 1)
                    {
                        char cNext = text[i + 1];
                        kern = (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
                        if (kern > gAdvanceX * 5 || kern < -(gAdvanceX * 5))
                            kern = 0;
                        rc.Kern = kern;
                        penX += (float)kern;
                    }
                    #endregion

                }

            }
            bool printedHeader = false;
            if (spacingError != 0)
            {
                for (int i = 0; i < renderedChars.Count; i++)
                {
                    //if (measuredChars[i].RightEdge != renderedChars[i].RightEdge)
                    //{
                    if (!printedHeader)
                        DebugChar.PrintHeader();
                    printedHeader = true;
                    Debug.Print(measuredChars[i].ToString());
                    Debug.Print(renderedChars[i].ToString());
                    //}
                }
                string msg = string.Format("Right edge: {0,3} ({1}) {2}",
                    spacingError,
                    spacingError == 0 ? "perfect" : spacingError > 0 ? "space  " : "clipped",
                    face.FamilyName);
                System.Diagnostics.Debug.Print(msg);
                //throw new ApplicationException(msg);
            }
            return bmp;
        }

    }
}
