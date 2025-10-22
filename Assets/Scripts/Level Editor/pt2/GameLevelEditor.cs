using UnityEngine;
using System.Collections.Generic;

public class GameLevelEditor : MonoBehaviour
{
    public Camera editorCamera;
    public GameSongManager songManager;

    private bool placingHold = false;
    private bool placingTap = false;
    private GameNote currentHoldNote = null;

    private List<GameNote> selectedNotes = new List<GameNote>();
    private bool copyMode = false;
    private Vector3 copyStart;
    private Vector3 copyEnd;

    void Update()
    {
        HandleCamera();
        HandlePlacement();
        HandleSelection();
    }

    private void HandleCamera()
    {
        float moveSpeed = 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) editorCamera.transform.position += Vector3.up * moveSpeed;
        if (Input.GetKey(KeyCode.S)) editorCamera.transform.position += Vector3.down * moveSpeed;
        if (Input.GetKey(KeyCode.A)) editorCamera.transform.position += Vector3.left * moveSpeed;
        if (Input.GetKey(KeyCode.D)) editorCamera.transform.position += Vector3.right * moveSpeed;

        editorCamera.transform.position += Vector3.forward * Input.mouseScrollDelta.y * 10f;
    }

    private void HandlePlacement()
    {
        if ((placingTap || placingHold) && Input.GetMouseButtonDown(0))
        {
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);

                // Snap to segment
                float segmentHeight = 1f; // or calculated from BPM
                if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    int nearestSegment = Mathf.RoundToInt(hit.y / segmentHeight);
                    hit.y = nearestSegment * segmentHeight;
                }

                int lane = GetNearestLane(hit.x);

                // Instantiate note
                GameObject noteObj = Instantiate(songManager.notePrefab, new Vector3(songManager.laneXPositions[lane], hit.y, 0f), Quaternion.identity);
                GameNote noteScript = noteObj.GetComponent<GameNote>();
                noteScript.isHold = placingHold;
                noteScript.holdDuration = placingHold ? 1f : 0f;
                noteScript.time = hit.y * songManager.songData.speedMultiplier;
                noteScript.speedMultiplier = songManager.songData.speedMultiplier;
                noteScript.hitLine = songManager.hitLine;
                if (noteScript.isHold) noteScript.SetupHoldVisual();
            }
        }
    }

    private int GetNearestLane(float x)
    {
        int laneIndex = 0;
        float closest = Mathf.Abs(x - songManager.laneXPositions[0]);
        for (int i = 1; i < songManager.laneXPositions.Length; i++)
        {
            float dist = Mathf.Abs(x - songManager.laneXPositions[i]);
            if (dist < closest)
            {
                closest = dist;
                laneIndex = i;
            }
        }
        return laneIndex;
    }

    private void HandleSelection()
    {
        // TODO: implement copy / drag selection
    }

    public void ToggleCopyMode() => copyMode = !copyMode;
    public void ToggleDragMode() => Debug.Log("Drag Mode enabled");
}