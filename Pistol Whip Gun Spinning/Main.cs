using MelonLoader;
using UnityEngine;
using Harmony;
using UnhollowerRuntimeLib;
using Valve.VR;
using System;

namespace Pistol_Whip_Gun_Spinning
{
    public class GunSpinning : MelonMod
    {

        static Hand[] hands;
        static bool started = false;
        static bool updateOffsets = false;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();

            MelonPrefs.RegisterCategory("Pistol_Whip_Gun_Spinning", "Gun Spinning Prefs");
            MelonPrefs.RegisterFloat("Pistol_Whip_Gun_Spinning", "sensitivity", 150f);
            MelonPrefs.RegisterFloat("Pistol_Whip_Gun_Spinning", "speedDecay", 0.25f);
            MelonPrefs.RegisterFloat("Pistol_Whip_Gun_Spinning", "maxSpeed", 1500f);
        }

        [HarmonyPatch(typeof(UIStateController), "OnPressPauseButtonOnController", new System.Type[] { })]
        public static class SongPause
        {
            private static void Postfix()
            {
                for (int i = 0; i < 2; i++)
                {
                    hands[i].transform.localRotation = hands[i].returnAngle;
                    hands[i].state = State.Stopped;
                }
            }
        }

        [HarmonyPatch(typeof(UIStateController), "ShowDeathCanvas", new System.Type[] { })]
        public static class Dead
        {
            private static void Prefix()
            {
                for (int i = 0; i < 2; i++)
                {
                    hands[i].transform.localRotation = hands[i].returnAngle;
                    hands[i].state = State.Stopped;
                }
            }
        }

        [HarmonyPatch(typeof(ControllerOffsetMonitor), "OnOffsetsUpdated", new System.Type[] { })]
        public static class CallUpdateOffsets
        {
            public static void Postfix()
            {
                updateOffsets = true;
            }
        }


        [HarmonyPatch(typeof(ControllerOffsetMonitor), "Update", new System.Type[] { })]
        public static class UpdateOffsets
        {
            public static void Postfix()
            {
                if (updateOffsets)
                {
                    hands[0].returnAngle = hands[0].com.rotationalOffset * hands[1].com.userRotationalOffset;   //MESSY FIX: Left seems to update first, so we get userRotationalOffset off of that one on update
                    hands[1].returnAngle = hands[1].com.rotationalOffset * hands[1].com.userRotationalOffset;
                    hands[0].returnPos = hands[0].com.positionalOffset + hands[0].com.userPositionalOffset;
                    hands[1].returnPos = hands[1].com.positionalOffset + hands[1].com.userPositionalOffset;
                    updateOffsets = false;

                    //MelonLogger.Log("Right result: " + hands[0].returnAngle.eulerAngles.ToString());
                    //MelonLogger.Log("Left result: " + hands[1].returnAngle.eulerAngles.ToString());

                    //MelonLogger.Log("");

                }
            }
        }


