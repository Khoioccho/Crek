using UnityEngine;

namespace PostApoc
{
    // First/third-person controller on a CharacterController, driven by the new Input System.
    // Press V to toggle the view; mouse-wheel zooms the third-person camera.
    // The camera is driven manually (not parented) so both modes share one camera cleanly.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 4.2f;
        public float runSpeed = 7.2f;
        public float gravity = 22f;
        public float jumpVel = 6.5f;
        public float lookSpeed = 0.09f;
        public float pitchClamp = 80f;
        public bool controlEnabled;

        public bool ThirdPerson { get; private set; }

        public Combatant Combat;
        public Transform Hand;
        public float attackRange = 3f;
        public float attackDamage = 10f;
        public float heavyDamage = 20f;
        public float attackCooldown = 0.5f;
        public float heavyCooldown = 0.9f;
        float _atkCd, _attackT;
        bool _swingFlip;

        [Header("Souls systems")]
        public float maxStamina = 100f;
        public float stamina = 100f;
        public float staminaRegen = 40f;
        public float lightCost = 18f, heavyCost = 35f, rollCost = 25f, sprintCostPerSec = 12f;
        float _stamDelay;
        public float rollDuration = 0.55f;
        public float rollSpeed = 9f;
        float _rollT; Vector3 _rollDir;
        public static int Souls;
        public Combatant LockTarget { get; private set; }
        float _shake;
        bool _hooked;

        Animator _anim;
        bool _aSpeed, _aAtk, _aJump, _aDie, _aRoll, _diedOnce;
        FirstPersonArms _fpArms;

        Transform _visualRoot;
        GameObject _weapon;
        [Header("Equipped weapon (right-hand socket)")]
        public Vector3 weaponLocalPos = Vector3.zero;
        public Vector3 weaponLocalEuler = Vector3.zero;
        public float weaponScale = 0.8f;

        [Header("First person")]
        public float fpEyeHeight = 0.65f;
        [Header("Third person")]
        public float tpHeight = 1.5f;
        public float tpDistance = 4.5f;
        public float tpMinDistance = 1.5f;
        public float tpMaxDistance = 8f;

        const int IgnoreRaycastLayer = 2;

        CharacterController _cc;
        Camera _cam;
        Renderer[] _visuals;
        Transform _visualT;
        Vector3 _visBasePos;
        Quaternion _visBaseRot;
        float _stride;
        float _camYaw, _camPitch, _bodyYaw, _vy;
        int _camMask;

        // Show the body mesh only in third person; keep just its shadow in first person.
        public void SetVisual(GameObject root)
        {
            _visuals = root != null ? root.GetComponentsInChildren<Renderer>(true) : null;
            _visualT = root != null ? root.transform : null;
            _visualRoot = _visualT;
            if (_visualT != null)
            {
                _visBasePos = _visualT.localPosition;
                _visBaseRot = _visualT.localRotation;
            }

            _anim = root != null ? root.GetComponentInChildren<Animator>() : null;
            if (_anim != null)
            {
                if (_anim.GetComponent<PAAnimEvents>() == null) _anim.gameObject.AddComponent<PAAnimEvents>();
                var ctrl = Resources.Load<RuntimeAnimatorController>("Models/PlayerAnim");
                if (ctrl != null)
                {
                    _anim.runtimeAnimatorController = ctrl;
                    _anim.applyRootMotion = false;
                    _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    foreach (var p in _anim.parameters)
                    {
                        if (p.name == "Speed") _aSpeed = true;
                        else if (p.name == "Attack") _aAtk = true;
                        else if (p.name == "Jump") _aJump = true;
                        else if (p.name == "Die") _aDie = true;
                        else if (p.name == "Roll") _aRoll = true;
                    }
                }
            }

            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (_visuals == null) return;
            var mode = ThirdPerson
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            foreach (var r in _visuals)
                if (r != null) r.shadowCastingMode = mode;
        }

