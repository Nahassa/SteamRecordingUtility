# Anti-patterns

Defects that have actually shipped in this repository. Each entry is a regression test
waiting to be written. Do not reintroduce them, and treat their presence in a diff as a
blocking review comment.

---

### Fallback decided, then ignored

Encoder selection logged "AV1 NVENC not available, falling back to hevc_nvenc", then the
argument builder checked a mode flag *first* and emitted `-c:v av1_nvenc` regardless. The
`encoder` parameter was accepted and discarded.

**Rule:** the resolved encoder is the single input to argument construction. No later branch
may re-derive it from a mode flag or setting.

### Capability inferred from a substring match

`ffmpeg -encoders` output was searched for `"hevc_nvenc"` and the result treated as "the GPU
can do this". That string is present on any machine whose FFmpeg was built with NVENC,
including machines with no NVIDIA hardware at all.

**Rule:** capability comes from a trial encode. Compile-time support is not runtime support.

### SAR computed from the target, applied to the source

"Preserve pixels" mode computed the sample aspect ratio from the *configured output*
resolution, then applied it to a file it had never measured. For any source not already
exactly that size, the tag was wrong — and because the code never probed dimensions, it also
logged a reassuring message naming a resolution the file did not have.

**Rule:** geometry maths reads probed source values. If a value has not been measured, it
cannot appear in a formula.

### Audio re-encoded by omission

No `-c:a` flag anywhere. FFmpeg therefore applied the container default, re-encoding audio
to AAC at ~128 kbps on every single conversion — a generation loss on every file, invisible
in the logs. Absent `-map 0`, additional audio tracks were dropped entirely.

**Rule:** stream handling is always explicit. `-map 0 -c:a copy -c:s copy`. Silence in the
command means a default you did not choose.

### Bit depth truncated by a filter

`format=nv12` was inserted to satisfy `hwupload_cuda`. NV12 is 8-bit, so this quietly
destroyed 10-bit sources before they reached the encoder, in service of a GPU scaler that
was lower quality than the CPU path it replaced.

**Rule:** a format conversion inserted to satisfy a downstream filter must not reduce
precision. If it must, the filter does not belong on the quality path.

### Undrained pipe before WaitForExit

Three helpers set `RedirectStandardError = true`, never read the stream, then called
`WaitForExit()`. This deadlocks whenever FFmpeg's output exceeds the pipe buffer.

**Rule:** every redirected stream is read to completion before waiting. Prefer
`WaitForExitAsync` with both streams consumed concurrently.

### Cancellation misread as failure

Cancelling killed FFmpeg, which exited non-zero, which matched the "GPU scaling failed"
condition — so the app immediately launched a second full encode on the CPU.

**Rule:** check the cancellation token before interpreting an exit code. Cancellation is a
distinct outcome from failure and must not trigger retry or fallback logic.

### Success assumed from exit code, then the original deleted

Exit code 0 moved the source file into `processed/` with no verification that the output was
complete or playable.

**Rule:** verify before destroying. Output exists, is non-empty, duration within tolerance.
Encode to a temporary name and rename on success, so a partial file is never mistaken for a
finished one.

### Output path derived from input filename with `-y`

`Path.Combine(outputFolder, fileName)` plus `-y`. When input and output folders match, the
tool overwrites the file it is reading.

**Rule:** compare canonicalised paths and refuse before starting. Never rely on the user not
choosing the same folder twice.

### Command assembled by string concatenation

Arguments were built with `$"-i \"{inputPath}\" ..."`. A filename containing a quote breaks
the command; nothing escapes filter-special characters.

**Rule:** `ProcessStartInfo.ArgumentList`, one token per element. The OS handles quoting.

### Progress parsed by scraping stderr one character at a time

`RunFFmpegAsync` read stderr via `StandardError.Read()` in a character loop, rebuilt lines on
`\r`, string-matched for `time=`, and marshalled to the UI thread from inside the loop —
process runner, progress parser, and UI updater fused into one method.

**Rule:** `-progress pipe:1 -nostats` gives structured `key=value` output on stdout. Parse
that. Keep execution, parsing, and reporting in separate units.

### Duplicated filter-building logic in a fallback path

The CPU fallback re-implemented ~18 lines of filter construction, and drifted: `setdar=16/9`
in one path, `setdar=16:9` in the other. A dead `if/else` sat above it whose two branches
were byte-identical.

**Rule:** one filter-graph builder, parameterised. A fallback selects different *inputs* to
that builder, never a second copy of it.

### A control that does nothing

The main window's resolution dropdown had no handler attached and was read by nothing.
Changing it appeared to work and had no effect on output.

**Rule:** every control either has a binding or does not exist. A silently inert input is
worse than a missing one.

### Two sources of truth for one policy

`chkMoveProcessed.Checked` and `settings.MoveProcessedFiles` both expressed "move processed
files"; the conversion loop read the checkbox, so the persisted setting was decorative.

**Rule:** settings are the model. UI binds to the model. Business logic reads the model and
never a control.

### Business logic reading the UI

`ConvertVideosAsync` took its input and output folders from `TextBox.Text` and its file
policy from a `CheckBox`, wrote progress into `Label` and `ProgressBar`, cleared four
`PictureBox` references, and logged by mutating a `RichTextBox`.

**Rule:** the pipeline takes a request object and reports through `IProgress<T>` and a
logging abstraction. It must compile without a WinForms reference — enforce this by putting
it in a project that does not have one.
