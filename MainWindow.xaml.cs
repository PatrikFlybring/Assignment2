using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace Assignment2 {
    public partial class MainWindow : Window {
        private Thickness spacing = new Thickness(5);
        private HttpClient http = new HttpClient();
        // We will need these as instance variables to access in event handlers.
        private TextBox addFeedTextBox;
        private Button addFeedButton;
        private ComboBox selectFeedComboBox;
        private Button loadArticlesButton;
        private StackPanel articlePanel;
        private List<WebsiteData> websiteList = new List<WebsiteData>();
        private List<FeedData> FeedDataList = new List<FeedData>();

        // Sample feeds:
        // https://www.cinemablend.com/rss/topic/news/movies
        // https://screencrush.com/feed/


        public MainWindow() {
            InitializeComponent();
            Start();
        }

        private void Start() {
            // Window options
            Title = "Feed Reader";
            Width = 800;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Scrolling
            var root = new ScrollViewer();
            root.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Content = root;

            // Main grid
            var grid = new Grid();
            root.Content = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var addFeedLabel = new Label {
                Content = "Feed URL:",
                Margin = spacing
            };
            grid.Children.Add(addFeedLabel);

            addFeedTextBox = new TextBox {
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedTextBox);
            Grid.SetColumn(addFeedTextBox, 1);

            addFeedButton = new Button {
                Content = "Add Feed",
                Margin = spacing,
                Padding = spacing,
            };
            addFeedButton.Click += AddFeed_Click;
            grid.Children.Add(addFeedButton);
            Grid.SetColumn(addFeedButton, 2);

            var selectFeedLabel = new Label {
                Content = "Select Feed:",
                Margin = spacing
            };
            grid.Children.Add(selectFeedLabel);
            Grid.SetRow(selectFeedLabel, 1);

            selectFeedComboBox = new ComboBox {
                Margin = spacing,
                Padding = spacing,
                IsEditable = false
            };
            selectFeedComboBox.Items.Add("All Feeds");
            selectFeedComboBox.SelectedIndex = 0;
            grid.Children.Add(selectFeedComboBox);
            Grid.SetRow(selectFeedComboBox, 1);
            Grid.SetColumn(selectFeedComboBox, 1);

            loadArticlesButton = new Button {
                Content = "Load Articles",
                Margin = spacing,
                Padding = spacing
            };
            loadArticlesButton.Click += LoadArticles_Click;
            grid.Children.Add(loadArticlesButton);
            Grid.SetRow(loadArticlesButton, 1);
            Grid.SetColumn(loadArticlesButton, 2);

            articlePanel = new StackPanel {
                Orientation = Orientation.Vertical,
                Margin = spacing
            };
            grid.Children.Add(articlePanel);
            Grid.SetRow(articlePanel, 2);
            Grid.SetColumnSpan(articlePanel, 3);
        }

        private async void AddFeed_Click(object sender, RoutedEventArgs e) {
            addFeedButton.IsEnabled = false;
            XDocument rssResults = await LoadDocumentAsync(addFeedTextBox.Text);

            WebsiteData newFeed = new WebsiteData();
            newFeed.Title = rssResults.Descendants("title").First().Value;
            newFeed.URL = addFeedTextBox.Text;
            websiteList.Add(newFeed);
            
            selectFeedComboBox.Items.Add(newFeed.Title);
            selectFeedComboBox.SelectedItem = newFeed.Title;
            addFeedTextBox.Clear();
            addFeedButton.IsEnabled = true;
        }

        private async Task CollectArticles(string url) {
            XDocument rssResults = await LoadDocumentAsync(url);

            for (int i = 0; i < 5; i++) {
                FeedData data = new FeedData();

                data.Title = rssResults.Descendants("title").First().Value;

                data.ArticleTitle = rssResults.Descendants("item").Descendants("title").Skip(i).First().Value;

                string dateTimeString = rssResults.Descendants("item").Descendants("pubDate").Skip(i).First().Value;
                data.PubDate = DateTime.ParseExact(dateTimeString.Substring(0, 25), "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                
                FeedDataList.Add(data);
            }
        }
        private async void LoadArticles_Click(object sender, RoutedEventArgs e) {
            articlePanel.Children.Clear();
            FeedDataList.Clear();
            int i = selectFeedComboBox.SelectedIndex;

            loadArticlesButton.IsEnabled = false;
            if (i == 0 && websiteList.Count() != 0) {
                List<Task> tasks = new List<Task>();
                for (int y = 0; y < websiteList.Count(); y++) {
                    // Collect data from all feeds to a list
                    //await CollectArticles(websiteList[y].URL);
                    tasks.Add(CollectArticles(websiteList[y].URL));
                }
                await Task.WhenAll(tasks);
            }
            else {
                // Collect data from selected feed to a list
                await CollectArticles(websiteList[i-1].URL);
            }

            // Print feed data from the list into the stackpanel
            foreach (FeedData feedData in FeedDataList.OrderBy(d => d.PubDate)) {
                var articlePlaceholder = new StackPanel {
                    Orientation = Orientation.Vertical,
                    Margin = spacing
                };
                articlePanel.Children.Add(articlePlaceholder);

                // Date + title
                var articleTitle = new TextBlock {
                    Text = feedData.PubDate.ToString("yyyy-MM-dd HH:mm") + " - " + feedData.ArticleTitle,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                articlePlaceholder.Children.Add(articleTitle);

                // Website name
                var articleWebsite = new TextBlock {
                    Text = feedData.Title
                };
                articlePlaceholder.Children.Add(articleWebsite);
            }
            loadArticlesButton.IsEnabled = true;
        }

        public class FeedData {
            public string Title { get; set; }
            public string ArticleTitle { get; set; }
            public DateTime PubDate { get; set; }
        }

        public class WebsiteData {
            public string URL { get; set; }
            public string Title { get; set; }
        }

        private async Task<XDocument> LoadDocumentAsync(string url) {
            // This is just to simulate a slow/large data transfer and make testing easier.
            // Remove it if you want to.

            await Task.Delay(1000);
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var feed = XDocument.Load(stream);

            return feed;
        }
    }
}