using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace TimeTableApp
{
    public static class TimetableRenderer
    {
        private const float VisualScale = 0.95f;
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

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                RectangleF table = new RectangleF(
                    width * 0.045f,
                    height * 0.165f,
                    width * 0.90f,
                    height * 0.70f);

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
                DrawInfoBand(g, topInfoBand, labelW, topInfoCount, data.TopInfo, badge4);
                DrawTimeBand(g, topTimeBand, labelW, topTimeCount, data.TopTimes, badge3, data.TopLeftHour, data.TopOverlayColumn, data.TopOverlayHour);
                DrawInfoBand(g, midInfoBand, labelW, midInfoCount, data.MidInfo, badge4);
                DrawTimeBand(g, midTimeBand, labelW, midTimeCount, data.MidTimes, badge3, data.MidLeftHour, data.MidOverlayColumn, data.MidOverlayHour);

                float miniLabelW = leftBottom.Width * 0.11f;

                if (data.DrawBottomVerticalLines)
                {
                    DrawMiniBandGrid(g, leftBottom, miniLabelW, lwThin, Math.Max(bottomInfoCount, bottomTimeCount));
                }

                DrawInfoBand(g, leftBottomTop, miniLabelW, bottomInfoCount, data.BottomInfo, badge4);
                DrawTimeBand(g, leftBottomBottom, miniLabelW, bottomTimeCount, data.BottomTimes, badge3, string.Empty, -1, string.Empty);
                DrawRightNotes(g, rightBottom);
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

        private static void DrawInfoBand(Graphics g, RectangleF band, float labelW, int count, TimetableTrainInfoRow[] rows, string badge)
        {
            DrawBadge(g, new RectangleF(band.Left, band.Top, labelW, band.Height), badge);

            RectangleF[] cols = BuildCols(band, labelW, count);
            using (Font fCode = FontFor(band.Height * 0.16f, FontStyle.Bold))
            using (Font fNo = FontFor(band.Height * 0.19f, FontStyle.Regular))
            using (Font fType = FontFor(band.Height * 0.22f, FontStyle.Bold))
            using (Font fDst = FontFor(band.Height * 0.18f, FontStyle.Regular))
            using (Brush bBlack = new SolidBrush(CBlack))
            {
                for (int i = 0; i < count; i++)
                {
                    TimetableTrainInfoRow row = GetInfo(rows, i);
                    RectangleF c = cols[i];
                    float y = c.Top + band.Height * 0.01f;
                    DrawInfoTimeCode(g, new RectangleF(c.Left, y, c.Width, band.Height * 0.2f), row.Code, ParseColor(row.CodeColor, COrange), fCode);

                    y += band.Height * 0.20f;
                    DrawCenterNoEllipsis(g, row.TrainNo, fNo, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.18f), 0.10f);
                    y += band.Height * 0.18f;
                    DrawCenter(g, row.TrainType, fType, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.22f));
                    y += band.Height * 0.22f;
                    DrawCenterNoEllipsis(g, row.Destination, fDst, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.26f), 0.10f);
                }
            }
        }

        private static void DrawInfoTimeCode(Graphics g, RectangleF rect, string raw, Color color, Font fallbackFont)
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
                using (Font fMM = FontFor(rect.Height * 0.90f, FontStyle.Bold))
                using (Font fSS = FontFor(rect.Height * 0.62f, FontStyle.Bold))
                using (Font fParen = FontFor(rect.Height * 0.78f, FontStyle.Bold))
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

        private static void DrawTimeBand(Graphics g, RectangleF band, float labelW, int count, TimetableTimeRow[] rows, string badge, string leftHour, int overlayCol, string overlayHour)
        {
            DrawBadge(g, new RectangleF(band.Left, band.Top, labelW, band.Height), badge);

            bool hasLeftHour = !string.IsNullOrWhiteSpace(leftHour);
            bool hasOverlayHour = overlayCol >= 0 && !string.IsNullOrEmpty(overlayHour);
            RectangleF[] cols = BuildCols(band, labelW, count);
            hasOverlayHour = hasOverlayHour && overlayCol < cols.Length;

            if (hasLeftHour)
            {
                DrawHourLabelInSlot(g, band, cols, -1, leftHour);
            }

            if (hasOverlayHour)
            {
                DrawHourLabelInSlot(g, band, cols, overlayCol, overlayHour);
            }

            using (Font fMM = FontFor(band.Height * 0.40f, FontStyle.Bold))
            using (Font fSS = FontFor(band.Height * 0.23f, FontStyle.Bold))
            using (Font fMid = FontFor(band.Height * 0.19f, FontStyle.Regular))
            using (Font fNote = FontFor(band.Height * 0.15f, FontStyle.Regular))
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

                    if (!string.IsNullOrEmpty(row.NoteBlue) || !string.IsNullOrEmpty(row.NoteRed))
                    {
                        float noteY = c.Bottom - fNote.Height - band.Height * 0.02f;
                        DrawNote(g, c, noteY, fNote, row.NoteBlue, row.NoteRed);
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

        private static void DrawHourLabelInSlot(Graphics g, RectangleF band, RectangleF[] cols, int slot, string text)
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
            DrawHourText(g, text, new RectangleF(x, y, colW, h), band.Height);
        }

        private static void DrawHourText(Graphics g, string text, RectangleF rect, float bandHeight)
        {
            string value = text ?? string.Empty;
            using (Brush b = new SolidBrush(CBlack))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags = StringFormatFlags.NoWrap;

                float size = Math.Max(6f, bandHeight * 0.20f);
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

        private static void DrawRightNotes(Graphics g, RectangleF right)
        {
            float x = right.Left + right.Width * 0.04f;
            float y = right.Top + right.Height * 0.05f;
            using (Font fHead = FontFor(right.Height * 0.088f, FontStyle.Bold))
            using (Font fBody = FontFor(right.Height * 0.079f, FontStyle.Regular))
            {
                y = DrawRunLine(g, x, y, fHead, "【停車駅】  快速：", CBlack, "T 快", CGreen, "  高槻から各駅", CBlack);
                y += right.Height * 0.02f;
                y = DrawRunLine(g, x, y, fBody, "高槻での", CBlack, "新快速", CBlue, "乗り換え時間は", CBlack, "3 分以上", CRed);
                y = DrawRunLine(g, x + right.Width * 0.40f, y, fBody, "遅延時の注意！", CRed);
                y += right.Height * 0.05f;
                y = DrawRunLine(g, x, y, fBody, "[xxxxM x 分]：高槻での", CBlack, "新快速", CBlue, "接続時間", CBlack);
                y += right.Height * 0.05f;
                y = DrawRunLine(g, x, y, fHead, "【乗車位置】 12 両＝△①〜⑫、10 両＝△①〜⑩、", CBlack);
                y = DrawRunLine(g, x + right.Width * 0.10f, y, fHead, "8 両＝△①〜⑧、6 両＝△③〜⑧", CBlack);
                y = DrawRunLine(g, x + right.Width * 0.15f, y, fHead, "C 電＝○①〜⑦　③女性専用車両", CBlack);
                y += right.Height * 0.055f;
                DrawRunLine(g, x, y, fHead, "【客扱終了表示】 ", CBlack, "10 両以上の列車", CRed, "（赤字表記）", CBlack);
            }
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

        private static void DrawNote(Graphics g, RectangleF c, float y, Font f, string blue, string red)
        {
            if (string.IsNullOrEmpty(blue))
            {
                using (Brush b = new SolidBrush(CRed))
                {
                    DrawCenter(g, red, f, b, new RectangleF(c.Left, y, c.Width, f.Height + 2f));
                }
                return;
            }

            if (string.IsNullOrEmpty(red))
            {
                using (Brush b = new SolidBrush(CBlue))
                {
                    DrawCenter(g, blue, f, b, new RectangleF(c.Left, y, c.Width, f.Height + 2f));
                }
                return;
            }

            SizeF sb = g.MeasureString(blue, f, 500, StringFormat.GenericTypographic);
            SizeF sr = g.MeasureString(red, f, 500, StringFormat.GenericTypographic);
            float total = sb.Width + sr.Width + 2f;
            float x = c.Left + (c.Width - total) * 0.5f;
            using (Brush bb = new SolidBrush(CBlue))
            using (Brush br = new SolidBrush(CRed))
            {
                g.DrawString(blue, f, bb, x, y, StringFormat.GenericTypographic);
                g.DrawString(red, f, br, x + sb.Width + 2f, y, StringFormat.GenericTypographic);
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

        private static void DrawBadge(Graphics g, RectangleF rect, string platformText)
        {
            using (Brush black = new SolidBrush(CBlack))
            using (Font badgeFont = FontFor(rect.Height * 0.19f, FontStyle.Bold))
            {
                DrawCenter(g, platformText ?? string.Empty, badgeFont, black, new RectangleF(rect.Left, rect.Top + rect.Height * 0.04f, rect.Width, rect.Height * 0.24f));
            }
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

        public string TopLeftHour;
        public int TopOverlayColumn;
        public string TopOverlayHour;
        public string MidLeftHour;
        public int MidOverlayColumn;
        public string MidOverlayHour;

        public bool DrawBottomVerticalLines;

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
                TopLeftHour = string.Empty,
                TopOverlayColumn = -1,
                TopOverlayHour = string.Empty,
                MidLeftHour = string.Empty,
                MidOverlayColumn = -1,
                MidOverlayHour = string.Empty,
                DrawBottomVerticalLines = false,
                TopInfo = new TimetableTrainInfoRow[0],
                TopTimes = new TimetableTimeRow[0],
                MidInfo = new TimetableTrainInfoRow[0],
                MidTimes = new TimetableTimeRow[0],
                BottomInfo = new TimetableTrainInfoRow[0],
                BottomTimes = new TimetableTimeRow[0]
            };
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
    }
}
