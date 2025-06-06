using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private static GameObject popupPrefab;
    
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    
    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }
    
    public static DamagePopup Create(Vector3 position, float damageAmount)
    {
        // Tạo prefab nếu chưa có
        if (popupPrefab == null)
        {
            popupPrefab = Resources.Load<GameObject>("Prefabs/DamagePopup");
        }
        
        // Tạo instance
        GameObject popupObject = Instantiate(popupPrefab, position, Quaternion.identity);
        DamagePopup popup = popupObject.GetComponent<DamagePopup>();
        popup.Setup(damageAmount);
        
        return popup;
    }
    
    public void Setup(float damageAmount)
    {
        textMesh.text = damageAmount.ToString("0");
        textColor = textMesh.color;
        disappearTimer = 1f;
        moveVector = new Vector3(0.7f, 1f) * 3f;
    }
    
    private void Update()
    {
        // Di chuyển popup
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;
        
        // Giảm dần độ trong suốt
        if (disappearTimer > 0)
        {
            disappearTimer -= Time.deltaTime;
            
            if (disappearTimer <= 0)
            {
                // Bắt đầu biến mất
                textColor.a = 0;
                textMesh.color = textColor;
                Destroy(gameObject);
            }
            else
            {
                // Giảm dần độ trong suốt
                textColor.a = disappearTimer;
                textMesh.color = textColor;
            }
        }
    }
}
