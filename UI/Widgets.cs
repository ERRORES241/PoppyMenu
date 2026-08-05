using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace PoppyMenu
{
    internal static class Widgets
    {
        /* ═══════════════════════════════════════════════════════════════
         *  Text
         * ═══════════════════════════════════════════════════════════════ */

        internal static void Header(string text)
        {
            GUILayout.Space(2);
            GUILayout.Label(text.ToUpperInvariant(), Theme.SubHeader);
        }

        internal static void Label(string text) => GUILayout.Label(text, Theme.Label);
        internal static void Hint(string text)  => GUILayout.Label(text, Theme.Hint);

        internal static void Separator()
        {
            GUILayout.Space(2);
            Rect r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            Theme.Fill(new Rect(r.x, r.y, r.width, 1), new Color(1, 1, 1, 0.06f));
            GUILayout.Space(2);
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Buttons
         * ═══════════════════════════════════════════════════════════════ */

        internal static bool Button(string text) => GUILayout.Button(text, Theme.Button, GUILayout.Height(30));

        internal static void Button(string text, Action onClick)
        {
            if (GUILayout.Button(text, Theme.Button, GUILayout.Height(30))) onClick?.Invoke();
        }

        internal static void PrimaryButton(string text, Action onClick)
        {
            if (GUILayout.Button(text, Theme.Primary, GUILayout.Height(30))) onClick?.Invoke();
        }

        internal static void DangerButton(string text, Action onClick)
        {
            if (GUILayout.Button(text, Theme.Danger_, GUILayout.Height(30))) onClick?.Invoke();
        }

        private static string _armed;

        internal static void ConfirmButton(string id, string text, Action onConfirm)
        {
            bool armed = _armed == id;
            if (GUILayout.Button(armed ? "Confirm: " + text + "?" : text,
                    armed ? Theme.Danger_ : Theme.Button, GUILayout.Height(30)))
            {
                if (armed) { _armed = null; onConfirm?.Invoke(); }
                else _armed = id;
            }
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Toggle Switch  (capsule track + circle thumb)
         * ═══════════════════════════════════════════════════════════════ */

        internal static bool Toggle(string label, bool value) => Toggle(label, value, KeyCode.None);

        internal static bool Toggle(string label, bool value, KeyCode key)
        {
            bool clicked = GUILayout.Button("", Theme.ToggleBase, GUILayout.Height(32));
            Rect r = GUILayoutUtility.GetLastRect();

            // ── Label ──
            float rightReserve = 52f;
            if (key != KeyCode.None) rightReserve += 50f;
            Rect labelRect = new Rect(r.x + 12, r.y, r.width - rightReserve - 12, r.height);
            GUI.Label(labelRect, label, Theme.Label);

            // ── Keybind hint ──
            if (key != KeyCode.None)
            {
                Rect kr = new Rect(r.xMax - 100f, r.y, 44f, r.height);
                GUI.Label(kr, "[" + key + "]", Theme.Hint);
            }

            // ── Track (capsule) ──
            const float trackW = 36f, trackH = 18f;
            float tx = r.xMax - trackW - 10f;
            float ty = r.y + (r.height - trackH) * 0.5f;
            Rect trackRect = new Rect(tx, ty, trackW, trackH);
            GUI.DrawTexture(trackRect, value ? Theme.ToggleTrackOn : Theme.ToggleTrackOff);

            // ── Thumb (circle) ──
            const float thumbSize = 14f;
            float thumbX = value ? trackRect.xMax - thumbSize - 2f : trackRect.x + 2f;
            float thumbY = trackRect.y + (trackH - thumbSize) * 0.5f;
            GUI.DrawTexture(new Rect(thumbX, thumbY, thumbSize, thumbSize), Theme.ToggleThumb);

            return clicked ? !value : value;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Custom Slider  (track + filled bar + circle thumb)
         * ═══════════════════════════════════════════════════════════════ */

        private static int _sliderHot;

        internal static float Slider(string label, float value, float min, float max)
        {
            GUILayout.Space(2f);
            // ── Label + value ──
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Theme.Label, GUILayout.ExpandWidth(true));
            GUILayout.Label(value.ToString("0.##"), new GUIStyle(Theme.Label)
            {
                alignment = TextAnchor.MiddleRight
            }, GUILayout.Width(56));
            GUILayout.EndHorizontal();

            GUILayout.Space(-6f);

            // ── Slider track area ──
            Rect track = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(20), GUILayout.ExpandWidth(true));

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            const float pad = 7f;
            float usableW = track.width - pad * 2f;
            if (usableW < 1f) usableW = 1f;
            float t = Mathf.InverseLerp(min, max, value);

            // ── Input handling ──
            Event evt = Event.current;
            switch (evt.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (track.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        _sliderHot = controlId;
                        t = Mathf.Clamp01((evt.mousePosition.x - track.x - pad) / usableW);
                        value = Mathf.Lerp(min, max, t);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        t = Mathf.Clamp01((evt.mousePosition.x - track.x - pad) / usableW);
                        value = Mathf.Lerp(min, max, t);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        _sliderHot = 0;
                        evt.Use();
                    }
                    break;
            }

            // ── Draw track ──
            if (evt.type == EventType.Repaint)
            {
                float trackH = 4f;
                float trackY = track.y + (track.height - trackH) * 0.5f;
                float fillW = t * usableW;

                // Background track
                Theme.Fill(new Rect(track.x + pad, trackY, usableW, trackH), Theme.SlotOff);

                // Filled portion
                if (fillW > 1f)
                    Theme.Fill(new Rect(track.x + pad, trackY, fillW, trackH), Theme.Accent);

                // Thumb
                const float thumbSz = 14f;
                float thumbX = track.x + pad + t * usableW - thumbSz * 0.5f;
                float thumbY = track.y + (track.height - thumbSz) * 0.5f;
                GUI.DrawTexture(new Rect(thumbX, thumbY, thumbSz, thumbSz), Theme.SliderThumbTex);
            }

            return value;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  IntStepper  (label ─── [ − ] value [ + ])
         * ═══════════════════════════════════════════════════════════════ */

        internal static int IntStepper(string label, int value, int step,
            int min = int.MinValue, int max = int.MaxValue)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(28));

            GUILayout.Label(label, Theme.Label, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("\u2212", Theme.Button, GUILayout.Width(32), GUILayout.Height(26)))
                value = Mathf.Clamp(value - step, min, max);

            GUILayout.Label(value.ToString(), Theme.ValueLabel, GUILayout.Width(60), GUILayout.Height(26));

            if (GUILayout.Button("+", Theme.Button, GUILayout.Width(32), GUILayout.Height(26)))
                value = Mathf.Clamp(value + step, min, max);

            GUILayout.EndHorizontal();
            return value;
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Section Cards
         * ═══════════════════════════════════════════════════════════════ */

        internal static int OpenSections;

        internal static void SectionBegin(string title)
        {
            GUILayout.Space(2);
            GUILayout.BeginVertical(Theme.Card);
            OpenSections++;

            // Accent bar at top
            Rect bar = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
            Theme.Fill(new Rect(bar.x, bar.y, bar.width, 2), Theme.Accent2);
            GUILayout.Space(4);

            if (!string.IsNullOrEmpty(title))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=#" + Theme.Hex(Theme.Accent2) + ">\u25CF</color>",
                    new GUIStyle(Theme.SubHeader) { richText = true }, GUILayout.Width(16));
                GUILayout.Label(title.ToUpperInvariant(), Theme.SubHeader);
                GUILayout.EndHorizontal();
            }
        }

        internal static void SectionEnd()
        {
            if (OpenSections <= 0) return;
            GUILayout.EndVertical();
            OpenSections--;
            GUILayout.Space(2);
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Keybind Row
         * ═══════════════════════════════════════════════════════════════ */

        internal static void KeybindRow(string label, ConfigEntry<KeyCode> entry)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(28));
            GUILayout.Label(label, Theme.Label, GUILayout.ExpandWidth(true));

            bool listening = Rebind.Listening == entry;
            if (GUILayout.Button(listening ? "press a key..." : entry.Value.ToString(),
                    listening ? Theme.Primary : Theme.Button,
                    GUILayout.Width(120), GUILayout.Height(26)))
            {
                if (listening) Rebind.Cancel(); else Rebind.Capture(entry);
            }

            if (GUILayout.Button("\u2715", Theme.Button, GUILayout.Width(28), GUILayout.Height(26)))
            {
                entry.Value = KeyCode.None;
                if (listening) Rebind.Cancel();
            }
            GUILayout.EndHorizontal();
        }

        /* ═══════════════════════════════════════════════════════════════
         *  Picker Button  (▸ label)
         * ═══════════════════════════════════════════════════════════════ */

        internal static void PickerButton(string text, string title, List<ListPicker.Row> rows)
        {
            if (GUILayout.Button("\u25B8  " + text, Theme.Button, GUILayout.Height(28)))
                ListPicker.Open(title, rows);
        }
    }
}
