# Brand resource tools

The [brand kit](../../brand/README.md#reproduce-the-kit) explains setup, source ownership,
licenses, optical sizes, offline generation and publication use. These tools are optional; the
application consumes checked-in assets without Python or network access.

- `prepare-fonts.py` builds static faces from bundled pinned sources. `--fetch` explicitly
  downloads those same revisions. Keep `brand/fonts/manifest.json` with source and derived hashes.
- `export.py` renders the SVG masters using resvg, outlines bundled lettering, builds ICO/ICNS
  and writes publication images plus `brand/exports/manifest.json`. No installed fonts are loaded.
- `check-windows-icons.py <Desktop-exe> <ignored-output> [--window HWND] [--shortcut file.lnk]`
  renders icons obtained from Windows APIs. Outputs are local verification evidence, not masters.
- `verify.py [--native <captured-icon-directory>]` checks retained hashes, static fonts/coverage,
  exact optical ICO frames and PNG-backed ICNS structure. Native comparison accounts for DIB
  premultiplied-alpha rounding; opacity and fully opaque pixels must match exactly.

The Windows check uses an explicitly supplied shortcut and does not create or change shortcuts.
The caller may create a disposable `.lnk` under `.artifacts/` for Shell extraction. EXE extraction
requests every ICO size; WM_GETICON inspects actual small/large window icons. These checks do not
prove the visual composition of Alt+Tab or the taskbar, other DPI settings, or Explorer cache refresh.

Run the [VisualCheck identity route](../Seqvium.VisualCheck/README.md#visual-identity-review) for
real font resolution, Russian/English, theme, About and populated workspace checks. Run catalog and
field matrix after shared typography/style changes. Keep the selected real publication captures
distinct from ignored transient GUI artifacts.
