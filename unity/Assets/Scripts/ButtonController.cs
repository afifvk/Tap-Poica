using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ButtonController :MonoBehaviour
{

    public void OnTapFromController()
    {
        transform.Rotate(Vector3.forward, 180f);
        GameManager.HitNote();
    }

    public void OnHoldStartFromController()
    {
        transform.Rotate(Vector3.forward, 180f);
        GameManager.Instance.HoldStart();
    }

    public void OnHoldEndFromController()
    {
        transform.Rotate(Vector3.forward, 180f);
        GameManager.Instance.HoldEnd();
    }

}
