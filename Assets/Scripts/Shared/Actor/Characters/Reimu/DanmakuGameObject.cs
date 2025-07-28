
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DanmakuGameObject : MonoBehaviour
{ 
    public Transform Model;      

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
        Model.position = pos;
    }
}