using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LetterScript : MonoBehaviour
{
    private Color start_col;

    private float sin_timer = 0;
    private float sin_amplitude = 2;
    private float sin_frequency = 10;

    private float gravity_timer = 0;
    private float gravity_delay = 1.0f;
    private float gravity_acceleration = -98.1f;
    private Vector3 vel;

    private float shake_time_between = 0.25f;
    private float shake_timer = 0;
    private float shake_speed = 0.25f;
    private float shake_amplitude = 5;
    private Vector3 current_shake;

    public void EndOfLife() { end_life = true; start_col = this.GetComponent<RawImage>().color;}
    private bool end_life = false;
    private float end_timer = 0;
    private float end_duration = 0.5f;

    public void SetAnimationStyle(AnimationStyle n_style) { anim_style = n_style;}
    private AnimationStyle anim_style;

    // Start is called before the first frame update
    void Start()
    {
        shake_timer = shake_time_between;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.localPosition = this.transform.localPosition + vel * Time.deltaTime;
        if (end_life)
        {
            end_timer += Time.deltaTime;
            if (end_timer >= end_duration) { Destroy(this.transform.parent.gameObject); }
            this.GetComponent<RawImage>().color = new Color(start_col.r, start_col.g, start_col.b, start_col.a * ((end_duration - end_timer)/end_duration));
        }

        switch(anim_style) 
        {
            case AnimationStyle.None:
                break;
            case AnimationStyle.Gravity:
                gravity_timer += Time.deltaTime;
                if (gravity_timer >= gravity_delay) { vel += new Vector3(0, gravity_acceleration * Time.deltaTime, 0); }
                break;
            case AnimationStyle.Wiggle:
                sin_timer += Time.deltaTime;
                this.transform.localPosition = new Vector3(Mathf.Cos(sin_timer * sin_frequency), Mathf.Sin(sin_timer * sin_frequency) * sin_amplitude, 0);
                break;
            case AnimationStyle.Sin_Wave:
                sin_timer += Time.deltaTime;
                this.transform.localPosition = new Vector3(0,Mathf.Sin(sin_timer * sin_frequency) * sin_amplitude,0);
                break;
            case AnimationStyle.Shaking:
                shake_timer += Time.deltaTime;
                if (shake_timer >= shake_time_between) { GenerateNewShake(); }
                this.transform.localPosition += (current_shake - this.transform.localPosition) * shake_speed;
                break;
        }
    }

    void GenerateNewShake()
    {
        int rand_dir = Random.Range(0, 360);
        current_shake = new Vector3(Mathf.Cos(rand_dir),Mathf.Sin(rand_dir),0) * shake_amplitude;
    }
}
