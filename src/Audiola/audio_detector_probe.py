#!/usr/bin/env python3
import argparse
import json
import subprocess
from pathlib import Path

VARIANTS = {
    "00": ("reference_clean", ""),
    "01": ("metadata_clean_resample_441", "aresample=44100"),

    "02": (
        "tiny_noise_floor",
        "anoisesrc=color=white:amplitude=0.00035:d=999[a];"
        "[0:a][a]amix=inputs=2:duration=first:weights=1 0.25[out]"
    ),

    "03": (
        "pink_noise_floor",
        "anoisesrc=color=pink:amplitude=0.00045:d=999[a];"
        "[0:a][a]amix=inputs=2:duration=first:weights=1 0.22[out]"
    ),

    "04": ("air_rolloff_18k", "lowpass=f=18000"),
    "05": ("air_rolloff_16k", "lowpass=f=16000"),

    "06": (
        "dynamics_loosen",
        "acompressor=threshold=-12dB:ratio=1.15:attack=25:release=180:knee=4:makeup=1,"
        "alimiter=limit=0.985"
    ),

    "07": ("stereo_narrow", "stereotools=mlev=1:slev=0.72"),

    "08": (
        "transient_soften",
        "acompressor=threshold=-18dB:ratio=1.45:attack=3:release=70:knee=2:makeup=1,"
        "alimiter=limit=0.985"
    ),

    "09": ("tape_soften", "aecho=0.8:0.88:12:0.06,lowpass=f=17500"),
    "10": ("reverb_change", "aecho=0.8:0.88:45:0.08"),
    "11": ("mono_compat", "pan=stereo|c0=0.65*c0+0.35*c1|c1=0.35*c0+0.65*c1"),

    "12": (
        "reamp_sim",
        "highpass=f=35,lowpass=f=15500,"
        "acompressor=threshold=-14dB:ratio=1.2:attack=15:release=120:knee=3:makeup=1"
    ),

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

    "23": ("resample_441_then_float", "aresample=44100,aresample=48000"),
    "24": ("micro_pitch_shift", "asetrate=48000*1.002,aresample=48000,atempo=0.998003992"),
    "25": ("micro_timing_shift", "atempo=1.003,asetrate=48000*0.997,aresample=48000"),

    "26": (
        "vinyl_noise_subtle",
        "anoisesrc=color=pink:amplitude=0.00025:d=999[n];"
        "[0:a][n]amix=inputs=2:duration=first:weights=1 0.18[out]"
    ),

    "27": ("phase_decorrelate_subtle", "stereotools=phase=8:mlev=1:slev=0.9"),
    "28": ("mid_side_rebalance", "stereotools=mlev=0.96:slev=0.78"),

    "29": (
        "soft_clip_safe",
        "acompressor=threshold=-10dB:ratio=1.08:attack=30:release=160:knee=5:makeup=1,"
        "alimiter=limit=0.972"
    ),

    "30": (
        "telephone_band_tiny_parallel",
        "asplit=2[dry][wet];"
        "[wet]highpass=f=250,lowpass=f=3800,volume=0.045[wet2];"
        "[dry][wet2]amix=inputs=2:duration=first:weights=1 1[out]"
    ),

    "31": (
        "room_tiny_parallel",
        "asplit=2[dry][wet];"
        "[wet]aecho=0.8:0.86:65:0.045,lowpass=f=9000[wet2];"
        "[dry][wet2]amix=inputs=2:duration=first:weights=1 1[out]"
    ),

    "32": (
        "combined_quality_preserving",
        "highpass=f=28,lowpass=f=17800,"
        "acompressor=threshold=-13dB:ratio=1.1:attack=24:release=180:knee=5:makeup=1,"
        "stereotools=mlev=0.98:slev=0.8,"
        "aecho=0.82:0.88:22:0.028,"
        "alimiter=limit=0.985"
    ),

    "33": (
        "combined_heavy_fingerprint_shift",
        "highpass=f=38,lowpass=f=14500,"
        "acompressor=threshold=-15dB:ratio=1.25:attack=10:release=105:knee=3:makeup=1,"
        "stereotools=mlev=0.94:slev=0.58,"
        "aecho=0.75:0.82:38:0.065,"
        "alimiter=limit=0.965"
    ),
       "40": (
        "suno_cleanup_subtle",
        "highpass=f=32,"
        "lowpass=f=16800,"
        "equalizer=f=3500:t=q:w=1.2:g=-0.8,"
        "equalizer=f=7200:t=q:w=1.1:g=-1.2,"
        "equalizer=f=11500:t=q:w=1.4:g=-1.0,"
        "acompressor=threshold=-14dB:ratio=1.12:attack=18:release=150:knee=4:makeup=1,"
        "stereotools=mlev=0.98:slev=0.74,"
        "aecho=0.82:0.88:19:0.025,"
        "alimiter=limit=0.982"
    ),

    "41": (
        "suno_cleanup_medium",
        "highpass=f=38,"
        "lowpass=f=15500,"
        "equalizer=f=2800:t=q:w=1.1:g=-0.8,"
        "equalizer=f=5200:t=q:w=1.3:g=-1.2,"
        "equalizer=f=8500:t=q:w=1.4:g=-1.8,"
        "equalizer=f=12500:t=q:w=1.2:g=-1.4,"
        "acompressor=threshold=-15dB:ratio=1.18:attack=14:release=125:knee=3:makeup=1,"
        "stereotools=phase=6:mlev=0.96:slev=0.64,"
        "aecho=0.78:0.84:31:0.045,"
        "alimiter=limit=0.972"
    ),

    "42": (
        "suno_cleanup_heavy",
        "highpass=f=45,"
        "lowpass=f=13800,"
        "equalizer=f=2400:t=q:w=1.0:g=-1.0,"
        "equalizer=f=4100:t=q:w=1.2:g=-1.5,"
        "equalizer=f=7600:t=q:w=1.5:g=-2.2,"
        "equalizer=f=10800:t=q:w=1.4:g=-2.0,"
        "acompressor=threshold=-16dB:ratio=1.28:attack=9:release=95:knee=3:makeup=1,"
        "stereotools=phase=10:mlev=0.94:slev=0.52,"
        "aecho=0.74:0.80:42:0.065,"
        "alimiter=limit=0.958"
    ),

    "43": (
        "suno_cleanup_parallel_room",
        "asplit=2[dry][wet];"
        "[wet]highpass=f=180,lowpass=f=8200,aecho=0.78:0.84:72:0.055,volume=0.08[wet2];"
        "[dry]highpass=f=32,lowpass=f=16500,equalizer=f=7800:t=q:w=1.3:g=-1.4[dry2];"
        "[dry2][wet2]amix=inputs=2:duration=first:weights=1 1[out]"
    ),

    "44": (
        "suno_cleanup_resample_phase",
        "aresample=44100,"
        "aresample=48000,"
        "stereotools=phase=7:mlev=0.97:slev=0.70,"
        "lowpass=f=16200,"
        "alimiter=limit=0.98"
    ),

    "45": (
        "suno_cleanup_best_quality",
        "highpass=f=30,"
        "lowpass=f=17200,"
        "equalizer=f=6500:t=q:w=1.2:g=-0.9,"
        "equalizer=f=10500:t=q:w=1.5:g=-1.1,"
        "acompressor=threshold=-13dB:ratio=1.1:attack=22:release=170:knee=5:makeup=1,"
        "stereotools=mlev=0.99:slev=0.78,"
        "aecho=0.84:0.89:17:0.022,"
        "alimiter=limit=0.984"
    )
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
        str(path),
    ]

    try:
        p = subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        return json.loads(p.stdout)
    except Exception as e:
        return {"error": str(e)}


