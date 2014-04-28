using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
  public string gameMode = "server";
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
  protected float[,] fHeights;
  protected float[,,] fAlphas;
  protected float[,] fNextHeights;
  protected float[,,] fNextAlphas;
  protected Terrain fTerrain = null;
  protected TerrainData fTerrainData = null;

  // Use this for initialization
  void Start ()
  {
    if (instance != null) {
      // raise exception ?
    }
    instance = this;
    if (towerPreFab == null) {
      towerPreFab = GameObject.Find ("Tower");
    }
    fTerrain = GameObject.Find ("Terrain").GetComponent<Terrain> ();
    fTerrainData = fTerrain.terrainData;

    gameMode = PlayerPrefs.GetString ("gameMode", gameMode);
    gameName = PlayerPrefs.GetString ("gameName", gameName);
    playerName = PlayerPrefs.GetString ("playerName", playerName);
    gamePort = PlayerPrefs.GetInt ("gamePort", gamePort);
    maxPlayers = PlayerPrefs.GetInt ("maxPlayers", maxPlayers);

    if (gameMode.Equals ("server")) {
      print ("run server");
      Network.InitializeServer (maxPlayers - 1, gamePort, !Network.HavePublicAddress ());
      MasterServer.RegisterHost (gameTypeName, gameName, "by " + playerName);
      //GenerateLevel ();
    } else {
      GameObject lHostData = GameObject.Find ("NetworkHostData");
      Network.Connect (lHostData.GetComponent<NetworkHostData> ().hostData);
      print ("run client at " + lHostData.GetComponent<NetworkHostData> ().hostData.comment);
    }
  }

  public float[,] GetTerrainHeights (int aX, int aY, int aWidth, int aHeight)
  {
    return fTerrainData.GetHeights (aX, aY, aWidth, aHeight);
  }

  public void SetTerrainHeights (int aX, int aY, float[,] aHeights)
  {
    fTerrainData.SetHeights (aX, aY, aHeights);
  }
  
  public float[,,] GetTerrainAlphas (int aX, int aY, int aWidth, int aHeight)
  {
    /*
    float[,] lResult = new float[aHeight, aWidth];
    for (int lx=0; lx<aWidth; lx++) {
      for (int ly=0; ly<aHeight; ly++) {
        lResult [ly, lx] = fNextAlphas [aY + ly, aX + lx];
      }
    }
    return lResult;
    */
    return fTerrainData.GetAlphamaps (aX, aY, aWidth, aHeight);
  }
  
  public void SetTerrainAlphas (int aX, int aY, float[,,] aAlphas)
  {
    fTerrainData.SetAlphamaps (aX, aY, aAlphas);
  }

  public int GetAlphaMapLayerCount ()
  {
    return fTerrainData.alphamapLayers;
  }

  public Vector2 ConvertToAlphaMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - fTerrain.transform.position;
    lPos.x = lPos.x / fTerrain.terrainData.size.x * fTerrain.terrainData.alphamapWidth;
    lPos.z = lPos.z / fTerrain.terrainData.size.z * fTerrain.terrainData.alphamapHeight;
    return new Vector2 (lPos.x, lPos.z);
  }
  
  public Vector2 ConvertToHeightMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - fTerrain.transform.position;
    lPos.x = lPos.x / fTerrain.terrainData.size.x * fTerrain.terrainData.heightmapWidth;
    lPos.z = lPos.z / fTerrain.terrainData.size.z * fTerrain.terrainData.heightmapHeight;
    return new Vector2 (lPos.x, lPos.z);
  }
  
  void DoAlphaMapByHeights ()
  {
    int lw = fTerrainData.alphamapWidth;
    int lh = fTerrainData.alphamapHeight;
    fAlphas = fTerrainData.GetAlphamaps (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        int lt = Mathf.FloorToInt (fHeights [ly, lx] * 1);
        for (int ll = 0; ll < fTerrainData.alphamapLayers; ll++) {
          if (ll == lt) {
            fAlphas [ly, lx, ll] = 1.0f;
          } else {
            fAlphas [ly, lx, ll] = 0.0f;
          }
        }
      }
    }
  }

  void DoXY ()
  {
    int lw = fTerrainData.heightmapWidth;
    int lh = fTerrainData.heightmapHeight;
    fHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fHeights [ly, lx] = (float)ly / lw;
      }
    }
    DoAlphaMapByHeights ();
  }
  
  void DoPerlin ()
  {
    Perlin lPerlin = new Perlin ();
    int lw = fTerrainData.heightmapWidth;
    int lh = fTerrainData.heightmapHeight;
    fHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fHeights [ly, lx] = 0.5f + lPerlin.Noise (lx * scaleWidth / lw, ly * scaleHeight / lh) * scale;
      }
    }
    DoAlphaMapByHeights ();
  }

  void DoImprovedPerlin ()
  {
    ImprovedPerlin lPerlin = new ImprovedPerlin ();
    int lw = fTerrainData.heightmapWidth;
    int lh = fTerrainData.heightmapHeight;
    fHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fHeights [ly, lx] = 0.5f + lPerlin.Noise (lx * scaleWidth / lw, ly * scaleHeight / lh, 0.0f) * scale;
      }
    }
    DoAlphaMapByHeights ();
  }
  
  void SetMap ()
  {
    fTerrainData.SetHeights (0, 0, fHeights);
    fTerrainData.SetAlphamaps (0, 0, fAlphas);
    fNextHeights = fHeights;
    fNextAlphas = fAlphas;
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
    case 2:
      DoXY ();
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
    float ly = fHeights [aY, aX];
    return fTerrain.transform.position
    /*  + new Vector3 (
        (float)aY / (float)fTerrainData.heightmapHeight * fTerrainData.size.z,
        ly * fTerrainData.size.y,
        (float)aX / (float)fTerrainData.heightmapWidth * fTerrainData.size.x
    );*/
      + new Vector3 (
        (float)aX / (float)fTerrainData.heightmapWidth * fTerrainData.size.x,
        ly * fTerrainData.size.y,
        (float)aY / (float)fTerrainData.heightmapHeight * fTerrainData.size.z
    );
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
      for (int lx = 10; lx < (lw - 10); lx++) {
        lym = 0;
        lyy = 0;
        for (int ly = 10; ly < (lh - 10); ly++) {
          if (lPoss [lxx, lyy].h < fHeights [ly, lx]) {
            lPoss [lxx, lyy].x = lx;
            lPoss [lxx, lyy].y = ly;
            lPoss [lxx, lyy].h = fHeights [ly, lx];
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
          Vector3 lTPos = TerrainPosToWorldPos (lPos.x, lPos.y) + Vector3.down * 0.5f;
          print (lPos.x + " " + lPos.y + " " + lPos.h + " " + lTPos);
          /*for (int ll = 0; ll < fTerrainData.alphamapLayers; ll++) {
            if (ll == 3) {
              fAlphas [lPos.y, lPos.x, ll] = 1.0f;
            } else {
              fAlphas [lPos.y, lPos.x, ll] = 0.0f;
            }
          }*/
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

  // Server Stuff

  void OnServerInitialized ()
  {
    print ("OnServerInitialized");
    GenerateLevel ();
  }
  
  void OnPlayerConnected (NetworkPlayer player)
  {
    print ("Player " + player.ToString () + " connected from " + player.ipAddress + ":" + player.port);
    networkView.RPC ("SetLevel", player, fHeights);
  }

  // Client Stuff

  [RPC]
  void SetLevel (float[,] aHeightmap)
  {
    print ("height map received " + aHeightmap.Length);
  }

  void OnConnectedToServer ()
  {
    print ("network connected.");
  }

  void OnFailedToConnect (NetworkConnectionError aError)
  {
    print ("network failed! " + aError.ToString ());
  }

  // Update is called once per frame
  void Update ()
  {
  
  }
}
