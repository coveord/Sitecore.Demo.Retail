var gulp = require("gulp");
var msbuild = require("gulp-msbuild");
var debug = require("gulp-debug");
var foreach = require("gulp-foreach");
var rename = require("gulp-rename");
var watch = require("gulp-watch");
var merge = require("merge-stream");
var newer = require("gulp-newer");
var util = require("gulp-util");
var runSequence = require("run-sequence");
var path = require("path");
var config = require("./gulp-config.js")();
var nugetRestore = require('gulp-nuget-restore');
var fs = require('fs');
var unicorn = require("./scripts/unicorn.js");
var habitat = require("./scripts/habitat.js");
var exec = require("child_process").exec;
var rimraf = require("gulp-rimraf");
var rimrafDir = require("rimraf");
var courier = require("./scripts/sitecore.courier.js");

module.exports.config = config;

gulp.task("default", function (callback) {
  config.runCleanBuilds = true;
  return runSequence(
    "01-Copy-Sitecore-Lib",
    "02-Nuget-Restore",
    "03-Publish-All-Projects",
    "04-Apply-Xml-Transform",
    "05-Sync-Unicorn",
    "06-Deploy-Transforms",
	callback);
});

gulp.task("deploy", function (callback) {
  config.runCleanBuilds = true;
  return runSequence(
    "01-Copy-Sitecore-Lib",
    "02-Nuget-Restore",
    "03-Publish-All-Projects",
    "04-Apply-Xml-Transform",
    "06-Deploy-Transforms",
	callback);
});

/*****************************
  Initial setup
*****************************/
gulp.task("01-Copy-Sitecore-Lib", function () {
  console.log("Copying Sitecore Libraries and License file");

  fs.statSync(config.sitecoreLibraries);

  var files = config.sitecoreLibraries + "/**/*";

  var libs = gulp.src(files).pipe(gulp.dest("./lib/Sitecore"));
  var license = gulp.src(config.licensePath).pipe(gulp.dest("./lib"));

  return merge(libs, license);
});

gulp.task("02-Nuget-Restore", function (callback) {
  var solution = "./" + config.solutionName + ".sln";
  return gulp.src(solution).pipe(nugetRestore());
});

gulp.task("03-Publish-All-Projects", function (callback) {
  return runSequence(
    "Build-Solution",
    "Publish-Storefront-Projects",
    "Publish-Foundation-Projects",
    "Publish-Feature-Projects",
    "Publish-Css",
    "Publish-Project-Projects", callback);
});

gulp.task("04-Apply-Xml-Transform", function () {
  var layerPathFilters = ["./src/Foundation/**/*.transform", "./src/Feature/**/*.transform", "./src/Project/**/*.transform", "!./src/**/obj/**/*.transform", "!./src/**/bin/**/*.transform"];
  return gulp.src(layerPathFilters)
    .pipe(foreach(function (stream, file) {
      var fileToTransform = file.path.replace(/.+code\\(.+)\.transform/, "$1");
      fileToTransform = fileToTransform.replace(/.+legacy\\(.+)\.transform/, "$1");
      util.log("Applying configuration transform: " + file.path);
      return gulp.src("./scripts/applytransform.targets")
        .pipe(msbuild({
          targets: ["ApplyTransform"],
          configuration: config.buildConfiguration,
          logCommand: false,
          verbosity: "minimal",
          stdout: true,
          errorOnFail: true,
          maxcpucount: 0,
          toolsVersion: 14.0,
          properties: {
            Platform: config.buildPlatform,
            WebConfigToTransform: config.websiteRoot,
            TransformFile: file.path,
            FileToTransform: fileToTransform
          }
        }));
    }));
});

gulp.task("05-Sync-Unicorn", function (callback) {
  var options = {};
  options.siteHostName = habitat.getSiteUrl();
  options.authenticationConfigFile = config.websiteRoot + "/App_config/Include/Unicorn/Unicorn.UI.config";
  options.maxBuffer = Infinity;
  unicorn(function() { return callback() }, options);
});


gulp.task("06-Deploy-Transforms", function () {
  return gulp.src(["./src/**/code/**/*.transform", "./src/**/legacy/**/*.transform"]).pipe(gulp.dest(config.websiteRoot + "/temp/transforms"));
});

/*****************************
  Copy assemblies to all local projects
*****************************/
gulp.task("Copy-Local-Assemblies", function () {
  console.log("Copying site assemblies to all local projects");
  var files = config.sitecoreLibraries + "/**/*";

  var root = "./src";
  var projects = [root + "/**/code/bin", root + "/**/legacy/bin"];
  return gulp.src(projects, { base: root })
    .pipe(foreach(function (stream, file) {
      console.log("copying to " + file.path);
      gulp.src(files)
        .pipe(gulp.dest(file.path));
      return stream;
    }));
});

