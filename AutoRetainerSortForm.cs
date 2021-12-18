﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using AutoRetainerSort.Classes;
using LlamaLibrary.Logging;
using LlamaLibrary.RemoteWindows;

namespace AutoRetainerSort
{
    public partial class AutoRetainerSortForm : Form
    {
        private static readonly LLogger Log = new LLogger(Strings.LogPrefix, Colors.Orange);

        public AutoRetainerSortForm()
        {
            InitializeComponent();
        }

        private BindingSource _bsInventories;

        private void Form_Load(object sender, EventArgs e)
        {
            if (AutoRetainerSortSettings.Instance.WindowPosition != Point.Empty)
            {
                Location = AutoRetainerSortSettings.Instance.WindowPosition;
            }

            _bsInventories = new BindingSource(AutoRetainerSortSettings.Instance, "InventoryOptions");
            listBoxInventoryOptions.DataSource = _bsInventories;
            listBoxInventoryOptions.DisplayMember = "Value";

            propertyGridSettings.SelectedObject = AutoRetainerSortSettings.Instance;
        }

        private void Form_Close(object sender, FormClosedEventArgs e)
        {
            AutoRetainerSortSettings.Instance.WindowPosition = Location;
            AutoRetainerSortSettings.Instance.Save();
        }

        private void Listbox_DoubleClick(object sender, EventArgs e)
        {
            var selectedItem = (KeyValuePair<int, InventorySortInfo>)listBoxInventoryOptions.SelectedItem;
            var editForm = new EditInventoryOptionsForm(selectedItem.Value, selectedItem.Key);
            editForm.Show(this);
        }

        private void AddNew_Click(object sender, EventArgs e)
        {
            using (var addNewForm = new AddNewInventoryForm())
            {
                var dr = addNewForm.ShowDialog(this);
                if (dr == DialogResult.Cancel)
                {
                    return;
                }

                AutoRetainerSortSettings.Instance.InventoryOptions.Add(addNewForm.Index, new InventorySortInfo(addNewForm.RetainerName));
                ResetBindingSource();
            }

            AutoRetainerSortSettings.Instance.Save();
        }

        private void ResetBindingSource()
        {
            _bsInventories.DataSource = AutoRetainerSortSettings.Instance;
            _bsInventories.ResetBindings(true);
            listBoxInventoryOptions.ResetBindings();
            listBoxInventoryOptions.Refresh();
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            var selectedItem = (KeyValuePair<int, InventorySortInfo>)listBoxInventoryOptions.SelectedItem;
            if (selectedItem.Key >= ItemSortStatus.SaddlebagInventoryIndex)
            {
                var dr = MessageBox.Show(
                    $"Are you sure you want to delete {selectedItem.Value.Name}?",
                    "Really Delete?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Exclamation);
                if (dr != DialogResult.Yes)
                {
                    return;
                }
            }
            else
            {
                var dr = MessageBox.Show(
                    $"Are you REALLY sure you want to delete the Player Inventory from being sorted?{Environment.NewLine}This is probably going to break things... don't blame me.",
                    Strings.WarningCaption,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Hand);
                if (dr != DialogResult.Yes)
                {
                    return;
                }
            }

            if (AutoRetainerSortSettings.Instance.InventoryOptions.Remove(selectedItem.Key))
            {
                Log.Information($"We've removed {selectedItem.Value.Name} from the list. Good bye, so long!");
            }
            else
            {
                Log.Information($"Something went wrong with trying to remove {selectedItem.Value.Name} from the list... Index: {selectedItem.Key}");
            }

            AutoRetainerSortSettings.Instance.Save();
            ResetBindingSource();
        }

        private void AutoSetup_Click(object sender, EventArgs e)
        {
            if (!AutoRetainerSortSettings.Instance.InventoryOptions.ContainsKey(ItemSortStatus.PlayerInventoryIndex))
            {
                var dr = MessageBox.Show(
                    "It looks like you don't have the player inventory added to the indexes... somehow. Do you want me to re-add that for you?",
                    "Um.",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (dr == DialogResult.Yes)
                {
                    AutoRetainerSortSettings.Instance.InventoryOptions.Add(ItemSortStatus.PlayerInventoryIndex, new InventorySortInfo("Player Inventory"));
                }
            }

            if (!AutoRetainerSortSettings.Instance.InventoryOptions.ContainsKey(ItemSortStatus.SaddlebagInventoryIndex))
            {
                var dr = MessageBox.Show(
                    "It looks like you don't have the chocobo saddlebag added to the indexes. Do you want me to re-add that for you?",
                    "Hey Listen!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (dr == DialogResult.Yes)
                {
                    AutoRetainerSortSettings.Instance.InventoryOptions.Add(ItemSortStatus.SaddlebagInventoryIndex, new InventorySortInfo("Chocobo Saddlebag"));
                }
            }

            MessageBox.Show(
                Strings.AutoSetup_CacheAdvice,
                "Careful!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            var warningResult = MessageBox.Show(
                Strings.AutoSetup_OverwriteWarning,
                "Warning!",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (warningResult == DialogResult.No)
            {
                return;
            }

            var conflictUnsorted = MessageBox.Show(
                Strings.AutoSetup_ConflictQuestion,
                "Conflict?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;

            var newInventorySetup = AutoRetainerSortSettings.Instance.InventoryOptions;

            foreach (var inventorySortInfo in newInventorySetup.Values)
            {
                inventorySortInfo.SortTypes.Clear();
            }

            var orderedRetainerList = RetainerList.Instance.OrderedRetainerList;

            for (var i = 0; i < orderedRetainerList.Length; i++)
            {
                if (newInventorySetup.ContainsKey(i))
                {
                    continue;
                }

                var retInfo = orderedRetainerList[i];
                if (!retInfo.Active)
                {
                    continue;
                }

                newInventorySetup.Add(i, new InventorySortInfo(retInfo.Name));
            }

            AutoRetainerSortSettings.Instance.InventoryOptions = newInventorySetup;

            ItemSortStatus.UpdateFromCache(orderedRetainerList);

            var sortTypeCounts = new Dictionary<SortType, Dictionary<int, int>>();

            foreach (var sortType in Enum.GetValues(typeof(SortType)).Cast<SortType>())
            {
                sortTypeCounts[sortType] = new Dictionary<int, int>();
            }

            foreach (var cachedInventory in ItemSortStatus.GetAllInventories())
            {
                foreach (var sortType in cachedInventory.ItemCounts.Select(x => ItemSortStatus.GetSortInfo(x.Key).SortType))
                {
                    var indexCountDic = sortTypeCounts[sortType];
                    if (indexCountDic.ContainsKey(cachedInventory.Index))
                    {
                        indexCountDic[cachedInventory.Index]++;
                    }
                    else
                    {
                        indexCountDic.Add(cachedInventory.Index, 1);
                    }
                }
            }

            foreach (var typeDicPair in sortTypeCounts)
            {
                var sortType = typeDicPair.Key;
                var indexCount = typeDicPair.Value.Keys.Count;
                if (indexCount == 0)
                {
                    continue;
                }

                if (indexCount > 1 && conflictUnsorted)
                {
                    continue;
                }

                var desiredIndex = typeDicPair.Value.OrderByDescending(x => x.Value).First().Key;
                AutoRetainerSortSettings.Instance.InventoryOptions[desiredIndex].SortTypes.Add(new SortTypeWithCount(sortType));
            }

            ResetBindingSource();
            AutoRetainerSortSettings.Instance.Save();
            Log.Information("Auto-Setup done!");
        }
    }
}