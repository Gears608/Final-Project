using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float cameraSpeed = 20f;   //iniializes the variable which holds the speed which the tracker can move
    public float cameraBoarderWidth = 10f;  //initializes the variable which holds the size of the boarder round the screen which moves the player camera when the cursor enters
    public float scrollSpeed = 10f; //initializses the variable which holds the speed at which the player can scroll in and out
    public float minimumZ = 0f;       //initialzises the variable which holds the minimum z position that the player camera can be
    public float maximumZ = 20f;  //initializes the variable which holds the maximum z position that the player camera can be

    void Update()
    {
        Move();
    }

    /*
     * A function which handles the movement of the camera
     */
    private void Move()
    {
        Vector2 movement = new Vector2(0, 0);

        if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - cameraBoarderWidth)  //requirements for moving up
        {
            if (transform.position.y < 50)
            {
                movement.y += cameraSpeed;  //increases the movement in the y axis
            }
        }

        if (Input.GetKey("s") || Input.mousePosition.y <= cameraBoarderWidth) //requirements for moving down
        {
            if (transform.position.y > 0)
            {
                movement.y -= cameraSpeed; //decreases the movement in the y axis
            }
        }

        if (Input.GetKey("a") || Input.mousePosition.x <= cameraBoarderWidth) //requirements for moving left
        {
            if (transform.position.x > 0)
            {
                movement.x -= cameraSpeed; //decreases the movement in the x axis
            }
        }

        if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - cameraBoarderWidth) //requirements for moving right
        {
            if (transform.position.x < 100)
            {
                movement.x += cameraSpeed; //increases the movement in the x axis
            }
        }

        transform.Translate(movement * Time.deltaTime);  //moves the tracker to the new position

        Vector3 currentPosition = transform.position;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentPosition.z += scroll * scrollSpeed * 1000f * Time.deltaTime;
        currentPosition.z = Mathf.Clamp(currentPosition.z, -maximumZ, -minimumZ);  //prevents the camera from going too high or low
        transform.position = (currentPosition);
    }
}