        // The animated right-hand bone (warrior is Humanoid).
        public Transform HandBone
        {
            get
            {
                if (_anim != null && _anim.isHuman)
                {
                    var b = _anim.GetBoneTransform(HumanBodyBones.RightHand);
                    if (b != null) return b;
                }
                return null;
            }
        }

        // Builds the axe and parents it to the right-hand bone so it swings with the
        // animation. Counter-scales for the model's scale; re-registers renderers so the
        // weapon hides with the body in first person.
        public void EquipAxe()
        {
            var hand = HandBone != null ? HandBone : Hand;
            if (hand == null) return;
            if (_weapon != null) Destroy(_weapon);

            _weapon = PAArt.BuildAxe(hand, Vector3.zero, Vector3.zero);
            float ls = hand.lossyScale.x;
            _weapon.transform.localScale = Vector3.one * (ls > 0.0001f ? weaponScale / ls : weaponScale);

            // Orient the shaft (+Y) along the forearm->hand direction so it points out of
            // the grip (not into the floor). Rig-agnostic, then a tunable offset on top.
            if (_anim != null && _anim.isHuman)
            {
                var lower = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
                if (lower != null)
                {
                    Vector3 gripDir = (hand.position - lower.position).normalized;
                    if (gripDir.sqrMagnitude > 0.0001f)
                        _weapon.transform.rotation = Quaternion.FromToRotation(_weapon.transform.up, gripDir) * _weapon.transform.rotation;
                }
            }
            _weapon.transform.localPosition += weaponLocalPos;
            _weapon.transform.localRotation = _weapon.transform.localRotation * Quaternion.Euler(weaponLocalEuler);

            if (_visualRoot != null) { _visuals = _visualRoot.GetComponentsInChildren<Renderer>(true); ApplyVisual(); }
        }

