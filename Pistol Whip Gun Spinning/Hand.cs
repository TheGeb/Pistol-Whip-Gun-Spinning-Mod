using UnityEngine;
using MelonLoader;

namespace Pistol_Whip_Gun_Spinning
{
    enum State
    {
        Spinning,   //Currently spinning without any reason to stop
        Returning,  //Returning to forward position
        Stopped     //At forward position
    }

    //CH bindings for each hand
    enum Activator
    {
        LeftX = 0,
        LeftY = 1,
        LeftTrigger = 8,
        LeftGrip = 10,
        LeftA = 10,  //Mapping grip is the same as the A button
        RightX = 3,
        RightY = 4,
        RightTrigger = 9,
        RightGrip = 11,
        RightA = 11  //Mapping grip is the same as the A button
    }

    class Hand
    {
        //public Side side;
        public State state = State.Stopped;
        public Activator activator;

        //settings
        /*
        public float sensitivity = 150f; //make this number higher if you want the gun to spin easier.
        public float speedDecay = 0.25f; //This is the percent of speed lost per second as a decimal
        public float maxSpeed = 1500; //determines max rotation speed in Degrees per second
        */
        public float sensitivity = MelonPrefs.GetFloat("Pistol_Whip_Gun_Spinning", "sensitivity"); //make this number higher if you want the gun to spin easier.
        public float speedDecay = MelonPrefs.GetFloat("Pistol_Whip_Gun_Spinning", "speedDecay"); //This is the percent of speed lost per second as a decimal
        public float maxSpeed = MelonPrefs.GetFloat("Pistol_Whip_Gun_Spinning", "maxSpeed"); //determines max rotation speed in Degrees per second

        //angle stuff
        public Vector3 returnPos;
        public Vector3 endPos;
        public Quaternion returnAngle; //starting gun angle with no spin
        public Quaternion endAngle;

        //unity objects
        public GameObject hand;
        public Transform transform;
        public CHKinematicVelocityEstimator vTracker;
        public ControllerOffsetMonitor com;

        //velocity stuff
        public float startingSpeed = 0;
        public float currentSpinSpeed = 0;
        public float trackedSpeed = 0;

        //time stuff
        public float totalTime =0; //used for interpolation and velocity decay

        public Hand() { }

        public void Spin()
        {
            //Update controller's angular velocity
            trackedSpeed = sensitivity * Vector3.Dot(vTracker.GetAngularVelocityEstimate(), -1 * hand.transform.right); //not sure what unit angular velocity is given in here.

            if (trackedSpeed * trackedSpeed > maxSpeed*maxSpeed)  //lazy absolute value check for magnitude comparison
            {
               trackedSpeed = (trackedSpeed > maxSpeed ? maxSpeed : -maxSpeed); //handles positive/negative spin
            }

            if (trackedSpeed * trackedSpeed > currentSpinSpeed * currentSpinSpeed) //if we are rotating faster than the current rotation, move at the new speed
            {
                currentSpinSpeed = trackedSpeed;
                startingSpeed = currentSpinSpeed;
                totalTime = 0;
            }

            //transform.localRotation *= Quaternion.AngleAxis(currentSpinSpeed * Time.deltaTime, Vector3.left);

            transform.RotateAround( transform.TransformPoint(new Vector3(0,0.041f,0.061f)), transform.TransformDirection(Vector3.left), currentSpinSpeed * Time.deltaTime);
                //Rotate around trigger LocalX? /UnityXR_VRCameraRig(Clone)/TrackingSpace/Right Hand/Pointer/Player Gun (Desert Eagle)(Clone)/Pivot/Recoil Pivot/deagle/deagle_trigger
                //create gameobject at 0.41, 0.41, then rotate parent around gameobject X Axis

            totalTime += Time.deltaTime;
            currentSpinSpeed = (1 - speedDecay * totalTime)  * startingSpeed;

            if (totalTime > (1 / speedDecay))    //prevents decay calculation from speeding up the gun in the opposite direction
            {
                InitReturn();
            }
        }

        public void Recoil()
        {
            currentSpinSpeed += (currentSpinSpeed > 0 ? (maxSpeed - currentSpinSpeed) : (-maxSpeed - currentSpinSpeed)) * 0.25f;
            startingSpeed = currentSpinSpeed;
            totalTime = 0;
        }

        public float getInput(int id)
        {
            return Input.GetAxisRaw("joystick_axis_" + id);
        }

        public void InitReturn()
        {
            state = State.Returning;
            endAngle = transform.localRotation;
            endPos = transform.localPosition;
            currentSpinSpeed = 0; 
            totalTime = 0; //start the counter for rapid spin return
        }

        public void InitSpin()
        {
            state = State.Spinning;
        }

        public void Return()
        {
            totalTime += Time.deltaTime;
            if (totalTime * 12f < 1f) //return spins currently happen in 1/12 of a second, 1 represents perfectly restored angle
            {
                transform.transform.localRotation = Quaternion.Slerp(endAngle, returnAngle, totalTime * 12f); //TODO: Have this depend on spin direction?
                transform.transform.localPosition = Vector3.Slerp(endPos, returnPos, totalTime * 12f);

                //transform.RotateAround(transform.TransformPoint(new Vector3(0, 0.041f, 0.061f)), transform.TransformDirection(Vector3.left), currentSpinSpeed * totalTime);
            }
            else
            {
                //Snap to natural position to avoid overshoot
                transform.transform.localRotation = returnAngle;
                transform.transform.localPosition = returnPos;
                state = State.Stopped;
                totalTime = 0;
            }
        }

    }
}