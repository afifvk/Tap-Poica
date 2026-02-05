using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ButtonController :MonoBehaviour
{
    // SpriteRenderer _spriteRenderer;
    // public Sprite defaultImage;
    // public Sprite pressedImage;
    // public KeyCode keyToPress;

    void Start()
    {
        // _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // if(Input.GetKeyDown(keyToPress))
        // {
        // _spriteRenderer.sprite = pressedImage;
        // Testing. Change later
        // OnTapFromController();
        // OnHoldStartFromController();
        // }

        // if(!Input.GetKeyUp(keyToPress)) return;
        // _spriteRenderer.sprite = defaultImage;
        // Testing. Change later
        // OnHoldEndFromController();
    }

    void ResetSprite()
    {
        // _spriteRenderer.sprite = defaultImage;
    }

    public void OnTapFromController()
    {
        // _spriteRenderer.sprite = pressedImage;
        transform.Rotate(Vector3.forward, 180f);
        GameManager.HitNote();
        // Invoke(nameof(ResetSprite), 0.12f);
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
