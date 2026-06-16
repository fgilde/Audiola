#!/usr/bin/env python3
"""Audiola lokale Voice-Engine (VoiceBox-artig).

Subcommands (JSON nach stdout, Fortschritt nach stderr):
  list-models  --models-dir DIR
  download     --model ID --models-dir DIR [--device auto|cuda|cpu]
  tts          --model ID --text T --language de --out OUT.wav [--speaker REF.wav] [--device ..] [--speed 1.0]
  transcribe   --input IN.wav --model base --device auto         -> {"segments":[{start,end,text}]}
  vc           (noch nicht unterstützt)

Abhängigkeiten (je nach Nutzung):
  pip install faster-whisper          # Transkription
  pip install TTS                     # Coqui XTTS v2 (Cloning)
  pip install kokoro soundfile        # Kokoro TTS
  pip install torch                   # GPU-Erkennung / Backend
"""
import argparse
import json
import os
import sys

CATALOG = [
    {"id": "kokoro",     "name": "Kokoro 82M",            "description": "Schnelles Preset-TTS (CPU-tauglich). pip install kokoro soundfile.", "capability": "tts",  "sizeMb": 330},
    {"id": "qwen3-tts",  "name": "Qwen3-TTS 1.7B",        "description": "Mehrsprachiges Cloning. pip install qwen-tts.",                       "capability": "both", "sizeMb": 1700},
    {"id": "xtts-v2",    "name": "Coqui XTTS v2",         "description": "Mehrsprachig, Zero-Shot-Cloning. pip install TTS.",                   "capability": "both", "sizeMb": 1900},
    {"id": "chatterbox", "name": "Chatterbox",            "description": "Cloning aus Sample. pip install chatterbox-tts.",                     "capability": "both", "sizeMb": 3000},
    {"id": "seed-vc",    "name": "seed-vc (Stimmtausch)", "description": "Zero-Shot Voice Conversion für Sprache + Gesang — erhält Melodie/Timing.", "capability": "clone", "sizeMb": 1500},
]

WHISPER_CATALOG = [
    {"id": "whisper-tiny",     "name": "Whisper tiny",     "sizeMb": 75},
    {"id": "whisper-base",     "name": "Whisper base",     "sizeMb": 145},
    {"id": "whisper-small",    "name": "Whisper small",    "sizeMb": 480},
    {"id": "whisper-medium",   "name": "Whisper medium",   "sizeMb": 1500},
    {"id": "whisper-large-v3", "name": "Whisper large-v3", "sizeMb": 3000},
    {"id": "whisper-turbo",    "name": "Whisper turbo",    "sizeMb": 1600},
]


def whisper_size(model_id):
    return model_id[len("whisper-"):] if model_id.startswith("whisper-") else model_id


# Qwen3-TTS erwartet ausgeschriebene Sprachnamen statt ISO-Codes.
QWEN_LANG = {
    "de": "german", "german": "german", "deutsch": "german",
    "en": "english", "english": "english",
    "fr": "french", "french": "french",
    "it": "italian", "italian": "italian",
    "es": "spanish", "spanish": "spanish",
    "pt": "portuguese", "portuguese": "portuguese",
    "ru": "russian", "russian": "russian",
    "ja": "japanese", "japanese": "japanese",
    "ko": "korean", "korean": "korean",
    "zh": "chinese", "cn": "chinese", "chinese": "chinese",
}


def qwen_lang(code):
    return QWEN_LANG.get((code or "auto").strip().lower(), "auto")


def log(*a):
    print(*a, file=sys.stderr, flush=True)


def resolve_device(d):
    if d and d != "auto":
        return d
    try:
        import torch
        return "cuda" if torch.cuda.is_available() else "cpu"
    except Exception:
        return "cpu"


def marker(models_dir, model_id):
    return os.path.join(models_dir, f"{model_id}.installed")


def seedvc_repo(models_dir):
    return os.path.join(models_dir, "seed-vc")


