using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.Events;
using System.Linq.Expressions;

public class RoomPlacer : EditorWindow
{

    [MenuItem("Tools/RoomPlacer")]

    public static void OpenWindow() => GetWindow(typeof(RoomPlacer));

    public int indexRoom = 0;

    SerializedObject so;
    public List<GameObject> roomsPrefabs = null;
    public GameObject[] prefabs;
    [SerializeField] bool[] selectedPrefabs;
    [SerializeField] Vector3 prefabRotation = new Vector3(0f,0f,0f);
    private List<GameObject> doorsList;
    private List<GameObject> roomsSpawned = new List<GameObject>();
    public bool turboMode = false;
    



    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;

        so = new SerializedObject(this);

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/RoomPlacer" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        if (selectedPrefabs == null || selectedPrefabs.Length != prefabs.Length)
        {
            selectedPrefabs = new bool[prefabs.Length];
            selectedPrefabs[indexRoom] = true;
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    
    private void OnGUI()
    {

        so.Update();

        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
        
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        DrawGUI();
        
        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }
        Transform cam = sceneView.camera.transform;

        if (CameraRaycast() != Vector3.zero)
        {

            if (Event.current.type == EventType.Repaint)
            {   
                DrawRoomPreview();
            }
        }

        if(Event.current.keyCode == KeyCode.B && Event.current.type == EventType.KeyDown)
        {
            RotateFunction(-1f);
        }
        if (Event.current.keyCode == KeyCode.N && Event.current.type == EventType.KeyDown)
        {
            RotateFunction(1f);
        }

        if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
        {
            PlaceRoom();
        }
        if (Event.current.keyCode == KeyCode.T && Event.current.type == EventType.KeyDown)
        {
            EnableTurboMode();
        }
    }

    void DrawGUI()
    {
        Handles.BeginGUI();
        Rect rect = new Rect(160, 8, 100, 100);
        
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();

            bool wasSelected = selectedPrefabs[i];
            selectedPrefabs[i] = GUI.Toggle(rect, selectedPrefabs[i], new GUIContent(icon));
            if (EditorGUI.EndChangeCheck() && selectedPrefabs[i] && !wasSelected)
            {
               
                for (int j = 0; j < selectedPrefabs.Length; j++)
                {
                    indexRoom = i;
                    if (j != i)
                        selectedPrefabs[j] = false;
                }
                Repaint(); 
            }

            rect.x += rect.width + 30;
           
            if (GUI.Button(new Rect(1000, 170, 50, 30), "+ 90�"))
                RotateFunction(1f);

            if (GUI.Button(new Rect(1000, 220, 50, 30), "- 90�"))
                RotateFunction(-1f);

            if (GUI.Button(new Rect(1000, 270, 50, 30), "Undo"))
                Undo();

            turboMode = GUI.Toggle(new Rect(1000, 330, 50, 30), turboMode, "Turbo");

        }

