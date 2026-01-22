using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    void Start()
    {
        //гарантируем наличие коллайдера
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    public void Init(int xCoord, int yCoord)
    {
        x = xCoord;
        y = yCoord;
        name = $"Cell_{x}_{y}";
    }

    public Vector2Int GetCoordinates()
    {
        return new Vector2Int(x, y);
    }

    void OnMouseDown()
    {
        Debug.Log($"Клик по клетке: ({x}, {y})");
    }
}
