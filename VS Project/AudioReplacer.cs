using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SinAPI;
using OModAPI;
using System.IO;

namespace SideLoader
{
    public class AudioReplacer : MonoBehaviour
    {
        public SideLoader _base;

        private AudioSource m_CurrentAudioSource;

        internal void Start()
        {
            m_CurrentAudioSource = gameObject.GetOrAddComponent<AudioSource>();
            DontDestroyOnLoad(m_CurrentAudioSource);

            On.GlobalAudioManager.PlayMusic += PlayMusicHook;
        }

        public IEnumerator LoadAudioClips()
        {
            for (int i = 0; i < _base.FilePaths[ResourceTypes.Audio].Count(); i++)
            {
                string filePath = @"file://" + Path.GetFullPath(_base.FilePaths[ResourceTypes.Audio][i]);
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                AudioClip clip = WWWAudioExtensions.GetAudioClip(new WWW(filePath));
                DontDestroyOnLoad(clip);

                if (clip != null)
                {
                    OLogger.Warning("Loading clip: " + fileName + " from " + filePath);

                    while (clip.loadState != AudioDataLoadState.Loaded)
                        yield return new WaitForSeconds(0.1f);

                    if (_base.AudioClips.ContainsKey(fileName))
                    {
                        _base.AudioClips[fileName] = clip;
                    }
                    else
                    {
                        _base.AudioClips.Add(fileName, clip);
                    }

                    OLogger.Warning("Loaded audio clip: " + fileName);
                }
            }

            _base.Loading = false;
            yield return null;
        }

        private AudioSource PlayMusicHook(On.GlobalAudioManager.orig_PlayMusic orig, GlobalAudioManager self, GlobalAudioManager.Sounds _sound, float _fade)
        {
            string songName = _sound.ToString();

            if (_base.AudioClips.ContainsKey(songName)
                && At.GetValue(typeof(GlobalAudioManager), self, "s_musicSources") is DictionaryExt<GlobalAudioManager.Sounds, GlobalAudioManager.MusicSource> dict)
            {
                // set our custom clip to the actual GlobalAudioManager dictionary, so it works with the game systems as expected

                if (!dict.ContainsKey(_sound) && At.Call(self, "GetPrefabPath", new object[] { _sound }) is string prefabPath)
                {
                    GameObject gameObject = Resources.Load("_Sounds/" + prefabPath) as GameObject;
                    gameObject = Instantiate(gameObject);
                    AudioSource component = gameObject.GetComponent<AudioSource>();
                    DontDestroyOnLoad(gameObject);
                    dict.Add(_sound, new GlobalAudioManager.MusicSource(component));
                }

                dict[_sound].Source.clip = _base.AudioClips[_sound.ToString()];

                At.SetValue(dict, typeof(GlobalAudioManager), self, "s_musicSources");

                At.Call(self, "CleanUpMusic", null);

                At.SetValue(_sound, typeof(GlobalAudioManager), self, "s_currentMusic");

                StartCoroutine(FadeMusic(self, _sound, dict[_sound], _fade));

                return dict[_sound].Source;
            }
            else
            {
                return orig(self, _sound, _fade);
            }
        }

        private IEnumerator FadeMusic(GlobalAudioManager _manager, GlobalAudioManager.Sounds _music, GlobalAudioManager.MusicSource musSource, float _time, bool _in = true)
        {
            float vol;
            float targetVol;
            if (!_in)
            {
                vol = musSource.Source.volume;
                targetVol = 0f;
            }
            else
            {
                vol = 0f;
                targetVol = musSource.OrigVolume;
            }
            if (!musSource.Source.gameObject.activeSelf)
            {
                musSource.Source.gameObject.SetActive(true);
            }
            if (_in)
            {
                musSource.Source.Play();
            }
            float lerp = 0f;
            while (lerp < 1f)
            {
                if (!musSource.Source)
                {
                    break;
                }
                lerp = Mathf.Clamp01(lerp + Time.deltaTime / _time);
                musSource.Source.volume = Mathf.Lerp(vol, targetVol, lerp);
                yield return null;
            }
            if (!_in && musSource.Source)
            {
                if (At.GetValue(typeof(GlobalAudioManager), _manager, "m_eventMusic") is GlobalAudioManager.Sounds _eventMusic && _eventMusic != GlobalAudioManager.Sounds.NONE
                    && _music == _eventMusic)
                {
                    musSource.Source.Stop();
                }
                else
                {
                    musSource.Source.Pause();
                }
            }

            yield return null;
        }
    }
}
