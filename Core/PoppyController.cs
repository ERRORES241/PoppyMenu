using System.Collections.Generic;
using UnityEngine;

namespace PoppyMenu
{
    internal class PoppyController : MonoBehaviour
    {
        private readonly List<PoppyModule> _modules = new List<PoppyModule>();
        private readonly List<TabGroup> _groups = new List<TabGroup>();
        private bool _catalogsTried;
        private string _lastScene;
        private static bool _startupConfigApplied;

        private static string ActiveScene() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        private void Awake()
        {
            _groups.Add(new TabGroup("Aimbot", new AimbotModule()));
            _groups.Add(new TabGroup("Visuals", new RenderModule()));
            _groups.Add(new TabGroup("Movement", new MovementModule()));
            _groups.Add(new TabGroup("Player", new PlayerModule(), new StatsModule(), new ItemsModule()));
            _groups.Add(new TabGroup("World", new WorldModule(), new TeleporterModule(), new SpawnModule(), new RunModule(), new PlayersModule()));
            _groups.Add(new TabGroup("Fun", new FunModule()));
            _groups.Add(new TabGroup("Settings", new ConfigsModule(), new KeybindsModule(), new SettingsModule(), new MacrosModule(), new ConsoleModule()));

            foreach (TabGroup g in _groups) _modules.AddRange(g.Pages);

            gameObject.AddComponent<CursorOverlay>();
        }

        private void Start()
        {
            if (!_startupConfigApplied)
            {
                _startupConfigApplied = true;
                try { ConfigsModule.ApplyStartupConfig(); } catch (System.Exception e) { Log.Error(e); }
            }
        }

        private void Update()
        {
            PlayerContext.Refresh();

            if (PlayerContext.InGame)
            {
                if (!Catalogs.Ready && !_catalogsTried)
                {
                    _catalogsTried = true;
                    try { Catalogs.Refresh(); } catch (System.Exception e) { Log.Error(e); }
                    _lastScene = ActiveScene();
                }
                else if (Catalogs.Ready)
                {
                    string scene = ActiveScene();
                    if (scene != _lastScene)
                    {
                        _lastScene = scene;
                        try { Catalogs.RefreshSpawnCards(); } catch (System.Exception e) { Log.Error(e); }
                    }
                }
            }

            NetUtil.TickGuards();

            Rebind.Poll();
            if (Rebind.IsActive && !MenuRoot.Visible) Rebind.Cancel();

            if (!PlayerContext.InGame) WorldModule.RestoreTime();

            HandleHotkeys();

            InputCapture.Sync(MenuRoot.Visible || ListPicker.IsOpen);

            if (PlayerContext.InGame)
            {
                foreach (PoppyModule m in _modules)
                {
                    try { m.Tick(); }
                    catch (System.Exception e) { Log.Error($"{m.Name}.Tick: {e}"); }
                }
            }
        }

        private void HandleHotkeys()
        {
            if (Rebind.IsActive) return;

            if (Input.GetKeyDown(ModConfig.ToggleMenuKey.Value))
            {
                MenuRoot.Visible = !MenuRoot.Visible;
                if (!MenuRoot.Visible)
                {
                    ListPicker.Close();
                    MenuRoot.SaveLayout();
                }
            }

            BindStore.Poll();
        }

        private void OnGUI()
        {
            Theme.EnsureInit();

            if (PlayerContext.InGame)
            {
                foreach (PoppyModule m in _modules)
                {
                    try { m.DrawOverlay(); }
                    catch {  }
                }
            }

            Notify.Draw();

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * ModConfig.UiScale.Value);
            MenuRoot.Draw(_groups);
        }

        private void LateUpdate()
        {
            if (MenuRoot.Visible || ListPicker.IsOpen)
                Cursor.lockState = CursorLockMode.None;
        }

        private void OnDestroy()
        {
            MenuRoot.Visible = false;
            ListPicker.Close();
            try { InputCapture.Shutdown(); } catch { }
            try { Aim.Shutdown(); } catch { }
            try { Safety.Shutdown(); } catch { }
            try { ConsoleCommands.Shutdown(); } catch { }
            foreach (PoppyModule m in _modules)
            {
                try { m.OnUnload(); } catch { }
            }
        }
    }
}
