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
        public const float speedMultiplier = 175; //change this if you want the gun to spin easier
        public const float speedDecay = 0.25f; //This is the percent of speed lost per second as a decimal
        public const float maxSpeed = 7.5f; //determines max rotation speed - Doesn't really have units as far as I know

        //angle stuff
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
            trackedSpeed = Vector3.Dot(vTracker.GetAngularVelocityEstimate(), -1 * hand.transform.right);

            if (trackedSpeed * trackedSpeed > maxSpeed*maxSpeed)  //lazy absolute value for comparison
            {
               trackedSpeed = (trackedSpeed > maxSpeed ? maxSpeed : -maxSpeed); //handles positive/negative spin
            }

            if (trackedSpeed * trackedSpeed > currentSpinSpeed * currentSpinSpeed)
            {
                currentSpinSpeed = trackedSpeed;
                startingSpeed = currentSpinSpeed;
                totalTime = 0;
            }

            transform.localRotation *= Quaternion.AngleAxis(currentSpinSpeed * speedMultiplier * Time.deltaTime, Vector3.left);
            totalTime += Time.deltaTime;
            currentSpinSpeed = (1 - speedDecay * totalTime)  * startingSpeed;

            if (totalTime > (1 / speedDecay))    //prevents decay calculation from speeding up the gun in the opposite direction
            {
                InitReturn();
            }
        }

        public float getInput(int id)
        {
            return Input.GetAxisRaw("joystick_axis_" + id);
        }

        public void InitReturn()
        {
            state = State.Returning;
            endAngle = transform.localRotation;
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
            if (totalTime * 12f < 1f) //spins currently happen in 1/12 of a second, 1 represents perfectly restored angle
            {   
                transform.transform.localRotation = Quaternion.Slerp(endAngle, returnAngle, totalTime * 12f); //TODO: Have this depend on spin direction?
            }
            else
            {
                //Snap to natural position to avoid overshoot
                transform.transform.localRotation = returnAngle;
                state = State.Stopped;
                totalTime = 0;
            }
        }

    }
}