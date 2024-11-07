using UnityEngine;

//ƒJƒƒ‰‚ğ‰ñ“]‚³‚¹‚é
public class CameraController : MonoBehaviour
{
    Vector3 lookAtPosition = Vector3.zero;

    [SerializeField]
    bool isAutoRotate;

    // Update is called once per frame
    void Update()
    {
        if (isAutoRotate)
        {
            transform.RotateAround(lookAtPosition, new Vector3(0, 1, 0), 0.01f);
        }
    }
}
