using UnityEngine;
using System.Collections;

public class TurmKI : MonoBehaviour
{

  public float radius = 30.0f;
  protected Transform fPlattform = null;
  protected Transform fKatapult = null;
  protected Transform fKatapultBase = null;
  protected Transform fRohr = null;
  protected Transform fLader = null;
  protected Transform fHoehe = null;
  protected Quaternion fKatapultOrgRot;
  protected GameObject fTargetPlayer = null;

  // Use this for initialization
  void Start ()
  {
    fPlattform = transform.FindChild ("Plattform");
    fHoehe = transform.FindChild ("Hoehe");
    fKatapultBase = fHoehe.FindChild ("KatapultBase");
    fKatapult = fKatapultBase.FindChild ("Katapult");
    fRohr = fKatapult.FindChild ("Rohr");
    fLader = fKatapult.FindChild ("Lader");
    fKatapultOrgRot = fKatapult.rotation;
  }
  
  // Update is called once per frame
  void Update ()
  {
    if (fTargetPlayer == null) {
      GameObject[] lPlayers = GameObject.FindGameObjectsWithTag ("Player");
      foreach (GameObject lPlayer in lPlayers) {
        float lDist = (lPlayer.transform.position - transform.position).magnitude;
        if (lDist <= radius) {
          //print(lPlayer.name);
          fTargetPlayer = lPlayer;
        }
      }
    } else {
      if ((fTargetPlayer.transform.position - transform.position).magnitude > radius) {
        fTargetPlayer = null;
        fKatapult.rotation = fKatapultOrgRot;
      }
    }
    if (fTargetPlayer != null) {
      //fKatapult.Rotate(new Vector3(1,0,0));
      fKatapult.rotation = Quaternion.LookRotation(fKatapult.position - fTargetPlayer.transform.position, Vector3.up);
      //fKatapult.LookAt(fTargetPlayer.transform.position);
    }
//    Collider[] lCols = Physics.OverlapSphere(transform.position, radius);
//    foreach(Collider lCol in lCols) {
//      if (lCol.gameObject.name.Equals("Panzer")) {
//        print (lCol);
//      }
//    }
  }
}
