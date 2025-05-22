using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford.Common.UI;
using BulwarkStudios.Stanford.Core.TimeManagement;
using BulwarkStudios.Utils.UI;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using IMHelper;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MoreSpeedOptions;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("IXION.exe")]
[BepInDependency("captnced.IMHelper")]
public class Plugin : BasePlugin
{
    private const string Guid = "captnced.MoreSpeedOptions";
    private const string Name = "MoreSpeedOptions";
    private const string Version = "1.1.0";
    internal new static ManualLogSource Log;
    private static bool enabled;
    private static Harmony harmony;

    public override void Load()
    {
        Log = base.Log;
        harmony = new Harmony(Guid);
        if (IL2CPPChainloader.Instance.Plugins.ContainsKey("captnced.IMHelper")) enabled = ModsMenu.isSelfEnabled();
        if (!enabled)
            Log.LogInfo("Disabled by IMHelper!");
        else
            init();
    }

    private static void init()
    {
        ButtonManager.addButton("Super Faster", "Hotkey for changing the game speed to two times the max", 8,
            KeyCode.Alpha4);
        ButtonManager.addButton("Super Fastest", "Hotkey for changing the game speed to four times the max", 16,
            KeyCode.Alpha5);
        harmony.PatchAll();
        foreach (var patch in harmony.GetPatchedMethods())
            Log.LogInfo("Patched " + patch.DeclaringType + ":" + patch.Name);
        Log.LogInfo("Loaded \"" + Name + "\" version " + Version + "!");
    }

    private static void disable()
    {
        ButtonManager.destroyButtons();
        harmony.UnpatchSelf();
        Log.LogInfo("Unloaded \"" + Name + "\" version " + Version + "!");
    }

    public static void enable(bool value)
    {
        enabled = value;
        if (enabled) init();
        else disable();
    }
}

public class Button
{
    private readonly int speed;

    public Button(string Name, string description, int Speed, KeyCode DefaultKey)
    {
        name = Name;
        speed = Speed;
        key = DefaultKey;
        _ = new SettingsHelper.KeySetting(ButtonManager.buttonSection, Name, description, key, trigger, true);
        ButtonManager.buttonsCount++;
    }

    public KeyCode key { get; set; }
    public string name { get; }

    public void create()
    {
        var superFastButton = GameObject.Find("SuperFast");
        var newButton = Object.Instantiate(superFastButton, superFastButton.transform.parent);
        newButton.name = name;
        newButton.transform.SetAsLastSibling();
        newButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
        newButton.GetComponent<UITooltipHoverHelper>().enabled = false;
        Action buttonClicked = delegate { trigger(); };
        newButton.GetComponent<UiButton>().add_OnTriggered(buttonClicked);
        newButton.GetComponent<UiButton>().overrideState = UiButton.STATE.CUSTOM1;
        Action<TimeManager.TIME_SCALES> timeScaleChanged = delegate { ButtonManager.timeChanged(name); };
        TimeManager.add_OnTimeChanged(timeScaleChanged);
        loadButtonTexture();
    }

    private void loadButtonTexture()
    {
        var button = GameObject.Find(name);
        var stream =
            typeof(Plugin).Assembly.GetManifestResourceStream("MoreSpeedOptions.assets." + name + ".png");
        if (stream == null) return;
        var oldSprite = button.transform.FindChild("Icon").GetComponent<Image>().sprite;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(stream.ReadBytes());
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            oldSprite.textureRectOffset);
        button.transform.FindChild("Icon").GetComponent<Image>().sprite = sprite;
    }

    public void trigger()
    {
        TimeManager.SetTime(TimeManager.TIME_SCALES.X4);
        TimeManager.TimeScale = speed;
        ButtonManager.deselectAllOtherButtons(name);
        var uiButton = GameObject.Find(name).GetComponent<UiButton>();
        uiButton.overridedState = true;
        uiButton.UpdateState(UiButton.STATE.CUSTOM1);
    }
}

public class ButtonManager
{
    public static int buttonsCount;
    public static List<Button> buttons = new();
    public static SettingsHelper.SettingsSection buttonSection = new("MoreSpeedOptions");

    public static void destroyButtons()
    {
        buttonSection.destroySection();
        buttonSection = null;
    }

    public static void addButton(string name, string description, int speed, KeyCode defaultKey)
    {
        buttonSection ??= new SettingsHelper.SettingsSection("MoreSpeedOptions");
        buttons.Add(new Button(name, description, speed, defaultKey));
    }

    public static void fixCyclesSize()
    {
        var cyclesTxt = GameObject.Find("CyclesTxt");
        cyclesTxt.GetComponent<LayoutElement>().preferredWidth = 113 + 29 * buttonsCount;
    }

    public static void setupDefaultButtons()
    {
        foreach (var uiButton in GameObject.Find("SuperFast").transform.parent.GetComponentsInChildren<UiButton>())
        {
            Action buttonSelected = delegate { deselectAllOtherButtons(uiButton.gameObject.name); };
            uiButton.add_OnTriggered(buttonSelected);
        }

        GameObject.Find("Canvas/Pause").GetComponent<UIPauseStripes>();
    }

    public static void timeChanged(string buttonName)
    {
        var uiButton = GameObject.Find(buttonName).GetComponent<UiButton>();
        uiButton.overridedState = false;
        uiButton.UpdateState(UiButton.STATE.NORMAL);
    }


    public static void deselectAllOtherButtons(string except)
    {
        foreach (var uiButton in GameObject.Find("SuperFast").transform.parent.GetComponentsInChildren<UiButton>())
            if (uiButton.gameObject.name != except)
            {
                uiButton.overridedState = false;
                uiButton.UpdateState(UiButton.STATE.NORMAL);
            }
    }
}