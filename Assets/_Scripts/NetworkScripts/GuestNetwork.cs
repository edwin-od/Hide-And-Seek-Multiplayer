using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class GuestNetwork {

    private const int PORT = 6321;
    private const int MAX_RECONNECT_ATTEMPTS = 20;
    private const int HOST_UNREACHABLE_TIMEOUT = 10;     // In seconds

    private string guestName;
    private string newHost = "";

    private float hostTimeoutCounter = 0f;

    private bool becomeHost = false;
    private bool socketReady = false;
    private bool gameStarted = false;
    private bool isHider = false;
    private bool isCaught = false;
    private bool endGameFlag = false;
    private bool connecting = false;

    private int score = 0;

    private Socket socket;
    private NetworkStream stream;
    private StreamWriter writer;
    private StreamReader reader;

    private MatchOrchestrator match;
    private PlayerController player;
    private List<PlayerNetworkController> players;
    private List<string> removedPlayersQueue;

    public GuestNetwork(string username) {
        guestName = username;        
        players = new List<PlayerNetworkController>();
        player = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Player")).GetComponent<PlayerController>();
        removedPlayersQueue = new List<string>();
    }

    public void regularUpdate() {
        //Everything past here requires socket connection to be established
        if (!socketReady)
            return;
        //Check host timeout counter
        if (hostTimeoutCounter > HOST_UNREACHABLE_TIMEOUT) {
            hostTimeoutCounter = 0f;
            connecting = false;

            Debug.Log("Host has been terminated! Assigning new host..");

            //Attempt to assign new host if player is assigned new host by old one
            if (newHost == IPv4()) {
                //Assign self as new host
                CloseSocket();
                becomeHost = true;
                return;
            } else {
                //Attempt to connect to new host
                CloseSocket();
                ReconnectToNewHost(newHost);
                return;
            }

        } else if (hostTimeoutCounter >= 3) {
            connecting = true;
        }

        hostTimeoutCounter += Time.deltaTime;

        //Check if host sent a message
        if (stream.DataAvailable) {

            //Host is active
            hostTimeoutCounter = 0f;
            connecting = false;

            //Message received from host
            string data = reader.ReadLine();
            if (data != null) {
                if (data == "Host terminated" || data.Split('/')[0] == "Host terminated") {
                    string[] gInf = data.Split('/');
                    //If host has been terminated socket is closed
                    CloseSocket();
                    Debug.Log("Host has been terminated! Assigning new host..");

                    if (data == "Host terminated") {
                        //Exit Match
                        Debug.Log("Not enough players to assign new host! Terminating Match..");
                        return;
                    }

                    //Attempt to assign new host if player is assigned new host by old one
                    if (gInf[1] == IPv4()) {
                        //Assign self as new host
                        CloseSocket();
                        becomeHost = true;
                        return;
                    } else {
                        //Attempt to connect to new host
                        CloseSocket();
                        ReconnectToNewHost(gInf[1]);
                        return;
                    }
                } else if (data == "Lobby full") {
                    //Lobby is full terminate connection
                    CloseSocket();
                    Debug.Log("Lobby is full!");
                    return;
                } else if (data == "Game started") {
                    //Game already started terminate connection
                    CloseSocket();
                    Debug.Log("Game already started!");
                    return;
                } else {
                    //Process data sent from host
                    OnIncomingData(data);
                }
            }
        }

    }

    public void updateNetworkAnimator() {

        if (socketReady && gameStarted && match != null && !isCaught) {
            Broadcast("animator/" +
                guestName + "/" +
                player.getVerticalAxis() + "/" +
                player.getHorizontalAxis() + "/" +
                player.getFallDistance() + "/" +
                player.IsCrouching() + "/" +
                player.IsJumping() + "/" +
                player.GetComponent<Transform>().position.x + "/" +
                player.GetComponent<Transform>().position.y + "/" +
                player.GetComponent<Transform>().position.z + "/" +
                player.GetComponent<Transform>().eulerAngles.x + "/" +
                player.GetComponent<Transform>().eulerAngles.y + "/" +
                player.GetComponent<Transform>().eulerAngles.z + "/" +
                "power" + "/" +
                isHider);
        }
        
    }

    public void resetCaught() {
        isCaught = false;
        foreach(PlayerNetworkController p in players) {
            p.setIsCaught(false);
        }
    }
    public bool getIsCaught() {
        return isCaught;
    }

    //Get IPv4
    private string IPv4() {

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    //What to do when message is received from host
    private void OnIncomingData(string data) {

        string[] dataInfo = data.Split('/');

        if (dataInfo[0] == "animator") {
            NetworkAnimator(dataInfo[1],
                float.Parse(dataInfo[2]),
                float.Parse(dataInfo[3]),
                float.Parse(dataInfo[4]),
                bool.Parse(dataInfo[5]),
                bool.Parse(dataInfo[6]),
                new Vector3(float.Parse(dataInfo[7]), float.Parse(dataInfo[8]), float.Parse(dataInfo[9])),
                new Vector3(float.Parse(dataInfo[10]), float.Parse(dataInfo[11]), float.Parse(dataInfo[12])),
                dataInfo[13],
                bool.Parse(dataInfo[14]));
        } else if (dataInfo[0] == "state") {
            int curState = int.Parse(dataInfo[1]);
            switch (curState) {
                case 1:
                    match.setState(curState);
                    match.setHidingElapsedTime(int.Parse(dataInfo[2]));
                    break;
                case 2:
                    match.setState(curState);
                    match.setSeekingElapsedTime(int.Parse(dataInfo[2]));
                    break;
                case 3:
                    match.setState(curState);
                    match.setMatchElapsedTime(int.Parse(dataInfo[2]));
                    match.setZoneCenter(new Vector3(float.Parse(dataInfo[3]), float.Parse(dataInfo[4]), float.Parse(dataInfo[5])));
                    match.setZoneScale(new Vector3(float.Parse(dataInfo[6]), float.Parse(dataInfo[7]), float.Parse(dataInfo[8])));
                    break;
                case 4:
                    match.setState(curState);
                    match.setScoreboardElapsedTime(int.Parse(dataInfo[2]));
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
        } else if (dataInfo[0] == "start") {
            match = new MatchOrchestrator();
            isHider = bool.Parse(dataInfo[1]);
            gameStarted = true;
        } else if (dataInfo[0] == "caught") {
            if(dataInfo[1].Equals(guestName))
                isCaught = true;
            else {
                foreach (PlayerNetworkController p in players) {
                    if (p.getGuestName().Equals(dataInfo[1])) {
                        p.setIsCaught(true);
                        removedPlayersQueue.Add(p.getGuestName());
                        break;
                    }
                }
            }
        } else if (dataInfo[0] == "stop") {
            endGameFlag = true;
            match.stop();
            match = null;
            gameStarted = false;
        } else {
            newHost = dataInfo[0];

            List<string> guestNames = new List<string>();
            for (int i = 1; i < dataInfo.Length; i++) {
                string[] playerInfo = dataInfo[i].Split('\\');

                if (guestName.Equals(playerInfo[0])) {
                    score = int.Parse(playerInfo[1]);
                    continue;
                }

                guestNames.Add(playerInfo[0]);
                if(players.Count == 0) {                    
                    PlayerNetworkController playerNC = new PlayerNetworkController(playerInfo[0], int.Parse(playerInfo[1]));
                    players.Add(playerNC);
                    Debug.Log(playerInfo[0] + " joined");
                    continue;
                }
                
                for (int j = 0; j < players.Count; j++) {
                    if (players[j].getGuestName().Equals(playerInfo[0])) {
                        players[j].setScore(int.Parse(playerInfo[1]));
                        goto skipAdd;
                    }                    
                }
                PlayerNetworkController playernc = new PlayerNetworkController(playerInfo[0], int.Parse(playerInfo[1]));
                players.Add(playernc);
                Debug.Log(playerInfo[0] + " joined");
            skipAdd:;
            }
            updatePlayers(guestNames);
        }

    }

    public bool resetEndGameFlag() {
        bool temp = endGameFlag;
        endGameFlag = false;
        return temp;
    }

    public void registerCatch(string catchName) {        
        Broadcast("catch/" + catchName);
    }

    //Update players with list name
    private void updatePlayers (List<string> playerNames) {
        List<PlayerNetworkController> disconnects = new List<PlayerNetworkController>();
        foreach (PlayerNetworkController playerNC in players) {
            foreach (string playerName in playerNames) {
                if (playerNC.getGuestName().Equals(playerName)) {                    
                    goto con;
                }                
            }
            disconnects.Add(playerNC);
        con:;
        }
        foreach (PlayerNetworkController d in disconnects) {
            Debug.Log(d.getGuestName() + " left");
            removedPlayersQueue.Add(d.getGuestName());
            players.Remove(players[players.IndexOf(d)]);
        }
    }

    public List<string> resetRemovedPlayersQueue() {
        List<string> temp = new List<string>();
        for (int i=0; i < removedPlayersQueue.Count; i++) {
            temp.Add(removedPlayersQueue[i]);
        }
        removedPlayersQueue.Clear();
        return temp;
    }

    //Check if a player is already added
    private bool playerExists(string guestName) {
        for (int i=0; i < players.Count; i++) {
            if (players[i].getGuestName().Equals(guestName))
                return true;
        }
        return false;
    }

    //Map network data to player animator
    private void NetworkAnimator(string guestName, float v, float h, float f, bool isC, bool isJ, Vector3 pos, Vector3 rot, string pow, bool isHider) {

        if (players.Count == 0)
            return;

        foreach (PlayerNetworkController p in players) {
            if (p.getGuestName() == guestName) {

                p.setVerticalAxis(v);
                p.setHorizontalAxis(h);
                p.setFallDistance(f);
                p.IsCrouching(isC);
                p.IsJumping(isJ);
                p.setPosition(pos);
                p.setRotation(rot);
                p.setPower(pow);
                p.setMatchRole(isHider);

                return;
            }
        }
    }

    //Broadcast message to host
    public void Broadcast(string data) {
        try {
            if (!socketReady)
                return;
            writer.WriteLine(data);
            writer.Flush();
        } catch (Exception e) {
            if (e.ToString().Contains("Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host")) {
                gameStarted = false;
            }
        }
    }

    //Check if game is closed to close socket
    private void OnApplicationQuit() {
        CloseSocket();
    }
    //Check if code is disabled or GameObject is destroyed to close socket
    private void OnDisable() {
        CloseSocket();
    }
    //Close socket
    public void CloseSocket() {
        if (!socketReady)
            return;
        writer.Close();
        reader.Close();
        socket.Close();
        socketReady = false;
        gameStarted = false;
    }
    
    //Connect to server and send first message
    public void ConnectToHost(string hostIP) {
        if (socketReady)
            CloseSocket();

        new Thread(() => {

            connecting = true;

            try {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(hostIP), PORT);

                socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.SendTimeout = 1000;
                socket.ReceiveTimeout = 1000;
                socket.Connect(ep);
                stream = new NetworkStream(socket,FileAccess.ReadWrite,true);
                writer = new StreamWriter(stream);
                reader = new StreamReader(stream);
                socketReady = true;

                if (guestName != "")
                    Broadcast("new/"+guestName+"/"+IPv4());

                Debug.Log("Connected to Host");

            } catch (Exception e) {
                Debug.Log("Socket error: " + e.Message);
            }

            connecting = false;

        }).Start();
    }

    //Attempt to connect to newly assigned host
    private void ReconnectToNewHost(string hostIP) {
        if (socketReady)
            CloseSocket();        

        new Thread(() => {

            connecting = true;

            int reconAttempts = 0;
            while (reconAttempts < MAX_RECONNECT_ATTEMPTS && !socketReady) {

                reconAttempts++;

                try {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(hostIP), PORT);

                    socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.SendTimeout = 1000;
                    socket.ReceiveTimeout = 1000;
                    socket.Connect(ep);
                    stream = new NetworkStream(socket, FileAccess.ReadWrite, true);
                    writer = new StreamWriter(stream);
                    reader = new StreamReader(stream);
                    socketReady = true;

                    if (guestName != "")
                        Broadcast("old/" + guestName + "/" + IPv4() + "/" + isHider + "/" + score + "/" + isCaught);
                    else
                        CloseSocket();

                    Debug.Log("Reconnected to Host(After " + reconAttempts + " attempts)");
                    Thread.Sleep(250);
                } catch (Exception e) {
                    Debug.Log("Socket error(Reconnect attempt #" + reconAttempts + "): " + e.Message);
                    CloseSocket();
                    Thread.Sleep(1000);
                }
            }
            if (!socketReady) {
                Debug.Log("Could not reconnect");
            }

            connecting = false;

        }).Start();
    }

    public bool isConnecting() {
        return connecting;
    }

    public MatchOrchestrator getMatch() {
        return match;
    }
    public bool isBecomeHost() {
        bool temp = becomeHost;
        becomeHost = false;
        return temp;
    }
    public bool isRoleHider() {
        return isHider;
    }
    public void setRoleHider(bool isHider) {
        this.isHider = isHider;
    }
    public int getScore() {
        return score;
    }

    public bool isSocketReady() {
        return socketReady;
    }
    public bool isGameStarted() {
        return gameStarted;
    }

    public List<PlayerNetworkController> GetPlayerNetworkControllers() {
        return players;
    }

    public string toString() {
        if (players.Count == 0)
            return "";
        string str = players[0].getGuestName() + " (Host)/" + guestName;
        for(int i = 1; i < players.Count; i++) {
            str += "/" + players[i].getGuestName();
        }
        return str;
    }
    public string toStringStats() {
        if (players.Count == 0)
            return "";
        string str = players[0].getGuestName() + " (Host) -> score = " + players[0].getScore() + "/" + guestName + " -> score = " + score;
        for (int i = 1; i < players.Count; i++) {
            str += "/" + players[i].getGuestName() + " -> score = " + players[i].getScore();
        }
        return str;
    }

    public int getPlayerCount() {
        return players.Count;
    }
}