        public void Setup(Camera cam, float startYaw)
        {
            _cc = GetComponent<CharacterController>();
            _cam = cam;
            _camYaw = startYaw;
            _bodyYaw = startYaw;
            _camPitch = 8f;

            cam.transform.SetParent(null); // driven manually in LateUpdate
            gameObject.layer = IgnoreRaycastLayer; // so the camera collision cast skips us
            _camMask = ~(1 << IgnoreRaycastLayer);

            Hand = new GameObject("Hand").transform;
            Hand.SetParent(transform, false);
            Hand.localPosition = new Vector3(0.3f, 0.25f, 0.3f);

            _fpArms = gameObject.AddComponent<FirstPersonArms>();
            _fpArms.Init(cam.transform);

            ApplyCamera();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.Instance;
            bool dead = Combat != null && Combat.IsDead;
            bool active = controlEnabled && !dead && (gm == null || !gm.Paused);
            bool rolling = _rollT > 0f;

            if (!_hooked && Combat != null)
            {
                _hooked = true;
                Combat.OnDamaged += a => _shake = 0.3f;
                Combatant.Died += OnSomeoneDied;
            }
            if (_shake > 0f) _shake = Mathf.Max(0f, _shake - dt);

            // stamina regen (after a short delay since last spend)
            _stamDelay -= dt;
            if (_stamDelay <= 0f && stamina < maxStamina)
                stamina = Mathf.Min(maxStamina, stamina + staminaRegen * dt);

            // lock-on
            if (active && PAInput.LockOnDown())
            {
                if (LockTarget != null) LockTarget = null;
                else LockTarget = AcquireLockTarget();
            }
            if (LockTarget != null &&
                (LockTarget.IsDead || (LockTarget.transform.position - transform.position).sqrMagnitude > 26f * 26f))
                LockTarget = null;

            // attacks (blocked while rolling / out of stamina)
            _atkCd -= dt;
            if (_attackT > 0f) _attackT -= dt;
            if (active && !rolling && _atkCd <= 0f && stamina > 0f)
            {
                bool light = PAInput.AttackDown();
                bool heavy = !light && PAInput.HeavyDown();
                if (light || heavy)
                {
                    _atkCd = heavy ? heavyCooldown : attackCooldown;
                    _attackT = 0.22f;
                    Spend(heavy ? heavyCost : lightCost);
                    if (_anim != null && _aAtk) _anim.SetTrigger("Attack");
                    if (_fpArms != null) _fpArms.Attack();
                    AudioManager.Sfx(AudioManager.Whoosh, transform.position, heavy ? 0.9f : 0.65f);
                    DoAttack(heavy ? heavyDamage : attackDamage);
                }
            }

            if (active && PAInput.TogglePerspectiveDown())
            {
                ThirdPerson = !ThirdPerson;
                ApplyVisual();
            }

            if (active)
            {
                PASettings.Load();
                float sens = lookSpeed * PASettings.Sensitivity;
                Vector2 look = PAInput.LookDelta();
                _camYaw += look.x * sens;
                _camPitch = Mathf.Clamp(_camPitch - look.y * sens, -pitchClamp, pitchClamp);

                if (ThirdPerson)
                {
                    float scroll = PAInput.ScrollY();
                    if (scroll > 0.01f) tpDistance = Mathf.Clamp(tpDistance - 0.5f, tpMinDistance, tpMaxDistance);
                    else if (scroll < -0.01f) tpDistance = Mathf.Clamp(tpDistance + 0.5f, tpMinDistance, tpMaxDistance);
                }
            }

            // lock-on steers the third-person camera toward the target
            if (LockTarget != null && ThirdPerson)
            {
                Vector3 toT = LockTarget.transform.position - transform.position;
                float wantYaw = Mathf.Atan2(toT.x, toT.z) * Mathf.Rad2Deg;
                _camYaw = Mathf.LerpAngle(_camYaw, wantYaw, dt * 6f);
                _camPitch = Mathf.Lerp(_camPitch, 12f, dt * 4f);
            }

            // movement is camera-relative
            Vector2 mv = active && !rolling ? PAInput.Move() : Vector2.zero;
            Vector3 moveDir = Quaternion.Euler(0f, _camYaw, 0f) * new Vector3(mv.x, 0f, mv.y);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            bool sprinting = PAInput.Run() && stamina > 0f && moveDir.sqrMagnitude > 0.01f;
            if (sprinting) Spend(sprintCostPerSec * dt);
            Vector3 wish = moveDir * (sprinting ? runSpeed : walkSpeed);

            // dodge roll (C): burst of speed + i-frames
            if (active && !rolling && _cc.isGrounded && stamina > 0f && PAInput.RollDown())
            {
                _rollT = rollDuration;
                rolling = true;
                _rollDir = moveDir.sqrMagnitude > 0.01f ? moveDir : transform.forward;
                Spend(rollCost);
                if (Combat != null) Combat.Invulnerable = true;
                _bodyYaw = Mathf.Atan2(_rollDir.x, _rollDir.z) * Mathf.Rad2Deg;
                if (_anim != null && _aRoll) _anim.SetTrigger("Roll");
            }
            if (rolling)
            {
                _rollT -= dt;
                wish = _rollDir * rollSpeed;
                if (Combat != null && _rollT < rollDuration * 0.3f) Combat.Invulnerable = false; // i-frames end late-roll
                if (_rollT <= 0f && Combat != null) Combat.Invulnerable = false;
            }

            // body facing
            if (rolling)
            {
                _bodyYaw = Mathf.Atan2(_rollDir.x, _rollDir.z) * Mathf.Rad2Deg;
            }
            else if (ThirdPerson)
            {
                if (LockTarget != null)
                {
                    Vector3 toT = LockTarget.transform.position - transform.position;
                    _bodyYaw = Mathf.MoveTowardsAngle(_bodyYaw, Mathf.Atan2(toT.x, toT.z) * Mathf.Rad2Deg, 540f * dt);
                }
                else if (moveDir.sqrMagnitude > 0.001f)
                {
                    float target = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                    _bodyYaw = Mathf.MoveTowardsAngle(_bodyYaw, target, 540f * dt);
                }
            }
            else _bodyYaw = _camYaw;
            transform.rotation = Quaternion.Euler(0f, _bodyYaw, 0f);

            if (_cc.isGrounded)
            {
                _vy = -2f;
                if (active && !rolling && PAInput.JumpDown())
                {
                    _vy = jumpVel;
                    if (_anim != null && _aJump) _anim.SetTrigger("Jump");
                }
            }
            else _vy -= gravity * dt;

            Vector3 vel = wish;
            vel.y = _vy;
            _cc.Move(vel * dt);

            Vector3 hv = _cc.velocity; hv.y = 0f;
            float planar = hv.magnitude;
            if (_anim != null && _aSpeed)
            {
                _anim.SetFloat("Speed", planar);
                if (Combat != null && Combat.IsDead && _aDie && !_diedOnce) { _diedOnce = true; _anim.SetTrigger("Die"); }
            }
            else AnimateBody(dt);

            // dodge-roll visual fallback: only tumble procedurally if there's no real roll clip
            if (_visualT != null && !(_anim != null && _aRoll))
            {
                if (rolling && _rollT > 0f)
                {
                    float spin = 360f * (1f - _rollT / rollDuration);
                    _visualT.localRotation = _visBaseRot * Quaternion.Euler(spin, 0f, 0f);
                }
                else if (_anim != null) _visualT.localRotation = _visBaseRot;
            }

            if (_fpArms != null) { _fpArms.SetVisible(!ThirdPerson); _fpArms.SetSpeed(planar); }
        }