def is_installed(models_dir, model_id):
    if not models_dir:
        return False
    if model_id == "seed-vc":
        return os.path.isfile(os.path.join(seedvc_repo(models_dir), "inference.py"))
    return os.path.exists(marker(models_dir, model_id))


def cmd_list_models(args):
    models = [{**m, "installed": is_installed(args.models_dir, m["id"])} for m in CATALOG]
    whisper = [{**w, "installed": is_installed(args.models_dir, w["id"])} for w in WHISPER_CATALOG]
    print(json.dumps({"models": models, "whisper": whisper, "device": resolve_device(args.device)}))


def cmd_download(args):
    os.makedirs(args.models_dir, exist_ok=True)
    dev = resolve_device(args.device)
    log(f"Lade Modell '{args.model}' ({dev}) …")
    try:
        if args.model == "xtts-v2":
            from TTS.api import TTS  # noqa
            TTS("tts_models/multilingual/multi-dataset/xtts_v2")
        elif args.model == "kokoro":
            from kokoro import KPipeline  # noqa
            KPipeline(lang_code="a")
        elif args.model == "chatterbox":
            from chatterbox.tts import ChatterboxTTS  # noqa
            ChatterboxTTS.from_pretrained(device=("cuda" if dev == "cuda" else "cpu"))
        elif args.model == "qwen3-tts":
            _load_qwen("cpu")  # nur Download anstoßen
        elif args.model.startswith("whisper-"):
            from faster_whisper import WhisperModel
            WhisperModel(whisper_size(args.model),
                         device=("cuda" if dev == "cuda" else "cpu"),
                         compute_type=("float16" if dev == "cuda" else "int8"))
        elif args.model == "seed-vc":
            if not os.path.isfile(os.path.join(seedvc_repo(args.models_dir), "inference.py")):
                log("seed-vc bitte einmalig in den Modellordner klonen: "
                    f"git clone https://github.com/Plachtaa/seed-vc \"{seedvc_repo(args.models_dir)}\" "
                    "und dort pip install -r requirements.txt. Die Gewichte lädt seed-vc beim ersten Lauf.")
                sys.exit(1)
            log("seed-vc ist vorhanden.")
            return
        else:
            log(f"Unbekanntes Modell: {args.model}")
            sys.exit(2)
        open(marker(args.models_dir, args.model), "w").close()
        log("Fertig.")
    except ImportError as e:
        log(f"Paket fehlt: {e}. Bitte das jeweilige pip-Paket installieren (siehe Beschreibung).")
        sys.exit(1)
    except Exception as e:
        log(f"Fehler beim Download: {e}")
        sys.exit(1)


def _write_wav(path, samples, sr):
    import soundfile as sf
    sf.write(path, samples, sr)


def _load_qwen(device_map):
    """Lädt das Qwen3-TTS-Modell (Paket `qwen-tts`)."""
    try:
        from qwen_tts import Qwen3TTSModel
    except ImportError:
        from qwen_tts.inference.qwen3_tts_model import Qwen3TTSModel
    import torch
    dtype = torch.bfloat16 if device_map != "cpu" else torch.float32
    return Qwen3TTSModel.from_pretrained("Qwen/Qwen3-TTS-12Hz-1.7B-Base", device_map=device_map, dtype=dtype)


