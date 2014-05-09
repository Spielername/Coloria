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

  [System.Serializable]
  public class TerrainModifierPencil
  {
    public string name = "<name>";
    public Texture2D pencil;
  }
  
  [System.Serializable]
  public class TerrainModifierSetup
  {
    public TerrainModifierPencil[] pencils = new TerrainModifierPencil[1];
  }

  [System.Serializable]
  public class GameProperties
  {
    public float minTextureValueForScore = 0.5f;
  }
  
  public GameSetup gameSetup = new GameSetup ();
  public TerrainConfig terrainConfig = new TerrainConfig ();
  public LevelGeneration levelGeneration = new LevelGeneration ();
  public TerrainModifierSetup terrainModifierSetup = new TerrainModifierSetup ();
  public GameProperties gameProperties = new GameProperties();
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
  protected int[] fTerrainScores;
  protected Hashtable fPencils = new Hashtable ();

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
    for (int lI = 0; lI < terrainModifierSetup.pencils.Length; lI++) {
      fPencils.Add (terrainModifierSetup.pencils [lI].name, terrainModifierSetup.pencils [lI]);
    }
    //fPencils["<name>"];
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
    fTerrainScores = new int[fTerrainPartTextureCount];
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
            lSplats [lII].texture = lSplats [lII - 1].texture;
            lSplats [lII].normalMap = lSplats [lII - 1].normalMap;
            lSplats [lII].tileOffset = lSplats [lII - 1].tileOffset;
            lSplats [lII].tileSize = lSplats [lII - 1].tileSize;
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

  public TerrainModifierPencil GetPencilByName (string aName)
  {
    return (TerrainModifierPencil)fPencils [aName];
  }
  
  //********************************************
  //
  //                 HEIGHT
  //
  //********************************************
  
  public float SampleHeight (Vector3 aPos)
  {
    Vector3 lPos = aPos - (transform.position - new Vector3 (terrainConfig.worldSize / 2.0f, 0, terrainConfig.worldSize / 2.0f));
    int ltx = Mathf.Clamp ((int)(lPos.x / (terrainConfig.worldSize / fTerrainPartDimCount)), 0, fTerrainPartDimCount - 1);
    int lty = Mathf.Clamp ((int)(lPos.z / (terrainConfig.worldSize / fTerrainPartDimCount)), 0, fTerrainPartDimCount - 1);
    return fTerrains [ltx, lty].SampleHeight (aPos);
  }

  public float GetTerrainHeigth (int aX, int aY)
  {
    return fHeights [aY, aX];
  }

  public float GetTerrainLevelHeigth (int aX, int aY)
  {
    return fLevelHeights [aY, aX];
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

  public void SplashOnMap (Texture2D aTexture, Vector3 aPoint, Vector3 aScale, float aSize, float aFactor, bool aCheckLevel = true, float aMaxLevelDiff = 0.01f)
  {
    bool lModified = false;
    Vector2 lPos1 = ConvertToHeightMapPoint (aPoint - aScale * aSize);
    Vector2 lPos2 = ConvertToHeightMapPoint (aPoint + aScale * aSize);
    int lX = Mathf.FloorToInt (lPos1.x);
    int lY = Mathf.FloorToInt (lPos1.y);
    int lW = Mathf.FloorToInt (lPos2.x) - lX;
    int lH = Mathf.FloorToInt (lPos2.y) - lY;
    float[,] lHeights = GetTerrainHeights (lX, lY, lW, lH);
    for (int lxx = 0; lxx < (lW - 1); lxx++) {
      for (int lyy = 0; lyy < (lH - 1); lyy++) {
        Color lC = aTexture.GetPixelBilinear ((float)lxx / (lW - 1.0f), (float)lyy / (lH - 1.0f));
        float lS = (1.0f - lC.a) * aFactor;
        if (lS != 0.0f) {
          lS += lHeights [lyy, lxx];
          if (aCheckLevel) {
            float lLS = GameController.instance.GetTerrainLevelHeigth (lX + lxx, lY + lyy);
            if (lS < lLS) {
              lS = lLS;
            } else if (lS > (lLS + aMaxLevelDiff)) {
              lS = lLS + aMaxLevelDiff;
            }
          }
          if (lS > 1.0f) {
            lS = 1.0f;
          }
          lModified = lModified || lHeights [lyy, lxx] != lS;
          lHeights [lyy, lxx] = lS;
        }
      }
    }
    if (lModified) {
      SetTerrainHeights (lX, lY, lHeights);
    }
  }

  public void SplashOnMap (string aPencilName, Vector3 aPoint, Vector3 aScale, float aSize, float aFactor, bool aCheckLevel = true, float aMaxLevelDiff = 0.01f)
  {
    Texture2D lTex = GetPencilByName (aPencilName).pencil;
    SplashOnMap (lTex, aPoint, aScale, aSize, aFactor, aCheckLevel, aMaxLevelDiff);
  }
  
  //********************************************
  //
  //                 ALPHA
  //
  //********************************************

  public float[] GetTerrainAlpha (int aX, int aY)
  {
    float[] lAlphas = new float[fTerrainPartTextureCount];
    for (int ll = 0; ll < fTerrainPartTextureCount; ll++) {
      lAlphas [ll] = fAlphas [aY, aX, ll];
    }
    return lAlphas;
  }
  
  public float[] GetTerrainLevelAlpha (int aX, int aY)
  {
    float[] lAlphas = new float[fTerrainPartTextureCount];
    for (int ll = 0; ll < fTerrainPartTextureCount; ll++) {
      lAlphas [ll] = fLevelAlphas [aY, aX, ll];
    }
    return lAlphas;
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
          float lOld = fAlphas [aY + ly, aX + lx, ll];
          float lNew = aAlphas [ly, lx, ll];
          if (lOld >= gameProperties.minTextureValueForScore && lNew < gameProperties.minTextureValueForScore) {
            fTerrainScores[ll]--;
          } else if (lOld < gameProperties.minTextureValueForScore && lNew >= gameProperties.minTextureValueForScore) {
            fTerrainScores[ll]++;
          }
          fAlphas [aY + ly, aX + lx, ll] = lNew;
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
    lPos.x = lPos.x / terrainConfig.worldSize * terrainConfig.paintSize;
    lPos.z = lPos.z / terrainConfig.worldSize * terrainConfig.paintSize;
    return new Vector2 (lPos.x, lPos.z);
  }
  
  public Vector2 ConvertToHeightMapPoint (Vector3 aPos)
  {
    Vector3 lPos = aPos - (transform.position - new Vector3 (terrainConfig.worldSize / 2.0f, 0, terrainConfig.worldSize / 2.0f));
    lPos.x = lPos.x / terrainConfig.worldSize * terrainConfig.mapSize;
    lPos.z = lPos.z / terrainConfig.worldSize * terrainConfig.mapSize;
    return new Vector2 (lPos.x, lPos.z);
  }

  void SetTextureOnMap (float[,,] aAlphas, int aX, int aY, float aValue, int aTextureNumber)
  {
    float lV = aAlphas [aY, aX, aTextureNumber];
    float lO = 1.0f - lV;
    float lNO = 1.0f - aValue;
    float lNOfak = 1.0f;
    if (lO > 0.0f) {
      lNOfak = lNO / lO;
    }
    if (aValue == 0.0f && lV >= 1.0f) {
      for (int lA = 0; lA < GetAlphaMapLayerCount(); lA++) {
        if (lA == aTextureNumber) {
          aAlphas [aY, aX, lA] = aValue; // 0.0f
        } else if (lA == 0) {
          aAlphas [aY, aX, lA] = 1.0f;
        } else {
          aAlphas [aY, aX, lA] = 0.0f;
        }
      }
    } else {
      for (int lA = 0; lA < GetAlphaMapLayerCount(); lA++) {
        if (lA == aTextureNumber) {
          aAlphas [aY, aX, lA] = aValue;
        } else {
          aAlphas [aY, aX, lA] *= lNOfak;
        }
      }
    }
  }
  
  public void PaintOnMap (Texture2D aTexture, int aTextureNumber, Vector3 aPoint, Vector3 aScale, float aSize)
  {
    bool lModified = false;
    Vector2 lPos1 = ConvertToAlphaMapPoint (aPoint - aScale * aSize);
    Vector2 lPos2 = ConvertToAlphaMapPoint (aPoint + aScale * aSize);
    int lX = Mathf.FloorToInt (lPos1.x);
    int lY = Mathf.FloorToInt (lPos1.y);
    int lW = Mathf.FloorToInt (lPos2.x) - lX;
    int lH = Mathf.FloorToInt (lPos2.y) - lY;
    float[,,] lAlphas = GetTerrainAlphas (lX, lY, lW, lH);
    for (int lxx = 0; lxx < (lW - 1); lxx++) {
      for (int lyy = 0; lyy < (lH - 1); lyy++) {
        Color lC = aTexture.GetPixelBilinear ((float)lxx / (lW - 1.0f), (float)lyy / (lH - 1.0f));
        float lS = 1.0f - lC.a;
        if (lS > 0.0f) {
          lS += lAlphas [lyy, lxx, aTextureNumber];
          if (lS > 1.0f) {
            lS = 1.0f;
          }
          lModified = lModified || lAlphas [lyy, lxx, aTextureNumber] != lS;
          SetTextureOnMap (lAlphas, lxx, lyy, lS, aTextureNumber);
        }
      }
    }
    if (lModified) {
      SetTerrainAlphas (lX, lY, lAlphas);
    }
  }

  public int GetPlayerSplashTextureNumber (int aPlayer)
  {
    if (aPlayer > 0) {
      return terrainConfig.textures.Length + aPlayer * (TerrainConfigPlayerTexture.textureCount - 1);
    } else {
      return 0;
    }
  }

  // aPlayer = 0 -> base terrain
  //           1 -> first player
  public void PaintOnMap (string aPencilName, int aPlayer, Vector3 aPoint, Vector3 aScale, float aSize)
  {
    Texture2D lTex = GetPencilByName (aPencilName).pencil;
    int lTexNum = GetPlayerSplashTextureNumber (aPlayer);
    PaintOnMap (lTex, lTexNum, aPoint, aScale, aSize);
  }

  void DoAlphaMapByHeights ()
  {
    int lw = terrainConfig.paintSize;
    int lh = terrainConfig.paintSize;
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        float lheight = fHeights [ly, lx] * terrainConfig.textures.Length;
        int lt = Mathf.FloorToInt (lheight);
        //float lc = Mathf.Ceil (lheight);
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

  //********************************************
  //
  //                 GENERATORS
  //
  //********************************************
  
  void DoXY ()
  {
    int lw = terrainConfig.mapSize;
    int lh = terrainConfig.mapSize;
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
    int lw = terrainConfig.mapSize;
    int lh = terrainConfig.mapSize;
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
    int lw = terrainConfig.mapSize;
    int lh = terrainConfig.mapSize;
    for (int lx = 0; lx < lw; lx++) {
      for (int ly = 0; ly < lh; ly++) {
        fHeights [ly, lx] = 0.5f + lPerlin.Noise (lx * levelGeneration.scaleWidth / lw, ly * levelGeneration.scaleHeight / lh, 0.0f) * levelGeneration.scale;
      }
    }
    DoAlphaMapByHeights ();
  }
  
  void SetMap ()
  {
    fLevelHeights = fHeights.Clone () as float[,];
    fLevelAlphas = fAlphas.Clone () as float[,,];
    SetTerrainHeights (0, 0, fHeights);
    SetTerrainAlphas (0, 0, fAlphas);
  }

  void GenerateMap ()
  {
    switch (levelGeneration.generationMode) {
    case 0: // Perlin
      DoPerlin ();
      break;
    case 1: // ImprovedPerlin
      DoImprovedPerlin ();
      break;
    case 2: // Schräge 
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
        (float)aX / (float)terrainConfig.mapSize * terrainConfig.worldSize,
        ly * terrainConfig.worldHeight,
        (float)aY / (float)terrainConfig.mapSize * terrainConfig.worldSize
    );
  }

  void GenerateTowers ()
  {
    if (levelGeneration.towerPreFab != null && levelGeneration.towerCount > 0) {
      int lcount = Mathf.FloorToInt (Mathf.Sqrt (levelGeneration.towerCount));
      int lw = terrainConfig.mapSize;
      int lh = terrainConfig.mapSize;
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
          //print ("Tower at " + lPos.x + " " + lPos.y + " " + lPos.h + " " + lTPos);
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
    fTerrainScores = CalculateTerrainAreas(0.5f);
  }

  //********************************************
  //
  //                 SCORE
  //
  //********************************************

  public float[] CalculateTerrainAreas ()
  {
    float[] lSums = new float[fAlphas.GetLength(2)];
    for(int ll = 0; ll < fAlphas.GetLength(2); ll++) {
      lSums[ll] = 0.0f;
      for(int lx = 0; lx < fAlphas.GetLength(1); lx++) {
        for(int ly = 0; ly < fAlphas.GetLength(0); ly++) {
          lSums[ll] += fAlphas[ly,lx,ll];
        }
      }
    }
    return lSums;
  }
  
  public int[] CalculateTerrainAreas (float lMin)
  {
    int[] lSums = new int[fAlphas.GetLength(2)];
    for(int ll = 0; ll < fAlphas.GetLength(2); ll++) {
      lSums[ll] = 0;
      for(int lx = 0; lx < fAlphas.GetLength(1); lx++) {
        for(int ly = 0; ly < fAlphas.GetLength(0); ly++) {
          if (fAlphas[ly,lx,ll] >= lMin) {
            lSums[ll]++;
          }
        }
      }
    }
    return lSums;
  }
  
  //********************************************
  //
  //                 SERVER STUFF
  //
  //********************************************
  
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

  //********************************************
  //
  //                 CLIENT STUFF
  //
  //********************************************
  
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

  void OnGUI ()
  {
    /*
    GUILayout.Label ("Scores (float):");
    float[] lFS = CalculateTerrainAreas();
    for(int lI = 0; lI < lFS.Length; lI++) {
      GUILayout.Label (lI + " = " + lFS[lI]);
    }
    GUILayout.Label ("Scores (int):");
    int[] lIS = CalculateTerrainAreas(0.5f);
    for(int lI = 0; lI < lIS.Length; lI++) {
      GUILayout.Label (lI + " = " + lIS[lI]);
    }
    */
    GUILayout.Label ("Scores (int):");
    int lSum = 0;
    for(int lI = 0; lI < fTerrainScores.Length; lI++) {
      GUILayout.Label (lI + " = " + fTerrainScores[lI]);
      lSum += fTerrainScores[lI];
    }
    GUILayout.Label ("Sum = " + lSum);
  }
}
