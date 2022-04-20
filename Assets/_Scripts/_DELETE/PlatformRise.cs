using UnityEngine;

public class PlatformRise : MonoBehaviour {

    private Vector3 teleportLocation = new Vector3(0f, 1520f, -350f);
	
    void OnTriggerEnter(Collider other) {

        if (other.tag == "Player") {
            other.GetComponent<CharacterController>().enabled = false;
            other.transform.SetPositionAndRotation(teleportLocation, Quaternion.Euler(0f, 0f, 0f));
            other.GetComponent<CharacterController>().enabled = true;
        }

    }
    
}
