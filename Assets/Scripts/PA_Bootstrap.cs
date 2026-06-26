using UnityEngine;

namespace PostApoc
{
    // Boots the whole game from code with no scene wiring required.
    // Works in any scene (the project ships SampleScene), in the editor and in builds.
    public static class PABootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null) return;
            var go = new GameObject("[Game]");
            go.AddComponent<GameManager>();
        }
    }
}
