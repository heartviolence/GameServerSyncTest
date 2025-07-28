
using UnityEngine;

public static class Vector3Extenstions
{
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector3 X0Z(this Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    } 

    public static Vector3 D2_D3(this Vector3 v, float yValue = 0)
    {
        return new Vector3(v.x, yValue, v.y);
    }

    public static Vector3 D3_D2(this Vector3 v)
    {
        return new Vector3(v.x, v.z,0);
    }
}
