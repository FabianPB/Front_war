// ═══════════════════════════════════════════════════════════════════════════════
// WAR · PlayerController
// ─────────────────────────────────────────────────────────────────────────────
// Input forwarder for the LOCAL player. Reads keyboard/mouse, sends intents to
// the GameClient (server-authoritative). Server-acknowledged position arrives
// via MoveResult / PlayerStateUpdate and gets applied smoothly.
//
// This controller does NOT decide position, HP, or cooldowns — it only sends
// intents and reflects what comes back.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using WAR.Network;

namespace WAR.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Network")]
        [SerializeField] private GameClient _client;
        [SerializeField] private CharacterLoader _loader;

        [Header("Movement")]
        [Tooltip("Smoothing factor for visual lerp toward server-authoritative position.")]
        [SerializeField] private float _interpolationSpeed = 8f;
        [Tooltip("Min distance to trigger a new MoveTo invoke.")]
        [SerializeField] private float _moveThreshold = 0.5f;
        [Tooltip("Minimum ms between two MoveTo invokes (matches server rate-limit 200ms).")]
        [SerializeField] private int _moveCooldownMs = 220;

        private float _lastServerX;
        private float _lastServerY;
        private bool _hasServerPos;
        private float _nextMoveAllowedTime;

        private PlayerFullStateDto _lastFullState;

        private void OnEnable()
        {
            if (_client is null) { Debug.LogError("[PlayerController] GameClient not assigned."); return; }
            _client.OnPlayerStateUpdate += HandleMyState;
            _client.OnMoveResult += HandleMoveResult;
            _client.OnCombatResult += HandleCombatResult;
        }

        private void OnDisable()
        {
            if (_client is null) return;
            _client.OnPlayerStateUpdate -= HandleMyState;
            _client.OnMoveResult -= HandleMoveResult;
            _client.OnCombatResult -= HandleCombatResult;
        }

        // ─── Server → Client ────────────────────────────────────────────────

        private void HandleMyState(PlayerFullStateDto s)
        {
            if (s.PlayerId != _client.CurrentPlayerId) return;
            _lastFullState = s;
            _lastServerX = s.X;
            _lastServerY = s.Y;
            _hasServerPos = true;
        }

        private void HandleMoveResult(MoveResultDto m)
        {
            _lastServerX = m.X;
            _lastServerY = m.Y;
            _hasServerPos = true;
        }

        private void HandleCombatResult(OnlineCombatResult r)
        {
            // Gameplay / VFX / SFX hooks go in a dedicated CombatPresenter.
            // Left intentionally empty here.
        }

        // ─── Input / intent ─────────────────────────────────────────────────

        private void Update()
        {
            if (_client is null || !_client.IsConnected || _lastFullState is null) return;
            ReadMovementIntent();
            ReadCombatIntent();
            LerpVisualToServer();
        }

        private void ReadMovementIntent()
        {
            if (Time.unscaledTime < _nextMoveAllowedTime) return;

            var kb = Keyboard.current;
            if (kb is null) return;

            var dx = 0f;
            var dy = 0f;
            if (kb.wKey.isPressed) dy -= 1f;
            if (kb.sKey.isPressed) dy += 1f;
            if (kb.aKey.isPressed) dx -= 1f;
            if (kb.dKey.isPressed) dx += 1f;

            if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f) return;

            var len = Mathf.Sqrt(dx * dx + dy * dy);
            dx /= len; dy /= len;

            var targetX = _lastServerX + dx;
            var targetY = _lastServerY + dy;

            _ = _client.MoveToAsync(targetX, targetY);
            _nextMoveAllowedTime = Time.unscaledTime + _moveCooldownMs / 1000f;
        }

        private void ReadCombatIntent()
        {
            var kb = Keyboard.current;
            if (kb is null) return;

            // Space = basic attack on selected target (target selection handled elsewhere).
            if (kb.spaceKey.wasPressedThisFrame)
            {
                var target = SelectionTracker.CurrentTargetId;
                if (!string.IsNullOrEmpty(target))
                    _ = _client.BasicAttackAsync(target);
            }

            // Digits 1..9 for skills 1..9 (skills are 0-indexed server-side on the skill list).
            for (var i = 0; i < 9; i++)
            {
                var key = kb[$"digit{i + 1}Key"];
                if (key is not null && key.wasPressedThisFrame)
                {
                    var target = SelectionTracker.CurrentTargetId;
                    if (!string.IsNullOrEmpty(target))
                        _ = _client.UseSkillAsync(i, target);
                }
            }
        }

        private void LerpVisualToServer()
        {
            if (!_hasServerPos) return;
            if (!_loader.TryGetGameObject(_client.CurrentPlayerId, out var go)) return;

            var target = new Vector3(_lastServerX, 0f, _lastServerY);
            go.transform.position = Vector3.Lerp(
                go.transform.position,
                target,
                Time.deltaTime * _interpolationSpeed);
        }
    }

    /// <summary>
    /// Placeholder for a selection system. Fill in when the UI / world-picking is wired.
    /// For Paso 1 demo, point this at whichever player was last clicked.
    /// </summary>
    public static class SelectionTracker
    {
        public static string CurrentTargetId { get; set; }
    }
}
