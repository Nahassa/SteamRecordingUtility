# Colour, bit depth, and geometry

## Bit depth

Resolve output bit depth from the probe, never from a constant:

| Source `pix_fmt` / `bits_per_raw_sample` | Output |
|---|---|
| `yuv420p`, 8 | `yuv420p10le` (preferred) or `yuv420p` |
| `yuv420p10le`, `p010le`, 10 | 10-bit — mandatory |
| 12-bit or higher | 10-bit minimum; do not drop to 8 |

Encoding 8-bit source at 10-bit is a real gain for HEVC and AV1: the extra precision in the
transform reduces banding in gradients (skies, smoke, fades) at effectively no cost. It is
the recommended default, not a special case.

**Format names are encoder-specific.** Software encoders take `yuv420p10le`; NVENC takes
`p010le`. `nv12` is 8-bit only — inserting `format=nv12` anywhere in a filter chain silently
truncates a 10-bit source, which is how this codebase previously lost bit depth.

## Colour metadata

Filters do not reliably carry colour tags across format conversions. Tag the output
explicitly, with values read from the probe:

```
-color_primaries PRI -color_trc TRC -colorspace MTX -color_range RNG
```

Common combinations:

| Content | primaries | trc | colorspace | range |
|---|---|---|---|---|
| SDR HD/4K | `bt709` | `bt709` | `bt709` | `tv` |
| HDR10 | `bt2020` | `smpte2084` | `bt2020nc` | `tv` |
| HLG | `bt2020` | `arib-std-b67` | `bt2020nc` | `tv` |

If the probe reports `unknown` for a 1080p+ SDR source, `bt709`/`tv` is the correct
assumption — but log that an assumption was made. Leaving the fields unspecified is not an
acceptable outcome; it pushes the guess onto every downstream player, which is how footage
ends up looking different in different apps.

For **libx265**, container-level flags are not sufficient for HDR. The metadata must also go
into the bitstream:

```
-x265-params "...:hdr-opt=1:repeat-headers=1:colorprim=bt2020:transfer=smpte2084:\
colormatrix=bt2020nc:master-display=G(...)B(...)R(...)WP(...)L(...):max-cll=NNN,NNN"
```

Carry `master-display` and `max-cll` from the source's side data when present. Losing them
turns correct HDR into over-bright, wrongly-mapped HDR.

## HDR

Two acceptable outcomes. Silently producing the third is a bug.

1. **Preserve** — 10-bit, bt2020/PQ tagging intact, mastering metadata carried. Default.
2. **Tone-map deliberately** — a real operator, logged as a conversion:
   ```
   zscale=t=linear:npl=100,tonemap=hable:desat=0,zscale=p=bt709:t=bt709:m=bt709:r=tv,format=yuv420p10le
   ```
3. **~~Decode HDR, drop the tags, encode as if SDR~~** — produces the washed-out, grey,
   desaturated result. This is what happens by default when nothing is specified.

## Scaling

**First decide whether to scale at all.** If target dimensions equal source dimensions,
emit no scale filter. A resample is a lossy operation; performing one to reach the size the
file already is, is pure degradation.

Preferred, when `zscale` is available (requires `--enable-libzimg` — probe `ffmpeg -filters`):

```
zscale=w=W:h=H:f=lanczos:dither=error_diffusion
```

`zscale` is colour-aware and works at higher internal precision than `swscale`. Fallback:

```
scale=W:H:flags=lanczos+accurate_rnd+full_chroma_int
```

**Do not use `scale_cuda` on the quality path.** It is 8-bit NV12-limited in most builds,
lower quality than either option above, and reaching it requires `format=nv12` + `hwupload`,
which destroys bit depth before the resize. It belongs behind an explicit "prioritise speed"
toggle or nowhere.

## Aspect ratio

**Never emit an unconditional `setdar` or `-aspect`.** Forcing a display aspect that
disagrees with the pixel geometry stretches the picture.

Rules:

- Square pixels in, square pixels out. Do not invent a SAR.
- If SAR must be set, compute it from the **source** dimensions and the intended display
  aspect. Computing it from the *target* resolution — in a mode that does not resample — is
  meaningless, because the pixels are still the source's.
- To reach a fixed canvas without distortion:
  ```
  scale=W:H:force_original_aspect_ratio=decrease,pad=W:H:(ow-iw)/2:(oh-ih)/2
  ```
- To remove letterbox/pillarbox bars, detect the real picture bounds first:
  ```
  ffmpeg -i INPUT -vf cropdetect=limit=24:round=2:reset=0 -frames:v 300 -f null -
  ```
  Read the suggested `crop=w:h:x:y` from the output, sample at several points in the file
  (not just the opening frames, which are often black), and apply the crop. Cropping bars
  preserves geometry; stretching to fill does not.

## Colour adjustment (`eq`)

- **Skip entirely at identity values** (`brightness=0`, `contrast=1`, `saturation=1`). Do
  not pay a filter pass and a format conversion to change nothing.
- Run before any downscale, so the adjustment happens at full resolution.
- Give the filter headroom — convert up before, and dither down once at the end:
  ```
  format=yuv420p10le,eq=brightness=B:contrast=C:saturation=S
  ```
  Applying `eq` in 8-bit and then encoding introduces banding that the source did not have.
- Always format numbers with `CultureInfo.InvariantCulture`. A comma decimal separator
  produces a malformed filter string on non-English locales.
- Be wary of non-identity defaults. A default saturation of 1.2 means every file is
  silently altered; if a boost is wanted it should be a visible choice, not a default.
