using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [Header("Note Settings")]
    public bool canBePressed = false;
    public bool isLongNote = false;
    public bool isBeingHeld = false;
    public KeyCode keyToPress;

    [Header("Effects")]
    public GameObject hiteffect, goodeffect, perfecteffect, misseffect;

    void Update()
    {
        // Optional manual key press //to change
        if (Input.GetKeyDown(keyToPress) && canBePressed)
        {
            Pressed();
        }
    }

    // Called when a single note is hit
    public void Pressed() //to change - timely pressed
    {
        if (!canBePressed) return;

        float yDist = Mathf.Abs(transform.position.y);

        if (yDist > 0.5f)
        {
            Debug.Log("Good!");
            GameManager.instance.GoodHit();
            if (goodeffect) Instantiate(goodeffect, transform.position, goodeffect.transform.rotation);
        }
        else if (yDist > 0.25f)
        {
            Debug.Log("Hit!");
            GameManager.instance.NormalHit();
            if (hiteffect) Instantiate(hiteffect, transform.position, hiteffect.transform.rotation);
        }
        else
        {
            Debug.Log("Perfect!");
            GameManager.instance.PerfectHit();
            if (perfecteffect) Instantiate(perfecteffect, transform.position, perfecteffect.transform.rotation);
        }

        canBePressed = false;
        Destroy(gameObject);
    }

    // Called when player starts holding a long note
    public void HoldStart()
    {
        if (!isLongNote) return;
        isBeingHeld = true;
        canBePressed = false;
    }

    // Called when player stops holding a long note
    public void HoldEnd()
    {
        if (!isBeingHeld) return;
        isBeingHeld = false;

        GameManager.instance.GoodHit(); // Example: reward for holding
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Activator"))
            canBePressed = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Activator"))
        {
            canBePressed = false;
            GameManager.instance.NoteMissed();
            if (misseffect) Instantiate(misseffect, transform.position, misseffect.transform.rotation);
        }
    }
}
