using UnityEngine;
using TMPro;

public class DropDownManager : MonoBehaviour
{
    [SerializeField] protected TMP_Dropdown _drop;

    private void Start()
    {
        _drop.onValueChanged.AddListener(ValueChanged);
    }

    public virtual void ValueChanged(int index)
    {
        string selected = _drop.options[index].text;
    }
}