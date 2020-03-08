///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "default");
var configuration = Argument("configuration", "Release");

var sln = "./back/Sequel/Sequel.sln";
var distDir = "./dist";
var distDirFullPath = MakeAbsolute(Directory($"{distDir}")).FullPath;
var publishDir = "./publish";
var publishDirFullPath = MakeAbsolute(Directory($"{publishDir}")).FullPath;
var winWarpPacker = "./build/warp/windows-x64.warp-packer.exe";
var linuxWarpPacker = "./build/warp/linux-x64.warp-packer";
var framework = "netcoreapp3.1";
var logger = Environment.GetEnvironmentVariable("TF_BUILD") == "True" ? $"-l:trx --results-directory {publishDirFullPath}" : "-l:console;verbosity=normal";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx => 
{ 
    Information($"Building Sequel");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("clean").Does(() =>
{
    CreateDirectory(distDir);
    
    CleanDirectories(distDir);
    CleanDirectories(publishDir);
    CleanDirectories($"./**/obj/{framework}");
    CleanDirectories(string.Format("./**/obj/{0}", configuration));
    CleanDirectories(string.Format("./**/bin/{0}", configuration));
});

Task("install-vue-cli").Does(() =>
{
    IEnumerable<string> standardOutput;
    StartProcess("powershell", new ProcessSettings().SetRedirectStandardOutput(true).WithArguments
    (
        args => args.Append($"npm list -g @vue/cli")
    ), out standardOutput);

	if (!string.Join(" ", standardOutput).Contains("@vue/cli"))
    {
        StartProcess("powershell", new ProcessSettings().WithArguments
        (
            args => args.Append($"npm install -g @vue/cli")
        ));
    }
});

Task("win-publish-front").WithCriteria(() => IsRunningOnWindows()).Does(() =>
{
    StartProcess("powershell", new ProcessSettings().UseWorkingDirectory("./front").WithArguments
    (
        args => args.Append($"./node_modules/.bin/vue-cli-service build")
                    .Append($"--dest {publishDirFullPath}/app/win-x64/wwwroot")
                    .Append($"--target app")
                    .Append($"--mode production")
    ));
});

Task("linux-publish-front").WithCriteria(() => IsRunningOnUnix()).Does(() =>
{
    StartProcess("powershell", new ProcessSettings().UseWorkingDirectory("./front").WithArguments
    (
        args => args.Append($"./node_modules/.bin/vue-cli-service build")
                    .Append($"--dest {publishDirFullPath}/app/linux-x64/wwwroot")
                    .Append($"--target app")
                    .Append($"--mode production")
    ));
});

Task("build-back").Does(() =>
{
    DotNetCoreBuild(sln, new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        Verbosity = DotNetCoreVerbosity.Minimal
    });
});

Task("win-publish-back").WithCriteria(() => IsRunningOnWindows()).Does(() =>
{
    DotNetCorePublish("./back/Sequel/src/Sequel", new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir + "/app/win-x64",
        Runtime = "win-x64"
    });
});

Task("linux-publish-back").WithCriteria(() => IsRunningOnUnix()).Does(() =>
{
    DotNetCorePublish("./back/Sequel/src/Sequel", new DotNetCorePublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir + "/app/linux-x64",
        Runtime = "linux-x64"
    });
});

Task("win-warp").WithCriteria(() => IsRunningOnWindows()).Does(() =>
{
    StartProcess(winWarpPacker, new ProcessSettings().WithArguments
    (
        args => args.Append($"--arch windows-x64")
                    .Append($"--input_dir {publishDir}/app/win-x64")
                    .Append($"--exec Sequel.exe")
                    .Append($"--output {distDir}/sequel.exe")
    ));
});

Task("linux-warp").WithCriteria(() => IsRunningOnUnix()).Does(() =>
{
    StartProcess(linuxWarpPacker, new ProcessSettings().WithArguments
    (
        args => args.Append($"--arch linux-x64")
                    .Append($"--input_dir {publishDir}/app/linux-x64")
                    .Append($"--exec Sequel")
                    .Append($"--output {distDir}/sequel")
    ));
});

Task("default")
    .IsDependentOn("clean")
    .IsDependentOn("install-vue-cli")
    .IsDependentOn("win-publish-front")
    .IsDependentOn("linux-publish-front")
    .IsDependentOn("build-back")
    .IsDependentOn("win-publish-back")
    .IsDependentOn("win-warp")
    .IsDependentOn("linux-publish-back")
    .IsDependentOn("linux-warp");

RunTarget(target);