using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapLoader : MonoBehaviour {

    #region Loading Screen

    #region Inspector Data

    public GameObject loadScreen;
    public Image progressBar;

    #endregion

    private int progress;
    public enum Progress {
        Waiting,
        Initializing = 5,
        Convert_Rooms = 10,
        Set_Seed = 16,
        Creating_Main_Path = 25,
        Branching_Main_Path = 40,
        Placing_Rooms = 100,
    }

    public void SetProgress(Progress newProgress)
    {
        UpdateLoadingBar((int)newProgress - progress);
    }

    public void UpdateLoadingBar(int adding)
    {
        if (updateLoading != null)
            StopCoroutine(updateLoading);
        updateLoading = StartCoroutine(_UpdateLoadingBar(adding));
    }

    public float loadbarFillspeed = 0.01f;
    private Coroutine updateLoading;
    private IEnumerator _UpdateLoadingBar(int adding)
    {
        float i = 0;
        float other = (float)adding / 100;
        progress += adding;
        
        while (i < other)
        {
            i += Time.deltaTime;
            progressBar.fillAmount += i;
            yield return new WaitForSeconds(loadbarFillspeed);
        }

        if(progress == 100)
        {
            //remove loading screen
            loadScreen.SetActive(false);
        }
    }

    #endregion
}
