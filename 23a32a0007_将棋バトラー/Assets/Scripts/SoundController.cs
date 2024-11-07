using System.Collections.Generic;
using UnityEngine;

//BGM&SE����
public class SoundController : MonoBehaviour
{
    [SerializeField]
    List<AudioClip> bgm, se;

    AudioSource audioSource;

    //BGM�Đ�
    public void PlayBGM()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = bgm[0];
        audioSource.Play();
    }

    //SE�Đ�
    public void PlaySE(int num)
    {
        audioSource.PlayOneShot(se[num]);
    }
}
