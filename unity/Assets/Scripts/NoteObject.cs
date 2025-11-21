using UnityEngine;

public class NoteObject :MonoBehaviour
{
    public bool canBePressed;
    public KeyCode keyToPress;

    public GameObject hiteffect, goodeffect, perfecteffect, misseffect;


    void Start()
    {

    }

    void Update()
    {
        if(!Input.GetKeyDown(keyToPress)) return;

        if(!canBePressed) return;
        gameObject.SetActive(false);
        //GameManager.instance.NoteHit();

        if(Mathf.Abs(transform.position.y) > 0.25)
        {
            Debug.Log("Hit!");
            GameManager.Instance.NormalHit();
            Instantiate(hiteffect, transform.position, hiteffect.transform.rotation);
        }
        else if(Mathf.Abs(transform.position.y) > 0.5f)
        {
            Debug.Log("Good!");
            GameManager.Instance.GoodHit();
            Instantiate(goodeffect, transform.position, goodeffect.transform.rotation);
        }
        else
        {
            Debug.Log("Perfect!");
            GameManager.Instance.PerfectHit();
            Instantiate(perfecteffect, transform.position, perfecteffect.transform.rotation);

        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.CompareTag("Activator")) return;
        canBePressed = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(!other.CompareTag("Activator")) return;
        canBePressed = false;
        GameManager.Instance.NoteMissed();
        Instantiate(misseffect, transform.position, misseffect.transform.rotation);
    }

}