def cmd_tts(args):
    dev = resolve_device(args.device)
    log(f"TTS '{args.model}' auf {dev} …")
    try:
        if args.model == "xtts-v2":
            from TTS.api import TTS
            tts = TTS("tts_models/multilingual/multi-dataset/xtts_v2").to(dev)
            if not args.speaker:
                raise RuntimeError("XTTS benötigt ein Referenz-Sample (--speaker).")
            tts.tts_to_file(text=args.text, speaker_wav=args.speaker,
                            language=args.language or "de", file_path=args.out)
        elif args.model == "kokoro":
            import numpy as np
            from kokoro import KPipeline
            pipe = KPipeline(lang_code=(args.language or "a")[0])
            audio = np.concatenate([chunk.audio for chunk in pipe(args.text, voice="af_heart")])
            _write_wav(args.out, audio, 24000)
        elif args.model == "chatterbox":
            import torchaudio
            from chatterbox.tts import ChatterboxTTS
            m = ChatterboxTTS.from_pretrained(device=("cuda" if dev == "cuda" else "cpu"))
            wav = m.generate(args.text, audio_prompt_path=args.speaker) if args.speaker else m.generate(args.text)
            torchaudio.save(args.out, wav, m.sr)
        elif args.model == "qwen3-tts":
            if not args.speaker:
                raise RuntimeError("Qwen3-TTS-Cloning benötigt ein Referenz-Sample (--speaker).")
            model = _load_qwen("cuda:0" if dev == "cuda" else "cpu")
            # x_vector_only_mode=True: klont nur über den Stimm-Embedding, ohne Referenztext.
            wavs, sr = model.generate_voice_clone(
                text=args.text, language=qwen_lang(args.language),
                ref_audio=args.speaker, x_vector_only_mode=True)
            audio = wavs[0] if isinstance(wavs, list) else wavs
            _write_wav(args.out, audio, sr)
        else:
            raise RuntimeError(
                f"TTS-Modell '{args.model}' wird vom lokalen Backend nicht unterstützt. "
                "Für eigene (geklonte) Stimmen: XTTS v2 oder Chatterbox. Für Presets: Kokoro.")
        log("Fertig.")
    except Exception as e:
        log(f"TTS-Fehler: {e}")
        sys.exit(1)


def cmd_transcribe(args):
    dev = resolve_device(args.device)
    log(f"Transkribiere mit Whisper '{args.model}' auf {dev} …")
    try:
        from faster_whisper import WhisperModel
        compute = "float16" if dev == "cuda" else "int8"
        model = WhisperModel(args.model, device=dev, compute_type=compute)
        segments, _ = model.transcribe(args.input, vad_filter=True)
        out = [{"start": float(s.start), "end": float(s.end), "text": s.text} for s in segments]
        print(json.dumps({"segments": out}))
    except Exception as e:
        log(f"Transkriptions-Fehler: {e}")
        sys.exit(1)


def cmd_gpu_check(args):
    info = {"torch": False, "cuda": False, "device_name": "", "torch_version": ""}
    try:
        import torch
        info["torch"] = True
        info["torch_version"] = torch.__version__
        info["cuda"] = bool(torch.cuda.is_available())
        if info["cuda"]:
            info["device_name"] = torch.cuda.get_device_name(0)
    except Exception as e:
        info["error"] = str(e)
    print(json.dumps(info))


