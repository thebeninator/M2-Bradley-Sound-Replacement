using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GHPC.Weapons;
using M2BradleyResound;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using FMOD;
using FMODUnity;
using MelonLoader.Utils;
using System.IO;
using GHPC.Audio;
using GHPC.AI;
using GHPC.Camera;
using GHPC.Player;
using GHPC.State;
using NWH.VehiclePhysics;

[assembly: MelonInfo(typeof(Mod), "M2 Bradley Resound", "1.0.0", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace M2BradleyResound
{
    [HarmonyPatch(typeof(WeaponAudio), "FinalStartLoop")]
    public class ReplaceSound
    {
        public static FMOD.Sound sound_exterior;
        public static FMOD.Sound[] sounds = new FMOD.Sound[3]; 

        public static bool Prefix(WeaponAudio __instance)
        {
            if (__instance.SingleShotMode && __instance.SingleShotEventPaths[0] == "event:/Weapons/autocannon_m242_single")
            {
                var corSystem = RuntimeManager.CoreSystem;

                Vector3 vec = __instance.transform.position;

                VECTOR pos = new VECTOR();
                pos.x = vec.x;
                pos.y = vec.y;
                pos.z = vec.z;

                VECTOR vel = new VECTOR();
                vel.x = 0f;
                vel.y = 0f;
                vel.z = 0f;

                bool interior = __instance.IsInterior && __instance == Mod.player_manager.CurrentPlayerWeapon.Weapon.WeaponSound;

                ChannelGroup channelGroup;
                corSystem.createChannelGroup("master", out channelGroup);

                channelGroup.setVolumeRamp(false);
                channelGroup.setMode(MODE._3D_WORLDRELATIVE);

                FMOD.Channel channel;
                corSystem.playSound(interior ? sounds[UnityEngine.Random.Range(0, 1)] : sound_exterior, channelGroup, true, out channel);
                //corSystem.playSound(sound, channelGroup, true, out channel);

                float game_vol = Mod.audio_settings_manager._previousVolume;
                float gun_vol = (interior) ? (game_vol + 0.10f * (game_vol * 10)) : (game_vol + 0.07f * (game_vol * 10));

                channel.setVolume(gun_vol);
                channel.setVolumeRamp(false);
                channel.set3DAttributes(ref pos, ref vel);
                channelGroup.set3DAttributes(ref pos, ref vel);
                channel.setPaused(false);

                return false;
            }

            return true;
        }
    }

    public class Mod : MelonMod
    {
        private GameObject game_manager;
        public static AudioSettingsManager audio_settings_manager;
        public static PlayerInput player_manager;

        public override void OnInitializeMelon() {
            var corSystem = FMODUnity.RuntimeManager.CoreSystem;

            //for (int i = 0; i < 3; i++)
            //{
                corSystem.createSound(Path.Combine(MelonEnvironment.ModsDirectory + "/m2resound", "25mm_shot.wav"), MODE._3D_INVERSETAPEREDROLLOFF, out ReplaceSound.sounds[0]);
                ReplaceSound.sounds[0].set3DMinMaxDistance(35f, 5000f);
            //}

            corSystem.createSound(Path.Combine(MelonEnvironment.ModsDirectory + "/m2resound", "25mm_shot_exterior.wav"), MODE._3D_INVERSETAPEREDROLLOFF, out ReplaceSound.sound_exterior);
            ReplaceSound.sound_exterior.set3DMinMaxDistance(35f, 5000f);
        }

        public override void OnSceneWasLoaded(int idx, string scene_name)
        {
            if (scene_name == "MainMenu2_Scene" || scene_name == "LOADER_MENU" || scene_name == "LOADER_INITIAL" || scene_name == "t64_menu") return;

            game_manager = GameObject.Find("_APP_GHPC_");
            audio_settings_manager = game_manager.GetComponent<AudioSettingsManager>();
            player_manager = game_manager.GetComponent<PlayerInput>();

        }
    }
}
