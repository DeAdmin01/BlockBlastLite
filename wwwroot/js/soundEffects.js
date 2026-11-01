// Advanced Procedural Web Audio Synthesizer for Block Blast Lite
// High fidelity, warm acoustic harmonics, zero dependencies, zero latency, 100% exception-safe

// Advanced Procedural Web Audio Synthesizer for Block Blast Lite
// Warm, organic, soothing acoustic harmonics with zero latency and zero clipping
// Pure Web Audio API: Kalimba, Marimba, Celeste, Warm Wooden Thump, Master Limiter

class SoundEngine {
    constructor() {
        this.ctx = null;
        this.isMuted = false;
        this.masterGain = null;
        this.compressor = null;
        this.sfxGain = null;
        this.shoutoutGain = null;

        // BGM State
        this.bgmAudio = null;
        this.bgmTrack = 'aero'; // 'aero' or 'quiet'
        this.isBgmEnabled = true;
        this.bgmVolume = 0.25; // 25% default as requested by user
        this.currentBgmPath = '';
        this.bgmFadeInterval = null;

        // SFX & Voice State
        this.isSfxEnabled = true;
        this.sfxVolume = 1.0;
        this.isVoiceEnabled = true;
        this.voiceVolume = 1.0;
        this.voiceGender = 'mixed'; // 'male' | 'female' | 'mixed'

        // Shoutouts audio buffers and cooldown anti-spam state
        this.shoutoutBuffers = {};
        this.lastWordTime = {};
        this.lastGlobalVoiceTime = 0;
        this.isLoadingShoutouts = false;

        // Background / App Lifecycle state
        this.isAppInBackground = false;
        this.wasBgmPlayingBeforeBackground = false;
    }

