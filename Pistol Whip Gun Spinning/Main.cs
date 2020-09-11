using MelonLoader;
using UnityEngine;
using Harmony;
using UnhollowerRuntimeLib;
using Valve.VR;

namespace Pistol_Whip_Gun_Spinning
{

    /*
    var hand = GameObject.Find("/UnityXR_VRCameraRig(Clone)/TrackingSpace/Left Hand/Pointer").gameObject;

    var com = hand.GetComponent(Il2CppType.Of<ControllerOffsetMonitor>()).TryCast<ControllerOffsetMonitor>();

    var returnAngle = com.rotationalOffset * com.userRotationalOffset;
    returnAngle.ToString();

    returnAngle.eulerAngles.ToString();
    */

    public class GunSpinning : MelonMod
    {

        static Hand[] hands;
        static bool started = false;
        static bool updateOffsets = false;
        static bool steamVR = true;

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

        [HarmonyPatch(typeof(ControllerOffsetMonitor), "OnOffsetsUpdated", new System.Type[] { })]    //Grabs controllers when they become available
        public static class CallUpdateOffsets
        {
            public static void Postfix()
            {
                updateOffsets = true;
            }
        }


        [HarmonyPatch(typeof(ControllerOffsetMonitor), "Update", new System.Type[] { })]    //Grabs controllers when they become available
        public static class UpdateOffsets
        {
            public static void Postfix()
            {
                if (updateOffsets)
                {
                    hands[0].returnAngle = hands[0].com.rotationalOffset * hands[1].com.userRotationalOffset;   //MESSY FIX: Left seems to update first, so we get userRotationalOffset off of that one on update
                    hands[1].returnAngle = hands[1].com.rotationalOffset * hands[1].com.userRotationalOffset;
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
                    hands[0].activator = Activator.RightGrip;   //TODO: Test this for oculus
                    hands[0].transform = GameObject.Find("RightControllerAnchor").transform.Find("Pointer").transform;
                    hands[0].vTracker = hands[0].hand.GetComponent(Il2CppType.Of<CHKinematicVelocityEstimator>()).TryCast<CHKinematicVelocityEstimator>();
                    hands[0].com = hands[0].transform.gameObject.GetComponent(Il2CppType.Of<ControllerOffsetMonitor>()).TryCast<ControllerOffsetMonitor>();

                    //Left Hand Setup
                    hands[1] = new Hand();
                    hands[1].hand = GameObject.Find("LeftHandAnchor");
                    //MelonLogger.Log("Left Hand Ready!");
                    hands[1].activator = Activator.LeftGrip;   //TODO: Test this for oculus
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

        //required for compatibility with ricochet mod
        public static bool hasReset = true;

        [HarmonyPatch(typeof(Gun), "Fire", new System.Type[] { })]
        public static class Fire
        {
            static Quaternion[] localRotations = new Quaternion[2];

            public static void Prefix()
            {
                if (hasReset)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (hands[i].state == State.Spinning) //if we fire while spinning, shoot where you're actually pointing
                        {
                            localRotations[i] = hands[i].transform.localRotation;
                            hands[i].transform.localRotation = hands[i].returnAngle;
                        }
                    }
                    hasReset = false; //skips the first spin prefix when a ricochet occurs so localrotations don't get overwritten
                }
            }

            public static void Postfix()
            {

                for (int i = 0; i < 2; i++)
                {
                    //MelonLogger.Log(Quaternion.Angle(hands[i].transform.localRotation, hands[i].returnAngle).ToString());
                    if (hands[i].state == State.Spinning) //if we fire while spinning, bring it to an end
                    {
                        hands[i].transform.localRotation = localRotations[i];
                        hands[i].totalTime = hands[i].totalTime * 0.8f;   //Shooting the gun counteracts decay a bit for longer spins and more fun :P
                    }
                }

                if (!hasReset)
                {
                    hasReset = true; //allows the second spin prefix when a ricochet occurs
                }
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
