
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HerzRaumTrigger : MonoBehaviour
{
    [SerializeField] private string SCENE_TO_LOAD;

    private const float WAIT_TIMER = 5.5f;
    private bool _hasBeenTriggered = false;

    public void TriggerEnding()
    {
        if (_hasBeenTriggered) return;

        _hasBeenTriggered = true;
        StartCoroutine(WaitUntilGameOver());
    }

    private IEnumerator WaitUntilGameOver()
    {
        yield return new WaitForSeconds(WAIT_TIMER);

        SceneManager.LoadScene(SCENE_TO_LOAD);
    }
}
