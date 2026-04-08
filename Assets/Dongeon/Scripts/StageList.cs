using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageList", menuName = "DongeonSystem/StageList")]
public class StageList : ScriptableObject
{
    public List<StageData> stages = new List<StageData>(); // 모든 스테이지 데이터 목록
}
