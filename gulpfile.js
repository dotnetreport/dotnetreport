var gulp = require('gulp');
var uglify = require('gulp-uglify');
var concat = require('gulp-concat');
var rimraf = require("rimraf");
var merge = require('merge-stream');

gulp.task("minify", function () {

	return gulp.src('Scripts/dotnetreport.js')
		// Minify the file
		.pipe(uglify())
		.pipe(concat("dotnetreport.min.js"))
		// Output
		.pipe(gulp.dest('Scripts/'));
});

