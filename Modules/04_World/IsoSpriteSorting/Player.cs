using UnityEngine;

public class Player : MonoBehaviour {
    public Transform t;
    public Rigidbody2D rgd;
    private const float speed = 125f;
    void Update() {
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
            rgd.linearVelocity = new Vector2(0, -speed) * Time.deltaTime;
        } else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
            rgd.linearVelocity = new Vector2(0, speed) * Time.deltaTime;
        } else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
            rgd.linearVelocity = new Vector2(-speed, 0) * Time.deltaTime;
        } else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
            rgd.linearVelocity = new Vector2(speed, 0) * Time.deltaTime;
        } else {
            rgd.linearVelocity = Vector2.zero;
        }
        rgd.angularVelocity = 0;
        rgd.rotation = 0;
    }
}
