namespace tv_series_files_organizer
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public partial class Form1 : Form
    {
        private static readonly Regex EpisodeRegex = new Regex(@"(.*S\d{2}E\d{2})", RegexOptions.IgnoreCase);
        private static readonly string[] AllowedExtensions = { ".srt", ".mkv", ".mp4" };
        string current_path;

        public Form1()
        {
            InitializeComponent();
            button1.Enabled = false;
            label1.AutoSize = true;
            label1.MaximumSize = new Size(350, 0);
            label1.Text = "";
            pictureBox1.Visible = false;
            label3.Visible = false;
            current_path = "";
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            label2.Visible = false;
            pictureBox1.Visible = true;
            if (files[0] != current_path)
            {
                current_path = files[0];
                label1.Text = current_path;
                button1.Enabled = true;
                if (label3.Visible == true)
                    label3.Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label3.Visible = true;

            string folderPath = current_path;

            // Move .mkv and .mp4 files from root to episode-specific folders and handle .srt files
            foreach (var extension in new[] { "*.mkv", "*.mp4" })
            {
                foreach (var filePath in Directory.EnumerateFiles(folderPath, extension))
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var match = Regex.Match(fileName, @"(.*S\d{2}E\d{2})", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var episodeFolderName = match.Groups[1].Value;
                        var episodeFolderPath = Path.Combine(folderPath, episodeFolderName);
                        Directory.CreateDirectory(episodeFolderPath); // Create episode-specific folder
                        var newFilePath = Path.Combine(episodeFolderPath, Path.GetFileName(filePath));
                        File.Move(filePath, newFilePath); // Move the .mkv or .mp4 file to the episode-specific folder

                        // Move the corresponding .srt file to the episode-specific folder if it exists
                        var srtFileName = fileName + "-eng.srt";
                        var srtFilePath = Path.Combine(folderPath, srtFileName);
                        if (File.Exists(srtFilePath))
                        {
                            var newSrtFilePath = Path.Combine(episodeFolderPath, srtFileName);
                            File.Move(srtFilePath, newSrtFilePath);
                        }
                    }
                }
            }


            // 2. Rename all folders and move misplaced files
            foreach (var dirPath in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
            {
                var dirName = Path.GetFileName(dirPath);
                var match = Regex.Match(dirName, @"(.*E\d{2})", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var newName = match.Groups[1].Value;
                    var newPath = Path.Combine(Path.GetDirectoryName(dirPath), newName);

                    if (dirPath != newPath) // Check if source and destination paths are different
                    {
                        Directory.Move(dirPath, newPath);
                    }
                }
            }


            // 3. Handle each episode folder
            foreach (var dirPath in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
            {
                var rootSubsFolder = Path.Combine(folderPath, "Subs");
                var episodeSubsFolder = Path.Combine(dirPath, "Subs");

                if (Directory.Exists(rootSubsFolder) || Directory.Exists(episodeSubsFolder))
                {
                    var videoFiles = Directory.EnumerateFiles(dirPath, "*.mkv")
                        .Concat(Directory.EnumerateFiles(dirPath, "*.mp4")).ToList();
                    if (videoFiles.Any())
                    {
                        var videoFileName = Path.GetFileNameWithoutExtension(videoFiles.First());
                        var videoMatch = Regex.Match(videoFileName, @"(.*S\d{2}E\d{2})", RegexOptions.IgnoreCase);
                        if (videoMatch.Success)
                        {
                            var videoSE = videoMatch.Groups[1].Value;

                            var srtFiles = new List<string>();

                            if (Directory.Exists(rootSubsFolder))
                            {
                                srtFiles.AddRange(Directory.EnumerateFiles(rootSubsFolder, "*" + videoSE + "*.srt"));
                            }

                            if (Directory.Exists(episodeSubsFolder))
                            {
                                srtFiles.AddRange(Directory.EnumerateFiles(episodeSubsFolder, "*" + videoSE + "*.srt"));
                            }

                            if (srtFiles.Any())
                            {
                                var newestSrt = srtFiles.OrderByDescending(x => File.GetLastWriteTime(x)).First();
                                File.Copy(newestSrt, Path.Combine(dirPath, videoFileName + ".srt"), true);
                                File.Delete(newestSrt);
                            }
                        }
                    }
                }
                // Remove the "Subs" folder inside the episode folder if it exists
                if (Directory.Exists(episodeSubsFolder))
                {
                    Directory.Delete(episodeSubsFolder, true); // true enables recursive deletion
                }
            }


            // Remove all files except .srt, .mkv, or .mp4
            foreach (var filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(filePath);
                if (extension != ".srt" && extension != ".mkv" && extension != ".mp4")
                {
                    File.Delete(filePath);
                }
            }

            // Remove remaining .srt files and "Subs" folders in the root folder
            foreach (var dirPath in Directory.EnumerateDirectories(folderPath))
            {
                if (Path.GetFileName(dirPath).ToLower() == "subs")
                {
                    Directory.Delete(dirPath, true);
                }
            }

            foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.srt"))
            {
                File.Delete(filePath);
            }

            // Remove all empty directories
            foreach (var dirPath in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories))
            {
                if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                {
                    Directory.Delete(dirPath);
                }
            }
        }
    }
}