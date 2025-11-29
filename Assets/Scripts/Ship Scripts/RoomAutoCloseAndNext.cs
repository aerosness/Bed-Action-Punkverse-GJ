using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomAutoCloseAndNext : MonoBehaviour
{
    public DoorButtonSlidingDoors doorSystem;
    public float delay = 0.3f;
    public string playerTag = "Player";

    bool done = false;

    private void OnTriggerEnter(Collider other)
    {
        if (done) return;
        if (!other.CompareTag(playerTag)) return;

        done = true;
        StartCoroutine(Process());
    }

    System.Collections.IEnumerator Process()
    {
        doorSystem.ForceCloseDoors();
        yield return new WaitForSeconds(doorSystem.openDuration + delay);

        int current = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(current + 1);
    }
}