/*****************************
  Publish
*****************************/
var publishProjects = function (location, dest) {
  dest = dest || config.websiteRoot;
  var targets = ["Build"];

  console.log("publish to " + dest + " folder");
  return gulp.src([location + "/**/code/*.csproj", location + "/**/legacy/*.csproj", location + "/*.csproj"])
    .pipe(foreach(function (stream, file) {
      return stream
        .pipe(debug({ title: "Building project:" }))
        .pipe(msbuild({
          targets: targets,
          configuration: config.buildConfiguration,
          logCommand: false,
          verbosity: "minimal",
          stdout: true,
          errorOnFail: true,
          maxcpucount: 0,
          toolsVersion: 14.0,
          properties: {
        Platform: config.publishPlatform,
            DeployOnBuild: "true",
            DeployDefaultTarget: "WebPublish",
            WebPublishMethod: "FileSystem",
            DeleteExistingFiles: "false",
            publishUrl: dest,
            _FindDependencies: "false"
          }
        }));
    }));
};

gulp.task("Build-Solution", function () {
	console.log("Building Solution");
  var targets = ["Build"];
  if (config.runCleanBuilds) {
    targets = ["Clean", "Build"];
  }
  var solution = "./" + config.solutionName + ".sln";
  return gulp.src(solution)
      .pipe(msbuild({
          targets: targets,
          configuration: config.buildConfiguration,
          logCommand: false,
          verbosity: "minimal",
          stdout: true,
          errorOnFail: true,
          maxcpucount: 0,
          toolsVersion: 14.0,
          properties: {
            Platform: config.buildPlatform
          }
        }));
});

gulp.task("Publish-Storefront-Projects", function () {
	console.log("Publishing Storefront Projects");
  return publishProjects("./src/Foundation/Commerce/storefront/{CommonSettings,CF/CSF}");
});

gulp.task("Publish-Foundation-Projects", function () {
  return publishProjects("./src/Foundation");
});

gulp.task("Publish-Feature-Projects", function () {
  return publishProjects("./src/Feature");
});

gulp.task("Publish-Project-Projects", function () {
  return publishProjects("./src/Project");
});

gulp.task("Publish-Assemblies", function () {
  var root = "./src";
  var binFiles = root + "/**/code/**/bin/Sitecore.{Feature,Foundation,Habitat}.*.{dll,pdb}";
  var destination = config.websiteRoot + "/bin/";
  return gulp.src(binFiles, { base: root })
    .pipe(rename({ dirname: "" }))
    .pipe(newer(destination))
    .pipe(debug({ title: "Copying " }))
    .pipe(gulp.dest(destination));
});

gulp.task("Publish-All-Views", function () {
  var root = "./src";
  var roots = [root + "/**/Views", "!" + root + "/**/obj/**/Views"];
  var files = "/**/*.cshtml";
  var destination = config.websiteRoot + "\\Views";
  return gulp.src(roots, { base: root }).pipe(
    foreach(function (stream, file) {
      console.log("Publishing from " + file.path);
      gulp.src(file.path + files, { base: file.path })
        .pipe(newer(destination))
        .pipe(debug({ title: "Copying " }))
        .pipe(gulp.dest(destination));
      return stream;
    })
  );
});

gulp.task("Publish-All-Configs", function () {
  var root = "./src";
  var roots = [root + "/**/App_Config", "!" + root + "/**/obj/**/App_Config"];
  var files = "/**/*.config";
  var destination = config.websiteRoot + "\\App_Config";
  return gulp.src(roots, { base: root }).pipe(
    foreach(function (stream, file) {
      console.log("Publishing from " + file.path);
      gulp.src(file.path + files, { base: file.path })
        .pipe(newer(destination))
        .pipe(debug({ title: "Copying " }))
        .pipe(gulp.dest(destination));
      return stream;
    })
  );
});

gulp.task("Publish-Css", function () {
    var root = "./src";
    var roots = [root + "/**/styles", "!" + root + "/**/obj/**/styles"];
    var files = "/**/*.css";
    var destination = config.websiteRoot + "\\styles";
    return gulp.src(roots, { base: root }).pipe(
      foreach(function (stream, file) {
          console.log("Publishing from " + file.path);
          gulp.src(file.path + files, { base: file.path })
            .pipe(newer(destination))
            .pipe(debug({ title: "Copying " }))
            .pipe(gulp.dest(destination));
          return stream;
      })
    );
});

