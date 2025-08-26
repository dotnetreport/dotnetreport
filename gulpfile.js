var gulp = require('gulp');
var uglify = require('gulp-uglify');
var concat = require('gulp-concat');
var rimraf = require("rimraf");
var merge = require('merge-stream');
var pump = require('pump');

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
	"gridstack": {
		"dist/gridstack-all.js": "",
		"dist/gridstack.min.css": ""
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
	"tributejs": {
		"dist/tribute.min.js": "",
		"dist/tribute.css": ""
	},
	"summernote": {
		"dist/summernote-bs5.min.js": "",
		"dist/summernote-bs5.min.css": "",
		"dist/font/*": "font/"
	}
};

gulp.task("clean", function (cb) {
	const cleanTasks = [
		rimraf.sync("wwwroot/js/dotnetreport.js"),
		rimraf.sync("wwwroot/js/dotnetreport-helper.js"),
		rimraf.sync("wwwroot/js/dotnetreport-setup.js"),
		rimraf.sync("wwwroot/css/dotnetreport.css"),
		rimraf.sync("wwwroot/img/report-logo.png"),
		rimraf.sync("wwwroot/lib/")
	];
	cb();
	return Promise.all(cleanTasks); // Optional, to make sure all tasks are resolved
});

gulp.task("scripts", function (done) {
	const tasks = [];

	for (const prop in deps) {

		console.log("Prepping Scripts for: " + prop);
		for (const itemProp in deps[prop]) {
			tasks.push(function (cb) {
				pump(
					gulp.src(`node_modules/${prop}/${itemProp}`, { encoding: false }),
					gulp.dest(`wwwroot/lib/${prop}/${deps[prop][itemProp]}`),
					cb
				);
			});
		}
	}

	// move dotnet report files
	tasks.push(function (cb) { pump(gulp.src("Scripts/dotnetreport.js"), gulp.dest("wwwroot/js/"), cb); });
	tasks.push(function (cb) { pump(gulp.src("Scripts/dotnetreport-helper.js"), gulp.dest("wwwroot/js/"), cb); });
	tasks.push(function (cb) { pump(gulp.src("Scripts/dotnetreport-setup.js"),gulp.dest("wwwroot/js/"),cb);})
	tasks.push(function (cb) { pump(gulp.src("Content/dotnetreport.css"), gulp.dest("wwwroot/css/"), cb); });

	tasks.push(function (cb) {
		pump(
			gulp.src("Content/img/report-logo.png", { encoding: false }),
			gulp.dest("wwwroot/img/"),
			cb
		);
	});

	// Run tasks in series
	gulp.series(...tasks)(done);
});

gulp.task('watch', function () {
	gulp.watch('Scripts/*.js', gulp.series('scripts'));
	gulp.watch('Content/*.css', gulp.series('scripts'));
})

gulp.task('build', gulp.series(
	'clean',
	'minify',
	'scripts'
));
