using UnityEngine;

public class GridEffectApplier : MonoBehaviour
{
    public Material flowMaterial; // 위에서 만든 쉐이더가 적용된 재질

    void LateUpdate()
    {
        // GridManager가 생성한 InGameGrid 오브젝트를 찾습니다.
        GameObject container = GameObject.Find("InGameGrid");
        if (container == null) return;

        // 모든 자식(LineRenderer)을 확인합니다.
        for (int i = 0; i < container.transform.childCount; i++)
        {
            Transform line = container.transform.GetChild(i);
            LineRenderer lr = line.GetComponent<LineRenderer>();

            if (lr != null)
            {
                // 월드 좌표를 기반으로 이 선이 가장자리에 있는지 판단합니다.
                // GridManager의 width, height, cellSize 정보를 가져와 계산해도 되지만
                // 여기서는 간단하게 "가장 끝에 위치한 선들"만 타겟팅합니다.
                
                if (IsEdgeLine(line.position))
                {
                    lr.material = flowMaterial;
                }
            }
        }
        
        // 성능을 위해 한 번 적용 후 스크립트를 비활성화합니다.
        this.enabled = false; 
    }

    bool IsEdgeLine(Vector3 pos)
    {
        // 로직: GridManager의 위치와 범위를 계산하여 
        // 현재 라인의 좌표가 외곽 좌표와 일치하는지 체크
        // (작업 중인 씬의 좌표값에 맞춰 세밀하게 조정 가능합니다)
        return true; // 일단 모든 라인에 적용해보고 확인하세요.
    }
}