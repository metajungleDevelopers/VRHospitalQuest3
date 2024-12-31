using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using MetaJungle.Utilities;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HololensDrawingHandler : SingletonMonoBehaviourPunCallbacks<HololensDrawingHandler>
{
    private Dictionary<string, GameObject> activeGameObjectMeshes = new Dictionary<string, GameObject>(); // Almacena los GameObjects de los meshes activos por jugador
    private Dictionary<string, Mesh> activeMeshes = new Dictionary<string, Mesh>(); // Almacena los meshes activos por jugador
    private Dictionary<string, Vector3> lastMousePositions = new Dictionary<string, Vector3>(); // Almacena las ?ltimas posiciones del mouse por jugador

    [HideInInspector]
    public List<GameObject> paintMeshes = new List<GameObject>(); // Lista de trazos creados
    [HideInInspector]
    public Vector3 worldPosition;

    public bool canDraw = true;

    bool currentlyDrawing;

    public const string paintMeshName = "Paint Mesh";

    private int currentSortingOrder = 0;

    [Header("Quest Input")]
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private OVRInput.Controller rightController;
    [SerializeField] private float pinchThreshold = 0.7f;

    [Header("Painting")]
    [Range(0.001f, 0.04f)]
    [SerializeField] float lineThickness = .1f; // Grosor de la l?nea
    [SerializeField] float minDistanceToDraw = .5f; // Distancia m?nima para dibujar
    [SerializeField] float drawingDepth = 2; // Distancia a la que se crearan los gameobjects de los trazos

    [Header("Materiales")]
    [SerializeField] List<Material> paintMaterials; // Lista de materiales para los colores de pintura

    [SerializeField] Camera camara;

    [SerializeField] private Slider thicknessSlider; // Slider para ajustar el grosor de la l?nea


    //[SerializeField] private UIHandler uiHandler;
    [SerializeField] private GameObject streamingBox;

    public UnityEvent OnDrawingActive;

    bool startDrawingOverUI;

    private void Start()
    {
        InitializeMaterials();

        ColorHandler.Instance.paintColor = ColorHandler.Instance.paintColorPallete[ColorHandler.Instance.currentColorIndex];

        if (thicknessSlider != null)
        {
            // Configurar el slider de grosor
            thicknessSlider.value = lineThickness;
        }
    }

    private void InitializeMaterials()
    {
        // Inicializar materiales de pintura
        if (paintMaterials.Count < ColorHandler.Instance.paintColorPallete.Length)
        {
            paintMaterials.Clear();

            for (int i = 0; i < ColorHandler.Instance.paintColorPallete.Length; i++)
            {
                paintMaterials.Add(new Material(Shader.Find("Unlit/Color")));
                paintMaterials[i].color = ColorHandler.Instance.paintColorPallete[i];
            }
        }
    }

    private void Update()
    {
        if (!canDraw) return;

        bool isDrawing = false;
        Vector3 drawPosition = Vector3.zero;

        // Check for hand tracking pinch
        if (rightHand != null && rightHand.IsTracked)
        {
            float pinchStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            if (pinchStrength > pinchThreshold)
            {
                isDrawing = true;
                drawPosition = rightHand.PointerPose.position;
            }
        }
        // Check for controller trigger
        else if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, rightController))
        {
            isDrawing = true;
            drawPosition = OVRInput.GetLocalControllerPosition(rightController);
        }

        if (isDrawing)
        {
            HandleDrawing(drawPosition);
        }
        else if (currentlyDrawing)
        {
            TerminarTrazo_Quest();
        }
    }

    private void HandleDrawing(Vector3 position)
    {
        if (!PhotonNetwork.InRoom) return;

        string playerId = PhotonNetwork.AuthValues.UserId;
        worldPosition = position;

        if (!activeMeshes.ContainsKey(playerId))
        {
            CrearPrincipioDelTrazo(worldPosition, ColorHandler.Instance.currentColorIndex, playerId);
        }
        else
        {
            ContinuarTrazo(worldPosition, lineThickness, Camera.main.transform.forward, playerId);
        }
    }

    private void TerminarTrazo_Quest()
    {
        if (PhotonNetwork.InRoom)
            TerminarTrazo(PhotonNetwork.AuthValues.UserId);

        /*if (uiHandler.pincelActivado && currentlyDrawing)
        {
            uiHandler.ActivarDesactivarPintar();
            currentlyDrawing = false;
        }*/

        canDraw = false;
    }

    void CrearPrincipioDelTrazo(Vector3 worldPosition, int colorIndex, string playerId)
    {
        if (!activeMeshes.ContainsKey(playerId))
        {
            Mesh mesh = new Mesh();
            activeGameObjectMeshes[playerId] = BasicUtilities.CreateNewPaintMesh(mesh, colorIndex, paintMeshName, paintMeshes, paintMaterials, UndoRedoHandler.Instance.undoStack, ref currentSortingOrder);
            activeMeshes[playerId] = mesh;
        }

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        vertices[0] = worldPosition;
        vertices[1] = worldPosition;
        vertices[2] = worldPosition;
        vertices[3] = worldPosition;

        uv[0] = Vector2.zero;
        uv[1] = Vector2.zero;
        uv[2] = Vector2.zero;
        uv[3] = Vector2.zero;

        triangles[0] = 0;
        triangles[1] = 3;
        triangles[2] = 1;

        triangles[3] = 1;
        triangles[4] = 3;
        triangles[5] = 2;

        activeMeshes[playerId].vertices = vertices;
        activeMeshes[playerId].uv = uv;
        activeMeshes[playerId].triangles = triangles;
        activeMeshes[playerId].MarkDynamic();

        lastMousePositions[playerId] = worldPosition;
    }

    void ContinuarTrazo(Vector3 worldPosition, float lineThickness, Vector3 camaraForward, string playerId)
    {
        if (Vector3.Distance(worldPosition, lastMousePositions[playerId]) > minDistanceToDraw && activeMeshes.ContainsKey(playerId))
        {
            currentlyDrawing = true;

            Vector3[] vertices = new Vector3[activeMeshes[playerId].vertices.Length + 2];
            Vector2[] uv = new Vector2[activeMeshes[playerId].vertices.Length + 2];
            int[] triangles = new int[activeMeshes[playerId].triangles.Length + 6];

            activeMeshes[playerId].vertices.CopyTo(vertices, 0);
            activeMeshes[playerId].uv.CopyTo(uv, 0);
            activeMeshes[playerId].triangles.CopyTo(triangles, 0);

            int vIndex = vertices.Length - 4;
            int vIndex0 = vIndex + 0;
            int vIndex1 = vIndex + 1;
            int vIndex2 = vIndex + 2;
            int vIndex3 = vIndex + 3;

            Vector3 mouseForwardVector = (worldPosition - lastMousePositions[playerId]).normalized;

            // Calcula el vector de direcci?n
            Vector3 normalDirection = Vector3.Cross(camaraForward, mouseForwardVector);
            normalDirection.Normalize();

            // Calcular nuevos v?rtices
            Vector3 newVertexUp = worldPosition + normalDirection * lineThickness;
            Vector3 newVertexDown = worldPosition - normalDirection * lineThickness;

            vertices[vIndex2] = newVertexUp;
            vertices[vIndex3] = newVertexDown;

            uv[vIndex2] = Vector2.zero;
            uv[vIndex3] = Vector2.zero;

            int tIndex = triangles.Length - 6;

            triangles[tIndex + 0] = vIndex0;
            triangles[tIndex + 1] = vIndex2;
            triangles[tIndex + 2] = vIndex1;

            triangles[tIndex + 3] = vIndex1;
            triangles[tIndex + 4] = vIndex2;
            triangles[tIndex + 5] = vIndex3;

            activeMeshes[playerId].vertices = vertices;
            activeMeshes[playerId].uv = uv;
            activeMeshes[playerId].triangles = triangles;

            lastMousePositions[playerId] = worldPosition;
        }
    }

    void TerminarTrazo(string playerId)
    {
        if (activeMeshes.ContainsKey(playerId))
        {
            activeMeshes.Remove(playerId);
            activeGameObjectMeshes.Remove(playerId);
        }


        if (lastMousePositions.ContainsKey(playerId))
            lastMousePositions.Remove(playerId);
    }


    private void TerminarTrazo_Hololens()
    {
        if (PhotonNetwork.InRoom)
            TerminarTrazo(PhotonNetwork.AuthValues.UserId);

        //Cada vez que se deje de hacer pinch quitaremos el que se pueda dibujar para no dibujar por accidente
        /*if (uiHandler.pincelActivado && currentlyDrawing)
        {
            uiHandler.ActivarDesactivarPintar();
            currentlyDrawing = false;
        }*/

        canDraw = false;
    }

    [PunRPC]
    public void RecibirTrazo_RPC(int colorIndex, Vector3[] vertices, Vector2[] uv, int[] triangles)
    {
        Mesh receivedMesh = new Mesh();
        receivedMesh.vertices = vertices;
        receivedMesh.uv = uv;
        receivedMesh.triangles = triangles;

        BasicUtilities.CreateNewPaintMesh(receivedMesh, colorIndex, paintMeshName, paintMeshes, paintMaterials, UndoRedoHandler.Instance.undoStack, ref currentSortingOrder);
    }

    //Se llama desde el boton de la UI
    public void ActivatePainting()
    {
        EraserHandler.Instance.canErase = false;
        //ShapesHandler.Instance.canCreateShape = false;
        canDraw = true; // Desactiva el dibujo si el borrador est? activado

        OnDrawingActive.Invoke();
    }

    public void ChangeThickness()
    {
        EraserHandler.Instance.eraseRadius = thicknessSlider.value;
        lineThickness = thicknessSlider.value;
    }

    public void ActivateStreamingScreen()
    {
        streamingBox.SetActive(true);
    }
}
