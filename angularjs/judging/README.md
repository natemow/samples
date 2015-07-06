# Judging

## AngularJS app ( *jgui* )

Note the `#jguiConfigData` script element in `index.volt`; global resource
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