def cmd_vc(args):
    """Speech-to-Speech / Singing Voice Conversion.

    Primär: seed-vc (zero-shot, Sprache + Gesang, erhält Melodie & Timing).
      git clone https://github.com/Plachtaa/seed-vc <models-dir>/seed-vc  (+ requirements)
    Fallback: OpenVoice (nur Sprache), falls als Paket installiert.
    """
    dev = resolve_device(args.device)
    if not args.speaker:
        log("Kein Referenz-Sample (--speaker) für den Stimmtausch angegeben.")
        sys.exit(1)

    # 1) seed-vc (bevorzugt – auch Gesang)
    inf = os.path.join(seedvc_repo(args.models_dir), "inference.py")
    if os.path.isfile(inf):
        import subprocess, tempfile, glob, shutil
        work = tempfile.mkdtemp(prefix="seedvc_")
        try:
            steps = int(args.diffusion_steps)
        except (TypeError, ValueError):
            steps = 30
        if dev != "cuda":
            steps = min(steps, 15)  # CPU: deckeln, sonst extrem langsam
            log("Hinweis: seed-vc läuft auf CPU sehr langsam — für brauchbare Geschwindigkeit CUDA-GPU nutzen.")
        auto_f0 = "True" if str(args.auto_f0_adjust).strip().lower() in ("1", "true", "yes") else "False"
        log(f"Stimmtausch via seed-vc auf {dev} (Schritte={steps}, auto-f0={auto_f0}) …")
        cmd = [sys.executable, "-u", inf,
               "--source", args.input, "--target", args.speaker, "--output", work,
               "--diffusion-steps", str(steps), "--f0-condition", "True", "--auto-f0-adjust", auto_f0]
        # Ausgabe live durchreichen, damit man den Fortschritt sieht (kein stilles Puffern).
        proc = subprocess.Popen(cmd, cwd=seedvc_repo(args.models_dir),
                                stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1)
        for line in proc.stdout:
            log(line.rstrip())
        proc.wait()
        wavs = sorted(glob.glob(os.path.join(work, "*.wav")), key=os.path.getmtime)
        if proc.returncode != 0 or not wavs:
            log(f"seed-vc inference fehlgeschlagen (Code {proc.returncode}).")
            sys.exit(1)
        shutil.copy(wavs[-1], args.out)
        log("Fertig.")
        return

    # 2) OpenVoice (nur Sprache)
    try:
        from openvoice.api import ToneColorConverter
        from openvoice import se_extractor
        log(f"Stimmtausch via OpenVoice (nur Sprache) auf {dev} …")
        ckpt = os.path.join(args.models_dir, "openvoice", "converter")
        converter = ToneColorConverter(os.path.join(ckpt, "config.json"), device=dev)
        converter.load_ckpt(os.path.join(ckpt, "checkpoint.pth"))
        src_se, _ = se_extractor.get_se(args.input, converter, vad=True)
        tgt_se, _ = se_extractor.get_se(args.speaker, converter, vad=True)
        converter.convert(audio_src_path=args.input, src_se=src_se, tgt_se=tgt_se, output_path=args.out)
        log("Fertig.")
        return
    except ImportError:
        pass
    except Exception as e:
        log(f"OpenVoice-Fehler: {e}")
        sys.exit(1)

    log("Kein lokales Stimmtausch-Backend gefunden. Für Gesang+Sprache seed-vc installieren: "
        f"git clone https://github.com/Plachtaa/seed-vc \"{seedvc_repo(args.models_dir)}\" "
        "(+ pip install -r requirements.txt). Alternativ OpenVoice für reine Sprache.")
    sys.exit(1)


def main():
    p = argparse.ArgumentParser()
    sub = p.add_subparsers(dest="cmd", required=True)

    lm = sub.add_parser("list-models"); lm.add_argument("--models-dir", default=""); lm.add_argument("--device", default="auto")
    dl = sub.add_parser("download"); dl.add_argument("--model", required=True); dl.add_argument("--models-dir", default=""); dl.add_argument("--device", default="auto")
    tt = sub.add_parser("tts")
    tt.add_argument("--model", required=True); tt.add_argument("--text", required=True); tt.add_argument("--language", default="de")
    tt.add_argument("--speaker", default=""); tt.add_argument("--out", required=True); tt.add_argument("--device", default="auto")
    tt.add_argument("--models-dir", default=""); tt.add_argument("--speed", default="1.0")
    tr = sub.add_parser("transcribe"); tr.add_argument("--input", required=True); tr.add_argument("--model", default="base"); tr.add_argument("--device", default="auto")
    vc = sub.add_parser("vc"); vc.add_argument("--input", required=True); vc.add_argument("--speaker", default=""); vc.add_argument("--out", required=True); vc.add_argument("--device", default="auto"); vc.add_argument("--models-dir", default=""); vc.add_argument("--diffusion-steps", default="30"); vc.add_argument("--auto-f0-adjust", default="False")
    sub.add_parser("gpu-check")

    args = p.parse_args()
    {
        "list-models": cmd_list_models,
        "download": cmd_download,
        "tts": cmd_tts,
        "transcribe": cmd_transcribe,
        "vc": cmd_vc,
        "gpu-check": cmd_gpu_check,
    }[args.cmd](args)


if __name__ == "__main__":
    main()
