using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal static class Theme
    {
        // ── Accent (user-customizable) ──────────────────────────────────
        internal static Color Accent  = new Color32(0xE5, 0x38, 0x4A, 0xFF);
        internal static Color Accent2 = new Color32(0xF0, 0x58, 0x4E, 0xFF);

        // ── Fixed palette ───────────────────────────────────────────────
        internal static readonly Color On      = new Color32(0x2E, 0xCC, 0x71, 0xFF);
        internal static readonly Color Danger  = new Color32(0xE7, 0x4C, 0x3C, 0xFF);
        internal static readonly Color Text    = new Color32(0xE8, 0xE8, 0xF0, 0xFF);
        internal static readonly Color TextDim = new Color32(0x72, 0x72, 0x8A, 0xFF);

        // ── Surface hierarchy (deep → light) ────────────────────────────
        internal static readonly Color WindowBg  = new Color32(0x0C, 0x0C, 0x12, 0xF6);
        internal static readonly Color SidebarBg = new Color32(0x0A, 0x0A, 0x10, 0xFF);
        internal static readonly Color HeaderBg  = new Color32(0x12, 0x12, 0x1C, 0xFF);
        internal static readonly Color CardBg    = new Color32(0x16, 0x16, 0x20, 0xFF);
        internal static readonly Color RowBg     = new Color32(0x1C, 0x1C, 0x28, 0xFF);
        internal static readonly Color SlotOff   = new Color32(0x24, 0x24, 0x30, 0xFF);
        internal static readonly Color Hovered   = new Color32(0x2C, 0x2C, 0x3C, 0xFF);

        // ── Styles ──────────────────────────────────────────────────────
        internal static GUIStyle Window, Header, SubHeader, Label, Hint, Pill;
        internal static GUIStyle Button, Primary, Danger_, SideItem, SideItemActive;
        internal static GUIStyle SwitchOn, SwitchOff, Card, Search, RowButton, IconBtn, ChipLabel;
        internal static GUIStyle ToggleBase, ValueLabel;

        // ── Textures for custom controls ────────────────────────────────
        internal static Texture2D ToggleTrackOn, ToggleTrackOff, ToggleThumb;
        internal static Texture2D SliderThumbTex;

        // ── Private state ───────────────────────────────────────────────
        private static bool _ready;
        private static readonly Dictionary<long, Texture2D> _texCache = new Dictionary<long, Texture2D>();

        /* ═══════════════════════════════════════════════════════════════
         *  Texture Generation
         * ═══════════════════════════════════════════════════════════════ */

        internal static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        /// <summary>
        /// Creates a rounded-rectangle Texture2D suitable for 9-slice via GUIStyle.border.
        /// </summary>
        internal static Texture2D RoundedRect(int w, int h, int r, Color fill)
        {
            long key = HashKey(w, h, r, fill);
            if (_texCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int py = 0; py < h; py++)
                for (int px2 = 0; px2 < w; px2++)
                {
                    float a = RoundMask(px2, py, w, h, r);
                    Color c = fill; c.a *= a;
                    px[py * w + px2] = c;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _texCache[key] = tex;
            return tex;
        }

        /// <summary>
        /// Creates a vertical-gradient rounded-rectangle texture.
        /// </summary>
        internal static Texture2D GradientV(int w, int h, int r, Color bottom, Color top)
        {
            long key;
            unchecked { key = HashKey(w, h, r, bottom) * 31 + top.GetHashCode(); }
            if (_texCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int py = 0; py < h; py++)
            {
                float t = h > 1 ? (float)py / (h - 1) : 0f;
                Color rowCol = Color.Lerp(bottom, top, t);
                for (int px2 = 0; px2 < w; px2++)
                {
                    float a = RoundMask(px2, py, w, h, r);
                    Color c = rowCol; c.a *= a;
                    px[py * w + px2] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _texCache[key] = tex;
            return tex;
        }

        /// <summary>
        /// Creates a circle texture with anti-aliased edges.
        /// </summary>
        internal static Texture2D Circle(int size, Color fill)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int py = 0; py < size; py++)
                for (int px2 = 0; px2 < size; px2++)
                {
                    float dx = px2 - c, dy = py - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(c + 0.5f - dist);
                    Color col = fill; col.a *= a;
                    px[py * size + px2] = col;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        /// <summary>
        /// Returns 0..1 alpha for a pixel in a rounded-corner rectangle.
        /// </summary>
        private static float RoundMask(int x, int y, int w, int h, int r)
        {
            if (r <= 0) return 1f;
            float cx, cy;
            if      (x < r     && y < r)     { cx = r - 0.5f;     cy = r - 0.5f; }
            else if (x >= w - r && y < r)     { cx = w - r - 0.5f; cy = r - 0.5f; }
            else if (x < r     && y >= h - r) { cx = r - 0.5f;     cy = h - r - 0.5f; }
            else if (x >= w - r && y >= h - r) { cx = w - r - 0.5f; cy = h - r - 0.5f; }
            else return 1f;

            float dx = x - cx, dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(r - dist + 0.5f);
        }

        private static long HashKey(int w, int h, int r, Color c)
        {
            unchecked
            {
                long hash = 17;
                hash = hash * 397 + w;
                hash = hash * 397 + h;
                hash = hash * 397 + r;
                hash = hash * 397 + c.GetHashCode();
                return hash;
            }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Drawing Helpers
         * ═══════════════════════════════════════════════════════════════ */

        internal static void Fill(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        internal static void DrawTex(Rect r, Texture2D tex)
        {
            if (tex != null) GUI.DrawTexture(r, tex);
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Initialization
         * ═══════════════════════════════════════════════════════════════ */

        internal static void EnsureInit()
        {
            if (_ready) return;
            _ready = true;
            _texCache.Clear();

            // ── Derived colours ─────────────────────────────────────
            Color accentDim = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            Color onDim     = new Color(On.r, On.g, On.b, 0.18f);
            Color onHov     = new Color(On.r, On.g, On.b, 0.28f);
            Color dangerDim = new Color(Danger.r, Danger.g, Danger.b, 0.80f);

            // ── Rounded 9-slice textures ────────────────────────────
            const int BR = 6;   // button radius
            const int CR = 6;   // card radius

            Texture2D winTex   = RoundedRect(24, 24, 10, WindowBg);
            Texture2D cardTex  = RoundedRect(20, 20, CR, CardBg);
            Texture2D btnTex   = RoundedRect(16, 16, BR, SlotOff);
            Texture2D btnHov   = RoundedRect(16, 16, BR, Hovered);
            Texture2D btnAct   = RoundedRect(16, 16, BR, accentDim);
            Texture2D priTex   = RoundedRect(16, 16, BR, Accent);
            Texture2D priHov   = RoundedRect(16, 16, BR, Accent2);
            Texture2D danTex   = RoundedRect(16, 16, BR, dangerDim);
            Texture2D danHov   = RoundedRect(16, 16, BR, Danger);
            Texture2D sideNorm = Solid(new Color(0, 0, 0, 0));
            Texture2D sideAct  = RoundedRect(16, 16, 5, CardBg);
            Texture2D swOnTex  = RoundedRect(16, 16, BR, onDim);
            Texture2D swOnHov  = RoundedRect(16, 16, BR, onHov);
            Texture2D swOffTex = RoundedRect(16, 16, BR, RowBg);
            Texture2D swOffHov = RoundedRect(16, 16, BR, Hovered);
            Texture2D rowTex   = RoundedRect(16, 16, 4, RowBg);
            Texture2D rowHov   = RoundedRect(16, 16, 4, Hovered);
            Texture2D searchTx = RoundedRect(16, 16, BR, RowBg);
            Texture2D pillTex  = RoundedRect(20, 20, 10, RowBg);
            Texture2D togRow   = RoundedRect(16, 16, 4, new Color(RowBg.r, RowBg.g, RowBg.b, 0.5f));
            Texture2D togRowH  = RoundedRect(16, 16, 4, Hovered);

            // ── Toggle & slider control textures ────────────────────
            ToggleTrackOn  = RoundedRect(36, 18, 9, On);
            ToggleTrackOff = RoundedRect(36, 18, 9, SlotOff);
            ToggleThumb    = Circle(14, Color.white);
            SliderThumbTex = Circle(14, Accent);

            // ── Border settings ─────────────────────────────────────
            RectOffset bBtn  = new RectOffset(BR, BR, BR, BR);
            RectOffset bCard = new RectOffset(CR, CR, CR, CR);
            RectOffset bWin  = new RectOffset(10, 10, 10, 10);

            /* ─────── Window ────────────────────────────────────────── */
            Window = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin  = new RectOffset(0, 0, 0, 0),
                border  = bWin
            };
            Window.normal.background = winTex;

            /* ─────── Text styles ───────────────────────────────────── */
            Header = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold, fontSize = 16,
                alignment = TextAnchor.MiddleLeft, richText = true,
                wordWrap = false
            };
            Header.normal.textColor = Text;

            SubHeader = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold, fontSize = 12,
                alignment = TextAnchor.MiddleLeft, richText = true,
                wordWrap = false
            };
            SubHeader.normal.textColor = Accent2;

            Label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, wordWrap = true, richText = true,
                alignment = TextAnchor.MiddleLeft
            };
            Label.normal.textColor = Text;

            Hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, wordWrap = true, richText = true,
                alignment = TextAnchor.MiddleLeft
            };
            Hint.normal.textColor = TextDim;

            Pill = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 3, 3),
                richText = true, border = new RectOffset(10, 10, 10, 10)
            };
            Pill.normal.textColor = Text;
            Pill.normal.background = pillTex;

            ValueLabel = new GUIStyle(Label) { alignment = TextAnchor.MiddleCenter };

            /* ─────── Buttons ───────────────────────────────────────── */
            Button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12, alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 7, 7),
                margin  = new RectOffset(2, 2, 2, 2),
                border  = bBtn
            };
            Button.normal.background = btnTex;  Button.normal.textColor = Text;
            Button.hover.background  = btnHov;  Button.hover.textColor  = Color.white;
            Button.active.background = btnAct;

            Primary = new GUIStyle(Button);
            Primary.normal.background = priTex;  Primary.normal.textColor = Color.white;
            Primary.hover.background  = priHov;

            Danger_ = new GUIStyle(Button);
            Danger_.normal.background = danTex;  Danger_.normal.textColor = Color.white;
            Danger_.hover.background  = danHov;

            /* ─────── Sidebar ───────────────────────────────────────── */
            SideItem = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12, alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 8, 8, 8),
                margin  = new RectOffset(0, 0, 1, 1),
                border  = new RectOffset(5, 5, 5, 5)
            };
            SideItem.normal.background = sideNorm; SideItem.normal.textColor = TextDim;
            SideItem.hover.background  = rowHov;   SideItem.hover.textColor  = Text;
            SideItem.active.background = sideAct;

            SideItemActive = new GUIStyle(SideItem);
            SideItemActive.normal.background = sideAct;
            SideItemActive.normal.textColor  = Color.white;
            SideItemActive.fontStyle = FontStyle.Bold;

            /* ─────── Toggle rows (fallback styles kept for compat) ── */
            SwitchOn = new GUIStyle(Button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(10, 52, 7, 7)
            };
            SwitchOn.normal.background = swOnTex;  SwitchOn.normal.textColor = Color.white;
            SwitchOn.hover.background  = swOnHov;

            SwitchOff = new GUIStyle(SwitchOn);
            SwitchOff.normal.background = swOffTex; SwitchOff.normal.textColor = TextDim;
            SwitchOff.hover.background  = swOffHov;

            ToggleBase = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin  = new RectOffset(0, 0, 1, 1),
                border  = new RectOffset(4, 4, 4, 4)
            };
            ToggleBase.normal.background = togRow;
            ToggleBase.hover.background  = togRowH;
            ToggleBase.active.background = togRowH;
            ToggleBase.normal.textColor  = new Color(0, 0, 0, 0);
            ToggleBase.hover.textColor   = new Color(0, 0, 0, 0);
            ToggleBase.active.textColor  = new Color(0, 0, 0, 0);

            /* ─────── Cards & containers ────────────────────────────── */
            Card = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin  = new RectOffset(0, 0, 3, 3),
                border  = bCard
            };
            Card.normal.background = cardTex;

            /* ─────── Text fields ───────────────────────────────────── */
            Search = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                padding  = new RectOffset(10, 10, 6, 6),
                border   = bBtn
            };
            Search.normal.textColor  = Text;
            Search.normal.background = searchTx;
            Search.focused.textColor  = Text;
            Search.focused.background = searchTx;
            Search.hover.background   = searchTx;

            /* ─────── Row buttons (picker list) ─────────────────────── */
            RowButton = new GUIStyle(Button)
            {
                alignment = TextAnchor.MiddleLeft, fontSize = 12,
                padding   = new RectOffset(28, 8, 6, 6),
                border    = new RectOffset(4, 4, 4, 4)
            };
            RowButton.normal.background = rowTex;
            RowButton.hover.background  = rowHov;

            /* ─────── Icon buttons ──────────────────────────────────── */
            IconBtn = new GUIStyle(Button)
            {
                fontSize = 13, fontStyle = FontStyle.Bold,
                padding  = new RectOffset(0, 0, 2, 2),
                alignment = TextAnchor.MiddleCenter
            };

            /* ─────── Chip label ────────────────────────────────────── */
            ChipLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, alignment = TextAnchor.MiddleLeft, richText = true
            };
            ChipLabel.normal.textColor = Text;

            /* ─────── Scrollbar customisation ───────────────────────── */
            ApplyScrollbarSkin();
        }

        private static void ApplyScrollbarSkin()
        {
            try
            {
                GUISkin skin = GUI.skin;
                Texture2D trackTex = Solid(new Color(0f, 0f, 0f, 0.06f));
                Texture2D thumbTex = RoundedRect(8, 24, 4, new Color32(0x50, 0x50, 0x60, 0xA0));

                skin.verticalScrollbar.normal.background = trackTex;
                skin.verticalScrollbar.fixedWidth = 6;
                skin.verticalScrollbar.margin = new RectOffset(0, 2, 0, 0);

                skin.verticalScrollbarThumb.normal.background = thumbTex;
                skin.verticalScrollbarThumb.fixedWidth = 6;
                skin.verticalScrollbarThumb.border = new RectOffset(4, 4, 4, 4);

                skin.horizontalScrollbar.fixedHeight = 0;
                skin.horizontalScrollbarThumb.fixedHeight = 0;
            }
            catch { }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Accent Management
         * ═══════════════════════════════════════════════════════════════ */

        internal static void ApplyAccent(Color c)
        {
            Accent  = c;
            Accent2 = Color.Lerp(c, Color.white, 0.18f);
            _ready  = false;
        }

        internal static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }
}
