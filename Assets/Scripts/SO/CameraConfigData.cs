using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfigData", menuName = "SO Config/Camera Config")]
public class CameraConfigData : ScriptableObject
{
    public Vector3 Offset = new(0, 16f, -6.5f);
}
