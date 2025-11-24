using UnityEngine;

public class ButtonController :MonoBehaviour

{
    private SpriteRenderer theSR;
    public Sprite defaultImage;
    public Sprite pressedImage;
   //public KeyCode keyToPress;

    void Start()
    {
        theSR = GetComponent<SpriteRenderer>();

    }

    void ResetSprite()
        {
        theSR.sprite = defaultImage;
    /*  void Update()
      {
          if(Input.GetKeyDown(keyToPress))
          {
              theSR.sprite = pressedImage;
          }

          if(Input.GetKeyUp(keyToPress))
          {
              theSR.sprite = defaultImage;
          } */  

      }

    public void OnTapFromController()
    {
        theSR.sprite = pressedImage;
        GameManager.instance.HitNote();
        Invoke(nameof(ResetSprite), 0.12f);
    }

    public void OnHoldStartFromController()
    {
        theSR.sprite = pressedImage;
        GameManager.instance.HoldStart();
    }

    public void OnHoldEndFromController()
    {
        ResetSprite();
        GameManager.instance.HoldEnd();
    }

}
