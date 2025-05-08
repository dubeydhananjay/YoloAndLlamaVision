using UnityEngine;
using TMPro;
using UnityEngine.UIElements;

public class DetectionGridPanel : MonoBehaviour
{
    public DetectionGridCube cubePrefab;       // The cube prefab with a TextMeshPro label
    public int maxColumns = 3;          // Maximum number of columns in the grid
    public float spacing = 0.15f;       // Spacing between cubes
    private DetectionGridCube[] spawnedCubes;  // Array to track spawned cubes
    public TMPro.TextMeshProUGUI textMeshPro;

    private void Start()
    {
        //DisplayDetections();
    }

    public void DisplayDetections()
    {
        ClearCubes();
        int l = 15;
        int r = 4;
        int c = 3;
        spawnedCubes = new DetectionGridCube[l];
        for (int i = 0; i < l; i++)
        {
            int row = i / maxColumns;
            int col = i % maxColumns;

            // Calculate position in the grid
            Vector3 position = new Vector3(
                col * spacing - (c - 1) * spacing / 2, // Center horizontally
                -row * spacing + (r - 1) * spacing / 2,   // Center vertically
                0
            );

            // Instantiate the cube
            DetectionGridCube cube = Instantiate(cubePrefab, transform);
            cube.transform.localPosition = position;
            spawnedCubes[i] = cube;
            cube.SetText("TV");

        }

    }
    // Call this method with detection data to spawn the grid
    public void DisplayDetections(YoloDetection.Detection[] detections)
    {
        // Clear any existing cubes
        ClearCubes();

        if (detections == null || detections.Length == 0)
        {
            Debug.Log("No detections to display.");
            //textMeshPro.text += $"\nNo detections to display.";
            return;
        }
        textMeshPro.text += $"\nDetection: {detections.Length}";
        // Calculate grid size
        int numDetections = detections.Length;
        int rows = Mathf.CeilToInt((float)numDetections / maxColumns);
        int columns = Mathf.Min(numDetections, maxColumns);

        // Spawn cubes in a grid
        spawnedCubes = new DetectionGridCube[numDetections];
        for (int i = 0; i < numDetections; i++)
        {
            int row = i / maxColumns;
            int col = i % maxColumns;

            // Calculate position in the grid
            Vector3 position = new Vector3(
                col * spacing - (columns - 1) * spacing / 2, // Center horizontally
                -row * spacing + (rows - 1) * spacing / 2,   // Center vertically
                0
            );

            // Instantiate the cube
            DetectionGridCube cube = Instantiate(cubePrefab, transform);
            cube.transform.localPosition = position;
            cube.gameObject.SetActive(true);
            spawnedCubes[i] = cube;

            // Set the label
            cube.SetText($"{detections[i].@class}");
            cube.SetImage($"{detections[i].cropped_image}");
           
        }
    }

    // Clear all spawned cubes
    private void ClearCubes()
    {
        if (spawnedCubes != null)
        {
            foreach (DetectionGridCube cube in spawnedCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
        }
        spawnedCubes = null;
    }

    private void OnDestroy()
    {
        ClearCubes();
    }
}