    init() {
        try {
            if (!this.ctx) {
                const AudioContext = window.AudioContext || window.webkitAudioContext;
                if (AudioContext) {
                    this.ctx = new AudioContext();

                    // Master Dynamics Compressor / Limiter to prevent clipping, harsh peaks or DAC pops
                    this.compressor = this.ctx.createDynamicsCompressor();
                    this.compressor.threshold.setValueAtTime(-14, this.ctx.currentTime);
                    this.compressor.knee.setValueAtTime(8, this.ctx.currentTime);
                    this.compressor.ratio.setValueAtTime(6, this.ctx.currentTime);
                    this.compressor.attack.setValueAtTime(0.003, this.ctx.currentTime);
                    this.compressor.release.setValueAtTime(0.15, this.ctx.currentTime);

                    // Master Gain
                    this.masterGain = this.ctx.createGain();
                    this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : 0.42, this.ctx.currentTime);

                    // Dedicated SFX Gain for block tactile sounds & chimes
                    this.sfxGain = this.ctx.createGain();
                    this.sfxGain.gain.setValueAtTime(this.isSfxEnabled ? this.sfxVolume : 0, this.ctx.currentTime);
                    this.sfxGain.connect(this.masterGain);

                    // Dedicated Voice Shoutout Gain for crisp vocal presence
                    this.shoutoutGain = this.ctx.createGain();
                    this.shoutoutGain.gain.setValueAtTime(this.isVoiceEnabled ? 1.15 * this.voiceVolume : 0, this.ctx.currentTime);
                    this.shoutoutGain.connect(this.masterGain);

                    this.masterGain.connect(this.compressor);
                    this.compressor.connect(this.ctx.destination);

                    this._preloadShoutouts();
                }
            }
            if (this.ctx && this.ctx.state === 'suspended') {
                this.ctx.resume().catch(() => {});
            }
            // Auto start BGM if enabled and not already playing
            if (this.isBgmEnabled && !this.isMuted && (!this.bgmAudio || this.bgmAudio.paused)) {
                this.playBgm();
            }
        } catch (e) {
            // AudioContext permission handling
        }
    }

    // Preload both male and female voice shoutout audio files into memory buffers
    async _preloadShoutouts() {
        if (this.isLoadingShoutouts || !this.ctx) return;
        this.isLoadingShoutouts = true;

        const files = {
            // Male voice pack
            'good_m': 'audio/good.mp3',
            'great_m': 'audio/great.mp3',
            'impressive_m': 'audio/impressive.mp3',
            'excellent_m': 'audio/excellent.mp3',
            'amazing_m': 'audio/amazing.mp3',
            'unbelievable_m': 'audio/unbelivable.mp3',
            'unbelivable_m': 'audio/unbelivable.mp3',
            'insane_m': 'audio/insane.mp3',

            // Female voice pack
            'good_f': 'audio/good_f.mp3',
            'great_f': 'audio/great_f.mp3',
            'impressive_f': 'audio/impressive_f.mp3',
            'excellent_f': 'audio/excellent_f.mp3',
            'amazing_f': 'audio/amazing_f.mp3',
            'unbelievable_f': 'audio/unbelivable_f.mp3',
            'unbelivable_f': 'audio/unbelivable_f.mp3',
            'insane_f': 'audio/insane_f.mp3'
        };

        for (const [key, path] of Object.entries(files)) {
            if (this.shoutoutBuffers[key]) continue;
            try {
                const response = await fetch(path);
                if (response.ok) {
                    const arrayBuffer = await response.arrayBuffer();
                    const audioBuffer = await this.ctx.decodeAudioData(arrayBuffer);
                    this.shoutoutBuffers[key] = audioBuffer;
                }
            } catch (err) {
                // Silently fallback if needed
            }
        }
    }

    // =========================================================================
    // BGM Background Music Engine (Smooth Loop, Crossfade & Volume Control)
    // =========================================================================

    playBgm() {
        if (this.isAppInBackground || this.isMuted || !this.isBgmEnabled) {
            this.pauseBgm();
            return;
        }

        // Primary paths without spaces to prevent Android AssetManager / WebView 404 lookup issues
        const path = this.bgmTrack === 'aero' ? 'audio/bgm/Aero.mp3' : 'audio/bgm/quiet_geometry.mp3';

        if (!this.bgmAudio || this.currentBgmPath !== path) {
            if (this.bgmAudio) {
                try {
                    this.bgmAudio.pause();
                    this.bgmAudio.removeAttribute('src');
                    this.bgmAudio.load();
                } catch (e) {}
            }

            this.bgmAudio = new Audio(path);
            this.bgmAudio.loop = true;
            this.currentBgmPath = path;
            this.bgmAudio.volume = 0; // Start at 0 for smooth fade-in
        }

        try {
            const playPromise = this.bgmAudio.play();
            if (playPromise !== undefined) {
                playPromise.then(() => {
                    this._fadeBgmTo(this.isMuted || !this.isBgmEnabled ? 0 : this.bgmVolume, 800);
                }).catch(() => {
                    // Browser autoplay policy: will resume on first user interaction
                });
            }
        } catch (e) {}
    }

    pauseBgm(fadeTimeMs = 400) {
        if (!this.bgmAudio) return;
        this._fadeBgmTo(0, fadeTimeMs, () => {
            if (this.bgmAudio) {
                try { this.bgmAudio.pause(); } catch (e) {}
            }
        });
    }

    _fadeBgmTo(targetVolume, durationMs = 600, onComplete = null) {
        if (!this.bgmAudio) return;
        if (this.bgmFadeInterval) {
            clearInterval(this.bgmFadeInterval);
            this.bgmFadeInterval = null;
        }

        const clampedTarget = Math.max(0, Math.min(1, targetVolume));
        const startVolume = this.bgmAudio.volume;
        const steps = 18;
        const stepTime = durationMs / steps;
        let currentStep = 0;

        this.bgmFadeInterval = setInterval(() => {
            currentStep++;
            const t = currentStep / steps;
            if (this.bgmAudio) {
                this.bgmAudio.volume = Math.max(0, Math.min(1, startVolume + (clampedTarget - startVolume) * t));
            }
            if (currentStep >= steps) {
                clearInterval(this.bgmFadeInterval);
                this.bgmFadeInterval = null;
                if (this.bgmAudio) this.bgmAudio.volume = clampedTarget;
                if (onComplete) onComplete();
            }
        }, stepTime);
    }

    setBgmTrack(track) {
        const normalized = track === 'aero' ? 'aero' : 'quiet';
        if (this.bgmTrack !== normalized) {
            this.bgmTrack = normalized;
            if (this.isBgmEnabled && !this.isMuted) {
                this.pauseBgm(300);
                setTimeout(() => {
                    this.playBgm();
                }, 320);
            }
        }
    }

    setBgmEnabled(enabled) {
        this.isBgmEnabled = !!enabled;
        if (this.isBgmEnabled && !this.isMuted) {
            this.playBgm();
        } else {
            this.pauseBgm();
        }
    }

    setBgmVolume(volume) {
        this.bgmVolume = Math.max(0, Math.min(1, Number(volume)));
        if (this.bgmAudio && this.isBgmEnabled && !this.isMuted) {
            this._fadeBgmTo(this.bgmVolume, 150);
        }
    }

    setSfxEnabled(enabled) {
        this.isSfxEnabled = !!enabled;
        if (this.sfxGain && this.ctx) {
            this.sfxGain.gain.setValueAtTime(this.isSfxEnabled ? this.sfxVolume : 0, this.ctx.currentTime);
        }
    }

    setSfxVolume(volume) {
        this.sfxVolume = Math.max(0, Math.min(1, Number(volume)));
        if (this.sfxGain && this.ctx && this.isSfxEnabled) {
            this.sfxGain.gain.setValueAtTime(this.sfxVolume, this.ctx.currentTime);
        }
    }

    setVoiceEnabled(enabled) {
        this.isVoiceEnabled = !!enabled;
        if (this.shoutoutGain && this.ctx) {
            this.shoutoutGain.gain.setValueAtTime(this.isVoiceEnabled ? 1.15 * this.voiceVolume : 0, this.ctx.currentTime);
        }
    }

    setVoiceVolume(volume) {
        this.voiceVolume = Math.max(0, Math.min(1, Number(volume)));
        if (this.shoutoutGain && this.ctx && this.isVoiceEnabled) {
            this.shoutoutGain.gain.setValueAtTime(1.15 * this.voiceVolume, this.ctx.currentTime);
        }
    }

    setVoiceGender(gender) {
        // 'male' | 'female' | 'mixed'
        this.voiceGender = (gender === 'female' || gender === 'male' || gender === 'mixed') ? gender : 'mixed';
    }

    setMuted(muted) {
        this.isMuted = !!muted;
        try {
            if (this.masterGain && this.ctx) {
                this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : 0.42, this.ctx.currentTime);
            }
            if (this.isMuted) {
                this.pauseBgm(200);
            } else if (this.isBgmEnabled) {
                this.playBgm();
            }
        } catch (e) {}
    }

    // Gentle tactile wooden click / bubble pop (pleasant UI tap)
    playClick() {
        if (this.isAppInBackground || this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;

            // Soft tactile marimba click
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            const filter = this.ctx.createBiquadFilter();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(680, now);
            osc.frequency.exponentialRampToValueAtTime(320, now + 0.04);

            filter.type = 'lowpass';
            filter.frequency.setValueAtTime(1400, now);

            // Gentle micro-envelope to prevent DAC pop
            gain.gain.setValueAtTime(0.0001, now);
            gain.gain.linearRampToValueAtTime(0.22, now + 0.004);
            gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.045);

            osc.connect(filter);
            filter.connect(gain);
            gain.connect(this.masterGain);

            osc.start(now);
            osc.stop(now + 0.05);
            osc.onended = () => { osc.disconnect(); filter.disconnect(); gain.disconnect(); };
        } catch (e) {}
    }

    // Pleasant warm acoustic pickup (soft kalimba harmonic pluck)
    playPickup() {
        if (this.isAppInBackground || this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;

            // Harmonic dual-sine kalimba pluck
            const fundamental = 523.25; // C5
            const overtone = fundamental * 2.756; // Soft bell overtone

            const osc1 = this.ctx.createOscillator();
            const osc2 = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc1.type = 'sine';
            osc1.frequency.setValueAtTime(fundamental, now);
            osc1.frequency.exponentialRampToValueAtTime(fundamental * 1.08, now + 0.07);

            osc2.type = 'sine';
            osc2.frequency.setValueAtTime(overtone, now);

            gain.gain.setValueAtTime(0.0001, now);
            gain.gain.linearRampToValueAtTime(0.24, now + 0.005);
            gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.08);

            const gain2 = this.ctx.createGain();
            gain2.gain.setValueAtTime(0.06, now);
            gain2.gain.exponentialRampToValueAtTime(0.0001, now + 0.05);

            osc1.connect(gain);
            osc2.connect(gain2);
            gain2.connect(gain);
            gain.connect(this.masterGain);

            osc1.start(now);
            osc2.start(now);
            osc1.stop(now + 0.085);
            osc2.stop(now + 0.085);
            osc1.onended = () => { osc1.disconnect(); osc2.disconnect(); gain.disconnect(); gain2.disconnect(); };
        } catch (e) {}
    }

    // Deeply satisfying warm acoustic wooden puzzle "thump / thock"
    // Rich low resonant body + warm muted wooden knock transient (zero harsh frequencies)
    playPlace() {
        if (this.isAppInBackground || this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;

            // 1. Deep warm resonant wood body
            const bodyOsc = this.ctx.createOscillator();
            const bodyGain = this.ctx.createGain();
            bodyOsc.type = 'sine';
            bodyOsc.frequency.setValueAtTime(145, now);
            bodyOsc.frequency.exponentialRampToValueAtTime(58, now + 0.09);

            bodyGain.gain.setValueAtTime(0.0001, now);
            bodyGain.gain.linearRampToValueAtTime(0.38, now + 0.004);
            bodyGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.095);

            bodyOsc.connect(bodyGain);
            bodyGain.connect(this.masterGain);

            // 2. Mellow acoustic wood tap transient (warm lowpassed knock)
            const knockOsc = this.ctx.createOscillator();
            const knockFilter = this.ctx.createBiquadFilter();
            const knockGain = this.ctx.createGain();

            knockOsc.type = 'triangle';
            knockOsc.frequency.setValueAtTime(380, now);
            knockOsc.frequency.exponentialRampToValueAtTime(110, now + 0.035);

            knockFilter.type = 'lowpass';
            knockFilter.frequency.setValueAtTime(650, now);

            knockGain.gain.setValueAtTime(0.0001, now);
            knockGain.gain.linearRampToValueAtTime(0.28, now + 0.003);
            knockGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.04);

            knockOsc.connect(knockFilter);
            knockFilter.connect(knockGain);
            knockGain.connect(this.masterGain);

            bodyOsc.start(now);
            knockOsc.start(now);
            bodyOsc.stop(now + 0.1);
            knockOsc.stop(now + 0.045);

            bodyOsc.onended = () => {
                bodyOsc.disconnect();
                bodyGain.disconnect();
                knockOsc.disconnect();
                knockFilter.disconnect();
                knockGain.disconnect();
            };
        } catch (e) {}
    }

    // Soft muted double rubber bump (calm and polite, zero harsh buzz)
    playInvalid() {
        if (this.isAppInBackground || this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;

            // Two gentle low-frequency damp pulses (105Hz -> 75Hz)
            [0, 0.065].forEach((offset, idx) => {
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                const t = now + offset;

                osc.type = 'sine';
                osc.frequency.setValueAtTime(idx === 0 ? 115 : 92, t);
                osc.frequency.exponentialRampToValueAtTime(65, t + 0.05);

                gain.gain.setValueAtTime(0.0001, t);
                gain.gain.linearRampToValueAtTime(0.18, t + 0.004);
                gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.055);

                osc.connect(gain);
                gain.connect(this.masterGain);

                osc.start(t);
                osc.stop(t + 0.06);
                osc.onended = () => { osc.disconnect(); gain.disconnect(); };
            });
        } catch (e) {}
    }

    // Crystal celeste / marimba chime voice generator
    _playChimeTone(freq, startTime, duration = 0.42, volume = 0.22) {
        if (!this.ctx) return;
        try {
            // Fundamental pure tone
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            const filter = this.ctx.createBiquadFilter();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(freq, startTime);
            osc.frequency.exponentialRampToValueAtTime(freq * 1.006, startTime + duration);

            // Shimmering overtone (harmonic 2.76x for marimba/crystal chime feel)
            const harmOsc = this.ctx.createOscillator();
            const harmGain = this.ctx.createGain();
            harmOsc.type = 'sine';
            harmOsc.frequency.setValueAtTime(freq * 2.76, startTime);

            harmGain.gain.setValueAtTime(0.0001, startTime);
            harmGain.gain.linearRampToValueAtTime(volume * 0.24, startTime + 0.005);
            harmGain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration * 0.45);

            filter.type = 'lowpass';
            filter.frequency.setValueAtTime(3200, startTime);

            gain.gain.setValueAtTime(0.0001, startTime);
            gain.gain.linearRampToValueAtTime(volume, startTime + 0.006);
            gain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration);

            osc.connect(filter);
            filter.connect(gain);
            harmOsc.connect(harmGain);
            harmGain.connect(gain);
            gain.connect(this.masterGain);

            osc.start(startTime);
            harmOsc.start(startTime);
            osc.stop(startTime + duration);
            harmOsc.stop(startTime + duration * 0.5);

            osc.onended = () => {
                osc.disconnect();
                harmOsc.disconnect();
                harmGain.disconnect();
                filter.disconnect();
                gain.disconnect();
            };
        } catch (e) {}
    }

    // Lush, harmonic, sparkling line clear fanfare
    // Musical Pentatonic Scales with dynamic combo pitch modulation
    playLineClear(clearedCells = 8, clearedLines = 1, combo = 1) {
        if (this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;
            // Pentatonic scale modulation based on combo: C -> D -> E -> G -> A -> C'
            const pitchShift = Math.pow(2, Math.min(combo - 1, 7) * (2 / 12));

            // Tier 1: Single Line (8-11 cells) - Sparkling 4-note crystal chime
            if (clearedLines <= 1 && clearedCells < 12) {
                // C5, E5, G5, C6 (Major triad with octave resolution)
                const notes = [523.25, 659.25, 783.99, 1046.50];
                notes.forEach((freq, idx) => {
                    const delay = idx * 0.04;
                    this._playChimeTone(freq * pitchShift, now + delay, 0.42, 0.22);
                });
            }
            // Tier 2: Double Line (12-19 cells) - Dual harp sweep with warm bass root
            else if (clearedLines === 2 || (clearedCells >= 12 && clearedCells < 20)) {
                // Gentle warm sub-pulse (not muddy, very smooth)
                const sub = this.ctx.createOscillator();
                const subGain = this.ctx.createGain();
                sub.type = 'sine';
                sub.frequency.setValueAtTime(130.81 * pitchShift, now); // C3
                sub.frequency.exponentialRampToValueAtTime(65.4, now + 0.18);
                subGain.gain.setValueAtTime(0.0001, now);
                subGain.gain.linearRampToValueAtTime(0.26, now + 0.005);
                subGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.18);
                sub.connect(subGain);
                subGain.connect(this.masterGain);
                sub.start(now);
                sub.stop(now + 0.19);
                sub.onended = () => { sub.disconnect(); subGain.disconnect(); };

                // 6-note shimmering pentatonic sweep (C5, D5, E5, G5, A5, C6)
                const notes = [523.25, 587.33, 659.25, 783.99, 880.00, 1046.50];
                notes.forEach((freq, idx) => {
                    const delay = idx * 0.034;
                    this._playChimeTone(freq * pitchShift, now + delay, 0.46, 0.20);
                });
            }
            // Tier 3: Triple Line (20-27 cells) - Triumphant vibraphone fanfare chord
            else if (clearedLines === 3 || (clearedCells >= 20 && clearedCells < 28)) {
                // Warm resonant body
                const sub = this.ctx.createOscillator();
                const subGain = this.ctx.createGain();
                sub.type = 'sine';
                sub.frequency.setValueAtTime(174.61 * pitchShift, now); // F3
                sub.frequency.exponentialRampToValueAtTime(43.65, now + 0.24);
                subGain.gain.setValueAtTime(0.0001, now);
                subGain.gain.linearRampToValueAtTime(0.34, now + 0.006);
                subGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.25);
                sub.connect(subGain);
                subGain.connect(this.masterGain);
                sub.start(now);
                sub.stop(now + 0.26);
                sub.onended = () => { sub.disconnect(); subGain.disconnect(); };

                // 8-note Major 9th cascade (C4, G4, C5, E5, G5, B5, D6, G6)
                const notes = [261.63, 392.00, 523.25, 659.25, 783.99, 987.77, 1174.66, 1567.98];
                notes.forEach((freq, idx) => {
                    const delay = idx * 0.028;
                    this._playChimeTone(freq * pitchShift, now + delay, 0.52, 0.19);
                });
            }
            // Tier 4: Quad or Mega Clear (28+ cells) - Dazzling orchestral celeste waterfall
            else {
                // Cinematic warm 808-style smooth sub drop
                const sub = this.ctx.createOscillator();
                const subGain = this.ctx.createGain();
                sub.type = 'sine';
                sub.frequency.setValueAtTime(120, now);
                sub.frequency.exponentialRampToValueAtTime(32, now + 0.38);
                subGain.gain.setValueAtTime(0.0001, now);
                subGain.gain.linearRampToValueAtTime(0.42, now + 0.006);
                subGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.4);
                sub.connect(subGain);
                subGain.connect(this.masterGain);
                sub.start(now);
                sub.stop(now + 0.42);
                sub.onended = () => { sub.disconnect(); subGain.disconnect(); };

                // 10-note cascading celeste waterfall across 3 octaves
                const megaNotes = [
                    261.63, 329.63, 392.00, 523.25, 659.25, 783.99,
                    1046.50, 1318.51, 1567.98, 2093.00
                ];
                megaNotes.forEach((freq, idx) => {
                    const delay = idx * 0.024;
                    this._playChimeTone(freq * pitchShift, now + delay, 0.58, 0.18);
                });
            }

            if (combo >= 2) {
                this.playComboStinger(now + 0.15, combo);
            }
        } catch (e) {}
    }

    // Euphoric shimmering crystal sparkle for combo streaks
    playComboStinger(startTime, combo) {
        try {
            const arpeggio = [659.25, 783.99, 987.77, 1174.66, 1318.51, 1567.98];
            const pitchMult = 1 + Math.min(combo - 2, 8) * 0.06;
            arpeggio.forEach((freq, i) => {
                const t = startTime + i * 0.03;
                this._playChimeTone(freq * pitchMult, t, 0.35, 0.14);
            });
        } catch (e) {}
    }

    // Play spoken voice shoutout with anti-spam cooldown protection
    // Words: 'good', 'great', 'impressive', 'excellent', 'amazing', 'unbelievable', 'insane'
    playShoutout(word) {
        if (this.isAppInBackground || this.isMuted) return false;
        try {
            this.init();
            if (!this.ctx) return false;

            const normalizedWord = String(word).trim().toLowerCase();
            const isUnbelievable = normalizedWord.startsWith('unbeliev') || normalizedWord.startsWith('unbeliv');
            const isInsane = normalizedWord === 'insane';

            const now = this.ctx.currentTime;

            const tierMap = {
                'good': 1,
                'great': 2,
                'impressive': 3,
                'excellent': 4,
                'amazing': 5,
                'unbelievable': 6,
                'unbelivable': 6,
                'insane': 7
            };
            const currentTier = isInsane ? 7 : (isUnbelievable ? 6 : (tierMap[normalizedWord] || 1));

            // Reset graduation ladder if quiet for > 4.5s
            if (now - this.lastGlobalVoiceTime > 4.5) {
                this.lastVoiceTier = 0;
            }

            // Graduation rule:
            // Escalation to a higher rank (e.g. Great after Good, or Insane) ALWAYS ignores cooldown.
            // Otherwise, enforce strict 3.5s cooldown between spoken phrases to eliminate audio fatigue.
            const isGraduation = currentTier > (this.lastVoiceTier || 0);
            if (!isGraduation && (now - this.lastGlobalVoiceTime < 3.5)) {
                return false; // Yield to cooldown
            }

            // Choose gender variant: 'male' -> '_m', 'female' -> '_f', 'mixed' -> random 50/50
            let chosenGender = this.voiceGender;
            if (chosenGender === 'mixed') {
                chosenGender = Math.random() < 0.5 ? 'male' : 'female';
            }
            const suffix = chosenGender === 'female' ? '_f' : '_m';
            const baseWord = isUnbelievable ? 'unbelievable' : normalizedWord;

            // Find matching buffer with fallback
            let buffer = this.shoutoutBuffers[baseWord + suffix];
            if (!buffer && baseWord === 'unbelievable') {
                buffer = this.shoutoutBuffers['unbelivable' + suffix];
            }
            // Fallback to opposite gender if not loaded yet
            if (!buffer) {
                const altSuffix = chosenGender === 'female' ? '_m' : '_f';
                buffer = this.shoutoutBuffers[baseWord + altSuffix] || this.shoutoutBuffers[baseWord];
            }

            if (!buffer) {
                this._preloadShoutouts();
                return false;
            }

            // Record cooldown timestamps
            this.lastWordTime[normalizedWord] = now;
            this.lastGlobalVoiceTime = now;
            this.lastVoiceTier = currentTier;

            const source = this.ctx.createBufferSource();
            source.buffer = buffer;
            source.connect(this.shoutoutGain || this.masterGain);
            source.start(0);

            return true;
        } catch (e) {
            return false;
        }
    }

    // Epic, euphoric celebratory fanfare when the player achieves a 100% full board wipe (Clean Slate)
    // Brighter, longer (2.8 - 3.2s), soaring celestial arpeggio, wide stereo brass sub-swell,
    // multi-layered major chords, and sparkling crystal chime waterfall
    playBoardClear() {
        if (this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;

            // 1. Triumphant warm sub-swell rising with deep low-end punch (lasts ~2.5s)
            const sub = this.ctx.createOscillator();
            const subGain = this.ctx.createGain();
            sub.type = 'sine';
            sub.frequency.setValueAtTime(90, now);
            sub.frequency.exponentialRampToValueAtTime(36, now + 0.5);
            subGain.gain.setValueAtTime(0.0001, now);
            subGain.gain.linearRampToValueAtTime(0.48, now + 0.008);
            subGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.55);
            sub.connect(subGain);
            subGain.connect(this.masterGain);
            sub.start(now);
            sub.stop(now + 0.58);
            sub.onended = () => { sub.disconnect(); subGain.disconnect(); };

            // 2. Majestic ascending Major Triad Fanfare (G4 -> C5 -> E5 -> G5 -> C6)
            const fanfare = [392.00, 523.25, 659.25, 783.99, 1046.50];
            fanfare.forEach((freq, idx) => {
                const delay = idx * 0.055;
                this._playChimeTone(freq, now + delay, 0.65, 0.28);
            });

            // 3. Sustained sparkling C Major 9 shimmer chord (C5 - E5 - G5 - B5 - D6)
            const chordNotes1 = [523.25, 659.25, 783.99, 987.77, 1174.66];
            const chordT1 = now + 0.28;
            chordNotes1.forEach(freq => {
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                const filter = this.ctx.createBiquadFilter();

                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, chordT1);

                filter.type = 'lowpass';
                filter.frequency.setValueAtTime(2400, chordT1);

                gain.gain.setValueAtTime(0.0001, chordT1);
                gain.gain.linearRampToValueAtTime(0.24, chordT1 + 0.06);
                gain.gain.exponentialRampToValueAtTime(0.0001, chordT1 + 1.8);

                osc.connect(filter);
                filter.connect(gain);
                gain.connect(this.masterGain);

                osc.start(chordT1);
                osc.stop(chordT1 + 1.9);
                osc.onended = () => { osc.disconnect(); filter.disconnect(); gain.disconnect(); };
            });

            // 4. Grand resolution chord (F Major 7 add 9 -> C Major with luminous warmth)
            const chordNotes2 = [349.23, 440.00, 523.25, 659.25, 783.99, 1046.50];
            const chordT2 = now + 0.75;
            chordNotes2.forEach(freq => {
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                const filter = this.ctx.createBiquadFilter();

                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, chordT2);

                filter.type = 'lowpass';
                filter.frequency.setValueAtTime(2800, chordT2);

                gain.gain.setValueAtTime(0.0001, chordT2);
                gain.gain.linearRampToValueAtTime(0.18, chordT2 + 0.08);
                gain.gain.exponentialRampToValueAtTime(0.0001, chordT2 + 1.9);

                osc.connect(filter);
                filter.connect(gain);
                gain.connect(this.masterGain);

                osc.start(chordT2);
                osc.stop(chordT2 + 2.0);
                osc.onended = () => { osc.disconnect(); filter.disconnect(); gain.disconnect(); };
            });

            // 5. Extended stardust sparkle cascade (1.2s to 2.8s)
            const sparkleNotes = [
                1567.98, 1760.00, 2093.00, 2349.32, 2637.02, 3135.96,
                3520.00, 4186.01, 3135.96, 2637.02, 2093.00
            ];
            sparkleNotes.forEach((freq, idx) => {
                this._playChimeTone(freq, chordT1 + 0.2 + idx * 0.045, 0.55, 0.16);
            });
        } catch (e) {}
    }

    // Peaceful, soothing Rhodes / Music-box minor progression (calm and gentle)
    playGameOver() {
        if (this.isMuted) return;
        try {
            this.init();
            if (!this.ctx) return;

            const now = this.ctx.currentTime;
            // Calming, gentle acoustic minor chords (A minor 7: C5 -> A4 -> G4 -> E4 -> C4)
            const notes = [523.25, 440.00, 392.00, 329.63, 261.63];

            notes.forEach((freq, idx) => {
                const delay = idx * 0.14;
                const osc = this.ctx.createOscillator();
                const gain = this.ctx.createGain();
                const filter = this.ctx.createBiquadFilter();

                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, now + delay);
                osc.frequency.exponentialRampToValueAtTime(freq * 0.98, now + delay + 0.55);

                filter.type = 'lowpass';
                filter.frequency.setValueAtTime(900, now + delay);

                gain.gain.setValueAtTime(0.0001, now + delay);
                gain.gain.linearRampToValueAtTime(0.22, now + delay + 0.015);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + delay + 0.58);

                osc.connect(filter);
                filter.connect(gain);
                gain.connect(this.masterGain);

                osc.start(now + delay);
                osc.stop(now + delay + 0.6);
                osc.onended = () => { osc.disconnect(); filter.disconnect(); gain.disconnect(); };
            });
        } catch (e) {}
    }

    // Immediate voice preview in settings (zero cooldown, instant feedback, cancellation of previous preview)
    previewVoice(gender) {
        if (this.isAppInBackground || this.isMuted || !this.isVoiceEnabled) return;
        try {
            this.init();
            if (this.ctx && this.ctx.state === 'suspended') {
                this.ctx.resume().catch(() => {});
            }

            let targetGender = gender || this.voiceGender;
            if (targetGender === 'mixed') {
                targetGender = Math.random() < 0.5 ? 'male' : 'female';
            }
            const suffix = targetGender === 'female' ? '_f' : '_m';
            const phrase = 'great';
            const bufferKey = phrase + suffix;
            const directPath = targetGender === 'female' ? 'audio/great_f.mp3' : 'audio/great.mp3';

            // Stop any currently playing preview to avoid echo/overlap
            if (this._activePreviewSource) {
                try { this._activePreviewSource.stop(); } catch (e) {}
                this._activePreviewSource = null;
            }
            if (this._activePreviewAudio) {
                try {
                    this._activePreviewAudio.pause();
                    this._activePreviewAudio.currentTime = 0;
                } catch (e) {}
                this._activePreviewAudio = null;
            }

            const buffer = this.shoutoutBuffers[bufferKey];
            if (buffer && this.ctx) {
                const source = this.ctx.createBufferSource();
                source.buffer = buffer;
                source.connect(this.shoutoutGain || this.masterGain);
                source.start(0);
                this._activePreviewSource = source;
            } else {
                // Fallback to standard HTML5 Audio for instant playback while buffers load
                const aud = new Audio(directPath);
                aud.volume = Math.max(0, Math.min(1, (this.voiceVolume || 1.0) * (this.isMuted ? 0 : 1)));
                aud.play().catch(() => {});
                this._activePreviewAudio = aud;
                this._preloadShoutouts();
            }
        } catch (e) {
            try {
                const aud = new Audio(gender === 'female' ? 'audio/great_f.mp3' : 'audio/great.mp3');
                aud.volume = this.voiceVolume || 1.0;
                aud.play().catch(() => {});
            } catch (err) {}
        }
    }

    // App Lifecycle handlers: pause all audio in background, restore when returning
    onAppPause() {
        this.isAppInBackground = true;
        this.wasBgmPlayingBeforeBackground = Boolean(this.bgmAudio && !this.bgmAudio.paused && !this.isMuted && this.isBgmEnabled);

        // Immediately pause BGM
        if (this.bgmAudio) {
            try {
                this.bgmAudio.pause();
            } catch (e) {}
        }

        // Cancel any active preview
        if (this._activePreviewAudio) {
            try {
                this._activePreviewAudio.pause();
                this._activePreviewAudio.currentTime = 0;
            } catch (e) {}
            this._activePreviewAudio = null;
        }
        if (this._activePreviewSource) {
            try { this._activePreviewSource.stop(); } catch (e) {}
            this._activePreviewSource = null;
        }

        // Suspend Web Audio context to silence all ongoing synthesis/chimes
        if (this.ctx && this.ctx.state === 'running') {
            try {
                this.ctx.suspend().catch(() => {});
            } catch (e) {}
        }
    }

    onAppResume() {
        this.isAppInBackground = false;

        // Resume Web Audio context
        if (this.ctx && this.ctx.state === 'suspended') {
            try {
                this.ctx.resume().catch(() => {});
            } catch (e) {}
        }

        // Resume BGM only if it was active before entering background
        if (this.wasBgmPlayingBeforeBackground && this.isBgmEnabled && !this.isMuted) {
            this.playBgm();
        }
    }
}

