using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ButtonController :MonoBehaviour
{
    // SpriteRenderer _spriteRenderer;
    // public Sprite defaultImage;
    // public Sprite pressedImage;
    //public KeyCode keyToPress;

    void Start()
    {
        // _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /*  void Update()
    {
        if(Input.GetKeyDown(keyToPress))
        {
            theSR.sprite = pressedImage;
        }

        if(Input.GetKeyUp(keyToPress))
        {
            theSR.sprite = defaultImage;
        }
    } */

    void ResetSprite()
    {
        // _spriteRenderer.sprite = defaultImage;
    }

    public void OnTapFromController()
    {
        // _spriteRenderer.sprite = pressedImage;
        transform.Rotate(Vector3.forward, 180f);
        GameManager.Instance.HitNote();
        Invoke(nameof(ResetSprite), 0.12f);
    }

    public void OnHoldStartFromController()
    {
        transform.Rotate(Vector3.forward, 180f);
        // _spriteRenderer.sprite = pressedImage;
        GameManager.Instance.HoldStart();
    }

    public void OnHoldEndFromController()
    {
        transform.Rotate(Vector3.forward, 180f);
        // ResetSprite();
        GameManager.Instance.HoldEnd();
    }

}
