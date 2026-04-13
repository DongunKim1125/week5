using UnityEngine;

public class GoalEffect : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 90f;

    [Header("Pulse")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    
    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        // // 1. 회전 연출
        // transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        // 2. 박동(Pulse) 연출: 사인파를 이용해 크기 조절
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = initialScale + new Vector3(scaleOffset, scaleOffset, 0);
    }
}
