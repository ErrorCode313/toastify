﻿using log4net;
using System.Diagnostics;
using System.Media;
using System.Net;
using System.Windows;
using System.Windows.Navigation;
using Toastify.ViewModel;
using ToastifyAPI.GitHub;
using ToastifyAPI.GitHub.Model;
using Dispatch = System.Windows.Threading.Dispatcher;

namespace Toastify.View
{
    public partial class ChangelogView : Window
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ChangelogView));

        private static bool showed;

        private readonly ChangelogViewModel viewModel;

        public ChangelogView()
        {
            this.InitializeComponent();

            this.viewModel = new ChangelogViewModel();
            this.DataContext = this.viewModel;
        }

        internal static void Launch()
        {
            if (showed)
                return;

            if (logger.IsDebugEnabled)
                logger.Debug("Launching ChangelogViewer...");

            App.CallInSTAThreadAsync(() =>
            {
                ChangelogView changelogView = new ChangelogView();
                changelogView.DownloadChangelog();

                showed = true;
                changelogView.Show();
                SystemSounds.Asterisk.Play();

                Dispatch.Run();
            }, true, "Changelog Viewer");
        }

        private void DownloadChangelog()
        {
            logger.Info("Downloading latest changelog...");

            GitHubAPI gitHubAPI = new GitHubAPI(App.ProxyConfig);
            Release release = gitHubAPI.GetReleaseByTagName(App.RepoInfo, App.CurrentVersion);
            if (release.HttpStatusCode != HttpStatusCode.OK)
                release = gitHubAPI.GetLatestRelease(App.RepoInfo);

            // OnDownloaded
            if (release.HttpStatusCode == HttpStatusCode.OK)
            {
                this.viewModel.ReleaseBodyMarkdown = $"## {release.Name}\n" +
                                                     $"{gitHubAPI.GitHubify(release.Body)}";

                this.viewModel.PublishedAt = release.PublishedAt?.ToString(App.UserCulture);

                logger.Info("Changelog downloaded");
            }
            else
            {
                this.viewModel.ReleaseBodyMarkdown = "### Failed loading the changelog!\n" +
                                                     $"You can read the latest changes [here]({Releases.GetUrlOfLatestRelease(App.RepoInfo)}).";
                this.PanelPublished.Visibility = Visibility.Collapsed;

                logger.Warn($"Failed to download the latest changelog. StatusCode = {release.HttpStatusCode}");
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true };
            Process.Start(psi);
            e.Handled = true;
        }
    }
}