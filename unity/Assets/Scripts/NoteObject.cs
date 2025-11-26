using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [Header("Note Settings")] public NoteType noteType;
    public KeyCode keyToPress;
    public SpriteRenderer line;
    public BoxCollider2D lineCollider;
    public CircleCollider2D tailCollider, headCollider;

    const float WindowGood = 2f;
    const float WindowNormal = 1.5f;

    [Header("Effects")] public GameObject hitEffect;
    public GameObject goodEffect, perfectEffect, missEffect;

    [HideInInspector] public bool isBeingHeld;
    [HideInInspector] public bool canBePressed;
    float _speed;
    double _duration;
    float _lifetime;

    public void Initialize(NoteData data, float speed)
    {
        _duration = data.durationMs;
        _speed = speed;

        if (noteType != NoteType.Long) return;

        var visualHeight = speed * (float)_duration / 1000f;
        transform.Translate(Vector3.up * visualHeight);
        line.transform.localScale = new Vector3(line.transform.localScale.x, visualHeight, line.transform.localScale.z);
        tailCollider.transform.localPosition = Vector3.up * visualHeight / 2f;
        headCollider.transform.localPosition = Vector3.down * visualHeight / 2f;
    }

    void FixedUpdate()
    {
        _lifetime += Time.fixedDeltaTime;
        transform.Translate(_speed * Time.fixedDeltaTime * Vector3.down);
        // Optional manual key press //to change
        if (!Input.GetKeyDown(keyToPress) || !canBePressed) return;
        Pressed();

        if (noteType != NoteType.Long) return;

        if (Input.GetKey(keyToPress) && canBePressed && !isBeingHeld)
        {
            // Logic to start holding if we hit the head successfully
            // (Usually called inside Pressed() if it's a long note)
        }

        if (!Input.GetKeyUp(keyToPress) || !isBeingHeld) return;
        HoldEnd();
    }

    // Called when a single note is hit
    public void Pressed() //to change - timely pressed
    {
        if (!canBePressed) return;

        var yDist = Mathf.Abs(transform.position.y);

        Judge(yDist);

        canBePressed = false;
        Destroy(gameObject);
    }

    // Called when player starts holding a long note
    public void HoldStart()
    {
        if (noteType != NoteType.Long) return;
        isBeingHeld = true;

        var yDist = Mathf.Abs(transform.position.y);

        Judge(yDist);
        // canBePressed = false;
    }

    // Called when player stops holding a long note
    public void HoldEnd()
    {
        if (!isBeingHeld) return;
        isBeingHeld = false;

        // Calculate Tail Distance
        var headY = transform.position.y;
        var noteHeight = transform.localScale.y;
        var tailY = headY + noteHeight;

        var dist = Mathf.Abs(tailY); // assuming judgment line is at 0

        // Use EXACT same judgment as Pressed
        Judge(dist);

        GameManager.Instance.GoodHit(); // Example: reward for holding
        Destroy(gameObject);
    }

    void SpawnEffect(GameObject effectPrefab)
    {
        if (effectPrefab) Instantiate(effectPrefab, transform.position, effectPrefab.transform.rotation);
    }

    void Judge(float distance)
    {
        switch (distance)
        {
            case > WindowGood: // > 0.5
                // Debug.Log("Good!");
                GameManager.Instance.GoodHit();
                SpawnEffect(goodEffect);
                break;

            case > WindowNormal: // > 0.25
                // Debug.Log("Hit!");
                GameManager.Instance.NormalHit();
                SpawnEffect(hitEffect);
                break;

            default: // <= 0.25 (The best one)
                // Debug.Log("Perfect!");
                GameManager.Instance.PerfectHit();
                SpawnEffect(perfectEffect);
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Activator")) return;
        canBePressed = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Activator")) return;
        Debug.Log(_lifetime);
        canBePressed = false;
        GameManager.Instance.NoteMissed();
        SpawnEffect(missEffect);
        
        if (noteType == NoteType.Long && isBeingHeld)
        {
            Destroy(lineCollider.gameObject);
            Destroy(headCollider.gameObject);
            Destroy(tailCollider.gameObject);
        }
        
        Destroy(gameObject);
    }
}
