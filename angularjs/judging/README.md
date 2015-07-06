# Judging

## AngularJS app ( *jgui* )

All of the AngularJS stuff lives in `/public/judging/app`. The main template for
the judging app is at `/app/views-judging/judging/index.volt`.

Note the `#jguiConfigData` script element in this file as well; global resource
paths, various module configs, etc. are controlled exclusively by this JSON.

## Assets

### Gulp CSS setup

* `sudo npm install --global gulp`
* `npm install`

### Compiling SCSS to CSS

* `gulp css`
* `gulp watch` to compile css on .scss changes.
* `gulp watch-reload` to watch, with live-reload

### CSS spriting

`gulp sprites` to generate a sprite sheet from images in assets/images/sprites/
