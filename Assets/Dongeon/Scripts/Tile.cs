using UnityEngine;
using System.Collections.Generic;

public enum TileType { Normal, Fixed, KeyLocked }

public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private TileType tileType = TileType.Normal;
    [SerializeField] private int lockID = 0; 
    [SerializeField] private bool isLocked = false; 
    [SerializeField] private bool isOccupiedByPlayer;
    [SerializeField] private bool invertGravity;

    [Header("Visuals (For Player)")]
    [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.8f);       
    [SerializeField] private Color unlockedColor = Color.white;                   
    [SerializeField] private Color invertGravityColor = new Color(0.8f, 0.8f, 1f);

    [Header("Interaction Visuals")]
    [Tooltip("마우스를 올렸을 때 외곽선 색상")]
    private Color hoverOutlineColor = new Color(0.549f, 0.549f, 0.549f); 
    [Tooltip("드래그 중일 때 외곽선 색상")]
    private Color dragOutlineColor = Color.white; 
    [Tooltip("외곽선 두께 (1.0 = 타일과 동일, 1.04 = 4% 더 크게 → 가장자리만 튀어나와 테두리처럼 보임)")]
    private float outlineScale = 1.02f;
    [Tooltip("호버/드래그 외곽선이 인접 타일에 가려지지 않도록 올릴 Sorting Order 값")]
    [SerializeField] private int hoverSortingOrderOffset = 50;
    
    // 추가됨: 고정 타일일 때 최종 색상의 밝기를 얼마나 줄일 것인가 (1 = 그대로, 0.85 = 15% 어둡게)
    [Tooltip("고정 타일일 때 적용할 밝기 배율")]
    [SerializeField] private float fixedBrightness = 0.85f;
    
    [SerializeField] private SpriteRenderer borderRenderer;

    [Header("Fixed Tile Visuals (Auto Generated)")]
    [Tooltip("인스펙터에서 못 이미지를 넣어주세요.")]
    [SerializeField] private Sprite nailSprite;
    [Tooltip("모서리에서 안쪽으로 얼마나 들어올지 결정합니다.")]
    [SerializeField] private float nailOffset = 0.4f; 

    private GameObject[] _generatedNails = new GameObject[4];
    private SpriteRenderer _outlineRenderer; // 외곽선 전용 렌더러 (자동 생성)
    private int _baseBorderSortingOrder;    // borderRenderer의 원래 sortingOrder (복구용)
    private Dictionary<Renderer, int> _hoverOriginalSortingOrders = new Dictionary<Renderer, int>(); // 호버 시 복구용

    public int LockID => lockID;
    public TileType Type => tileType;
    public Vector2Int GridPosition { get; set; }
    public bool IsOccupiedByPlayer { get => isOccupiedByPlayer; set => isOccupiedByPlayer = value; }
    public bool InvertGravity => invertGravity;

    public bool CanMove => (tileType == TileType.Normal || (tileType == TileType.KeyLocked && !isLocked)) && !isOccupiedByPlayer;

    private void Awake()
    {
        GenerateFixedVisuals();
    }

    private void Start()
    {
        GridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.RegisterTile(this);
        transform.position = GridManager.Instance.GridToWorld(GridPosition);
        
        GenerateOutline(); // 외곽선 오브젝트 자동 생성
        UpdateState();
    }

    private void GenerateFixedVisuals()
    {
        float halfSize = 0.5f; 

        // 점선 로직 제거됨, 못 4개만 생성
        if (nailSprite != null)
        {
            Vector3[] corners = new Vector3[4]
            {
                new Vector3(-halfSize + nailOffset, halfSize - nailOffset, -0.1f), 
                new Vector3(halfSize - nailOffset, halfSize - nailOffset, -0.1f),  
                new Vector3(-halfSize + nailOffset, -halfSize + nailOffset, -0.1f),
                new Vector3(halfSize - nailOffset, -halfSize + nailOffset, -0.1f)  
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject nailObj = new GameObject($"Nail_{i}_Auto");
                nailObj.transform.SetParent(transform);
                nailObj.transform.localPosition = corners[i];
                nailObj.transform.localScale = Vector3.one * 0.5f; 
                
                SpriteRenderer sr = nailObj.AddComponent<SpriteRenderer>();
                sr.sprite = nailSprite;
                sr.sortingOrder = 6;
                
                _generatedNails[i] = nailObj;
            }
        }
        
        ToggleFixedVisuals(false);
    }

    public void Unlock()
    {
        isLocked = false;
        UpdateState();
    }

    private void UpdateState()
    {
        if (tileType == TileType.Normal) isLocked = false;
        GetComponent<BoxCollider2D>().isTrigger = (tileType != TileType.KeyLocked || !isLocked);
        ApplyColorPriority();
    }

    private void ApplyColorPriority()
    {
        // 1. 기본이 되는 상태(베이스 컬러)를 먼저 결정합니다.
        Color targetColor = unlockedColor;

        if (isLocked)
        {
            targetColor = lockedColor;
        }
        else if (invertGravity)
        {
            targetColor = invertGravityColor;
        }

        // 2. 만약 고정 타일(Fixed)이라면, 위에서 결정된 베이스 컬러에 밝기 배율을 곱합니다.
        if (tileType == TileType.Fixed)
        {
            targetColor = new Color(
                targetColor.r * fixedBrightness, 
                targetColor.g * fixedBrightness, 
                targetColor.b * fixedBrightness, 
                targetColor.a
            );
        }

        // 3. 렌더러에 최종 색상 적용
        if (borderRenderer != null)
        {
            borderRenderer.color = targetColor;
        }

        // 고정 타일 여부에 따라 못 켜기/끄기
        ToggleFixedVisuals(tileType == TileType.Fixed);
    }

    private void ToggleFixedVisuals(bool isActive)
    {
        for (int i = 0; i < 4; i++)
        {
            if (_generatedNails[i] != null)
            {
                _generatedNails[i].SetActive(isActive);
            }
        }
    }

    public void SetGridPosition(Vector2Int newPos)
    {
        GridPosition = newPos;
        transform.position = GridManager.Instance.GridToWorld(newPos);
    }

    /// <summary>
    /// 외곽선 전용 GameObject를 자동 생성.
    /// borderRenderer와 동일한 스프라이트를 outlineScale 배 크게 배치하고
    /// sortingOrder를 borderRenderer보다 낮게 설정해 가장자리만 튀어나오게 함.
    /// </summary>
    private void GenerateOutline()
    {
        if (borderRenderer == null || borderRenderer.sprite == null) return;

        // borderRenderer 원래 sortingOrder 저장 (SetHoverState에서 복구에 사용)
        _baseBorderSortingOrder = borderRenderer.sortingOrder;

        GameObject outlineObj = new GameObject("TileOutline_Auto");
        outlineObj.transform.SetParent(transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localScale = Vector3.one * outlineScale;

        _outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        _outlineRenderer.sprite = borderRenderer.sprite;
        _outlineRenderer.sortingOrder = _baseBorderSortingOrder - 1; // 기본: 배경 뒤에 배치
        _outlineRenderer.color = Color.clear; // 기본 투명
    }

    /// <summary>
    /// 마우스 호버 외곽선 표시/해제.
    /// 호버 시 outline과 borderRenderer의 sortingOrder를 함께 올려
    /// 인접 타일의 배경에 가려지지 않도록 처리.
    /// </summary>
    public void SetHoverState(bool isHovered)
    {
        if (_outlineRenderer == null) return;

        if (isHovered)
        {
            // 1. 자식 포함 모든 렌더러(outline 제외)를 hoverSortingOrderOffset만큼 올림
            //    → 자식 플랫폼들도 outline 위에 유지되어 가려지지 않음
            _hoverOriginalSortingOrders.Clear();
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in allRenderers)
            {
                if (r == _outlineRenderer) continue; // outline은 별도 처리
                _hoverOriginalSortingOrders[r] = r.sortingOrder;
                r.sortingOrder += hoverSortingOrderOffset;
            }

            // 2. outline은 올라간 borderRenderer보다 1 낮게 설정
            //    → 인접 타일보다는 위, 자신의 borderRenderer보다는 아래 → 테두리만 보임
            _outlineRenderer.sortingOrder = _baseBorderSortingOrder + hoverSortingOrderOffset - 1;
            _outlineRenderer.color = hoverOutlineColor;
        }
        else
        {
            // 모든 렌더러 원래 sortingOrder로 복구
            foreach (var kvp in _hoverOriginalSortingOrders)
            {
                if (kvp.Key != null)
                    kvp.Key.sortingOrder = kvp.Value;
            }
            _hoverOriginalSortingOrders.Clear();

            _outlineRenderer.sortingOrder = _baseBorderSortingOrder - 1;
            _outlineRenderer.color = Color.clear;
        }
    }

    /// <summary>
    /// 드래그 외곽선 표시/해제.
    /// 드래그 중 sortingOrder는 TileInputHandler가 모든 렌더러에 일괄 적용하므로
    /// 이 메서드에서는 색상만 제어.
    /// </summary>
    public void SetDragState(bool isDragging)
    {
        if (_outlineRenderer == null) return;
        _outlineRenderer.color = isDragging ? dragOutlineColor : Color.clear;
    }

    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) isOccupiedByPlayer = true; }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) isOccupiedByPlayer = false; }

    private void OnValidate() 
    { 
        if (tileType == TileType.Normal) isLocked = false;
        ApplyColorPriority();
    }
}