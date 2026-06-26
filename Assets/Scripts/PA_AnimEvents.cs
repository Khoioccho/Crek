using UnityEngine;

namespace PostApoc
{
    // Imported attack clips (slash pack, FP arms) have baked AnimationEvents like
    // "PlayParticle". We spawn our own VFX in code, so these are harmless no-ops that
    // just stop the "AnimationEvent has no receiver" errors. Added to any GameObject
    // that owns an Animator playing those clips.
    public class PAAnimEvents : MonoBehaviour
    {
        public void PlayParticle() { }
        public void PlayParticle(string s) { }
        public void PlayParticle(float f) { }
        public void PlayParticle(int i) { }
        public void PlayParticle(Object o) { }
    }
}
