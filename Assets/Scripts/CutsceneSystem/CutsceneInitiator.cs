using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This Cutscene Initiator is used for when Cutscenes have their own Unity scenes (like beginning and end cutscene)
/// It automatically starts a given cutscene and loads the next given scene
/// Still needs a CutsceneManager which coordinates the playing of the cutscene!
/// </summary>
public class CutsceneInitiator : MonoBehaviour
{
    [SerializeField] private CutsceneSO _cutsceneToBegin;
    [SerializeField] private CutsceneManager _cutsceneManager;
    [SerializeField] private string NEXT_SCENE_TO_LOAD;

    private void Start()
    {
        StartCoroutine(PlayCutsceneThenLoadScene());
    }

    private IEnumerator PlayCutsceneThenLoadScene()
    {
        bool finished = false;
        void OnFinished() => finished = true;

        _cutsceneManager.OnCutsceneFinished += OnFinished;
        _cutsceneManager.StartNewCutscene(_cutsceneToBegin);

        yield return new WaitUntil(() => finished);

        _cutsceneManager.OnCutsceneFinished -= OnFinished;
        SceneManager.LoadScene(NEXT_SCENE_TO_LOAD);
    }
}
