using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SamuraiFighter.Characters;
using SamuraiFighter.Combat;
using SamuraiFighter.Input;
using SamuraiFighter.UI;

namespace SamuraiFighter.Match
{
    public class MatchController : MonoBehaviour
    {
        [SerializeField] private Fighter _p1;
        [SerializeField] private Fighter _p2;
        [SerializeField] private Health _p1Health;
        [SerializeField] private Health _p2Health;
        [SerializeField] private PlayerInputHandler _p1Input;
        [SerializeField] private DummyAI _p2AI;
        [SerializeField] private RoundTimer _timer;
        [SerializeField] private Text _banner;
        [SerializeField] private Image[] _p1Pips;
        [SerializeField] private Image[] _p2Pips;

        [SerializeField] private Vector3 _p1Start = new Vector3(-2.5f, 0f, 0f);
        [SerializeField] private Vector3 _p2Start = new Vector3(2.5f, 0f, 0f);
        [SerializeField] private int _roundsToWin = 2;
        [SerializeField] private float _introSeconds = 1.5f;
        [SerializeField] private float _roundEndSeconds = 2f;
        [SerializeField] private float _roundSeconds = 60f;
        [SerializeField] private Color _pipWonColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private Color _pipEmptyColor = new Color(0.25f, 0.25f, 0.3f, 0.8f);

        private enum Phase { Intro, InRound, RoundEnd, MatchEnd }
        private Phase _phase;
        private float _phaseTimer;
        private int _p1Wins;
        private int _p2Wins;
        private int _roundNumber;

        private void Start()
        {
            _p1Wins = 0;
            _p2Wins = 0;
            _roundNumber = 0;
            UpdatePips();
            BeginIntro();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                var s = SceneManager.GetActiveScene();
                SceneManager.LoadScene(s.name);
                return;
            }

            switch (_phase)
            {
                case Phase.Intro:
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f) BeginRound();
                    break;

                case Phase.InRound:
                    if (_p1Health != null && _p1Health.IsDead) { EndRound(2, "K.O."); break; }
                    if (_p2Health != null && _p2Health.IsDead) { EndRound(1, "K.O."); break; }
                    if (_timer != null && !_timer.IsRunning && _timer.Remaining <= 0f)
                    {
                        int p1 = _p1Health != null ? _p1Health.CurrentHP : 0;
                        int p2 = _p2Health != null ? _p2Health.CurrentHP : 0;
                        int winner = p1 > p2 ? 1 : (p2 > p1 ? 2 : 0);
                        EndRound(winner, "TIME UP");
                    }
                    break;

                case Phase.RoundEnd:
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f)
                    {
                        if (_p1Wins >= _roundsToWin || _p2Wins >= _roundsToWin) BeginMatchEnd();
                        else BeginIntro();
                    }
                    break;
            }
        }

        private void BeginIntro()
        {
            _roundNumber++;
            if (_p1 != null) _p1.ResetFighter(_p1Start, true);
            if (_p2 != null) _p2.ResetFighter(_p2Start, false);
            SetInputEnabled(false);
            if (_timer != null) _timer.Stop();
            ShowBanner("ROUND " + _roundNumber);
            _phase = Phase.Intro;
            _phaseTimer = _introSeconds;
        }

        private void BeginRound()
        {
            SetInputEnabled(true);
            HideBanner();
            if (_timer != null) _timer.StartTimer(_roundSeconds);
            _phase = Phase.InRound;
        }

        private void EndRound(int winner, string reason)
        {
            SetInputEnabled(false);
            if (_timer != null) _timer.Stop();
            if (winner == 1) _p1Wins++;
            else if (winner == 2) _p2Wins++;
            UpdatePips();
            string who = winner == 1 ? "P1" : (winner == 2 ? "P2" : "DRAW");
            ShowBanner(reason + (winner == 0 ? "" : " — " + who));
            _phase = Phase.RoundEnd;
            _phaseTimer = _roundEndSeconds;
        }

        private void BeginMatchEnd()
        {
            SetInputEnabled(false);
            string who = _p1Wins > _p2Wins ? "P1 WINS" : (_p2Wins > _p1Wins ? "P2 WINS" : "DRAW");
            ShowBanner(who + "\nPress R");
            _phase = Phase.MatchEnd;
        }

        private void SetInputEnabled(bool enabled)
        {
            if (_p1Input != null) _p1Input.enabled = enabled;
            if (_p2AI != null) _p2AI.enabled = enabled;
            if (!enabled)
            {
                if (_p1 != null) _p1.SetMoveInput(0f);
                if (_p2 != null) _p2.SetMoveInput(0f);
            }
        }

        private void ShowBanner(string text)
        {
            if (_banner == null) return;
            _banner.text = text;
            _banner.enabled = true;
        }

        private void HideBanner()
        {
            if (_banner != null) _banner.enabled = false;
        }

        private void UpdatePips()
        {
            ApplyPips(_p1Pips, _p1Wins);
            ApplyPips(_p2Pips, _p2Wins);
        }

        private void ApplyPips(Image[] pips, int wins)
        {
            if (pips == null) return;
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null) continue;
                pips[i].color = i < wins ? _pipWonColor : _pipEmptyColor;
            }
        }

        public void Configure(Fighter p1, Fighter p2, Health p1Health, Health p2Health,
                              PlayerInputHandler p1Input, DummyAI p2AI,
                              RoundTimer timer, Text banner, Image[] p1Pips, Image[] p2Pips)
        {
            _p1 = p1; _p2 = p2;
            _p1Health = p1Health; _p2Health = p2Health;
            _p1Input = p1Input; _p2AI = p2AI;
            _timer = timer; _banner = banner;
            _p1Pips = p1Pips; _p2Pips = p2Pips;
        }
    }
}