/*****************************
 Watchers
*****************************/
gulp.task("Auto-Publish-Css", function () {
  var root = "./src";
  var roots = [root + "/**/styles", "!" + root + "/**/obj/**/styles"];
  var files = "/**/*.css";
  var destination = config.websiteRoot + "\\styles";
  gulp.src(roots, { base: root }).pipe(
    foreach(function (stream, rootFolder) {
      gulp.watch(rootFolder.path + files, function (event) {
        if (event.type === "changed") {
          console.log("publish this file " + event.path);
          gulp.src(event.path, { base: rootFolder.path }).pipe(gulp.dest(destination));
        }
        console.log("published " + event.path);
      });
      return stream;
    })
  );
});

gulp.task("Auto-Publish-Views", function () {
  var root = "./src";
  var roots = [root + "/**/Views", "!" + root + "/**/obj/**/Views"];
  var files = "/**/*.cshtml";
  var destination = config.websiteRoot + "\\Views";
  gulp.src(roots, { base: root }).pipe(
    foreach(function (stream, rootFolder) {
      gulp.watch(rootFolder.path + files, function (event) {
        if (event.type === "changed") {
          console.log("publish this file " + event.path);
          gulp.src(event.path, { base: rootFolder.path }).pipe(gulp.dest(destination));
        }
        console.log("published " + event.path);
      });
      return stream;
    })
  );
});

gulp.task("Auto-Publish-Assemblies", function () {
  var root = "./src";
  var roots = [root + "/**/code/**/bin", root + "/**/legacy/**/bin"];
  var files = "/**/Sitecore.{Feature,Foundation,Habitat}.*.{dll,pdb}";;
  var destination = config.websiteRoot + "/bin/";
  gulp.src(roots, { base: root }).pipe(
    foreach(function (stream, rootFolder) {
      gulp.watch(rootFolder.path + files, function (event) {
        if (event.type === "changed") {
          console.log("publish this file " + event.path);
          gulp.src(event.path, { base: rootFolder.path }).pipe(gulp.dest(destination));
        }
        console.log("published " + event.path);
      });
      return stream;
    })
  );
});

