using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace TimeTableApp
{
    public static class TimetableRenderer
    {
        private const float VisualScale = 1.00f;
        private static readonly Color CBlack = Color.FromArgb(0, 0, 0);
        private static readonly Color CGreen = Color.FromArgb(31, 122, 31);
        private static readonly Color COrange = Color.FromArgb(192, 87, 0);
        private static readonly Color CBlue = Color.FromArgb(26, 85, 199);
        private static readonly Color CRed = Color.FromArgb(176, 0, 0);
        private static readonly object FontInitLock = new object();
        private static readonly PrivateFontCollection PrivateFonts = new PrivateFontCollection();
        private static bool FontsLoaded;

        public static Bitmap RenderSample(int width, int height)
        {
            return Render(TimetableData.CreateDefault(), width, height);
        }

        public static Bitmap Render(TimetableData data, int width, int height)
        {
            if (data == null)
            {
                data = TimetableData.CreateDefault();
            }
            float contentTextScale = NormalizeContentTextScale(data.ContentTextScale);

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                RectangleF table = new RectangleF(
                    width * 0.015f,
                    height * 0.085f,
                    width * 0.97f,
                    height * 0.83f);

                GraphicsState state = g.Save();
                if (Math.Abs(VisualScale - 1f) > 0.0001f)
                {
                    float cx = table.Left + table.Width * 0.5f;
                    float cy = table.Top + table.Height * 0.5f;
                    g.TranslateTransform(cx, cy);
                    g.ScaleTransform(VisualScale, VisualScale);
                    g.TranslateTransform(-cx, -cy);
                }

                float lwOuter = Math.Max(1.5f, table.Width * 0.0023f);
                float lwThin = Math.Max(1f, lwOuter * 0.50f);

                float headerH = table.Height * 0.058f;
                float infoH = table.Height * 0.165f;
                float timeH = table.Height * 0.165f;
                float info2H = table.Height * 0.165f;
                float time2H = table.Height * 0.165f;
                float bottomH = table.Height - (headerH + infoH + timeH + info2H + time2H);

                float y = table.Top;
                RectangleF header = new RectangleF(table.Left, y, table.Width, headerH); y += headerH;
                RectangleF topInfoBand = new RectangleF(table.Left, y, table.Width, infoH); y += infoH;
                RectangleF topTimeBand = new RectangleF(table.Left, y, table.Width, timeH); y += timeH;
                RectangleF midInfoBand = new RectangleF(table.Left, y, table.Width, info2H); y += info2H;
                RectangleF midTimeBand = new RectangleF(table.Left, y, table.Width, time2H); y += time2H;
                RectangleF bottomBand = new RectangleF(table.Left, y, table.Width, bottomH);

                float rightW = bottomBand.Width * 0.415f;
                RectangleF leftBottom = new RectangleF(bottomBand.Left, bottomBand.Top, bottomBand.Width - rightW, bottomBand.Height);
                RectangleF rightBottom = new RectangleF(leftBottom.Right, bottomBand.Top, rightW, bottomBand.Height);
                RectangleF leftBottomTop = new RectangleF(leftBottom.Left, leftBottom.Top, leftBottom.Width, leftBottom.Height * 0.5f);
                RectangleF leftBottomBottom = new RectangleF(leftBottom.Left, leftBottomTop.Bottom, leftBottom.Width, leftBottom.Height - leftBottomTop.Height);

                using (Pen pOuter = new Pen(CBlack, lwOuter))
                using (Pen pThin = new Pen(CBlack, lwThin))
                {
                    g.DrawRectangle(pOuter, table.X, table.Y, table.Width, table.Height);
                    g.DrawLine(pThin, table.Left, header.Bottom, table.Right, header.Bottom);
                    g.DrawLine(pThin, table.Left, topInfoBand.Bottom, table.Right, topInfoBand.Bottom);
                    g.DrawLine(pThin, table.Left, topTimeBand.Bottom, table.Right, topTimeBand.Bottom);
                    g.DrawLine(pThin, table.Left, midInfoBand.Bottom, table.Right, midInfoBand.Bottom);
                    g.DrawLine(pThin, table.Left, midTimeBand.Bottom, table.Right, midTimeBand.Bottom);
                    g.DrawLine(pThin, leftBottom.Right, leftBottom.Top, leftBottom.Right, leftBottom.Bottom);
                    g.DrawLine(pThin, leftBottom.Left, leftBottomTop.Bottom, leftBottom.Right, leftBottomTop.Bottom);
                }

                DrawHeader(g, header, data);

                float labelW = table.Width * 0.06f;
                string badge3 = string.IsNullOrWhiteSpace(data.Platform3Label) ? "③" : data.Platform3Label;
                string badge4 = string.IsNullOrWhiteSpace(data.Platform4Label) ? "④" : data.Platform4Label;
                int topInfoCount = Math.Max(1, SafeLength(data.TopInfo));
                int topTimeCount = Math.Max(1, SafeLength(data.TopTimes) + CountOverlaySlots(data.TopOverlayHour));
                int midInfoCount = Math.Max(1, SafeLength(data.MidInfo));
                int midTimeCount = Math.Max(1, SafeLength(data.MidTimes) + CountOverlaySlots(data.MidOverlayHour));
                int bottomInfoCount = Math.Max(1, SafeLength(data.BottomInfo));
                int bottomTimeCount = Math.Max(1, SafeLength(data.BottomTimes));
                DrawInfoBand(g, topInfoBand, labelW, topInfoCount, data.TopInfo, badge4, contentTextScale);
                DrawTimeBand(g, topTimeBand, labelW, topTimeCount, data.TopTimes, badge3, data.TopLeftHour, data.TopOverlayColumn, data.TopOverlayHour, contentTextScale);
                DrawInfoBand(g, midInfoBand, labelW, midInfoCount, data.MidInfo, badge4, contentTextScale);
                DrawTimeBand(g, midTimeBand, labelW, midTimeCount, data.MidTimes, badge3, data.MidLeftHour, data.MidOverlayColumn, data.MidOverlayHour, contentTextScale);

                float miniLabelW = leftBottom.Width * 0.11f;

                if (data.DrawBottomVerticalLines)
                {
                    DrawMiniBandGrid(g, leftBottom, miniLabelW, lwThin, Math.Max(bottomInfoCount, bottomTimeCount));
                }

                DrawInfoBand(g, leftBottomTop, miniLabelW, bottomInfoCount, data.BottomInfo, badge4, contentTextScale);
                DrawTimeBand(g, leftBottomBottom, miniLabelW, bottomTimeCount, data.BottomTimes, badge3, string.Empty, -1, string.Empty, contentTextScale);
                DrawRightNotes(g, rightBottom, data.RemarksText, contentTextScale);
                g.Restore(state);
            }

            return bmp;
        }

        private static void DrawHeader(Graphics g, RectangleF band, TimetableData data)
        {
            using (Font fStation = FontFor(band.Height * 0.70f, FontStyle.Bold))
            using (Font fSub = FontFor(band.Height * 0.45f, FontStyle.Regular))
            using (Font fDate = FontFor(band.Height * 0.60f, FontStyle.Regular))
            using (Brush bBlack = new SolidBrush(CBlack))
            using (Brush bWhite = new SolidBrush(Color.White))
            {
                float x = band.Left + band.Width * 0.018f;
                float y = band.Top + band.Height * 0.10f;
                g.DrawString(data.StationName ?? string.Empty, fStation, bBlack, x, y);
                g.DrawString(data.SubTitle ?? string.Empty, fSub, bBlack, x + band.Width * 0.10f, y + band.Height * 0.05f);

                RectangleF box = new RectangleF(band.Left + band.Width * 0.60f, band.Top + band.Height * 0.10f, band.Width * 0.10f, band.Height * 0.80f);
                g.FillRectangle(Brushes.Black, box);
                using (Font fService = FontFor(band.Height * 0.50f, FontStyle.Bold))
                {
                    DrawCenter(g, data.ServiceLabel ?? string.Empty, fService, bWhite, box);
                }

                StringFormat right = new StringFormat();
                right.Alignment = StringAlignment.Far;
                right.LineAlignment = StringAlignment.Center;
                g.DrawString(data.RevisedDate ?? string.Empty, fDate, bBlack, new RectangleF(band.Left, band.Top, band.Width - band.Width * 0.02f, band.Height), right);
                right.Dispose();
            }
        }

        private static void DrawInfoBand(Graphics g, RectangleF band, float labelW, int count, TimetableTrainInfoRow[] rows, string badge, float contentTextScale)
        {
            DrawBadge(g, new RectangleF(band.Left, band.Top, labelW, band.Height), badge, contentTextScale);

            RectangleF[] cols = BuildCols(band, labelW, count);
            using (Font fCode = FontFor(band.Height * 0.16f * contentTextScale, FontStyle.Bold))
            using (Font fNo = FontFor(band.Height * 0.19f * contentTextScale, FontStyle.Regular))
            using (Font fType = FontFor(band.Height * 0.22f * contentTextScale, FontStyle.Bold))
            using (Font fDst = FontFor(band.Height * 0.18f * contentTextScale, FontStyle.Regular))
            using (Brush bBlack = new SolidBrush(CBlack))
            {
                for (int i = 0; i < count; i++)
                {
                    TimetableTrainInfoRow row = GetInfo(rows, i);
                    RectangleF c = cols[i];
                    float y = c.Top + band.Height * 0.01f;
                    DrawInfoTimeCode(g, new RectangleF(c.Left, y, c.Width, band.Height * 0.2f), row.Code, ParseColor(row.CodeColor, COrange), fCode, contentTextScale);

                    y += band.Height * 0.20f;
                    DrawCenterNoEllipsis(g, row.TrainNo, fNo, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.18f), 0.10f);
                    y += band.Height * 0.18f;
                    DrawCenter(g, row.TrainType, fType, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.22f));
                    y += band.Height * 0.22f;
                    DrawCenterNoEllipsis(g, row.Destination, fDst, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.26f), 0.10f);
                }
            }
        }

        private static void DrawInfoTimeCode(Graphics g, RectangleF rect, string raw, Color color, Font fallbackFont, float contentTextScale)
        {
            string digits = NormalizeInfoCodeTime4(raw);
            using (Brush b = new SolidBrush(color))
            {
                if (digits.Length == 0)
                {
                    DrawCenter(g, raw, fallbackFont, b, rect);
                    return;
                }

                string mm = digits.Substring(0, 2);
                string ss = digits.Substring(2, 2);
                using (Font fMM = FontFor(rect.Height * 0.90f * contentTextScale, FontStyle.Bold))
                using (Font fSS = FontFor(rect.Height * 0.62f * contentTextScale, FontStyle.Bold))
                using (Font fParen = FontFor(rect.Height * 0.78f * contentTextScale, FontStyle.Bold))
                {
                    SizeF sL = g.MeasureString("(", fParen, 120, StringFormat.GenericTypographic);
                    SizeF sMM = g.MeasureString(mm, fMM, 120, StringFormat.GenericTypographic);
                    SizeF sSS = g.MeasureString(ss, fSS, 120, StringFormat.GenericTypographic);
                    SizeF sR = g.MeasureString(")", fParen, 120, StringFormat.GenericTypographic);
                    float total = sL.Width + sMM.Width + sSS.Width + sR.Width;
                    float x = rect.Left + (rect.Width - total) * 0.5f;
                    float yMM = rect.Top + rect.Height * 0.03f;
                    float ySS = yMM + (sMM.Height - sSS.Height) + rect.Height * 0.01f;
                    float yP = yMM + (sMM.Height - sL.Height) * 0.55f;

                    g.DrawString("(", fParen, b, x, yP, StringFormat.GenericTypographic);
                    x += sL.Width;
                    g.DrawString(mm, fMM, b, x, yMM, StringFormat.GenericTypographic);
                    x += sMM.Width;
                    g.DrawString(ss, fSS, b, x, ySS, StringFormat.GenericTypographic);
                    x += sSS.Width;
                    g.DrawString(")", fParen, b, x, yP, StringFormat.GenericTypographic);
                }
            }
        }

        private static void DrawTimeBand(Graphics g, RectangleF band, float labelW, int count, TimetableTimeRow[] rows, string badge, string leftHour, int overlayCol, string overlayHour, float contentTextScale)
        {
            DrawBadge(g, new RectangleF(band.Left, band.Top, labelW, band.Height), badge, contentTextScale);

            bool hasLeftHour = !string.IsNullOrWhiteSpace(leftHour);
            bool hasOverlayHour = overlayCol >= 0 && !string.IsNullOrEmpty(overlayHour);
            RectangleF[] cols = BuildCols(band, labelW, count);
            hasOverlayHour = hasOverlayHour && overlayCol < cols.Length;

            if (hasLeftHour)
            {
                DrawHourLabelInSlot(g, band, cols, -1, leftHour, contentTextScale);
            }

            if (hasOverlayHour)
            {
                DrawHourLabelInSlot(g, band, cols, overlayCol, overlayHour, contentTextScale);
            }

            using (Font fMM = FontFor(band.Height * 0.40f * contentTextScale, FontStyle.Bold))
            using (Font fSS = FontFor(band.Height * 0.23f * contentTextScale, FontStyle.Bold))
            using (Font fMid = FontFor(band.Height * 0.19f * contentTextScale, FontStyle.Regular))
            using (Font fNote = FontFor(band.Height * 0.15f * contentTextScale, FontStyle.Regular))
            using (Brush bBlack = new SolidBrush(CBlack))
            {
                int timeRowIndex = 0;
                for (int i = 0; i < count; i++)
                {
                    bool reservedByOverlay = hasOverlayHour && i == overlayCol;
                    if (reservedByOverlay)
                    {
                        continue;
                    }

                    TimetableTimeRow row = GetTime(rows, timeRowIndex);
                    timeRowIndex++;
                    RectangleF c = cols[i];
                    DrawTime(g, c, band, row.Time4, fMM, fSS, row.Highlight ? CGreen : CBlack);
                    DrawCenter(g, row.TrainNo, fMid, bBlack, new RectangleF(c.Left, c.Top + band.Height * 0.47f, c.Width, band.Height * 0.18f));
                    DrawCenterNoEllipsis(g, row.TypeAndDestination, fMid, bBlack, new RectangleF(c.Left, c.Top + band.Height * 0.64f, c.Width, band.Height * 0.19f), 0.10f);

                    if (!string.IsNullOrEmpty(row.NoteBlue) || !string.IsNullOrEmpty(row.NoteRed) || row.UreSeat)
                    {
                        float noteY = c.Bottom - fNote.Height - band.Height * 0.02f;
                        DrawNote(g, c, noteY, fNote, row.NoteBlue, row.NoteRed, row.UreSeat);
                    }
                }
            }
        }

        private static string NormalizeInfoCodeTime4(string value)
        {
            string digits = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (char.IsDigit(value[i]))
                    {
                        digits += value[i];
                    }
                }
            }

            if (digits.Length == 0)
            {
                return string.Empty;
            }

            if (digits.Length == 1)
            {
                return "0" + digits + "00";
            }

            if (digits.Length == 2)
            {
                return digits + "00";
            }

            if (digits.Length == 3)
            {
                return digits.PadLeft(4, '0');
            }

            string d4 = digits.Substring(0, 4);
            // Backward-compat: old bug saved "57" as "0057".
            if (d4.StartsWith("00", StringComparison.Ordinal) && d4.Substring(2, 2) != "00")
            {
                return d4.Substring(2, 2) + "00";
            }

            return d4;
        }

        private static void DrawHourLabelInSlot(Graphics g, RectangleF band, RectangleF[] cols, int slot, string text, float contentTextScale)
        {
            if (cols == null || cols.Length == 0 || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            float colW = cols[0].Width;
            float x;
            if (slot < 0)
            {
                float leftPad = band.Width * 0.004f;
                float leftAreaL = band.Left + leftPad;
                float leftAreaR = cols[0].Left - leftPad;
                float leftAreaW = Math.Max(1f, leftAreaR - leftAreaL);
                float desiredW = cols[0].Width * 0.5f;
                colW = Math.Min(desiredW, leftAreaW * 0.92f);
                x = leftAreaL + (leftAreaW - colW) * 0.5f;
            }
            else
            {
                int col = slot;
                if (col < 0)
                {
                    col = 0;
                }
                if (col >= cols.Length)
                {
                    col = cols.Length - 1;
                }

                x = cols[col].Left;
                colW = cols[col].Width;
            }

            float h = band.Height * 0.22f;
            float baselineBottom = band.Top + band.Height * 0.46f;
            float y = baselineBottom - h;
            DrawHourText(g, text, new RectangleF(x, y, colW, h), band.Height, contentTextScale);
        }

        private static void DrawHourText(Graphics g, string text, RectangleF rect, float bandHeight, float contentTextScale)
        {
            string value = text ?? string.Empty;
            using (Brush b = new SolidBrush(CBlack))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags = StringFormatFlags.NoWrap;

                float size = Math.Max(6f, bandHeight * 0.20f * contentTextScale);
                float min = 3f;
                Font picked = null;
                for (int i = 0; i < 36; i++)
                {
                    Font candidate = FontFor(size, FontStyle.Bold);
                    SizeF measured = g.MeasureString(value, candidate, 1200, StringFormat.GenericTypographic);
                    if (measured.Width <= rect.Width * 0.98f && measured.Height <= rect.Height * 0.98f)
                    {
                        picked = candidate;
                        break;
                    }

                    candidate.Dispose();
                    size *= 0.90f;
                    if (size < min)
                    {
                        break;
                    }
                }

                if (picked == null)
                {
                    picked = FontFor(min, FontStyle.Bold);
                }

                using (picked)
                {
                    g.DrawString(value, picked, b, rect, sf);
                }
            }
        }

        private static int CountOverlaySlots(string overlayHour)
        {
            return string.IsNullOrWhiteSpace(overlayHour) ? 0 : 1;
        }

        private static void DrawMiniBandGrid(Graphics g, RectangleF band, float labelW, float lwThin, int count)
        {
            int columnCount = Math.Max(1, count);
            using (Pen p = new Pen(CBlack, lwThin))
            {
                float x0 = band.Left + labelW;
                g.DrawLine(p, x0, band.Top, x0, band.Bottom);
                float cw = (band.Width - labelW) / columnCount;
                for (int i = 1; i < columnCount; i++)
                {
                    float x = x0 + cw * i;
                    g.DrawLine(p, x, band.Top, x, band.Bottom);
                }
            }
        }

        private static int SafeLength<T>(T[] rows)
        {
            return rows == null ? 0 : rows.Length;
        }

        private static void DrawRightNotes(Graphics g, RectangleF right, string remarksText, float contentTextScale)
        {
            string text = (remarksText ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            RectangleF body = new RectangleF(
                right.Left + right.Width * 0.008f,
                right.Top + right.Height * 0.02f,
                right.Width * 0.984f,
                right.Height * 0.965f);

            string[] lines = text.Split('\n');
            if (lines.Length == 0)
            {
                return;
            }
            MarkupRun[][] markupLines = ParseMarkupLines(lines);

            using (Font baseFont = FontFor(right.Height * 0.079f * contentTextScale, FontStyle.Regular))
            using (Font fitted = CreateFittedFontNoWrapLines(g, markupLines, baseFont, body, 0.30f, 3.00f))
            {
                float lineHeight = MeasureTightLineHeight(g, fitted);
                float totalTextHeight = lineHeight * markupLines.Length;
                float extraGap = 0f;
                if (markupLines.Length > 1 && totalTextHeight < body.Height)
                {
                    extraGap = (body.Height - totalTextHeight) / (markupLines.Length - 1);
                }

                float y = body.Top;
                for (int i = 0; i < markupLines.Length; i++)
                {
                    MarkupRun[] runs = markupLines[i];
                    float x = body.Left;
                    for (int j = 0; j < runs.Length; j++)
                    {
                        string runText = runs[j].Text ?? string.Empty;
                        if (runText.Length == 0)
                        {
                            continue;
                        }

                        using (Brush b = new SolidBrush(runs[j].Color))
                        {
                            g.DrawString(runText, fitted, b, x, y, StringFormat.GenericTypographic);
                        }
                        x += g.MeasureString(runText, fitted, 2000, StringFormat.GenericTypographic).Width;
                    }

                    y += lineHeight;
                    if (i < markupLines.Length - 1)
                    {
                        y += extraGap;
                    }
                }
            }
        }

        private static bool FitsNoWrapLines(Graphics g, MarkupRun[][] lines, Font font, RectangleF rect)
        {
            if (lines == null || lines.Length == 0)
            {
                return true;
            }

            if (rect.Width <= 1f || rect.Height <= 1f)
            {
                return false;
            }

            float lineHeight = MeasureTightLineHeight(g, font);
            float totalHeight = lineHeight * lines.Length;
            if (totalHeight > rect.Height * 0.999f)
            {
                return false;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                float w = MeasureMarkupLineWidth(g, lines[i], font);
                if (w > rect.Width)
                {
                    return false;
                }
            }

            return true;
        }

        private static Font CreateFittedFontNoWrapLines(Graphics g, MarkupRun[][] lines, Font baseFont, RectangleF rect, float minScale, float maxScale)
        {
            float minSize = Math.Max(3f, baseFont.Size * minScale);
            float maxSize = Math.Max(minSize, baseFont.Size * maxScale);
            float low = minSize;
            float high = maxSize;

            Font best = null;
            for (int i = 0; i < 22; i++)
            {
                float size = (low + high) * 0.5f;
                Font candidate = null;
                try
                {
                    candidate = new Font(baseFont.FontFamily, size, baseFont.Style, baseFont.Unit);
                    if (FitsNoWrapLines(g, lines, candidate, rect))
                    {
                        if (best != null)
                        {
                            best.Dispose();
                        }
                        best = (Font)candidate.Clone();
                        low = size;
                    }
                    else
                    {
                        high = size;
                    }
                }
                catch
                {
                    high = size;
                }
                finally
                {
                    if (candidate != null)
                    {
                        candidate.Dispose();
                    }
                }
            }

            if (best != null)
            {
                return best;
            }

            try
            {
                return new Font(baseFont.FontFamily, minSize, baseFont.Style, baseFont.Unit);
            }
            catch
            {
                return (Font)baseFont.Clone();
            }
        }

        private static MarkupRun[][] ParseMarkupLines(string[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                return new MarkupRun[0][];
            }

            MarkupRun[][] parsed = new MarkupRun[lines.Length][];
            for (int i = 0; i < lines.Length; i++)
            {
                parsed[i] = ParseMarkupLine(lines[i]);
            }

            return parsed;
        }

        private static MarkupRun[] ParseMarkupLine(string line)
        {
            string s = line ?? string.Empty;
            List<MarkupRun> runs = new List<MarkupRun>();
            int i = 0;
            while (i < s.Length)
            {
                int open = s.IndexOf('{', i);
                if (open < 0)
                {
                    AddMarkupRun(runs, s.Substring(i), CBlack);
                    break;
                }

                if (open > i)
                {
                    AddMarkupRun(runs, s.Substring(i, open - i), CBlack);
                }

                int bar = s.IndexOf('|', open + 1);
                if (bar < 0)
                {
                    AddMarkupRun(runs, s.Substring(open), CBlack);
                    break;
                }

                int close = s.IndexOf('}', bar + 1);
                if (close < 0)
                {
                    AddMarkupRun(runs, s.Substring(open), CBlack);
                    break;
                }

                string colorToken = s.Substring(open + 1, bar - open - 1).Trim();
                string content = s.Substring(bar + 1, close - bar - 1);
                Color color;
                if (TryGetMarkupColor(colorToken, out color))
                {
                    AddMarkupRun(runs, content, color);
                }
                else
                {
                    AddMarkupRun(runs, s.Substring(open, close - open + 1), CBlack);
                }

                i = close + 1;
            }

            if (runs.Count == 0)
            {
                runs.Add(new MarkupRun { Text = string.Empty, Color = CBlack });
            }

            return runs.ToArray();
        }

        private static void AddMarkupRun(List<MarkupRun> runs, string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (runs.Count > 0 && runs[runs.Count - 1].Color.ToArgb() == color.ToArgb())
            {
                runs[runs.Count - 1].Text += text;
                return;
            }

            runs.Add(new MarkupRun { Text = text, Color = color });
        }

        private static bool TryGetMarkupColor(string token, out Color color)
        {
            string t = (token ?? string.Empty).Trim().ToLowerInvariant();
            switch (t)
            {
                case "blue":
                case "b":
                case "青":
                    color = CBlue;
                    return true;
                case "red":
                case "r":
                case "赤":
                    color = CRed;
                    return true;
                case "green":
                case "g":
                case "緑":
                    color = CGreen;
                    return true;
                case "orange":
                case "o":
                case "橙":
                case "オレンジ":
                    color = COrange;
                    return true;
                default:
                    color = CBlack;
                    return false;
            }
        }

        private static float MeasureMarkupLineWidth(Graphics g, MarkupRun[] runs, Font font)
        {
            if (runs == null || runs.Length == 0)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < runs.Length; i++)
            {
                string t = runs[i].Text ?? string.Empty;
                if (t.Length == 0)
                {
                    continue;
                }

                total += g.MeasureString(t, font, 2000, StringFormat.GenericTypographic).Width;
            }

            return total;
        }

        private static float MeasureTightLineHeight(Graphics g, Font font)
        {
            try
            {
                SizeF s = g.MeasureString("あ", font, 200, StringFormat.GenericTypographic);
                return Math.Max(1f, s.Height * 1.02f);
            }
            catch
            {
                return Math.Max(1f, font.GetHeight(g));
            }
        }

        private sealed class MarkupRun
        {
            public string Text;
            public Color Color;
        }

        private static float DrawRunLine(Graphics g, float x, float y, Font f, string t1, Color c1)
        {
            using (Brush b1 = new SolidBrush(c1))
            {
                g.DrawString(t1, f, b1, x, y);
            }

            return y + f.Height + 1f;
        }

        private static float DrawRunLine(Graphics g, float x, float y, Font f, string t1, Color c1, string t2, Color c2, string t3, Color c3)
        {
            return DrawRunLine(g, x, y, f, t1, c1, t2, c2, t3, c3, string.Empty, CBlack);
        }

        private static float DrawRunLine(Graphics g, float x, float y, Font f, string t1, Color c1, string t2, Color c2, string t3, Color c3, string t4, Color c4)
        {
            float cx = x;
            cx = DrawRun(g, cx, y, f, t1, c1);
            cx = DrawRun(g, cx, y, f, t2, c2);
            cx = DrawRun(g, cx, y, f, t3, c3);
            DrawRun(g, cx, y, f, t4, c4);
            return y + f.Height + 1f;
        }

        private static float DrawRun(Graphics g, float x, float y, Font f, string text, Color c)
        {
            using (Brush b = new SolidBrush(c))
            {
                g.DrawString(text ?? string.Empty, f, b, x, y);
            }

            SizeF s = g.MeasureString(text ?? string.Empty, f, 1200, StringFormat.GenericTypographic);
            return x + s.Width;
        }

        private static void DrawNote(Graphics g, RectangleF c, float y, Font f, string blue, string red, bool ureSeat)
        {
            string ure = ureSeat ? "(うれしート)" : string.Empty;
            bool hasBlue = !string.IsNullOrEmpty(blue);
            bool hasRed = !string.IsNullOrEmpty(red);
            bool hasUre = !string.IsNullOrEmpty(ure);
            if (!hasBlue && !hasRed && !hasUre)
            {
                return;
            }

            float wBlue = 0f;
            float wRed = 0f;
            float wUre = 0f;
            int partCount = 0;
            if (hasBlue)
            {
                wBlue = g.MeasureString(blue, f, 500, StringFormat.GenericTypographic).Width;
                partCount++;
            }
            if (hasRed)
            {
                wRed = g.MeasureString(red, f, 500, StringFormat.GenericTypographic).Width;
                partCount++;
            }
            if (hasUre)
            {
                wUre = g.MeasureString(ure, f, 500, StringFormat.GenericTypographic).Width;
                partCount++;
            }

            float gap = 2f;
            float total = wBlue + wRed + wUre + gap * Math.Max(0, partCount - 1);
            float x = c.Left + (c.Width - total) * 0.5f;
            using (Brush bb = new SolidBrush(CBlue))
            using (Brush br = new SolidBrush(CRed))
            using (Brush bo = new SolidBrush(COrange))
            {
                bool hasPrev = false;
                if (hasBlue)
                {
                    g.DrawString(blue, f, bb, x, y, StringFormat.GenericTypographic);
                    x += wBlue;
                    hasPrev = true;
                }

                if (hasRed)
                {
                    if (hasPrev)
                    {
                        x += gap;
                    }
                    g.DrawString(red, f, br, x, y, StringFormat.GenericTypographic);
                    x += wRed;
                    hasPrev = true;
                }

                if (hasUre)
                {
                    if (hasPrev)
                    {
                        x += gap;
                    }
                    g.DrawString(ure, f, bo, x, y, StringFormat.GenericTypographic);
                }
            }
        }

        private static void DrawTime(Graphics g, RectangleF c, RectangleF band, string time4, Font fMM, Font fSS, Color color)
        {
            string t = NormalizeTime4(time4);
            string mm = t.Substring(0, 2);
            string ss = t.Substring(2, 2);
            SizeF sMM = g.MeasureString(mm, fMM, 200, StringFormat.GenericTypographic);
            SizeF sSS = g.MeasureString(ss, fSS, 200, StringFormat.GenericTypographic);
            float x = c.Left + (c.Width - sMM.Width - sSS.Width) * 0.5f;
            float yMM = c.Top + band.Height * 0.065f;
            float ySS = yMM + (sMM.Height - sSS.Height) + band.Height * 0.01f;
            using (Brush b = new SolidBrush(color))
            {
                g.DrawString(mm, fMM, b, x, yMM, StringFormat.GenericTypographic);
                g.DrawString(ss, fSS, b, x + sMM.Width, ySS, StringFormat.GenericTypographic);
            }
        }

        private static string NormalizeTime4(string value)
        {
            string digits = NormalizeTime4OrEmpty(value);
            return digits.Length == 0 ? "0000" : digits;
        }

        private static string NormalizeTime4OrEmpty(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string digits = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    digits += value[i];
                }
            }

            if (digits.Length == 0)
            {
                return string.Empty;
            }

            if (digits.Length >= 4)
            {
                return digits.Substring(0, 4);
            }

            return digits.PadLeft(4, '0');
        }

        private static TimetableTrainInfoRow GetInfo(TimetableTrainInfoRow[] rows, int index)
        {
            if (rows != null && index >= 0 && index < rows.Length && rows[index] != null)
            {
                return rows[index];
            }

            return new TimetableTrainInfoRow();
        }

        private static TimetableTimeRow GetTime(TimetableTimeRow[] rows, int index)
        {
            if (rows != null && index >= 0 && index < rows.Length && rows[index] != null)
            {
                return rows[index];
            }

            return new TimetableTimeRow();
        }

        private static RectangleF[] BuildCols(RectangleF band, float labelW, int count)
        {
            float px = band.Width * 0.012f;
            float gap = band.Width * (count >= 10 ? 0.008f : 0.011f);
            float left = band.Left + labelW + px;
            float right = band.Right - px;
            float cw = (right - left - gap * (count - 1)) / count;
            RectangleF[] cols = new RectangleF[count];
            for (int i = 0; i < count; i++)
            {
                cols[i] = new RectangleF(left + i * (cw + gap), band.Top + band.Height * 0.02f, cw, band.Height * 0.96f);
            }

            return cols;
        }

        private static void DrawBadge(Graphics g, RectangleF rect, string platformText, float contentTextScale)
        {
            using (Brush black = new SolidBrush(CBlack))
            using (Font badgeFont = FontFor(rect.Height * 0.19f * contentTextScale, FontStyle.Bold))
            {
                DrawCenter(g, platformText ?? string.Empty, badgeFont, black, new RectangleF(rect.Left, rect.Top + rect.Height * 0.04f, rect.Width, rect.Height * 0.24f));
            }
        }

        private static float NormalizeContentTextScale(float value)
        {
            if (value <= 0f)
            {
                return 0.95f;
            }

            if (value < 0.50f)
            {
                return 0.50f;
            }

            if (value > 1.50f)
            {
                return 1.50f;
            }

            return value;
        }

        private static void DrawCenter(Graphics g, string text, Font f, Brush b, RectangleF rect)
        {
            string value = text ?? string.Empty;
            StringFormat s = new StringFormat();
            s.Alignment = StringAlignment.Center;
            s.LineAlignment = StringAlignment.Center;
            s.Trimming = StringTrimming.EllipsisCharacter;
            s.FormatFlags = StringFormatFlags.NoWrap;
            using (Font fitted = CreateFittedFont(g, value, f, rect, s))
            {
                g.DrawString(value, fitted, b, rect, s);
            }

            s.Dispose();
        }

        private static void DrawCenterNoEllipsis(Graphics g, string text, Font f, Brush b, RectangleF rect, float minScale)
        {
            string value = text ?? string.Empty;
            StringFormat s = new StringFormat();
            s.Alignment = StringAlignment.Center;
            s.LineAlignment = StringAlignment.Center;
            s.Trimming = StringTrimming.None;
            s.FormatFlags = StringFormatFlags.NoWrap;
            using (Font fitted = CreateFittedFont(g, value, f, rect, s, minScale))
            {
                g.DrawString(value, fitted, b, rect, s);
            }

            s.Dispose();
        }

        private static Font CreateFittedFont(Graphics g, string text, Font baseFont, RectangleF rect, StringFormat format, float minScale = 0.28f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Font)baseFont.Clone();
            }

            float size = baseFont.Size;
            float minSize = Math.Max(3f, baseFont.Size * minScale);
            SizeF layout = new SizeF(Math.Max(1f, rect.Width), Math.Max(1f, rect.Height));
            for (int i = 0; i < 30; i++)
            {
                Font candidate = null;
                try
                {
                    candidate = new Font(baseFont.FontFamily, size, baseFont.Style, baseFont.Unit);
                    SizeF m = g.MeasureString(text, candidate, layout, format);
                    if (m.Width <= rect.Width * 0.96f && m.Height <= rect.Height * 0.96f)
                    {
                        return candidate;
                    }
                }
                catch
                {
                }

                if (candidate != null)
                {
                    candidate.Dispose();
                }

                size *= 0.92f;
                if (size <= minSize)
                {
                    break;
                }
            }

            try
            {
                return new Font(baseFont.FontFamily, Math.Max(minSize, size), baseFont.Style, baseFont.Unit);
            }
            catch
            {
                return (Font)baseFont.Clone();
            }
        }

        private static Font FontFor(float sizePx, FontStyle style)
        {
            float s = Math.Max(6f, sizePx);

            EnsurePrivateFontsLoaded();
            Font fPrivate = TryCreatePrivateFont(s, style);
            if (fPrivate != null)
            {
                return fPrivate;
            }

            string[] names = { "BIZ UDPGothic", "Meiryo UI", "Meiryo", "MS UI Gothic" };
            for (int i = 0; i < names.Length; i++)
            {
                try
                {
                    return new Font(names[i], s, style, GraphicsUnit.Pixel);
                }
                catch
                {
                }
            }

            return new Font(SystemFonts.DefaultFont.FontFamily, s, style, GraphicsUnit.Pixel);
        }

        private static void EnsurePrivateFontsLoaded()
        {
            if (FontsLoaded)
            {
                return;
            }

            lock (FontInitLock)
            {
                if (FontsLoaded)
                {
                    return;
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                TryLoadFontsFromDir(Path.Combine(baseDir, "Fonts"));
                TryLoadFontsFromDir(Path.GetFullPath(Path.Combine(baseDir, "..", "Fonts")));
                FontsLoaded = true;
            }
        }

        private static void TryLoadFontsFromDir(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return;
            }

            string[] files = Directory.GetFiles(dir, "*.ttf");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    PrivateFonts.AddFontFile(files[i]);
                }
                catch
                {
                }
            }
        }

        private static Font TryCreatePrivateFont(float sizePx, FontStyle style)
        {
            if (PrivateFonts.Families == null || PrivateFonts.Families.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < PrivateFonts.Families.Length; i++)
            {
                FontFamily family = PrivateFonts.Families[i];
                try
                {
                    FontStyle useStyle = family.IsStyleAvailable(style) ? style : FontStyle.Regular;
                    return new Font(family, sizePx, useStyle, GraphicsUnit.Pixel);
                }
                catch
                {
                }
            }

            return null;
        }

        private static Color ParseColor(string raw, Color fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            string s = raw.Trim().ToLowerInvariant();
            if (s == "orange" || s == "o" || s == "橙")
            {
                return COrange;
            }
            if (s == "blue" || s == "b" || s == "青")
            {
                return CBlue;
            }
            if (s == "green" || s == "g" || s == "緑")
            {
                return CGreen;
            }
            if (s == "red" || s == "r" || s == "赤")
            {
                return CRed;
            }
            if (s == "black" || s == "k" || s == "黒")
            {
                return CBlack;
            }

            return fallback;
        }
    }

    public sealed class TimetableData
    {
        public string StationName;
        public string SubTitle;
        public string ServiceLabel;
        public string RevisedDate;
        public string Platform3Label;
        public string Platform4Label;
        public string RemarksText;

        public string TopLeftHour;
        public int TopOverlayColumn;
        public string TopOverlayHour;
        public string MidLeftHour;
        public int MidOverlayColumn;
        public string MidOverlayHour;

        public bool DrawBottomVerticalLines;
        public float ContentTextScale;

        public TimetableTrainInfoRow[] TopInfo;
        public TimetableTimeRow[] TopTimes;
        public TimetableTrainInfoRow[] MidInfo;
        public TimetableTimeRow[] MidTimes;
        public TimetableTrainInfoRow[] BottomInfo;
        public TimetableTimeRow[] BottomTimes;

        public static TimetableData CreateDefault()
        {
            return TimetableStorage.LoadDefaultData();
        }

        public static TimetableData CreateEmpty()
        {
            return new TimetableData
            {
                StationName = string.Empty,
                SubTitle = string.Empty,
                ServiceLabel = string.Empty,
                RevisedDate = string.Empty,
                Platform3Label = "③",
                Platform4Label = "④",
                RemarksText = DefaultRemarksText(),
                TopLeftHour = string.Empty,
                TopOverlayColumn = -1,
                TopOverlayHour = string.Empty,
                MidLeftHour = string.Empty,
                MidOverlayColumn = -1,
                MidOverlayHour = string.Empty,
                DrawBottomVerticalLines = false,
                ContentTextScale = 0.95f,
                TopInfo = new TimetableTrainInfoRow[0],
                TopTimes = new TimetableTimeRow[0],
                MidInfo = new TimetableTrainInfoRow[0],
                MidTimes = new TimetableTimeRow[0],
                BottomInfo = new TimetableTrainInfoRow[0],
                BottomTimes = new TimetableTimeRow[0]
            };
        }

        public static string DefaultRemarksText()
        {
            return
                "【停車駅】 快速：T快 高槻から各駅\r\n" +
                "高槻での新快速乗り換え時間は3分以上（遅延時の注意）\r\n" +
                "[xxxxM x分]：高槻での新快速接続時間\r\n" +
                "【乗車位置】12両=△①〜⑫、10両=△①〜⑩、8両=△①〜⑧、6両=△③〜⑧\r\n" +
                "C電=○①〜⑦ ③女性専用車両\r\n" +
                "【客扱終了表示】10両以上の列車（赤字表記）";
        }
    }

    public sealed class TimetableTrainInfoRow
    {
        public string Code;
        public string CodeColor;
        public string TrainNo;
        public string TrainType;
        public string Destination;
    }

    public sealed class TimetableTimeRow
    {
        public string Time4;
        public bool Highlight;
        public string TrainNo;
        public string TypeAndDestination;
        public string NoteBlue;
        public string NoteRed;
        public bool UreSeat;
    }
}
