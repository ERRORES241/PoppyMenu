using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class PlayerContext
    {
        internal static NetworkUser User;
        internal static CharacterMaster Master;
        internal static CharacterBody Body;
        internal static Inventory Inventory;
        internal static HealthComponent Health;
        internal static SkillLocator Skills;
        internal static CharacterMotor Motor;
        internal static InputBankTest InputBank;

        internal static bool InGame => Run.instance != null;

        internal static bool HasBody => Master != null && Body != null;

        private static CharacterBody _cachedBody;

        internal static void Refresh()
        {
            if (!InGame)
            {
                User = null;
                Master = null;
                Body = null;
                Inventory = null;
                Health = null;
                Skills = null;
                Motor = null;
                InputBank = null;
                _cachedBody = null;
                return;
            }

            var players = NetworkUser.readOnlyLocalPlayersList;
            if (players.Count > 0)
            {
                NetworkUser nu = players[0];
                if (nu != null)
                {
                    User = nu;
                    Master = nu.master;
                    if (Master != null)
                    {
                        Inventory = Master.inventory;
                        CharacterBody b = Master.GetBody();
                        Body = b;
                        if (b != null)
                        {
                            Health = b.healthComponent;
                            Skills = b.skillLocator;
                            if (b != _cachedBody)
                            {
                                _cachedBody = b;
                                Motor = b.GetComponent<CharacterMotor>();
                                InputBank = b.GetComponent<InputBankTest>();
                            }
                            return;
                        }
                    }
                }
            }

            User = null;
            Master = null;
            Body = null;
            Inventory = null;
            Health = null;
            Skills = null;
            Motor = null;
            InputBank = null;
            _cachedBody = null;
        }

        internal static Ray AimRay()
        {
            if (InputBank != null)
                return new Ray(InputBank.aimOrigin, InputBank.aimDirection);
            Camera cam = Camera.main;
            return cam != null ? cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f)) : default;
        }
    }
}
