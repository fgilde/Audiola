#!/usr/bin/env python3
import argparse
import json
import subprocess
from pathlib import Path

VARIANTS = {
    "00": ("reference_clean", ""),
    "01": ("metadata_clean_resample_441", "aresample=44100"),
    "02": ("tiny_noise_floor", "anoisesrc=color=white:amplitude=0.00035:d=999[a];[0:a][a]amix=inputs=2:duration=first:weights=1 0.25"),
    "03": ("pink_noise_floor", "anoisesrc=color=pink:amplitude=0.00045:d=999[a];[0:a][a]amix=inputs=2:duration=first:weights=1 0.22"),
    "04": ("air_rolloff_18k", "lowpass=f=18000"),
    "05": ("air_rolloff_16k", "lowpass=f=16000"),
    "06": ("dynamics_loosen", "acompressor=threshold=-12dB:ratio=1.15:attack=25:release=180:knee=4:makeup=1,alimiter=limit=0.985"),
    "07": ("stereo_narrow", "stereotools=mlev=1:slev=0.72"),
    "08": ("transient_soften", "acompressor=threshold=-18dB:ratio=1.45:attack=3:release=70:knee=2:makeup=1,alimiter=limit=0.985"),
    "09": ("tape_soften", "aecho=0.8:0.88:12:0.06,lowpass=f=17500"),
    "10": ("reverb_change", "aecho=0.8:0.88:45:0.08"),
    "11": ("mono_compat", "pan=stereo|c0=0.65*c0+0.35*c1|c1=0.35*c0+0.65*c1"),
    "12": ("reamp_sim", "highpass=f=35,lowpass=f=15500,acompressor=threshold=-14dB:ratio=1.2:attack=15:release=120:knee=3:makeup=1"),
        "20": (
        "combined_subtle_fingerprint_shift",
        "highpass=f=30,lowpass=f=17500,"
        "acompressor=threshold=-13dB:ratio=1.12:attack=22:release=170:knee=4:makeup=1,"
        "stereotools=mlev=1:slev=0.82,"
        "aecho=0.8:0.88:18:0.035,"
        "alimiter=limit=0.985"
    ),

    "21": (
        "combined_stronger_fingerprint_shift",
        "highpass=f=35,lowpass=f=16000,"
        "acompressor=threshold=-14dB:ratio=1.18:attack=18:release=140:knee=3:makeup=1,"
        "stereotools=mlev=1:slev=0.68,"
        "aecho=0.8:0.86:32:0.055,"
        "alimiter=limit=0.985"
    ),

    "22": (
        "combined_warm_reamp_style",
        "highpass=f=40,lowpass=f=15000,"
        "acompressor=threshold=-15dB:ratio=1.22:attack=12:release=110:knee=3:makeup=1,"
        "aecho=0.75:0.82:24:0.05,"
        "alimiter=limit=0.982"
    ),
}

def run(cmd):
    try:
        subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        return None
    except subprocess.CalledProcessError as e:
        return e.stderr or str(e)

def ffprobe_tags(path):
    cmd = [
        "ffprobe", "-hide_banner", "-v", "quiet",
        "-print_format", "json",
        "-show_format",
        str(path)
    ]
    try:
        p = subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        return json.loads(p.stdout)
    except Exception as e:
        return {"error": str(e)}

def make_variant(input_path, output_path, fmt, filtergraph, mp3_bitrate):
    cmd = [
        "ffmpeg", "-hide_banner", "-y",
        "-i", str(input_path),
        "-map_metadata", "-1",
        "-map_chapters", "-1",
        "-vn", "-sn",
        "-bitexact",
    ]

    if filtergraph:
        cmd += ["-filter_complex", filtergraph, "-map", "[out]" if "[out]" in filtergraph else "0:a"]
        if "[out]" not in filtergraph:
            cmd = [
                "ffmpeg", "-hide_banner", "-y",
                "-i", str(input_path),
                "-map_metadata", "-1",
                "-map_chapters", "-1",
                "-vn", "-sn",
                "-bitexact",
                "-af", filtergraph,
            ]

    if fmt == "wav":
        cmd += ["-c:a", "pcm_s24le", str(output_path)]
    elif fmt == "flac":
        cmd += ["-c:a", "flac", "-compression_level", "5", str(output_path)]
    elif fmt == "mp3":
        cmd += [
            "-c:a", "libmp3lame",
            "-b:a", mp3_bitrate,
            "-write_id3v1", "0",
            "-id3v2_version", "0",
            str(output_path)
        ]
    else:
        raise ValueError(f"Unsupported format: {fmt}")

    return run(cmd)

def main():
    parser = argparse.ArgumentParser(description="Create audio variants for AI-detector false-positive testing.")
    parser.add_argument("input", help="Input audio file, e.g. master.wav")
    parser.add_argument("-o", "--output", default="variants", help="Output folder")
    parser.add_argument("--formats", nargs="+", default=["wav"], choices=["wav", "flac", "mp3"])
    parser.add_argument("--mp3-bitrate", default="320k")
    parser.add_argument("--only", nargs="+", help="Only create selected variants, e.g. --only 00 04 07")
    parser.add_argument("--continue-on-error", action="store_true", help="Keep going if a variant fails")
    args = parser.parse_args()

    input_path = Path(args.input)
    out_dir = Path(args.output)
    out_dir.mkdir(parents=True, exist_ok=True)

    selected = args.only if args.only else list(VARIANTS.keys())

    report = {
        "input": str(input_path),
        "input_tags": ffprobe_tags(input_path),
        "outputs": [],
        "errors": [],
    }

    for key in selected:
        if key not in VARIANTS:
            msg = f"Unknown variant: {key}"
            if args.continue_on_error:
                report["errors"].append(msg)
                continue
            raise SystemExit(msg)

        name, filt = VARIANTS[key]

        for fmt in args.formats:
            out_path = out_dir / f"{key}_{name}.{fmt}"
            print(f"Creating {out_path}")

            err = make_variant(input_path, out_path, fmt, filt, args.mp3_bitrate)

            if err:
                report["errors"].append({
                    "variant": key,
                    "name": name,
                    "format": fmt,
                    "error": err,
                })
                print(f"FAILED {key}_{name}.{fmt}")
                if not args.continue_on_error:
                    raise SystemExit(err)
                continue

            report["outputs"].append({
                "variant": key,
                "name": name,
                "format": fmt,
                "file": str(out_path),
                "tags_after_export": ffprobe_tags(out_path),
            })

    report_path = out_dir / "audio_detector_probe_report.json"
    report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

    print(f"Done. Report written to {report_path}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())