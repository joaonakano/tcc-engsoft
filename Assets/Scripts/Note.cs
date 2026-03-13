using UnityEngine;

public class Note : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        gameObject.transform.Translate(Vector3.left * Time.deltaTime * 5f);
    }
}
