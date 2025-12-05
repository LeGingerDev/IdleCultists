using System.Collections;
using LGD.Utilities.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class DelayStart : MonoBehaviour
{
    [MinMaxSlider(0f, 5f)]
    public Vector2 delayTime = new Vector2(0.5f, 1.5f);

    public GameObject toToggleOn;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delayTime.GetRandom());
        toToggleOn.SetActive(true);
    }
}
