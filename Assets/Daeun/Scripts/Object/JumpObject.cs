using UnityEngine;
using System.Collections;

/// <summary>
/// 용수철 점프 오브젝트.
/// 발판과 코일을 에디터에서 직접 배치한 뒤 인스펙터에 드래그&드롭하면 동작합니다.
///
/// [사용법]
/// 1. 빈 오브젝트(JumpObject 루트)를 만들고 이 스크립트를 추가하세요.
/// 2. 루트의 자식으로 발판(Platform) 오브젝트와 코일(Coil_0, Coil_1, ...) 오브젝트들을 만드세요.
///    - 발판에는 BoxCollider2D를 꼭 붙여 주세요.
///    - 코일에는 콜라이더가 없어도 됩니다.
/// 3. 인스펙터에서 Platform Transform과 Coil Transforms 배열을 연결하세요.
/// 4. Player Layer를 플레이어 레이어로 설정하세요.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JumpObject : MonoBehaviour
{
    public enum JumpSide { Top, Bottom, Left, Right }

    // ════════════════════════
    //  인스펙터 연결 필드
    // ════════════════════════
    [Header("── 방향 설정 ──")]
    [Tooltip("어느 면에서 부딪혔을 때 점프가 발동될지 설정합니다. (로컬 기준)")]
    [SerializeField] private JumpSide activeSide = JumpSide.Top;

    [Header("── 오브젝트 연결 (필수) ──")]
    [Tooltip("발판 Transform. BoxCollider2D가 붙어 있어야 합니다.")]
    [SerializeField] private Transform platformTransform;

    [Tooltip("코일 Transform 배열. 위(인덱스 0)에서 아래(마지막 인덱스) 순서로 넣어 주세요.")]
    [SerializeField] private Transform[] coilTransforms;

    [Header("── 레이어 ──")]
    [Tooltip("플레이어 레이어 마스크")]
    [SerializeField] private LayerMask playerLayer;

    // ════════════════════════
    //  바운스 설정
    // ════════════════════════
    [Header("── 바운스 설정 ──")]
    [Tooltip("점프가 발동하기 위한 최소 낙하 속도. 이보다 살살 떨어지면 그냥 땅처럼 걸어다닐 수 있습니다.")]
    [SerializeField] private float activationMinSpeed = 2.0f;
    [Tooltip("낙하 높이에 곱해지는 배율 (1 = 떨어진 높이만큼 튕김)")]
    [SerializeField] private float heightMultiplier = 1.0f;
    [Tooltip("기본으로 추가되는 보너스 높이")]
    [SerializeField] private float bonusHeight = 3f;
    [Tooltip("바운스 직후 좌우 입력이 잠기는 시간")]
    [SerializeField] private float inputLockTime = 0.15f;

    // ════════════════════════
    //  용수철 애니메이션
    // ════════════════════════
    [Header("── 용수철 애니메이션 ──")]
    [Tooltip("최소 충격 속도 (이하면 애니메이션 없음)")]
    [SerializeField] private float minImpactSpeed = 2f;
    [Tooltip("최대 충격 속도 (이 이상이면 최대 압축)")]
    [SerializeField] private float maxImpactSpeed = 20f;
    [Tooltip("최대 압축 비율 (0.7 = 코일 높이가 30%까지 줄어듦)")]
    [SerializeField] private float maxCompressionRatio = 0.7f;
    [Tooltip("최소 압축 유지 시간 (살짝 밟을 때)")]
    [SerializeField] private float minCompressDuration = 0.08f;
    [Tooltip("최대 압축 유지 시간 (높이서 떨어질 때)")]
    [SerializeField] private float maxCompressDuration = 0.35f;
    [Tooltip("용수철이 원래로 돌아오는 시간")]
    [SerializeField] private float releaseDuration = 0.15f;
    [Tooltip("복원 후 출렁거리는 횟수")]
    [SerializeField] private int oscillationCount = 3;
    [Tooltip("출렁거림 지속 시간")]
    [SerializeField] private float oscillationDuration = 0.3f;
    [Tooltip("속도 → 압축 강도 커브")]
    [SerializeField] private AnimationCurve compressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ════════════════════════
    //  내부 변수
    // ════════════════════════
    private Rigidbody2D _ownRb;
    private Coroutine _springCoroutine;

    // 코일/발판 원본 트랜스폼 캐시
    private Vector3 _platformOriginalLocalPos;
    private Vector3[] _coilOriginalLocalPos;
    private Vector3[] _coilOriginalLocalScale;
    private float _totalCoilStackHeight; // 코일 더미의 전체 높이

    // 충돌 상태 추적 (인접 타일에서 옆면 → 위 접촉 전환 감지용)
    private bool _wasOnTopContact = false;
    private float _trackedImpactSpeed = 0f;
    private bool _playerWasAirborne = false;  // 점프 후 실제로 낙하했는지 여부
    private float _peakFallSpeed = 0f;        // 낙하 중 최대 속도 (착지 시 사용)
    private float _sustainedBounceHeight = 0f;

    // ════════════════════════
    //  초기화
    // ════════════════════════
    private void Awake()
    {
        // 루트에 Kinematic Rigidbody2D가 있어야 자식 Collider의 충돌 이벤트가 이 스크립트로 옵니다
        _ownRb = GetComponent<Rigidbody2D>();
        _ownRb.bodyType = RigidbodyType2D.Kinematic;
        _ownRb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void Start()
    {
        CacheOriginalTransforms();
    }

    /// <summary>
    /// 발판 + 코일의 원본 위치/스케일을 캐싱합니다.
    /// </summary>
    private void CacheOriginalTransforms()
    {
        // 발판
        if (platformTransform != null)
            _platformOriginalLocalPos = platformTransform.localPosition;

        // 코일
        if (coilTransforms == null || coilTransforms.Length == 0)
        {
            Debug.LogWarning($"[JumpObject] {gameObject.name}: Coil Transforms이 비어 있습니다. 인스펙터에서 연결해 주세요.");
            return;
        }

        _coilOriginalLocalPos   = new Vector3[coilTransforms.Length];
        _coilOriginalLocalScale = new Vector3[coilTransforms.Length];

        for (int i = 0; i < coilTransforms.Length; i++)
        {
            if (coilTransforms[i] == null)
            {
                Debug.LogWarning($"[JumpObject] Coil Transforms[{i}]가 null입니다.");
                continue;
            }
            _coilOriginalLocalPos[i]   = coilTransforms[i].localPosition;
            _coilOriginalLocalScale[i] = coilTransforms[i].localScale;
        }

        // 코일 더미 전체 높이 계산 (최상단 코일 상단 ~ 최하단 코일 하단)
        int last = coilTransforms.Length - 1;
        float topY    = _coilOriginalLocalPos[0].y    + _coilOriginalLocalScale[0].y    * 0.5f;
        float bottomY = _coilOriginalLocalPos[last].y - _coilOriginalLocalScale[last].y * 0.5f;
        _totalCoilStackHeight = Mathf.Abs(topY - bottomY);
    }

    // ════════════════════════
    //  충돌 감지 — 위에서 밟을 때만 반응
    // ════════════════════════

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;
        if (_springCoroutine != null) return;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        Vector2 activeDir = GetActiveWorldDirection();
        
        float playerVelAlongDir = playerRb != null ? Vector2.Dot(playerRb.linearVelocity, activeDir) : 0f;
        float relativeVelAlongDir = Vector2.Dot(collision.relativeVelocity, activeDir);
        
        // 물리 엔진 보정 전 속도(relative)와 보정 후 속도(linear)를 모두 확인하여 다가오고 있었는지 판정
        bool isMovingTowards = playerVelAlongDir <= 0.1f || relativeVelAlongDir > 0.1f;
        float enterSpeed = isMovingTowards ? Mathf.Abs(relativeVelAlongDir) : 0f;
        
        DE_PlayerController controller = collision.gameObject.GetComponent<DE_PlayerController>();
        if (controller != null)
        {
            // 인접한 타일에서 점프해 넘어오면서 수직 속도가 0으로 강제 보정된 경우 (방금 착지한 경우)
            // 직전 공중에서의 최대 낙하 속도를 가져와 충격량으로 사용합니다.
            if (controller.TimeSinceLanded < 0.1f && controller.LastPeakFallSpeed > enterSpeed)
            {
                enterSpeed = controller.LastPeakFallSpeed;
            }
            else if (!controller.IsGrounded)
            {
                // 플레이어가 공중에서(점프 등으로) 도달했는데 코너에 걸려 충격 속도가 너무 낮게 측정된 경우
                if (enterSpeed < activationMinSpeed)
                {
                    enterSpeed = activationMinSpeed + 0.1f;
                }
            }
        }
        
        _trackedImpactSpeed = enterSpeed;
        _playerWasAirborne = false;
        _peakFallSpeed = 0f;

        if (!IsLandedOnActiveSide(collision))
        {
            _wasOnTopContact = false;
            return;
        }

        _wasOnTopContact = true;
        ExecuteSpringBounce(collision, enterSpeed);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;
        if (_springCoroutine != null) { _wasOnTopContact = true; return; }

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        Vector2 activeDir = GetActiveWorldDirection();
        
        float playerVelAlongDir = playerRb != null ? Vector2.Dot(playerRb.linearVelocity, activeDir) : 0f;

        bool onTopNow = IsLandedOnActiveSide(collision);

        if (!onTopNow)
        {
            // 발판 해당 면을 향해 다가가고 있다면 공중에 있었다고 기록
            if (playerVelAlongDir < -0.5f)
            {
                _playerWasAirborne = true;
                float fallSpeed = Mathf.Abs(playerVelAlongDir);
                if (fallSpeed > _peakFallSpeed)
                    _peakFallSpeed = fallSpeed;
            }
        }
        else if (onTopNow && !_wasOnTopContact)
        {
            if (_playerWasAirborne)
            {
                float bestSpeed = Mathf.Max(_peakFallSpeed, _trackedImpactSpeed);
                // 공중에 있었다는 기록이 확실하므로 최소 활성화 속도를 보장합니다.
                if (bestSpeed < activationMinSpeed)
                {
                    bestSpeed = activationMinSpeed + 0.1f;
                }
                ExecuteSpringBounce(collision, bestSpeed);
            }
            _peakFallSpeed = 0f;
            _playerWasAirborne = false;
            _trackedImpactSpeed = 0f;
        }

        _wasOnTopContact = onTopNow;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;
        _wasOnTopContact = false;
        _trackedImpactSpeed = 0f;
        _playerWasAirborne = false;
        _peakFallSpeed = 0f;
    }

    public Vector2 GetActiveWorldDirection()
    {
        switch (activeSide)
        {
            case JumpSide.Top: return transform.up;
            case JumpSide.Bottom: return -transform.up;
            case JumpSide.Left: return -transform.right;
            case JumpSide.Right: return transform.right;
            default: return transform.up;
        }
    }

    private bool IsLandedOnActiveSide(Collision2D collision)
    {
        Vector2 activeDir = GetActiveWorldDirection();
        
        bool isCorrectSide = false;
        if (collision.contactCount > 0)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                // Unity 2D의 Contact Normal은 상대방(Player)에서 내 쪽(JumpPad)으로 향합니다.
                // 즉 플레이어가 위(activeDir)에서 떨어졌다면 normal은 아래(-activeDir)를 향합니다.
                if (Vector2.Dot(normal, activeDir) < -0.3f)
                {
                    isCorrectSide = true;
                    break;
                }
            }
        }

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        bool isMovingTowards = true;
        if (playerRb != null)
        {
            float currentVelDir = Vector2.Dot(playerRb.linearVelocity, activeDir);
            float relativeVelDir = Vector2.Dot(collision.relativeVelocity, activeDir);
            isMovingTowards = currentVelDir <= 0.1f || relativeVelDir > 0.1f;
        }

        return isCorrectSide && isMovingTowards;
    }

    // ════════════════════════
    //  바운스 실행
    // ════════════════════════

    private void ExecuteSpringBounce(Collision2D collision, float impactSpeed)
    {
        DE_PlayerController controller = collision.gameObject.GetComponent<DE_PlayerController>();
        Rigidbody2D playerRb           = collision.gameObject.GetComponent<Rigidbody2D>();
        Collider2D playerCol           = collision.collider;

        if (controller == null || playerRb == null)
            return;

        // 너무 살살 떨어지면(예: 그냥 걸어갈 때) 바운스 발동 안 함
        if (impactSpeed < activationMinSpeed)
            return;

        float gravity    = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y * playerRb.gravityScale));
        float fallHeight = (impactSpeed * impactSpeed) / (2f * gravity);

        Debug.Log($"[JumpObject] impactSpeed={impactSpeed:F2}  fallHeight={fallHeight:F2}");

        // 효과음
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.jump);

        bool isBounceChain = !controller.CanReceiveBounceBonus;

        float bonusApplied = 0f;
        if (controller.CanReceiveBounceBonus)
        {
            bonusApplied = bonusHeight;
            controller.CanReceiveBounceBonus = false;
        }

        float targetHeight = (fallHeight * heightMultiplier) + bonusApplied;
        targetHeight       = Mathf.Max(targetHeight, bonusHeight * 0.5f); // 최소 높이 보장
        if (isBounceChain)
        {
            targetHeight = Mathf.Max(targetHeight, _sustainedBounceHeight);
        }
        _sustainedBounceHeight = targetHeight;
        float launchSpeed  = Mathf.Sqrt(2f * gravity * targetHeight);

        // ── 압축 비율 & 체류 시간 계산 ──
        float speedRatio       = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, impactSpeed);
        float curvedRatio      = compressionCurve.Evaluate(speedRatio);
        // 최소 압축을 보장하여 살짝 밟아도 코일이 눈에 보이게 움직임
        float compressionAmt   = Mathf.Max(curvedRatio * maxCompressionRatio, 0.15f);
        float compressDuration = Mathf.Lerp(minCompressDuration, maxCompressDuration, curvedRatio);

        // ── 애니메이션 시작 ──
        if (_springCoroutine != null)
            StopCoroutine(_springCoroutine);

        _springCoroutine = StartCoroutine(
            SpringRoutine(controller, playerRb, playerCol, launchSpeed, compressionAmt, compressDuration, GetActiveWorldDirection())
        );

        Debug.Log($"[JumpObject] fallH={fallHeight:F1} | targetH={targetHeight:F1} | " + 
        $"launch={launchSpeed:F1} | compression={compressionAmt:F2} | compressSec={compressDuration:F3}s");
    }

    // ════════════════════════
    //  용수철 코루틴
    // ════════════════════════

    /// <summary>
    /// 3단계 용수철 애니메이션:
    /// 1) 압축 — 낙하 속도에 비례해 짧거나 길게 눌림 (체류 시간)
    /// 2) 해제 — 원위치로 복원되면서 플레이어 발사
    /// 3) 출렁거림 — 발사 후 감쇠 진동
    /// </summary>
    private IEnumerator SpringRoutine(
        DE_PlayerController controller,
        Rigidbody2D playerRb,
        Collider2D playerCol,
        float launchSpeed,
        float compressionAmt,
        float compressDuration,
        Vector2 activeDir)
    {
        Collider2D platformCol = platformTransform != null ? platformTransform.GetComponent<Collider2D>() : null;

        // 애니메이션 도중 충돌 처리가 플레이어의 속도를 갉아먹거나 버그를 일으키는 것을 방지
        if (platformCol != null && playerCol != null)
        {
            Physics2D.IgnoreCollision(playerCol, platformCol, true);
        }

        // 입력 잠금 (압축 + 해제 시간 동안)
        controller.InputLockTimer = compressDuration + releaseDuration + inputLockTime;

        // 플레이어를 용수철 위에 고정 (중력 & 속도 제거)
        playerRb.linearVelocity = Vector2.zero;
        playerRb.gravityScale   = 0f;

        Vector3 prevPlatformPos = platformTransform != null ? platformTransform.position : transform.position;

        // ── 1단계: 압축 ──
        float elapsed = 0f;
        while (elapsed < compressDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / compressDuration);
            float eased = EaseOutQuad(t); // 빠르게 눌리고 멈춴
            ApplyCompression(eased * compressionAmt);

            if (platformTransform != null)
            {
                Vector3 currentPlatformPos = platformTransform.position;
                Vector3 delta = currentPlatformPos - prevPlatformPos;
                playerRb.position = playerRb.position + (Vector2)delta;
                prevPlatformPos = currentPlatformPos;
            }

            // 플레이어 속도를 0으로 유지하여 제자리 부유 방지
            playerRb.linearVelocity = Vector2.zero;

            yield return null;
        }

        // ── 2단계: 해제 + 발사 ──

        // 중력 복원
        Vector2Int gridPos  = GridManager.Instance.WorldToGrid(controller.transform.position);
        Tile currentTile    = GridManager.Instance.GetTileAt(gridPos);
        playerRb.gravityScale = (currentTile != null && currentTile.InvertGravity) ? -1f : 1f;

        elapsed = 0f;
        bool launched = false;
        while (elapsed < releaseDuration)
        {
            elapsed += Time.deltaTime;
            float t         = Mathf.Clamp01(elapsed / releaseDuration);
            float eased     = EaseInQuad(t); // 천천히 시작 → 빠르게 복원
            float remaining = compressionAmt * (1f - eased);
            ApplyCompression(remaining);

            if (!launched && platformTransform != null)
            {
                Vector3 currentPlatformPos = platformTransform.position;
                Vector3 delta = currentPlatformPos - prevPlatformPos;
                playerRb.position = playerRb.position + (Vector2)delta;
                prevPlatformPos = currentPlatformPos;
            }

            // 복원 30% 지점에서 플레이어 발사
            if (!launched && t >= 0.3f)
            {
                launched = true;
                controller.ApplyExternalForce(activeDir * launchSpeed);
                controller.GetComponentInChildren<DE_PlayerVisuals>()?.TriggerBounce(activeDir);
            }

            yield return null;
        }

        // 안전 발사 (혹시 못 쐈다면)
        if (!launched)
        {
            controller.ApplyExternalForce(activeDir * launchSpeed);
            controller.GetComponentInChildren<DE_PlayerVisuals>()?.TriggerBounce(activeDir);
        }

        // ── 3단계: 출렁거림 ──
        elapsed = 0f;
        float oscAmp = compressionAmt * 0.3f;
        while (elapsed < oscillationDuration)
        {
            elapsed += Time.deltaTime;
            float t        = Mathf.Clamp01(elapsed / oscillationDuration);
            float dampened = (1f - t);
            float wave     = Mathf.Sin(t * Mathf.PI * 2f * oscillationCount) * dampened * oscAmp;
            ApplyCompression(wave);
            yield return null;
        }

        // 원위치 복원 및 충돌 복구
        if (platformCol != null && playerCol != null)
        {
            Physics2D.IgnoreCollision(playerCol, platformCol, false);
        }
        ResetToOriginal();
        _springCoroutine = null;
    }

    // ════════════════════════
    //  압축/복원 헬퍼
    // ════════════════════════

    /// <summary>
    /// compressionRatio(0~1)만큼 코일을 압축합니다.
    /// 맨 아래 코일을 고정점(앵커)으로 삼고, 위로 갈수록 더 많이 이동합니다.
    /// 음수는 살짝 늘어나는 효과 (출렁거림용).
    /// </summary>
    private void ApplyCompression(float compressionRatio)
    {
        if (_coilOriginalLocalPos == null || coilTransforms == null || coilTransforms.Length == 0)
            return;

        float absRatio = Mathf.Abs(compressionRatio);
        int last = coilTransforms.Length - 1;

        // 맨 아래 코일의 원본 하단 Y (로컬) — 이 지점은 절대로 움직이지 않습니다
        float bottomAnchorY = _coilOriginalLocalPos[last].y - _coilOriginalLocalScale[last].y * 0.5f;

        for (int i = 0; i < coilTransforms.Length; i++)
        {
            if (coilTransforms[i] == null) continue;

            // 각 코일의 원본 하단 Y (로컬)
            float coilOriginalBottomY = _coilOriginalLocalPos[i].y - _coilOriginalLocalScale[i].y * 0.5f;

            // 이 코일이 앵커(맨 아래)에서 얼마나 위에 있는지 비율 (0 = 맨 아래, 1 = 맨 위)
            float heightFromBottom = _totalCoilStackHeight > 0.001f
                ? (coilOriginalBottomY - bottomAnchorY) / _totalCoilStackHeight
                : 0f;

            // 아래 코일일수록 이동량 = 0, 위로 갈수록 최대 이동량
            // 압축이면 아래(앵커)로, 신장이면 위로 이동
            float displacement = heightFromBottom * absRatio * _totalCoilStackHeight;
            float directedDisplacement = compressionRatio >= 0f ? -displacement : displacement;

            // newY = 원본 중심 Y + 변위
            float newCenterY = _coilOriginalLocalPos[i].y + directedDisplacement;
            coilTransforms[i].localPosition = new Vector3(
                _coilOriginalLocalPos[i].x,
                newCenterY,
                _coilOriginalLocalPos[i].z
            );

            // 위에 있는 코일일수록 더 크게 찌그러짐 (아래는 거의 그대로)
            float scaleEffect = heightFromBottom * absRatio;
            float scaleY = compressionRatio >= 0f
                ? Mathf.Max(1f - scaleEffect * 0.8f, 0.1f)   // 압축: 최소 10%까지
                : 1f + scaleEffect * 0.4f;                     // 신장: 최대 40%까지 늘어남

            coilTransforms[i].localScale = new Vector3(
                _coilOriginalLocalScale[i].x,
                _coilOriginalLocalScale[i].y * scaleY,
                _coilOriginalLocalScale[i].z
            );
        }

        // 발판은 맨 위 코일(인덱스 0) 상단에 딱 붙여 이동
        if (platformTransform != null && coilTransforms[0] != null)
        {
            float coil0Top = coilTransforms[0].localPosition.y
                           + coilTransforms[0].localScale.y * 0.5f;

            platformTransform.localPosition = new Vector3(
                _platformOriginalLocalPos.x,
                coil0Top + platformTransform.localScale.y * 0.5f,
                _platformOriginalLocalPos.z
            );
        }
    }

    /// <summary>
    /// 코일 + 발판을 원래 위치/스케일로 복원합니다.
    /// </summary>
    private void ResetToOriginal()
    {
        if (coilTransforms == null) return;

        for (int i = 0; i < coilTransforms.Length; i++)
        {
            if (coilTransforms[i] == null) continue;
            coilTransforms[i].localPosition = _coilOriginalLocalPos[i];
            coilTransforms[i].localScale    = _coilOriginalLocalScale[i];
        }

        if (platformTransform != null)
            platformTransform.localPosition = _platformOriginalLocalPos;
    }

    // ── Easing ──
    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseInQuad(float t)  => t * t;

    private void OnDrawGizmosSelected()
    {
        Vector2 activeDir = transform.up;
        if (Application.isPlaying) 
        {
            activeDir = GetActiveWorldDirection();
        }
        else 
        {
            switch (activeSide)
            {
                case JumpSide.Top: activeDir = transform.up; break;
                case JumpSide.Bottom: activeDir = -transform.up; break;
                case JumpSide.Left: activeDir = -transform.right; break;
                case JumpSide.Right: activeDir = transform.right; break;
            }
        }

        Vector3 center = platformTransform != null ? platformTransform.position : transform.position;
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(center, activeDir * 1.5f);
        
        Vector3 right = Quaternion.Euler(0, 0, -135) * activeDir;
        Vector3 left = Quaternion.Euler(0, 0, 135) * activeDir;
        Gizmos.DrawRay(center + (Vector3)activeDir * 1.5f, right * 0.3f);
        Gizmos.DrawRay(center + (Vector3)activeDir * 1.5f, left * 0.3f);
    }
}
