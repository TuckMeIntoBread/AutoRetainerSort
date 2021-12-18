using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ff14bot.Managers;
using static AutoRetainerSort.Classes.ItemSortInfo;

namespace AutoRetainerSort
{
    public partial class AddNewItemForm : Form
    {
        private static List<SearchResult> _itemNameCache;
        public static List<SearchResult> ItemNameCache
        {
            get
            {
                if (_itemNameCache == null)
                {
                    var unorderedList = new HashSet<SearchResult>();
                    foreach (var idItemPair in DataManager.ItemCache)
                    {
                        if (idItemPair.Key == 0 || idItemPair.Key > QualityOffset || idItemPair.Value == null || string.IsNullOrEmpty(idItemPair.Value.CurrentLocaleName))
                        {
                            continue;
                        }
                        unorderedList.Add(new SearchResult(idItemPair.Key, idItemPair.Value.EnglishName));
                    }

                    _itemNameCache = unorderedList.OrderBy(x => x.Name.Length).ThenBy(x => x.RawItemId).ToList();
                }

                return _itemNameCache;
            }
        }

        public AddNewItemForm()
        {
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            if (Owner != null)
            {
                var ownerCenterX = Owner.Location.X + (Owner.Width / 2) - (Width / 2);
                var ownerCenterY = Owner.Location.Y + (Owner.Height / 2) - (Width / 2);
                Location = new Point(ownerCenterX, ownerCenterY);
            }

            listBoxSearchResults.DataSource = _bsSearchResults;
        }

        private readonly BindingSource _bsSearchResults = new BindingSource();

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var searchResults = new List<SearchResult>();
            if (string.IsNullOrEmpty(textBoxSearch.Text))
            {
                return;
            }

            string[] splitSearchText = textBoxSearch.Text.Split(' ');
            var foundCount = 0;
            foreach (SearchResult searchResult in ItemNameCache)
            {
                if (searchResult.Name.MatchesSearch(splitSearchText))
                {
                    searchResults.Add(searchResult);
                    if (++foundCount >= 10)
                    {
                        break;
                    }
                }
            }

            _bsSearchResults.DataSource = searchResults;
            _bsSearchResults.ResetBindings(true);
            listBoxSearchResults.ClearSelected();
            listBoxSearchResults.ResetBindings();
            listBoxSearchResults.Refresh();
        }

        private void SearchResults_SelectionChanged(object sender, EventArgs e)
        {
            if (listBoxSearchResults.SelectedIndex < 0)
            {
                txtBoxItem.Text = string.Empty;
                return;
            }

            if (!(listBoxSearchResults.SelectedItem is SearchResult selectedItem))
            {
                txtBoxItem.Text = string.Empty;
                return;
            }

            SelectedSearchResult = selectedItem;
            txtBoxItem.Text = selectedItem.Name;
        }

        public SearchResult SelectedSearchResult;

        private void ModifierRadioButton_Changed(object sender, EventArgs e)
        {
            if (sender == rdBtnHQ)
            {
                checkBoxHQ.Checked = false;
                checkBoxHQ.Enabled = false;
                checkBoxCollectable.Enabled = true;
            }
            else if (sender == rdBtnCollectable)
            {
                checkBoxCollectable.Checked = false;
                checkBoxCollectable.Enabled = false;
                checkBoxHQ.Enabled = true;
            }
            else if (sender == rdBtnNone)
            {
                checkBoxCollectable.Enabled = true;
                checkBoxHQ.Enabled = true;
            }
        }

        public bool ModifierNone => rdBtnNone.Checked;

        public bool ModifierHQ => rdBtnHQ.Checked;

        public bool ModifierCollectable => rdBtnCollectable.Checked;

        public bool IncludeHQ => checkBoxHQ.Checked;

        public bool IncludeCollectable => checkBoxCollectable.Checked;
    }

    public class SearchResult : IEquatable<SearchResult>
    {
        public readonly string Name;

        public readonly uint RawItemId;

        public override string ToString()
        {
            return Name;
        }

        public SearchResult(uint rawItemId, string name)
        {
            RawItemId = rawItemId;
            Name = name;
        }

        public bool Equals(SearchResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RawItemId == other.RawItemId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SearchResult)obj);
        }

        public override int GetHashCode() => (int)RawItemId;
    }
}