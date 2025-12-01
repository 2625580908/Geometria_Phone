using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NCalc; 

public class GraphPlotter : MonoBehaviour
{
    [System.Serializable]
    public class GraphFunction
    {
        public int index;
        public bool visible = true; 
        [Tooltip("Enter 'Sin(x) * z' or 'Pow(x, 2) + Pow(z, 2)'.")]
        public string equation = "Cos(x) * Sin(z)";
        public Color color = new Color(0f, 0.8f, 1f, 0.8f);

        [HideInInspector] public GameObject obj;
        [HideInInspector] public string lastEquation;
        [HideInInspector] public Color lastColor;
        [HideInInspector] public bool isParametric;
    }

    [Header("Resources")]
    public Material surfaceMaterial; 
    public Material lineMaterial;    

    [Header("Graph List")]
    public List<GraphFunction> functions = new List<GraphFunction>();

    [Header("Global Settings")]
    [Range(10, 254)]
    public int resolution = 100;

    [Range(1, 50)]
    public float range = 10f;

    [Header("Animation")]
    public bool animate = false;
    public float animationSpeed = 1.0f;

    [Header("Line Settings (For Curves)")]
    public float lineWidth = 0.1f;

    private int lastResolution;
    private float lastRange;
    private float currentTime;
    private float lastLineWidth; // Track line width changes

    void Start()
    {
        if (functions.Count == 0)
        {
            functions.Add(new GraphFunction { equation = "(x/2)^2-(z/2)^2", color = new Color(0, 0.8f, 1, 0.8f), index = 0, visible = true });
            functions.Add(new GraphFunction { equation = "t*sin(t*5);t;t*cos(t*5)", color = new Color(1, 0.5f, 0, 1f), index = 1, visible = true });
        }
        UpdateGraphs(true);
    }

    void Update()
    {
        bool globalChanged = (resolution != lastResolution || range != lastRange || lineWidth != lastLineWidth);

        if (animate)
        {
            currentTime += Time.deltaTime * animationSpeed;
            globalChanged = true; 
        }

        UpdateGraphs(globalChanged);

        lastResolution = resolution;
        lastRange = range;
        lastLineWidth = lineWidth;
    }

