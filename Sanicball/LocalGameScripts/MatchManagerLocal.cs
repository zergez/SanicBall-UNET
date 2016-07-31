using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sanicball
{

    /// <summary>
    /// Manages game state - scenes, players, all that jazz
    /// </summary>
    public class MatchManagerLocal : MonoBehaviour
    {
        [SerializeField]
        private string lobbySceneName = "Lobby";

        //Prefabs
        [SerializeField]
        private UI.PauseMenu pauseMenuPrefab;
        [SerializeField]
        private RaceManager raceManagerPrefab;

        //Match state
        private Data.MatchSettings currentSettings = new Data.MatchSettings();
        private bool inLobby = false;
        private bool lobbyTimerOn = false;
        private const float lobbyTimerMax = 3;
        private float lobbyTimer = lobbyTimerMax;

        //Bools for scene initializing
        private bool loadingLobby = false;
        private bool loadingStage = false;
        private bool firstTimeLoadingLobby = false;

        //Events
        public event EventHandler<MatchPlayerEventArgs> MatchPlayerAdded;
        public event EventHandler<MatchPlayerEventArgs> MatchPlayerRemoved;

        /// <summary>
        /// Contains all players in the game, even ones from other clients in online races
        /// </summary>
        public List<MatchPlayer> Players { get; private set; }
        public Data.MatchSettings CurrentSettings { get { return currentSettings; } }

        public MatchPlayer CreatePlayer(string name, ControlType ctrlType, int characterId)
        {
            var p = new MatchPlayer(name, ctrlType, characterId);
            Players.Add(p);
            if (inLobby)
            {
                SpawnLobbyBall(p);
            }

            p.ChangedReady += AnyPlayerChangedReadyHandler;

            StopLobbyTimer();

            if (MatchPlayerAdded != null)
                MatchPlayerAdded(this, new MatchPlayerEventArgs(p));

            return p;
        }

        public void RemovePlayer(MatchPlayer player)
        {
            if (Players.Contains(player))
            {
                Players.Remove(player);

                if (player.BallObject)
                {
                    Destroy(player.BallObject.gameObject);
                }

                if (MatchPlayerRemoved != null)
                    MatchPlayerRemoved(this, new MatchPlayerEventArgs(player));
            }
        }

        public void SetCharacter(MatchPlayer player, int character)
        {
            if (!inLobby)
            {
                Debug.LogError("Cannot set character outside of lobby!");
            }

            player.CharacterId = character;
            SpawnLobbyBall(player);
        }

        private void AnyPlayerChangedReadyHandler(object sender, EventArgs e)
        {
            var allReady = Players.TrueForAll(a => a.ReadyToRace);
            if (allReady && !lobbyTimerOn)
            {
                StartLobbyTimer();
            }
            if (!allReady && lobbyTimerOn)
            {
                StopLobbyTimer();
            }
        }

        private void StartLobbyTimer()
        {
            lobbyTimerOn = true;
            LobbyReferences.Active.CountdownField.enabled = true;
        }

        private void StopLobbyTimer()
        {
            lobbyTimerOn = false;
            lobbyTimer = lobbyTimerMax;
            LobbyReferences.Active.CountdownField.enabled = false;
        }

        #region Scene changing / race loading

        public void GoToLobby()
        {
            if (inLobby) return;

            loadingStage = false;
            loadingLobby = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        public void GoToStage()
        {
            var targetStage = Data.ActiveData.Stages[CurrentSettings.StageId];

            loadingStage = true;
            loadingLobby = false;

            CameraFade.StartAlphaFade(Color.black, false, 0.3f, 0.05f, () =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetStage.sceneName);
            });
        }

        //Check if we were loading the lobby or the race
        private void OnLevelWasLoaded(int level)
        {
            if (loadingLobby)
            {
                InitLobby();
                loadingLobby = false;
                if (firstTimeLoadingLobby)
                {
                    //Let the player pick settings first time entering the lobby
                    LobbyReferences.Active.MatchSettingsPanel.Show();
                    firstTimeLoadingLobby = false;
                }
            }
            if (loadingStage)
            {
                InitRace();
                loadingStage = false;
                foreach (var p in Players)
                {
                    p.ReadyToRace = false;
                }
            }
        }

        private void InitMatch()
        {
            firstTimeLoadingLobby = true;
            GoToLobby();
        }

        //Initiate the lobby after loading lobby scene
        private void InitLobby()
        {
            inLobby = true;
            foreach (var p in Players)
            {
                SpawnLobbyBall(p);
            }
        }

        //Initiate a race after loading the stage scene
        private void InitRace()
        {
            inLobby = false;
            var raceManager = Instantiate(raceManagerPrefab);
            raceManager.Settings.CopyValues(CurrentSettings);
        }

        public void QuitMatch()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
            Destroy(gameObject);
        }

        #endregion Scene changing / race loading

        private void Start()
        {
            currentSettings.CopyValues(Data.ActiveData.MatchSettings);
            Players = new List<MatchPlayer>();
            DontDestroyOnLoad(gameObject);
            InitMatch();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                if (!UI.PauseMenu.GamePaused)
                {
                    Instantiate(pauseMenuPrefab);
                }
                else
                {
                    var menu = FindObjectOfType<UI.PauseMenu>();
                    if (menu)
                        Destroy(menu.gameObject);
                }
            }

            if (inLobby && Input.GetKeyDown(KeyCode.O))
            {
                LobbyReferences.Active.MatchSettingsPanel.Show();
            }

            if (lobbyTimerOn && inLobby)
            {
                lobbyTimer -= Time.deltaTime;
                LobbyReferences.Active.CountdownField.text = "Match starts in " + Mathf.Ceil(lobbyTimer);

                if (lobbyTimer <= 0)
                {
                    GoToStage();
                    StopLobbyTimer();
                }
            }
        }

        private void SpawnLobbyBall(MatchPlayer player)
        {
            var spawner = LobbyReferences.Active.BallSpawner;
            if (player.BallObject != null)
            {
                Destroy(player.BallObject.gameObject);
            }
            player.BallObject = spawner.SpawnBall(Data.PlayerType.Normal, player.CtrlType, player.CharacterId, "Player");
        }
    }

   
}