using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SongCanvasController : MonoBehaviour
{
    public GameObject songCanvas;
    public GameObject songListCanvas;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Only re assign if we are back in the Lobby
        if (scene.name == "Lobby")
        {
            Debug.Log("LOADED LOBBY");
            songCanvas = GameObject.Find("Song Canvas");
            songListCanvas = GameObject.Find("Song List Canvas");
        }
    }

    private void Update()
    {
        if (songCanvas == null || songListCanvas == null) return;

        bool show = SceneManager.GetActiveScene().name == "Lobby" && NetworkManager.Singleton.IsHost;

        songCanvas.SetActive(show);
        songListCanvas.SetActive(show);
    }
}