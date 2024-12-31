using UnityEngine;
using UnityEngine.Audio;

namespace MetaJungle.Utilities
{
    public static class AudioUtility
    {
        /// <summary>
        /// Reproduce un Audiclip en una posicion dada en el world space
        /// </summary>
        /// <param name="clip">Audio a reproducir</param>
        /// <param name="position">Posicion en world space desde donde se originara el audio</param>
        /// <param name="volume">Volumen del sonido</param>
        /// <param name="gameObjectName">Nombre del GameObject que tendra el audio</param>
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, [UnityEngine.Internal.DefaultValue("1.0F")] float volume, string gameObjectName = "One shot audio")
        {
            GameObject gameObject = new GameObject(gameObjectName);
            gameObject.transform.position = position;
            AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
            audioSource.clip = clip;
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;
            audioSource.Play();
            Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
        }

        /// <summary>
        /// SobreCarga del metodo PlayClipAtPoint para usar un Audiomixer
        /// </summary>
        /// <param name="clip">Audio a reproducir</param>
        /// <param name="position">Posicion en world space desde donde se originara el audio</param>
        /// <param name="volume">Volumen del sonido</param>
        /// <param name="audioMixer">El audio mixer donde se encuentra el grupo al que se asignara el audio source</param>
        /// <param name="gameObjectName">Nombre del GameObject que tendra el audio</param>
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, [UnityEngine.Internal.DefaultValue("1.0F")] float volume, AudioMixer audioMixer,string gameObjectName = "One shot audio")
        {
            GameObject gameObject = new GameObject(gameObjectName);
            gameObject.transform.position = position;
            AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
            audioSource.clip = clip;
            audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;
            audioSource.Play();
            Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
        }
    }

}