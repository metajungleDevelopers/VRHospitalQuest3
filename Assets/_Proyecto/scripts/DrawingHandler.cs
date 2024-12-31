using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
//using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using MetaJungle.Utilities;
using UnityEngine.UI;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class DrawingHandler : SingletonMonoBehaviourPunCallbacks<DrawingHandler>
{
    private Dictionary<string, GameObject> activeGameObjectMeshes = new Dictionary<string, GameObject>(); // Almacena los GameObjects de los meshes activos por jugador
    private Dictionary<string, Mesh> activeMeshes = new Dictionary<string, Mesh>(); // Almacena los meshes activos por jugador
    private Dictionary<string, Vector3> lastMousePositions = new Dictionary<string, Vector3>(); // Almacena las últimas posiciones del mouse por jugador

    [HideInInspector]
    public List<GameObject> paintMeshes = new List<GameObject>(); // Lista de trazos creados
    [HideInInspector]
    public Vector3 worldPosition;

    private int currentSortingOrder = 0;

    public const string paintMeshName = "Paint Mesh";

    private Transform cameraNetworkTransform;

    [Header("Painting")]
    [Range(0.005f, 0.04f)]
    [SerializeField] float lineThickness = .1f; // Grosor de la línea
    [SerializeField] float minDistanceToDraw = .5f; // Distancia mínima para dibujar
    [SerializeField] float drawingDepth = 2; // Distancia a la que se crearan los gameobjects de los trazos

    [Header("Materiales")]
    [SerializeField] List<Material> paintMaterials; // Lista de materiales para los colores de pintura

    [Header("Rendering")]
    [SerializeField] Camera camara;
    [SerializeField] GameObject FMWebManager;

    [SerializeField] bool isPcClient;

    public bool canDraw = true;

    public UnityEvent OnDrawingActive;

    [SerializeField] private Slider thicknessSlider; // Slider para ajustar el grosor de la línea
    [SerializeField] private GameObject streamingBox;
    // Dibujo en pizarra extra

    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public GameObject renderTextureUIElement; // Assign your UI element here

    public RawImage renderTextureRawImage; // Assign this in the Inspector

    private Texture2D drawingTexture;
    private Vector2 lastMousePositionOnTexture;

    public float sendInterval = 0.1f; // Adjust as needed
    private float sendTimer = 0f;

    

    private List<DrawingCommand> drawingCommands = new List<DrawingCommand>();

    class RemoteDrawingState
    {
        public Vector2 lastMousePosition;
        public Color color;
        public float thickness;
    }

    private Dictionary<string, RemoteDrawingState> remoteDrawingStates = new Dictionary<string, RemoteDrawingState>();

    // Lista para almacenar las coordenadas de los píxeles modificados
    private List<Vector2> modifiedPixels = new List<Vector2>();

    private void Start()
    {
        InitializeMaterials();
        cameraNetworkTransform = camara.transform.parent;
        ColorHandler.Instance.paintColor = ColorHandler.Instance.paintColorPallete[ColorHandler.Instance.currentColorIndex];


        if (thicknessSlider != null)
        {
            // Configurar el slider de grosor
            thicknessSlider.value = lineThickness;
        }


        // Initialize the extra drawingTexture
        int width = (int)renderTextureRawImage.rectTransform.rect.width;
        int height = (int)renderTextureRawImage.rectTransform.rect.height;
        drawingTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        drawingTexture.filterMode = FilterMode.Point;

        // Optionally, fill the texture with a background color
        Color fillColor = Color.clear;
        Color[] fillPixels = new Color[width * height];
        for (int i = 0; i < fillPixels.Length; i++)
        {
            fillPixels[i] = fillColor;
        }
        drawingTexture.SetPixels(fillPixels);
        drawingTexture.Apply();

        // Assign the texture to the RawImage
        renderTextureRawImage.texture = drawingTexture;
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

    void Update()
    {
        worldPosition = BasicUtilities.GetMouseWorldPosition(camara, drawingDepth);

        bool isOverUI = IsPointerOverUIElement(renderTextureUIElement);

        if (canDraw && PhotonNetwork.InRoom)
        {
            if (isOverUI)
            {
                // Handle drawing on the render texture
                HandleRenderTextureDrawing();
            }
            else
            {
                // Existing code for drawing in 3D space
                if (Input.GetMouseButtonDown(0))
                {
                    CrearPrincipioDelTrazo(worldPosition, ColorHandler.Instance.currentColorIndex, PhotonNetwork.AuthValues.UserId);
                    if (camara.transform.parent == cameraNetworkTransform)
                    {
                        camara.transform.parent = null;
                        FMWebManager.SetActive(false);
                    }
                }

                if (Input.GetMouseButton(0))
                {
                    ContinuarTrazo(worldPosition, lineThickness, camara.transform.forward, PhotonNetwork.AuthValues.UserId);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    photonView.RPC(nameof(RecibirTrazo_RPC), RpcTarget.OthersBuffered, ColorHandler.Instance.currentColorIndex, activeMeshes[PhotonNetwork.AuthValues.UserId].vertices, activeMeshes[PhotonNetwork.AuthValues.UserId].uv, activeMeshes[PhotonNetwork.AuthValues.UserId].triangles);
                    TerminarTrazo(PhotonNetwork.AuthValues.UserId);
                }
            }
        }
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

            // Calcula el vector de dirección
            Vector3 normalDirection = Vector3.Cross(camaraForward, mouseForwardVector);
            normalDirection.Normalize();

            // Calcular nuevos vértices
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

    [PunRPC]
    public void RecibirTrazo_RPC(int colorIndex, Vector3[] vertices, Vector2[] uv, int[] triangles, bool isOverUI)
    {
    }

    void TerminarTrazo(string playerId)
    {
        if (activeMeshes.ContainsKey(playerId))
        {
            if(isPcClient)
                activeGameObjectMeshes[playerId].GetComponent<MeshRenderer>().enabled = false;

            activeMeshes.Remove(playerId);
            activeGameObjectMeshes.Remove(playerId);
        }
            

        if (lastMousePositions.ContainsKey(playerId))
            lastMousePositions.Remove(playerId);

        camara.transform.parent = cameraNetworkTransform;
        camara.transform.localPosition = Vector3.zero;
        camara.transform.localRotation = Quaternion.identity;
        FMWebManager.SetActive(true);
    }

    //Se llama desde el boton de la UI
    public void ActivatePainting()
    {
        EraserHandler.Instance.canErase = false;
        //ShapesHandler.Instance.canCreateShape = false;
        canDraw = true; // Desactiva el dibujo si el borrador está activado
        
        OnDrawingActive.Invoke();
    }

    public void ChangeThickness()
    {
        EraserHandler.Instance.eraseRadius = thicknessSlider.value;
        lineThickness = thicknessSlider.value;
    }

    // Pizarra extra

    bool IsPointerOverUIElement(GameObject uiElement)
    {
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == uiElement)
            {
                return true;
            }
        }
        return false;
    }

    void HandleRenderTextureDrawing()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePositionOnTexture = GetMousePositionOnTexture();
            DrawOnTexture(lastMousePositionOnTexture, Color.black, lineThickness);
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePositionOnTexture = GetMousePositionOnTexture();
            DrawLineOnTexture(lastMousePositionOnTexture, currentMousePositionOnTexture, Color.black, lineThickness);
            lastMousePositionOnTexture = currentMousePositionOnTexture;
        }
        else if (Input.GetMouseButtonUp(0) && modifiedPixels.Count > 0)
        {
            // Cuando el usuario deja de hacer el gesto de pinch, aplicar los cambios y enviar los píxeles modificados
            drawingTexture.Apply();  // Aplicar todos los cambios acumulados
            //photonView.RPC(nameof(SendPixelsForRenderTexture_RPC), RpcTarget.Others, modifiedPixels.ToArray());
            modifiedPixels.Clear(); // Limpiar la lista para el próximo dibujo
        }
    }

    Vector2 GetMousePositionOnTexture()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            renderTextureRawImage.rectTransform,
            Input.mousePosition,
            null,
            out Vector2 localPoint);

        Rect rect = renderTextureRawImage.rectTransform.rect;
        float x = (localPoint.x - rect.x) / rect.width * drawingTexture.width;
        float y = (localPoint.y - rect.y) / rect.height * drawingTexture.height;

        return new Vector2(x, y);
    }

    void DrawOnTexture(Vector2 position, Color color, float thickness)
    {
        int x = (int)position.x;
        int y = (int)position.y;
        int radius = Mathf.CeilToInt(thickness);

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (x + i >= 0 && x + i < drawingTexture.width && y + j >= 0 && y + j < drawingTexture.height)
                {
                    drawingTexture.SetPixel(x + i, y + j, color);
                    // Acumular la posición modificada en la lista
                    modifiedPixels.Add(new Vector2(x + i, y + j));
                }
            }
        }
        drawingTexture.Apply();
    }

    void DrawLineOnTexture(Vector2 start, Vector2 end, Color color, float thickness)
    {
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            DrawOnTexture(new Vector2(x0, y0), color, thickness);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        
    }

    public void ActivateStreamingScreen()
    {
        streamingBox.SetActive(true);
    }

}
