using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;

public class FPSDropDown : DropDownManager
{
    public override void ValueChanged(int index)
    {
        base.ValueChanged(index);
        string selected = _drop.options[index].text;
        Match match = Regex.Match(selected, @"\d+");
        if (match.Success)
        {
            int fps = int.Parse(match.Value);
            Settings.Instance.ChangeFPSLock(fps);
        }
        else
        {
            Settings.Instance.UnlockFPS();
        }
    }
}
