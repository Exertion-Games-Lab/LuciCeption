using UnityEngine;

namespace LuciReality.Audio
{
  public class AudioSingleton : MonoBehaviour
  {
    public static AudioSingleton Instance;
    public AudioSource AudioSource;

    public void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Destroy(this);
      }


      AudioSource = GetComponent<AudioSource>();

    }

  }
}