---
name: video-encoding-policy
description: This project's house standard for building FFmpeg transcode commands. Use whenever writing, reviewing, or debugging any code that constructs FFmpeg or ffprobe arguments, chooses an encoder, builds a filter chain, handles pixel format / bit depth / colour space / HDR, sets resolution or aspect ratio, probes encoder capability, or moves and overwrites source video files. Covers libsvtav1, libx265, hevc_nvenc, av1_nvenc.
---

# Video encoding policy

The goal of this project is **minimal quality degradation, not speed.** Every rule below
follows from that. Where a rule costs encode time, that is an accepted trade.

## The one rule that generates the others

**Probe the source, then decide. Never assume.**

Historically every quality defect in this codebase traced back to a hardcoded assumption
about input that was never measured: 8-bit, 16:9, one audio track, NVENC present. A single
`ffprobe` call removes the entire class.

Before building any command, obtain a source description:

```
ffprobe -v error -print_format json -show_format -show_streams -show_entries \
  "stream=index,codec_type,codec_name,width,height,pix_fmt,bits_per_raw_sample,\
sample_aspect_ratio,display_aspect_ratio,color_range,color_space,color_transfer,\
color_primaries,r_frame_rate,avg_frame_rate,channels,channel_layout" -i INPUT
```

Every downstream decision — bit depth, colour tagging, scaling, stream mapping, whether to
scale at all — reads from that result. A function that builds encoder arguments without a
probe result in scope is wrong by construction.

## Invariants

These are not defaults. Breaking one is a bug.

1. **Never reduce bit depth.** 10-bit in, 10-bit out. Encoding 8-bit source *at* 10-bit is
   permitted and encouraged for HEVC/AV1 — it reduces banding and costs nothing at these
   presets.
2. **Never re-encode audio.** `-c:a copy`. Re-encode only when the target container cannot
   carry the source codec, and say so in the log when it happens.
3. **Never drop streams silently.** `-map 0` and explicit `-map_metadata 0 -map_chapters 0`.
   Steam captures routinely carry separate game and microphone tracks.
4. **Never distort geometry.** No unconditional `setdar` / `-aspect`. See
   `references/color-and-scaling.md`.
5. **Never lose colour metadata.** Primaries, transfer, matrix and range are tagged
   explicitly on output, carried from the probe. Never leave them unspecified.
6. **Never silently convert HDR to SDR.** Either preserve HDR end-to-end or tone-map
   deliberately through a real operator, as an explicit choice that is logged.
7. **Never skip the identity check.** If brightness/contrast/saturation are at identity
   values, omit the `eq` filter entirely rather than paying a filter pass for a no-op.
8. **Never write output over input.** Compare canonicalised full paths before starting.
9. **Never treat exit code 0 as success on its own.** Verify the output before moving or
   deleting an original. See "Safety" below.
10. **Never build the command as a concatenated string.** Use
    `ProcessStartInfo.ArgumentList`, one element per token. This eliminates the entire
    quoting/injection bug class — filenames with quotes, spaces, `%`, or filter-special
    characters stop being a hazard.

## Capability probing

`ffmpeg -encoders | grep nvenc` proves only that FFmpeg was **compiled** with NVENC. It
returns true on a machine with no NVIDIA GPU. It is not a capability check.

A real check is a trial encode:

```
ffmpeg -hide_banner -loglevel error -f lavfi -i nullsrc=s=256x256:d=0.1 \
  -c:v ENCODER [candidate options] -f null -
```

Rules:
- Probe the encoder **and the specific option set** you intend to use. `-tune uhq` exists
  only on recent builds; `scale_cuda`'s `interp_algo` only on some. Availability of the
  encoder does not imply availability of its flags.
- Cache results keyed by the output of `ffmpeg -version`, so a swapped binary re-probes.
- **Drain stdout and stderr before `WaitForExit()`.** Redirecting a pipe and not reading it
  deadlocks when FFmpeg's output exceeds the buffer. Read both to completion first.
- When a probe fails, fall back and **log the substitution**. A fallback the user cannot see
  is indistinguishable from a bug.
- The fallback decision must reach the argument builder. If selection resolves to
  `hevc_nvenc`, nothing downstream may still emit `av1_nvenc`.

## Encoder selection

Default order is quality-per-bitrate, since speed is explicitly not the goal:

1. `libsvtav1` — best quality per bit at `-preset 3..5`. Default.
2. `libx265` — `-preset slow`, when AV1 playback compatibility is a problem.
3. `av1_nvenc` / `hevc_nvenc` — explicit opt-in for speed. Meaningfully worse compression
   efficiency at equal size; correct only when encode time matters more than the file.

Exact flag templates for each: `references/encoders.md`.

## Command shape

Order matters; keep it consistent:

```
ffmpeg -hide_banner -loglevel error -progress pipe:1 -nostats
       -i INPUT
       [-filter_complex | -vf FILTERS]
       -map 0 -map_metadata 0 -map_chapters 0
       -c:v ENCODER [encoder options]
       [colour tagging]
       -c:a copy -c:s copy
       -fps_mode passthrough
       [container flags]
       OUTPUT.tmp
```

- **`-progress pipe:1 -nostats`** emits `key=value` lines (`frame=`, `out_time_us=`,
  `speed=`) on stdout. Parse that. Do not scrape stderr character-by-character for `\r`
  delimited progress — it is fragile, and it forces the parser and the process runner into
  one function.
- **`-fps_mode passthrough`** preserves original timing. Game capture is frequently VFR;
  forcing CFR duplicates frames or drifts audio sync.
- `-movflags +faststart` for MP4 output.
- `-tag:v hvc1` for HEVC in MP4, or the file will not play in QuickTime and several browsers.

## Safety

- Encode to a temporary name in the destination directory, then rename on success. A killed
  or failed encode must never leave a file that looks like a finished output.
- Cancellation is not failure. A cancelled encode exits non-zero; do not let that trigger a
  fallback path and start a second encode.
- Before moving or deleting a source file, verify the output: it exists, is non-empty, and
  its duration is within tolerance (~0.5s) of the source. Exit code 0 alone is not enough.
- `-y` combined with an output path derived from the input filename will destroy the source
  if the input and output folders are the same. Check for this explicitly and refuse.

## Verification

Since the whole point is minimal degradation, measure it rather than assuming it:

```
ffmpeg -i OUTPUT -i SOURCE -lavfi \
  "[0:v]setpts=PTS-STARTPTS[dist];[1:v]setpts=PTS-STARTPTS[ref];\
[dist][ref]libvmaf=n_threads=8:log_fmt=json:log_path=vmaf.json" -f null -
```

First input is the distorted file, second is the reference. Guidance: VMAF ≥ 95 is
visually transparent for game content; ≥ 98 for archival. If a settings change cannot be
shown to move this number, it is not a quality improvement.

## References

- `references/encoders.md` — per-encoder flag templates and rate control
- `references/color-and-scaling.md` — bit depth, colour metadata, HDR, scaling, aspect ratio
- `references/anti-patterns.md` — defects previously shipped in this codebase; do not reintroduce
