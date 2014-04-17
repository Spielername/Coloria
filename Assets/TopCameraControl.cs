using UnityEngine;
using System.Collections;

public class TopCameraControl : MonoBehaviour {

  public GameObject player = null;
	// Use this for initialization
	void Start () {
	  if (player == null) {
      player = GameObject.Find("Panzer");
    }
	}
	
	// Update is called once per frame
	void FixedUpdate () {
    Vector3 lPos = player.transform.position;
    lPos.y = transform.position.y;
    transform.position = lPos;
	}
}
