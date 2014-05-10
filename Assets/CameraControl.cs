using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour {

  public float smooth = 3.5f;

  public GameObject player = null;
	
  void SmoothLookAt (Transform lPlayer)
  {
    // Create a vector from the camera towards the player.
    Vector3 relPlayerPosition = lPlayer.position - transform.position;
    
    // Create a rotation based on the relative position of the player being the forward vector.
    Quaternion lookAtRotation = Quaternion.LookRotation (relPlayerPosition, Vector3.up);
    
    // Lerp the camera's rotation between it's current rotation and the rotation that looks at the player.
    transform.rotation = Quaternion.Lerp (transform.rotation, lookAtRotation, smooth * Time.deltaTime);
  }

  // Update is called once per frame
	void FixedUpdate () {
    if (player == null) {
      player = GameController.instance.GetPlayer();
    }
    if (player != null) {
      Vector3 lPos = player.transform.position + (player.transform.forward * 8.0f) + Vector3.up * 5.0f;
      transform.position = Vector3.Lerp(transform.position, lPos, smooth * Time.deltaTime);
      SmoothLookAt(player.transform);
      //transform.LookAt(player.transform.position);
    }
	}
}
