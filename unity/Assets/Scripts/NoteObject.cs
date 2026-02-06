using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class NoteObject :MonoBehaviour
{
    [Header("Note Settings")] public NoteType noteType;
    public KeyCode keyToPress;
    public SpriteRenderer line;
    public BoxCollider2D lineCollider;
    public CircleCollider2D tailCollider, headCollider;

    const float WindowMissMs = 400f;
    const float WindowGoodMs = 250f;
    const float WindowNormalMs = 120f;

    [Header("Effects")] public GameObject hitEffect;
    public GameObject goodEffect, perfectEffect, missEffect;

    [HideInInspector] public bool isBeingHeld;
    float _speed;
    double _durationMs;
    public float LifetimeMs { get; private set; }
    bool _hasBeenJudged;

    public void Initialize(NoteData data, float speed, float lifetimeMs)
    {
        _durationMs = data.durationMs;
        _speed = speed;
        LifetimeMs = lifetimeMs;

        if(noteType != NoteType.Long) return;

        var visualHeight = speed * (float)_durationMs / 1000f;
        transform.Translate(Vector3.up * visualHeight);
        line.transform.localScale = new Vector3(line.transform.localScale.x, visualHeight, line.transform.localScale.z);
        lineCollider.transform.localScale = new Vector3(lineCollider.transform.localScale.x, visualHeight,
            lineCollider.transform.localScale.z);
        tailCollider.transform.localPosition = Vector3.up * visualHeight / 2f;
        headCollider.transform.localPosition = Vector3.down * visualHeight / 2f;
    }

    public void OnDestroy()
    {
        if(noteType != NoteType.Long) return;
        Destroy(lineCollider.gameObject);
        Destroy(headCollider.gameObject);
        Destroy(tailCollider.gameObject);
    }

    void FixedUpdate()
    {
        LifetimeMs -= Time.fixedDeltaTime * 1000f;
        transform.Translate(_speed * Time.fixedDeltaTime * Vector3.down);

        if(!TooLate()) return;
        GameManager.Instance.NoteMissed();
        SpawnEffect(missEffect);
        Destroy(gameObject);

        // Should be called in GameManager instead
        // TryToPress();
    }

    public bool CanBePressed()
    {
        return LifetimeMs <= WindowMissMs;
    }

    bool TooLate()
    {
        return noteType switch
        {
            NoteType.Short => LifetimeMs < -WindowMissMs,
            NoteType.Long => LifetimeMs + (float)_durationMs < -WindowMissMs,
            _ => true
        };
    }

    // Called when a single note is hit
    public void Pressed()
    {
        Judge(LifetimeMs);
        Destroy(gameObject);
    }

    // Called when player starts holding a long note
    public void HoldStart()
    {
        if(noteType != NoteType.Long) return;
        isBeingHeld = true;
        Judge(LifetimeMs);
    }

    // Called when player stops holding a long note
    public void HoldEnd()
    {
        if(!isBeingHeld) return;
        isBeingHeld = false;

        Judge(LifetimeMs + (float)_durationMs);

        if(noteType == NoteType.Long && isBeingHeld)
        {
            Destroy(lineCollider.gameObject);
            // Destroy(headCollider.gameObject);
            Destroy(tailCollider.gameObject);
        }
        else
            Destroy(gameObject);

    }

    void SpawnEffect(GameObject effectPrefab)
    {
        if(effectPrefab)
            Instantiate(
                effectPrefab,
                new Vector3(
                    // Add random offset for better visibility
                    transform.position.x + Random.Range(-0.4f, 0.4f),
                    1f + Random.Range(0.2f, 0.2f),
                    transform.position.z),
                effectPrefab.transform.rotation);
    }

    void Judge(float hitTimeMs)
    {
        if (_hasBeenJudged) return;
        switch (Mathf.Abs(hitTimeMs))
        {
            case > WindowGoodMs:
                // Debug.Log("Good!");
                GameManager.Instance.GoodHit();
                SpawnEffect(goodEffect);
                break;

            case > WindowNormalMs:
                // Debug.Log("Hit!");
                GameManager.Instance.NormalHit();
                SpawnEffect(hitEffect);
                break;

            default:
                // Debug.Log("Perfect!");
                GameManager.Instance.PerfectHit();
                SpawnEffect(perfectEffect);
                break;
        }
    }
}