        void Spend(float amount)
        {
            stamina = Mathf.Max(0f, stamina - amount);
            _stamDelay = 0.7f;
        }

        Combatant AcquireLockTarget()
        {
            Vector3 fwd = _cam != null ? _cam.transform.forward : transform.forward;
            fwd.y = 0f; fwd.Normalize();
            Combatant best = null; float bestScore = float.MaxValue;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.faction != Faction.Enemy) continue;
                Vector3 to = c.transform.position - transform.position; to.y = 0f;
                float dist = to.magnitude;
                if (dist > 22f) continue;
                float ang = Vector3.Angle(fwd, to.normalized);
                if (ang > 75f) continue;
                float score = dist + ang * 0.15f;
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        void OnSomeoneDied(Combatant c)
        {
            if (c != null && c.faction == Faction.Enemy && Combat != null && !Combat.IsDead)
                Souls += 50;
        }

        void OnDestroy() { Combatant.Died -= OnSomeoneDied; }

        // Souls-style respawn: restore health, reset the death animation, regain control.
        public void ResetAfterRespawn()
        {
            if (Combat != null) Combat.Revive();
            stamina = maxStamina;
            _diedOnce = false;
            _rollT = 0f;
            LockTarget = null;
            if (_anim != null) { _anim.Rebind(); _anim.Update(0f); }
            controlEnabled = true;
        }

        // Procedural "walk cycle" for the un-rigged character mesh.
        void AnimateBody(float dt)
        {
            if (_visualT == null) return;
            Vector3 v = _cc.velocity; v.y = 0f;
            float spd = v.magnitude;
            float norm = Mathf.Clamp01(spd / runSpeed);

            _stride += spd * dt * 2.2f;
            float bob = Mathf.Abs(Mathf.Sin(_stride)) * 0.07f * norm;
            float breathe = Mathf.Sin(Time.time * 1.6f) * 0.012f * (1f - norm);
            float roll = Mathf.Sin(_stride) * 5f * norm;
            float lean = 6f * norm;

            float atkLean = _attackT > 0f ? Mathf.Sin((1f - _attackT / 0.22f) * Mathf.PI) * 22f : 0f;
            _visualT.localPosition = _visBasePos + Vector3.up * (bob + breathe);
            _visualT.localRotation = _visBaseRot * Quaternion.Euler(lean + atkLean, 0f, roll);
        }

