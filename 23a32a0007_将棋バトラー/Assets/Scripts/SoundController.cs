using System.Collections.Generic;
using UnityEngine;

//BGM&SE制御
public class SoundController : MonoBehaviour
{
    [SerializeField]
    List<AudioClip> bgm, se;

    AudioSource audioSource;

    //BGM再生
    public void PlayBGM()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = bgm[0];
        audioSource.Play();
    }

    //SE再生
    public void PlaySE(int num)
    {
        audioSource.PlayOneShot(se[num]);
    }
}