/*****************************
 Commerce
*****************************/
gulp.task("CE-Install-Commerce-Server", function (callback) {
    var options = { maxBuffer: 4024 * 1024 };
    return exec("powershell -executionpolicy unrestricted -file .\\install-commerce-server.ps1", options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("CE-Install-Commerce-Sites", function (callback) {
    var options = { maxBuffer: 4024 * 1024 };
    return exec("powershell -executionpolicy unrestricted -file .\\install-commerce-sites.ps1", options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("CE-Uninstall-Commerce", function (callback) {
    var options = { maxBuffer: 1024 * 1024 };
    return exec("powershell -executionpolicy unrestricted -file .\\uninstall-commerce.ps1", options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("CE~default", function (callback) {
    config.runCleanBuilds = true;
    return runSequence(
      "CE-01-Nuget-Restore",
      "CE-02-Publish-CommerceEngine-Projects",
      callback);
});

gulp.task("CE-01-Nuget-Restore", function (callback) {
    return runSequence("02-Nuget-Restore", callback);
});

gulp.task("CE-02-Publish-CommerceEngine-Projects", function (callback) {
    var cmd = "dotnet publish ./src/Foundation/Commerce/Engine -o " + config.commerceEngineRoot
    var options = { maxBuffer: 1024 * 1024 };
    console.log("cmd: " + cmd);
    return exec(cmd, options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("CE-Import-CSCatalog", function (callback) {
    var dataPath = config.commerceDatabasePath + "\\Catalog.xml";
    var command = "\"& {Import-Module CSPS; Import-CSCatalog -Name " + config.commerceServerSiteName + " -File " + dataPath + " -ImportSchemaChanges $true -Mode Full}\""
    var cmd = "powershell -executionpolicy unrestricted -command " + command
    var options = { maxBuffer: 1024 * 1024 };
    console.log("cmd: " + cmd);
    return exec(cmd, options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("CE-Export-CSCatalog", function (callback) {
    var dataPath = config.commerceDatabasePath + "\\Catalog.xml";
    var command = "\"& {Import-Module CSPS; Export-CSCatalog -Name " + config.commerceServerSiteName + " -File " + dataPath + " -SchemaExportType All -Mode Full}\""
    var cmd = "powershell -executionpolicy unrestricted -command " + command
    var options = { maxBuffer: 1024 * 1024 };
    console.log("cmd: " + cmd);
    return exec(cmd, options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err );
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("CE-Import-CSInventory", function (callback) {
    var dataPath = config.commerceDatabasePath + "\\Inventory.xml";
    var command = "& {Import-Module CSPS; Import-CSInventory -Name " + config.commerceServerSiteName + " -File " + dataPath + " -ImportSchemaChanges $true -Mode Full}"
    var options = { maxBuffer: 1024 * 1024 };
    return exec("powershell -executionpolicy unrestricted -command \"" + command + "\"", options, function (err, stdout, stderr) {
        if (err) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

/*****************************
  Kill Tasks
*****************************/
gulp.task("Kill-w3wp-Tasks", function (callback) {
    var cmd = "@tskill w3wp /a /v"
    var options = { maxBuffer: 1024 * 1024 };
    console.log("cmd: " + cmd);
    return exec(cmd, options, function (err, stdout, stderr) {
        if ((err) && (!stderr.includes("Could not find process"))) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

gulp.task("Kill-iisexpress-Tasks", function (callback) {
    var cmd = "@tskill iisexpress /a /v"
    var options = { maxBuffer: 1024 * 1024 };
    console.log("cmd: " + cmd);
    return exec(cmd, options, function (err, stdout, stderr) {
        if ((err) && (!stderr.includes("Could not find process"))) {
            console.error("exec error: " + err);
            throw err;
        }
        console.log("stdout: " + stdout);
        console.log("stderr: " + stderr);
        callback();
    });
});

/*****************************
 Package
*****************************/
var packageSourcePath = path.resolve("./packageSrc");
var packageDestinationPath = path.resolve("./package");

gulp.task("package", function (callback) {
  config.runCleanBuilds = true;
  config.websiteRoot = packageSourcePath;
  config.buildConfiguration = "Release";

  return runSequence(
    "Package-Clean",
    "03-Publish-All-Projects",
    "Package-Clean-Files",
    "Package-Enable-Production-Settings",
    "Package-Copy-Serialized-Items",
    "Package-Generate-Update-Package",
    callback
  );
});

gulp.task("packagedev", function (callback) {
  config.runCleanBuilds = true;
  config.websiteRoot = packageSourcePath;
  config.buildConfiguration = "Release";

  return runSequence(
    "Package-Clean",
    "03-Publish-All-Projects",
    "Package-Clean-Files",
    "Package-Copy-Serialized-Items",
    "Package-Generate-Update-Package",
    callback
  );
});

gulp.task("Package-Clean", function (callback) {
  rimrafDir.sync(packageSourcePath);
  fs.mkdirSync(packageSourcePath);
  rimrafDir.sync(packageDestinationPath);
  fs.mkdirSync(packageDestinationPath);
  callback();
});

gulp.task("Package-Clean-Files", function (callback) {
  var excludeList = [
    packageSourcePath + "\\bin\\{Antlr3,Coveo,Microsoft.Extensions.DependencyInjection,Microsoft.Web.Infrastructure,Newtonsoft,Rainbow,Sitecore,System,WebActivatorEx,WebGrease}*{dll,pdb}",
    packageSourcePath + "\\packages.config",
    packageSourcePath + "\\App_Config\\Include\\{Feature,Foundation,Project}\\*Serialization.config",
    "!" + packageSourcePath + "\\bin\\Sitecore.Support*dll",
    "!" + packageSourcePath + "\\bin\\Sitecore.{Feature.Commerce,Foundation.Commerce,Demo}*dll"
  ];
  console.log(excludeList);

  return gulp.src(excludeList, { read: false }).pipe(rimraf({ force: true }));
});

gulp.task("Package-Enable-Production-Settings", function (callback) {
  var excludeList = [
    packageSourcePath + "\\App_Config\\Include\\{Feature,Foundation,Project,zzz}\\*.DevSettings.config"
  ];
  console.log(excludeList);
  gulp.src(excludeList, { read: false }).pipe(rimraf({ force: true }));

  fs.rename(
    packageSourcePath + "\\App_Config\\Include\\zzz\\zzz.Demo.Production.Settings.config.example",
    packageSourcePath + "\\App_Config\\Include\\zzz\\zzz.Demo.Production.Settings.config"
  );
  callback();
});

gulp.task("Package-Copy-Serialized-Items", function (callback) {
  var includeList = [
    "./src/**/serialization/**/*.yml",
    "./src/**/serialization/Users/**/*.yml",
    "./src/**/serialization/Roles/**/*.yml",
    "!./src/**/bower_components/**/*.yml",
    "!./src/Project/Retail/serialization/Storefront.Content/**/*.yml",
    "!./src/Foundation/Commerce.Errors/serialization/**/*.yml"
  ];

  return gulp.src(includeList).pipe(gulp.dest(packageSourcePath + "/Data"));
});

gulp.task("Package-Generate-Update-Package", function(callback) {
  courier.runner("-t \"" + packageSourcePath + "\" -o \"" + packageDestinationPath + "/Sitecore.Demo.Retail.update\" -r -f", callback);
});