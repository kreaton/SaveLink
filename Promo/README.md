# SaveLink promo

A 70-second Remotion promo for SaveLink, in 16:9 (`SaveLinkPromo`) and 9:16 (`SaveLinkPromoVertical`). Run the commands from this folder. Rendered videos go in `out/`, which isn't checked in, and neither is `node_modules/`.

```bash
npm install
npm run studio          # preview and tweak
npm run render:wide     # out/SaveLink-Promo-16x9-4K.mp4 (3840x2160)
npm run render:vertical # out/SaveLink-Promo-9x16-4K.mp4 (2160x3840)
```

- `public/` holds the assets: the screen recording, `Fresh Energy.mp3` (as `music.mp3`), and the desktop screenshots.
- Everything is timed in beats of the track (136 BPM) through `b(n)` in `src/constants.ts`. `MUSIC_START_SEC` puts the track's drop on beat 124.
- The zooms and callouts are in `src/scenes/Demo.tsx`. Times are seconds in the recording, and rectangles are pixels in the 1196x720 recording.
- `GridFrame` and `ContactSheet` are helper compositions for measuring the recording. `node scripts/stills.mjs <id> <dir> <scale> <frames...>` renders test stills.
