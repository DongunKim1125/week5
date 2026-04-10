using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 이동 가능한 타일의 외곽선을 그리는 컴포넌트.
///
/// [렌더러 구조]
/// _loopLR    : loop = true, 4점 → 모서리 이음새 없는 완벽한 사각형.
///              호버 시 또는 4면이 전부 외곽일 때 사용.
/// _segLR[2]  : 부분 외곽 시 연속 구간(L·U자형)을 그리는 경로 렌더러 (최대 2개).
///
/// [색상 정책]
/// - 일반 상태 : 버텍스 색 = Color.white (= 재질 색 그대로 표시, 별도 tint 없음)
/// - 호버 상태 : 버텍스 색 = hoverColor (인스펙터에서 설정)
///
/// [호버 감지]
/// Tile.cs 수정 없이 GridManager.WorldToGrid 로 직접 판별.
/// </summary>
[RequireComponent(typeof(Tile))]
public class MovableTileEdgeLine : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("선에 적용할 재질")]
    public Material lineMaterial;
    
    // ⬇️ 새로 추가하는 변수: 평상시 선 색상
    [Tooltip("일반 상태의 선 색상 (주황색으로 설정하세요)")]
    public Color normalColor = new Color(1f, 0.5f, 0f, 1f);

    [Tooltip("타일 크기의 절반값 (GridManager cellSize = 1 기준 → 0.5 권장, 시각 조정 가능)")]
    public float halfSize = 1.5f;

    [Tooltip("선의 두께")]
    public float lineWidth = 0.08f;

    [Tooltip("타일 위로 선이 표시되도록 하는 Z축 오프셋 (음수일수록 카메라와 가까움)")]
    public float zOffset = 0f;

    [Header("Hover Effect")]
    [Tooltip("호버 시 선 색상 (일반 상태는 재질 기본색 사용)")]
    public Color hoverColor = Color.white;

    [Tooltip("호버 시 선 두께 배율 (1.0 = 동일)")]
    public float hoverLineWidthMultiplier = 1.5f;

    // =========================================================
    // ⬇️ 새로 추가된 펄스(Pulse) 효과 설정
    // =========================================================
    [Header("Pulse Effect (Alpha)")]
    [Tooltip("투명도 깜빡임(펄스) 효과 사용 여부")]
    public bool usePulseEffect = true;
    
    [Tooltip("최소 투명도 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    
    [Tooltip("최대 투명도 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] public float maxAlpha = 1.0f;
    
    [Tooltip("투명도가 변하는 속도")]
    public float pulseSpeed = 3f;
    // =========================================================

    // ─────────────────────────────────────────────────────────
    // 내부 상수
    // ─────────────────────────────────────────────────────────
    // 코너 인덱스: 0=TopLeft, 1=TopRight, 2=BottomRight, 3=BottomLeft
    private static readonly int[] EdgeStartCorner = { 0, 1, 2, 3 }; // 엣지→시작 코너
    private static readonly int[] EdgeEndCorner   = { 1, 2, 3, 0 }; // 엣지→끝 코너

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,    // 0: Top
        Vector2Int.right, // 1: Right
        Vector2Int.down,  // 2: Bottom
        Vector2Int.left,  // 3: Left
    };

    // ─────────────────────────────────────────────────────────
    // 런타임 상태
    // ─────────────────────────────────────────────────────────
    private Vector3[]     _corners;
    private LineRenderer  _loopLR;
    private LineRenderer[] _segLR = new LineRenderer[2];

    private Tile   _tile;
    private Camera _mainCamera;
    private bool   _isHovered;

    // ─────────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────────
    private void Start()
    {
        _tile       = GetComponent<Tile>();
        _mainCamera = Camera.main;

        _corners = new Vector3[]
        {
            new Vector3(-halfSize,  halfSize, zOffset), // 0: TopLeft
            new Vector3( halfSize,  halfSize, zOffset), // 1: TopRight
            new Vector3( halfSize, -halfSize, zOffset), // 2: BottomRight
            new Vector3(-halfSize, -halfSize, zOffset), // 3: BottomLeft
        };

        _loopLR  = CreateLoopRenderer();
        _segLR[0] = CreateSegmentRenderer("Seg_0");
        _segLR[1] = CreateSegmentRenderer("Seg_1");
    }

    // ─────────────────────────────────────────────────────────
    // 매 프레임: 시각 효과의 부드러움을 위해 Update로 변경
    // ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (_tile == null || GridManager.Instance == null || _mainCamera == null) return;

        // 1. 모두 숨김 (매 프레임 클린 슬레이트)
        HideAll();

        bool isTileActive = _tile.CanMove && !_tile.IsOccupiedByPlayer;
        if (!isTileActive) return;

        // 2. 호버 감지
        Vector3    mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int mouseGrid  = GridManager.Instance.WorldToGrid(mouseWorld);
        _isHovered = (mouseGrid == _tile.GridPosition);

        // 3. 호버 상태 → 루프 렌더러로 4면 전체 표시 (hoverColor 적용, 마우스를 올렸을 땐 선명하게)
        if (_isHovered)
        {
            ApplyToLR(_loopLR, hoverColor, lineWidth * hoverLineWidthMultiplier);
            _loopLR.enabled = true;
            return;
        }

        // =========================================================
        // 4. 일반 상태 알파값 계산 (Sine 함수를 이용한 펄스)
        // =========================================================
        float currentAlpha = normalColor.a; // 인스펙터에서 설정한 기본 알파값
        if (usePulseEffect)
        {
            // Mathf.Sin은 -1 ~ 1 사이를 왕복하므로, 이를 0 ~ 1 사이로 정규화
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }
        
        // 투명도가 적용된 기본 색상 (normalColor의 RGB를 그대로 가져옴)
        Color normalColorWithPulse = new Color(normalColor.r, normalColor.g, normalColor.b, currentAlpha);
        // =========================================================


        bool[] visible     = GetVisibleEdges();
        int    visibleCount = CountTrue(visible);

        if (visibleCount == 0) return;

        if (visibleCount == 4)
        {
            // 4면 모두 외곽 → 루프 렌더러 사용 (모서리 완벽 이음)
            ApplyToLR(_loopLR, normalColorWithPulse, lineWidth);
            _loopLR.enabled = true;
        }
        else
        {
            // 일부 면만 외곽 → 연속 구간별 꺾인 경로 렌더러 사용
            DrawSegments(visible, normalColorWithPulse);
        }
    }

    // ─────────────────────────────────────────────────────────
    // 외곽 엣지 계산
    // ─────────────────────────────────────────────────────────
    private bool[] GetVisibleEdges()
    {
        bool[] visible = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            Tile neighbor = GridManager.Instance.GetTileAt(_tile.GridPosition + Directions[i]);
            bool connected = neighbor != null &&
                             neighbor.CanMove &&
                             !neighbor.IsOccupiedByPlayer;
            visible[i] = !connected;
        }
        return visible;
    }

    // ─────────────────────────────────────────────────────────
    // 연속 구간 탐색 (순환 배열 처리)
    // ─────────────────────────────────────────────────────────
    private List<List<int>> FindContiguousRuns(bool[] visible)
    {
        var runs    = new List<List<int>>();
        List<int> cur = null;

        for (int i = 0; i < 4; i++)
        {
            if (visible[i])
            {
                if (cur == null) cur = new List<int>();
                cur.Add(i);
            }
            else
            {
                if (cur != null) { runs.Add(cur); cur = null; }
            }
        }
        if (cur != null) runs.Add(cur);

        if (runs.Count >= 2)
        {
            var first = runs[0];
            var last  = runs[runs.Count - 1];
            if (last[last.Count - 1] == 3 && first[0] == 0)
            {
                last.AddRange(first);
                runs.RemoveAt(0);
            }
        }

        return runs;
    }

    // ─────────────────────────────────────────────────────────
    // 세그먼트 렌더러 설정 (투명도 색상 전달받도록 수정됨)
    // ─────────────────────────────────────────────────────────
    private void DrawSegments(bool[] visible, Color applyColor)
    {
        List<List<int>> runs = FindContiguousRuns(visible);

        for (int r = 0; r < _segLR.Length; r++)
        {
            if (r >= runs.Count) break; 

            List<int>    run = runs[r];
            LineRenderer lr  = _segLR[r];

            lr.positionCount = run.Count + 1;
            lr.SetPosition(0, _corners[EdgeStartCorner[run[0]]]);
            for (int i = 0; i < run.Count; i++)
                lr.SetPosition(i + 1, _corners[EdgeEndCorner[run[i]]]);

            ApplyToLR(lr, applyColor, lineWidth);
            lr.enabled = true;
        }
    }

    // ─────────────────────────────────────────────────────────
    // LineRenderer 색상/두께 적용
    // ─────────────────────────────────────────────────────────
    private static void ApplyToLR(LineRenderer lr, Color color, float width)
    {
        lr.startColor = color;
        lr.endColor   = color;
        lr.startWidth = width;
        lr.endWidth   = width;
    }

    // ─────────────────────────────────────────────────────────
    // 전체 숨김 (매 프레임 시작 시 호출)
    // ─────────────────────────────────────────────────────────
    private void HideAll()
    {
        if (_loopLR != null) _loopLR.enabled = false;
        foreach (var lr in _segLR) if (lr != null) lr.enabled = false;
    }

    // ─────────────────────────────────────────────────────────
    // 렌더러 생성 헬퍼
    // ─────────────────────────────────────────────────────────
    private LineRenderer CreateLoopRenderer()
    {
        var obj = new GameObject("Edge_Loop");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        var lr = obj.AddComponent<LineRenderer>();
        lr.material          = lineMaterial;
        lr.loop              = true;
        lr.useWorldSpace     = false;
        lr.numCornerVertices = 8;
        lr.positionCount     = 4;
        lr.sortingOrder      = 15;
        lr.enabled           = false;

        lr.SetPosition(0, _corners[0]);
        lr.SetPosition(1, _corners[1]);
        lr.SetPosition(2, _corners[2]);
        lr.SetPosition(3, _corners[3]);

        return lr;
    }

    private LineRenderer CreateSegmentRenderer(string objName)
    {
        var obj = new GameObject(objName);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        var lr = obj.AddComponent<LineRenderer>();
        lr.material          = lineMaterial;
        lr.loop              = false;
        lr.useWorldSpace     = false;
        lr.numCapVertices    = 4;
        lr.numCornerVertices = 4;
        lr.sortingOrder      = 15;
        lr.enabled           = false;

        return lr;
    }

    private static int CountTrue(bool[] arr)
    {
        int n = 0;
        foreach (var b in arr) if (b) n++;
        return n;
    }
}