def make_variant(input_path, output_path, fmt, filtergraph, mp3_bitrate):
    if filtergraph and "[out]" in filtergraph:
        cmd = [
            "ffmpeg", "-hide_banner", "-y",
            "-i", str(input_path),
            "-map_metadata", "-1",
            "-map_chapters", "-1",
            "-vn", "-sn",
            "-bitexact",
            "-filter_complex", filtergraph,
            "-map", "[out]",
        ]
    elif filtergraph:
        cmd = [
            "ffmpeg", "-hide_banner", "-y",
            "-i", str(input_path),
            "-map_metadata", "-1",
            "-map_chapters", "-1",
            "-vn", "-sn",
            "-bitexact",
            "-af", filtergraph,
        ]
    else:
        cmd = [
            "ffmpeg", "-hide_banner", "-y",
            "-i", str(input_path),
            "-map_metadata", "-1",
            "-map_chapters", "-1",
            "-vn", "-sn",
            "-bitexact",
        ]

    if fmt == "wav":
        cmd += ["-ac", "2", "-c:a", "pcm_f32le", str(output_path)]
    elif fmt == "flac":
        cmd += ["-ac", "2", "-c:a", "flac", "-compression_level", "5", str(output_path)]
    elif fmt == "mp3":
        cmd += [
            "-ac", "2",
            "-c:a", "libmp3lame",
            "-b:a", mp3_bitrate,
            "-write_id3v1", "0",
            "-id3v2_version", "0",
            str(output_path),
        ]
    else:
        raise ValueError(f"Unsupported format: {fmt}")

    return run(cmd)


def main():
    parser = argparse.ArgumentParser(
        description="Create audio variants for AI-detector false-positive testing."
    )

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