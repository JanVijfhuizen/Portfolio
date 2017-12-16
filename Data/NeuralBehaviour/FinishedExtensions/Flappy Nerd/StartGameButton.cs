using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;
using Jext;
using UnityEngine.UI;

public class StartGameButton : MonoBehaviour {

    [SerializeField]
    private Image fadeImage;
    [SerializeField]
    private float fadeSpeed = 3;

	public void StartGame()
    {
        StartCoroutine(Load("FlappyNetwork"));
    }

    private IEnumerator Load(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(
            Methods.FadeToBlack(fadeImage, fadeSpeed, Methods.FadeType.FadeOut));
        SceneManager.LoadScene("FlappyNetwork", LoadSceneMode.Single);
    }
}
