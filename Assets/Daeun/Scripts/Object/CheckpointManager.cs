/// <summary>
/// 씬 리로드 이후에도 체크포인트 위치를 유지하기 위한 정적 매니저.
/// static 필드는 Unity 씬 전환에도 파괴되지 않으므로 별도 DontDestroyOnLoad 없이 작동함.
/// </summary>
public static class CheckpointManager
{
    private static bool _hasCheckpoint = false;
    private static UnityEngine.Vector3 _spawnPosition;

    /// 현재 유효한 체크포인트가 존재하는지 여부

    public static bool HasCheckpoint => _hasCheckpoint;


    /// 체크포인트를 새 위치로 등록함. 이미 등록된 체크포인트가 있어도 덮어씀.
    public static void SetCheckpoint(UnityEngine.Vector3 position)
    {
        _spawnPosition = position;
        _hasCheckpoint = true;
        UnityEngine.Debug.Log($"<color=cyan>[Checkpoint] 스폰 포인트 저장: {position}</color>");
    }

    /// 저장된 체크포인트 위치를 반환하고 즉시 초기화.
    /// DE_PlayerHealth.Start()에서 한 번만 소비하도록 설계됨.

    public static UnityEngine.Vector3 ConsumeCheckpoint()
    {
        _hasCheckpoint = false;
        return _spawnPosition;
    }

  
    /// 체크포인트 초기화 (스테이지 선택 화면 복귀 등에서 호출)
    public static void ClearCheckpoint()
    {
        _hasCheckpoint = false;
        UnityEngine.Debug.Log("<color=cyan>[Checkpoint] 체크포인트 초기화됨</color>");
    }
}