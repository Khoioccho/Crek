using System;
using UnityEngine;

namespace PostApoc
{
    // The first villager. Faces the player, shows a quest marker, and opens dialogue.
    public class NPC : MonoBehaviour
    {
        string[] _lines;
        Action _onDone;
        bool _canInteract;
        bool _done;
        Transform _marker;

        public void Configure(Transform marker) { _marker = marker; }

        public void Enable(string[] lines, Action onDone)
        {
            _lines = lines; _onDone = onDone; _canInteract = true; _done = false;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;

            Vector3 to = gm.Player.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(to), Time.deltaTime * 4f);

            if (_marker != null)
            {
                _marker.localPosition = new Vector3(0f, 2.15f + Mathf.Sin(Time.time * 2f) * 0.09f, 0f);
                _marker.Rotate(0f, 90f * Time.deltaTime, 0f);
                _marker.gameObject.SetActive(!_done);
            }

            if (_canInteract && !_done)
            {
                float dist = to.magnitude;
                if (dist < 3.2f)
                {
                    gm.Hud.SetPrompt("[E] / Click   —   Talk to the Villager");
                    if (PAInput.InteractDown() || PAInput.MouseLeftDown())
                    {
                        _canInteract = false; _done = true;
                        gm.Hud.ClearPrompt();
                        gm.Hud.ShowDialogue(_lines, _onDone);
                    }
                }
                else gm.Hud.ClearPrompt();
            }
        }
    }
}
