﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using VSDocumentReopen.Domain.Documents;
using VSDocumentReopen.Infrastructure.HistoryCommands;
using VSDocumentReopen.Infrastructure.Logging;
using VSDocumentReopen.VS.ToolWindows.IconHandling.ButtonStates;

namespace VSDocumentReopen.VS.ToolWindows
{
	public partial class ClosedDocumentsHistoryControl
	{
		private void _openSelected_Click(object sender, RoutedEventArgs e)
		{
			HandleOperatons(_reopenSomeDocumentsCommandFactory);
		}

		private void _removeSelected_Click(object sender, RoutedEventArgs e)
		{
			HandleOperatons(_removeSomeDocumentsCommandFactory);
		}

		private void _removeNonExisting_Click(object sender, RoutedEventArgs e)
		{
			var removeNonExistingCommand = _removeSomeDocumentsCommandFactory.CreateCommand(_documentHistoryQueries.GetAll()
				.Where(x => !x.IsValid()).ToArray());
			removeNonExistingCommand.Execute();
			LoggerContext.Current.Logger.Info($"Command: {removeNonExistingCommand.GetType()} from ToolWindow (Remove Non existing) was executed");
		}

		private void _clearAll_Click(object sender, RoutedEventArgs e)
		{
			_clearHistoryCommand.Execute();
			LoggerContext.Current.Logger.Info($"Command: {_clearHistoryCommand.GetType()} from ToolWindow (Clear all) was executed");
		}

		private void _search_OnSearch(object sender, RoutedEventArgs e)
		{
			HandleSearch();
		}

		private void _listView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (((FrameworkElement)e.OriginalSource).DataContext is ClosedDocumentHistoryItem item)
			{
				var command = _reopenSomeDocumentsCommandFactory.CreateCommand(item.ClosedDocument);
				command.Execute();
				LoggerContext.Current.Logger.Info($"Command: {command.GetType()} from ToolWindow was executed");
			}
		}

		private void _listView_OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				HandleOperatons(_removeSomeDocumentsCommandFactory);
			}
		}

		private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_listView.SelectedItems.Count > 0)
			{
				_openSelected.GetImageButtonState().Enable();
				_removeSelected.GetImageButtonState().Enable();
				return;
			}

			_openSelected.GetImageButtonState().Disable();
			_removeSelected.GetImageButtonState().Disable();
		}

		private void _listViewColumnHeader_Click(object sender, RoutedEventArgs e)
		{
			var column = (sender as GridViewColumnHeader);
			string sortBy = column.Tag.ToString();

			if (listViewSortCol != null)
			{
				AdornerLayer.GetAdornerLayer(listViewSortCol)?.Remove(listViewSortAdorner);
				_listView.Items.SortDescriptions.Clear();
			}

			var newDir = ListSortDirection.Ascending;
			if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
			{
				newDir = ListSortDirection.Descending;
			}

			listViewSortCol = column;
			listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);

			AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
			_listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
		}

		private void _listViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var header = ((GridViewColumnHeader)sender);
			var min = (header.Content as TextBlock)?.MinWidth ?? 35;

			if (e.NewSize.Width < min)
			{
				e.Handled = true;
				header.Column.Width = min;
			}
		}

		private void _listViewRemoveSort_Click(object sender, RoutedEventArgs e)
		{
			if (listViewSortCol != null)
			{
				AdornerLayer.GetAdornerLayer(listViewSortCol)?.Remove(listViewSortAdorner);
				_listView.Items.SortDescriptions.Clear();
				_listView.Items.Refresh();

				listViewSortCol = null;
				listViewSortAdorner = null;
			}
		}

		private void _listViewShowColumns_Click(object sender, RoutedEventArgs e)
		{
			var menu = e?.OriginalSource as MenuItem;
			if (menu is null)
			{
				return;
			}

			if (!menu.IsChecked) //remove
			{
				HideColumn(menu.Header.ToString());
			}
			else //add
			{
				var column = _hiddenColumns.SingleOrDefault(x => x.GetGridViewHeaderText() == menu.Header.ToString());
				if (column is null)
				{
					return;
				}
				_listViewContect.Columns.Insert(int.Parse(menu.Uid), column);
				_hiddenColumns.Remove(column);
			}
		}
		private void HideColumn(string header)
		{
			var column = _listViewContect.Columns.SingleOrDefault(x => x.GetGridViewHeaderText() == header);
			if (column is null)
			{
				return;
			}

			_listViewContect.Columns.Remove(column);
			_hiddenColumns.Add(column);
		}
		private void SetColumnData(string header, int index, double width = 0)
		{
			var column = _listViewContect.Columns.SingleOrDefault(x => x.GetGridViewHeaderText() == header);
			if (column is null)
			{
				return;
			}

			if (width <= 0)
			{
				width = (column.Header as GridViewColumnHeader).MinWidth;
			}

			_listViewContect.Columns.Remove(column);
			_listViewContect.Columns.Insert(index, column);

			column.Width = width <= 35 ? 35 : width;
		}

		private void _resetShowColumnMenu_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in _hiddenColumns)
			{
				_listViewContect.Columns.Add(item);
			}
			_hiddenColumns.Clear();

			foreach (var item in _columnHeaders)
			{
				SetColumnData(item.Value, item.Key);
			}
		}

		private void HandleOperatons(IHistoryCommandFactory historyCommandFactory)
		{
			var selectedItems = _listView.SelectedItems.Cast<ClosedDocumentHistoryItem>()
				.Select(s => s.ClosedDocument);

			var command = historyCommandFactory.CreateCommand(selectedItems.ToArray());
			command.Execute();

			LoggerContext.Current.Logger.Info($"Command: {command.GetType()} from ToolWindow was executed");
		}

		private void HandleSearch()
		{
			var searchText = _search.Text;

			if (string.IsNullOrWhiteSpace(searchText))
			{
				UpdateHistoryView(GetFullHistory);
			}
			else
			{
				searchText = searchText.ToLower();
				UpdateHistoryView((doc) => doc.FullName.ToLower().Contains(searchText) || doc.Name.ToLower().Contains(searchText));
			}
		}

		private void UpdateHistoryView(Func<IClosedDocument, bool> documentFilter)
		{
			_listView.Items.Clear();

			var history = _documentHistoryQueries.GetAll();
			var i = 1;

			foreach (var doc in history)
			{
				if (documentFilter(doc))
				{
					_listView.Items.Add(new ClosedDocumentHistoryItem(doc, i,
						GetFileTypeBitmapSource(doc)));
				}
				i++;
			}

			var count = i - 1;
			if (count > 0)
			{
				_clearAll.GetImageButtonState().Enable();
			}
			else
			{
				_clearAll.GetImageButtonState().Disable();
			}

			var nonExisting = _documentHistoryQueries.GetAll().Where(x => !x.IsValid());
			if (nonExisting.Any())
			{
				_removeNonExisting.GetImageButtonState().Enable();
			}
			else
			{
				_removeNonExisting.GetImageButtonState().Disable();
			}

			_numberOfItems.Content = string.IsNullOrWhiteSpace(_search.Text)
				? count.ToString()
				: $"{_listView.Items.Count}/{count}";

			//sort
			if (_listView.Items.SortDescriptions.Count > 0)
			{
				var sort = _listView.Items.SortDescriptions.First();

				_listView.Items.SortDescriptions.Clear();
				_listView.Items.SortDescriptions.Add(sort);

				_listView.Items.Refresh();
			}
		}
	}
}