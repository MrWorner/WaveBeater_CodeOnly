using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IsoSpriteSorting : MonoBehaviour
{
#if UNITY_EDITOR
    private static double lastSortTime = 0;
    private const double SortInterval = 0.033;

    [InitializeOnLoad]
    static class EditorUpdater
    {
        static EditorUpdater()
        {
            EditorApplication.update += OnEditorUpdate;
        }
    }

    private static void OnEditorUpdate()
    {
        if (Application.isPlaying) return;
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastSortTime < SortInterval) return;
        lastSortTime = currentTime;
        IsoSpriteSorting[] sorters = FindObjectsByType<IsoSpriteSorting>(FindObjectsSortMode.None);
        if (sorters.Length > 0)
        {
            sorters[0].SortScene();
        }
    }
#endif

    public bool isMovable;
    [SerializeField]
    private bool renderBelowAll;
    public bool RenderBelowAll
    {
        get { return renderBelowAll; }
        set { renderBelowAll = value; }
    }

    [NonSerialized]
    public bool registered = false;
    [NonSerialized]
    public bool forceSort;
    [NonSerialized]
    public readonly List<IsoSpriteSorting> staticDependencies = new List<IsoSpriteSorting>(16);
    [NonSerialized]
    public readonly List<IsoSpriteSorting> inverseStaticDependencies = new List<IsoSpriteSorting>(16);
    [NonSerialized]
    public readonly List<IsoSpriteSorting> movingDependencies = new List<IsoSpriteSorting>(8);

    private readonly List<IsoSpriteSorting> visibleStaticDependencies = new List<IsoSpriteSorting>(16);
    private int visibleStaticLastRefreshFrame = 0;
    public List<IsoSpriteSorting> VisibleStaticDependencies
    {
        get
        {
            if (visibleStaticLastRefreshFrame < Time.frameCount)
            {
                IsoSpriteSortingManager.FilterListByVisibility(staticDependencies, visibleStaticDependencies);
                visibleStaticLastRefreshFrame = Time.frameCount;
            }
            return visibleStaticDependencies;
        }
    }
    public List<IsoSpriteSorting> VisibleMovingDependencies => movingDependencies;

    public enum SortType
    {
        Point,
        Line
    }

    public SortType sortType = SortType.Point;
    public Vector2 SorterPositionOffset = new Vector2();
    public Vector2 SorterPositionOffset2 = new Vector2();
    private Transform[] renderersToSortTransforms;

    [Title("Компоненты для сортировки")]
    public Renderer[] renderersToSort;
    public Canvas[] canvasesToSort;

    private Transform t;

    [NonSerialized] public Vector2 SortingPoint1;
    [NonSerialized] public Vector2 SortingPoint2;
    private int lastRefreshedFrame;
    public void RefreshCache()
    {
        if ((renderersToSort != null && renderersToSort.Length > 0) || (canvasesToSort != null && canvasesToSort.Length > 0))
        {
            // Для границ по-прежнему используем рендерер, если он есть
            if (renderersToSort != null && renderersToSort.Length > 0)
                cachedBounds = new Bounds2D(renderersToSort[0].bounds);

            Vector2 pos = t.position;
            SortingPoint1 = SorterPositionOffset + pos;
            SortingPoint2 = SorterPositionOffset2 + pos;
        }
        lastRefreshedFrame = Time.frameCount;
    }

    public Vector2 AsPoint => sortType == SortType.Line ? (SortingPoint1 + SortingPoint2) / 2 : SortingPoint1;

    private float SortingLineCenterHeight
    {
        get
        {
            if (sortType == SortType.Line)
            {
                return (SortingPoint1.y + SortingPoint2.y) / 2;
            }
            else
            {
                Debug.LogError("calling line center height on point type");
                return SortingPoint1.y;
            }
        }
    }

