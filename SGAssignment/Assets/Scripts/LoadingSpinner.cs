using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [SerializeField]
    private float _spinSpeed = 360f;

    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    private void Update()
    {
        transform.localEulerAngles += Vector3.forward * _spinSpeed * Time.deltaTime;
    }
}
