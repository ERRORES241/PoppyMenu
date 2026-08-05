using KinematicCharacterController;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class MovementModule : PoppyModule
    {
        internal override string Name => "Move";

        internal static bool Flight;
        internal static bool NoClip;
        internal static bool AlwaysSprint;
        internal static bool JumpPack;
        internal static bool Bhop;

        internal static void ToggleFlight() => Flight = !Flight;
        internal static void ToggleSprint() => AlwaysSprint = !AlwaysSprint;
        internal static void ToggleNoClip() => NoClip = !NoClip;

        private bool _gravityOff;
        private bool _collisionsDisabled;
        private KinematicCharacterMotor _kcm;
        private CharacterBody _lastBody;

        internal override void Tick()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody) return;

            CharacterMotor motor = PlayerContext.Motor;
            CharacterBody body = PlayerContext.Body;

            if (body != _lastBody)
            {
                _lastBody = body;
                _kcm = body.GetComponent<KinematicCharacterMotor>();
                _collisionsDisabled = false;
                _gravityOff = false;
            }

            if (_kcm != null && NoClip != _collisionsDisabled)
            {
                SetCollisions(!NoClip);
                _collisionsDisabled = NoClip;
            }

            bool flying = Flight || NoClip;
            if (flying && motor != null)
            {
                motor.useGravity = false;
                motor.disableAirControlUntilCollision = false;
                _gravityOff = true;

                Vector3 move = Vector3.zero;
                Camera cam = Camera.main;
                if (cam != null && !InputCapture.Active)
                {
                    Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
                    Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();
                    move = fwd * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal");
                    if (Input.GetKey(KeyCode.Space)) move.y += 1f;
                    if (Input.GetKey(KeyCode.LeftControl)) move.y -= 1f;
                }

                if (move.sqrMagnitude > 0.01f)
                {
                    if (move.sqrMagnitude > 1f) move.Normalize();
                    motor.velocity = move * ModConfig.FlightSpeed.Value;
                }
                else
                {
                    motor.velocity = Vector3.Lerp(motor.velocity, Vector3.zero, 12f * Time.deltaTime);
                }
            }
            else if (motor != null && _gravityOff)
            {
                motor.useGravity = true;
                _gravityOff = false;
            }

            if (AlwaysSprint && body != null)
            {
                bool isMoving = (body.inputBank != null && body.inputBank.moveVector.sqrMagnitude > 0.01f) || (motor != null && motor.velocity.sqrMagnitude > 0.1f);
                bool attacking = body.inputBank != null && (body.inputBank.skill1.down || body.inputBank.skill2.down || body.inputBank.skill3.down || body.inputBank.skill4.down);
                if (isMoving && !attacking)
                    body.isSprinting = true;
            }

            if (JumpPack && motor != null && !InputCapture.Active && Input.GetKeyDown(KeyCode.Space) && !flying)
            {
                Vector3 v = motor.velocity; v.y = 40f; motor.velocity = v;
            }

        } // End of Tick()

        internal static void Init()
        {
            On.RoR2.PlayerCharacterMasterController.FixedUpdate += PlayerCharacterMasterController_FixedUpdate;
        }

        private static void PlayerCharacterMasterController_FixedUpdate(On.RoR2.PlayerCharacterMasterController.orig_FixedUpdate orig, PlayerCharacterMasterController self)
        {
            orig(self);

            if (Bhop && self.body != null && !InputCapture.Active)
            {
                CharacterMotor motor = self.body.characterMotor;
                InputBankTest input = self.bodyInputs;

                // If player is holding jump and lands on the ground, trick the game into thinking jump was JUST pressed!
                // This triggers the 100% native RoR2 jump (with proper Wax Quail math, animations, sounds, and state machine transitions).
                if (motor != null && input != null && motor.isGrounded && input.jump.down)
                {
                    input.jump.PushState(false);
                    input.jump.PushState(true);
                }
            }
        }

        private void SetCollisions(bool on)
        {
            if (_kcm == null) return;
            _kcm.SetCapsuleCollisionsActivation(on);
            _kcm.SetMovementCollisionsSolvingActivation(on);
            _kcm.SetGroundSolvingActivation(on);
        }

        internal override void OnUnload()
        {
            if (_kcm != null && _collisionsDisabled) SetCollisions(true);
            if (PlayerContext.Motor != null && _gravityOff) PlayerContext.Motor.useGravity = true;
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Movement");
            Flight = Widgets.Toggle("Flight", Flight);
            NoClip = Widgets.Toggle("No-Clip (fly through walls)", NoClip);
            AlwaysSprint = Widgets.Toggle("Always Sprint", AlwaysSprint);
            JumpPack = Widgets.Toggle("Jump Pack", JumpPack);
            Bhop = Widgets.Toggle("BunnyHop (Auto-jump on land)", Bhop);
            ModConfig.FlightSpeed.Value = Widgets.Slider("Fly Speed", ModConfig.FlightSpeed.Value, 5f, 150f);
            Widgets.Hint("Fly: WASD move, Space up, Left Ctrl down. You can still aim and fire while flying.");
            Widgets.SectionEnd();
        }
    }
}
