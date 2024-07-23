using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 15f;

    void Update()
    {
        //float horizontal = Input.GetAxis("Horizontal");
        //float vertical = Input.GetAxis("Vertical");

        //Vector2 movement = new Vector2(horizontal, vertical) * moveSpeed * Time.deltaTime;
        //transform.Translate(movement);
    }

    public void Move(long dt, string cmd)
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector2 movement = new Vector2(horizontal, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);
    }
}
