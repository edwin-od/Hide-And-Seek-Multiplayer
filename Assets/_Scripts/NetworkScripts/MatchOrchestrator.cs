using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class MatchOrchestrator {

    public const int HIDING_TIMEOUT = 30; //90s (1m 30s)
    public const int SEEKING_TIMEOUT = 15; //45s (0m 45s)
    public const int MATCH_TIMEOUT = 300; //600s (10m 0s)
    public const int SCOREBOARD_TIMEOUT = 20; //10s (0m 10s)
    public const float ZONE_SHRINK_MATCH_TIME_PERCENTAGE = 0.8f;

    public GameObject zone;

    private System.Random rand;

    private List<Guest> guests;    
    private Vector3 zoneCenter;
    private Vector3 zoneInitialScale;
    private float zoneShrinkRate;
    private int hidingElapsedTime;
    private int seekingElapsedTime;
    private int matchElapsedTime;
    private int scoreboardElapsedTime;
    private int zoneShrinkQueue;

    private int state;

    /*
     * state = 0 -> Match has not started
     * state = 1 -> Hiders dropping on the map && Counting down for hiders to hide
     * state = 2 -> Seekers dropping on the map && Counting down for zone to start shrinking
     * state = 3 -> Zone is shrinking && Counting down for the match to end
     * state = 4 -> Resetting for second round && Switching roles && Showing results 
     * state = 5 -> Hiders dropping on the map && Counting down for hiders to hide
     * state = 6 -> Seekers dropping on the map && Counting down for zone to start shrinking
     * state = 7 -> Zone is shrinking && Counting down for the match to end
     * state = 8 -> Match has ended && Showing results
     * state = 9 -> Returning to lobby
     */


    public MatchOrchestrator(List<Guest> guests) {
        this.guests = guests;
        zone = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Zone"));
        rand = new System.Random();
        zoneCenter = Vector3.zero;
        zoneInitialScale = zone.GetComponent<Transform>().localScale;
        //zoneShrinkRate = 0.010417f; // (5 / (8 * 60))
        setZoneShrinkTime((int)(MATCH_TIMEOUT * ZONE_SHRINK_MATCH_TIME_PERCENTAGE));
        hidingElapsedTime = 0;
        seekingElapsedTime = 0;
        matchElapsedTime = 0;
        scoreboardElapsedTime = 0;
        zoneShrinkQueue = 0;
        state = 0;
    }
    public MatchOrchestrator() {
        rand = new System.Random();
        zone = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.CompareTag("Zone"));
        zoneCenter = Vector3.zero;
        zoneInitialScale = zone.GetComponent<Transform>().localScale;
        //zoneShrinkRate = 0.010417f; // (5 / (8 * 60))
        setZoneShrinkTime((int)(MATCH_TIMEOUT * ZONE_SHRINK_MATCH_TIME_PERCENTAGE));
        hidingElapsedTime = 0;
        seekingElapsedTime = 0;
        matchElapsedTime = 0;
        scoreboardElapsedTime = 0;
        zoneShrinkQueue = 0;
        state = 0;
    }

    public void setMatchGuests(List<Guest> guests) {
        this.guests = guests;
    }



    public List<bool> generateRoles() {
        if (guests == null || guests.Count == 0)
            return null;

        List<bool> isHiders = new List<bool>();
        
        bool roleRandomStart = rand.Next(0, 2) == 1 ? false : true;
        for (int i=0; i < guests.Count; i++) {

            if (i % 2 == 0) {
                isHiders.Add(roleRandomStart);
                guests[i].isHider = roleRandomStart;
            } else {
                isHiders.Add(!roleRandomStart);
                guests[i].isHider = !roleRandomStart;
            }
        }

        return isHiders;
    }
    public List<bool> switchRoles() {
        if (guests == null || guests.Count == 0)
            return null;

        List<bool> isHiders = new List<bool>();

        foreach (Guest g in guests) {
            g.isHider = !g.isHider;
            isHiders.Add(g.isHider);
        }

        return isHiders;
    }



    public Vector3 generateZoneCenter() {
        //zoneCenter = new Vector3(rand.Next(0, 1001), (zone != null ? zone.GetComponent<Transform>().position.y : -20f), rand.Next(0, 1001));
        zoneCenter = new Vector3(rand.Next(250, 751), (zone != null ? zone.GetComponent<Transform>().position.y : -20f), rand.Next(250, 751));
        return zoneCenter;
    }
    public Vector3 getZoneCenter() {
        return zoneCenter;
    }
    public Vector3 setZoneCenter(Vector3 zoneCenter) {
        this.zoneCenter = zoneCenter;
        return zoneCenter;
    }


    public float getZoneScale() {
        return (zone.GetComponent<Transform>().localScale.x + zone.GetComponent<Transform>().localScale.y) / 2f; 
    }
    public float getInitialZoneScale() {
        return (zoneInitialScale.x + zoneInitialScale.y) / 2f;
    }
    public Vector3 setZoneScale(Vector3 zoneScale) {
        if (zone == null)
            return Vector3.zero;
        zone.GetComponent<Transform>().localScale = zoneScale;
        return zone.GetComponent<Transform>().localScale;
    }
    public void resetZoneScale() {
        zone.GetComponent<Transform>().localScale = zoneInitialScale;
    }
    public void shrinkZone() {
        if (zone.GetComponent<Transform>().localScale.x > 0 && zone.GetComponent<Transform>().localScale.y > 0) { 
            zone.GetComponent<Transform>().localScale = new Vector3(zone.GetComponent<Transform>().localScale.x - zoneShrinkRate,
                                                                        zone.GetComponent<Transform>().localScale.y - zoneShrinkRate, 
                                                                        zone.GetComponent<Transform>().localScale.z);
        } else {
            zone.GetComponent<Transform>().localScale = Vector3.zero;
        }
    }


    public float getZoneShrinkRate() {
        return zoneShrinkRate;
    }
    public void setZoneShrinkTime(int totalZoneShrinkTime) {
        if (zone != null) {
            float zoneScale = (zone.GetComponent<Transform>().localScale.x + zone.GetComponent<Transform>().localScale.y) / 2f;
            zoneShrinkRate = zoneScale / totalZoneShrinkTime;
        }
    }
    public float setZoneShrinkRate(float zoneShrinkRate) {
        this.zoneShrinkRate = zoneShrinkRate;
        return zoneShrinkRate;
    }
    
    public int resetZoneShrinkQueue() {
        int temp = zoneShrinkQueue;
        zoneShrinkQueue = 0;
        return temp;
    }



    public int getState() {
        return state;
    }
    public int setState(int state) {
        this.state = state;
        return state;
    }



    public int getHidingElapsedTime() {
        return hidingElapsedTime;
    }
    public int getHidingRemainingTime() {
        return HIDING_TIMEOUT - hidingElapsedTime;
    }
    public int setHidingElapsedTime(int hidingElapsedTime) {
        this.hidingElapsedTime = hidingElapsedTime;
        return hidingElapsedTime;
    }

    public int getSeekingElapsedTime() {
        return seekingElapsedTime;
    }
    public int getSeekingRemainingTime() {
        return SEEKING_TIMEOUT - seekingElapsedTime;
    }
    public int setSeekingElapsedTime(int seekingElapsedTime) {
        this.seekingElapsedTime = seekingElapsedTime;
        return seekingElapsedTime;
    }

    public int getMatchElapsedTime() {
        return matchElapsedTime;
    }
    public int getMatchRemainingTime() {
        return MATCH_TIMEOUT - matchElapsedTime;
    }
    public int setMatchElapsedTime(int matchElapsedTime) {
        this.matchElapsedTime = matchElapsedTime;
        return matchElapsedTime;
    }

    public int getScoreboardElapsedTime() {
        return scoreboardElapsedTime;
    }
    public int getScoreboardRemainingTime() {
        return SCOREBOARD_TIMEOUT - scoreboardElapsedTime;
    }
    public int setScoreboardElapsedTime(int scoreboardElapsedTime) {
        this.scoreboardElapsedTime = scoreboardElapsedTime;
        return scoreboardElapsedTime;
    }


    //Starting match
    public void start() {
        new Thread(() => {
            
            ///////////////////-- ROUND 1 --///////////////////

            //STATE 0 in action
            state = 0;

            hidingElapsedTime = 0;
            seekingElapsedTime = 0;
            matchElapsedTime = 0;
            scoreboardElapsedTime = 0;

            //STATE 1 in action
            state = 1;
            hidingElapsedTime = 0;
            while (hidingElapsedTime++ < HIDING_TIMEOUT) {
                Thread.Sleep(1000);
            }
            hidingElapsedTime = 0;

            //STATE 2 in action
            state = 2;
            seekingElapsedTime = 0;
            while (seekingElapsedTime++ < SEEKING_TIMEOUT) {
                Thread.Sleep(1000);
            }
            seekingElapsedTime = 0;

            //STATE 3 in action
            state = 3;
            matchElapsedTime = 0;
            while (matchElapsedTime++ < MATCH_TIMEOUT) {
                zoneShrinkQueue++;
                Thread.Sleep(1000);
            }
            matchElapsedTime = 0;

            //STATE 4 in action
            state = 4;
            scoreboardElapsedTime = 0;
            while (scoreboardElapsedTime++ < SCOREBOARD_TIMEOUT) {
                Thread.Sleep(1000);
            }
            scoreboardElapsedTime = 0;

            ///////////////////-- ROUND 2 --///////////////////

            //STATE 5 in action
            state = 5;
            hidingElapsedTime = 0;
            while (hidingElapsedTime++ < HIDING_TIMEOUT) {
                Thread.Sleep(1000);
            }
            hidingElapsedTime = 0;

            //STATE 6 in action
            state = 6;
            seekingElapsedTime = 0;
            while (seekingElapsedTime++ < SEEKING_TIMEOUT) {
                Thread.Sleep(1000);
            }
            seekingElapsedTime = 0;

            //STATE 7 in action
            state = 7;
            matchElapsedTime = 0;
            while (matchElapsedTime++ < MATCH_TIMEOUT) {
                zoneShrinkQueue++;
                Thread.Sleep(1000);
            }
            matchElapsedTime = 0;

            //STATE 8 in action
            state = 8;
            scoreboardElapsedTime = 0;
            while (scoreboardElapsedTime++ < SCOREBOARD_TIMEOUT) {
                Thread.Sleep(1000);
            }
            scoreboardElapsedTime = 0;

            //STATE 9 in action 
            state = 9;

        }).Start();
    }

    //Starting match
    public void start(int startState) {
        new Thread(() => {
            switch (startState) {
                case 0:
                    ///////////////////-- ROUND 1 --///////////////////
                    
                    //STATE 0 in action 
                    state = 0;                    
                    goto case 1;
                case 1:
                    //STATE 1 in action
                    state = 1;
                    hidingElapsedTime = 0;
                    while (hidingElapsedTime++ < HIDING_TIMEOUT) {
                        Thread.Sleep(1000);
                    }
                    hidingElapsedTime = 0;
                    goto case 2;
                case 2:
                    //STATE 2 in action
                    state = 2;
                    seekingElapsedTime = 0;
                    while (seekingElapsedTime++ < SEEKING_TIMEOUT) {
                        Thread.Sleep(1000);
                    }
                    seekingElapsedTime = 0;
                    goto case 3;
                case 3:
                    //STATE 3 in action
                    state = 3;
                    matchElapsedTime = 0;
                    while (matchElapsedTime++ < MATCH_TIMEOUT) {
                        zoneShrinkQueue++;
                        Thread.Sleep(1000);
                    }
                    matchElapsedTime = 0;
                    goto case 4;
                case 4:
                    //STATE 4 in action
                    state = 4;
                    scoreboardElapsedTime = 0;
                    while (scoreboardElapsedTime++ < SCOREBOARD_TIMEOUT) {
                        Thread.Sleep(1000);
                    }
                    scoreboardElapsedTime = 0;
                    
                    goto case 5;
                case 5:
                    ///////////////////-- ROUND 2 --///////////////////

                    //STATE 5 in action
                    state = 5;
                    hidingElapsedTime = 0;
                    while (hidingElapsedTime++ < HIDING_TIMEOUT) {
                        Thread.Sleep(1000);
                    }
                    hidingElapsedTime = 0;
                    goto case 6;
                case 6:
                    //STATE 6 in action
                    state = 6;
                    seekingElapsedTime = 0;
                    while (seekingElapsedTime++ < SEEKING_TIMEOUT) {
                        Thread.Sleep(1000);
                    }
                    seekingElapsedTime = 0;
                    goto case 7;
                case 7:
                    //STATE 7 in action
                    state = 7;
                    matchElapsedTime = 0;
                    while (matchElapsedTime++ < MATCH_TIMEOUT) {
                        zoneShrinkQueue++;
                        Thread.Sleep(1000);
                    }
                    matchElapsedTime = 0;
                    goto case 8;
                case 8:
                    //STATE 8 in action
                    state = 8;
                    scoreboardElapsedTime = 0;
                    while (scoreboardElapsedTime++ < SCOREBOARD_TIMEOUT) {
                        Thread.Sleep(1000);
                    }
                    scoreboardElapsedTime = 0;
                    goto case 9;
                case 9:
                    //STATE 9 in action
                    state = 9;
                    break;
            }

        }).Start();
    }

    //Stopping match
    public void stop() {
        resetZoneScale();
    }
}
