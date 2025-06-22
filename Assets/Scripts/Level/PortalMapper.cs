using UnityEngine;

public class PortalMapper : MonoBehaviour
{
    private AudioSource audioSource;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameManager.Instance.SwitchToNextMap();
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        Debug.Log("PortalMapper enabled at position: " + transform.position);
        if (audioSource != null)
        {
            audioSource.Play();
            Debug.Log("PortalMapper audio started");
        }
    }

    private void OnDisable()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("PortalMapper audio stopped");
        }
    }

}
