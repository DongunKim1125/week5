using UnityEngine;

public class GridEffectApplier : MonoBehaviour
{
    [Header("Glow Settings")]
    public Material flowMaterial;      // 위에서 만든 쉐이더가 할당된 재질
    public float glowLineWidth = 0.08f; // 흐르는 빛의 선 두께

    private void Start()
    {
        // GridManager의 그리드 생성이 끝난 직후 테두리를 그리도록 지연 실행
        Invoke(nameof(CreateContinuousEdgeLine), 0.1f);
    }

    private void CreateContinuousEdgeLine()
    {
        // 1. GridManager가 생성한 선들의 부모 오브젝트 찾기
        Transform container = transform.Find("InGameGrid");
        if (container == null) return;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        // 2. 생성된 모든 선의 위치를 순회하여 가장 바깥쪽(테두리) 좌표 추출
        foreach (Transform child in container)
        {
            LineRenderer lr = child.GetComponent<LineRenderer>();
            if (lr != null && lr.positionCount >= 2)
            {
                Vector3 p0 = lr.GetPosition(0);
                Vector3 p1 = lr.GetPosition(1);

                minX = Mathf.Min(minX, p0.x, p1.x);
                minY = Mathf.Min(minY, p0.y, p1.y);
                maxX = Mathf.Max(maxX, p0.x, p1.x);
                maxY = Mathf.Max(maxY, p0.y, p1.y);
            }
        }

        // 3. 외곽을 감싸는 새로운 단일(한 줄) LineRenderer 생성
        GameObject glowObj = new GameObject("PerimeterGlowLine");
        glowObj.transform.SetParent(transform);

        LineRenderer glowLine = glowObj.AddComponent<LineRenderer>();
        glowLine.material = flowMaterial;
        
        // 빛이 원래 선보다 살짝 위(카메라 쪽)에 보이도록 Z값 조정
        float zPos = 0.05f; 

        // 4. 모서리를 연결하여 사각형을 그리기 위해 점을 5개로 설정 (루프)
        glowLine.positionCount = 5;
        glowLine.SetPosition(0, new Vector3(minX, minY, zPos)); // 좌측 하단
        glowLine.SetPosition(1, new Vector3(minX, maxY, zPos)); // 좌측 상단
        glowLine.SetPosition(2, new Vector3(maxX, maxY, zPos)); // 우측 상단
        glowLine.SetPosition(3, new Vector3(maxX, minY, zPos)); // 우측 하단
        glowLine.SetPosition(4, new Vector3(minX, minY, zPos)); // 좌측 하단 (마무리)

        // 5. 쉐이더 흐름을 위한 핵심 설정
        glowLine.useWorldSpace = true;
        glowLine.startWidth = glowLineWidth;
        glowLine.endWidth = glowLineWidth;
        
        // TextureMode.Stretch: 전체 선(5개의 점)을 하나의 UV(0~1)로 취급하여 쉐이더가 한 줄로 흐르게 만듭니다.
        glowLine.textureMode = LineTextureMode.Stretch; 
        glowLine.sortingOrder = 10; // 기존 노란색 선(-10) 위로 명확하게 렌더링되도록 설정
    }
}