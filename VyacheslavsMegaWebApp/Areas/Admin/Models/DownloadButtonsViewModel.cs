using System;
using System.Collections.Generic;
using VyacheslavsMegaWebApp.Data.Entities;

namespace VyacheslavsMegaWebApp.Areas.Admin.Models
{
    public class DownloadButtonsViewModel
    {
        public DownloadButtonsViewModel(List<DownloadButton> downloadButtons)
        {
            DownloadButtons = downloadButtons;
        }

        public List<DownloadButton> DownloadButtons { get; set; }
        public bool LauncherFileExists { get; set; }
        public long LauncherFileSizeBytes { get; set; }
        public DateTime? LauncherLastModified { get; set; }

        public bool SetupFileExists { get; set; }
        public long SetupFileSizeBytes { get; set; }
        public DateTime? SetupLastModified { get; set; }

        public bool ZipFileExists { get; set; }
        public long ZipFileSizeBytes { get; set; }
    }
}
