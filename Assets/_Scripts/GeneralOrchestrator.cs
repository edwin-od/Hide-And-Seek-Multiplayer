using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GeneralOrchestrator : MonoBehaviour {

    private const string RECONNECT_GUI_TEXT = "Reconnecting";
    private const int MIN_USERNAME_CHARS = 3;
    private const int MAX_USERNAME_CHARS = 18;
    private const float CATCH_DISTANCE_THRESHOLD = 1f;

    private string reconnectDotsSTR = "";

    private List<GameObject> playerModels;

    private NetworkManager networkManager;

    public Color hiderColor;
    public Color seekerColor;

    public GameObject spawn;
    public GameObject player;
    public GameObject zone;
    public GameObject uiCam;

    public GameObject canvasUsername;
    public GameObject canvasStartJoinLobby;
    public GameObject canvasLobby;
    public GameObject canvasCountdown;
    public GameObject startGameButton;
    public GameObject usernameError;
    public GameObject joinStartLobbyError;
    public GameObject joinStartLobbyNetError;
    public GameObject startGameError;
    public GameObject startGameMorePlayersError;
    public InputField usernameField;
    public InputField lobbyIDField;
    public Text lobbyIDTXT;
    public Text hostLobbyTXT;
    public Text player1LobbyTXT;
    public Text player2LobbyTXT;
    public Text player3LobbyTXT;
    public Text player4LobbyTXT;
    public Text player5LobbyTXT;
    public Text seekerCountdownTXT;
    public Text playerCountdownTXT;
    public GameObject starGameBackButton;
    public GameObject playerZoneCountdownTXT;
    public GameObject playerMatchCountdownTXT;
    public GameObject playerSeekerCountdownTXT;
    public Text lobbyCountdownTXT;
    public GameObject lobbyMatchCountdownTXT;
    public GameObject lobbyScoreboardCountdownTXT;

    private int state;

    void Awake() {
        DontDestroyOnLoad(transform.gameObject);
    }

    void Start() {
        SceneManager.LoadScene("UI");

        networkManager = new NetworkManager();
        
        canvasUsername.SetActive(true);
        canvasStartJoinLobby.SetActive(false);
        canvasLobby.SetActive(false);
        canvasCountdown.SetActive(false);
        startGameButton.SetActive(false);
        lobbyIDTXT.gameObject.SetActive(false);
        hostLobbyTXT.text = "";
        player1LobbyTXT.text = "";
        player2LobbyTXT.text = "";
        player3LobbyTXT.text = "";
        player4LobbyTXT.text = "";
        player5LobbyTXT.text = "";
        usernameError.SetActive(false);
        joinStartLobbyNetError.SetActive(false);
        joinStartLobbyError.SetActive(false);
        startGameError.SetActive(false);
        startGameMorePlayersError.SetActive(false);
        playerZoneCountdownTXT.SetActive(false);
        playerMatchCountdownTXT.SetActive(false);
        playerSeekerCountdownTXT.SetActive(true);
        lobbyCountdownTXT.gameObject.SetActive(false);
        lobbyMatchCountdownTXT.SetActive(false);
        lobbyScoreboardCountdownTXT.SetActive(false);
        starGameBackButton.SetActive(true);

        spawn.SetActive(false);
        player.SetActive(false);
        zone.SetActive(false);
        uiCam.SetActive(true);

        playerModels = new List<GameObject>();

        state = 0;

        Physics.IgnoreCollision(player.GetComponent<Collider>(), zone.GetComponent<Collider>(), true);

        StartCoroutine(updateNetworkAnimator());
        StartCoroutine(updateNetwork());
        StartCoroutine(updateLoby());
    }

    void Update() {
        if (canvasLobby && canvasLobby.activeSelf) {
            if (!lobbyCountdownTXT.gameObject.activeSelf)
                updateScrollView();
            else
                updateScrollViewScoreboard();
        }

        if (networkManager.resetTerminateFlag()) 
            exitToJoinStartLobby();

        if(networkManager.resetBecomeHostFlag() && networkManager.isNetworkReady() && canvasLobby && canvasLobby.activeSelf) {
            canvasUsername.SetActive(false);
            canvasStartJoinLobby.SetActive(false);
            canvasLobby.SetActive(true);
            canvasCountdown.SetActive(false);
            startGameButton.SetActive(true);
            lobbyIDTXT.gameObject.SetActive(true);
            lobbyIDTXT.text = "Lobby ID: " + networkManager.IPv4().Split('.')[3];
            starGameBackButton.SetActive(true);
            lobbyCountdownTXT.gameObject.SetActive(false);
            lobbyMatchCountdownTXT.SetActive(false);
            lobbyScoreboardCountdownTXT.SetActive(false);
            startGameMorePlayersError.SetActive(false);
        }            

        networkManager.regularUpdate();

        if (networkManager.isGameReady() && networkManager.GetMatchOrchestrator() != null) {
            int curState = networkManager.GetMatchOrchestrator().getState();
            if (networkManager.GetMatchOrchestrator() != null) {
                switch (curState) {
                    case 1:
                        if (state != curState) {
                            state = curState;

                            zone.GetComponent<Renderer>().enabled = false;

                            networkManager.GetMatchOrchestrator().resetZoneScale();
                            networkManager.GetMatchOrchestrator().generateZoneCenter();
                            networkManager.GetMatchOrchestrator().zone.GetComponent<Transform>().position = networkManager.GetMatchOrchestrator().getZoneCenter();

                            if (!networkManager.isHider()) {
                                canvasUsername.SetActive(false);
                                canvasStartJoinLobby.SetActive(false);
                                canvasLobby.SetActive(false);
                                canvasCountdown.SetActive(true);
                            } else {
                                enterMatch();
                                instantiatePlayerModels(true);
                                playerZoneCountdownTXT.SetActive(false);
                                playerMatchCountdownTXT.SetActive(false);
                                playerSeekerCountdownTXT.SetActive(true);
                            }
                        }
                        if (!networkManager.isHider()) {
                            int m = (int)(networkManager.GetMatchOrchestrator().getHidingRemainingTime() / 60f);
                            seekerCountdownTXT.text = m + "m " + (networkManager.GetMatchOrchestrator().getHidingRemainingTime() - m * 60) + "s";
                        } else {
                            int m = (int)(networkManager.GetMatchOrchestrator().getHidingRemainingTime() / 60f);
                            playerCountdownTXT.text = m + "m " + (networkManager.GetMatchOrchestrator().getHidingRemainingTime() - m * 60) + "s";
                        }
                        checkRemovedPlayers();
                        break;
                    case 2:
                        if (state != curState) {
                            state = curState;
                            if (!networkManager.isHider()) {
                                enterMatch();
                                instantiatePlayerModels();
                            } else {
                                instantiatePlayerModels(false);
                            }
                            
                            playerZoneCountdownTXT.SetActive(true);
                            playerMatchCountdownTXT.SetActive(false);
                            playerSeekerCountdownTXT.SetActive(false);
                        }
                        checkRemovedPlayers();
                        int min = (int)(networkManager.GetMatchOrchestrator().getSeekingRemainingTime() / 60f);
                        playerCountdownTXT.text = min + "m " + (networkManager.GetMatchOrchestrator().getSeekingRemainingTime() - min * 60) + "s";
                        break;
                    case 3:
                        if (state != curState) {
                            state = curState;

                            zone.GetComponent<Renderer>().enabled = networkManager.isHider();

                            playerZoneCountdownTXT.SetActive(false);
                            playerMatchCountdownTXT.SetActive(true);
                            playerSeekerCountdownTXT.SetActive(false);
                        }                       


                        int zoneShrinkQueue = networkManager.GetMatchOrchestrator().resetZoneShrinkQueue();
                        for (int i = 0; i < zoneShrinkQueue; i++) {
                            networkManager.GetMatchOrchestrator().shrinkZone();
                        }

                        checkRemovedPlayers();

                        if (!networkManager.isCaught()) {
                            int m = (int)(networkManager.GetMatchOrchestrator().getMatchRemainingTime() / 60f);
                            playerCountdownTXT.text = m + "m " + (networkManager.GetMatchOrchestrator().getMatchRemainingTime() - m *60) + "s";
                        } else {
                            if (!canvasLobby || !canvasLobby.activeSelf)
                                exitMidMatchToScoreboard();
                            int m = (int)(networkManager.GetMatchOrchestrator().getMatchRemainingTime() / 60f);
                            lobbyCountdownTXT.text = m + "m " + (networkManager.GetMatchOrchestrator().getMatchRemainingTime() - m * 60) + "s";
                        }
                        break;
                    case 4:
                        if (state != curState) {
                            state = curState;

                            networkManager.checkSurvivedHiders();
                            destroyPlayerModels();
                            networkManager.resetCatches();

                            networkManager.switchOwnRole();
                            networkManager.GetMatchOrchestrator().switchRoles();
                            networkManager.GetMatchOrchestrator().resetZoneScale();
                            networkManager.GetMatchOrchestrator().generateZoneCenter();
                            networkManager.GetMatchOrchestrator().zone.GetComponent<Transform>().position = networkManager.GetMatchOrchestrator().getZoneCenter();

                            if (!canvasLobby || !canvasLobby.activeSelf)
                                exitToScoreboard(state);
                        }
                        int mn = (int)(networkManager.GetMatchOrchestrator().getScoreboardRemainingTime() / 60f);
                        lobbyCountdownTXT.text = mn + "m " + (networkManager.GetMatchOrchestrator().getScoreboardRemainingTime() - mn * 60) + "s";
                        break;
                    case 5:
                        goto case 1;
                    case 6:
                        goto case 2;
                    case 7:
                        goto case 3;
                    case 8:
                        goto case 4;
                }
            }
        } else if (networkManager.resetEndGameFlag()) {
            if (networkManager.isNetworkReady())
                exitToLobby();
            else
                exitToJoinStartLobby();
        }
    }

    private IEnumerator updateNetworkAnimator() {

        if (networkManager.isConnecting()) {

            switch (reconnectDotsSTR.Length) {
                case 3:
                    reconnectDotsSTR = "";
                    break;
                case 2:
                    reconnectDotsSTR = "...";
                    break;
                case 1:
                    reconnectDotsSTR = "..";
                    break;
                case 0:
                    reconnectDotsSTR = ".";
                    break;
                default:
                    break;
            }
        }

        networkManager.updateNetworkAnimator();

        yield return new WaitForSeconds(NetworkManager.NETWORK_ANIMATOR_UPDATE_PERIOD_SECONDS);
        StartCoroutine(updateNetworkAnimator());
    }

    private IEnumerator updateNetwork() {        

        networkManager.updateNetwork();
        checkOutsideZone();          
        detectCatch();

        yield return new WaitForSeconds(NetworkManager.NETWORK_UPDATE_PERIOD_SECONDS);
        StartCoroutine(updateNetwork());
    }

    private IEnumerator updateLoby() {

        networkManager.sendPlayersDetailsAndNewHostIP();

        yield return new WaitForSeconds(NetworkManager.LOBBY_UPDATE_PERIOD_SECONDS);
        StartCoroutine(updateLoby());
    }    

    private void enterMatch() {
        SceneManager.LoadScene("Nature");
        canvasUsername.SetActive(false);
        canvasStartJoinLobby.SetActive(false);
        canvasLobby.SetActive(false);
        canvasCountdown.SetActive(false);
        uiCam.SetActive(false);
        spawn.SetActive(true);
        player.SetActive(true);
        zone.SetActive(true);
        foreach (Transform g in player.GetComponent<Transform>()) {
            if(g.tag == "PlayerModel") {
                foreach (Transform m in g) {
                    if(m.name == "Body") {
                        if(networkManager.isHider())
                            m.GetComponent<Renderer>().material.color = hiderColor;
                        else
                            m.GetComponent<Renderer>().material.color = seekerColor;
                        break;
                    }
                }
                break;
            }
        }
        System.Random rand = new System.Random();
        player.GetComponent<Transform>().position = spawn.GetComponent<Transform>().position + new Vector3(rand.Next(-50, 51), 0f, rand.Next(-50, 51));
    }
    private void exitToLobby() {
        SceneManager.LoadScene("UI");
        canvasUsername.SetActive(false);
        canvasStartJoinLobby.SetActive(false);
        canvasLobby.SetActive(true);
        canvasCountdown.SetActive(false);
        startGameError.SetActive(false);
        startGameMorePlayersError.SetActive(false);
        startGameButton.SetActive(networkManager.isHost());
        lobbyIDTXT.gameObject.SetActive(networkManager.isHost());
        starGameBackButton.SetActive(true);
        lobbyCountdownTXT.gameObject.SetActive(false);
        lobbyMatchCountdownTXT.SetActive(false);
        lobbyScoreboardCountdownTXT.SetActive(false);
        spawn.SetActive(false);
        player.SetActive(false);
        zone.SetActive(false);
        uiCam.SetActive(true);
        networkManager.resetAll();
    }
    private void exitToScoreboard(int state) {
        SceneManager.LoadScene("UI");
        canvasUsername.SetActive(false);
        canvasStartJoinLobby.SetActive(false);
        canvasLobby.SetActive(true);
        canvasCountdown.SetActive(false);
        starGameBackButton.SetActive(false);
        startGameError.SetActive(false);
        startGameMorePlayersError.SetActive(false);
        startGameButton.SetActive(false);
        lobbyIDTXT.gameObject.SetActive(false);
        lobbyCountdownTXT.gameObject.SetActive(true);
        if (state != 8) {
            lobbyMatchCountdownTXT.SetActive(false);
            lobbyScoreboardCountdownTXT.SetActive(true);
        } else {
            lobbyMatchCountdownTXT.SetActive(false);
            lobbyScoreboardCountdownTXT.SetActive(false);
        }
        spawn.SetActive(false);
        player.SetActive(false);
        zone.SetActive(false);
        uiCam.SetActive(true);
    }
    private void exitMidMatchToScoreboard() {
        SceneManager.LoadScene("UI");
        canvasUsername.SetActive(false);
        canvasStartJoinLobby.SetActive(false);
        canvasLobby.SetActive(true);
        canvasCountdown.SetActive(false);
        startGameError.SetActive(false);
        starGameBackButton.SetActive(false);
        startGameMorePlayersError.SetActive(false);
        startGameButton.SetActive(false);
        lobbyIDTXT.gameObject.SetActive(false);
        lobbyCountdownTXT.gameObject.SetActive(true);
        lobbyMatchCountdownTXT.SetActive(true);
        lobbyScoreboardCountdownTXT.SetActive(false);
        spawn.SetActive(false);
        player.SetActive(false);
        zone.SetActive(false);
        uiCam.SetActive(true);
    }
    private void exitToJoinStartLobby() {
        SceneManager.LoadScene("UI");
        canvasUsername.SetActive(false);
        canvasStartJoinLobby.SetActive(true);
        canvasLobby.SetActive(false);
        canvasCountdown.SetActive(false);
        spawn.SetActive(false);
        player.SetActive(false);
        zone.SetActive(false);
        uiCam.SetActive(true);
        networkManager.resetAll();
    }


    private void checkOutsideZone() {
        if (!networkManager.isHider())
            return;
        Vector3 zoneCenter = player.GetComponent<Transform>().position - new Vector3(zone.GetComponent<Transform>().position.x,
            player.GetComponent<Transform>().position.y,
            zone.GetComponent<Transform>().position.z);
        Ray rayTowardsZone = new Ray(player.GetComponent<Transform>().position, -zoneCenter);
        Ray rayAwayFromZone = new Ray(player.GetComponent<Transform>().position, zoneCenter);

        RaycastHit hitT, hitA;
        Physics.Raycast(rayTowardsZone, out hitT, 1000, LayerMask.GetMask("Zone"));
        Physics.Raycast(rayAwayFromZone, out hitA, 1000, LayerMask.GetMask("Zone"));

        if (hitA.collider != null && hitA.collider.tag.Equals("Zone") && hitT.collider != null && hitT.collider.tag.Equals("Zone")) {
            if (RenderSettings.fog == true)
                RenderSettings.fog = false;
            return;
        }
        if (RenderSettings.fog == false) {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.05f;
        }
    }

    private void detectCatch() {
        if (networkManager.isHider())
            return;    

        foreach (GameObject p in playerModels) {
            float d = Vector3.Distance(player.GetComponent<Transform>().position, p.GetComponent<Transform>().position);
            if(d < CATCH_DISTANCE_THRESHOLD) {
                networkManager.registerCatch(p.name);
            }
        }
    }

    public void updateScrollView() {
        hostLobbyTXT.text = "";
        player1LobbyTXT.text = "";
        player2LobbyTXT.text = "";
        player3LobbyTXT.text = "";
        player4LobbyTXT.text = "";
        player5LobbyTXT.text = "";

        string[] playerNames = networkManager.getPlayerNames();
        if (playerNames == null)
            return;
        for (int i = 0; i < playerNames.Length; i++) {
            switch (i) {
                case 0:
                    hostLobbyTXT.text = playerNames[i];
                    break;
                case 1:
                    player1LobbyTXT.text = playerNames[i];
                    break;
                case 2:
                    player2LobbyTXT.text = playerNames[i];
                    break;
                case 3:
                    player3LobbyTXT.text = playerNames[i];
                    break;
                case 4:
                    player4LobbyTXT.text = playerNames[i];
                    break;
                case 5:
                    player5LobbyTXT.text = playerNames[i];
                    break;
            }
        }
    }
    public void updateScrollViewScoreboard() {
        hostLobbyTXT.text = "";
        player1LobbyTXT.text = "";
        player2LobbyTXT.text = "";
        player3LobbyTXT.text = "";
        player4LobbyTXT.text = "";
        player5LobbyTXT.text = "";

        string[] playerStats = networkManager.getPlayerStats();
        if (playerStats == null)
            return;
        for (int i = 0; i < playerStats.Length; i++) {
            switch (i) {
                case 0:
                    hostLobbyTXT.text = playerStats[i];
                    break;
                case 1:
                    player1LobbyTXT.text = playerStats[i];
                    break;
                case 2:
                    player2LobbyTXT.text = playerStats[i];
                    break;
                case 3:
                    player3LobbyTXT.text = playerStats[i];
                    break;
                case 4:
                    player4LobbyTXT.text = playerStats[i];
                    break;
                case 5:
                    player5LobbyTXT.text = playerStats[i];
                    break;
            }
        }
    }

    public void RegisterUsername() {
        usernameError.SetActive(false);
        string u = usernameField.text;
        if (u == null || u.Equals("")) {
            usernameError.SetActive(true);
            return;
        }
        if (u.All(c => char.IsLetterOrDigit(c) || c.Equals('_'))) {
            if (char.IsDigit(u[0]) || u[0].Equals('_')) {
                usernameError.SetActive(true);
                return;
            }
            if (u.Length < MIN_USERNAME_CHARS || u.Length > MAX_USERNAME_CHARS) {
                usernameError.SetActive(true);
                return;
            }
            networkManager.setName(u);
            canvasUsername.SetActive(false);
            canvasStartJoinLobby.SetActive(true);
            canvasLobby.SetActive(false);
            canvasCountdown.SetActive(false);
        } else {
            usernameError.SetActive(true);
            return;
        }
    }

    public void JoinLobby() {
        joinStartLobbyNetError.SetActive(false);
        joinStartLobbyError.SetActive(false);
        string id = lobbyIDField.text;
        if (id == null || id.Equals("") || !id.All(c => char.IsDigit(c))) {
            joinStartLobbyError.SetActive(true);
            return;
        }
        string ip = networkManager.IPv4();

        networkManager.Guest(ip.Split('.')[0] + "." + ip.Split('.')[1] + "." + ip.Split('.')[2] + "." + id);

        int timeout = 10;
        while (!networkManager.isNetworkReady() && timeout-- > 0) {
            Thread.Sleep(1000);
        }

        if (networkManager.isNetworkReady()) {
            canvasUsername.SetActive(false);
            canvasStartJoinLobby.SetActive(false);
            canvasLobby.SetActive(true);
            canvasCountdown.SetActive(false);
            startGameButton.SetActive(false);
            lobbyIDTXT.gameObject.SetActive(false);
            starGameBackButton.SetActive(true);
            lobbyCountdownTXT.gameObject.SetActive(false);
            lobbyMatchCountdownTXT.SetActive(false);
            lobbyScoreboardCountdownTXT.SetActive(false);
            startGameMorePlayersError.SetActive(false);
        } else {
            joinStartLobbyNetError.SetActive(true);
        }
    }

    public void StartLobby() {
        joinStartLobbyNetError.SetActive(false);
        joinStartLobbyError.SetActive(false);

        networkManager.Host();

        int timeout = 10;
        while (!networkManager.isNetworkReady() && timeout-- > 0) {
            Thread.Sleep(1000);
        }

        if (networkManager.isNetworkReady()) {
            canvasUsername.SetActive(false);
            canvasStartJoinLobby.SetActive(false);
            canvasLobby.SetActive(true);
            canvasCountdown.SetActive(false);
            startGameButton.SetActive(true);
            lobbyIDTXT.gameObject.SetActive(true);
            lobbyIDTXT.text = "Lobby ID: " + networkManager.IPv4().Split('.')[3];
            starGameBackButton.SetActive(true);
            lobbyCountdownTXT.gameObject.SetActive(false);
            lobbyMatchCountdownTXT.SetActive(false);
            lobbyScoreboardCountdownTXT.SetActive(false);
            startGameMorePlayersError.SetActive(false);
        } else {
            joinStartLobbyNetError.SetActive(true);
        }
    }

    public void StartGame() {
        startGameError.SetActive(false);
        startGameMorePlayersError.SetActive(false);

        string[] players = networkManager.getPlayerNames();
        if (players == null || players.Length < 2) {
            startGameMorePlayersError.SetActive(true);
            return;
        }

        networkManager.StartMatch();

        int timeout = 5;
        while (!networkManager.isGameReady() && timeout-- > 0) {
            Thread.Sleep(1000);
        }

        if (!networkManager.isGameReady()) {
            startGameError.SetActive(true);
        }
    }

    public void BackToUsername() {
        canvasUsername.SetActive(true);
        canvasStartJoinLobby.SetActive(false);
        canvasLobby.SetActive(false);
        canvasCountdown.SetActive(false);
        usernameError.SetActive(false);
        joinStartLobbyNetError.SetActive(false);
        joinStartLobbyError.SetActive(false);
        startGameError.SetActive(false);
        startGameMorePlayersError.SetActive(false);
        usernameField.text = "";
        lobbyIDField.text = "";
    }

    public void BackToJoinStartLobby() {
        canvasUsername.SetActive(false);
        canvasStartJoinLobby.SetActive(true);
        canvasLobby.SetActive(false);
        canvasCountdown.SetActive(false);
        usernameError.SetActive(false);
        joinStartLobbyNetError.SetActive(false);
        joinStartLobbyError.SetActive(false);
        startGameError.SetActive(false);
        usernameField.text = "";
        lobbyIDField.text = "";

        networkManager.closeAll();
    }

    public void instantiatePlayerModels(bool isHiders) {

        Transform spawn = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Respawn")).GetComponent<Transform>();
        GameObject hiderModel = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Hider"));
        GameObject seekerModel = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Seeker"));
        if (hiderModel == null || seekerModel == null || spawn == null)
            return;

        List<PlayerNetworkController> pncs = networkManager.getPlayerNetworkControllers();
        if (pncs == null || pncs.Count == 0)
            return;

        foreach (PlayerNetworkController pnc in pncs) {
            if (pnc.getIsHider() == isHiders) {
                GameObject pm = Instantiate(isHiders ? hiderModel : seekerModel, spawn.position, spawn.rotation) as GameObject;
                pm.name = pnc.getGuestName();
                pnc.setPlayerModel(pm);
                playerModels.Add(pm);
            }
        }
    }

    public void instantiatePlayerModels() {

        Transform spawn = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Respawn")).GetComponent<Transform>();
        GameObject hiderModel = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Hider"));
        GameObject seekerModel = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Seeker"));
        if (hiderModel == null || seekerModel == null || spawn == null)
            return;

        List<PlayerNetworkController> pncs = networkManager.getPlayerNetworkControllers();
        if (pncs == null || pncs.Count == 0)
            return;

        foreach (PlayerNetworkController pnc in pncs) {
            if (pnc.getIsHider()) {
                GameObject pm = Instantiate(hiderModel, spawn.position, spawn.rotation) as GameObject;
                pm.name = pnc.getGuestName();
                pnc.setPlayerModel(pm);
                playerModels.Add(pm);
            } else {
                GameObject pm = Instantiate(seekerModel, spawn.position, spawn.rotation) as GameObject;
                pm.name = pnc.getGuestName();
                pnc.setPlayerModel(pm);
                playerModels.Add(pm);
            }
        }
    }

    public void destroyPlayerModels() {
        if (playerModels == null) {
            playerModels = new List<GameObject>();
            return;
        } else if (playerModels.Count == 0)
            return;

        foreach (GameObject playerModel in playerModels) {
            Destroy(playerModel);
        }
        playerModels = new List<GameObject>();
    }

    public void checkRemovedPlayers() {
        if (playerModels == null) {
            playerModels = new List<GameObject>();
            return;
        } else if (playerModels.Count == 0)
            return;

        List<string> removedPlayersQueue = networkManager.resetRemovedPlayersQueue();
        if (removedPlayersQueue == null || removedPlayersQueue.Count == 0)
            return;

        foreach (string name in removedPlayersQueue) {
            foreach (GameObject playerModel in playerModels) {
                if (name.Equals(playerModel.name)) {
                    playerModels.Remove(playerModel);
                    Destroy(playerModel);
                    break;
                }
            }
        }
    }

    void OnGUI() {
        if (networkManager.isConnecting()) {
            GUI.Label(new Rect(5, 5, 120, 50), RECONNECT_GUI_TEXT + reconnectDotsSTR);
        }
    }
}
