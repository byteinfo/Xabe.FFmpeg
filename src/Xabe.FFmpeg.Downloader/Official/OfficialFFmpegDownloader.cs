﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Xabe.FFmpeg.Downloader
{
    internal class OfficialFFmpegDownloader : FFmpegDownloaderBase
    {
        private readonly LinkProvider _linkProvider;

        internal OfficialFFmpegDownloader() : base()
        {
            _linkProvider = new LinkProvider(_operatingSystemProvider);
        }

        internal OfficialFFmpegDownloader(IOperatingSystemProvider operatingSystemProvider) : base(operatingSystemProvider)
        {
            _linkProvider = new LinkProvider(operatingSystemProvider);
        }

        public override async Task GetLatestVersion(string path, IProgress<ProgressInfo> progress = null, int retries = DEFAULT_MAX_RETRIES)
        {
            var latestVersion = GetLatestVersionInfo();

            if (!CheckIfUpdateAvailable(latestVersion.Version, path) && !CheckIfFilesExist(path))
            {
                return;
            }

            await DownloadLatestVersion(latestVersion, path, progress, retries);

            SaveVersion(latestVersion, path);
        }

/*
{
    "version": "6.1",
    "permalink": "https://ffbinaries.com/api/v1/version/6.1",
    "bin": {
        "windows-64": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-win-64.zip"
        },
        "linux-32": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-32.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-linux-32.zip"
        },
        "linux-64": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-64.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-linux-64.zip"
        },
        "linux-armhf": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-armhf-32.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-linux-armhf-32.zip"
        },
        "linux-armel": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-armel-32.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-linux-armel-32.zip"
        },
        "linux-arm64": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-arm-64.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-linux-arm-64.zip"
        },
        "osx-64": {
            "ffmpeg": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-macos-64.zip",
            "ffprobe": "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-macos-64.zip"
        }
    }
}
*/

        internal FFbinariesVersionInfo GetLatestVersionInfo()
        {
            using (var wc = new WebClient())
            {
                var json = wc.DownloadString("http://ffbinaries.com/api/v1/version/latest");
                return JsonConvert.DeserializeObject<FFbinariesVersionInfo>(json);
            }
        }

        internal async Task DownloadLatestVersion(FFbinariesVersionInfo latestFFmpegBinaries, string path, IProgress<ProgressInfo> progress = null, int retries = DEFAULT_MAX_RETRIES)
        {
            Links links = _linkProvider.GetLinks(latestFFmpegBinaries);

            var ffmpegZipDownloadTask = DownloadFile(links.FFmpegLink, progress, retries);
            var ffprobeZipDownloadTask = DownloadFile(links.FFprobeLink, progress, retries);

            var ffmpegZip = await ffmpegZipDownloadTask;
            var ffprobeZip = await ffprobeZipDownloadTask;

            Extract(ffmpegZip, path ?? ".");
            Extract(ffprobeZip, path ?? ".");

            File.Delete(ffmpegZip);
            File.Delete(ffprobeZip);
        }

        private bool CheckIfUpdateAvailable(string latestVersion, string path)
        {
            var versionPath = Path.Combine(path ?? ".", "version.json");
            if (!File.Exists(versionPath))
            {
                return true;
            }

            FFbinariesVersionInfo currentVersion = JsonConvert.DeserializeObject<FFbinariesVersionInfo>(File.ReadAllText(versionPath));
            if (currentVersion != null)
            {
                if (new Version(latestVersion) > new Version(currentVersion.Version))
                {
                    return true;
                }
            }

            return false;
        }

        internal void SaveVersion(FFbinariesVersionInfo latestVersion, string path)
        {
            var versionPath = Path.Combine(path ?? ".", "version.json");
            File.WriteAllText(versionPath, JsonConvert.SerializeObject(new DownloadedVersion()
            {
                Version = latestVersion.Version
            }, Formatting.Indented));
        }
    }
}
