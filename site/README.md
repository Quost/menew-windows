# Website content

The page https://menew.pururucos.com.br/windows (and `/pt/windows`, `/de/windows`, `/es/windows`, `/zh/windows`) gets its texts from this repository. The site is built and deployed from [Quost/menew-site](https://github.com/Quost/menew-site), whose README describes the content contract in full.

## What lives here

- `site/content.json`: every text of the page in the 5 languages (`en`, `pt`, `de`, `es`, `zh`), under `"texts"`, with the same keys in each. A little inline HTML is allowed (`<b>`, `<code>`, `<span class="grad">`). `"contract": 1` and `"platform": "windows"` identify the format.
  - `win_*`: the "coming soon" page, shown while this repository has no published release.
  - `win_dl_*` and `win_card_d`: the download page and the Windows card on the overview, shown automatically from the first published (non-draft) release on. Keep them true to what that release does before tagging it.
- `site/assets/` (optional, not there yet): screenshots, English at the top level and `site/assets/<lang>/` for the other languages, published under `/assets/windows/`. A screenshot only appears on the page once the template in menew-site (`site/templates/windows.html`) shows it; ask for that there.

## What updates by itself

- No flag to flip: when the first release of this repository is published, the next site build (menew-site checks every 30 minutes) turns `/windows` into a download page for that release, lets search engines index it and adds it to the sitemap. A pre-release shows "Preview", a normal release "Stable".
- Text changes merged into `main` reach the site the same way. No token, secret or workflow is needed here.
- If this folder is ever missing, the site falls back to its own copy of the "coming soon" texts.

## Check and preview

From a checkout of Quost/menew-site next to this repository:

```sh
python3 scripts/content.py windows ../menew-windows/site                    # validate
python3 scripts/build.py --content windows=../menew-windows/site --serve    # preview at http://localhost:8000/windows
```

## Rules

English, Portuguese, German, Spanish and Chinese for every text. No em-dash. Be honest about the state of the port: say what works today, no promises or dates.
