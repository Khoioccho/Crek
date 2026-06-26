using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PostApoc
{
    public enum Faction { Friendly, Enemy }

    // Health + faction for any fighter (player, villagers, skeletons). Keeps a global
    // registry so the AIs can find/target each other.
    public class Combatant : MonoBehaviour
    {
        public Faction faction = Faction.Friendly;
        public float maxHealth = 100f;
        public float health = 100f;
        public float regenPerSec = 0f;
        public bool destroyOnDeath = true;
        public bool Invulnerable;                       // i-frames (dodge roll)
        public System.Action<float> OnDamaged;          // for hit feedback (camera shake)
        public static System.Action<Combatant> Died;    // global death event (souls)
        public bool IsDead { get; private set; }

        public static readonly List<Combatant> All = new List<Combatant>();

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            if (!IsDead && regenPerSec > 0f && health < maxHealth)
                health = Mathf.Min(maxHealth, health + regenPerSec * Time.deltaTime);
        }

        public void TakeDamage(float amt)
        {
            if (IsDead || Invulnerable) return;
            health -= amt;
            OnDamaged?.Invoke(amt);
            AudioManager.Sfx(AudioManager.Thud, transform.position, 0.8f);
            if (health <= 0f) { health = 0f; Die(); }
        }

        void Die()
        {
            IsDead = true;
            All.Remove(this);
            Died?.Invoke(this);
            if (destroyOnDeath) StartCoroutine(Sink());
        }

        // Souls-style respawn (player only — destroyOnDeath must be false).
        public void Revive()
        {
            if (!IsDead) return;
            IsDead = false;
            health = maxHealth;
            Invulnerable = false;
            if (!All.Contains(this)) All.Add(this);
        }

        IEnumerator Sink()
        {
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            yield return new WaitForSeconds(0.4f);
            float t = 0f; Vector3 s = transform.position;
            while (t < 1.6f) { t += Time.deltaTime; transform.position = s + Vector3.down * (t / 1.6f) * 1.8f; yield return null; }
            Destroy(gameObject);
        }

        public static int Count(Faction f)
        {
            int n = 0;
            foreach (var c in All) if (c != null && !c.IsDead && c.faction == f) n++;
            return n;
        }

        public static Combatant Nearest(Faction f, Vector3 pos)
        {
            Combatant best = null; float bd = float.MaxValue;
            foreach (var c in All)
            {
                if (c == null || c.IsDead || c.faction != f) continue;
                float d = (c.transform.position - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }
    }
}
