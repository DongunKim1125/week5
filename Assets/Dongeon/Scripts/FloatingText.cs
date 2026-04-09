using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// 지정한 월드 좌표에서 위로 떠오르다 서서히 사라지는 플로팅 힌트 텍스트.
/// FloatingText.Show()로 동적 생성하며, 지속 시간이 끝나면 자동으로 소멸함.
/// </summary>
public class FloatingText : MonoBehaviour
{
    private TextMeshPro _tmp;
    private float _floatSpeed;
    private float _duration;
    private float _fadeStartRatio; // 전체 시간의 몇 % 지점부터 페이드 시작할지 (0~1)

    /// <summary>
    /// 플로팅 텍스트를 월드 좌표에 생성하고 애니메이션을 시작함.
    /// </summary>
    /// <param name="message">표시할 문구</param>
    /// <param name="worldPos">생성할 월드 좌표</param>
    /// <param name="textColor">텍스트 색상</param>
    /// <param name="fontSize">폰트 크기 (월드 단위)</param>
    /// <param name="floatSpeed">초당 위로 이동하는 거리</param>
    /// <param name="duration">총 표시 시간 (초)</param>
    /// <param name="fadeStartRatio">이 비율 이후부터 페이드 시작 (0.6 = 60% 지점부터)</param>
    public static FloatingText Show(
        string message,
        Vector3 worldPos,
        Color textColor,
        TMP_FontAsset fontAsset = null,
        float fontSize = 0.35f,
        float floatSpeed = 0.4f,
        float duration = 3f,
        float fadeStartRatio = 0.6f,
        float outlineWidth = 0.2f) // 외곽선 두께 매개변수 추가 (기본값 0.2)
    {
        GameObject go = new GameObject("FloatingText_Auto");
        go.transform.position = worldPos;

        FloatingText ft = go.AddComponent<FloatingText>();
        ft._floatSpeed = floatSpeed;
        ft._duration = duration;
        ft._fadeStartRatio = Mathf.Clamp01(fadeStartRatio);

        ft._tmp = go.AddComponent<TextMeshPro>();
        if (fontAsset != null) ft._tmp.font = fontAsset;
        ft._tmp.text = message;
        ft._tmp.color = textColor;
        ft._tmp.fontSize = fontSize;
        ft._tmp.alignment = TextAlignmentOptions.Center;
        ft._tmp.sortingOrder = 300;

        // --- 외곽선 설정 추가 ---
        ft._tmp.outlineColor = Color.black; // 검은색 외곽선 고정
        ft._tmp.outlineWidth = outlineWidth; // 인스펙터에서 받아온 두께 적용
        // -----------------------

        ft.StartCoroutine(ft.Animate());
        return ft;
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        Color originalColor = _tmp.color;
        Vector3 startPos = transform.position;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;

            // 위로 둥둥 이동
            transform.position = startPos + Vector3.up * (_floatSpeed * elapsed);

            // fadeStartRatio 이후부터 서서히 투명해짐
            if (t >= _fadeStartRatio)
            {
                float fadeT = (t - _fadeStartRatio) / (1f - _fadeStartRatio);
                _tmp.color = new Color(
                    originalColor.r,
                    originalColor.g,
                    originalColor.b,
                    Mathf.Lerp(1f, 0f, fadeT)
                );
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
