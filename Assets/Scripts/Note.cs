using UnityEngine;

public class Note : MonoBehaviour
{
    float distance;

    private Transform hitLine;

    public void SetHitline(Transform line)
    {
        hitLine = line;
    }

    void Update()
    {
        gameObject.transform.Translate(Vector3.left * Time.deltaTime * 5f);

        distance = Mathf.Abs(transform.position.x - hitLine.position.x);

        if (distance < 0.5f)
        {
            //Debug.Log("Hit!");
            Destroy(gameObject);
        }

        //Debug.Log($"Distancia Nota: {distance}");
    }
}
