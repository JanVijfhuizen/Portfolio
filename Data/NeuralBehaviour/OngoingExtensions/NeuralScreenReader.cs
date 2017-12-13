using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NeuralScreenReader : NeuralBehaviour
{
    [SerializeField]
    private bool visualizeScreenReading;

    protected bool Ready
    {
        get
        {
            return convScreenGreyValues.Count > 0;
        }
    }

    [SerializeField]
    private int convScreenX = 16, convScreenY = 9; //my personal aspect ratio, imput count is 16*9
    [SerializeField]
    private float refreshTimeScreenReader = 1;

    [SerializeField]
    private Color screenReadColor;
    GUIStyle style = new GUIStyle();
    protected virtual void Awake()
    {
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 9;
        style.normal.textColor = screenReadColor;

        StartCoroutine(UpdateScreenData());
        screen = new Texture2D(Screen.width, Screen.height);
    }

    protected override List<float> GetInput(bool isTraining)
    {
        return convScreenGreyValues;
    }

    protected virtual void OnPostRender()
    {
        if (!readyToCapture)
            return;
        screen.ReadPixels(new Rect(0, 0, ScreenX, ScreenY), 0, 0);
        screen.Apply();

        convScreenGreyValues.Clear();       
        convScreen = screen.GetPixels();

        int calcX = Mathf.FloorToInt((float)ScreenX / convScreenX);
        int calcY = Mathf.FloorToInt((float)ScreenY / convScreenY);

        float index;
        for (float height = 0.5f; height < convScreenY; height++)
            for (float width = 0.5f; width < convScreenX; width++)
            {
                index = calcY * height * ScreenX + calcX * width;
                convScreenGreyValues.Add(convScreen[(int)index].grayscale);
            }

        readyToCapture = false;
    }

    protected virtual void OnGUI()
    {
        if (!Ready || !visualizeScreenReading)
            return;
        calcX = Mathf.Floor((float)ScreenX / convScreenX);
        calcY = Mathf.Floor((float)ScreenY / convScreenY);
        float calc;

        for (int y = 0; y < convScreenY; y++)
            for (int x = 0; x < convScreenX; x++)
            {
                calc = convScreenGreyValues[x + y * convScreenX];
                GUI.Box(new Rect(calcX * x, calcY * (convScreenY - 1 - y), calcX, calcY), 
                    RoundGreyValue(calc).ToString(), style);
            }
    }

    private float RoundGreyValue(float value)
    {
        float ret = Mathf.Round(value * 100);
        return ret / 100;
    }

    private bool readyToCapture = false;
    private int ScreenX
    {
        get
        {
            return screen.width;
        }
    }
    private int ScreenY
    {
        get
        {
            return screen.height;
        }
    }

    private Texture2D screen;
    private List<float> convScreenGreyValues = new List<float>();
    private Color[] convScreen;
    private float calcX, calcY;
    private IEnumerator UpdateScreenData()
    {
        while (true)
        {
            readyToCapture = true;
            yield return new WaitForSeconds(refreshTimeScreenReader);
        }
    }

    protected override abstract IEnumerator Rate(NeuralNetwork net);
}
