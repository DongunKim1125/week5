using UnityEngine;

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

    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) isOccupiedByPlayer = true; }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) isOccupiedByPlayer = false; }

    private void OnValidate() 
    { 
        if (tileType == TileType.Normal) isLocked = false;
        ApplyColorPriority();
    }
}