window.soundEngine = new SoundEngine();

window.blockBlastAudio = {
    init: () => window.soundEngine.init(),
    playClick: () => window.soundEngine.playClick(),
    playPickup: () => window.soundEngine.playPickup(),
    playPlace: () => window.soundEngine.playPlace(),
    playInvalid: () => window.soundEngine.playInvalid(),
    playLineClear: (cells, lines, combo) => window.soundEngine.playLineClear(cells, lines, combo),
    playBoardClear: () => window.soundEngine.playBoardClear(),
    playGameOver: () => window.soundEngine.playGameOver(),
    playShoutout: (word) => window.soundEngine.playShoutout(word),
    previewVoice: (gender) => window.soundEngine.previewVoice(gender),
    setMuted: (muted) => window.soundEngine.setMuted(muted),

    // BGM Methods
    playBgm: () => window.soundEngine.playBgm(),
    pauseBgm: () => window.soundEngine.pauseBgm(),
    setBgmTrack: (track) => window.soundEngine.setBgmTrack(track),
    setBgmEnabled: (enabled) => window.soundEngine.setBgmEnabled(enabled),
    setBgmVolume: (vol) => window.soundEngine.setBgmVolume(vol),

    // SFX Methods
    setSfxEnabled: (enabled) => window.soundEngine.setSfxEnabled(enabled),
    setSfxVolume: (vol) => window.soundEngine.setSfxVolume(vol),

    // Voice Methods
    setVoiceEnabled: (enabled) => window.soundEngine.setVoiceEnabled(enabled),
    setVoiceVolume: (vol) => window.soundEngine.setVoiceVolume(vol),
    setVoiceGender: (gender) => window.soundEngine.setVoiceGender(gender),

    // App Lifecycle Methods
    handleAppPause: () => window.soundEngine.onAppPause(),
    handleAppResume: () => window.soundEngine.onAppResume()
};