        void DoAttack(float damage)
        {
            SpawnSlash();
            Vector3 origin = transform.position;
            Vector3 fwd = transform.forward;

            // locked target gets priority if it's in reach
            if (LockTarget != null && !LockTarget.IsDead)
            {
                Vector3 toL = LockTarget.transform.position - origin; toL.y = 0f;
                if (toL.sqrMagnitude <= attackRange * attackRange) { LockTarget.TakeDamage(damage); return; }
            }

            Combatant best = null; float bd = attackRange * attackRange;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.faction != Faction.Enemy) continue;
                Vector3 to = c.transform.position - origin; to.y = 0f;
                float sq = to.sqrMagnitude;
                if (sq > attackRange * attackRange) continue;
                if (Vector3.Dot(fwd, to.normalized) < 0.2f) continue;
                if (sq < bd) { bd = sq; best = c; }
            }
            if (best != null) best.TakeDamage(damage);
        }

        void SpawnSlash()
        {
            _swingFlip = !_swingFlip;
            Vector3 pos = transform.position + transform.forward * 1.0f + Vector3.up * 1.1f;

            // use the imported URP slash VFX if present
            var prefab = Resources.Load<GameObject>("Models/SlashVFX");
            if (prefab != null)
            {
                var fx = Instantiate(prefab, pos, Quaternion.LookRotation(transform.forward, Vector3.up));
                fx.transform.Rotate(0f, 0f, _swingFlip ? 35f : -35f, Space.Self);
                Destroy(fx, 1.5f);
                return;
            }

            Quaternion rot = _cam != null
                ? Quaternion.LookRotation((pos - _cam.transform.position).normalized, Vector3.up)
                : transform.rotation;

            var go = new GameObject("Slash");
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * 0.9f;
            go.transform.Rotate(0f, 0f, _swingFlip ? -45f : 45f, Space.Self);

            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = PAArt.SlashMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mat = PAArt.AdditiveUnlit(new Color(0.8f, 0.95f, 1f, 1f));
            mr.sharedMaterial = mat;

            go.AddComponent<SlashFade>().Init(mat, _swingFlip ? 1f : -1f);
        }

        void LateUpdate() => ApplyCamera();

        void ApplyCamera()
        {
            if (_cam == null) return;

            if (ThirdPerson)
            {
                Vector3 pivot = transform.position + Vector3.up * tpHeight;
                Quaternion rot = Quaternion.Euler(_camPitch, _camYaw, 0f);
                Vector3 back = rot * Vector3.back;

                float dist = tpDistance;
                if (Physics.SphereCast(pivot, 0.25f, back, out var hit, tpDistance, _camMask, QueryTriggerInteraction.Ignore))
                    dist = Mathf.Max(tpMinDistance * 0.5f, hit.distance - 0.1f);

                Vector3 camPos = pivot + back * dist + ShakeOffset();
                _cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(pivot - camPos, Vector3.up));
            }
            else
            {
                Vector3 eye = transform.position + Vector3.up * fpEyeHeight + ShakeOffset();
                _cam.transform.SetPositionAndRotation(eye, Quaternion.Euler(_camPitch, _camYaw, 0f));
            }
        }

        Vector3 ShakeOffset()
        {
            if (_shake <= 0f) return Vector3.zero;
            float a = _shake * 0.12f;
            return new Vector3(
                (Mathf.PerlinNoise(Time.time * 30f, 0.3f) - 0.5f) * a,
                (Mathf.PerlinNoise(0.7f, Time.time * 30f) - 0.5f) * a, 0f);
        }
    }
}
