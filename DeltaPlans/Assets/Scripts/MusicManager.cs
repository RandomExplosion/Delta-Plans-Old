using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{

    private AudioSource _musicPlayer;
    public Music[] music;

    [System.Serializable]
    public class Music
    {
        public AudioClip clip;

        public string[] ScenesToPlay;
    }

    private void Awake()
    {
        _musicPlayer = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        //Check if there is already a Music Player in the scene
        if (GameObject.FindGameObjectsWithTag("Music").Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            //Make This Object not be destroyed when a new scene is loaded
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string curScene = SceneManager.GetActiveScene().name;

        //Check which music should be playing
        foreach (Music track in music)
        {
            foreach (string item in track.ScenesToPlay)
            {

                if (item.Equals(curScene))
                {

                    //Check if the correct music is already playing
                    if (_musicPlayer.clip != track.clip) //If not 
                    {
                        //Play the correct music
                        _musicPlayer.clip = track.clip;

                        if (!_musicPlayer.isPlaying)
                        {
                            _musicPlayer.Play();
                        }

                    }
                }
            }
        }
    }

}
