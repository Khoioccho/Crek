using System.Collections;
using UnityEngine;

namespace PostApoc
{
    // Drives the prologue: wake up -> leave the house -> walk to the village ->
    // meet the first NPC -> goblins appear (teaser). Combat is the next chapter.
    public class TutorialManager : MonoBehaviour
    {
        WorldBuilder _world;
        PlayerController _player;
        HUD _hud;
        bool _talked;

        public void Begin(WorldBuilder world, PlayerController player, HUD hud)
        {
            _world = world; _player = player; _hud = hud;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            _player.controlEnabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _hud.SetFade(1f);
            yield return new WaitForSeconds(0.6f);
            _hud.ShowToast("Day 4,317 since the Collapse.", 3.5f);
            yield return new WaitForSeconds(2.0f);
            yield return _hud.FadeTo(0f, 2.5f);

            _hud.ShowToast("You wake in the ruins of an old house.   [WASD] move • [Shift] sprint • [Space] jump • [C] roll • [V] view", 7f);
            _player.controlEnabled = true;
            _hud.SetObjective("Get up and leave the house");

            while (InHouse(_player.transform.position)) yield return null;

            _hud.ShowToast("Cold wind claws across the wasteland. A village stands to the north.", 4.5f);
            _hud.SetObjective("Travel north to the village");
            _hud.SetWaypoint(_world.VillageCenter, "Village");

            while (_world.Villager != null &&
                   Flat(_player.transform.position, _world.Villager.transform.position) > 5f)
                yield return null;

            _hud.ClearWaypoint();
            _hud.SetObjective("Talk to the Villager");

            _talked = false;
            _world.Villager.Enable(new[]
            {
                "Villager:  You there — stranger! You picked a black day to wander in.",
                "Villager:  The dead are rising — skeletons march on us from the north ridge.",
                "Villager:  Our walls are thin and our blades are few. Will you stand with us?",
                "Villager:  Then take this — every hand counts. Defend the village with us!"
            }, OnTalked);

            while (!_talked) yield return null;

            // the villager arms you
            _world.GivePlayerWeapon(_player);
            _hud.ShowToast("The villager presses an axe into your hands. Defend the village!", 4f);
            yield return new WaitForSeconds(1.2f);

            _hud.SetObjective("Defend the village!");
            _world.ActivateVillagerCombat();                                  // villagers join in
            int n = Mathf.Clamp(Combatant.Count(Faction.Friendly), 3, 8);     // one skeleton per defender
            _world.SpawnGoblinWave(n);
            _hud.SetBossBar("RISEN  HORDE");
            _hud.ShowToast("[LMB] attack • [RMB] heavy • [Tab] lock-on • [C] roll (i-frames)", 6f);

            yield return new WaitForSeconds(0.6f);
            while (Combatant.Count(Faction.Enemy) > 0)
            {
                if (_player.Combat != null && _player.Combat.IsDead)
                {
                    // souls-style death: YOU DIED, lose souls, respawn at the village entrance
                    _player.controlEnabled = false;
                    yield return new WaitForSeconds(1.0f);   // let the death anim land
                    yield return _hud.BigBanner("YOU DIED", new Color(0.62f, 0.07f, 0.05f), 1.3f);
                    _player.transform.position = _world.VillageCenter + new Vector3(0f, 1f, -14f);
                    _player.ResetAfterRespawn();
                }
                yield return null;
            }

            _hud.ClearBossBar();
            _hud.SetObjective("");
            yield return _hud.BigBanner("VICTORY  ACHIEVED", new Color(0.83f, 0.66f, 0.30f), 1.6f);
            _hud.ShowBanner("PROLOGUE COMPLETE",
                "The raid is broken.  Next: character customization & deeper combat.");
        }

        void OnTalked() { _talked = true; }

        bool InHouse(Vector3 p)
        {
            return Mathf.Abs(p.x - _world.HouseCenter.x) < 4.3f
                && Mathf.Abs(p.z - _world.HouseCenter.z) < 4.3f;
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
