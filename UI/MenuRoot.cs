using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class MenuRoot
    {
        private const int WindowId = 0x5B7A00;
        internal static bool Visible;
        internal static int ActiveTab;

        private static Rect _rect = new Rect(40, 60, 580, 640);
        private static Vector2 _scroll;
        private static List<TabGroup> _groups;
        private static bool _posLoaded;

        private static GUIStyle _headerBar, _sidebar, _hudBox, _hudTitle, _hudLine, _footerHint;
        private static bool _resizing;

        private const float MinW = 420f, MinH = 360f, MaxW = 1200f, MaxH = 1000f;
        private const float SidebarW = 148f;
        private const float HeaderH = 34f;

        /* ═══════════════════════════════════════════════════════════════
         *  Public API
         * ═══════════════════════════════════════════════════════════════ */

        internal static void Draw(List<TabGroup> groups)
        {
            _groups = groups;
            EnsureLocalStyles();

            if (!_posLoaded)
            {
                _rect.x = ModConfig.WindowX.Value;
                _rect.y = ModConfig.WindowY.Value;
                _rect.width  = Mathf.Clamp(ModConfig.WindowW.Value, MinW, MaxW);
                _rect.height = Mathf.Clamp(ModConfig.WindowH.Value, MinH, MaxH);
                _posLoaded = true;
            }

            if (Visible)
                _rect = GUI.Window(WindowId, _rect, DrawWindow, "", Theme.Window);
            
            if (ModConfig.ShowHud.Value && PlayerContext.InGame)
                DrawHud();

            ListPicker.Draw();
        }

        internal static void SaveLayout()
        {
            ModConfig.WindowX.Value = _rect.x;
            ModConfig.WindowY.Value = _rect.y;
            ModConfig.WindowW.Value = _rect.width;
            ModConfig.WindowH.Value = _rect.height;
        }

        internal static bool ContainsPoint(Vector2 screenPoint)
        {
            if (!Visible) return false;
            float scale = Mathf.Max(0.1f, ModConfig.UiScale.Value);
            Rect s = new Rect(_rect.x * scale, _rect.y * scale, _rect.width * scale, _rect.height * scale);
            return s.Contains(screenPoint);
        }

        internal static void ResetPosition()
        {
            _rect = new Rect(40, 60, 580, 640);
            SaveLayout();
        }

        internal static string CurrentTabName =>
            _groups != null && _groups.Count > 0
                ? _groups[Mathf.Clamp(ActiveTab, 0, _groups.Count - 1)].Name : "";

        internal static void SelectTabByName(string name)
        {
            if (_groups == null) return;
            for (int i = 0; i < _groups.Count; i++)
                if (_groups[i].Name == name) { ActiveTab = i; Visible = true; return; }
            for (int i = 0; i < _groups.Count; i++)
                for (int j = 0; j < _groups[i].Pages.Count; j++)
                    if (_groups[i].Pages[j].Name == name) { ActiveTab = i; _groups[i].Page = j; Visible = true; return; }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Local Styles
         * ═══════════════════════════════════════════════════════════════ */

        private static void EnsureLocalStyles()
        {
            if (_headerBar != null) return;

            _headerBar = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 10, 0, 0) };
            _headerBar.normal.background = Theme.GradientV(4, 48, 0,
                Theme.HeaderBg,
                new Color(Theme.HeaderBg.r + 0.02f, Theme.HeaderBg.g + 0.02f, Theme.HeaderBg.b + 0.04f));

            _sidebar = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6, 6, 10, 10) };
            _sidebar.normal.background = Theme.Solid(Theme.SidebarBg);

            _hudBox = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                alignment = TextAnchor.UpperLeft
            };
            _hudBox.normal.background = Theme.RoundedRect(20, 20, 8, Theme.WindowBg);
            _hudBox.border = new RectOffset(8, 8, 8, 8);

            _footerHint = new GUIStyle(Theme.Hint) { alignment = TextAnchor.MiddleRight };
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Window
         * ═══════════════════════════════════════════════════════════════ */

        private static void DrawWindow(int id)
        {
            if (_groups == null || _groups.Count == 0) { GUI.DragWindow(); return; }
            ActiveTab = Mathf.Clamp(ActiveTab, 0, _groups.Count - 1);

            // Resize click detection
            if (Event.current.type == EventType.MouseDown
                && new Rect(_rect.width - 26f, _rect.height - 26f, 26f, 26f).Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }

            DrawHeaderBar();
            DrawHeaderSeparator();

            GUILayout.BeginHorizontal();
            DrawSidebar();
            DrawSidebarSeparator();
            DrawContent();
            GUILayout.EndHorizontal();

            DrawFooter();
            HandleResize();
            GUI.DragWindow(new Rect(0, 0, _rect.width, HeaderH));
        }

        /* ─────── Header ────────────────────────────────────────────── */

        private static void DrawHeaderBar()
        {
            GUILayout.BeginHorizontal(_headerBar, GUILayout.Height(HeaderH));

            // Logo
            GUILayout.Label("<b><color=#" + Theme.Hex(Theme.Accent) + ">Poppy Menu Enhanced</color></b>",
                Theme.Header, GUILayout.ExpandWidth(false));

            GUILayout.FlexibleSpace();
            DrawStatusPill();

            // Scale controls
            if (GUILayout.Button("\u2212", Theme.IconBtn, GUILayout.Width(28), GUILayout.Height(24)))
                ModConfig.UiScale.Value = Mathf.Clamp(ModConfig.UiScale.Value - 0.1f, 0.6f, 2f);
            if (GUILayout.Button("+", Theme.IconBtn, GUILayout.Width(28), GUILayout.Height(24)))
                ModConfig.UiScale.Value = Mathf.Clamp(ModConfig.UiScale.Value + 0.1f, 0.6f, 2f);

            GUILayout.Space(4);

            // Close
            if (GUILayout.Button("\u2715", Theme.IconBtn, GUILayout.Width(28), GUILayout.Height(24)))
            {
                Visible = false;
                SaveLayout();
                ListPicker.Close();
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawStatusPill()
        {
            string text; Color col;
            if (!PlayerContext.InGame)        { text = "MENU";            col = Theme.TextDim; }
            else if (NetUtil.IsServer)        { text = "HOST";            col = Theme.On; }
            else                              { text = "CLIENT";          col = new Color(0.95f, 0.78f, 0.28f); }

            Theme.Pill.normal.textColor = col;
            GUILayout.Label("\u25CF " + text, Theme.Pill, GUILayout.Height(24));
        }

        private static void DrawHeaderSeparator()
        {
            Rect r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            Theme.Fill(new Rect(r.x, r.y, r.width, 1), new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.25f));
        }

        /* ─────── Sidebar ───────────────────────────────────────────── */

        private static Vector2 _sideScroll;

        private static void DrawSidebar()
        {
            float sideH = Mathf.Max(100f, _rect.height - HeaderH - 32f);
            GUILayout.BeginVertical(_sidebar, GUILayout.Width(SidebarW), GUILayout.Height(sideH));
            _sideScroll = GUILayout.BeginScrollView(_sideScroll, GUILayout.Width(SidebarW));

            for (int i = 0; i < _groups.Count; i++)
            {
                bool active = i == ActiveTab;
                if (GUILayout.Button(_groups[i].Name, active ? Theme.SideItemActive : Theme.SideItem,
                        GUILayout.Height(34)))
                    ActiveTab = i;

                // Accent indicator bar on the left
                if (active)
                {
                    Rect btn = GUILayoutUtility.GetLastRect();
                    Theme.Fill(new Rect(btn.x, btn.y + 5, 3, btn.height - 10), Theme.Accent);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static void DrawSidebarSeparator()
        {
            Rect r = GUILayoutUtility.GetRect(1, 1, GUILayout.Width(1));
            float sideH = Mathf.Max(100f, _rect.height - HeaderH - 32f);
            Theme.Fill(new Rect(r.x, r.y, 1, sideH), new Color(1, 1, 1, 0.04f));
        }

        /* ─────── Content ───────────────────────────────────────────── */

        private static void DrawContent()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(4);

            if (!PlayerContext.InGame)
                GUILayout.Label("<color=#" + Theme.Hex(Theme.TextDim) + ">Start a run to use most features.</color>",
                    new GUIStyle(Theme.Hint) { richText = true, alignment = TextAnchor.MiddleCenter });

            TabGroup group = _groups[ActiveTab];
            group.Page = Mathf.Clamp(group.Page, 0, group.Pages.Count - 1);

            // ── Sub-tab pills ──
            bool hasSubNav = group.Pages.Count > 1;
            if (hasSubNav)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < group.Pages.Count; i++)
                {
                    bool act = i == group.Page;
                    if (GUILayout.Button(group.Pages[i].Name, act ? Theme.Primary : Theme.Button,
                            GUILayout.Height(26)))
                        group.Page = i;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            // ── Scrollable content ──
            PoppyModule page = group.Pages[group.Page];
            float contentH = Mathf.Max(100f, _rect.height - HeaderH - 36f - (hasSubNav ? 34f : 0f));
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(contentH));
            Widgets.OpenSections = 0;
            try
            {
                page.DrawMenu();
            }
            catch (System.Exception e)
            {
                while (Widgets.OpenSections > 0) Widgets.SectionEnd();
                GUILayout.Label("This tab hit an error, see console.", Theme.Label);
                Log.Error($"{page.Name}.DrawMenu: {e}");
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /* ─────── Footer ────────────────────────────────────────────── */

        private static void DrawFooter()
        {
            // Separator
            Rect sep = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            Theme.Fill(new Rect(sep.x, sep.y, sep.width, 1), new Color(1, 1, 1, 0.04f));

            GUILayout.BeginHorizontal(GUILayout.Height(22));
            int n = ActiveEffects().Count;
            string status = n > 0
                ? $"<color=#{Theme.Hex(Theme.On)}>\u25CF {n} active</color>"
                : "<color=#555562>\u25CB idle</color>";
            GUILayout.Label(status, new GUIStyle(Theme.Hint) { richText = true }, GUILayout.Width(100));

            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{ModConfig.ToggleMenuKey.Value}] toggle  \u00B7  drag header to move", _footerHint);
            GUILayout.EndHorizontal();
        }

        /* ─────── Resize Handle ─────────────────────────────────────── */

        private static void HandleResize()
        {
            // Draw grip (three diagonal lines)
            float gx = _rect.width - 16f, gy = _rect.height - 16f;
            Color grip = new Color(1, 1, 1, 0.15f);
            Theme.Fill(new Rect(gx + 8f,  gy + 12f, 6f, 1.5f), grip);
            Theme.Fill(new Rect(gx + 12f, gy + 8f,  1.5f, 6f), grip);
            Theme.Fill(new Rect(gx + 4f,  gy + 12f, 3f, 1.5f), grip);
            Theme.Fill(new Rect(gx + 12f, gy + 4f,  1.5f, 3f), grip);

            if (!_resizing) return;

            float scale = Mathf.Max(0.1f, ModConfig.UiScale.Value);
            float mouseX = Input.mousePosition.x / scale;
            float mouseY = (Screen.height - Input.mousePosition.y) / scale;
            _rect.width  = Mathf.Clamp(mouseX - _rect.x + 3f, MinW, MaxW);
            _rect.height = Mathf.Clamp(mouseY - _rect.y + 3f, MinH, MaxH);

            if (!Input.GetMouseButton(0)) { _resizing = false; SaveLayout(); }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  HUD Overlay
         * ═══════════════════════════════════════════════════════════════ */

        private static bool _draggingHud;
        private static Vector2 _hudDragOffset;

        private static void DrawHud()
        {
            if (_hudTitle == null)
            {
                _hudTitle = new GUIStyle(Theme.Label)
                {
                    richText = true, fontSize = 12, wordWrap = false, fontStyle = FontStyle.Bold
                };
                _hudLine = new GUIStyle(Theme.Hint)
                {
                    richText = true, fontSize = 11, wordWrap = false
                };
            }

            List<string> active = ActiveEffects();
            if (active.Count == 0) return;

            float maxW = 120f;
            foreach (string e in active)
            {
                Vector2 s = _hudLine.CalcSize(new GUIContent("\u25CF " + e));
                if (s.x > maxW) maxW = s.x;
            }

            Vector2 titleS = _hudTitle.CalcSize(new GUIContent($"POPPY  [{ModConfig.ToggleMenuKey.Value}]"));
            if (titleS.x > maxW) maxW = titleS.x;

            float padX = 14f;
            float padY = 12f;
            float titleH = 18f;
            float titleGap = 4f;
            float lineH = 16f;
            float lineGap = 1f;

            float width = maxW + padX * 2f;
            float h = padY * 2f + titleH + titleGap + active.Count * lineH + Mathf.Max(0, active.Count - 1) * lineGap;

            Rect r = new Rect(ModConfig.HudX.Value, ModConfig.HudY.Value, width, h);

            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                _draggingHud = true;
                _hudDragOffset = ev.mousePosition - new Vector2(r.x, r.y);
            }
            else if (ev.type == EventType.MouseUp)
            {
                _draggingHud = false;
            }
            else if (ev.type == EventType.MouseDrag && _draggingHud)
            {
                ModConfig.HudX.Value = ev.mousePosition.x - _hudDragOffset.x;
                ModConfig.HudY.Value = ev.mousePosition.y - _hudDragOffset.y;
                r.x = ModConfig.HudX.Value;
                r.y = ModConfig.HudY.Value;
            }

            // Draw Background
            GUI.Box(r, "", _hudBox);

            // Draw Content with absolute pixel-perfect coordinates
            float curY = r.y + padY;
            float contentX = r.x + padX;
            float contentW = width - padX * 2f;

            GUI.Label(new Rect(contentX, curY, contentW, titleH),
                "<b><color=#" + Theme.Hex(Theme.Accent) + ">POPPY</color></b>  " +
                "<size=9><color=#" + Theme.Hex(Theme.TextDim) + ">[" + ModConfig.ToggleMenuKey.Value + "]</color></size>",
                _hudTitle);
            
            curY += titleH + titleGap;

            foreach (string e in active)
            {
                GUI.Label(new Rect(contentX, curY, 14f, lineH), "<color=#" + Theme.Hex(Theme.On) + ">\u25CF</color>", _hudLine);
                GUI.Label(new Rect(contentX + 14f, curY, contentW - 14f, lineH), e, _hudLine);
                curY += lineH + lineGap;
            }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Active Effects
         * ═══════════════════════════════════════════════════════════════ */

        private static readonly List<string> _cachedActiveList = new List<string>();
        private static float _nextActiveCheck;

        internal static List<string> ActiveEffects()
        {
            if (Time.realtimeSinceStartup < _nextActiveCheck)
                return _cachedActiveList;

            _nextActiveCheck = Time.realtimeSinceStartup + 0.2f;
            _cachedActiveList.Clear();

            if (PlayerModule.GodMode)         _cachedActiveList.Add("God Mode");
            if (PlayerModule.InfiniteSkills)   _cachedActiveList.Add("Infinite Skills");
            if (Aim.Enabled)                   _cachedActiveList.Add("Aimbot");
            if (Aim.MagicBullet)               _cachedActiveList.Add("Magic Bullet");
            if (MovementModule.Flight)         _cachedActiveList.Add("Flight");
            if (MovementModule.NoClip)         _cachedActiveList.Add("No-Clip");
            if (MovementModule.AlwaysSprint)   _cachedActiveList.Add("Always Sprint");
            if (MovementModule.JumpPack)       _cachedActiveList.Add("Jump Pack");
            if (StatsModule.Active)            _cachedActiveList.Add("Stat Mods");
            if (ItemsModule.NoEquipmentCooldown) _cachedActiveList.Add("No Equip CD");
            if (RenderModule.EspMobs || RenderModule.EspInteractables || RenderModule.EspTeleporter)
                _cachedActiveList.Add("ESP");
            if (WorldModule.FreezeMatch)       _cachedActiveList.Add("Match Frozen");
            if (WorldModule.FreezeTimer)       _cachedActiveList.Add("Timer Frozen");
            if (System.Math.Abs(WorldModule.TimeScale - 1f) > 0.001f && !WorldModule.FreezeMatch)
                _cachedActiveList.Add($"Time {WorldModule.TimeScale:0.##}x");

            return _cachedActiveList;
        }
    }
}
