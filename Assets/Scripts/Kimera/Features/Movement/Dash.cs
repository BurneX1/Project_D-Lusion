using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Features
{
    public class Dash : MonoBehaviour, IActivable, IFeatureSetup, IFeatureUpdate, IFeatureFixedUpdate, IFeatureAction //Other channels
    {
        //Configuration
        [Header("Settings")]
        public Settings settings;
        //Control
        [Header("Control")]
        [SerializeField] private bool active;
        //States
        [Header("States")]
        private bool isDashing;
        private bool isCharging;
        public bool IsDashing { get { return isDashing; } }
        public bool IsCharging { get { return isCharging; } }
        private Vector3 speed;
        private bool tagToStop;
        private Vector3 speedAfterStop;
        //States /Time Management
        private Coroutine dashCharge;
        private float dashCooldownTimer;
        private float dashDurationTimer;
        public float timeToLand => isDashing ? Mathf.Max(dashDurationTimer / timeMultiplier, 0) : -1;
        public float realDashCooldown => Mathf.Max(dashCooldownTimer / timeMultiplier, 0);
        //Properties
        [Header("Properties")]
        //Shared Properties
        public float gravityValue;
        //Self Properties
        public float timeMultiplier;
        public float baseHeight;
        public float gravityMultiplierUpHill;
        public float gravityMultiplierDownHill;
        //Properties /Time Management
        public float chargeTimeSeconds;
        public float dashCooldownSeconds;
        //References
        [Header("References")]
        [SerializeField] private Movement movement;
        //Componentes
        [Header("Components")]
        [SerializeField] private Rigidbody cmp_rigidbody;

        private void Awake()
        {
            //Setup References
            if (!movement) movement = GetComponent<Movement>();

            //Setup Components
            if (!cmp_rigidbody) cmp_rigidbody = GetComponent<Rigidbody>();
        }

        public void SetupFeature(Controller controller)
        {
            settings = controller.settings;

            //Setup Properties
            gravityValue = settings.Search("gravityValue");
            timeMultiplier = settings.Search("timeMultiplier");
            baseHeight = settings.Search("baseHeight");
            gravityMultiplierUpHill = settings.Search("gravityMultiplierUpHill");
            gravityMultiplierDownHill = settings.Search("gravityMultiplierDownHill");
            chargeTimeSeconds = settings.Search("chargeTimeSeconds");
            dashCooldownSeconds = settings.Search("dashCooldownSeconds");

            ToggleActive(true);
        }

        public void UpdateFeature(Controller controller)
        {
            //Decrease Dash Timers When Cooldown is Active
            WaitQueue();
            
            if (!isDashing || isCharging) return;
            
            DashMovement();

            KineticEntity kinetic = controller as KineticEntity;

            if (kinetic == null) return;

            kinetic.currentSpeed = speed.magnitude * timeMultiplier;
        }

        public void FixedUpdateFeature(Controller controller)
        {
            if(!tagToStop) return;
            
            Debug.Log("Stop");
            cmp_rigidbody.AddForce(-cmp_rigidbody.velocity + speedAfterStop, ForceMode.VelocityChange);
            
            tagToStop = false;
            speedAfterStop = Vector3.zero;
        }

        public void FeatureAction(Controller controller, params Setting[] settings)
        {
            if(!active) return;

            TerrainEntity terrain = controller as TerrainEntity;
            if (terrain != null)
            {
                if (!terrain.onGround) return;
            }

            if (settings.Length < 1) return;

            try
            {
                Vector3 position = settings[0].value;
                ChargeDash(position);
            }
            catch
            {
                Debug.LogError("Dash: Invalid Settings");
            }
        }

        #region Dash Logic

        public void ChargeDash(Vector3 position)
        {
            //Check inner state: if the cooldown is not ready or the player is already dashing interrupt
            if (dashCooldownTimer > 0 || isDashing || isCharging) return;

            dashCharge = StartCoroutine(DashToPosition(position));
        }

        private IEnumerator DashToPosition(Vector3 position)
        {
            isCharging = true;

            //Start Calculations
            Vector3 startPosition = transform.position;
            Vector3 direction = position - startPosition;
            direction.y = 0f;

            float height = baseHeight + (position - startPosition).y;

            float flightTime = ProjectileMotion.GetFlightTime(height, gravityValue * gravityMultiplierUpHill) + ProjectileMotion.GetFlightTime(baseHeight, gravityValue * gravityMultiplierDownHill);

            float length = direction.magnitude;

            float newHorizontalSpeed = length / flightTime;

            //Get the speed to reach the target position
            speed = ProjectileMotion.GetStartSpeed(direction.normalized, height, newHorizontalSpeed, gravityValue * gravityMultiplierUpHill);

            //Disable movement while dashing and charging
            DashState(true);

            cmp_rigidbody.velocity = Vector3.zero;

            //Set Cooldown On Charge
            SetTimers(flightTime + chargeTimeSeconds * timeMultiplier);

            //Wait for the charge time
            yield return new WaitForSeconds(chargeTimeSeconds);
            
            isCharging = false;
        }

        private void DashMovement()
        {
            if (dashDurationTimer <= 0)
            {
                if(isDashing) DashState(false);
            }

            float timeDelta = Time.deltaTime * timeMultiplier;

            float gravity = -Mathf.Abs(gravityValue);

            gravity *= speed.y < 0 ? gravityMultiplierDownHill : gravityMultiplierUpHill;

            speed += Vector3.up * gravity * timeDelta;

            //Set the speed to the rigidbody
            Vector3 realSpeed = speed * timeMultiplier;

            transform.position += realSpeed * Time.deltaTime;
        }

        #endregion

        public bool GetActive()
        {
            return active;
        }

        public void ToggleActive(bool active)
        {
            this.active = active;

            if (active) return;

            InterruptDash();
        }

        #region Time Management 

        private void WaitQueue()
        {
            if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime * timeMultiplier;
            if (dashDurationTimer > 0) dashDurationTimer -= Time.deltaTime * timeMultiplier;
        }

        private void SetTimers(float dashDuration)
        {
            dashCooldownTimer = dashCooldownSeconds * timeMultiplier + dashDuration;

            dashDurationTimer = dashDuration;
        }

        #endregion

        #region State Management

        private void Landing()
        {
            //Allow Movement When landing after dashing
            DashState(false);

            //Stop the player from moving when landing, to avoid sliding, delegating to fixed update because of physics
            tagToStop = true;
            speedAfterStop = Vector3.zero;
        }
        private void DashState(bool state)
        {
            isDashing = state;

            //Disable movement while dashing
            movement.ToggleActiveSubcontroller(!state);
        }

        public void FlipDash(float angle)
        {
            InterruptDash(Quaternion.Euler(new Vector3(0f, angle, 0f)) * speed * .18f);
        }

        public void InterruptDash(Vector3 speedAfterInterruption = default(Vector3))
        {
            if (!isDashing) return;

            //If is still charging, interrupt the charge
            if (isCharging && dashCharge != null) StopCoroutine(dashCharge);

            //Allow movement when interrupted
            DashState(false);

            //Stop the player one frame when interrupted, to avoid sliding, delegating to fixed update because of physics
            tagToStop = true;
            //Set the speed after the interruption
            if(speedAfterInterruption != default(Vector3))speedAfterStop = speedAfterInterruption;
        }

        #endregion
    }
}
