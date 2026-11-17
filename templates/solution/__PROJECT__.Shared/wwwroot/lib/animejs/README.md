# Anime.js bundle

`anime.ryz.min.js` is a tree-shaken Anime.js 4.5.0 bundle for the framework motion layer.

It exports only:

- `animate`
- `createTimeline`
- `createScope`
- `stagger`
- `cleanInlineStyles`

Rebuild from the official package with esbuild:

```powershell
npm install animejs@4.5.0 esbuild@0.28.2
```

Entry file:

```javascript
import { animate } from "animejs/animation";
import { createTimeline } from "animejs/timeline";
import { createScope } from "animejs/scope";
import { cleanInlineStyles, stagger } from "animejs/utils";

export { animate, cleanInlineStyles, createScope, createTimeline, stagger };
```

```powershell
esbuild motion-entry.js --bundle --format=esm --minify --legal-comments=none `
  --banner:js="/*! Anime.js v4.5.0 | MIT | Julian Garnier | https://animejs.com */" `
  --outfile=anime.ryz.min.js
```

Anime.js is licensed under MIT. See `LICENSE.md`.
