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
  public class TerrainConfigPlayerTexture
  {
    public const int textureCount = 3;
    public TerrainConfigTexture baseTexture = new TerrainConfigTexture ();
    public TerrainConfigTexture splashTexture = new TerrainConfigTexture ();
    public TerrainConfigTexture borderTexture = new TerrainConfigTexture ();
  }
  
  [System.Serializable]
  public class TerrainConfig
  {
    public string tag = "Terrain";
    public int mapSize = 512;
    public int paintSize = 512;
    public int terrainCount = 64; // 8x8
    public float worldSize = 200;
    public float worldHeight = 50;
    public TerrainConfigTexture[] textures = new TerrainConfigTexture[1];
    public TerrainConfigPlayerTexture[] playerTextures = new TerrainConfigPlayerTexture[1];
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
  protected float[,] fLevelHeights;
  protected float[,,] fLevelAlphas;
  protected int fTerrainPartDimCount = 8;
  protected int fTerrainPartMapSize = 64;
  protected int fTerrainPartPaintSize = 64;
  protected int fTerrainPartTextureCount = 0;

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
    CreateMap ();
    CreateTerrainGameObjects ();

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

  void CreateMap ()
  {
    fTerrainPartDimCount = Mathf.FloorToInt (Mathf.Sqrt (terrainConfig.terrainCount));
    fTerrainPartMapSize = terrainConfig.mapSize / fTerrainPartDimCount;
    fTerrainPartPaintSize = terrainConfig.paintSize / fTerrainPartDimCount;
    fTerrainPartTextureCount = terrainConfig.textures.Length + terrainConfig.playerTextures.Length * TerrainConfigPlayerTexture.textureCount;
    fHeights = new float[terrainConfig.mapSize + 1, terrainConfig.mapSize + 1];
    fAlphas = new float[terrainConfig.paintSize, terrainConfig.paintSize, fTerrainPartTextureCount];
  }

  void CreateTerrainGameObjects ()
  {
    DestroyTerrainGameObjects ();
    float lhalf = (float)fTerrainPartDimCount / 2.0f;
    fTerrains = new Terrain[fTerrainPartDimCount, fTerrainPartDimCount];
    for (int lx = 0; lx < fTerrains.GetLength(0); lx++) {
      for (int ly = 0; ly < fTerrains.GetLength(1); ly++) {
        TerrainData lTerrainData = new TerrainData ();
        lTerrainData.heightmapResolution = fTerrainPartMapSize;
        lTerrainData.size = new Vector3 (terrainConfig.worldSize / fTerrainPartDimCount, terrainConfig.worldHeight, terrainConfig.worldSize / fTerrainPartDimCount);
        lTerrainData.baseMapResolution = terrainConfig.mapSize / fTerrainPartDimCount * 2;
        lTerrainData.name = "TerrainData_" + lx + "_" + ly;
        lTerrainData.alphamapResolution = fTerrainPartPaintSize;
        SplatPrototype[] lSplats = new SplatPrototype[fTerrainPartTextureCount];
        int lII = -1;
        for (int lI = 0; lI < terrainConfig.textures.Length; lI++) {
          lSplats [++lII] = new SplatPrototype ();
          lSplats [lII].texture = terrainConfig.textures [lI].texture; // LoadTexture2D("Gray0_Grid");
          lSplats [lII].normalMap = terrainConfig.textures [lI].normalMap;
          lSplats [lII].tileOffset = terrainConfig.textures [lI].tileOffset; // new Vector2(0, 0); 
          lSplats [lII].tileSize = terrainConfig.textures [lI].tileSize; // new Vector2(1, 1);
        }
        for (int lI = 0; lI < terrainConfig.playerTextures.Length; lI++) {
          lSplats [++lII] = new SplatPrototype ();
          lSplats [lII].texture = terrainConfig.playerTextures [lI].baseTexture.texture;
          lSplats [lII].normalMap = terrainConfig.playerTextures [lI].baseTexture.normalMap;
          lSplats [lII].tileOffset = terrainConfig.playerTextures [lI].baseTexture.tileOffset;
          lSplats [lII].tileSize = terrainConfig.playerTextures [lI].baseTexture.tileSize;
          lSplats [++lII] = new SplatPrototype ();
          lSplats [lII].texture = terrainConfig.playerTextures [lI].splashTexture.texture;
          if (lSplats [lII].texture == null) {
            lSplats [lII].texture = terrainConfig.playerTextures [lI].baseTexture.texture;
            lSplats [lII].normalMap = terrainConfig.playerTextures [lI].baseTexture.normalMap;
            lSplats [lII].tileOffset = terrainConfig.playerTextures [lI].baseTexture.tileOffset;
            lSplats [lII].tileSize = terrainConfig.playerTextures [lI].baseTexture.tileSize;
          } else {
            lSplats [lII].normalMap = terrainConfig.playerTextures [lI].splashTexture.normalMap;
            lSplats [lII].tileOffset = terrainConfig.playerTextures [lI].splashTexture.tileOffset;
            lSplats [lII].tileSize = terrainConfig.playerTextures [lI].splashTexture.tileSize;
          }
          lSplats [++lII] = new SplatPrototype ();
          lSplats [lII].texture = terrainConfig.playerTextures [lI].borderTexture.texture;
          if (lSplats [lII].texture == null) {
            lSplats [lII].texture = lSplats [lII - 1].texture; //terrainConfig.playerTextures [lI].baseTexture.texture;
            lSplats [lII].normalMap = lSplats [lII - 1].normalMap; //terrainConfig.playerTextures [lI].baseTexture.normalMap;
            lSplats [lII].tileOffset = lSplats [lII - 1].tileOffset; // terrainConfig.playerTextures [lI].baseTexture.tileOffset;
            lSplats [lII].tileSize = lSplats [lII - 1].tileSize; //terrainConfig.playerTextures [lI].baseTexture.tileSize;
          } else {
            lSplats [lII].normalMap = terrainConfig.playerTextures [lI].borderTexture.normalMap;
            lSplats [lII].tileOffset = terrainConfig.playerTextures [lI].borderTexture.tileOffset;
            lSplats [lII].tileSize = terrainConfig.playerTextures [lI].borderTexture.tileSize;
          }
        }
        lTerrainData.splatPrototypes = lSplats;
        //lTerrainData.SetDetailResolution(1024, lTerrainData.detailResolutionPerPatch);
        //AssetDatabase.CreateAsset(terrainData, "Assets/New Terrain.asset");
        GameObject lTerrain = Terrain.CreateTerrainGameObject (lTerrainData);
        lTerrain.tag = terrainConfig.tag;
        lTerrain.transform.position = transform.position
          + new Vector3 (((float)lx - lhalf) * terrainConfig.worldSize / fTerrainPartDimCount, 0.0f, ((float)ly - lhalf) * terrainConfig.worldSize / fTerrainPartDimCount);
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
    float lws = terrainConfig.worldSize / lsize;
    float lhalf = (float)lsize / 2.0f;
    for (float lx = -lhalf; lx < lhalf; lx++) {
      for (float ly = -lhalf; ly < lhalf; ly++) {
        Gizmos.DrawWireCube (
          transform.position + new Vector3 (lx * lws + lws / 2.0f, terrainConfig.worldHeight / 2.0f, ly * lws + lws / 2.0f),
          new Vector3 (lws, terrainConfig.worldHeight, lws));
      }
    }
  }

  public float SampleHeight (Vector3 aPos)
  {
    Vector3 lPos = aPos - (transform.position - new Vector3 (terrainConfig.worldSize / 2.0f, 0, terrainConfig.worldSize / 2.0f));
    int ltx = Mathf.Clamp ((int)(lPos.x / (terrainConfig.worldSize / fTerrainPartDimCount)), 0, fTerrainPartDimCount - 1);
    int lty = Mathf.Clamp ((int)(lPos.z / (terrainConfig.worldSize / fTerrainPartDimCount)), 0, fTerrainPartDimCount - 1);
    return fTerrains [ltx, lty].SampleHeight (aPos);
  }

  public float[,] GetTerrainHeights (int aX, int aY, int aWidth, int aHeight)
  {
    float [,] lHeights = new float[aWidth + 1, aHeight + 1];
    for (int lx = 0; lx< lHeights.GetLength(1); lx++) {
      for (int ly = 0; ly< lHeights.GetLength(0); ly++) {
        lHeights [ly, lx] = fHeights [aY + ly, aX + lx];
      }
    }
    return lHeights;
    //return fTerrainData.GetHeights (aX, aY, aWidth, aHeight);
  }

  public void SetTerrainHeights (int aX, int aY, float[,] aHeights)
  {
    int ltsx = Mathf.Clamp (aX / fTerrainPartMapSize, 0, fTerrainPartDimCount - 1);
    int ltsy = Mathf.Clamp (aY / fTerrainPartMapSize, 0, fTerrainPartDimCount - 1);
    int ltex = Mathf.Clamp ((aX + aHeights.GetLength (1) - 1) / fTerrainPartMapSize, ltsx, fTerrainPartDimCount - 1);
    int ltey = Mathf.Clamp ((aY + aHeights.GetLength (0) - 1) / fTerrainPartMapSize, ltsy, fTerrainPartDimCount - 1);
    for (int ltx = ltsx; ltx <= ltex; ltx++) {
      for (int lty = ltsy; lty <= ltey; lty++) {
        int lpsx = aX - ltx * fTerrainPartMapSize;
        int lpex = (aX + aHeights.GetLength (1)) - ltx * fTerrainPartMapSize;
        if (lpex > fTerrainPartMapSize) {
          lpex = fTerrainPartMapSize;
        }
        if (lpsx < 0) {
          lpsx = 0;
        }
        int lpsy = aY - lty * fTerrainPartMapSize;
        int lpey = (aY + aHeights.GetLength (0)) - lty * fTerrainPartMapSize;
        if (lpey > fTerrainPartMapSize) {
          lpey = fTerrainPartMapSize;
        }
        if (lpsy < 0) {
          lpsy = 0;
        }
        //print ("aX=" + aX + " aY=" + aY + " aW=" + aHeights.GetLength (1) + " aH=" + aHeights.GetLength (0) + " ltx=" + ltx + " lty=" + lty + " lpsx=" + lpsx + " lpex=" + lpex + " lpsy=" + lpsy + " lpey=" + lpey);
        int lzx = 0;
        int lzy = 0;
        if (ltx < ltex)
          lzx = 1;
        if (lty < ltey)
          lzy = 1;
        float[,] lHeights = new float[lpey - lpsy + lzy, lpex - lpsx + lzx];
        for (int lx = lpsx; lx < lpex + lzx; lx++) {
          for (int ly = lpsy; ly < lpey + lzy; ly++) {
            int lxx = lx + ltx * fTerrainPartMapSize - aX;
            int lyy = ly + lty * fTerrainPartMapSize - aY;
            float lV = 0.0f;
            try {
              lV = aHeights [lyy, lxx];
            } catch (System.Exception lE) {
              print ("aX=" + aX + " aY=" + aY + " aW=" + aHeights.GetLength (1) + " aH=" + aHeights.GetLength (0) + " ltx=" + ltx + " lty=" + lty + " lpsx=" + lpsx + " lpex=" + lpex + " lpsy=" + lpsy + " lpey=" + lpey);
              print ("[" + lyy + "," + lxx + "]" + lE.Message);
              return;
            }
            lHeights [ly - lpsy, lx - lpsx] = lV;
          }
        }
        fTerrains [ltx, lty].terrainData.SetHeights (lpsx, lpsy, lHeights);
      }
    }
    for (int lx = 0; lx< aHeights.GetLength(1); lx++) {
      for (int ly = 0; ly< aHeights.GetLength(0); ly++) {
        fHeights [aY + ly, aX + lx] = aHeights [ly, lx];
      }
    }
  }
  
  public float[,,] GetTerrainAlphas (int aX, int aY, int aWidth, int aHeight)
  {
    float[,,] lAlphas = new float[aWidth, aHeight, fTerrainPartTextureCount];
    for (int lx = 0; lx< lAlphas.GetLength(1); lx++) {
      for (int ly = 0; ly< lAlphas.GetLength(0); ly++) {
        for (int ll = 0; ll < fTerrainPartTextureCount; ll++) {
          lAlphas [ly, lx, ll] = fAlphas [aY + ly, aX + lx, ll];
        }
      }
    }
    return lAlphas;
  }
  
  public void SetTerrainAlphas (int aX, int aY, float[,,] aAlphas)
  {
    int ltsx = aX / fTerrainPartPaintSize;
    int ltsy = aY / fTerrainPartPaintSize;
    int ltex = (aX + aAlphas.GetLength (1) - 1) / fTerrainPartPaintSize;
    int ltey = (aY + aAlphas.GetLength (0) - 1) / fTerrainPartPaintSize;
    for (int ltx = ltsx; ltx <= ltex; ltx++) {
      for (int lty = ltsy; lty <= ltey; lty++) {
        int lpsx = aX - ltx * fTerrainPartPaintSize;
        int lpex = (aX + aAlphas.GetLength (1)) - ltx * fTerrainPartPaintSize;
        if (lpex > fTerrainPartPaintSize) {
          lpex = fTerrainPartPaintSize;
        }
        if (lpsx < 0) {
          lpsx = 0;
        }
        int lpsy = aY - lty * fTerrainPartPaintSize;
        int lpey = (aY + aAlphas.GetLength (0)) - lty * fTerrainPartPaintSize;
        if (lpey > fTerrainPartPaintSize) {
          lpey = fTerrainPartPaintSize;
        }
        if (lpsy < 0) {
          lpsy = 0;
        }
        //print ("aX=" + aX + " aY=" + aY + " aW=" + aAlphas.GetLength (1) + " aH=" + aAlphas.GetLength (0) + " ltx=" + ltx + " lty=" + lty + " lpsx=" + lpsx + " lpex=" + lpex + " lpsy=" + lpsy + " lpey=" + lpey);
        int lzx = 0;
        int lzy = 0;
        //if (ltx < ltex) lzx = 1;
        //if (lty < ltey) lzy = 1;
        float[,,] lAlphas = new float[lpey - lpsy + lzy, lpex - lpsx + lzx, fTerrainPartTextureCount];
        for (int lx = lpsx; lx < lpex + lzx; lx++) {
          for (int ly = lpsy; ly < lpey + lzy; ly++) {
            int lxx = lx + ltx * fTerrainPartPaintSize - aX;
            int lyy = ly + lty * fTerrainPartPaintSize - aY;
            for (int ll = 0; ll < fTerrainPartTextureCount; ll++) {
              float lV = 0.0f;
              try {
                lV = aAlphas [lyy, lxx, ll];
              } catch (System.Exception lE) {
                print ("aX=" + aX + " aY=" + aY + " aW=" + aAlphas.GetLength (1) + " aH=" + aAlphas.GetLength (0) + " ltx=" + ltx + " lty=" + lty + " lpsx=" + lpsx + " lpex=" + lpex + " lpsy=" + lpsy + " lpey=" + lpey);
                print ("[" + lyy + "," + lxx + "," + ll + "]" + lE.Message);
                return;
              }
              lAlphas [ly - lpsy, lx - lpsx, ll] = lV;
            }
          }
        }
        fTerrains [ltx, lty].terrainData.SetAlphamaps (lpsx, lpsy, lAlphas);
      }
    }
    for (int lx = 0; lx< aAlphas.GetLength(1); lx++) {
      for (int ly = 0; ly< aAlphas.GetLength(0); ly++) {
        for (int ll = 0; ll < fTerrainPartTextureCount; ll++) {
          fAlphas [aY + ly, aX + lx, ll] = aAlphas [ly, lx, ll];
        }
      }
    }
  }

  public int GetAlphaMapLayerCount ()
  {
    return fTerrainPartTextureCount;
  }

  public Vector2 ConvertToAlphaMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - (transform.position - new Vector3 (terrainConfig.worldSize / 2.0f, 0, terrainConfig.worldSize / 2.0f));
    lPos.x = lPos.x / terrainConfig.worldSize * terrainConfig.paintSize;  //fTerrain.terrainData.size.x * fTerrain.terrainData.alphamapWidth;
    lPos.z = lPos.z / terrainConfig.worldSize * terrainConfig.paintSize;  //fTerrain.terrainData.size.z * fTerrain.terrainData.alphamapHeight;
    return new Vector2 (lPos.x, lPos.z);
  }
  
  public Vector2 ConvertToHeightMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - (transform.position - new Vector3 (terrainConfig.worldSize / 2.0f, 0, terrainConfig.worldSize / 2.0f));
    lPos.x = lPos.x / terrainConfig.worldSize * terrainConfig.mapSize;  //fTerrain.terrainData.size.x * fTerrain.terrainData.heightmapWidth;
    lPos.z = lPos.z / terrainConfig.worldSize * terrainConfig.mapSize;  //fTerrain.terrainData.size.z * fTerrain.terrainData.heightmapHeight;
    return new Vector2 (lPos.x, lPos.z);
  }
  
  void DoAlphaMapByHeights ()
  {
    int lw = terrainConfig.paintSize; // fTerrainData.alphamapWidth;
    int lh = terrainConfig.paintSize; // fTerrainData.alphamapHeight;
    //fAlphas = fTerrainData.GetAlphamaps (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        int lt = Mathf.FloorToInt (fHeights [ly, lx] * terrainConfig.textures.Length);
        for (int ll = 0; ll < fTerrainPartTextureCount; ll++) {
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
    int lw = terrainConfig.mapSize; // fTerrainData.heightmapWidth;
    int lh = terrainConfig.mapSize; // fTerrainData.heightmapHeight;
    //fHeights = fTerrainData.GetHeights (0, 0, lw, lh);
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
    int lw = terrainConfig.mapSize; //fTerrainData.heightmapWidth;
    int lh = terrainConfig.mapSize; //fTerrainData.heightmapHeight;
    //fHeights = fTerrainData.GetHeights (0, 0, lw, lh);
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
    int lw = terrainConfig.mapSize; //fTerrainData.heightmapWidth;
    int lh = terrainConfig.mapSize; //fTerrainData.heightmapHeight;
    //fHeights = fTerrainData.GetHeights (0, 0, lw, lh);
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fHeights [ly, lx] = 0.5f + lPerlin.Noise (lx * levelGeneration.scaleWidth / lw, ly * levelGeneration.scaleHeight / lh, 0.0f) * levelGeneration.scale;
      }
    }
    DoAlphaMapByHeights ();
  }
  
  void SetMap ()
  {
    //fTerrainData.SetHeights (0, 0, fHeights);
    //fTerrainData.SetAlphamaps (0, 0, fAlphas);
    fLevelHeights = fHeights.Clone () as float[,];
    fLevelAlphas = fAlphas.Clone () as float[,,];
    SetTerrainHeights (0, 0, fHeights);
    SetTerrainAlphas (0, 0, fAlphas);
    /*
    int lw = terrainConfig.mapSize / fTerrains.GetLength (0);
    int lh = terrainConfig.mapSize / fTerrains.GetLength (1);
    float[,] lHeights = new float[lw + 1, lh + 1];
    for (int lx = 0; lx < fTerrains.GetLength(0); lx++) {
      for (int ly = 0; ly < fTerrains.GetLength(1); ly++) {
        for (int lxx = 0; lxx < lHeights.GetLength(1); lxx++) {
          for (int lyy = 0; lyy < lHeights.GetLength(0); lyy++) {
            lHeights [lyy, lxx] = fHeights [ly * lh + lyy, lx * lw + lxx];
          }
        }
        fTerrains [lx, ly].terrainData.SetHeights (0, 0, lHeights);
      }
    }
    */
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
    return transform.position - new Vector3 (terrainConfig.worldSize / 2.0f, 0, terrainConfig.worldSize / 2.0f)
      + new Vector3 (
        (float)aX / (float)terrainConfig.mapSize * terrainConfig.worldSize, // fTerrainData.heightmapWidth * fTerrainData.size.x,
        ly * terrainConfig.worldHeight, // fTerrainData.size.y,
        (float)aY / (float)terrainConfig.mapSize * terrainConfig.worldSize // fTerrainData.heightmapHeight * fTerrainData.size.z
    );
  }

  void GenerateTowers ()
  {
    if (levelGeneration.towerPreFab != null && levelGeneration.towerCount > 0) {
      int lcount = Mathf.FloorToInt (Mathf.Sqrt (levelGeneration.towerCount));
      int lw = terrainConfig.mapSize; //fTerrainData.heightmapWidth;
      int lh = terrainConfig.mapSize; //fTerrainData.heightmapHeight;
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
          print ("Tower at " + lPos.x + " " + lPos.y + " " + lPos.h + " " + lTPos);
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
