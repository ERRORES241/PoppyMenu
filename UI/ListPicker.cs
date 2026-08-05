using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal static class ListPicker
    {
        internal struct Row
        {
            internal string Label;
            internal Color Color;
            internal Action OnClick;
            internal Row(string label, Color color, Action onClick) { Label = label; Color = color; OnClick = onClick; }
        }

        private const int WindowId = 0x5B7A01;
        private static bool _open;
        private static string _title = "";
        private static string _search = "";
        private static Vector2 _scroll;
        private static List<Row> _rows = new List<Row>();
        private static Rect _rect = new Rect(640, 90, 360, 580);

        internal static bool IsOpen => _open;

        internal static void Open(string title, List<Row> rows)
        {
            _title  = title;
            _rows   = rows ?? new List<Row>();
            _search = "";
            _scroll = Vector2.zero;
            _open   = true;
        }

        internal static void Close() => _open = false;

        internal static bool ContainsPoint(Vector2 screenPoint)
        {
            if (!_open) return false;
            float scale = Mathf.Max(0.1f, ModConfig.UiScale.Value);
            Rect s = new Rect(_rect.x * scale, _rect.y * scale, _rect.width * scale, _rect.height * scale);
            return s.Contains(screenPoint);
        }

        internal static void Draw()
        {
            if (!_open) return;
            _rect = GUI.Window(WindowId, _rect, DrawWindow, "", Theme.Window);
        }

        private static void DrawWindow(int id)
        {
            // Resize click detection
            if (Event.current.type == EventType.MouseDown
                && new Rect(_rect.width - 26f, _rect.height - 26f, 26f, 26f).Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }

            // ── Header ──
            GUILayout.BeginHorizontal(GUILayout.Height(28));
            GUILayout.Label("<color=#" + Theme.Hex(Theme.Accent2) + ">\u25C6</color>  " + _title.ToUpperInvariant(),
                Theme.SubHeader);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("\u2715", Theme.IconBtn, GUILayout.Width(26), GUILayout.Height(22))) Close();
            GUILayout.EndHorizontal();

            // Separator
            Rect sep = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            Theme.Fill(new Rect(sep.x, sep.y, sep.width, 1), new Color(1, 1, 1, 0.06f));
            GUILayout.Space(4);

            // ── Search ──
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("poppy_picker_search");
            _search = GUILayout.TextField(_search ?? "", Theme.Search, GUILayout.Height(26));
            if (GUILayout.Button("clear", Theme.Button, GUILayout.Width(50), GUILayout.Height(26)))
                _search = "";
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            string filter = (_search ?? "").Trim();
            bool hasFilter = filter.Length > 0;

            // ── List ──
            _scroll = GUILayout.BeginScrollView(_scroll);
            int matched = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                if (hasFilter && row.Label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                matched++;

                bool clicked = GUILayout.Button(row.Label, Theme.RowButton, GUILayout.Height(26));
                Rect r = GUILayoutUtility.GetLastRect();

                // Colored dot indicator
                float dotSize = 8f;
                float dotX = r.x + 10f;
                float dotY = r.y + (r.height - dotSize) * 0.5f;
                Theme.Fill(new Rect(dotX, dotY, dotSize, dotSize), row.Color);

                if (clicked) row.OnClick?.Invoke();
            }
            if (matched == 0) GUILayout.Label("No matches.", Theme.Hint);
            GUILayout.EndScrollView();

            // ── Footer ──
            GUILayout.Space(2);
            GUILayout.Label($"{matched} of {_rows.Count}  \u00B7  Esc to close", Theme.Hint);

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }

            HandleResize();
            GUI.DragWindow(new Rect(0, 0, _rect.width, 28));
        }

        private static bool _resizing;

        private static void HandleResize()
        {
            // Draw grip
            float gx = _rect.width - 16f, gy = _rect.height - 16f;
            Color grip = new Color(1, 1, 1, 0.15f);
            Theme.Fill(new Rect(gx + 8f, gy + 12f, 6f, 1.5f), grip);
            Theme.Fill(new Rect(gx + 12f, gy + 8f, 1.5f, 6f), grip);

            if (!_resizing) return;

            float scale = Mathf.Max(0.1f, ModConfig.UiScale.Value);
            float mouseX = Input.mousePosition.x / scale;
            float mouseY = (Screen.height - Input.mousePosition.y) / scale;
            _rect.width  = Mathf.Clamp(mouseX - _rect.x + 3f, 280f, 800f);
            _rect.height = Mathf.Clamp(mouseY - _rect.y + 3f, 260f, 900f);

            if (!Input.GetMouseButton(0)) _resizing = false;
        }
    }
}
