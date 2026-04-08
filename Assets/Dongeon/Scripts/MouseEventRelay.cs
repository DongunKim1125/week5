using UnityEngine;

/// <summary>
/// 자식 오브젝트(검은 플랫폼 등)의 콜라이더에서 발생한 마우스 이벤트를
/// 부모의 TileDragger로 전달하는 릴레이 컴포넌트.
/// 검은 자식 오브젝트에 이 스크립트를 추가하면 타일 드래그가 정상 작동함.
/// </summary>
public class MouseEventRelay : MonoBehaviour
{
    private TileDragger _parentDragger;

    private void Awake()
    {
        // 부모 계층에서 TileDragger를 찾음
        _parentDragger = GetComponentInParent<TileDragger>();
    }

    private void OnMouseDown()
    {
        if (_parentDragger != null)
            _parentDragger.OnMouseDown();
    }

    private void OnMouseDrag()
    {
        if (_parentDragger != null)
            _parentDragger.OnMouseDrag();
    }

    private void OnMouseUp()
    {
        if (_parentDragger != null)
            _parentDragger.OnMouseUp();
    }
}
