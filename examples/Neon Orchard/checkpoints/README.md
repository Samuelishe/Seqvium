# Musical checkpoints

These snapshots preserve deliberate musical comparison points while the parent project remains
the single developing composition. They reuse the parent's immutable managed media and become
ordinary standalone projects through [Restore-Checkpoint.ps1](../Restore-Checkpoint.ps1).

## publication-01

[publication-01.json](publication-01.json) freezes the first public edition: 32 bars, 112 BPM, 4/4,
15 Patterns, 191 notes, nine Playlist tracks, 78 clips, 12 instrument channels and one Drums bus.
It preserves the original arrangement and Mixer with independently synthesized bass/FX replacing
the nonredistributable MusicRadar sources. All four VSCO recordings remain byte-identical.

Project SHA-256: `dba06a7d7c5e6a675bb408581639f12bc6c9a62b48cf48551fb3dfa4d6a5f109`.
Required media hashes and licenses: [MEDIA.json](../MEDIA.json).

The initial Windows native GUI export on Seqvium **0.1.5.0000** is 48 kHz stereo PCM16,
3,291,429 frames / 68.5714375 seconds, peak 0.8588256836 (about -1.32 dBFS), zero saturated frames.
Its SHA-256 is `c9c175ea60042a5bb8f20dadf7afbd9528727da4131077003ef5a0fdd124b1e0`;
the final **0.1.5.0001** native GUI export from a freshly restored independent checkpoint matches
that hash exactly. The BUILD adds demo resources and verification, with unchanged audio processing.
The render is reproducible from the included project/media; the 13 MB WAV is not duplicated in Git.
Native playback used Steinberg UR12 at 44.1 kHz stereo with real Mixer meters. This establishes
execution/export evidence, not subjective acoustic approval or a heavy-load performance guarantee.

Add a checkpoint only before a substantial musical change or for a specific engine comparison.
Never rewrite a frozen snapshot to make a newer engine's output match an older measurement.
