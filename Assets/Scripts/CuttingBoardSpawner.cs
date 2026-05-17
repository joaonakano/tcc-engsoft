using UnityEngine;

public class CuttingBoardSpawner : MonoBehaviour
{
    public GameObject tileComeco;
    public GameObject tileFim;
    public GameObject[] tilesMeio;

    private float tileWidth = 4.0f;
    public float horizonteDeVisao = 15f;

    public Transform cameraTransform;
    private float nextTileX;

    void Start()
    {
        nextTileX = transform.position.x;

        Instantiate(tileComeco, new Vector3(nextTileX, -0.95f, 0), Quaternion.identity);
        nextTileX += tileWidth;
    }

    private void Update()
    {
        if (cameraTransform.position.x + horizonteDeVisao > nextTileX)
        {
            SpawnProximoTile();
        }
    }

    void SpawnProximoTile()
    {
        GameObject prefab = tilesMeio[Random.Range(0, tilesMeio.Length)];

        Vector3 spawnPos = new Vector3(nextTileX, -0.95f, 0);
        GameObject novoTile = Instantiate(prefab, spawnPos, Quaternion.identity);

        nextTileX += tileWidth;

        Destroy(novoTile, 20f);
    }
}