        Handles.EndGUI();
    }


    Vector3 CameraRaycast()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }
        
        return Vector3.zero;
        
    }

    void DrawRoomPreview()
    {
        if (prefabs[indexRoom] == null) return;
        if (CheckIfAllFalse(selectedPrefabs)) return;

        Matrix4x4 worldMatrix = Matrix4x4.TRS(CameraRaycast(), Quaternion.Euler(prefabRotation), Vector3.one);
        MeshFilter[] filters = prefabs[indexRoom].GetComponentsInChildren<MeshFilter>();

        foreach(MeshFilter filter in filters)
        {
            Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMatrix = worldMatrix * childToPose;

            Mesh mesh = filter.sharedMesh;
            Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetPass(0);
            Graphics.DrawMeshNow(mesh, childToWorldMatrix);
            
        }
    }

    void PlaceRoom()
    {
        if (prefabs[indexRoom] == null) return;

        if (CheckIfAllFalse(selectedPrefabs)) return;

        GameObject roomToSpawn = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[indexRoom]);
        roomsSpawned.Add(roomToSpawn);
        roomToSpawn.transform.SetPositionAndRotation(CameraRaycast(), Quaternion.Euler(prefabRotation));

        if(Selection.activeGameObject != null)
        {
            SnapRoom(roomToSpawn);
            if (turboMode)
            {
                Selection.activeGameObject = roomToSpawn;
            }

            if (turboMode)
            {
                Selection.activeGameObject = roomToSpawn;
            }
        }


    }

    void RotateFunction(float multiplier)
    {
        prefabRotation.y += (90f * multiplier);
    }

    bool CheckIfAllFalse(bool[] roomPrefabs)
    {
        foreach (bool value in roomPrefabs)
        {
            if (value)
            {                
                return false;
            }
        }       
        return true;
    }

    void SnapRoom(GameObject room)
    {
        // Ottieni le porte delle stanze
        GameObject roomToSpawnDoor = GetRoomToSpawnDoor(room);
        GameObject roomSelectedDoor = GetSelectedRoomDoor(Selection.activeGameObject, room);

        
        if (roomToSpawnDoor != null || roomSelectedDoor != null)
        {
            Vector3 positionDifference = roomSelectedDoor.transform.position - roomToSpawnDoor.transform.position;
            room.transform.position += positionDifference;           
        }
        else
        {
            Debug.LogError("No doors found. Check the Room's Door");
        }
        /*if (Vector3.Dot(roomToSpawnDoor.transform.forward, roomSelectedDoor.transform.forward) > -1)
        {
            Debug.LogError("Prefab not instantiated. Check if the selected room and spawning room rotation value are matchable");
            DestroyImmediate(room);
        }*/
    }

    GameObject CheckSpawnedNearestDoor()
    {
        if (doorsList.Count > 0)
        {
            GameObject nearestDoor = doorsList[0];
            float minDistance = Vector3.Distance(Selection.activeGameObject.transform.position, nearestDoor.transform.position);

            for (int i = 1; i < doorsList.Count; i++)
            {
                float distance = Vector3.Distance(Selection.activeGameObject.transform.position, doorsList[i].transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestDoor = doorsList[i];
                }
            }
            doorsList.Clear();
            return nearestDoor;
        }
        else           
            return null; 
        
    }

    GameObject GetRoomToSpawnDoor(GameObject roomPrefab)
    {
        Transform[] children = roomPrefab.GetComponentsInChildren<Transform>();

        doorsList = new List<GameObject>();

        foreach(Transform child in children)
        {
            if(child.GetComponent<Door>() != null)
            {
                doorsList.Add(child.gameObject);
            }
        }
        return CheckSpawnedNearestDoor();
    }
    GameObject CheckSelectedNearestDoor(GameObject room)
    {
        try
        {
            if (doorsList.Count > 0)
            {
                GameObject nearestDoor = doorsList[0];
                float minDistance = Vector3.Distance(room.transform.position, nearestDoor.transform.position);

                for (int i = 1; i < doorsList.Count; i++)
                {
                    float distance = Vector3.Distance(room.transform.position, doorsList[i].transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestDoor = doorsList[i];
                    }
                }
                doorsList.Clear();
                return nearestDoor;
            }
            else
            {
                Debug.LogWarning("La lista delle porte � vuota.");
                return null; // o un altro valore di default che abbia senso nel tuo contesto
            }
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    GameObject GetSelectedRoomDoor(GameObject selectedRoom, GameObject roomToSpawn)
    {
        Transform[] children = selectedRoom.GetComponentsInChildren<Transform>();

        doorsList = new List<GameObject>();

        foreach (Transform child in children)
        {
            if (child.GetComponent<Door>() != null)
            {
                doorsList.Add(child.gameObject);
            }
        }
        return CheckSelectedNearestDoor(roomToSpawn);
    }
    void Undo()
    {
        if(roomsSpawned.Count > 0)
        {
            GameObject lastObject = roomsSpawned[roomsSpawned.Count - 1];

            roomsSpawned.Remove(lastObject);
            DestroyImmediate(lastObject);
        }
    }
    void EnableTurboMode()
    {

        turboMode = !turboMode;

    }
}
