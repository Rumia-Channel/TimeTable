using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace TimeTableApp
{
    public static class TimetableRenderer
    {
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
                DrawInfoBand(g, topInfoBand, labelW, 10, data.TopInfo, badge4);
                DrawTimeBand(g, topTimeBand, labelW, 10, data.TopTimes, badge3, data.TopLeftHour, data.TopOverlayColumn, data.TopOverlayHour);
                DrawInfoBand(g, midInfoBand, labelW, 10, data.MidInfo, badge4);
                DrawTimeBand(g, midTimeBand, labelW, 10, data.MidTimes, badge3, data.MidLeftHour, data.MidOverlayColumn, data.MidOverlayHour);

                float miniLabelW = leftBottom.Width * 0.11f;

                if (data.DrawBottomVerticalLines)
                {
                    DrawMiniBandGrid(g, leftBottom, miniLabelW, lwThin);
                }

                DrawInfoBand(g, leftBottomTop, miniLabelW, 6, data.BottomInfo, badge4);
                DrawTimeBand(g, leftBottomBottom, miniLabelW, 6, data.BottomTimes, badge3, string.Empty, -1, string.Empty);
                DrawRightNotes(g, rightBottom);
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
            DrawBadge(g, new RectangleF(band.Left, band.Top, labelW, band.Height), badge, false, string.Empty);

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
                    using (Brush bCode = new SolidBrush(ParseColor(row.CodeColor, COrange)))
                    {
                        DrawCenter(g, row.Code, fCode, bCode, new RectangleF(c.Left, y, c.Width, band.Height * 0.2f));
                    }

                    y += band.Height * 0.20f;
                    DrawCenter(g, row.TrainNo, fNo, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.18f));
                    y += band.Height * 0.18f;
                    DrawCenter(g, row.TrainType, fType, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.22f));
                    y += band.Height * 0.22f;
                    DrawCenter(g, row.Destination, fDst, bBlack, new RectangleF(c.Left, y, c.Width, band.Height * 0.26f));
                }
            }
        }

        private static void DrawTimeBand(Graphics g, RectangleF band, float labelW, int count, TimetableTimeRow[] rows, string badge, string leftHour, int overlayCol, string overlayHour)
        {
            DrawBadge(g, new RectangleF(band.Left, band.Top, labelW, band.Height), badge, true, leftHour);

            RectangleF[] cols = BuildCols(band, labelW, count);
            if (overlayCol >= 0 && overlayCol < cols.Length && !string.IsNullOrEmpty(overlayHour))
            {
                using (Font fOverlay = FontFor(band.Height * 0.22f, FontStyle.Bold))
                using (Brush bBlack = new SolidBrush(CBlack))
                {
                    DrawCenter(g, overlayHour, fOverlay, bBlack, new RectangleF(cols[overlayCol].Left, band.Top, cols[overlayCol].Width, band.Height * 0.20f));
                }
            }

            using (Font fMM = FontFor(band.Height * 0.40f, FontStyle.Bold))
            using (Font fSS = FontFor(band.Height * 0.23f, FontStyle.Bold))
            using (Font fMid = FontFor(band.Height * 0.19f, FontStyle.Regular))
            using (Font fNote = FontFor(band.Height * 0.15f, FontStyle.Regular))
            using (Brush bBlack = new SolidBrush(CBlack))
            {
                for (int i = 0; i < count; i++)
                {
                    TimetableTimeRow row = GetTime(rows, i);
                    RectangleF c = cols[i];
                    DrawTime(g, c, band, row.Time4, fMM, fSS, row.Highlight ? CGreen : CBlack);
                    DrawCenter(g, row.TrainNo, fMid, bBlack, new RectangleF(c.Left, c.Top + band.Height * 0.47f, c.Width, band.Height * 0.18f));
                    DrawCenter(g, row.TypeAndDestination, fMid, bBlack, new RectangleF(c.Left, c.Top + band.Height * 0.64f, c.Width, band.Height * 0.19f));

                    if (!string.IsNullOrEmpty(row.NoteBlue) || !string.IsNullOrEmpty(row.NoteRed))
                    {
                        float noteY = c.Bottom - fNote.Height - band.Height * 0.02f;
                        DrawNote(g, c, noteY, fNote, row.NoteBlue, row.NoteRed);
                    }
                }
            }
        }

        private static void DrawMiniBandGrid(Graphics g, RectangleF band, float labelW, float lwThin)
        {
            using (Pen p = new Pen(CBlack, lwThin))
            {
                float x0 = band.Left + labelW;
                g.DrawLine(p, x0, band.Top, x0, band.Bottom);
                float cw = (band.Width - labelW) / 6f;
                for (int i = 1; i < 6; i++)
                {
                    float x = x0 + cw * i;
                    g.DrawLine(p, x, band.Top, x, band.Bottom);
                }
            }
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
            if (string.IsNullOrEmpty(value))
            {
                return "0000";
            }

            string digits = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    digits += value[i];
                }
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

        private static void DrawBadge(Graphics g, RectangleF rect, string platformText, bool withHour, string hourText)
        {
            using (Brush black = new SolidBrush(CBlack))
            using (Font badgeFont = FontFor(rect.Height * 0.19f, FontStyle.Bold))
            {
                DrawCenter(g, platformText ?? string.Empty, badgeFont, black, new RectangleF(rect.Left, rect.Top + rect.Height * 0.04f, rect.Width, rect.Height * 0.24f));
            }

            if (withHour && !string.IsNullOrWhiteSpace(hourText))
            {
                using (Brush black = new SolidBrush(CBlack))
                using (Font hourFont = FontFor(rect.Height * 0.24f, FontStyle.Bold))
                {
                    DrawCenter(g, hourText, hourFont, black, new RectangleF(rect.Left, rect.Top + rect.Height * 0.24f, rect.Width, rect.Height * 0.42f));
                }
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

        private static Font CreateFittedFont(Graphics g, string text, Font baseFont, RectangleF rect, StringFormat format)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Font)baseFont.Clone();
            }

            float size = baseFont.Size;
            float minSize = Math.Max(4f, baseFont.Size * 0.28f);
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
            return new TimetableData
            {
                StationName = "茨木",
                SubTitle = "列車発車時刻表（平日 夕方 208C〜818T 上り  高槻・京都方面）",
                ServiceLabel = "茨木夕上り",
                RevisedDate = "2025年3月15日改正",
                Platform3Label = "③",
                Platform4Label = "④",
                TopLeftHour = "17時",
                TopOverlayColumn = 2,
                TopOverlayHour = "18時",
                MidLeftHour = string.Empty,
                MidOverlayColumn = 4,
                MidOverlayHour = "19時",
                DrawBottomVerticalLines = false,
                TopInfo = new[]
                {
                    new TimetableTrainInfoRow{ Code="(5045)", CodeColor="Orange", TrainNo="4039M", TrainType="特急", Destination="サンダーバード" },
                    new TimetableTrainInfoRow{ Code="(54)", CodeColor="Orange", TrainNo="8545M", TrainType="回送", Destination="" },
                    new TimetableTrainInfoRow{ Code="(57)", CodeColor="Blue", TrainNo="3498M", TrainType="新快", Destination="野洲" },
                    new TimetableTrainInfoRow{ Code="(03)", CodeColor="Orange", TrainNo="8864J", TrainType="貨物", Destination="" },
                    new TimetableTrainInfoRow{ Code="(0730)", CodeColor="Orange", TrainNo="6752M", TrainType="回送", Destination="" },
                    new TimetableTrainInfoRow{ Code="(12)", CodeColor="Blue", TrainNo="3500M", TrainType="新快", Destination="長浜" },
                    new TimetableTrainInfoRow{ Code="(1515)", CodeColor="Orange", TrainNo="1042M", TrainType="特急", Destination="はるか" },
                    new TimetableTrainInfoRow{ Code="(2110)", CodeColor="Orange", TrainNo="4041M", TrainType="特急", Destination="サンダーバード" },
                    new TimetableTrainInfoRow{ Code="(27)", CodeColor="Blue", TrainNo="3502A", TrainType="新快", Destination="野洲" },
                    new TimetableTrainInfoRow{ Code="(3245)", CodeColor="Blue", TrainNo="3804M", TrainType="新快", Destination="湖敦賀" }
                },
                TopTimes = new[]
                {
                    new TimetableTimeRow{ Time4="5255", Highlight=true, TrainNo="8027", TypeAndDestination="下牧・米原", NoteBlue="4+2", NoteRed="" },
                    new TimetableTimeRow{ Time4="5800", Highlight=false, TrainNo="206C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="0450", Highlight=false, TrainNo="1190C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="0755", Highlight=true, TrainNo="804T", TypeAndDestination="下牧・米原", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="1350", Highlight=false, TrainNo="208C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="1950", Highlight=false, TrainNo="1192C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="2255", Highlight=true, TrainNo="806T", TypeAndDestination="下牧・米原", NoteBlue="+6", NoteRed="" },
                    new TimetableTimeRow{ Time4="2850", Highlight=false, TrainNo="210C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="3450", Highlight=false, TrainNo="1194C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="3835", Highlight=true, TrainNo="808T", TypeAndDestination="下牧・長浜", NoteBlue="", NoteRed="" }
                },
                MidInfo = new[]
                {
                    new TimetableTrainInfoRow{ Code="(42)", CodeColor="Blue", TrainNo="3506M", TrainType="新快", Destination="琵琶賀" },
                    new TimetableTrainInfoRow{ Code="(4530)", CodeColor="Orange", TrainNo="1044M", TrainType="特急", Destination="はるか" },
                    new TimetableTrainInfoRow{ Code="(48)", CodeColor="Blue", TrainNo="3508M", TrainType="新快", Destination="野洲" },
                    new TimetableTrainInfoRow{ Code="(5315)", CodeColor="Orange", TrainNo="4043M", TrainType="特急", Destination="サンダーバード" },
                    new TimetableTrainInfoRow{ Code="(57)", CodeColor="Blue", TrainNo="3510A", TrainType="新快", Destination="草津" },
                    new TimetableTrainInfoRow{ Code="(00)", CodeColor="Orange", TrainNo="8088M", TrainType="回送", Destination="" },
                    new TimetableTrainInfoRow{ Code="(0545)", CodeColor="Orange", TrainNo="1084L", TrainType="貨物", Destination="" },
                    new TimetableTrainInfoRow{ Code="(09)", CodeColor="Orange", TrainNo="8092D", TrainType="回送", Destination="" },
                    new TimetableTrainInfoRow{ Code="(12)", CodeColor="Blue", TrainNo="3512M", TrainType="新快", Destination="米原" },
                    new TimetableTrainInfoRow{ Code="(1515)", CodeColor="Orange", TrainNo="1046M", TrainType="特急", Destination="はるか" }
                },
                MidTimes = new[]
                {
                    new TimetableTimeRow{ Time4="4450", Highlight=false, TrainNo="212C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="4950", Highlight=false, TrainNo="1196C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="5335", Highlight=true, TrainNo="810T", TypeAndDestination="下牧・野洲", NoteBlue="T", NoteRed="" },
                    new TimetableTimeRow{ Time4="5950", Highlight=false, TrainNo="214C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="0450", Highlight=false, TrainNo="1198C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="0755", Highlight=true, TrainNo="812T", TypeAndDestination="下牧・米原", NoteBlue="4+6", NoteRed="" },
                    new TimetableTimeRow{ Time4="1350", Highlight=false, TrainNo="216C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="1950", Highlight=false, TrainNo="1200C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="2255", Highlight=true, TrainNo="814T", TypeAndDestination="下牧・野洲", NoteBlue="4+8", NoteRed="" },
                    new TimetableTimeRow{ Time4="2850", Highlight=false, TrainNo="218C", TypeAndDestination="京都", NoteBlue="", NoteRed="" }
                },
                BottomInfo = new[]
                {
                    new TimetableTrainInfoRow{ Code="(3230)", CodeColor="Orange", TrainNo="1072M", TrainType="特急", Destination="らくラクびわこ" },
                    new TimetableTrainInfoRow{ Code="(3530)", CodeColor="Orange", TrainNo="5088", TrainType="貨物", Destination="" },
                    new TimetableTrainInfoRow{ Code="(39)", CodeColor="Blue", TrainNo="4136M", TrainType="回送", Destination="" },
                    new TimetableTrainInfoRow{ Code="(42)", CodeColor="Blue", TrainNo="3516M", TrainType="新快", Destination="長浜" },
                    new TimetableTrainInfoRow{ Code="(4530)", CodeColor="Orange", TrainNo="1048M", TrainType="特急", Destination="はるか" },
                    new TimetableTrainInfoRow{ Code="(4815)", CodeColor="Orange", TrainNo="641D", TrainType="特急", Destination="スーパーはくと" }
                },
                BottomTimes = new[]
                {
                    new TimetableTimeRow{ Time4="3450", Highlight=false, TrainNo="220C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="3755", Highlight=true, TrainNo="816T", TypeAndDestination="下牧・米原", NoteBlue="4+4", NoteRed="" },
                    new TimetableTimeRow{ Time4="4350", Highlight=false, TrainNo="220C", TypeAndDestination="京都", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="4950", Highlight=false, TrainNo="1204C", TypeAndDestination="高槻", NoteBlue="", NoteRed="" },
                    new TimetableTimeRow{ Time4="5255", Highlight=true, TrainNo="818T", TypeAndDestination="下牧・野洲", NoteBlue="4+8", NoteRed="" },
                    new TimetableTimeRow{ Time4="5850", Highlight=false, TrainNo="222C", TypeAndDestination="京都", NoteBlue="", NoteRed="" }
                }
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
