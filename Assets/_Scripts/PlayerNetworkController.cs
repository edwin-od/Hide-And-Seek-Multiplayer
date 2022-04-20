using UnityEngine;

public class PlayerNetworkController {

    private string guestName;
    private Animator anim;
    private GameObject playerModel;
    private bool isHider;
    private bool isCaught;
    private int score;

    public PlayerNetworkController(string guestName) {
        this.guestName = guestName;
    }
    public PlayerNetworkController(string guestName, int score) {
        this.guestName = guestName;
        this.score = score;
    }


    public void setGuestName(string name) {
        guestName = name;
    }
    public string getGuestName() {
        return guestName;
    }
    public void setVerticalAxis(float v) {
        if(anim != null)
            anim.SetFloat("vertical", v);
    }
    public void setHorizontalAxis(float h) {
        if (anim != null)
            anim.SetFloat("horizontal", h);
    }
    public void setFallDistance(float d) {
        if (anim != null)
            anim.SetFloat("fall", d);
    }
    public void IsCrouching(bool c) {
        if (anim != null)
            anim.SetBool("crouch", c);
    }
    public void IsJumping(bool j) {
        if (anim != null)
            anim.SetBool("jump", j);
    }
    public void setPosition(Vector3 p) {
        if(playerModel != null)
            playerModel.GetComponent<Transform>().position = p;
    }
    public void setRotation(Vector3 r) {
        if (playerModel != null)
            playerModel.GetComponent<Transform>().eulerAngles = r;
    }
    public void setPower(string pow) {
        if (pow.Equals("null"))
            return;

    }
    public void setMatchRole(bool isHider) {
        if (isHider != this.isHider) {
            //Flip team
            this.isHider = isHider;
        }
    }
    public bool getIsHider() {
        return isHider;
    }
    public int getScore() {
        return score;
    }
    public void setScore(int score) {
        this.score = score;
    }
    public bool getIsCaught() {
        return isCaught;
    }
    public void setIsCaught(bool isCaught) {
        this.isCaught = isCaught;
    }

    public GameObject getPlayerModel() {
        return playerModel;
    }
    public void setPlayerModel(GameObject playerModel) {
        this.playerModel = playerModel;
        if(playerModel != null)
            anim = this.playerModel.GetComponent<Animator>();
    }
}
