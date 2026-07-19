using System.Collections.Generic;
using UnityEngine;

namespace PostApoc
{
    // Enemy (skeleton) combat AI. Each one targets the friendly with the FEWEST current
    // attackers (then nearest), so the wave spreads out 1-on-1 instead of dogpiling.
    public class Goblin : MonoBehaviour
    {
        public float speed = 2.3f, attackRange = 2.2f, attackDamage = 6f, attackCooldown = 1.8f;

        Combatant _self, _target;
        Animator _anim;
        bool _hasSpeed, _hasAttack, _hasDie;
        Transform _body;
        float _phase, _cd, _growlT;

        public static readonly List<Goblin> All = new List<Goblin>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Awake() { _self = GetComponent<Combatant>(); }

        void Start()
        {
            _phase = Random.value * 6f;
            if (transform.childCount > 0) _body = transform.GetChild(0);
            _anim = GetComponentInChildren<Animator>();
            if (_anim != null)
                foreach (var p in _anim.parameters)
                {
                    if (p.name == "Speed") _hasSpeed = true;
                    else if (p.name == "Attack") _hasAttack = true;
                    else if (p.name == "Die") _hasDie = true;
                }
            speed *= Random.Range(0.9f, 1.1f);
            _growlT = Random.Range(0.2f, 2.5f);   // staggered spawn growls
        }

        void Update()
        {
            if (_self != null && _self.IsDead)
            {
                if (_anim != null && _hasDie) _anim.SetTrigger("Die");
                enabled = false;
                return;
            }
            float dt = Time.deltaTime;

            _growlT -= dt;
            if (_growlT <= 0f)
            {
                _growlT = Random.Range(5f, 11f);
                AudioManager.Sfx(AudioManager.SkeletonVoice, transform.position + Vector3.up * 1.2f, 0.7f);
            }

            if (_target == null || _target.IsDead) _target = PickTarget();

            bool moving = false;
            if (_target != null)
            {
                Vector3 to = _target.transform.position - transform.position; to.y = 0f;
                float dist = to.magnitude;
                moving = dist > attackRange;
                if (moving) { transform.position += to.normalized * speed * dt; Face(to, dt); }
                else
                {
                    Face(to, dt);
                    _cd -= dt;
                    if (_cd <= 0f)
                    {
                        _cd = attackCooldown;
                        if (_anim != null && _hasAttack) _anim.SetTrigger("Attack");
                        _target.TakeDamage(attackDamage);
                    }
                }
            }
            Animate(moving, dt);
        }

        Combatant PickTarget()
        {
            Combatant best = null; float bestScore = float.MaxValue;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.faction != Faction.Friendly) continue;
                int attackers = 0;
                foreach (var g in All) if (g != this && g.enabled && g._target == c) attackers++;
                float score = attackers * 1000f + (c.transform.position - transform.position).magnitude;
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        void Face(Vector3 to, float dt)
        {
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 7f);
        }

        void Animate(bool moving, float dt)
        {
            if (_anim != null && _hasSpeed) { _anim.SetFloat("Speed", moving ? 1f : 0f); return; }
            if (_body == null) return;
            if (moving)
            {
                _phase += dt * speed * 3.2f;
                _body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_phase)) * 0.05f, 0f);
                _body.localRotation = Quaternion.Euler(8f, 0f, Mathf.Sin(_phase) * 7f);
            }
            else
            {
                _phase += dt * 2.2f;
                _body.localRotation = Quaternion.Euler(3f, Mathf.Sin(_phase) * 3.5f, 0f);
            }
        }
    }
}
