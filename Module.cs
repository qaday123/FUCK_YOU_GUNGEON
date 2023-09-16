using BepInEx;
using Alexandria;
using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using HarmonyLib;
using Steamworks;
using System.Collections;
using XInputDotNetPure;
using static XInputDotNetPure.GamePadState.RawState;

namespace FuckYou
{
    [BepInDependency(Alexandria.Alexandria.GUID)] // this mod depends on the Alexandria API: https://enter-the-gungeon.thunderstore.io/package/Alexandria/Alexandria/
    [BepInDependency(ETGModMainBehaviour.GUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    [HarmonyPatch]
    public class Module : BaseUnityPlugin
    {
        public const string GUID = "qna.etg.fuckyou";
        public const string NAME = "FUCK YOU GUNGEON";
        public const string VERSION = "1.0.0";
        public const string TEXT_COLOR = "#FF0000";
        
        public static Texture2D ModLogo;
        public static bool LogoEnabled = false;

        Hook MainMenuFoyerUpdateHook;
        Hook DisableQuickStart;
        Hook PreventQuickStartLabel;

        public void Start()
        {
            new Harmony(GUID).PatchAll();
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }

        public void GMStart(GameManager g)
        {
            //GameObject gameObject = SpriteBuilder.SpriteFromResource("FuckYou/test_logo", null);
            ModLogo = ResourceExtractor.GetTextureFromResource("FuckYou/Resources/fuck_you_logo.png");//gameObject.GetComponent<Texture2D>();
            try
            {
                DisableQuickStart = new Hook(
                    typeof(FinalIntroSequenceManager).GetMethod("HandleBackgroundSkipChecks", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(Module).GetMethod("DisableQuickStartHook", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(FinalIntroSequenceManager)
                );
                PreventQuickStartLabel = new Hook(
                    typeof(FinalIntroSequenceManager).GetMethod("MoveQuickstartOnScreen", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(Module).GetMethod("MakeQuickStartLabelDisappear", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(FinalIntroSequenceManager)
                );
                MainMenuFoyerUpdateHook = new Hook(
                    typeof(MainMenuFoyerController).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(Module).GetMethod("MainMenuUpdateHook", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(MainMenuFoyerController)
                );
            }
            catch (Exception ex)
            {
                // ETGModConsole can't be called by anything that occurs in Init(), so write message to static strinng and check it later.
                Debug.LogException(ex);
                return;
            }
            Log($"If you manage to read this, fuck you.", TEXT_COLOR);
        }

        public static void Log(string text, string color="#FFFFFF")
        {
            ETGModConsole.Log($"<color={color}>{text}</color>");
        }
        private IEnumerator DisableQuickStartHook(Func<FinalIntroSequenceManager, IEnumerator> orig, FinalIntroSequenceManager self)
        {
            yield return null;
            for (; ; )
            {
                /*if (self.QuickStartObject.activeSelf)
                {
                    if (!BraveInput.PlayerlessInstance.IsKeyboardAndMouse(false))
                    {
                        self.QuickStartController.gameObject.SetActive(true);
                        self.QuickStartController.renderer.enabled = true;
                        self.QuickStartKeyboard.gameObject.SetActive(false);
                    }
                    else
                    {
                        self.QuickStartKeyboard.gameObject.SetActive(true);
                        self.QuickStartController.gameObject.SetActive(false);
                    }
                }*/
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    self.m_skipCycle = true;
                }
                if (!self.m_isDoingQuickStart && !self.m_skipCycle)
                {
                    if (self.QuickStartAvailable() && (BraveInput.PlayerlessInstance.ActiveActions.Device.Action4.WasPressed || Input.GetKeyDown(KeyCode.Q)))
                    {
                        self.m_skipCycle = true;
                    }
                    if (BraveInput.PlayerlessInstance.ActiveActions.Device.Action1.WasPressed || BraveInput.PlayerlessInstance.ActiveActions.Device.Action2.WasPressed || BraveInput.PlayerlessInstance.ActiveActions.Device.Action3.WasPressed || BraveInput.PlayerlessInstance.ActiveActions.Device.CommandWasPressed || BraveInput.PlayerlessInstance.ActiveActions.MenuSelectAction.WasPressed)
                    {
                        self.m_skipCycle = true;
                    }
                }
                yield return null;
            }
            yield break;
        }
        private IEnumerator MakeQuickStartLabelDisappear(Func<FinalIntroSequenceManager, IEnumerator> orig, FinalIntroSequenceManager self)
        {
            //ETGModConsole.Log("running hook 2");
            yield break;
        }
        private void MainMenuUpdateHook(Action<MainMenuFoyerController> orig, MainMenuFoyerController self)
        {
            //ETGModConsole.Log("running hook 1");
            orig(self);
            if (((dfTextureSprite)self.TitleCard).Texture.name != ModLogo.name)
            {
                if (ModLogo != null)
                {
                    ((dfTextureSprite)self.TitleCard).Texture = ModLogo;
                    LogoEnabled = true;
                }
            }
            if (!started)
            {
                self.NewGameButton.IsEnabled = false;
                self.NewGameButton.IsVisible = false;
                self.ControlsButton.IsEnabled = false;
                self.ControlsButton.IsVisible = false;
                self.StartCoroutine(EnableButtons(self));
            }
        }
        private IEnumerator EnableButtons(MainMenuFoyerController self)
        {
            //ETGModConsole.Log("running coroutine");
            started = true;
            bool enabled = false;
            int count = 0;
            while (!enabled)
            {
                if (BraveInput.PlayerlessInstance.IsKeyboardAndMouse(false))
                {
                    if (Input.anyKeyDown)
                    {
                        //ETGModConsole.Log("key pressed");
                        //AkSoundEngine.PostEvent("Play_WPN_peashooter_shot_01", self.gameObject);
                        if (count < 10)
                        {
                            if (Input.GetKeyDown(KeyboardInputs[count]))//KeyboardInputs[count]))
                            {
                                count++;
                            }
                            else if (!Input.GetKeyDown(KeyboardInputs[count]))
                            {
                                //AkSoundEngine.PostEvent("Play_WPN_peashooter_reload_01", self.gameObject);
                                count = 0;
                            }
                        }
                        if (count >= 10) enabled = true;
                    }
                }
                else
                {
                    //var gamepad = Gamepad.current;
                    if (BraveInput.PlayerlessInstance.ActiveActions.AnyActionPressed())
                    {
                        //ETGModConsole.Log(count);
                        if (count < 8)
                        {
                            if (count == 0 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadUp.WasPressed) count++;
                            else if (count == 1 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadUp.WasPressed) count++;
                            else if (count == 2 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadDown.WasPressed) count++;
                            else if (count == 3 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadDown.WasPressed) count++;
                            else if (count == 4 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadLeft.WasPressed) count++;
                            else if (count == 5 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadRight.WasPressed) count++;
                            else if (count == 6 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadLeft.WasPressed) count++;
                            else if (count == 7 && BraveInput.PlayerlessInstance.ActiveActions.Device.DPadRight.WasPressed) count++;
                            else count = 0;
                        }
                        if (count >= 8) enabled = true;
                    }
                }
                yield return null;
            }
            //AkSoundEngine.PostEvent("Play_UI_menu_select_01", GameManager.Instance.gameObject);
            self.NewGameButton.IsEnabled = true;
            self.NewGameButton.IsVisible = true;
            self.ControlsButton.IsEnabled = true;
            self.ControlsButton.IsVisible = true;
            yield break;
        }
        public static List<KeyCode> KeyboardInputs = new List<KeyCode>()
        {
            KeyCode.UpArrow,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.B,
            KeyCode.A
        };
        /*public static List<InControl.InputControl> ControllerInputs = new List<InControl.InputControl>()
        {
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadUp,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadUp,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadDown,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadDown,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadLeft,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadRight,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadLeft,
            BraveInput.PlayerlessInstance.ActiveActions.Device.DPadRight
        };*/


        public bool started = false;
    }
}
