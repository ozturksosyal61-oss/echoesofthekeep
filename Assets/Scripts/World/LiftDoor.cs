using UnityEngine;

namespace CoopGame
{
    /// <summary>Yukarı kalkan kapı paneli (yerel görsel + collider). Open'ı PuzzleDoor veya bulmaca görselleri ayarlar.</summary>
    public class LiftDoor : MonoBehaviour
    {
        [SerializeField] float lift = 3.2f;
        [SerializeField] float speed = 6f;

        public bool Open { get; set; }

        Vector3 closedPos;
        Collider col;

        void Awake()
        {
            closedPos = transform.position;
            col = GetComponent<Collider>();
        }

        void Update()
        {
            var target = closedPos + (Open ? Vector3.up * lift : Vector3.zero);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if (col != null) col.enabled = !Open;
        }
    }
}
