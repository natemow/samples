'use strict';

var gulp = require('gulp');
var sass = require('gulp-sass');
var sourcemaps = require('gulp-sourcemaps');
var livereload = require('gulp-livereload');
var spritesmith = require('gulp.spritesmith');

// CSS compilation watcher tasks.
var css = function() {
  // Base css tasks.
  gulp
    .src('./assets/scss/*.scss')
    .pipe(sourcemaps.init())
    .pipe(sass().on('error', sass.logError))
    .pipe(sourcemaps.write('.'))
    .pipe(gulp.dest('./assets/css'));

  // App module css tasks.
  gulp
    .src('./app/modules/**/*.scss')
    .pipe(sourcemaps.init())
    .pipe(sass().on('error', sass.logError))
    .pipe(sourcemaps.write('.'))
    .pipe(gulp.dest('./assets/css'));

  return gulp
    .src('./app/modules/*.scss')
    .pipe(sourcemaps.init())
    .pipe(sass().on('error', sass.logError))
    .pipe(sourcemaps.write('.'))
    .pipe(gulp.dest('./assets/css'));
};
  gulp.task('css', css);
  gulp.task('css-reload', function () {
    css()
      .pipe(livereload());
  });

// Watch and reload listeners.
gulp.task('watch', function () {
  gulp.watch('./assets/scss/*.scss', ['css']);
  gulp.watch('./app/modules/**/*.scss', ['css']);
  gulp.watch('./app/modules/*.scss', ['css']);
});
gulp.task('watch-reload', function () {
  livereload.listen();
  gulp.watch('./assets/scss/*.scss', ['css-reload']);
  gulp.watch('./app/modules/**/*.scss', ['css-reload']);
  gulp.watch('./app/modules/*.scss', ['css-reload']);
});

// Sprite tasks.
gulp.task('sprites', function () {
  var spriteData = gulp
    .src('./assets/images/sprites/*.*')
    .pipe(spritesmith({
      imgName: 'sprites.png',
      cssName: '_sprites.scss',
      padding: 10,
      algorithm: 'binary-tree',
      cssOpts: {functions: false}
    }));

  spriteData.img.pipe(gulp.dest('./assets/images/'));
  spriteData.css.pipe(gulp.dest('./assets/scss/'));
});
