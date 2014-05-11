using UnityEngine;
using System.Collections;

public class TurmKI : MonoBehaviour
{

  public float radius = 30.0f;
  public GameObject bullet = null;
  public float power = 200.0f;
  protected Transform fPlattform = null;
  protected Transform fKatapult = null;
  protected Transform fKatapultBase = null;
  protected Transform fRohr = null;
  protected Transform fLader = null;
  protected Transform fHoehe = null;
  protected Transform fStartPos = null;
  protected Quaternion fKatapultOrgRot;
  protected Vector3 fLaderOrgPos;
  protected GameObject fTargetPlayer = null;
  protected bool fStartFire = false;

  // Use this for initialization
  void Start ()
  {
    if (bullet == null) {
      bullet = GameObject.Find ("Kugel_Base");
    }
    fPlattform = transform.FindChild ("Plattform");
    fHoehe = transform.FindChild ("Hoehe");
    fKatapultBase = fHoehe.FindChild ("KatapultBase");
    fKatapult = fKatapultBase.FindChild ("Katapult");
    fRohr = fKatapult.FindChild ("Rohr");
    fLader = fRohr.FindChild ("Lader");
    fStartPos = fRohr.FindChild ("StartPos");
    fKatapultOrgRot = fKatapult.rotation;
    fLaderOrgPos = fLader.localPosition;
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
        fStartFire = false;
        fLader.localPosition = fLaderOrgPos;
        fKatapult.rotation = fKatapultOrgRot;
      }
    }
    if (fTargetPlayer != null) {
      if (fStartFire) {
        if (fLader.localPosition.y >= 0) {
          fTargetPlayer = null;
          fStartFire = false;
          fLader.localPosition = fLaderOrgPos;
          float lPower = power; //power * (minPower + ((Time.time - fStartFireTime) * timeScale));
          if (Network.isServer) {
            GameObject lBullet = Network.Instantiate (bullet, fStartPos.position, Quaternion.identity, GameController.RPC_GROUP_WEAPON) as GameObject;
            lBullet.transform.parent = GameController.instance.GetTempObjectContainer();
            lBullet.rigidbody.AddForce (fRohr.TransformDirection (Vector3.down) * lPower);
          }
        } else {
          fLader.localPosition += Vector3.up * 0.05f;
        }
      } else {
        Quaternion lDestRot = Quaternion.LookRotation (fKatapult.position - fTargetPlayer.transform.position, Vector3.up);
        fKatapult.rotation = Quaternion.Lerp (fKatapult.rotation, lDestRot, 0.1f);
        if ((fKatapult.rotation.eulerAngles - lDestRot.eulerAngles).magnitude < 0.1f) {
          fStartFire = true;
        }
      }
    }
  }
}