    void UpdateGraphs(bool forceUpdateAll)
    {
        // Cleanup logic
        HashSet<GameObject> validObjects = new HashSet<GameObject>();
        foreach(var f in functions) {
            if(f.obj != null) validObjects.Add(f.obj);
        }

        List<GameObject> toDestroy = new List<GameObject>();
        foreach(Transform child in transform) {
            if (child.name.StartsWith("Graph_") && !validObjects.Contains(child.gameObject)) {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach(var deadObj in toDestroy) {
            if (Application.isPlaying) Destroy(deadObj);
            else DestroyImmediate(deadObj); 
        }

        for (int i = 0; i < functions.Count; i++)
        {
            GraphFunction func = functions[i];
            func.index = i; 

            if (func.obj == null)
            {
                func.obj = new GameObject("Graph_" + func.index);
                func.obj.transform.SetParent(this.transform, false);
                func.obj.AddComponent<MeshFilter>();
                func.obj.AddComponent<MeshRenderer>();

                LineRenderer lr = func.obj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false; // Important for parenting
                
                forceUpdateAll = true; 
            }

            func.obj.name = $"Graph_{func.index}";

            if (func.obj.activeSelf != func.visible)
                func.obj.SetActive(func.visible);

            if (!func.visible) continue; 

            // Detect equation change to determine type
            bool equationChanged = func.equation != func.lastEquation;
            bool localChanged = equationChanged || func.color != func.lastColor;

            if (forceUpdateAll || localChanged)
            {
                // CRITICAL FIX 1: Determine type BEFORE updating material
                func.isParametric = func.equation.Contains(";");

                UpdateMaterial(func);

                if (forceUpdateAll || equationChanged)
                {
                    if (func.isParametric) GenerateParametricCurve(func);
                    else GenerateSurface(func);
                }

                func.lastEquation = func.equation;
                func.lastColor = func.color;
            }
        }
    }

    void UpdateMaterial(GraphFunction func)
    {
        MeshRenderer mr = func.obj.GetComponent<MeshRenderer>();
        LineRenderer lr = func.obj.GetComponent<LineRenderer>();

        // Ensure LineRenderer is local space for issue #2
        if(lr != null) lr.useWorldSpace = false;

        if (func.isParametric)
        {
            mr.enabled = false;
            lr.enabled = true;

            if (lineMaterial != null)
            {
                if (lr.sharedMaterial == null || lr.sharedMaterial.name.StartsWith("Default") || lr.sharedMaterial == lineMaterial)
                {
                    lr.material = new Material(lineMaterial);
                    lr.material.name = $"LineInstance_{func.index}";
                }

                Material instMat = lr.material;
                if (instMat.HasProperty("_BaseColor")) instMat.SetColor("_BaseColor", func.color);
                else if (instMat.HasProperty("_Color")) instMat.SetColor("_Color", func.color);
            }
            else
            {
                if (lr.material == null || lr.material.name.StartsWith("Default")) 
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.material.color = func.color;
            }
        }
        else
        {
            lr.enabled = false;
            mr.enabled = true;

            if (surfaceMaterial != null)
            {
                if (mr.sharedMaterial == null || mr.sharedMaterial.name.StartsWith("Default") || mr.sharedMaterial == surfaceMaterial)
                {
                    mr.material = new Material(surfaceMaterial);
                    mr.material.name = $"SurfaceInstance_{func.index}";
                }
            }
            else
            {
                if (mr.sharedMaterial == null || mr.sharedMaterial.name.StartsWith("Default"))
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
                    if (shader == null) shader = Shader.Find("Particles/Standard Surface");
                    if (shader == null) shader = Shader.Find("Standard"); 

                    Material mat = new Material(shader);
                    mat.name = "GraphMat_Particles_" + func.index;

                    mat.SetFloat("_Surface", 1.0f); 
                    mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); 
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);

                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                    mr.sharedMaterial = mat;
                }
            }

            Material instMat = mr.material;
            if (instMat.HasProperty("_BaseColor")) instMat.SetColor("_BaseColor", func.color);
            else if (instMat.HasProperty("_Color")) instMat.SetColor("_Color", func.color);
        }
    }

    void GenerateSurface(GraphFunction func)
    {
        MeshFilter mf = func.obj.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "Graph Mesh " + func.index;

        if (SystemInfo.supports32bitsIndexBuffer)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        else
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            if (resolution > 254) resolution = 254;
        }

        int vertsPerSide = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerSide * vertsPerSide];
        Color[] colors = new Color[vertices.Length]; 
        List<int> triangles = new List<int>(); 
        Vector2[] uvs = new Vector2[vertices.Length];

        float step = (range * 2f) / resolution;
        string processedEq = PreProcessEquation(func.equation);

        float fadeStart = range * 0.9f;

        try 
        {
            Expression e = new Expression(processedEq);
            e.Parameters["pi"] = Mathf.PI;
            e.Parameters["e"] = System.Math.E;
            e.Parameters["t"] = currentTime;
            e.Parameters["time"] = currentTime;

            // A. Generate Vertices
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float xPos = -range + (x * step);
                    float zPos = -range + (z * step); 

                    e.Parameters["x"] = xPos;
                    e.Parameters["z"] = zPos;

                    float yPos = 0f;
                    try 
                    {
                        object result = e.Evaluate();
                        float rawY = (float)System.Convert.ToDouble(result);

                        if (float.IsNaN(rawY) || float.IsInfinity(rawY)) yPos = 0f;
                        else yPos = Mathf.Clamp(rawY, -range, range);
                    }
                    catch { yPos = 0f; }

                    int i = x + z * vertsPerSide;
                    vertices[i] = new Vector3(xPos, yPos, zPos);
                    uvs[i] = new Vector2((float)x / resolution, (float)z / resolution);

                    float dist = Mathf.Max(Mathf.Abs(xPos), Mathf.Abs(zPos), Mathf.Abs(yPos));
                    float alpha = 1f; 
                    if (dist > fadeStart)
                        alpha = 1f - Mathf.Clamp01((dist - fadeStart) / (range - fadeStart));

                    colors[i] = new Color(1f, 1f, 1f, alpha);
                }
            }

            // B. Generate Triangles
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = x + z * vertsPerSide;
                    int a = i, b = i + vertsPerSide, c = i + 1, d = i + vertsPerSide + 1;

                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(c); triangles.Add(b); triangles.Add(d);
                }
            }

            mesh.vertices = vertices;
            mesh.colors = colors; 
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs;
            mesh.RecalculateNormals(); 
            mesh.RecalculateBounds();
            mf.mesh = mesh;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Graph Error ({func.index}): {ex.Message}");
        }
    }

    void GenerateParametricCurve(GraphFunction func)
    {
        LineRenderer lr = func.obj.GetComponent<LineRenderer>();
        lr.useWorldSpace = false; // Ensure it stays attached to parent

        string[] parts = func.equation.Split(';');
        if (parts.Length != 3) return;

        string eqX = PreProcessEquation(parts[0]);
        string eqY = PreProcessEquation(parts[1]);
        string eqZ = PreProcessEquation(parts[2]);

        int segments = resolution * 10; 

        // CRITICAL FIX 2: Apply lineWidth settings
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        float tMin = -range; 
        float tMax = range; 

        try 
        {
            Expression ex = new Expression(eqX);
            Expression ey = new Expression(eqY);
            Expression ez = new Expression(eqZ);

            ex.Parameters["pi"] = Mathf.PI; ey.Parameters["pi"] = Mathf.PI; ez.Parameters["pi"] = Mathf.PI;
            ex.Parameters["e"] = System.Math.E; ey.Parameters["e"] = System.Math.E; ez.Parameters["e"] = System.Math.E;

            List<List<Vector3>> allSegments = new List<List<Vector3>>();
            List<Vector3> currentSegment = new List<Vector3>();

            Vector3 prevPt = Vector3.zero;
            bool prevInside = false;

            for (int i = 0; i <= segments; i++)
            {
                float t = Mathf.Lerp(tMin, tMax, (float)i / segments);
                float evalT = t + (animate ? currentTime : 0);

                ex.Parameters["t"] = evalT; ey.Parameters["t"] = evalT; ez.Parameters["t"] = evalT;

                float valX = (float)System.Convert.ToDouble(ex.Evaluate());
                float valY = (float)System.Convert.ToDouble(ey.Evaluate());
                float valZ = (float)System.Convert.ToDouble(ez.Evaluate());

                Vector3 currPt = new Vector3(valX, valY, valZ);

                bool currInside = Mathf.Abs(currPt.x) <= range && 
                                  Mathf.Abs(currPt.y) <= range && 
                                  Mathf.Abs(currPt.z) <= range;

                if (i == 0)
                {
                    if (currInside) currentSegment.Add(currPt);
                }
                else
                {
                    if (prevInside && currInside)
                    {
                        currentSegment.Add(currPt);
                    }
                    else if (prevInside && !currInside)
                    {
                        Vector3 intersection = GetIntersection(prevPt, currPt, range);
                        currentSegment.Add(intersection);

                        allSegments.Add(new List<Vector3>(currentSegment));
                        currentSegment.Clear();
                    }
                    else if (!prevInside && currInside)
                    {
                        Vector3 intersection = GetIntersection(currPt, prevPt, range); 
                        currentSegment.Add(intersection);
                        currentSegment.Add(currPt);
                    }
                }

                prevPt = currPt;
                prevInside = currInside;
            }

            if (currentSegment.Count > 0) allSegments.Add(currentSegment);

            if (allSegments.Count > 0)
            {
                List<Vector3> bestSegment = allSegments[0];
                for(int k=1; k<allSegments.Count; k++) 
                    if(allSegments[k].Count > bestSegment.Count) bestSegment = allSegments[k];

                lr.positionCount = bestSegment.Count;
                lr.SetPositions(bestSegment.ToArray());
            }
            else
            {
                lr.positionCount = 0;
            }
        }
        catch { }
    }

    Vector3 GetIntersection(Vector3 inside, Vector3 outside, float r)
    {
        Vector3 dir = outside - inside;
        float tMin = 1.0f;

        if (outside.x > r) tMin = Mathf.Min(tMin, (r - inside.x) / dir.x);
        else if (outside.x < -r) tMin = Mathf.Min(tMin, (-r - inside.x) / dir.x);

        if (outside.y > r) tMin = Mathf.Min(tMin, (r - inside.y) / dir.y);
        else if (outside.y < -r) tMin = Mathf.Min(tMin, (-r - inside.y) / dir.y);

        if (outside.z > r) tMin = Mathf.Min(tMin, (r - inside.z) / dir.z);
        else if (outside.z < -r) tMin = Mathf.Min(tMin, (-r - inside.z) / dir.z);

        return inside + dir * tMin;
    }

    string PreProcessEquation(string eq)
    {
        if (string.IsNullOrEmpty(eq)) return "0";
        eq = eq.ToLower();
        eq = eq.Replace(" ", ""); 
        eq = eq.Replace("π", "pi");

        string powPattern = @"((?:[a-z0-9\.]+\s*)?\(.*?\)|[a-z0-9\.]+)\^((?:[a-z0-9\.]+\s*)?\(.*?\)|[a-z0-9\.]+)";
        int safety = 0;
        while (Regex.IsMatch(eq, powPattern) && safety++ < 5)
        {
            eq = Regex.Replace(eq, powPattern, "Pow($1, $2)");
        }

        eq = Regex.Replace(eq, @"(?<=\d)(?=[a-z\(])", "*");
        eq = Regex.Replace(eq, @"(?<=\))(?=[\d a-z\(])", "*");

        string[] funcs = new string[] { "sin", "cos", "tan", "asin", "acos", "atan", "sqrt", "abs", "pow", "exp", "log", "floor", "ceiling" };
        foreach (string f in funcs)
        {
            eq = eq.Replace(f + "(", char.ToUpper(f[0]) + f.Substring(1) + "(");
        }

        return eq;
    }

    float EvaluateMath(string processedFormula, float x, float z, float t)
    {
        try
        {
            Expression e = new Expression(processedFormula);
            e.Parameters["x"] = x;
            e.Parameters["z"] = z;
            e.Parameters["t"] = t;
            e.Parameters["pi"] = Mathf.PI;
            e.Parameters["e"] = System.Math.E;
            return (float)System.Convert.ToDouble(e.Evaluate());
        }
        catch { return 0f; }
    }
}