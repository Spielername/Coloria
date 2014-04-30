using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
  [System.Serializable]
  public class TerrainConfigTexture
  {
    public Texture2D texture = null;
    public Texture2D normalMap = null;
    public Vector2 tileOffset = Vector2.zero;
    public Vector2 tileSize = new Vector2 (1, 1);
  }

  [System.Serializable]
  public class TerrainConfig
  {
    public int mapSize = 512;
    public int paintSize = 512;
    public int terrainCount = 64; // 8x8
    public float worldSize = 200;
    public float worldHeight = 50;
    public TerrainConfigTexture[] textures = new TerrainConfigTexture[1];
  }
  
  [System.Serializable]
  public class GameSetup
  {
    public string gameMode = "server";
    public string gameTypeName = "org.mahn42.coloria";
    public string playerName = "Player";
    public string gameName = "Game";
    public int gamePort = 8042;
    public int maxPlayers = 8;
  }

  [System.Serializable]
  public class LevelGeneration
  {
    public float scale = 0.5f;
    public float scaleWidth = 10.0f;
    public float scaleHeight = 10.0f;
    public int towerCount = 10;
    public int generationMode = 0; // 0 = Perlin, 1 = ImprovedPerlin, 2 = XY (for testing)
    public GameObject towerPreFab = null;
  }

  public GameSetup gameSetup = new GameSetup ();
  public TerrainConfig terrainConfig = new TerrainConfig ();
  public LevelGeneration levelGeneration = new LevelGeneration ();
  public static GameController instance = null;
  protected Terrain[,] fTerrains;
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
    if (levelGeneration.towerPreFab == null) {
      levelGeneration.towerPreFab = GameObject.Find ("Tower");
    }
    CreateTerrainGameObjects ();
    fTerrain = GameObject.Find ("Terrain").GetComponent<Terrain> ();
    fTerrainData = fTerrain.terrainData;
    //print (fTerrainData.alphamapResolution);

    gameSetup.gameMode = PlayerPrefs.GetString ("gameMode", gameSetup.gameMode);
    gameSetup.gameName = PlayerPrefs.GetString ("gameName", gameSetup.gameName);
    gameSetup.playerName = PlayerPrefs.GetString ("playerName", gameSetup.playerName);
    gameSetup.gamePort = PlayerPrefs.GetInt ("gamePort", gameSetup.gamePort);
    gameSetup.maxPlayers = PlayerPrefs.GetInt ("maxPlayers", gameSetup.maxPlayers);

    if (gameSetup.gameMode.Equals ("server")) {
      print ("run server");
      Network.InitializeServer (gameSetup.maxPlayers - 1, gameSetup.gamePort, !Network.HavePublicAddress ());
      MasterServer.RegisterHost (gameSetup.gameTypeName, gameSetup.gameName, "by " + gameSetup.playerName);
      //GenerateLevel ();
    } else {
      print ("run client");
      GameObject lHostData = GameObject.Find ("NetworkHostData");
      Network.Connect (lHostData.GetComponent<NetworkHostData> ().hostData);
      print ("run client at " + lHostData.GetComponent<NetworkHostData> ().hostData.comment);
    }
  }

  void DestroyTerrainGameObjects ()
  {
    if (fTerrains != null && fTerrains.Length > 0) {
      for (int lx = 0; lx < fTerrains.GetLength(0); lx++) {
        for (int ly = 0; ly < fTerrains.GetLength(1); ly++) {
          Destroy (fTerrains [lx, ly].gameObject);
        }
      }
      fTerrains = null;
    }
  }

  Texture2D LoadTexture2D (string aName)
  {
    Texture2D lTexture2D = Resources.Load<Texture2D> (aName);
    if (lTexture2D == null) {
      print ("Texture2D '" + aName + "' not found!");
    }
    return lTexture2D;
  }

  void CreateTerrainGameObjects ()
  {
    DestroyTerrainGameObjects ();
    int lsize = Mathf.FloorToInt (Mathf.Sqrt (terrainConfig.terrainCount));
    float lhalf = (float)lsize / 2.0f;
    fTerrains = new Terrain[lsize, lsize];
    for (int lx = 0; lx < fTerrains.GetLength(0); lx++) {
      for (int ly = 0; ly < fTerrains.GetLength(1); ly++) {
        TerrainData lTerrainData = new TerrainData ();
        lTerrainData.heightmapResolution = terrainConfig.mapSize / lsize;
        lTerrainData.size = new Vector3 (terrainConfig.worldSize / lsize, terrainConfig.worldHeight, terrainConfig.worldSize / lsize);
        lTerrainData.baseMapResolution = terrainConfig.mapSize / lsize * 2;
        lTerrainData.name = "TerrainData_" + lx + "_" + ly;
        lTerrainData.alphamapResolution = terrainConfig.paintSize / lsize;
        SplatPrototype[] lSplats = new SplatPrototype[terrainConfig.textures.Length];
        for (int lI = 0; lI < lSplats.Length; lI++) {
          lSplats [lI] = new SplatPrototype ();
          lSplats [lI].texture = terrainConfig.textures [lI].texture; // LoadTexture2D("Gray0_Grid");
          lSplats [lI].normalMap = terrainConfig.textures[lI].normalMap;
          lSplats [lI].tileOffset = terrainConfig.textures [lI].tileOffset; // new Vector2(0, 0); 
          lSplats [lI].tileSize = terrainConfig.textures [lI].tileSize; // new Vector2(1, 1);
        }
        lTerrainData.splatPrototypes = lSplats;
        //lTerrainData.SetDetailResolution(1024, lTerrainData.detailResolutionPerPatch);
        //AssetDatabase.CreateAsset(terrainData, "Assets/New Terrain.asset");
        GameObject lTerrain = Terrain.CreateTerrainGameObject (lTerrainData);
        lTerrain.transform.position = transform.position
          + new Vector3 (((float)lx - lhalf) * terrainConfig.worldSize / lsize, terrainConfig.worldHeight / 2.0f, ((float)ly - lhalf) * terrainConfig.worldSize / lsize);
        lTerrain.transform.parent = transform;
        lTerrain.name = "Terrain_" + lx + "_" + ly;
        fTerrains [lx, ly] = lTerrain.GetComponent<Terrain> ();
      }
    }
    for (int lx = 0; lx < fTerrains.GetLength(0); lx++) {
      for (int ly = 0; ly < fTerrains.GetLength(1); ly++) {
        fTerrains [lx, ly].SetNeighbors (
          (lx <= 0) ? null : fTerrains [lx - 1, ly], // left
          (ly >= (fTerrains.GetLength (1) - 1)) ? null : fTerrains [lx, ly + 1],  // top
          (lx >= (fTerrains.GetLength (0) - 1)) ? null : fTerrains [lx + 1, ly], // right
          (ly <= 0) ? null : fTerrains [lx, ly - 1] // bottom
        );
      }
    }
  }

  void OnDrawGizmos ()
  {
    int lsize = Mathf.FloorToInt (Mathf.Sqrt (terrainConfig.terrainCount));
    float lhalf = (float)lsize / 2.0f;
    for (float lx = -lhalf; lx < lhalf; lx++) {
      for (float ly = -lhalf; ly < lhalf; ly++) {
        Gizmos.DrawWireCube (
          transform.position + new Vector3 (lx * terrainConfig.worldSize / lsize, terrainConfig.worldHeight / 2.0f, ly * terrainConfig.worldSize / lsize),
          new Vector3 (terrainConfig.worldSize / lsize, terrainConfig.worldHeight, terrainConfig.worldSize / lsize));
      }
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
        fHeights [ly, lx] = 0.5f + lPerlin.Noise (lx * levelGeneration.scaleWidth / lw, ly * levelGeneration.scaleHeight / lh) * levelGeneration.scale;
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
        fHeights [ly, lx] = 0.5f + lPerlin.Noise (lx * levelGeneration.scaleWidth / lw, ly * levelGeneration.scaleHeight / lh, 0.0f) * levelGeneration.scale;
      }
    }
    DoAlphaMapByHeights ();
  }
  
  void SetMap ()
  {
    fTerrainData.SetHeights (0, 0, fHeights);
    fTerrainData.SetAlphamaps (0, 0, fAlphas);
    fNextHeights = fHeights.Clone () as float[,];
    fNextAlphas = fAlphas.Clone () as float[,,];
    int lw = terrainConfig.mapSize / fTerrains.GetLength(0);
    int lh = terrainConfig.mapSize / fTerrains.GetLength(1);
    float[,] lHeights = new float[lw + 1, lh + 1];
    for (int lx = 0; lx < fTerrains.GetLength(0); lx++) {
      for (int ly = 0; ly < fTerrains.GetLength(1); ly++) {
        for (int lxx = 0; lxx < lHeights.GetLength(1); lxx++) {
          for (int lyy = 0; lyy < lHeights.GetLength(0); lyy++) {
            lHeights[lyy, lxx] = fNextHeights[ly * lh + lyy, lx * lw + lxx];
          }
        }
        fTerrains [lx, ly].terrainData.SetHeights(0,0,lHeights);
      }
    }
  }

  void GenerateMap ()
  {
    switch (levelGeneration.generationMode) {
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
    if (levelGeneration.towerPreFab != null && levelGeneration.towerCount > 0) {
      int lcount = Mathf.FloorToInt (Mathf.Sqrt (levelGeneration.towerCount));
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
          Instantiate (levelGeneration.towerPreFab, lTPos, Quaternion.identity);
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
