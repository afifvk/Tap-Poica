using UnityEngine;

public class NoteMover : MonoBehaviour
{
    public float speed = 5f;   // tweak in Inspector

    void Update()
    {
        // Move straight down every frame
        transform.position += Vector3.down * speed * Time.deltaTime;
    }
}