using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StepSlider : MonoBehaviour
{
    [SerializeField] private Image sliderStep = null;
    [SerializeField] private Image[] sliderSteps = null;

    [SerializeField] private Color normalColor = new Color(1, 1, 1);
    [SerializeField] private Color activeColor = new Color(198f / 255f, 1, 83 / 255f);

    public void OnChange(float v) {
        Slider slider = GetComponent<Slider>();
        for (int i = (int)v + 1; i <= slider.maxValue; i++) {
            sliderSteps[i].color = normalColor;
        }
        sliderSteps[(int)v].color = activeColor;
    }


    void Start()
    {
        Slider slider = GetComponent<Slider>();
        Rect rect = slider.GetComponent<RectTransform>().rect;

        sliderSteps = new Image[(int) slider.maxValue + 1];
        float width = rect.width - 20;
        for (int i = 0; i <= slider.maxValue; i++) {
            var img = Instantiate(sliderStep);
            img.transform.SetParent(transform, false);
            img.transform.localPosition = new Vector3(width / slider.maxValue * i - width / 2, 0, 0);
            img.color = i == 0 ? activeColor : normalColor;

            sliderSteps[i] = img;
        }
    }
}