#if UNITY_EDITOR
    [Button]
    public void SortScene()
    {
        IsoSpriteSorting[] isoSorters = FindObjectsByType<IsoSpriteSorting>(FindObjectsSortMode.None);
        for (int i = 0; i < isoSorters.Length; i++)
        {
            isoSorters[i].Setup();
        }
        IsoSpriteSortingManager.UpdateSorting();
        for (int i = 0; i < isoSorters.Length; i++)
        {
            isoSorters[i].Unregister();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
#endif

    private void Awake()
    {
        t = transform;
    }

    IEnumerator Start()
    {
        if (Application.isPlaying)
        {
            _ = IsoSpriteSortingManager.Instance;
            yield return null;
            Setup();
        }
    }

    public void CheckCacheRefresh()
    {
        if (isMovable && ShouldRefresh())
        {
            RefreshCache();
        }
    }

    private void OnEnable()
    {
        RefreshCache();
    }

    private bool ShouldRefresh()
    {
        if (t.hasChanged)
        {
            return true;
        }
        if (renderersToSortTransforms != null)
        {
            for (int i = 0; i < renderersToSortTransforms.Length; i++)
            {
                Transform rendT = renderersToSortTransforms[i];
                if (rendT != null && rendT.hasChanged)
                {
                    rendT.hasChanged = false;
                    return true;
                }
            }
        }
        return false;
    }

    public void LateUpdateHasChanged()
    {
        if (t.hasChanged && lastRefreshedFrame == Time.frameCount)
        {
            t.hasChanged = false;
        }
    }

    private void Setup()
    {
        t = transform;
        SetupSortableComponents();
        RefreshCache();
        IsoSpriteSortingManager.RegisterSprite(this);
    }

    private void SetupSortableComponents()
    {
        if (renderersToSort == null || renderersToSort.Length == 0)
        {
            renderersToSort = GetComponentsInChildren<Renderer>(true);
        }

        if (canvasesToSort == null || canvasesToSort.Length == 0)
        {
            canvasesToSort = GetComponentsInChildren<Canvas>(true);
        }

        renderersToSortTransforms = new Transform[renderersToSort.Length];
        for (int i = 0; i < renderersToSort.Length; i++)
        {
            renderersToSortTransforms[i] = renderersToSort[i].transform;
        }
    }

    public static int CompairIsoSortersBasic(IsoSpriteSorting sprite1, IsoSpriteSorting sprite2)
    {
        float y1 = sprite1.sortType == SortType.Point ? sprite1.SortingPoint1.y : sprite1.SortingLineCenterHeight;
        float y2 = sprite2.sortType == SortType.Point ? sprite2.SortingPoint1.y : sprite2.SortingLineCenterHeight;
        return y2.CompareTo(y1);
    }

    public static int CompareIsoSorters(IsoSpriteSorting sprite1, IsoSpriteSorting sprite2)
    {
        if (sprite1.sortType == SortType.Point && sprite2.sortType == SortType.Point)
        {
            return sprite2.SortingPoint1.y.CompareTo(sprite1.SortingPoint1.y);
        }
        else if (sprite1.sortType == SortType.Line && sprite2.sortType == SortType.Line)
        {
            return CompareLineAndLine(sprite1, sprite2);
        }
        else if (sprite1.sortType == SortType.Point && sprite2.sortType == SortType.Line)
        {
            return ComparePointAndLine(sprite1.SortingPoint1, sprite2);
        }
        else if (sprite1.sortType == SortType.Line && sprite2.sortType == SortType.Point)
        {
            return -ComparePointAndLine(sprite2.SortingPoint1, sprite1);
        }
        else
        {
            return 0;
        }
    }

    private static int CompareLineAndLine(IsoSpriteSorting line1, IsoSpriteSorting line2)
    {
        Vector2 line1Point1 = line1.SortingPoint1;
        Vector2 line1Point2 = line1.SortingPoint2;
        Vector2 line2Point1 = line2.SortingPoint1;
        Vector2 line2Point2 = line2.SortingPoint2;

        int comp1 = ComparePointAndLine(line1Point1, line2);
        int comp2 = ComparePointAndLine(line1Point2, line2);
        int oneVStwo = int.MinValue;
        if (comp1 == comp2) { oneVStwo = comp1; }

        int comp3 = ComparePointAndLine(line2Point1, line1);
        int comp4 = ComparePointAndLine(line2Point2, line1);
        int twoVSone = int.MinValue;
        if (comp3 == comp4) { twoVSone = -comp3; }

        if (oneVStwo != int.MinValue && twoVSone != int.MinValue)
        {
            if (oneVStwo == twoVSone) { return oneVStwo; }
            return CompareLineCenters(line1, line2);
        }
        else if (oneVStwo != int.MinValue)
        {
            return oneVStwo;
        }
        else if (twoVSone != int.MinValue)
        {
            return twoVSone;
        }
        else
        {
            return CompareLineCenters(line1, line2);
        }
    }

    private static int CompareLineCenters(IsoSpriteSorting line1, IsoSpriteSorting line2)
    {
        return -line1.SortingLineCenterHeight.CompareTo(line2.SortingLineCenterHeight);
    }

    private static int ComparePointAndLine(Vector3 point, IsoSpriteSorting line)
    {
        float pointY = point.y;
        if (pointY > line.SortingPoint1.y && pointY > line.SortingPoint2.y)
        {
            return -1;
        }
        else if (pointY < line.SortingPoint1.y && pointY < line.SortingPoint2.y)
        {
            return 1;
        }
        else
        {
            float slope = (line.SortingPoint2.y - line.SortingPoint1.y) / (line.SortingPoint2.x - line.SortingPoint1.x);
            float intercept = line.SortingPoint1.y - (slope * line.SortingPoint1.x);
            float yOnLineForPoint = (slope * point.x) + intercept;
            return yOnLineForPoint > point.y ? 1 : -1;
        }
    }

    public int RendererSortingOrder
    {
        get
        {
            if (renderersToSort != null && renderersToSort.Length > 0)
            {
                return renderersToSort[0].sortingOrder;
            }
            if (canvasesToSort != null && canvasesToSort.Length > 0)
            {
                return canvasesToSort[0].sortingOrder;
            }
            return 0;
        }
        set
        {
            if (renderersToSort != null)
            {
                for (int i = 0; i < renderersToSort.Length; i++)
                {
                    renderersToSort[i].sortingOrder = (i == 0) ? value : value + 1;
                }
            }
            if (canvasesToSort != null)
            {
                for (int i = 0; i < canvasesToSort.Length; i++)
                {
                    if (canvasesToSort[i] != null)
                    {
                        canvasesToSort[i].overrideSorting = true; // Важно для корректной работы
                        canvasesToSort[i].sortingOrder = value;
                    }
                }
            }
        }
    }

    [NonSerialized] public Bounds2D cachedBounds;

    public void SetRenderBelowAllAndReRegister(bool _renderBelowAll)
    {
        IsoSpriteSortingManager.UnregisterSprite(this);
        renderBelowAll = _renderBelowAll;
        IsoSpriteSortingManager.RegisterSprite(this);
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            Unregister();
        }
    }

    private void Unregister()
    {
        IsoSpriteSortingManager.UnregisterSprite(this);
    }
}