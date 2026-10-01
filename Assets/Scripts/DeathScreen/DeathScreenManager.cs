using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreenManager : MonoBehaviour
{
    [SerializeField] private string _sceneToLoad;
    [Min(1)]
    [SerializeField] private float TIME_TO_WAIT;

    private void Start()
    {
        StartCoroutine(WaitOnDeathScreenThenLoadNextScene());
    }

    private IEnumerator WaitOnDeathScreenThenLoadNextScene()
    {
        yield return new WaitForSeconds(TIME_TO_WAIT);

        SceneManager.LoadScene(_sceneToLoad);
    }
}
