using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public float lifetime = 1f;
    public float moveSpeed = 1f;
    public float fadeSpeed = 1f;

    private TextMeshPro textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void SetDamage(int damage)
    {
        textMesh.text = damage.ToString();
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        textMesh.alpha -= fadeSpeed * Time.deltaTime;

        if (textMesh.alpha <= 0)
        {
            Destroy(gameObject);
        }
    }
}