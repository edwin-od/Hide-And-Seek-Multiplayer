using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class HostNetwork {

    private const int PORT = 6321;
    private const int MAX_GUESTS = 6;

    private const int SEEKING_WIN_SCORE = 100;
    private const int HIDING_WIN_SCORE = 400;

    private List<Guest> guests;
    private List<Guest> disconnectList;
    private List<string> removedPlayersQueue;

    private TcpListener host;

    private MatchOrchestrator match;

    private PlayerController player;

    private bool lobbyStarted = false;
    private bool gameStarted = false;
    private bool isHider = false;
    private bool isCaught = false;
    private bool endGameFlag = false;

    private string hostName;

    private int score;


    public HostNetwork(string username) {
        hostName = username;        
        player = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Player")).GetComponent<PlayerController>();
        removedPlayersQueue = new List<string>();
    }
    public HostNetwork(string username, MatchOrchestrator match, bool isHider, int score, bool isCaught) {
        hostName = username;
        this.match = match;
        this.isHider = isHider;
        this.score = score;
        this.isCaught = isCaught;
        player = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Player")).GetComponent<PlayerController>();
        removedPlayersQueue = new List<string>();
    }

    public void regularUpdate() {

        if (!lobbyStarted)
            return;

        int caughtHiders = (isHider && isCaught) ? 1 : 0;
        int hidersCount = (isHider) ? 1 : 0;
        int seekersCount = (!isHider) ? 1 : 0;

        //Check if any client sent a message
        foreach (Guest g in guests) {
            if (!IsConnected(g.socket)) {

                disconnectList.Add(g);
                Debug.Log("Guest " + g.guestName + " disconnected.");

            } else {
                NetworkStream s = g.stream;
                if (s.DataAvailable) {
                    StreamReader reader = new StreamReader(s, true);
                    string data = reader.ReadLine();

                    if (data != null) {
                        //Process received message from guest
                        OnIncomingData(g, data);
                    }
                }
                if (gameStarted) {
                    if (g.isHider) {
                        caughtHiders += (g.isCaught) ? 1 : 0;
                        hidersCount++;
                    } else {
                        seekersCount++;
                    }
                }
            }
        }

        if (match != null && gameStarted && caughtHiders == hidersCount) {
            match.setMatchElapsedTime(MatchOrchestrator.MATCH_TIMEOUT);
        }
        if((match != null && (guests.Count == 0 || hidersCount == 0 || seekersCount == 0))) {
            endGameFlag = true;
            match.stop();
            match = null;
        }

        //Re-arrange client list after disconnects (to avoid 'foreach' conflict)
        for (int i = 0; i < disconnectList.Count; i++) {
            
            removedPlayersQueue.Add(guests[guests.IndexOf(disconnectList[i])].playerController.getGuestName());
            guests[guests.IndexOf(disconnectList[i])].socket.Close();
            guests.Remove(guests[guests.IndexOf(disconnectList[i])]);

        }
        disconnectList = new List<Guest>();        
    }

    public void updateHost() {        

        if (!lobbyStarted)
            return;        

        if (gameStarted && match != null) {     
            int state = match.getState();
            switch (state) {
                case 1:
                    Broadcast("state/" + state + "/" + match.getHidingElapsedTime());
                    break;
                case 2:
                    Broadcast("state/" + state + "/" + match.getSeekingElapsedTime());
                    break;
                case 3:
                    Broadcast("state/" + state + "/" + match.getMatchElapsedTime() + "/" 
                        + match.getZoneCenter().x + "/"
                        + match.getZoneCenter().y + "/"
                        + match.getZoneCenter().z + "/"
                        + match.zone.GetComponent<Transform>().localScale.x + "/"
                        + match.zone.GetComponent<Transform>().localScale.y + "/"
                        + match.zone.GetComponent<Transform>().localScale.z);                    
                    break;
                case 4:
                    Broadcast("state/" + state + "/" + match.getScoreboardElapsedTime());                    
                    break;
                case 5:
                    goto case 1;
                case 6:
                    goto case 2;
                case 7:
                    goto case 3;
                case 8:
                    goto case 4;
                case 9:
                    Broadcast("stop");
                    endGameFlag = true;
                    match.stop();
                    match = null;
                    break;
            }
        }

    }

    public void updateNetworkAnimator() {
        if (lobbyStarted && gameStarted && match != null && !isCaught) {
            Broadcast("animator/" +
                    hostName + "/" +
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

    public List<string> resetRemovedPlayersQueue() {
        List<string> temp = new List<string>();
        for (int i = 0; i < removedPlayersQueue.Count; i++) {
            temp.Add(removedPlayersQueue[i]);
        }
        removedPlayersQueue.Clear();
        return temp;
    }

    public bool resetEndGameFlag() {
        bool temp = endGameFlag;
        endGameFlag = false;
        return temp;
    }

    public void checkSurvivedHiders() {
        foreach (Guest g in guests) {
            if (g.isHider && !g.isCaught) {
                g.hidingWins++;
            }
        }
    }

    public void sendPlayersDetailsAndNewHostIP() {
        if (!lobbyStarted)
            return;
        if (guests.Count != 0) {
            string ipAndPlayerNames = guests[0].IPv4 + "/" + hostName + "\\" + score;
            for (int i = 0; i < guests.Count; i++) {
                ipAndPlayerNames += "/" + guests[i].guestName + "\\" + guests[i].getScore();
            }
            Broadcast(ipAndPlayerNames);
        }
    }
    
    //Host Pressed on Start Match
    public void startMatch() {
        match = new MatchOrchestrator(guests);

        foreach (Guest g in guests) {
            g.seekingWins = 0;
            g.hidingWins = 0;
        }

        //Assigning each player a role (Hider or Seeker)
        List<bool> roles = match.generateRoles();
        System.Random rand = new System.Random();
        if (roles != null) {
            if (roles.Count > 1) {
                int hiders = 0, seekers = 0;
                for (int i = 0; i < roles.Count; i++) {
                    if (roles[i])
                        hiders++;
                    else
                        seekers++;
                }
                if (hiders > seekers) {
                    isHider = false;
                } else if (hiders < seekers) {
                    isHider = true;
                } else {
                    isHider = rand.Next(0, 2) == 1 ? false : true;
                }
            } else {
                isHider = !roles[0];
            }
        } else {
            isHider = rand.Next(0, 2) == 1 ? false : true;
        }

        match.start();

        foreach (Guest g in guests) {
            Broadcast("start/" + g.isHider, new List<Guest>() { g });
        }

        gameStarted = true;        
    }
    //Start match on specific state
    public void startMatch(int startState) {
        new Thread(() => {
            int timeout = 10;
            while(timeout-- >= 0) {
                Thread.Sleep(1000);
            }
            match.setMatchGuests(guests);

            foreach (Guest g in guests) {
                g.seekingWins = 0;
                g.hidingWins = 0;
            }

            match.start(startState);

            foreach (Guest g in guests) {
                Broadcast("start/" + g.isHider, new List<Guest>() { g });
            }

            gameStarted = true;
        });
    }

    //What to do when a guest sends a message
    private void OnIncomingData(Guest guest, string data) {

        string[] gInf = data.Split('/');
        if (gInf[0] == "animator") {
            List<Guest> broadcastGuests = new List<Guest>();
            foreach (Guest g in guests) {
                if (!gInf[1].Equals(g.guestName)) {
                    broadcastGuests.Add(g);
                }
            }
            Broadcast(data, broadcastGuests);

            NetworkAnimator(gInf[1],
                float.Parse(gInf[2]),
                float.Parse(gInf[3]),
                float.Parse(gInf[4]),
                bool.Parse(gInf[5]),
                bool.Parse(gInf[6]),
                new Vector3(float.Parse(gInf[7]), float.Parse(gInf[8]), float.Parse(gInf[9])),
                new Vector3(float.Parse(gInf[10]), float.Parse(gInf[11]), float.Parse(gInf[12])),
                gInf[13],
                bool.Parse(gInf[14]));
        } else if (gInf[0] == "catch") {
            if (guest.isHider)
                return;
            Guest caughtGuest = FindGuest(gInf[1]);
            if ((caughtGuest == null && !gInf[1].Equals(hostName)) || (caughtGuest != null && !caughtGuest.isHider)
                 || (caughtGuest == null && gInf[1].Equals(hostName) && !isHider))
                return;
            if (caughtGuest == null && gInf[1].Equals(hostName)) {
                if (!isCaught) {
                    isCaught = true;
                    guest.seekingWins++;
                    Broadcast("caught/" + hostName);
                }
            } else {
                if (!caughtGuest.isCaught) {
                    caughtGuest.isCaught = true;
                    removedPlayersQueue.Add(caughtGuest.guestName);
                    guest.seekingWins++;
                    Broadcast("caught/" + caughtGuest.guestName);
                }
            }            
        } else if (gInf[0] == "new") {
            ProcessNewGuest(guest, gInf[1], gInf[2]);
        } else if (gInf[0] == "old") {
            ProcessOldGuest(guest, gInf[1], gInf[2], bool.Parse(gInf[3]), int.Parse(gInf[4]), bool.Parse(gInf[5]));
        }
    }

    public void resetCaught() {
        isCaught = false;
        foreach (Guest g in guests) {
            g.isCaught = false;
        }
    }
    public bool getIsCaught() {
        return isCaught;
    }

    public void registerCatch(string catchName) {
        Guest caughtGuest = FindGuest(catchName);
        if (caughtGuest != null && !caughtGuest.isCaught) {
            caughtGuest.isCaught = true;
            removedPlayersQueue.Add(catchName);
            score += Guest.SEEKING_WIN_SCORE;
            Broadcast("caught/" + catchName);
        }
    }

    //Map network data to player animator
    private void NetworkAnimator(string guestName, float v, float h, float f, bool isC, bool isJ, Vector3 pos, Vector3 rot, string pow, bool isHider) {

        if (guests.Count == 0)
            return;

        foreach (Guest g in guests) {            
            if (g.guestName.Equals(guestName)) {
                PlayerNetworkController p = g.playerController;
                p.setVerticalAxis(v);
                p.setHorizontalAxis(h);
                p.setFallDistance(f);
                p.IsCrouching(isC);
                p.IsJumping(isJ);
                p.setPosition(pos);
                p.setRotation(rot);
                p.setPower(pow);
                p.setMatchRole(isHider);
                p.setScore(g.getScore());
                return;
            }
        }
    }

    //Send message to all guests
    private void Broadcast(string data) {
        foreach (Guest g in guests) {
            //Debug.Log("Sending \"" + data + "\" to " + g.guestName);
            try {
                if (g == null || !g.socket.Connected) {
                    disconnectList.Add(g);
                    continue;
                }
                StreamWriter writer = new StreamWriter(g.stream);
                writer.WriteLine(data);
                writer.Flush();
            } catch (Exception e) {
                Debug.Log("Host write error: " + e.Message + " while sending to guest: " + g.guestName);
            }
        }
    }
    //Send message to List of guests
    public void Broadcast(string data, List<Guest> cl) {
        foreach (Guest c in cl) {
            //Debug.Log("Sending \"" + data + "\" to " + c.guestName);
            try {
                if (c == null || !c.socket.Connected)
                    continue;
                StreamWriter writer = new StreamWriter(c.stream);
                writer.WriteLine(data);
                writer.Flush();
            } catch (Exception e) {
                Debug.Log("Host write error: " + e.Message + " sending to guest: " + c.guestName);
            }
        }
    }

    private void ProcessNewGuest(Guest guest, string guestName, string IPv4) {

        if (guests.Count > MAX_GUESTS - 1) { // Including the host (this)
            Broadcast("Lobby full", new List<Guest>() { guest });
            Debug.Log("Guest " + guestName + " tried to connect but lobby is full!");
            disconnectList.Add(guest);
        } else if (gameStarted) {
            Broadcast("Game started", new List<Guest>() { guest });
            Debug.Log("Guest " + guestName + " tried to connect but game already started!");
            disconnectList.Add(guest);
        } else {
            Debug.Log("Guest " + guestName + " is connected");
            guest.guestName = guestName;
            guest.playerController = new PlayerNetworkController(guest.guestName);
            guest.IPv4 = IPv4;

            sendPlayersDetailsAndNewHostIP();
        }
    }
    private void ProcessOldGuest(Guest guest, string guestName, string IPv4, bool isHider, int score, bool isCaught) {

        if (guests.Count > MAX_GUESTS - 1) { // Including the host (this)
            Broadcast("Lobby full", new List<Guest>() { guest });
            Debug.Log("Guest " + guestName + " tried to reconnect but lobby is full!");
            disconnectList.Add(guest);
        } else {
            Debug.Log("Guest " + guestName + " has reconnected");
            guest.guestName = guestName;
            guest.setScore(score);
            guest.isHider = isHider;
            guest.isCaught = isCaught;
            guest.playerController = new PlayerNetworkController(guest.guestName);
            guest.IPv4 = IPv4;

            sendPlayersDetailsAndNewHostIP();
        }
    }

    //Accept a new TCP client and arrange its attributes
    private void AcceptTcpClient(IAsyncResult ar) {
        TcpListener listener = (TcpListener)ar.AsyncState;
        guests.Add(new Guest(listener.EndAcceptSocket(ar)));
        //Guest g = new Guest(listener.EndAcceptSocket(ar));
        StartListening();

        Guest g = guests[guests.Count - 1];
        NetworkStream s = g.stream;
        
        StreamReader reader = new StreamReader(s, true);
        string data = reader.ReadLine();

        if (data != null) {
            //Receive first client message
            string[] gInf = data.Split('/');
            if (gInf[0] == "new") {
                ProcessNewGuest(g, gInf[1], gInf[2]);
            } else if (gInf[0] == "old") {
                ProcessOldGuest(g, gInf[1], gInf[2], bool.Parse(gInf[3]), int.Parse(gInf[4]), bool.Parse(gInf[5]));
            } else {
                Debug.Log("Unidentified new guest is connected");
                Debug.Log(data);
            }
        }

    }

    //Begin acceptance of the new TCP client
    private void StartListening() {
        host.BeginAcceptTcpClient(AcceptTcpClient, host);
    }

    //Return a specific guest using guestName
    private Guest FindGuest(string name) {
        for (int i = 0; i < guests.Count; i++) {
            if (guests[i].guestName == name) {
                return guests[i];
            }
        }
        return null;
    }
    //Print whole guest List
    private void printGuestsList() {
        for (int i = 0; i < guests.Count; i++) {
            Debug.Log("Guest " + i + ": " + guests[i].guestName + " -- IPv4 "+guests[i].IPv4);
        }
    }

    //Check if guest is connected
    private bool IsConnected(Socket guest) {
        try {
            if (guest != null && guest.Connected) {
                if (guest.Poll(0, SelectMode.SelectRead)) {
                    return !(guest.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                return true;
            }
            return false;
        } catch {
            return false;
        }
    }

    //Start the lobby
    public void StartLobby() {

        lobbyStarted = false;
        guests = new List<Guest>();
        disconnectList = new List<Guest>();

        try {
            host = new TcpListener(IPAddress.Any, PORT);
            host.Start();

            StartListening();
            lobbyStarted = true;
            Debug.Log("Lobby started");

        } catch (Exception e) {

            Debug.Log("Socket error: " + e.Message);

        }
    }

    //Check if game is closed
    private void OnApplicationQuit() {
        terminateServer();
    }
    //Check if code is disabled or GameObject is destroyed
    private void OnDisable() {
        terminateServer();
    }

    //Send command to clients indicating that the server has been terminated and terminate it
    public void terminateServer() {
        if (!lobbyStarted)
            return;
        string tM = "Host terminated";

        if (guests.Count != 0) {
            tM += "/" + guests[0].IPv4;
        }

        Broadcast(tM);
        lobbyStarted = false;
        host.Stop();
        Debug.Log("Lobby terminated");
    }

    public MatchOrchestrator getMatch() {
        return match;
    }

    public bool isSocketReady() {
        return lobbyStarted;
    }

    public bool isGameStarted() {
        return gameStarted;
    }

    public bool isRoleHider() {
        return isHider;
    }
    public void setRoleHider(bool isHider) {
        this.isHider = isHider;
    }

    public List<PlayerNetworkController> GetPlayerNetworkControllers() {
        List<PlayerNetworkController> pncs = new List<PlayerNetworkController>();
        foreach(Guest g in guests) {
            pncs.Add(g.playerController);
        }
        return pncs;
    }

    public string toString() {
        string str = hostName + " (Host)";
        for (int i = 0; i < guests.Count; i++) {
            str += "/" + guests[i].guestName;
        }
        return str;
    }
    public string toStringStats() {
        string str = hostName + " (Host) -> score = " + score;
        for (int i = 0; i < guests.Count; i++) {
            str += "/" + guests[i].guestName + " -> score = " + guests[i].getScore();
        }
        return str;
    }
    
}

//ServerClient Object Class
public class Guest {
    public const int SEEKING_WIN_SCORE = 100;
    public const int HIDING_WIN_SCORE = 400;

    public Socket socket;
    public NetworkStream stream;
    public string guestName, IPv4;
    public bool isHider;
    public bool isCaught;
    public PlayerNetworkController playerController;
    public int hidingWins, seekingWins, preReconnectScore;

    public Guest(Socket guestSocket) {
        guestName = IPv4 = "";
        socket = guestSocket;
        stream = new NetworkStream(guestSocket);
        isHider = false;
        isCaught = false;
        hidingWins = 0;
        seekingWins = 0;
        preReconnectScore = 0;
    }

    public int getScore() {
        int totScore = 0;

        for (int i=0; i < seekingWins; i++) {
            totScore += SEEKING_WIN_SCORE;
        }
        for (int i = 0; i < hidingWins; i++) {
            totScore += HIDING_WIN_SCORE;
        }

        totScore += preReconnectScore;

        return totScore;
    }

    public void setScore(int score) {
        preReconnectScore = score;
    }
}
