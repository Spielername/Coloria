using UnityEngine;
using System.Collections;

public class ParticleSplash : MonoBehaviour
{
  public GameObject SplashEffect = null;
  protected Vector3 fCollisionPoint = Vector3.zero;
  private ParticleSystem myParticles;
  public float playTime = 1.0f;

  // Use this for initialization
  void Start ()
  {
    if (SplashEffect == null) {
      SplashEffect = GameObject.Find ("Particle_Splash");
    }
  }
  
  // Update is called once per frame
  void Update ()
  {
  
  }

  void OnCollisionEnter (Collision collision)
  {
    foreach (ContactPoint contact in collision.contacts) {
      if (contact.otherCollider.gameObject.CompareTag ("Terrain")) {
        fCollisionPoint = contact.point;
        
        Quaternion rot = Quaternion.FromToRotation (Vector3.up, contact.normal);
        Vector3 pos = contact.point;
        if (SplashEffect != null) {
          GameObject myParticles = Instantiate (SplashEffect, pos, rot) as GameObject;
          myParticles.transform.parent = GameController.instance.GetTempObjectContainer();
          Destroy (myParticles, playTime);
        }
      }
    }
  }
}
