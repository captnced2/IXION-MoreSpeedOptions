using System.Linq;
using BulwarkStudios.Stanford.Common.UI;
using BulwarkStudios.Stanford.Core.GameStates;
using HarmonyLib;
using UnityEngine;

namespace MoreSpeedOptions;

public class Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game.SetState))]
    public static class GameStatePatcher
    {
        public static void Postfix(Game.STATE state)
        {
            if (state == Game.STATE.PLAYING)
                if (!GameObject.Find(ButtonManager.buttons.First().name))
                {
                    ButtonManager.setupDefaultButtons();
                    foreach (var b in ButtonManager.buttons) b.create();
                    ButtonManager.fixCyclesSize();
                }
        }
    }

    [HarmonyPatch(typeof(UICycleClockIndicator), nameof(UICycleClockIndicator.RefreshCycleText))]
    public static class CycleTextPatcher
    {
        public static void Postfix(UICycleClockIndicator __instance)
        {
            var zeros = "";
            for (var i = __instance.txtCycleValue.m_characterCount;
                 i < __instance.txtCycleValue.m_characterCount + ButtonManager.buttonsCount * 2;
                 i++) zeros += '0';
            __instance.txtCycleValue.text = "<color=#444444>" + zeros + "</color>" + __instance.txtCycleValue.text;
        }
    }
}