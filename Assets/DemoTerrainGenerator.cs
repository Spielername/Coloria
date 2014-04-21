using UnityEngine;
using System.Collections;

public class DemoTerrainGenerator : MonoBehaviour
{

  public int mode = 0;
  public float scale = 0.5f;
  public float scaleWidth = 10.0f;
  public float scaleHeight = 10.0f;
  public float speed = 10.0f;
  protected Terrain fTerrain = null;
  protected TerrainData fTerrainData = null;
  protected float fTime = 0;
  protected float fStartTime = 0;
  protected float fStep = 0;
  protected float[,] fNewHeights;
  protected float[,,] fNewAlphas;

  // Use this for initialization
  void Start ()
  {
    fTerrain = GetComponent<Terrain> ();
    fTerrainData = fTerrain.terrainData;
    fTime = Time.time;
  }
  
  // Update is called once per frame
  void Update ()
  {
    if (fTime <= Time.time) {
      fStartTime = Time.time;
      fTime = Time.time + speed;
      switch (mode) {
      case 0:
        DoPerlin ();
        break;
      }
    }
    fStep = (Time.time - fStartTime) / (fTime - fStartTime);
    LerpHeights ();
    LerpAlphas ();
  }

  float Lerp (float v0, float v1, float t)
  {
    return v0 + (v1 - v0) * t;
  }

  void LerpHeights ()
  {
    int lw = fTerrainData.heightmapWidth;
    int lh = fTerrainData.heightmapHeight;
    float[,] lHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        lHeights [lx, ly] = Lerp (lHeights [lx, ly], fNewHeights [lx, ly], fStep);
      }
    }
    fTerrainData.SetHeights (0, 0, lHeights);
  }

  void LerpAlphas ()
  {
    int lw = fTerrainData.alphamapWidth;
    int lh = fTerrainData.alphamapHeight;
    float[,,] lAlphas = fTerrainData.GetAlphamaps (0, 0, lw, lh);
    for (int lx = 0; lx < (lw - 1); lx++) {
      for (int ly = 0; ly < (lh - 1); ly++) {
        for (int ll = 0; ll < fTerrainData.alphamapLayers; ll++) {
          lAlphas [lx, ly, ll] = Lerp (lAlphas [lx, ly, ll], fNewAlphas [lx, ly, ll], fStep);
        }
      }
    }
    fTerrainData.SetAlphamaps (0, 0, lAlphas);
  }
  
  void DoPerlin ()
  {
    Perlin lPerlin = new Perlin ();
    int lw = fTerrainData.heightmapWidth;
    int lh = fTerrainData.heightmapHeight;
    fNewHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fNewHeights [lx, ly] = 0.5f + lPerlin.Noise (lx * scaleWidth / lw, ly * scaleHeight / lh) * scale;
      }
    }
    lw = fTerrainData.alphamapWidth;
    lh = fTerrainData.alphamapHeight;
    fNewAlphas = fTerrainData.GetAlphamaps (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        int lt = Mathf.FloorToInt (fNewHeights [lx, ly] * fTerrainData.alphamapLayers);
        for (int ll = 0; ll < fTerrainData.alphamapLayers; ll++) {
          if (ll == lt) {
            fNewAlphas [lx, ly, ll] = 1.0f;
          } else {
            fNewAlphas [lx, ly, ll] = 0.0f;
          }
        }
      }
    }
  }
}
