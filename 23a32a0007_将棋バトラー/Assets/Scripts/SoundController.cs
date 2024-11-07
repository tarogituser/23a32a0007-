using System.Collections.Generic;
using UnityEngine;

//BGM&SEêßå‰
public class SoundController : MonoBehaviour
{
    [SerializeField]
    List<AudioClip> bgm, se;

    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    //BGMçƒê∂
    public void PlayBGM()
    {
        audioSource.clip = bgm[0];
        audioSource.Play();
    }

    //SEçƒê∂
    public void PlaySE(int num)
    {
        audioSource.PlayOneShot(se[num]);
    }
}
