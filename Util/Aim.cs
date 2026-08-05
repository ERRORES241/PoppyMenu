using HarmonyLib;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace PoppyMenu
{
    internal static class Aim
    {
        internal static bool Enabled;
        internal static bool Active;
        internal static HurtBox Target;

        internal static int Sorting;
        internal static bool UseFov;
        internal static float Fov = 60f;
        internal static float MaxRange = 300f;
        internal static bool RequireLoS;
        internal static bool PrioritizeBosses;
        internal static bool Sticky;
        internal static bool Highlight = true;
        internal static bool ShowFovCircle;
        internal static bool MagicBullet;
        internal static bool TargetWeakPoints = true;
        internal static bool NoSpread;
        internal static bool NoRecoil;

        private static Harmony _h;
        private static GUIStyle _label;

        internal static void Init()
        {
            _h = new Harmony("poppy.aimbot");
            var bulletFire = AccessTools.Method(typeof(BulletAttack), nameof(BulletAttack.Fire), new System.Type[0]);
            if (bulletFire != null) _h.Patch(bulletFire, prefix: new HarmonyMethod(typeof(Aim), nameof(BulletFirePrefix)));
            var fireProj = AccessTools.Method(typeof(ProjectileManager), nameof(ProjectileManager.FireProjectile), new[] { typeof(FireProjectileInfo) });
            if (fireProj != null) _h.Patch(fireProj, prefix: new HarmonyMethod(typeof(Aim), nameof(FireProjectilePrefix)));

            var initProj = AccessTools.Method(typeof(ProjectileManager), nameof(ProjectileManager.InitializeProjectile));
            if (initProj != null) _h.Patch(initProj, postfix: new HarmonyMethod(typeof(Aim), nameof(InitProjectilePostfix)));

            var applySpread = AccessTools.Method(typeof(Util), nameof(Util.ApplySpread), new[] { typeof(Vector3), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float) });
            if (applySpread != null) _h.Patch(applySpread, prefix: new HarmonyMethod(typeof(Aim), nameof(ApplySpreadPrefix)));

            var addRecoil = AccessTools.Method(typeof(CameraTargetParams), nameof(CameraTargetParams.AddRecoil), new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            if (addRecoil != null) _h.Patch(addRecoil, prefix: new HarmonyMethod(typeof(Aim), nameof(AddRecoilPrefix)));
        }

        private static bool ApplySpreadPrefix(Vector3 aimDirection, ref Vector3 __result)
        {
            if (NoSpread)
            {
                __result = aimDirection.normalized;
                return false;
            }
            return true;
        }

        private static bool AddRecoilPrefix()
        {
            if (NoRecoil)
            {
                return false;
            }
            return true;
        }

        internal static void Shutdown() { Target = null; _h?.UnpatchSelf(); _h = null; }

        internal static void Tick()
        {
            KeyCode hold = ModConfig.SilentAimKey.Value;
            Active = Enabled && (hold == KeyCode.None || Input.GetKey(hold));
            if (!Active || !PlayerContext.HasBody) { Target = null; return; }

            UpdateTarget();
        }

        private static BodyIndex _railgunnerBodyIndex = BodyIndex.None;
        private static bool _railgunnerIndexCached;

        private static bool IsRailgunner(CharacterBody me)
        {
            if (me == null) return false;
            if (!_railgunnerIndexCached)
            {
                _railgunnerBodyIndex = BodyCatalog.FindBodyIndex("RailgunnerBody");
                _railgunnerIndexCached = true;
            }
            if (_railgunnerBodyIndex != BodyIndex.None && me.bodyIndex == _railgunnerBodyIndex)
                return true;
            return false;
        }

        private static HurtBox GetTargetHurtBox(CharacterBody body, bool preferWeakPoint)
        {
            if (body == null) return null;
            HurtBox main = Util.FindBodyMainHurtBox(body);
            if (main == null && body.mainHurtBox != null) main = body.mainHurtBox;
            if (main == null && body.modelLocator != null && body.modelLocator.modelTransform != null)
            {
                var group = body.modelLocator.modelTransform.GetComponent<HurtBoxGroup>();
                if (group != null && group.mainHurtBox != null) main = group.mainHurtBox;
                else if (group != null && group.hurtBoxes != null && group.hurtBoxes.Length > 0)
                {
                    for (int i = 0; i < group.hurtBoxes.Length; i++)
                    {
                        if (group.hurtBoxes[i] != null && group.hurtBoxes[i].gameObject.activeInHierarchy)
                        {
                            main = group.hurtBoxes[i];
                            break;
                        }
                    }
                }
            }

            if (preferWeakPoint)
            {
                if (main != null && main.hurtBoxGroup != null && main.hurtBoxGroup.hurtBoxes != null)
                {
                    HurtBox[] boxes = main.hurtBoxGroup.hurtBoxes;
                    for (int i = 0; i < boxes.Length; i++)
                    {
                        HurtBox hb = boxes[i];
                        if (hb != null && hb.isSniperTarget && hb.gameObject.activeInHierarchy)
                            return hb;
                    }
                }

                try
                {
                    var list = HurtBox.readOnlySniperTargetsList;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            HurtBox hb = list[i];
                            if (hb != null && hb.healthComponent != null && hb.healthComponent.body == body && hb.gameObject.activeInHierarchy)
                                return hb;
                        }
                    }
                }
                catch { }

                if (main != null && main.hurtBoxGroup != null && main.hurtBoxGroup.hurtBoxes != null)
                {
                    HurtBox[] boxes = main.hurtBoxGroup.hurtBoxes;
                    for (int i = 0; i < boxes.Length; i++)
                    {
                        HurtBox hb = boxes[i];
                        if (hb != null && hb.damageModifier == HurtBox.DamageModifier.Weak && hb.gameObject.activeInHierarchy)
                            return hb;
                    }
                }
            }

            return main;
        }

        private static void UpdateTarget()
        {
            CharacterBody me = PlayerContext.Body;
            if (me == null) { Target = null; return; }

            Ray aim = PlayerContext.AimRay();
            Camera cam = Camera.main;

            if (Sticky && IsValidTarget(aim, cam)) return;

            Target = null;
            TeamIndex myTeam = me.teamComponent != null ? me.teamComponent.teamIndex : TeamIndex.Player;
            float rangeSqr = MaxRange * MaxRange;
            float bestScore = float.MaxValue;
            bool bestIsBoss = false;

            bool preferWeakPoint = TargetWeakPoints && IsRailgunner(me) && me.inputBank != null && (me.inputBank.skill2.down || me.inputBank.skill4.down);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            float fovRadiusPixels = float.MaxValue;
            if (UseFov && cam != null)
            {
                float halfFovRad = Mathf.Clamp(Fov * 0.5f, 0.5f, 89.9f) * Mathf.Deg2Rad;
                float camHalfFovRad = Mathf.Clamp(cam.fieldOfView * 0.5f, 0.5f, 89.9f) * Mathf.Deg2Rad;
                fovRadiusPixels = Screen.height * 0.5f * (Mathf.Tan(halfFovRad) / Mathf.Tan(camHalfFovRad));
            }

            var bodies = CharacterBody.readOnlyInstancesList;
            if (bodies == null) return;

            for (int i = 0; i < bodies.Count; i++)
            {
                CharacterBody body = bodies[i];
                if (!IsHostile(me, myTeam, body)) continue;

                HurtBox hb = GetTargetHurtBox(body, preferWeakPoint);
                if (hb == null) continue;

                Vector3 targetPos = hb.transform.position;
                Vector3 to = targetPos - aim.origin;
                float distSqr = to.sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                float dist2D = 0f;
                float angle = Vector3.Angle(aim.direction, to);

                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(targetPos);
                    if (sp.z <= 0f) continue;

                    dist2D = Vector2.Distance(screenCenter, new Vector2(sp.x, sp.y));
                    if (UseFov && dist2D > fovRadiusPixels) continue;
                }
                else if (UseFov && angle > Fov * 0.5f)
                {
                    continue;
                }

                if (RequireLoS)
                {
                    float dist = Mathf.Sqrt(distSqr);
                    if (dist > 0.1f && Physics.Raycast(aim.origin, to / dist, out RaycastHit hit, dist - 0.2f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                }

                bool boss = body.isBoss;
                if (PrioritizeBosses)
                {
                    if (bestIsBoss && !boss) continue;
                    if (boss && !bestIsBoss)
                    {
                        Target = hb;
                        bestScore = Score(body, angle, dist2D, Mathf.Sqrt(distSqr));
                        bestIsBoss = true;
                        continue;
                    }
                }

                float score = Score(body, angle, dist2D, Mathf.Sqrt(distSqr));
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIsBoss = boss;
                    Target = hb;
                }
            }
        }

        private static float Score(CharacterBody b, float angle, float dist2D, float dist3D)
        {
            switch (Sorting)
            {
                case 1: return dist3D;
                case 2: return b.healthComponent != null ? b.healthComponent.health : float.MaxValue;
                case 3: return b.healthComponent != null ? -b.healthComponent.health : float.MaxValue;
                default: return dist2D > 0f ? dist2D : angle;
            }
        }

        private static bool IsHostile(CharacterBody me, TeamIndex myTeam, CharacterBody body)
        {
            if (body == null || body == me) return false;
            if (body.healthComponent == null || !body.healthComponent.alive) return false;

            TeamIndex t = body.teamComponent != null ? body.teamComponent.teamIndex : TeamIndex.Monster;
            if (t == myTeam) return false;
            if (t == TeamIndex.Player && myTeam == TeamIndex.Player) return false;

            return true;
        }

        private static bool IsValidTarget(Ray aim, Camera cam)
        {
            if (Target == null || Target.healthComponent == null || !Target.healthComponent.alive) return false;
            Vector3 targetPos = Target.transform.position;
            Vector3 to = targetPos - aim.origin;
            if (to.sqrMagnitude > MaxRange * MaxRange) return false;

            if (UseFov)
            {
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(targetPos);
                    if (sp.z <= 0f) return false;

                    float halfFovRad = Mathf.Clamp(Fov * 0.5f, 0.5f, 89.9f) * Mathf.Deg2Rad;
                    float camHalfFovRad = Mathf.Clamp(cam.fieldOfView * 0.5f, 0.5f, 89.9f) * Mathf.Deg2Rad;
                    float fovRadiusPixels = Screen.height * 0.5f * (Mathf.Tan(halfFovRad) / Mathf.Tan(camHalfFovRad));

                    Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    float dist2D = Vector2.Distance(screenCenter, new Vector2(sp.x, sp.y));
                    if (dist2D > fovRadiusPixels) return false;
                }
                else if (Vector3.Angle(aim.direction, to) > Fov * 0.5f)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLocalOwner(GameObject owner)
        {
            CharacterBody me = PlayerContext.Body;
            return me != null && owner != null && owner == me.gameObject;
        }

        private static void BulletFirePrefix(BulletAttack __instance)
        {
            if (!IsLocalOwner(__instance.owner)) return;
            if (MagicBullet)
                __instance.stopperMask = LayerIndex.entityPrecise.mask;
            if (NoSpread)
            {
                __instance.minSpread = 0f;
                __instance.maxSpread = 0f;
                __instance.spreadPitchScale = 0f;
                __instance.spreadYawScale = 0f;
            }
            if (Active && Target != null)
            {
                Vector3 dir = Target.transform.position - __instance.origin;
                if (dir.sqrMagnitude > 0.001f) __instance.aimVector = dir.normalized;
                if (TargetWeakPoints && Target.isSniperTarget && IsRailgunner(PlayerContext.Body))
                {
                    __instance.sniper = true;
                }
            }
        }

        private static void FireProjectilePrefix(ref FireProjectileInfo fireProjectileInfo)
        {
            if (!Active || Target == null || !IsLocalOwner(fireProjectileInfo.owner)) return;
            Vector3 dir = Target.transform.position - fireProjectileInfo.position;
            if (dir.sqrMagnitude > 0.001f) fireProjectileInfo.rotation = Quaternion.LookRotation(dir.normalized);
        }

        private static void InitProjectilePostfix(ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            if (projectileController == null || projectileController.isPrediction) return;
            if (!IsLocalOwner(fireProjectileInfo.owner)) return;

            if (MagicBullet) projectileController.gameObject.AddComponent<PoppyGhost>();

            if (Active && Target != null)
            {
                CharacterBody me = PlayerContext.Body;
                TeamIndex team = me != null && me.teamComponent != null ? me.teamComponent.teamIndex : TeamIndex.Player;
                projectileController.gameObject.AddComponent<PoppyHoming>().Init(Target.transform, team);
            }
        }

        internal static void DrawOverlay()
        {
            if (!Enabled && !Active) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            if (ShowFovCircle && UseFov) DrawFovCircle(cam);
            if (Active && Highlight && Target != null) DrawLock(cam);
        }

        private static void DrawLock(Camera cam)
        {
            Vector3 sp = cam.WorldToScreenPoint(Target.transform.position);
            if (sp.z <= 0f) return;
            float y = Screen.height - sp.y;
            Color c = Theme.Accent;
            const float s = 16f, t = 2f, len = 8f;
            Theme.Fill(new Rect(sp.x - s, y - s, len, t), c); Theme.Fill(new Rect(sp.x - s, y - s, t, len), c);
            Theme.Fill(new Rect(sp.x + s - len, y - s, len, t), c); Theme.Fill(new Rect(sp.x + s - t, y - s, t, len), c);
            Theme.Fill(new Rect(sp.x - s, y + s - t, len, t), c); Theme.Fill(new Rect(sp.x - s, y + s - len, t, len), c);
            Theme.Fill(new Rect(sp.x + s - len, y + s - t, len, t), c); Theme.Fill(new Rect(sp.x + s - t, y + s - len, t, len), c);

            if (_label == null) _label = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _label.normal.textColor = c;
            GUI.Label(new Rect(sp.x - 40f, y - s - 16f, 80f, 14f), "LOCKED", _label);
        }

        private static void DrawLine(Vector2 p1, Vector2 p2, Color color, float width)
        {
            Matrix4x4 prevMatrix = GUI.matrix;
            Vector2 d = p2 - p1;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float length = d.magnitude;

            GUIUtility.RotateAroundPivot(angle, p1);
            Theme.Fill(new Rect(p1.x, p1.y - width * 0.5f, length, width), color);
            GUI.matrix = prevMatrix;
        }

        private static void DrawFovCircle(Camera cam)
        {
            float halfFovRad = Mathf.Clamp(Fov * 0.5f, 0.5f, 89.9f) * Mathf.Deg2Rad;
            float camHalfFovRad = Mathf.Clamp(cam.fieldOfView * 0.5f, 0.5f, 89.9f) * Mathf.Deg2Rad;
            float r = Screen.height * 0.5f * (Mathf.Tan(halfFovRad) / Mathf.Tan(camHalfFovRad));
            r = Mathf.Clamp(r, 8f, Screen.height * 1.5f);

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            Color col = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.75f);
            const int seg = 64;
            Vector2 prevPoint = new Vector2(cx + r, cy);
            for (int i = 1; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector2 nextPoint = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
                DrawLine(prevPoint, nextPoint, col, 1.5f);
                prevPoint = nextPoint;
            }
        }
    }
}
