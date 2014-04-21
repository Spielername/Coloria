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

  protected float[,] fNewHeights;
  protected float[,,] fNewAlphas;
  protected Terrain fTerrain = null;
  protected TerrainData fTerrainData = null;

  // Use this for initialization
  void Start ()
  {
    fTerrain = GameObject.Find("Terrain").GetComponent<Terrain> ();
    fTerrainData = fTerrain.terrainData;

    gameName = PlayerPrefs.GetString ("gameName", gameName);
    playerName = PlayerPrefs.GetString ("playerName", playerName);
    gamePort = PlayerPrefs.GetInt ("gamePort", gamePort);
    maxPlayers = PlayerPrefs.GetInt ("maxPlayers", maxPlayers);
    Network.InitializeServer (maxPlayers, gamePort, !Network.HavePublicAddress ());
    MasterServer.RegisterHost (gameTypeName, gameName, "by " + playerName);

    GenerateLevel ();
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
        int lt = Mathf.FloorToInt (fNewHeights [lx, ly] * 2);
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

  void SetMap() {
    fTerrainData.SetHeights(0,0,fNewHeights);
    fTerrainData.SetAlphamaps(0,0,fNewAlphas);
  }

  void GenerateLevel ()
  {
    DoPerlin();
    SetMap();
  }
  
  // Update is called once per frame
  void Update ()
  {
  
  }
}
