using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Equation : MonoBehaviour
{
    public int index = 0;
    
    public List<Sprite> activateSprites;
    
    public Button colorbtn;
    private bool isActive = true;
    public Button activatebtn;
    
    public Button delbtn;
    
    public InputField formulaInputField;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MainManager.Instance.mathematicalFunctions[index].formula = formulaInputField.text;
        
        colorbtn.onClick.AddListener(()=>
        {
            MainManager.Instance.newColorimg = colorbtn.transform.GetChild(0).GetComponent<Image>();
            MainManager.Instance.newIndex = index;
            MainManager.Instance.SetColorPicker(true);
        });
        
    
        activatebtn.onClick.AddListener(() =>
        {
            isActive = !isActive;
            MainManager.Instance.mathematicalFunctions[index].activate = isActive;
            activatebtn.GetComponent<Image>().sprite = isActive ? activateSprites[1] : activateSprites[0];
            LinkManager.Instance.SendRokidDevices();
        });
        
        formulaInputField.onValueChanged.AddListener((val)=>
        {
            MainManager.Instance.mathematicalFunctions[index].formula = val;
        });
        
        delbtn.onClick.AddListener(()=>
        {
            MainManager.Instance.EquationRemoveAt(index);
            LinkManager.Instance.SendRokidDevices();
            Destroy(gameObject);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
