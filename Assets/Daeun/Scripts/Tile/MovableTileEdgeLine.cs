using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 이동 가능한 타일의 외곽선을 그리는 컴포넌트.
/// </summary>
[RequireComponent(typeof(Tile))]
public class MovableTileEdgeLine : MonoBehaviour
{
    [Header("Line Settings")]
    public Material lineMaterial;
    public Color normalColor = new Color(1f, 0.5f, 0f, 1f);
    public float halfSize = 1.5f;
    public float lineWidth = 0.08f;
    public float zOffset = 0f;

    [Header("Hover Effect")]
    public Color hoverColor = Color.white;
    public float hoverLineWidthMultiplier = 1.5f;

    [Header("Pulse Effect (Alpha)")]
    public bool usePulseEffect = true;
    [Range(0f, 1f)] public float minAlpha = 0.0f;
    [Range(0f, 1f)] public float maxAlpha = 1.0f;
    public float pulseSpeed = 2f;

    // ─────────────────────────────────────────────────────────
    // 내부 상수
    // ─────────────────────────────────────────────────────────
    private static readonly int[] EdgeStartCorner = { 0, 1, 2, 3 };
    private static readonly int[] EdgeEndCorner   = { 1, 2, 3, 0 };

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
    private DE_PlayerController _playerController;

    // =========================================================
    // ★ 핵심 수정: 모든 타일이 완벽히 동일한 주기를 가지도록 정적(Static) 변수로 변경
    // =========================================================
    private static float s_currentAlphaRatio = 0f; 
    private static float s_pulseTimer = 0f;        
    private static int s_lastUpdateFrame = -1;
    // =========================================================

    private void Start()
    {
        _tile       = GetComponent<Tile>();
        _mainCamera = Camera.main;
        _playerController = FindFirstObjectByType<DE_PlayerController>();

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

    private void Update()
    {
        if (_tile == null || GridManager.Instance == null || _mainCamera == null) return;

        HideAll();

        bool isTileActive = _tile.Type != TileType.Fixed && !_tile.IsDragging;
        if (!isTileActive) return;

        if (_loopLR != null) _loopLR.transform.rotation = Quaternion.identity;
        foreach (var lr in _segLR) if (lr != null) lr.transform.rotation = Quaternion.identity;

        Vector3    mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int mouseGrid  = GridManager.Instance.WorldToGrid(mouseWorld);
        _isHovered = (mouseGrid == _tile.GridPosition);

        // =========================================================
        // 1. 전역 펄스 타이머 업데이트 (호버 감지 이전에 무조건 실행)
        // =========================================================
        if (s_lastUpdateFrame != Time.frameCount)
        {
            s_lastUpdateFrame = Time.frameCount;
            if (usePulseEffect && _playerController != null)
            {
                if (!_playerController.IsInputting)
                {
                    // 조작을 멈추면 먼저 비율이 1(100%)로 차오름
                    s_currentAlphaRatio = Mathf.Clamp01(s_currentAlphaRatio + Time.deltaTime * pulseSpeed);
                    
                    // ★ 수정된 부분: 서서히 켜지는 효과가 100% 완료된 시점부터 펄스 타이머를 시작함
                    if (s_currentAlphaRatio >= 1f)
                    {
                        s_pulseTimer += Time.deltaTime; 
                    }
                }
                else
                {
                    // 조작 중이면 빠르게 비율이 깎이고, 펄스 타이머를 0으로 초기화
                    s_currentAlphaRatio = Mathf.Clamp01(s_currentAlphaRatio - Time.deltaTime * (pulseSpeed * 2f)); 
                    s_pulseTimer = 0f; 
                }
            }
        }

        // 2. 호버 상태 → 루프 렌더러로 4면 전체 표시 후 조기 종료
        if (_isHovered)
        {
            ApplyToLR(_loopLR, hoverColor, lineWidth * hoverLineWidthMultiplier);
            _loopLR.enabled = true;
            return; 
        }

        // 3. 일반 상태 알파값 계산 (전역 변수 사용)
        float currentAlpha = minAlpha; 
        
        if (usePulseEffect && _playerController != null)
        {
            float pulseT = (Mathf.Cos(s_pulseTimer * pulseSpeed) + 1f) / 2f;
            float targetPulseAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulseT);

            currentAlpha = targetPulseAlpha * s_currentAlphaRatio;
        }
        else if (!usePulseEffect)
        {
            currentAlpha = normalColor.a;
        }
        
        Color normalColorWithPulse = new Color(normalColor.r, normalColor.g, normalColor.b, currentAlpha);
        
        if (currentAlpha <= 0.01f) return;

        // 4. 선 그리기
        bool[] visible     = GetVisibleEdges();
        int    visibleCount = CountTrue(visible);

        if (visibleCount == 0) return;

        if (visibleCount == 4)
        {
            ApplyToLR(_loopLR, normalColorWithPulse, lineWidth);
            _loopLR.enabled = true;
        }
        else
        {
            DrawSegments(visible, normalColorWithPulse);
        }
    }

    private bool[] GetVisibleEdges()
    {
        bool[] visible = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            Tile neighbor = GridManager.Instance.GetTileAt(_tile.GridPosition + Directions[i]);
            bool connected = neighbor != null && neighbor.Type != TileType.Fixed;
            visible[i] = !connected;
        }
        return visible;
    }

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

    private static void ApplyToLR(LineRenderer lr, Color color, float width)
    {
        lr.startColor = color;
        lr.endColor   = color;
        lr.startWidth = width;
        lr.endWidth   = width;
    }

    private void HideAll()
    {
        if (_loopLR != null) _loopLR.enabled = false;
        foreach (var lr in _segLR) if (lr != null) lr.enabled = false;
    }

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