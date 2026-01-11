using System.Collections.Generic;
using System.Net;
using GuanYao.Tool.Network;
using GuanYao.Tool.Singleton;
using GuanYao.Tool.SpatialDrawing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vuplex.WebView;

public class MainManager : SingletonMono<MainManager>
{
    public GraphPlotter graphPlotter;
    public GameObject MenuUi;
    public GameObject ColorPicker;
    public Button closebtn;
    
    public Image newColorimg;
    public int newIndex;
     
    public ColorPicker colorPicker;

    public GameObject ParentEquation;
    public GameObject PrefabsEquation;

    public Button Add;

    public Button Sync;

    public Button Preview;
    
    public Button WebView;
    
    public Button WebViewClose;
    
    public GameObject WebViewPrefabs;

    public CanvasWebViewPrefab canvasWebViewPrefab;
    [Header("函数数据")]
    public List<MathematicalFunction> mathematicalFunctions;
    public List<GameObject> mathematicalObjList;
    
    public int index = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mathematicalFunctions = new List<MathematicalFunction>();
        mathematicalObjList = new List<GameObject>();
        AddData("(x/2)^2-(z/2)^2");
        AddData("t*sin(t*5);t;t*cos(t*5)");
        
        closebtn.onClick.AddListener(()=>
        {
            ColorPicker.SetActive(false);
        });
        
        Add.onClick.AddListener(()=>
        {
           GameObject item = Instantiate(PrefabsEquation, ParentEquation.transform);
           MathematicalFunction mathematicalFunction = new MathematicalFunction();
           mathematicalFunction.index = index;
           mathematicalFunction.activate = true;
           mathematicalFunctions.Add(mathematicalFunction);
           item.GetComponent<Equation>().index = index;
           mathematicalObjList.Add(item);
           index++;
        });
        
        Preview.onClick.AddListener(() =>
        {
            SetPreviewUIActivate(true);
        });

        WebView.onClick.AddListener(() =>
        {
            MenuUi.SetActive(false);
            WebViewPrefabs.SetActive(true);
            WebViewClose.gameObject.SetActive(true);
        });


        WebViewClose.onClick.AddListener(() =>
        {
            Debug.Log("WebViewClose");
            MenuUi.SetActive(true);
            WebViewPrefabs.SetActive(false);
            WebViewClose.gameObject.SetActive(false);
        });
    }
   
    public MathematicalData message;

    public void AddData(string formula)
    {
        GameObject item = Instantiate(PrefabsEquation, ParentEquation.transform);
        item.GetComponentInChildren<InputField>().text = formula;
        MathematicalFunction mathematicalFunction = new MathematicalFunction();
        mathematicalFunction.index = index;
        mathematicalFunction.activate = true;
        mathematicalFunction.color = new RGBA(){r=0.24f,g=1,b=0,a =1};
        mathematicalFunction.formula = formula;
        mathematicalFunctions.Add(mathematicalFunction);
        item.GetComponent<Equation>().index = index;
        mathematicalObjList.Add(item);
        index++;
    }
    public void SetColorPicker(bool isbool)
    {
        ColorPicker.SetActive(isbool);
    }

    public void EquationRemoveAt(int RemoveAtindex)
    {
        mathematicalFunctions.RemoveAt(RemoveAtindex);
        mathematicalObjList.RemoveAt(RemoveAtindex);
        for (int i = 0; i < mathematicalFunctions.Count; i++)
        {
            mathematicalFunctions[i].index = i;
            mathematicalObjList[i].GetComponent<Equation>().index = i;
        }
        index--;
    }
    
    public void SetColorData(Color color)
    {
        if (newColorimg != null)
        {
            RGBA rgba = new RGBA();
            rgba.r = color.r;
            rgba.g = color.g;
            rgba.b = color.b;
            rgba.a = color.a;
            mathematicalFunctions[newIndex].color = rgba;
            newColorimg.color = color;
        }
        LinkManager.Instance.SendRokidDevices();
    }





    public GameObject PreviewUI;
    
    public void SetPreviewUIActivate(bool isActive)
    {
        if (mathematicalFunctions != null)
        {
            graphPlotter.functions.Clear();
            foreach (MathematicalFunction item in mathematicalFunctions)
            {
                GraphPlotter.GraphFunction graphFunction = new GraphPlotter.GraphFunction();
                graphFunction.index = item.index;
                graphFunction.visible = item.activate;
                graphFunction.equation = item.formula;
                if (item.color != null)
                    graphFunction.color = new Color(item.color.r, 
                        item.color.g, item.color.b, item.color.a);
                else
                    graphFunction.color = Color.green;
                graphPlotter.functions.Add(graphFunction);
            }
        }
        MenuUi.SetActive(!isActive);
        PreviewUI.SetActive(isActive);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
