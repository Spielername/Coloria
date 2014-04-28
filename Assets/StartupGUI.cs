using UnityEngine;
using System.Collections;

public class StartupGUI : MonoBehaviour
{

  public int borderLeft;
  public int borderTop;
  public int borderBottom;
  public int borderRight;
  public int innerWidth;
  public int innerHeight;
  public string playerName = "Player";
  public string gameName = "Game";
  public string gameTypeName = "org.mahn42.coloria";
  public int gamePort = 8042;
  public int maxPlayers = 8;
  protected string fGamePortStr = "8042";
  protected string fmaxPlayersStr = "8";
  protected Vector2 fGameListScrollPos = Vector2.zero;
  protected bool fIsRefreshingHostList = false;
  protected HostData[] fHostList = null;

  void Start ()
  {
    borderLeft = Screen.width / 100;
    borderRight = Screen.width / 100;
    borderTop = Screen.height / 100;
    borderBottom = Screen.height / 100;
    innerWidth = Screen.width - borderLeft - borderRight;
    innerHeight = Screen.height - borderTop - borderBottom;
  }

  void OnGUI ()
  {
    bool lCanStartServer = true;
    GUILayout.BeginArea (new Rect (borderLeft, borderTop, innerWidth, innerHeight));
    GUILayout.Label ("Coloria");
    GUILayout.BeginHorizontal (GUILayout.Width (innerWidth / 2), GUILayout.MinWidth (200));
    GUILayout.Label ("Playername: ");
    playerName = GUILayout.TextField (playerName);
    GUILayout.EndHorizontal ();
    GUILayout.BeginHorizontal (GUILayout.Width (innerWidth / 2), GUILayout.MinWidth (200));
    GUILayout.Label ("Gamename: ");
    gameName = GUILayout.TextField (gameName);
    GUILayout.Label ("Port: ");
    lCanStartServer = lCanStartServer && int.TryParse (fGamePortStr, out gamePort);
    fGamePortStr = GUILayout.TextField (fGamePortStr);
    lCanStartServer = lCanStartServer && int.TryParse (fmaxPlayersStr, out maxPlayers);
    GUILayout.Label ("max. Players: ");
    fmaxPlayersStr = GUILayout.TextField (fmaxPlayersStr);
    if (!lCanStartServer) {
      GUILayout.Button ("Start Game");
    } else {
      if (GUILayout.Button ("Start Game")) {
        StartServer();
      }
    }
    GUILayout.EndHorizontal ();
    fGameListScrollPos = GUILayout.BeginScrollView (fGameListScrollPos, GUILayout.Width (innerWidth / 2), GUILayout.MinWidth (200));
    if (fHostList != null && fHostList.Length > 0) {
      for (int i = 0; i<fHostList.Length; i++) {
        HostData lHost = fHostList [i];
        if (GUILayout.Button ("Join "
                              + lHost.gameName
                              + " [" + lHost.connectedPlayers + "/" + lHost.playerLimit + "] "
                              + (lHost.comment == null ? "" : (" (" + lHost.comment + ")")))) {
          JoinServer(lHost);
        }
      }
    }
    GUILayout.EndScrollView ();
    GUILayout.EndArea ();
  }

  private void RefreshHostList ()
  {
    if (!fIsRefreshingHostList) {
      fIsRefreshingHostList = true;
      MasterServer.RequestHostList (gameTypeName);
    }
  }

  void Update ()
  {
    if (fIsRefreshingHostList) {
      HostData[] lHostList = MasterServer.PollHostList ();
      if (lHostList.Length > 0) {
        fIsRefreshingHostList = false;
        fHostList = lHostList;
      }
    } else {
      RefreshHostList ();
    }
  }

  void StartServer() {
    PlayerPrefs.SetString("gameMode", "server");
    PlayerPrefs.SetInt("gamePort", gamePort);
    PlayerPrefs.SetInt("maxPlayers", maxPlayers);
    PlayerPrefs.SetString("gameName", gameName);
    PlayerPrefs.SetString("playerName", playerName);
    PlayerPrefs.Save();
    Application.LoadLevel ("Game");
  }

  void JoinServer(HostData aHost) {
    PlayerPrefs.SetString("gameMode", "client");
    PlayerPrefs.SetInt("gamePort", gamePort);
    PlayerPrefs.SetInt("maxPlayers", maxPlayers);
    PlayerPrefs.SetString("gameName", gameName);
    PlayerPrefs.SetString("playerName", playerName);
    PlayerPrefs.Save();
    GameObject lHostData = GameObject.Find("NetworkHostData");
    lHostData.GetComponent<NetworkHostData>().hostData = aHost;
    DontDestroyOnLoad(lHostData);
    Application.LoadLevel ("Game");
  }
  
}
