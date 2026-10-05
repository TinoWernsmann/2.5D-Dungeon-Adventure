using TMPro;
using UnityEngine;

public class NerveUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _counter;

    public void UpdateCounter(int currentCut, int maxCut)
    {
        _counter.text = currentCut + "/" + maxCut;
    }
}
