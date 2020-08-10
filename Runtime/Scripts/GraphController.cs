using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

[System.Serializable]
public class Graph
{
    public string graphTitle, xTitle, yTitle;
    public bool connectPoints;
}

public class GraphController : MonoBehaviour
{
    #region References
    public Transform origin;
    public Transform pointParent;
    public Transform xIncrementParent, yIncrementParent;
    public GameObject inspectorPanel;
    public TextMeshProUGUI inspectorText;
    public TextMeshProUGUI graphTitle, xTitle, yTitle;
    public GameObject pointPrefab;
    public TextMeshProUGUI currentModeText;
    public Canvas canvas;
    public LineRenderer lineRenderer;
    #endregion

    #region Vars
    private static List<List<Vector2>> points = new List<List<Vector2>>();
    public bool logistic = false;
    private int currentGraphNum;
    public Vector2 startValues;
    public Vector2 decimalPlaces;
    public Graph[] graphs;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        points.Clear();
        AddGraphs(graphs.Length);

        for (int i = 0; i < 50; i++)
        {
            if (Random.Range(0, 3f) > 1)
            {
                float x = i;
                float y = i / 50f + Random.Range(-0.2f, 0.2f) + 0.2f;
                y = Mathf.Clamp(y, 0, 1);
                AddPoint(0, x, y);
            }
        }
    }
    #endregion

    #region Public Methods
    public static void AddGraphs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            points.Add(new List<Vector2>());
        }
    }

    public static void AddPoint(int graphNum, float x, float y)
    {
        points[graphNum].Add(new Vector2(x, y));
    }

    public void OpenGraph(int graphNum)
    {
        currentGraphNum = graphNum;
        OpenGraph();
    }

    public void OpenGraph()
    {
        canvas.enabled = true;
        lineRenderer.enabled = true;
        SetGraphTitles();
        SetGraphAxes();
        ClearPoints();
        PlotPoints();
    }

    public void CloseGraph()
    {
        canvas.enabled = false;
        lineRenderer.enabled = false;
    }

    public Vector2 GetDistBetweenTicks()
    {
        Canvas.ForceUpdateCanvases();
        RectTransform xChild0 = xIncrementParent.GetChild(0).GetComponent<RectTransform>();
        RectTransform xChild1 = xIncrementParent.GetChild(1).GetComponent<RectTransform>();
        RectTransform yChild0 = yIncrementParent.GetChild(0).GetComponent<RectTransform>();
        RectTransform yChild1 = yIncrementParent.GetChild(1).GetComponent<RectTransform>();
        float xDist = xChild1.localPosition.x - xChild0.localPosition.x;
        float yDist = yChild0.localPosition.y - yChild1.localPosition.y;
        return new Vector2(xDist, yDist);
    }

    public void DisplayInspectorPoint(Point point)
    {
        inspectorPanel.SetActive(true);
        inspectorPanel.transform.position = point.transform.position;
        inspectorText.text = "(" + point.GetPoint().x.ToString($"F{decimalPlaces.x}") + ", " + point.GetPoint().y.ToString($"F{decimalPlaces.y}") + ")";
    }

    public void ClearInspectorPoint()
    {
        inspectorPanel.SetActive(false);
        inspectorText.text = "";
    }

    public static void ClearPointList()
    {
        points.Clear();
    }

    public void SwitchGraphingMode()
    {
        logistic = !logistic;
        currentModeText.text = "current mode: \n" + (logistic ? "logistic" : "linear");
        SetGraphAxes();
        ClearPoints();
        PlotPoints();
    }

    public void ExportToCSV()
    {
        string filePath = GetPath();

        StreamWriter outStream = File.CreateText(filePath);
        outStream.WriteLine(graphTitle.text);
        outStream.WriteLine("X,Y");
        foreach (Vector2 point in points[currentGraphNum])
        {
            outStream.WriteLine(point.x + "," + point.y);
        }
        outStream.Flush();
        outStream.Close();
    }
    #endregion

    #region Private Methods

    private void SetGraphTitles()
    {
        graphTitle.text = graphs[currentGraphNum].graphTitle;
        xTitle.text = graphs[currentGraphNum].xTitle;
        yTitle.text = graphs[currentGraphNum].yTitle;
    }

    private void ClearPoints()
    {
        foreach (Transform child in pointParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void PlotPoints()
    {
        Vector2 distBetweenTicks = GetDistBetweenTicks();
        Vector2 increments = GetIncrements();
        lineRenderer.positionCount = 0;

        foreach (Vector2 point in points[currentGraphNum])
        {
            print("HM");
            RectTransform pointOnGraph = Instantiate(pointPrefab, pointParent).GetComponent<RectTransform>();

            float xPos = ((point.x - startValues.x) / increments.x) * distBetweenTicks.x;
            float yPos = ((point.y - startValues.y) / (increments.y)) * distBetweenTicks.y;

            if (logistic)
            {
                xPos = ((Mathf.Log10(point.x + 1)) + 1) * distBetweenTicks.x;
                yPos = ((Mathf.Log10(point.y + 1)) + 1) * distBetweenTicks.y;
            }

            float canvasScale = transform.root.localScale.x;
            pointOnGraph.localPosition = new Vector3(xPos, yPos);

            pointOnGraph.GetComponent<Point>().SetupPoint(point, this);

            if (graphs[currentGraphNum].connectPoints)
            {
                lineRenderer.positionCount++;
                Vector3 pos = pointOnGraph.position;
                pos.z -= 1;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, pos);
            }
        }
    }

    /// <summary>Sets the increments for the x and y based on max values</summary>
    private void SetGraphAxes()
    {
        Vector2 increments = GetIncrements();

        if (increments.magnitude == 0)
        {
            increments.x = 10;
            increments.y = 10;
        }

        // x increment texts will be rotated only if the last increment has a length larger than 3. This is why the for loops starts at the end.
        bool rotateXIncrements = false;
        for (int i = xIncrementParent.childCount - 1; i >= 0; i--)
        {
            float num = (startValues.x + increments.x * (i));
            if (logistic)
            {
                num = Mathf.Pow(10, i);
            }

            TextMeshProUGUI text = xIncrementParent.GetChild(i).GetComponent<TextMeshProUGUI>();
            text.text = num.ToString($"F{decimalPlaces.x}");

            // rotate text so that it fits/isn't squished
            text.transform.rotation = Quaternion.Euler(0, 0, 0);
            if (i == xIncrementParent.parent.childCount - 1 && text.text.ToString().Length >= 4)
                rotateXIncrements = true;

            if (rotateXIncrements)
                text.transform.rotation = Quaternion.Euler(0, 0, -30);
        }

        for (int i = 0; i < yIncrementParent.childCount; i++)
        {
            float num = (startValues.y + increments.y * (i));
            if (logistic)
            {
                num = Mathf.Pow(10, i);
            }
            TextMeshProUGUI text = yIncrementParent.GetChild(yIncrementParent.childCount - 1 - i).GetComponent<TextMeshProUGUI>();
            text.text = num.ToString($"F{decimalPlaces.y}");
        }
    }

    private Vector2 GetIncrements()
    {
        // get max x and y values
        float maxX = 0;
        float maxY = 0;
        if (points.Count > 0)
        {
            foreach (Vector2 point in points[currentGraphNum])
            {
                if (point.x > maxX)
                    maxX = point.x;
                if (point.y > maxY)
                    maxY = point.y;
            }
        }


        float GetIncrement(float maxVal)
        {
            int orderOfMagnitude = Mathf.FloorToInt(Mathf.Log10(maxVal));
            // int powerMag = ((int)maxVal).ToString().Length;                   // power of x value (eg. 3 if 500)
            float normalized = maxVal / Mathf.Pow(10, orderOfMagnitude);      // normalizes the float to be in the tenths place (eg. 12 if 1250)
                                                                              // if (normalized >= 25)                                           // further normalizes value to be less than 25
                                                                              // {
                                                                              //     powerMag++;
                                                                              //     normalized /= 10;
                                                                              // }

            // int incrementDegree = 0;
            // if (normalized < 5)
            //     incrementDegree = 5;
            // else if (normalized < 10)
            //     incrementDegree = 10;
            // else if (normalized < 15)
            //     incrementDegree = 15;
            // else if (normalized < 20)
            //     incrementDegree = 20;
            // else if (normalized < 25)
            //     incrementDegree = 25;

            normalized = Mathf.Round(normalized * 10) / 10;
            float incrementDegree = Mathf.CeilToInt(normalized);
            float increment = (incrementDegree * Mathf.Pow(10, orderOfMagnitude - 1));
            return increment;
        }

        float xInc = GetIncrement(maxX - startValues.x);
        float yInc = GetIncrement(maxY - startValues.y);
        if (Mathf.Abs(xInc) == Mathf.Infinity)
        {
            xInc = 0.1f;
        }
        if (Mathf.Abs(yInc) == Mathf.Infinity)
        {
            yInc = 1f;
        }
        // float yInc = 1f;

        // if (xInc < 10)
        //     xInc = 10;
        // if (yInc < 10)
        //     yInc = 10;

        return new Vector2(xInc, yInc);
    }

    private string GetPath()
    {
        string fileName = graphTitle.text + ".csv";
#if UNITY_EDITOR
        return Application.dataPath + "/CSV/" + fileName;
#elif UNITY_ANDROID
        return Application.persistentDataPath + fileName;
#elif UNITY_IPHONE
        return Application.persistentDataPath +"/" + fileName;
#else
        return Application.dataPath + "/" + fileName;
#endif
    }
    #endregion
}
