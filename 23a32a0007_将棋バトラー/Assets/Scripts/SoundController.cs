using System.Collections.Generic;
using UnityEngine;

//BGM&SEêßå‰
public class SoundController : MonoBehaviour
{
    [SerializeField]
    List<AudioClip> bgm, se;

    AudioSource audioSource;

    //BGMçƒê∂
    public void PlayBGM()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = bgm[0];
        audioSource.Play();
    }

    //SEçƒê∂
    public void PlaySE(int num)
    {
        audioSource.PlayOneShot(se[num]);
    }
}
