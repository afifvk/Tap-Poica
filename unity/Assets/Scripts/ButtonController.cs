using UnityEngine;

public class ButtonController :MonoBehaviour

{
    SpriteRenderer _spriteRenderer;
    public Sprite defaultImage;
    public Sprite pressedImage;
    //public KeyCode keyToPress;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

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
        _spriteRenderer.sprite = defaultImage;
    }

    public void OnTapFromController()
    {
        _spriteRenderer.sprite = pressedImage;
        GameManager.Instance.HitNote();
        Invoke(nameof(ResetSprite), 0.12f);
    }

    public void OnHoldStartFromController()
    {
        _spriteRenderer.sprite = pressedImage;
        GameManager.Instance.HoldStart();
    }

    public void OnHoldEndFromController()
    {
        ResetSprite();
        GameManager.Instance.HoldEnd();
    }

}
