var gulp = require('gulp');
var uglify = require('gulp-uglify');
var concat = require('gulp-concat');
var rimraf = require("rimraf");
var merge = require('merge-stream');

gulp.task('dist', function (cb) {
	rimraf("wwwroot/js/dist/", cb);
	return gulp.src('wwwroot/js/dotnetreport.js')
		// Minify the file
		.pipe(uglify())
		.pipe(concat("dotnetreport.min.js"))
		// Output
		.pipe(gulp.dest('wwwroot/js/dist'));
});

gulp.task("minify", function () {

	var streams = [
		gulp.src(["wwwroot/js/*.js"])
			.pipe(uglify())
			.pipe(concat("site.min.js"))
			.pipe(gulp.dest("wwwroot/lib/site"))
	];

	return merge(streams);
});

// Dependency Dirs
var deps = {
	"bootstrap": {
		"dist/**/*": ""
	},
	"jquery": {
		"dist/*": ""
	},
	"jquery-ui-dist": {
		"jquery-ui.min.js": ""
	},
	"jquery-validation": {
		"dist/**/*": ""
	},
	"jquery-validation-unobtrusive": {
		"dist/*": ""
	},
	"knockout": {
		"build/output/*": ""
	},
	"lodash": {
		"lodash.min.js": ""
	},
	"toastr": {
		"build/*": ""
	},
	"bootstrap-datepicker": {
		"dist/**/*": ""
	},
	"font-awesome": {
		"**/*": ""
	},
	"select2": {
		"dist/**/*": ""
	},
	"jquery-blockui": {
		"jquery.blockUI.js": ""
	},
	"bootbox": {
		"dist/*": ""
	},
	"knockout-sortable": {
		"build/*": ""
	},
	"knockout-mapping": {
		"dist/*": ""
	},
	"x-editable-bs4": {
		"dist/bootstrap4-editable/**/*": ""
	},
	"knockout-x-editable": {
		"knockout.x-editable.min.js": ""
	},
	"fabric": {
		"dist/fabric.min.js": ""
	}
};

gulp.task("clean", function (cb) {
	rimraf("wwwroot/js/dotnetreport.js", cb);
	rimraf("wwwroot/js/dotnetreport-helper.js", cb);
	rimraf("wwwroot/js/dotnetreport-setup.js", cb);
	rimraf("wwwroot/css/dotnetreport.css", cb);
	rimraf("wwwroot/img/report-logo.png", cb);
	return rimraf("wwwroot/lib/", cb);
});

gulp.task("scripts", function () {

	var streams = [];

	for (var prop in deps) {
		console.log("Prepping Scripts for: " + prop);
		for (var itemProp in deps[prop]) {
			streams.push(gulp.src("node_modules/" + prop + "/" + itemProp)
				.pipe(gulp.dest("wwwroot/lib/" + prop + "/" + deps[prop][itemProp])));
		}
	}

	// move dotnet report files
	streams.push(gulp.src("Scripts/dotnetreport.js").pipe(gulp.dest("wwwroot/js/")));
	streams.push(gulp.src("Scripts/dotnetreport-helper.js").pipe(gulp.dest("wwwroot/js/")));
	streams.push(gulp.src("Scripts/dotnetreport-setup.js").pipe(gulp.dest("wwwroot/js/")));
	streams.push(gulp.src("Content/dotnetreport.css").pipe(gulp.dest("wwwroot/css/")));
	streams.push(gulp.src("Content/img/report-logo.png").pipe(gulp.dest("wwwroot/img/")));

	return merge(streams);

});

gulp.task('watch', function () {
	gulp.watch('Scripts/*.js', gulp.series('scripts'));
	gulp.watch('Content/*.css', gulp.series('scripts'));
	gulp.watch('Content/img/*.png', gulp.series('scripts'));
})

gulp.task('build', gulp.series(
	'clean',
	'minify',
	'scripts'
));
