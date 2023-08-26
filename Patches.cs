using HarmonyLib;
using Reptile;
using UnityEngine;
using System.Reflection;
using System;
using static Reptile.GraffitiGame;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace QuickGraffiti
{
    internal class Patches {

        [HarmonyPatch(typeof(GraffitiGame))]
        [HarmonyPatch("SetState")]
        class Patch_GraffitiGame_SetState
        {
            static bool Prefix(GraffitiGame __instance, GraffitiGameState setState)
            {
                if (setState == GraffitiGameState.SHOW_PIECE)
                {
                    Traverse.Create(__instance).Field("state").SetValue(setState);

                    GraffitiSpot gSpot = Traverse.Create(__instance).Field("gSpot").GetValue<GraffitiSpot>();
                    // Its gonna be a small graffiti, there is only one of them per character so nothing to randomize
                    if (gSpot.size == GraffitiSize.S)
                        return true;

                    GraffitiArtInfo graffitiArtInfo = Traverse.Create(__instance).Field("graffitiArtInfo").GetValue<GraffitiArtInfo>();
                    Player player = Traverse.Create(__instance).Field("player").GetValue<Player>();

                    List<GraffitiArt> art = graffitiArtInfo.FindBySize(gSpot.size);
                    GraffitiArt grafArt = art[UnityEngine.Random.Range(0, art.Count)];
                    Traverse.Create(__instance).Field("grafArt").SetValue(grafArt);


                    Player.TrickType type;
                    switch (gSpot.size)
                    {
                        case GraffitiSize.S:
                        default:
                            type = Player.TrickType.GRAFFITI_S;
                            break;

                        case GraffitiSize.M:
                            type = Player.TrickType.GRAFFITI_M;
                            break;

                        case GraffitiSize.L:
                            type = Player.TrickType.GRAFFITI_L;
                            break;

                        case GraffitiSize.XL:
                            type = Player.TrickType.GRAFFITI_XL;
                            break;
                    }

                    player.DoTrick(type, grafArt.title);

                    //gSpot.Paint(Crew.PLAYERS, grafArt);
                    MethodInfo paintMethod = gSpot.GetType().GetMethod("Paint", BindingFlags.NonPublic | BindingFlags.Instance);
                    paintMethod.Invoke(gSpot, new object[] { Crew.PLAYERS, grafArt, null });

                    gSpot.GiveRep();

                    MethodInfo SetStateVisualMethod = __instance.GetType().GetMethod("SetStateVisual", BindingFlags.NonPublic | BindingFlags.Instance);
                    SetStateVisualMethod.Invoke(__instance, new object[] { setState });
                    return false;
                }
;
                return true;
            }
        }

        [HarmonyPatch(typeof(GraffitiGame))]
        [HarmonyPatch("SetStateVisual")]
        class Patch_GraffitiGame_SetStateVisual
        {
            static bool Prefix(GraffitiGame __instance, GraffitiGameState setState)
            {
                if (setState == GraffitiGameState.COMPLETE_TARGETS)
                {
                    AudioManager audioManager = Traverse.Create(__instance).Field("audioManager").GetValue<AudioManager>();
                    MethodInfo PlaySfxGameplayMethod = audioManager.GetType().GetMethod(
                        "PlaySfxGameplay",
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        new Type[] { typeof(SfxCollectionID), typeof(AudioClipID), typeof(float) },
                        null
                    );

                    PlaySfxGameplayMethod.Invoke(audioManager, new object[] { SfxCollectionID.GraffitiSfx, AudioClipID.graffitiSlash, 0f });

                    Transform closeUpCameraTf = Traverse.Create(__instance).Field("closeUpCameraTf").GetValue<Transform>();
                    closeUpCameraTf.GetComponent<Camera>().targetTexture = new RenderTexture(closeUpCameraTf.GetComponent<Camera>().pixelWidth, closeUpCameraTf.GetComponent<Camera>().pixelHeight, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt);

                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GraffitiGame))]
        [HarmonyPatch("Update")]
        class Patch_GraffitiGame_Update
        {
            static DateTime last_graffiti;
            static bool Prefix(GraffitiGame __instance)
            {
                //Debug.Log("GraffitiGame::Update()");
                GraffitiGameState state = Traverse.Create(__instance).Field("state").GetValue<GraffitiGameState>();
                MethodInfo SetStateMethod = __instance.GetType().GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);

                if (state == GraffitiGameState.MAIN_STATE)
                {
                    if (DateTime.Now < last_graffiti.AddSeconds(__instance.TimePerTarget))
                    {
                        // We've sprayed too quickly, prematurely end this game.
                        // We cannot do this in Init or InitVisual because of race conditions.
                        GraffitiSpot gSpot = Traverse.Create(__instance).Field("gSpot").GetValue<GraffitiSpot>();
                        gSpot.SetState(GraffitiState.CANCEL);
                        __instance.End();
                    }
                    else
                    {
                        last_graffiti = DateTime.Now;
                        SetStateMethod.Invoke(__instance, new object[] { GraffitiGameState.COMPLETE_TARGETS });
                    }

                    return false;
                }
                else if (state == GraffitiGameState.COMPLETE_TARGETS)
                {
                    SetStateMethod.Invoke(__instance, new object[] { GraffitiGameState.SHOW_PIECE });
                    return false;
                }
                else if (state == GraffitiGameState.SHOW_PIECE)
                {
                    SetStateMethod.Invoke(__instance, new object[] { GraffitiGameState.FINISHED });
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(GraffitiGame))]
        [HarmonyPatch(nameof(GraffitiGame.InitVisual))]
        class Patch_GraffitiGame_InitVisual
        {
            static void Postfix(GraffitiGame __instance)
            {
                Transform graffitiCameraTf = Traverse.Create(__instance).Field("graffitiCameraTf").GetValue<Transform>();
                graffitiCameraTf.gameObject.SetActive(value: false);
            }
        }

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.EndGraffitiMode))]
        class Patch_Player_EndGraffitiMode
        {
            static void Postfix(Player __instance)
            {
                // Prevents can caps from being spammed
                Traverse.Create(__instance).Field("newSpraycan").SetValue(false);
            }
        }
    }
}
