using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PoppyMenu
{
    internal static class InputCapture
    {
        internal static bool Active { get; private set; }

        private static Harmony _h;
        private static readonly List<MPEventSystem> _held = new List<MPEventSystem>();

        internal static void Init()
        {
            _h = new Harmony("poppy.input");
            var update = AccessTools.Method(typeof(PlayerCharacterMasterController), "Update");
            if (update != null)
                _h.Patch(update, prefix: new HarmonyMethod(typeof(InputCapture), nameof(PcmcUpdatePrefix)));

            var fixedUpdate = AccessTools.Method(typeof(PlayerCharacterMasterController), "FixedUpdate");
            if (fixedUpdate != null)
                _h.Patch(fixedUpdate, prefix: new HarmonyMethod(typeof(InputCapture), nameof(PcmcFixedUpdatePrefix)));

            var raycastAll = AccessTools.Method(typeof(EventSystem), nameof(EventSystem.RaycastAll));
            if (raycastAll != null)
                _h.Patch(raycastAll, prefix: new HarmonyMethod(typeof(InputCapture), nameof(EventSystemRaycastAllPrefix)));
        }

        internal static bool IsMouseOverUI()
        {
            Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (MenuRoot.Visible && MenuRoot.ContainsPoint(mousePos)) return true;
            if (ListPicker.IsOpen && ListPicker.ContainsPoint(mousePos)) return true;
            return false;
        }

        private static bool EventSystemRaycastAllPrefix(PointerEventData eventData, List<RaycastResult> raycastResults)
        {
            if (IsMouseOverUI())
            {
                raycastResults.Clear();
                return false;
            }
            return true;
        }

        internal static void Sync(bool want)
        {
            if (want)
            {
                _held.RemoveAll(es => es == null);
                foreach (MPEventSystem es in MPEventSystem.instancesList)
                {
                    if (es == null || _held.Contains(es)) continue;
                    es.cursorOpenerCount++;
                    _held.Add(es);
                }
            }
            else if (_held.Count > 0)
            {
                foreach (MPEventSystem es in _held)
                    if (es != null) es.cursorOpenerCount = Mathf.Max(0, es.cursorOpenerCount - 1);
                _held.Clear();
            }
            Active = want;
        }

        internal static void Shutdown()
        {
            Sync(false);
            _h?.UnpatchSelf();
            _h = null;
        }

        private static bool PcmcUpdatePrefix(PlayerCharacterMasterController __instance)
        {
            if (!Active) return true;
            InputBankTest ib = __instance.bodyInputs;
            if (ib == null || ib != PlayerContext.InputBank) return true;

            ib.moveVector = Vector3.zero;
            ib.SetRawMoveStates(Vector2.zero);
            return false;
        }

        private static bool PcmcFixedUpdatePrefix(PlayerCharacterMasterController __instance)
        {
            if (!Active) return true;
            InputBankTest ib = __instance.bodyInputs;
            if (ib == null || ib != PlayerContext.InputBank) return true;

            ib.skill1.PushState(false);
            ib.skill2.PushState(false);
            ib.skill3.PushState(false);
            ib.skill4.PushState(false);
            ib.jump.PushState(false);
            ib.sprint.PushState(false);
            ib.interact.PushState(false);
            ib.activateEquipment.PushState(false);
            ib.ping.PushState(false);
            ib.nextEquipment.PushState(false);
            ib.prevEquipment.PushState(false);
            return false;
        }
    }
}