// Auto-unlock Web Audio and BGM on first user interaction (touch/click/pointer)
const unlockAudioEngine = () => {
    if (!window.soundEngine || window.soundEngine.isAppInBackground) return;
    window.soundEngine.init();
    if (window.soundEngine.isBgmEnabled && !window.soundEngine.isMuted) {
        if (!window.soundEngine.bgmAudio || window.soundEngine.bgmAudio.paused) {
            window.soundEngine.playBgm();
        }
    }
};
['pointerdown', 'touchstart', 'click', 'keydown'].forEach(evt => {
    document.addEventListener(evt, unlockAudioEngine, { passive: true });
});

// Full background & visibility tracking across all mobile browsers & WebViews
document.addEventListener('visibilitychange', () => {
    if (document.hidden || document.visibilityState === 'hidden') {
        window.soundEngine.onAppPause();
    } else {
        window.soundEngine.onAppResume();
    }
}, { passive: true });

window.addEventListener('pagehide', () => window.soundEngine.onAppPause(), { passive: true });
window.addEventListener('pageshow', () => window.soundEngine.onAppResume(), { passive: true });
window.addEventListener('blur', () => window.soundEngine.onAppPause(), { passive: true });
window.addEventListener('focus', () => {
    if (!document.hidden && document.visibilityState !== 'hidden') {
        window.soundEngine.onAppResume();
    }
}, { passive: true });
