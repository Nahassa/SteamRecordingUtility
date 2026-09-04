# Encoder flag templates

Quality-first settings. All assume the source has been probed and `BITDEPTH` resolved to
either 8 or 10 (never lower than the source — see the bit-depth rule in SKILL.md).

Tokens in `CAPS` are substituted. Every flag below is one `ArgumentList` element per token.

---

## libsvtav1 — default

```
-c:v libsvtav1
-crf 20                    # 18 archival, 20 default, 24 space-conscious
-preset 4                  # 2-4 quality-first; 6+ trades visible quality
-svtav1-params tune=0:enable-overlays=1:scd=1:keyint=10s
-pix_fmt yuv420p10le       # yuv420p only if forced to 8-bit output
```

- **`tune=0` is not optional.** SVT-AV1 defaults to `tune=1`, which optimises for PSNR and
  looks measurably worse to a human. `tune=0` is the subjective-quality mode.
- `enable-overlays=1` improves quality across scene transitions.
- `film-grain` stays off for game content — it is a synthesis tool for film sources and will
  add noise that was never there.
- SVT-AV1 accepts 10-bit input directly and benefits from it even for 8-bit sources.
- Encoding at `-preset 3` roughly doubles time over `4` for a small gain; `-preset 2` is
  rarely worth it. Do not go above `6`.

## libx265

```
-c:v libx265
-crf 18                    # 16 archival, 18 default; HEVC CRF is not x264 CRF
-preset slow               # slower/veryslow give diminishing returns
-x265-params aq-mode=3:aq-strength=1.0:psy-rd=2.0:psy-rdoq=1.0:rdoq-level=2:\
bframes=8:ref=5:rc-lookahead=60:no-sao=1:strong-intra-smoothing=0:deblock=-1,-1
-profile:v main10          # main10 whenever BITDEPTH=10
-pix_fmt yuv420p10le
-tag:v hvc1                # MP4 only; without it QuickTime and several browsers refuse
```

- **Do not pass `-tune` for game content.** `psnr` and `ssim` tunes actively degrade
  perceptual quality; `grain` is for film grain retention and is wrong here. Omitting tune
  is the correct choice.
- `no-sao=1` matters for synthetic/UI-heavy content — SAO smears fine detail and text.
- `psy-rd`/`psy-rdoq` preserve perceived detail at the cost of PSNR. That trade is correct
  for this project and is exactly why PSNR-based tunes are banned above.
- `deblock=-1,-1` reduces over-smoothing slightly. Do not go below `-2,-2`.
- HDR metadata must be passed **inside** `-x265-params` to reach the bitstream SEI; the
  top-level `-color_*` flags alone only tag the container. See `color-and-scaling.md`.

## hevc_nvenc — opt-in

```
-c:v hevc_nvenc
-preset p7                 # p7 = slowest/best; anything below p5 is visibly worse
-tune hq
-rc vbr -cq 20 -b:v 0      # -b:v 0 is REQUIRED; without it a default bitrate cap applies
-multipass fullres
-rc-lookahead 32
-spatial-aq 1 -aq-strength 8
-temporal-aq 1
-bf 4 -b_ref_mode middle   # b_ref_mode is a significant, free quality gain
-g 250
-profile:v main10          # main10 whenever BITDEPTH=10
-pix_fmt p010le            # NVENC 10-bit surface format; NOT yuv420p10le
-tag:v hvc1
```

- **`-b:v 0` with `-rc vbr -cq N`** is the canonical constant-quality NVENC form. Omitting
  it silently caps the bitrate on some builds and is the most common cause of "NVENC looks
  bad" reports.
- **`-b_ref_mode middle`** enables B-frames as reference. Absent from most guides; it is one
  of the largest single quality wins available on NVENC and costs nothing.
- `-rc constqp -qp N` is an alternative for strictly constant quantiser. Prefer VBR+CQ.
- `-tune lossless` is incompatible with `-rc`/`-cq`. If lossless is selected, emit neither.
- NVENC pixel formats are `nv12` (8-bit) and `p010le` (10-bit). Passing `yuv420p10le` to an
  NVENC encoder is an error.

## av1_nvenc — opt-in, Ada/Blackwell only

```
-c:v av1_nvenc
-preset p7
-tune hq                   # -tune uhq ONLY if probed successfully; not on older builds
-rc vbr -cq 22 -b:v 0
-multipass fullres
-rc-lookahead 32
-spatial-aq 1 -temporal-aq 1
-bf 3 -b_ref_mode middle
-pix_fmt p010le
```

- **Verify AQ option spelling before emitting.** FFmpeg's NVENC encoders have historically
  exposed both `-spatial-aq`/`-temporal-aq` (hyphen) and `-spatial_aq`/`-temporal_aq`
  (underscore), but coverage differs between encoders and builds. Resolve it with
  `ffmpeg -h encoder=av1_nvenc` or a trial encode rather than hardcoding one spelling. Do
  not use different spellings in different code paths.
- `-tune uhq` requires NVENC SDK 12.2+ and a recent FFmpeg. Probe the exact option set; do
  not infer support from the encoder merely being listed.
- At equal file size, `libsvtav1 -preset 4` still beats `av1_nvenc -preset p7`. Choose this
  encoder for throughput, not for quality.

---

## Rate control quick reference

| Intent | Flags |
|---|---|
| Constant quality, software | `-crf N` |
| Constant quality, NVENC | `-rc vbr -cq N -b:v 0` |
| Constant quantiser, NVENC | `-rc constqp -qp N` |
| Capped quality, NVENC | `-rc vbr -cq N -b:v 0 -maxrate XM -bufsize 2XM` |

Never emit `-cq` together with `-rc cbr` — the value is ignored and the combination
signals a bug. Never set `-b:v` to a nonzero value alongside `-cq` unless deliberately
capping, and always validate `maxrate >= bitrate` before emitting either.

## Container notes

- MP4: `-movflags +faststart`. HEVC needs `-tag:v hvc1`. AV1 gets `av01` automatically.
- MKV: carries anything; preferred when preserving multiple audio tracks or unusual codecs.
- Choose the container from what the streams require, not from the input file's extension.