        [HarmonyPatch(typeof(CHKinematicVelocityEstimator), "Awake", new System.Type[] { })]    //Grabs controllers when they become available
        public static class Init
        {
            public static void Postfix()
            {
                started = true;
                hands = new Hand[2];

                if (GameObject.Find("RightHandAnchor") != null)   //Oculus Setup
                {
                    //Right Hand setup
                    hands[0] = new Hand();
                    hands[0].hand = GameObject.Find("RightHandAnchor");
                    //MelonLogger.Log("Right Hand Ready!");
                    hands[0].activator = Activator.RightGrip;
                    hands[0].transform = GameObject.Find("RightControllerAnchor").transform.Find("Pointer").transform;
                    hands[0].vTracker = hands[0].hand.GetComponent(Il2CppType.Of<CHKinematicVelocityEstimator>()).TryCast<CHKinematicVelocityEstimator>();
                    hands[0].com = hands[0].transform.GetComponent(Il2CppType.Of<ControllerOffsetMonitor>()).TryCast<ControllerOffsetMonitor>();

                    //Left Hand Setup
                    hands[1] = new Hand();
                    hands[1].hand = GameObject.Find("LeftHandAnchor");
                    //MelonLogger.Log("Left Hand Ready!");
                    hands[1].activator = Activator.LeftGrip;
                    hands[1].transform = GameObject.Find("LeftControllerAnchor").transform.Find("Pointer").transform;
                    hands[1].vTracker = hands[1].hand.GetComponent(Il2CppType.Of<CHKinematicVelocityEstimator>()).TryCast<CHKinematicVelocityEstimator>();
                    hands[1].com = hands[1].transform.gameObject.GetComponent(Il2CppType.Of<ControllerOffsetMonitor>()).TryCast<ControllerOffsetMonitor>();
                }
                else //SteamVR Setup
                {
                    //Right Hand setup
                    hands[0] = new Hand();
                    hands[0].hand = GameObject.Find("Right Hand");
                    //MelonLogger.Log("Right Hand Ready!");
                    hands[0].activator = Activator.RightGrip;
                    hands[0].transform = hands[0].hand.transform.Find("Pointer").transform;
                    hands[0].vTracker = hands[0].hand.GetComponent(Il2CppType.Of<CHKinematicVelocityEstimator>()).TryCast<CHKinematicVelocityEstimator>();
                    hands[0].com = hands[0].transform.gameObject.GetComponent(Il2CppType.Of<ControllerOffsetMonitor>()).TryCast<ControllerOffsetMonitor>();

                    //Left Hand setup
                    hands[1] = new Hand();
                    hands[1].hand = GameObject.Find("Left Hand");
                    //MelonLogger.Log("Left Hand Ready!");
                    hands[1].activator = Activator.LeftGrip;
                    hands[1].transform = hands[1].hand.transform.Find("Pointer").transform;
                    hands[1].vTracker = hands[1].hand.GetComponent(Il2CppType.Of<CHKinematicVelocityEstimator>()).TryCast<CHKinematicVelocityEstimator>();
                    hands[1].com = hands[1].transform.gameObject.GetComponent(Il2CppType.Of<ControllerOffsetMonitor>()).TryCast<ControllerOffsetMonitor>();
                }
            }
        }

        [HarmonyPatch(typeof(Gun), "Fire", new System.Type[] { })]
        public static class Fire
        {
            static Quaternion[] localRotations = new Quaternion[2];
            static Vector3[] localPositions = new Vector3[2];
            public static bool patchPre = true;
            public static bool patchPost = true;

            public static void Prefix(Gun __instance)
            {
                if (patchPre)
                {
                    if (__instance.BulletCount != 0)
                    {
                        int handNum = getHand(__instance);
                        if (hands[handNum].state == State.Spinning) //if we fire while spinning, shoot where you're actually pointing
                        {
                            localRotations[handNum] = hands[handNum].transform.localRotation;
                            localPositions[handNum] = hands[handNum].transform.localPosition;
                            hands[handNum].transform.localRotation = hands[handNum].returnAngle;
                            hands[handNum].transform.localPosition = hands[handNum].returnPos;

                            //hands[handNum].totalTime = hands[handNum].totalTime * 0.8f;   
                            hands[handNum].Recoil();    //Shooting the gun increases spin speed and and counteracts decay when valid

                        }
                    }
                    else
                    {
                        patchPost = false;  //prevents resetting gun rotation when out of ammo
                    }
                    patchPre = false;   //this will be restored by postfix - Required for ricochet mod
                }
                else
                {
                    patchPost = false;
                }
            }

            public static void Postfix(Gun __instance)
            {
                if (patchPost) //Ready to return to normal spin location, this is needed because of ricochet mod
                {
                    int handNum = getHand(__instance);
                    if (hands[handNum].state == State.Spinning) //if we fire while spinning, bring it to an end
                    {
                        hands[handNum].transform.localRotation = localRotations[handNum];
                        hands[handNum].transform.localPosition = localPositions[handNum];
                    }
                }
                patchPre = true; //re-enables the spin patches to account for ricochets
                patchPost = true;
            }

            public static int getHand(Gun gun)
            {
                if (gun.gameObject.transform.parent.parent.name.ToLower().Contains("right"))
                { return 0; }
                else
                { return 1; }
            }
        }

        public override void OnUpdate()
        {
            if (started)
            {
                for (int i = 0; i < 2; i++)
                {

                    bool pushed = hands[i].getInput((int)hands[i].activator) == 0f; //wacky, but this is how CH makes me do it

                    if (hands[i].state != State.Spinning && pushed)
                    {
                        hands[i].state = State.Spinning;
                        //Hand.returnAngle = hands[i].transform.localRotation;
                    }

                    if (hands[i].state == State.Spinning)
                    {
                        if (!pushed)
                        {
                            hands[i].InitReturn();
                        }
                        else // either start or keep spinning, we don't care which
                        {
                            hands[i].Spin();
                        }
                    }

                    if (hands[i].state == State.Returning)
                    {
                        hands[i].Return(); //go back to normal gun position
                    }
                }
            }
        }
    }
}
