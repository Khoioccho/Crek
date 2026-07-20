using System.Collections;
using UnityEngine;

namespace PostApoc
{
    // Villager fighter (Rio, Humanoid). Idles until enemies appear, then advances on the
    // nearest skeleton and trades blows — driving the shared humanoid Animator
    // (Speed / Attack / Die). Falls back to a procedural sway if there's no Animator.
    public class AllyAI : MonoBehaviour
    {
        public float speed = 2.6f, attackRange = 2.2f, attackDamage = 7f, attackCooldown = 1.5f;

        Combatant _self, _target;
        float _cd, _seed, _baseYaw;
        Vector3 _basePos;
        bool _haveBase;

        Animator _anim;
        bool _aSpeed, _aAttack, _aDie, _died;

        void Awake()
        {
            _self = GetComponent<Combatant>();
            _seed = Random.value * 10f;
            _anim = GetComponentInChildren<Animator>();
            if (_anim != null)
                foreach (var p in _anim.parameters)
                {
                    if (p.name == "Speed") _aSpeed = true;
                    else if (p.name == "Attack") _aAttack = true;
                    else if (p.name == "Die") _aDie = true;
                }
        }

        void Update()
        {
            if (_self != null && _self.IsDead)
            {
                if (_anim != null && _aDie && !_died) { _died = true; _anim.SetTrigger("Die"); }
                enabled = false;
                return;
            }
            if (!_haveBase) { _basePos = transform.localPosition; _baseYaw = transform.localEulerAngles.y; _haveBase = true; }

            float dt = Time.deltaTime;
            if (_target == null || _target.IsDead) _target = Combatant.Nearest(Faction.Enemy, transform.position);

            float moving = 0f;
            if (_target == null)
            {
                if (_anim == null) Idle(dt);   // animator handles idle when present
            }
            else
            {
                Vector3 to = _target.transform.position - transform.position; to.y = 0f;
                float dist = to.magnitude;
                if (dist > attackRange) { AITraversal.Move(transform, to.normalized, speed, dt); Face(to, dt); moving = speed; }
                else
                {
                    Face(to, dt);
                    _cd -= dt;
                    if (_cd <= 0f)
                    {
                        _cd = attackCooldown;
                        if (_anim != null && _aAttack) _anim.SetTrigger("Attack");
                        StartCoroutine(DealHitAfterWindup(_target));
                    }
                }
            }

            if (_anim != null && _aSpeed) _anim.SetFloat("Speed", moving);
        }

        IEnumerator DealHitAfterWindup(Combatant target)
        {
            yield return new WaitForSeconds(0.30f);
            if (!enabled || _self == null || _self.IsDead || target == null || target.IsDead) yield break;
            if (AITraversal.CanHit(transform, target.transform, attackRange * 1.2f))
                target.TakeDamage(attackDamage);
        }

        void Face(Vector3 to, float dt)
        {
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 7f);
        }

        void Idle(float dt)
        {
            float t = Time.time + _seed;
            transform.localPosition = _basePos + new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.02f, 0f);
            transform.localEulerAngles = new Vector3(0f, _baseYaw + Mathf.Sin(t * 0.35f) * 14f, 0f);
        }
    }
}
