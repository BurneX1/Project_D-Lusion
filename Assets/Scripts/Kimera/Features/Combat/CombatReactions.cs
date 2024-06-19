using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Features
{
    public class CombatReactions :  MonoBehaviour, IActivable, IFeatureSetup //Other channels
    {
        //Configuration
        [Header("Settings")]
        public Controller controller;
        public Settings settings;
        //Control
        [Header("Control")]
        [SerializeField] private bool active;
        //States
        //Properties
        [Header("Properties")]
        public float disableTimeAfterHit;
        //References
        [Header("References")]
        public Life life;
        public Stun stun;
        public MovementIntelligence movementIntel;
        public CombatAnimator combatAnimator;
        //Componentes
        [Header("Components")]
        public Animator animator;
        public CrowdIntelligence<Enemy> enemyCrowd;

        private void Awake()
        {
            //Get References
            life = GetComponent<Life>();
            stun = GetComponent<Stun>();
            combatAnimator = GetComponent<CombatAnimator>();
            if (movementIntel == null) movementIntel = GetComponent<MovementIntelligence>();

            //Get Components
            if(animator == null) animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (life != null) life.OnDamage += ReactToDamage;
        }

        private void OnDisable()
        {
            if (life != null) life.OnDamage -= ReactToDamage;
        }

        public void SetupFeature(Controller controller)
        {
            settings = controller.settings;
            this.controller = controller;

            //Setup Properties
            disableTimeAfterHit = settings.Search("disableTimeAfterHit");

            ToggleActive(true);
        }

        private void ReactToDamage()
        {
            if (!active) return;

            if (stun != null) stun.StunSomeTime(disableTimeAfterHit);

            if (combatAnimator != null) combatAnimator.InputConditon("stop");

            Enemy meEnemy = controller as Enemy;
            if(enemyCrowd != null && life != null && meEnemy != null && movementIntel != null)
            {
                if (life.CurrentHealth <= movementIntel.runAwayLife)
                {
                    enemyCrowd.SetUnitOutOfBattle(meEnemy);
                }
            }
        }

        public bool GetActive()
        {
            return active;
        }

        public void ToggleActive(bool active)
        {
            this.active = active;
        }
    }
}