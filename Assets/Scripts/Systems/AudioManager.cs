using UnityEngine;

namespace TowerDefense.Systems
{
    /// <summary>
    /// 音频管理器。程序化生成背景音乐和所有音效，无需外部音频文件。
    /// 单例模式，自动创建。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Range(0f, 1f)] public float MusicVolume = 0.35f;
        [Range(0f, 1f)] public float SfxVolume = 0.5f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        // 音效缓存
        private AudioClip _clickClip;
        private AudioClip _buildClip;
        private AudioClip _upgradeClip;
        private AudioClip _arrowShootClip;
        private AudioClip _cannonShootClip;
        private AudioClip _laserShootClip;
        private AudioClip _enemyDeathClip;
        private AudioClip _waveStartClip;
        private AudioClip _goldClip;
        private AudioClip _errorClip;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = MusicVolume;
            _musicSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.volume = SfxVolume;
            _sfxSource.playOnAwake = false;

            GenerateAllClips();
        }

        private void Start()
        {
            PlayBGM();
        }

        // ============================================================
        //  公共播放接口
        // ============================================================

        public void PlayBGM()
        {
            if (_musicSource != null && !_musicSource.isPlaying)
            {
                _musicSource.clip = GenerateBGM();
                _musicSource.Play();
            }
        }

        public void StopBGM()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        public void PlayClick() => PlaySfx(_clickClip);
        public void PlayBuild() => PlaySfx(_buildClip);
        public void PlayUpgrade() => PlaySfx(_upgradeClip);
        public void PlayArrowShoot() => PlaySfx(_arrowShootClip);
        public void PlayCannonShoot() => PlaySfx(_cannonShootClip);
        public void PlayLaserShoot() => PlaySfx(_laserShootClip);
        public void PlayEnemyDeath() => PlaySfx(_enemyDeathClip);
        public void PlayWaveStart() => PlaySfx(_waveStartClip);
        public void PlayGold() => PlaySfx(_goldClip);
        public void PlayError() => PlaySfx(_errorClip);

        public void PlayTowerShoot(Towers.TowerType type)
        {
            switch (type)
            {
                case Towers.TowerType.Archer: PlayArrowShoot(); break;
                case Towers.TowerType.Cannon: PlayCannonShoot(); break;
                case Towers.TowerType.Laser: PlayLaserShoot(); break;
                case Towers.TowerType.Frost: PlayArrowShoot(); break;
                case Towers.TowerType.Poison: PlayArrowShoot(); break;
                case Towers.TowerType.Support: break;
            }
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            if (_sfxSource != null) _sfxSource.volume = SfxVolume;
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null) _musicSource.volume = MusicVolume;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(clip, SfxVolume);
            }
        }

        // ============================================================
        //  程序化生成所有音效
        // ============================================================

        private void GenerateAllClips()
        {
            _clickClip = GenerateTone(800f, 0.08f, 0.3f, WaveType.Square);
            _buildClip = GenerateChime(new[] { 523f, 659f, 784f }, 0.15f);
            _upgradeClip = GenerateChime(new[] { 523f, 659f, 784f, 1047f }, 0.2f);
            _arrowShootClip = GenerateWhoosh(0.12f);
            _cannonShootClip = GenerateExplosion(0.2f);
            _laserShootClip = GenerateLaser(0.15f);
            _enemyDeathClip = GenerateExplosion(0.25f);
            _waveStartClip = GenerateChime(new[] { 392f, 523f, 659f }, 0.25f);
            _goldClip = GenerateTone(1200f, 0.1f, 0.25f, WaveType.Sine);
            _errorClip = GenerateTone(200f, 0.15f, 0.3f, WaveType.Square);
        }

        private enum WaveType { Sine, Square, Triangle, Noise }

        /// <summary>生成简单音调</summary>
        private static AudioClip GenerateTone(float frequency, float duration, float volume, WaveType type)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float phase = t * frequency;
                float wave = type switch
                {
                    WaveType.Sine => Mathf.Sin(2f * Mathf.PI * phase),
                    WaveType.Square => Mathf.Sin(2f * Mathf.PI * phase) > 0 ? 1f : -1f,
                    WaveType.Triangle => Mathf.PingPong(phase * 2f, 1f) * 2f - 1f,
                    WaveType.Noise => Random.Range(-1f, 1f),
                    _ => 0f
                };
                // 包络：快速起音，指数衰减
                float envelope = Mathf.Exp(-t * 15f);
                data[i] = wave * volume * envelope;
            }

            var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>生成上升音阶（叮咚声）</summary>
        private static AudioClip GenerateChime(float[] frequencies, float noteDuration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.RoundToInt(sampleRate * noteDuration * frequencies.Length);
            var data = new float[totalSamples];
            int noteSamples = Mathf.RoundToInt(sampleRate * noteDuration);

            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    if (idx >= totalSamples) break;
                    float t = i / (float)sampleRate;
                    float wave = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t)
                               + 0.3f * Mathf.Sin(2f * Mathf.PI * frequencies[n] * 2f * t);
                    float envelope = Mathf.Exp(-t * 8f);
                    data[idx] = wave * 0.25f * envelope;
                }
            }

            var clip = AudioClip.Create("chime", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>生成"嗖"声（频率扫描）</summary>
        private static AudioClip GenerateWhoosh(float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float freq = Mathf.Lerp(1500f, 400f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f
                           + Random.Range(-0.3f, 0.3f);
                float envelope = Mathf.Sin(Mathf.PI * t / duration);
                data[i] = wave * 0.2f * envelope;
            }

            var clip = AudioClip.Create("whoosh", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>生成爆炸声（噪声+低频）</summary>
        private static AudioClip GenerateExplosion(float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float noise = Random.Range(-1f, 1f);
                float lowFreq = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.5f;
                float envelope = Mathf.Exp(-t * 12f);
                data[i] = (noise * 0.6f + lowFreq * 0.4f) * 0.35f * envelope;
            }

            var clip = AudioClip.Create("explosion", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>生成激光声（频率扫描+锯齿）</summary>
        private static AudioClip GenerateLaser(float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float freq = Mathf.Lerp(1200f, 600f, t / duration);
                float saw = 2f * (freq * t - Mathf.Floor(freq * t + 0.5f));
                float envelope = Mathf.Exp(-t * 10f);
                data[i] = saw * 0.15f * envelope;
            }

            var clip = AudioClip.Create("laser", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>生成循环背景音乐（简单和弦进行+旋律）</summary>
        private static AudioClip GenerateBGM()
        {
            int sampleRate = 44100;
            float bpm = 100f;
            float beatDur = 60f / bpm;
            int bars = 8;
            float totalDur = beatDur * 4 * bars;
            int samples = Mathf.RoundToInt(sampleRate * totalDur);
            var data = new float[samples];

            // C大调和弦进行: C - G - Am - F
            float[][] chords = {
                new[] { 261.63f, 329.63f, 392.00f }, // C
                new[] { 196.00f, 246.94f, 293.66f }, // G
                new[] { 220.00f, 261.63f, 329.63f }, // Am
                new[] { 174.61f, 220.00f, 261.63f }, // F
            };

            // 简单旋律音
            float[] melody = {
                523.25f, 659.25f, 783.99f, 659.25f,
                587.33f, 783.99f, 880.00f, 783.99f,
                523.25f, 659.25f, 880.00f, 783.99f,
                698.46f, 880.00f, 1046.50f, 880.00f,
            };

            for (int bar = 0; bar < bars; bar++)
            {
                var chord = chords[bar % 4];
                int barSamples = Mathf.RoundToInt(sampleRate * beatDur * 4);
                int barStart = bar * barSamples;

                for (int i = 0; i < barSamples; i++)
                {
                    int idx = barStart + i;
                    if (idx >= samples) break;
                    float t = i / (float)sampleRate;
                    float barT = t / (beatDur * 4);

                    // 和弦垫（柔和正弦波）
                    float chordWave = 0f;
                    foreach (var f in chord)
                    {
                        chordWave += Mathf.Sin(2f * Mathf.PI * f * t);
                    }
                    chordWave /= chord.Length;
                    float chordEnv = 0.12f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * barT));

                    // 旋律（每拍一个音）
                    int beat = Mathf.FloorToInt(t / beatDur);
                    float melodyFreq = melody[(bar * 4 + beat) % melody.Length];
                    float melodyT = t - beat * beatDur;
                    float melodyWave = Mathf.Sin(2f * Mathf.PI * melodyFreq * melodyT)
                                     + 0.3f * Mathf.Sin(2f * Mathf.PI * melodyFreq * 2f * melodyT);
                    float melodyEnv = 0.08f * Mathf.Exp(-melodyT * 6f);

                    // 低音鼓点（每拍）
                    float kick = 0f;
                    if (melodyT < 0.1f)
                    {
                        float kickFreq = Mathf.Lerp(150f, 50f, melodyT / 0.1f);
                        kick = Mathf.Sin(2f * Mathf.PI * kickFreq * melodyT) * 0.15f * Mathf.Exp(-melodyT * 20f);
                    }

                    data[idx] = chordWave * chordEnv + melodyWave * melodyEnv + kick;
                }
            }

            var clip = AudioClip.Create("BGM", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
