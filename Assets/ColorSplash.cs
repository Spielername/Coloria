﻿using UnityEngine;
using System.Collections;

public class ColorSplash : MonoBehaviour
{
  //public GameObject ground = null;
  public int terrainTextureNumber = 3;
  public float scaleDownFactor = 0.9f;
  public float splashFactor = 0.002f;
  public float minSize = 0.1f;
  public float splashPaintSize = 4.0f;
  public float splashHeightSize = 4.0f;
  public float maxLiveTime = 15.0f;
  protected Vector3 fCollisionPoint = Vector3.zero;
  protected Vector3 fLastCollisionPoint = Vector3.zero;
  //protected Terrain fTerrain = null;

  // Use this for initialization
  void Start ()
  {
    //if (ground == null) {
    //  ground = GameObject.Find ("Terrain");
    //}
    //fTerrain = ground.GetComponent<Terrain> ();
    Destroy(gameObject, maxLiveTime);
  }
  
  // Update is called once per frame
  void Update ()
  {
    if (fCollisionPoint != Vector3.zero) {
      if (fLastCollisionPoint == Vector3.zero || (fLastCollisionPoint - fCollisionPoint).magnitude >= 1.0f) {
        fLastCollisionPoint = fCollisionPoint;
        SplashOnMap ();
        PaintOnMap ();
      }
      transform.localScale = transform.localScale * scaleDownFactor;
      if (transform.localScale.magnitude < minSize) {
        Destroy (gameObject);
      }
    }
    //if (transform.position.y < ground.transform.position.y - fTerrain.terrainData.size.y) {
    //  Destroy (gameObject);
    //}
  }

  void SetTextureOnMap (float[,,] aAlphas, int aX, int aY, float aValue)
  {
    float lV = aAlphas [aY, aX, terrainTextureNumber];
    float lO = 1.0f - lV;
    float lNO = 1.0f - aValue;
    float lNOfak = 1.0f;
    if (lO > 0.0f) {
      lNOfak = lNO / lO;
    }
    if (aValue == 0.0f && lV >= 1.0f) {
      for (int lA = 0; lA < GameController.instance.GetAlphaMapLayerCount(); lA++) {
        if (lA == terrainTextureNumber) {
          aAlphas [aY, aX, lA] = aValue;
        } else if (lA == 0) {
          aAlphas [aY, aX, lA] = 1.0f;
        } else {
          aAlphas [aY, aX, lA] = 0.0f;
        }
      }
    } else {
      for (int lA = 0; lA < GameController.instance.GetAlphaMapLayerCount(); lA++) {
        if (lA == terrainTextureNumber) {
          aAlphas [aY, aX, lA] = aValue;
        } else {
          aAlphas [aY, aX, lA] *= lNOfak;
        }
      }
    }
  }
  
  void PaintOnMap ()
  {
    bool lModified = false;
    Vector2 lPos1 = GameController.instance.ConvertToAlphaMapPoint (fCollisionPoint - transform.localScale * splashPaintSize);
    Vector2 lPos2 = GameController.instance.ConvertToAlphaMapPoint (fCollisionPoint + transform.localScale * splashPaintSize);
    int lX = Mathf.FloorToInt (lPos1.x);
    int lY = Mathf.FloorToInt (lPos1.y);
    int lW = Mathf.FloorToInt (lPos2.x) - lX;
    int lH = Mathf.FloorToInt (lPos2.y) - lY;
    float[,,] lAlphas = GameController.instance.GetTerrainAlphas (lX, lY, lW, lH); //fTerrain.terrainData.GetAlphamaps (lX, lY, lW, lH);
    for (int lxx = 0; lxx < (lW - 1); lxx++) {
      for (int lyy = 0; lyy < (lH - 1); lyy++) {
        float lsx = (lxx - (lW / 2.0f)) / lW * 180.0f * Mathf.Deg2Rad;
        float lsy = (lyy - (lH / 2.0f)) / lH * 180.0f * Mathf.Deg2Rad;
        float lS = Mathf.Cos (lsx) * Mathf.Cos (lsy);
        if (lS > 0.0f) {
          lS += lAlphas [lyy, lxx, terrainTextureNumber];
          if (lS > 1.0f) {
            lS = 1.0f;
          }
          lModified = lModified || lAlphas [lyy, lxx, terrainTextureNumber] != lS;
          SetTextureOnMap (lAlphas, lxx, lyy, lS);
        }
      }
    }
    if (lModified) {
      GameController.instance.SetTerrainAlphas (lX, lY, lAlphas); //fTerrain.terrainData.SetAlphamaps (lX, lY, lAlphas);
    }
  }

  void SplashOnMap ()
  {
    Vector2 lPos1 = GameController.instance.ConvertToHeightMapPoint (fCollisionPoint - transform.localScale * splashHeightSize);
    Vector2 lPos2 = GameController.instance.ConvertToHeightMapPoint (fCollisionPoint + transform.localScale * splashHeightSize);
    int lX = Mathf.FloorToInt (lPos1.x);
    int lY = Mathf.FloorToInt (lPos1.y);
    int lW = Mathf.FloorToInt (lPos2.x) - lX;
    int lH = Mathf.FloorToInt (lPos2.y) - lY;
    float[,] lHeights = GameController.instance.GetTerrainHeights (lX, lY, lW, lH); // fTerrain.terrainData.GetHeights (lX, lY, lW, lH);
    for (int lxx = 0; lxx < (lW - 1); lxx++) {
      for (int lyy = 0; lyy < (lH - 1); lyy++) {
        float lsx = (lxx - (lW / 2.0f)) / lW * 180.0f * Mathf.Deg2Rad;
        float lsy = (lyy - (lH / 2.0f)) / lH * 180.0f * Mathf.Deg2Rad;
        float lS = Mathf.Cos (lsx) * Mathf.Cos (lsy);
        if (lS > 0.0f) {
          lHeights [lyy, lxx] += splashFactor * lS;
        }
      }
    }
    GameController.instance.SetTerrainHeights (lX, lY, lHeights); //fTerrain.terrainData.SetHeights (lX, lY, lHeights);
  }
  
  void OnCollisionEnter (Collision collision)
  {
    foreach (ContactPoint contact in collision.contacts) {
      if (contact.otherCollider.gameObject.name.Equals ("Terrain")) {
        fCollisionPoint = contact.point;
      }
      if (contact.otherCollider.gameObject.CompareTag ("TurmDetector")) {
        print ("Turm getroffen!");
      }
    }
  }

  void OnCollisionStay (Collision collisionInfo)
  {
    foreach (ContactPoint contact in collisionInfo.contacts) {
      if (contact.otherCollider.gameObject.name.Equals ("Terrain")) {
        fCollisionPoint = contact.point;
      }
    }
  }

  void OnCollisionExit (Collision collisionInfo)
  {
    foreach (ContactPoint contact in collisionInfo.contacts) {
      if (contact.otherCollider.gameObject.name.Equals ("Terrain")) {
        fCollisionPoint = Vector3.zero;
      }
    }
  }
}
