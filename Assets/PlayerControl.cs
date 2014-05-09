using UnityEngine;
using System.Collections;

public class PlayerControl : MonoBehaviour
{

  //public GameObject ground = null;
  public GameObject bullet = null;
  public float speed = 100.0f;
  public float power = 100.0f;
  public float minPower = 0.5f;
  public float timeScale = 1.0f;
  protected Transform fKatapult = null;
  protected Transform fRohr = null;
  protected Transform fStartPos = null;
  protected Transform fLader = null;
  protected float fStartFireTime = 0.0f;
  protected bool fLoad = false;
  //protected TerrainData fTerrainData = null;
  //protected Terrain fTerrain = null;

  // Use this for initialization
  void Start ()
  {
    //if (ground == null) {
    //  ground = GameObject.Find ("Terrain");
    //}
    //fTerrain = ground.GetComponent<Terrain> ();
    //fTerrainData = fTerrain.terrainData;
    fKatapult = transform.FindChild ("Katapult");
    fRohr = fKatapult.transform.FindChild ("Rohr");
    fStartPos = fRohr.FindChild ("StartPos");
    fLader = fRohr.FindChild ("Lader");
    if (bullet == null) {
      bullet = GameObject.Find ("Kugel");
    }
  }

  // Update is called once per frame
  void Update ()
  {
    float lS = Input.GetAxis ("Speed");
    float lH = Input.GetAxis ("Horizontal");
    float lV = Input.GetAxis ("Vertical");
    fKatapult.Rotate (new Vector3 (lV * Time.deltaTime * speed, 0, 0));
    transform.Rotate (new Vector3 (0, lH * Time.deltaTime * speed, 0));
    if (Input.GetButtonDown ("Fire1")) {
      fStartFireTime = Time.time;
      fLoad = true;
    }
    if (Input.GetKeyUp(KeyCode.Keypad0)) {
      bullet.GetComponent<ColorSplash>().player = 0;
    }
    if (Input.GetKeyUp(KeyCode.Keypad1)) {
      bullet.GetComponent<ColorSplash>().player = 1;
    }
    if (Input.GetKeyUp(KeyCode.Keypad2)) {
      bullet.GetComponent<ColorSplash>().player = 2;
    }
    if (Input.GetButtonUp ("Fire1")) {
      fLoad = false;
      Vector3 lPos = fLader.localPosition;
      lPos.y = 1.0f;
      fLader.localPosition = lPos;
      float lPower = power * (minPower + ((Time.time - fStartFireTime) * timeScale));
      GameObject lBullet = Instantiate (bullet, fStartPos.position, Quaternion.identity) as GameObject;
      lBullet.rigidbody.AddForce (fRohr.TransformDirection (Vector3.up) * lPower);
    }
    if (fLoad) {
      Vector3 lPos = fLader.localPosition;
      if (lPos.y > -1.0f) {
        lPos.y -= 0.01f;
      }
      fLader.localPosition = lPos;
    }
    {
      Vector3 lPos = transform.position;
      float lMax = GameController.instance.SampleHeight(transform.position + new Vector3 (-1, 0, 1)); // fTerrain.SampleHeight (transform.position + new Vector3 (-1, 0, 1));
      float lY = GameController.instance.SampleHeight(transform.position + new Vector3 (1, 0, 1)); // fTerrain.SampleHeight (transform.position + new Vector3 (1, 0, 1));
      if (lY > lMax)
        lMax = lY;
      lY = GameController.instance.SampleHeight(transform.position + new Vector3 (1, 0, -1)); // fTerrain.SampleHeight (transform.position + new Vector3 (1, 0, -1));
      if (lY > lMax)
        lMax = lY;
      lY = GameController.instance.SampleHeight(transform.position + new Vector3 (-1, 0, -1)); // fTerrain.SampleHeight (transform.position + new Vector3 (-1, 0, -1));
      if (lY > lMax)
        lMax = lY;
      lY = GameController.instance.SampleHeight(transform.position); // fTerrain.SampleHeight (transform.position);
      if (lY > lMax)
        lMax = lY;
      lPos.y = lMax + 0.5f;
      if (lS != 0.0f) {
        lPos = lPos + transform.forward * lS;
      }
      transform.position = lPos + transform.forward * lS;
    }
  }
}
