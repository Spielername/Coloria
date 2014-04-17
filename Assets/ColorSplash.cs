﻿using UnityEngine;
using System.Collections;

public class ColorSplash : MonoBehaviour
{

  public GameObject ground = null;
  public int terrainTextureNumber = 3;
  public float scaleDownFactor = 0.75f;
  public float minSize = 0.1f;
  protected Vector3 fCollisionPoint = Vector3.zero;
  protected Terrain fTerrain = null;

  // Use this for initialization
  void Start ()
  {
    if (ground == null) {
      ground = GameObject.Find ("Terrain");
    }
    fTerrain = ground.GetComponent<Terrain> ();
  }
  
  // Update is called once per frame
  void Update ()
  {
    if (fCollisionPoint != Vector3.zero) {
      SplashOnMap();
      //Invoke("PaintOnMap", 0.0f);
      PaintOnMap ();
      transform.localScale = transform.localScale * scaleDownFactor;
      if (transform.localScale.magnitude < minSize) {
        Destroy (gameObject);
      }
    }
  }

  Vector2 toAlphaMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - ground.transform.position;
    lPos.x = lPos.x / fTerrain.terrainData.size.x * fTerrain.terrainData.alphamapWidth;
    lPos.z = lPos.z / fTerrain.terrainData.size.z * fTerrain.terrainData.alphamapHeight;
    return new Vector2 (lPos.x, lPos.z);
  }

  Vector2 toHeightMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - ground.transform.position;
    lPos.x = lPos.x / fTerrain.terrainData.size.x * fTerrain.terrainData.heightmapWidth;
    lPos.z = lPos.z / fTerrain.terrainData.size.z * fTerrain.terrainData.heightmapHeight;
    return new Vector2 (lPos.x, lPos.z);
  }

  void SetTextureOnMap(float[,,] aAlphas, int aX, int aY, float aValue) {
    float lV = aAlphas [aX, aY, terrainTextureNumber];
    float lO = 1.0f - lV;
    float lNO = 1.0f - (lV - aValue);
    for (int lA = 0; lA < fTerrain.terrainData.alphamapLayers; lA++) {
      if (lA == terrainTextureNumber) {
        aAlphas [aX, aY, lA] = aValue;
      } else {
        aAlphas [aX, aY, lA] = 0.0f;
      }
    }
  }
  
  void PaintOnMap ()
  {
    Vector2 lPos1 = toAlphaMapPoint (fCollisionPoint - transform.localScale * 4.0f);
    Vector2 lPos2 = toAlphaMapPoint (fCollisionPoint + transform.localScale * 4.0f);
    //Vector3 lPos = fCollisionPoint - ground.transform.position;
    //lPos.x = lPos.x / fTerrain.terrainData.size.x * fTerrain.terrainData.alphamapWidth;
    //lPos.z = lPos.z / fTerrain.terrainData.size.z * fTerrain.terrainData.alphamapHeight;
    int lX = Mathf.FloorToInt (lPos1.x);
    int lY = Mathf.FloorToInt (lPos1.y);
    int lW = Mathf.FloorToInt (lPos2.x) - lX;
    int lH = Mathf.FloorToInt (lPos2.y) - lY;
    //print ("Pos: " + lPos + " X=" + lX + " Y=" + lY);
    float[,,] lAlphas = fTerrain.terrainData.GetAlphamaps (lX, lY, lW, lH);
    for (int lxx = 0; lxx < (lW - 1); lxx++) {
      for (int lyy = 0; lyy < (lH - 1); lyy++) {
        SetTextureOnMap(lAlphas, lxx, lyy, 1.0f);
        /*
        for (int lA = 0; lA < fTerrain.terrainData.alphamapLayers; lA++) {
          if (lA == terrainTextureNumber) {
            lAlphas [lxx, lyy, lA] = 1.0f;
          } else {
            lAlphas [lxx, lyy, lA] = 0.0f;
          }
        }
        */
      }
    }
    fTerrain.terrainData.SetAlphamaps (lX, lY, lAlphas);
  }

  void SplashOnMap ()
  {
    Vector2 lPos1 = toHeightMapPoint (fCollisionPoint - transform.localScale);
    Vector2 lPos2 = toHeightMapPoint (fCollisionPoint + transform.localScale);
    int lX = Mathf.FloorToInt (lPos1.x);
    int lY = Mathf.FloorToInt (lPos1.y);
    int lW = Mathf.FloorToInt (lPos2.x) - lX;
    int lH = Mathf.FloorToInt (lPos2.y) - lY;
    float[,] lHeights = fTerrain.terrainData.GetHeights (lX, lY, lW, lH);
    for (int lxx = 0; lxx < (lW - 1); lxx++) {
      for (int lyy = 0; lyy < (lH - 1); lyy++) {
        lHeights [lxx, lyy] += 0.001f;
      }
    }
    fTerrain.terrainData.SetHeights (lX, lY, lHeights);
  }
  
  void OnCollisionEnter (Collision collision)
  {
    foreach (ContactPoint contact in collision.contacts) {
      if (contact.otherCollider.gameObject.name.Equals ("Terrain")) {
        fCollisionPoint = contact.point;
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
