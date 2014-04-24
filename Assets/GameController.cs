using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{

  public string gameTypeName = "org.mahn42.coloria";
  public string playerName = "Player";
  public string gameName = "Game";
  public int gamePort = 8042;
  public int maxPlayers = 8;
  public float scale = 0.5f;
  public float scaleWidth = 10.0f;
  public float scaleHeight = 10.0f;
  public int towerCount = 10;
  public int generationMode = 0; // 0 = Perlin, 1 = ImprovedPerlin
  public GameObject towerPreFab = null;
  public static GameController instance = null;
  protected float[,] fNewHeights;
  protected float[,,] fNewAlphas;
  protected Terrain fTerrain = null;
  protected TerrainData fTerrainData = null;

  // Use this for initialization
  void Start ()
  {
    if (instance != null) {
      // raise exception ?
    }
    instance = this;
    towerPreFab = GameObject.Find ("Tower");
    fTerrain = GameObject.Find ("Terrain").GetComponent<Terrain> ();
    fTerrainData = fTerrain.terrainData;

    gameName = PlayerPrefs.GetString ("gameName", gameName);
    playerName = PlayerPrefs.GetString ("playerName", playerName);
    gamePort = PlayerPrefs.GetInt ("gamePort", gamePort);
    maxPlayers = PlayerPrefs.GetInt ("maxPlayers", maxPlayers);
    Network.InitializeServer (maxPlayers, gamePort, !Network.HavePublicAddress ());
    MasterServer.RegisterHost (gameTypeName, gameName, "by " + playerName);

    GenerateLevel ();
  }

  void DoAlphaMapByHeights ()
  {
    int lw = fTerrainData.alphamapWidth;
    int lh = fTerrainData.alphamapHeight;
    fNewAlphas = fTerrainData.GetAlphamaps (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        int lt = Mathf.FloorToInt (fNewHeights [lx, ly] * 1);
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
    DoAlphaMapByHeights ();
  }

  void DoImprovedPerlin ()
  {
    ImprovedPerlin lPerlin = new ImprovedPerlin ();
    int lw = fTerrainData.heightmapWidth;
    int lh = fTerrainData.heightmapHeight;
    fNewHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fNewHeights [lx, ly] = 0.5f + lPerlin.Noise (lx * scaleWidth / lw, ly * scaleHeight / lh, 0.0f) * scale;
      }
    }
    DoAlphaMapByHeights ();
  }
  
  void SetMap ()
  {
    fTerrainData.SetHeights (0, 0, fNewHeights);
    fTerrainData.SetAlphamaps (0, 0, fNewAlphas);
  }

  void GenerateMap ()
  {
    switch (generationMode) {
    case 0: // Perlin
      DoPerlin ();
      break;
    case 1: // Perlin
      DoImprovedPerlin ();
      break;
    }
  }

  protected struct __TowerPos
  {
    public int x, y;
    public float h;
  }

  Vector3 TerrainPosToWorldPos (int aX, int aY)
  {
    float ly = fNewHeights [aX, aY];
    return fTerrain.transform.position
      + new Vector3 (
        (float)aY / (float)fTerrainData.heightmapHeight * fTerrainData.size.z,
        ly * fTerrainData.size.y,
        (float)aX / (float)fTerrainData.heightmapWidth * fTerrainData.size.x
        );
    /*+ new Vector3 (
        (float)aX / (float)fTerrainData.heightmapWidth * fTerrainData.size.x,
        ly * fTerrainData.size.y,
        (float)aY / (float)fTerrainData.heightmapHeight * fTerrainData.size.z
    );*/
  }

  void GenerateTowers ()
  {
    print (fTerrainData.size);
    if (towerPreFab != null && towerCount > 0) {
      int lcount = Mathf.FloorToInt (Mathf.Sqrt (towerCount));
      int lw = fTerrainData.heightmapWidth;
      int lh = fTerrainData.heightmapHeight;
      int lxc = lw / lcount;
      int lyc = lh / lcount;
      int lxm = 0;
      int lym = 0;
      int lxx = 0;
      int lyy = 0;
      __TowerPos[,] lPoss = new __TowerPos[lcount, lcount];
      for (int lx = 0; lx < lw; lx++) {
        lym = 0;
        lyy = 0;
        for (int ly = 0; ly < lh; ly++) {
          if (lPoss [lxx, lyy].h < fNewHeights [lx, ly]) {
            lPoss [lxx, lyy].x = lx;
            lPoss [lxx, lyy].y = ly;
            lPoss [lxx, lyy].h = fNewHeights [lx, ly];
          }
          lym++;
          if (lym >= lyc) {
            lyy++;
            lym = 0;
          }
        }
        lxm++;
        if (lxm >= lxc) {
          lxx++;
          lxm = 0;
        }
      }
      for (int lx = 0; lx < lcount; lx++) {
        for (int ly = 0; ly < lcount; ly++) {
          __TowerPos lPos = lPoss [lx, ly];
          for (int ll = 0; ll < fTerrainData.alphamapLayers; ll++) {
            if (ll == 3) {
              fNewAlphas [lPos.x, lPos.y, ll] = 1.0f;
            } else {
              fNewAlphas [lPos.x, lPos.y, ll] = 0.0f;
            }
          }
          Vector3 lTPos = TerrainPosToWorldPos (lPos.x, lPos.y) + Vector3.down * 0.5f;
          print (lPos.x + " " + lPos.y + " " + lPos.h + " " + lTPos);
          Instantiate (towerPreFab, lTPos, Quaternion.identity);
        }
      }
    }
  }

  void GenerateLevel ()
  {
    GenerateMap ();
    GenerateTowers ();
    SetMap ();
  }

  // Update is called once per frame
  void Update ()
  {
  
  }
}
