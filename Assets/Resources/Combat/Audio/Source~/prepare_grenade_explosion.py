"""Prepare the licensed Mike Koenig grenade blast for the game's spatial one-shot."""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

here = Path(__file__).resolve().parent
source = here / "GrenadeExplosion_MikeKoenig.wav"
output = here.parent / "GrenadeExplosion_Heavy.wav"
with wave.open(str(source), "rb") as audio:
    rate, channels = audio.getframerate(), audio.getnchannels()
    assert audio.getsampwidth() == 2
    original = np.frombuffer(audio.readframes(audio.getnframes()), dtype="<i2").reshape(-1, channels).astype(np.float64) / 32768.0
mono = original.mean(axis=1)
# Keep the original impact and decay; gently emphasize the existing 85 Hz body.
fft_size = 1 << (2 * len(mono) - 1).bit_length()
frequencies = np.fft.rfftfreq(fft_size, 1 / rate)
bell = np.exp(-0.5 * (np.log(np.maximum(frequencies, 1.0) / 85.0) / 0.65) ** 2)
weighted = np.fft.irfft(np.fft.rfft(mono, fft_size) * (1.0 + 0.35 * bell), fft_size)[:len(mono)]
# Soft limiting raises the body without clipping the transient.
processed = np.tanh(weighted * 1.5)
processed[-int(rate * 0.04):] *= np.linspace(1, 0, int(rate * 0.04))
processed *= 0.98 / np.max(np.abs(processed))
pcm = np.round(processed * 32767).astype("<i2")
with wave.open(str(output), "wb") as audio:
    audio.setnchannels(1); audio.setsampwidth(2); audio.setframerate(rate); audio.writeframes(pcm.tobytes())
report = {
    "source": "https://soundbible.com/1467-Grenade-Explosion.html",
    "author": "Mike Koenig", "license": "CC BY 3.0",
    "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
    "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
    "sample_rate": rate, "channels": 1, "seconds": len(mono) / rate,
    "source_peak": float(np.max(np.abs(mono))), "processed_peak": float(np.max(np.abs(pcm / 32768.0))),
    "source_attack_rms": float(np.sqrt(np.mean(mono[:int(rate * .2)] ** 2))),
    "processed_attack_rms": float(np.sqrt(np.mean(processed[:int(rate * .2)] ** 2))),
    "processing": "Stereo downmix; +2.6 dB broad 85 Hz bell; soft limiter; 40 ms tail fade; peak 0.98; PCM16 mono, original duration."
}
(here / "processing.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
