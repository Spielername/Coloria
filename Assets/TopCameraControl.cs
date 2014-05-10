using UnityEngine;
using System.Collections;

public class TopCameraControl : MonoBehaviour {

  public GameObject player = null;
	
	// Update is called once per frame
	void FixedUpdate () {
    if (player == null) {
      player = GameController.instance.GetPlayer();
    }
    if (player != null) {
      Vector3 lPos = player.transform.position;
      lPos.y = transform.position.y;
      transform.position = lPos;
    }
	}
}
