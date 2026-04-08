using UnityEngine;

namespace Assets.Scripts.Views
{
    public sealed class SolitaireAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        [Header("Clips")]
        [SerializeField] private AudioClip takeCardClip;
        [SerializeField] private AudioClip bankClip;

        [Header("Volumes")]
        [Range(0f, 1f)][SerializeField] private float takeCardVolume = 1f;
        [Range(0f, 1f)][SerializeField] private float bankVolume = 1f;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, volume);
        }

        public void PlayTakeCard()
        {
            PlayOneShot(takeCardClip, takeCardVolume);
        }

        public void PlayBank()
        {
            PlayOneShot(bankClip, bankVolume);
        }

        public void StopAll()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }
}